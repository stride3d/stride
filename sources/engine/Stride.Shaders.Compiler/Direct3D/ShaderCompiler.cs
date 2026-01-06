// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using System;
using System.Linq;
using System.Text;
using System.Runtime.CompilerServices;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D.Compilers;

using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Core.UnsafeExtensions;
using Stride.Graphics;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Core.UnsafeExtensions.StringMarshal;

namespace Stride.Shaders.Compiler.Direct3D
{
    /// <summary>
    ///   Provides functionality to compile Shader source code into bytecode for various Shader stages
    ///   using the Direct3D compiler APIs (<c>fxc</c>).
    ///   It also handles Shader reflection to provide metadata about the compiled Shaders.
    /// </summary>
    internal unsafe class ShaderCompiler : IShaderCompiler
    {
        // D3DCOMPILE constants from d3dcompiler.h in the Windows SDK for Windows 10.0.22621.0

        public const uint D3DCOMPILE_DEBUG = (1 << 0);
        public const uint D3DCOMPILE_SKIP_OPTIMIZATION = (1 << 2);
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL0 = (1 << 14);
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL1 = 0;
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL2 = ((1 << 14) | (1 << 15));
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL3 = (1 << 15);


        /// <summary>
        ///   Compiles the specified Shader source code into byte-code for a given shader stage
        ///   using the Direct3D Compiler APIs (<c>fxc</c>).
        /// </summary>
        /// <param name="shaderSource">The source code of the Shader to compile.</param>
        /// <param name="entryPoint">The entry point function name within the Shader source.</param>
        /// <param name="stage">
        ///   The Shader stage for which the byte-code is being compiled
        ///   (e.g., <see cref="ShaderStage.Vertex"/>, <see cref="ShaderStage.Pixel"/>).
        /// </param>
        /// <param name="effectParameters">
        ///   A set of parameters that influence the compilation process, such as debug and optimization settings.
        /// </param>
        /// <param name="reflection">An object to be updated with reflection data from the compiled Shader.</param>
        /// <param name="sourceFilename">The optional filename of the Shader source, used for error reporting.</param>
        /// <returns>
        ///   A <see cref="ShaderBytecodeResult"/> containing the compiled Shader byte-code and any warnings or errors
        ///   encountered during compilation.
        /// </returns>
        /// <exception cref="ArgumentException">The specified Shader <paramref name="stage"/> is not supported.</exception>
        /// <exception cref="ArgumentException">
        ///   The specified <see cref="EffectCompilerParameters.Profile"/> in <paramref name="effectParameters"/> is not supported.
        /// </exception>
        /// <exception cref="NotImplementedException">
        ///   During reflection, if an unsupported <see cref="EffectParameterClass"/> or <see cref="EffectParameterType"/>
        ///   is encountered.
        /// </exception>
        public ShaderBytecodeResult Compile(string shaderSource, string entryPoint, ShaderStage stage,
                                            EffectCompilerParameters effectParameters, EffectReflection reflection,
                                            string? sourceFilename = null)
        {
            var d3dCompiler = D3DCompiler.GetApi();

            var isDebug = effectParameters.Debug;
            var optimLevel = effectParameters.OptimizationLevel;
            var profile = effectParameters.Profile;

            var shaderModel = ShaderStageToString(stage) + "_" + ShaderProfileFromGraphicsProfile(profile);

            uint effectFlags = 0;
            uint shaderFlags = 0;

            if (isDebug)
            {
                shaderFlags = D3DCOMPILE_DEBUG;

                // Somehow, this makes the compiler crash with internal errors in some cases when using loops
                //if (profile >= GraphicsProfile.Level_10_0)
                //    shaderFlags |= D3DCOMPILE_SKIP_OPTIMIZATION;
            }
            else switch (optimLevel)
            {
                case 0: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL0; break;
                case 1: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL1; break;
                case 2: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL2; break;
                case 3: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL3; break;
            }

            ComPtr<ID3D10Blob> bytecode = default;
            ComPtr<ID3D10Blob> compileErrors = default;
            var bytecodeResult = new ShaderBytecodeResult();

            HResult result = Compile();

            if (result.IsSuccess && bytecode.Handle is not null)
            {
                // TODO: Make this optional
                bytecodeResult.DisassembleText = Disassemble(bytecode);

                // As Effect bytecode binary can change when having debug info (with d3dcompiler_47),
                // we are calculating a bytecodeId on the stripped version
                using ComPtr<ID3D10Blob> strippedBytecode = Strip(bytecode);
                var bytecodeId = ObjectId.FromBytes(strippedBytecode.Handle->Buffer);

                var bytecodeBuffer = bytecode.Handle->Buffer;
                bytecodeResult.Bytecode = new ShaderBytecode(bytecodeId, bytecodeBuffer.ToArray()) { Stage = stage };

                // If compilation succeeded, then we can update reflection
                UpdateReflection(bytecodeResult.Bytecode, reflection, bytecodeResult);

                if (compileErrors.Handle is not null)
                {
                    bytecodeResult.Warning(GetTextFromBlob(compileErrors));
                }
            }

            bytecode.Dispose();
            compileErrors.Dispose();

            return bytecodeResult;


            //
            // Returns a string representation of the Shader stage.
            //
            static string ShaderStageToString(ShaderStage stage)
            {
                return stage switch
                {
                    ShaderStage.Compute => "cs",
                    ShaderStage.Vertex => "vs",
                    ShaderStage.Hull => "hs",
                    ShaderStage.Domain => "ds",
                    ShaderStage.Geometry => "gs",
                    ShaderStage.Pixel => "ps",

                    _ => throw new ArgumentException("Shader Stage not supported.", nameof(stage))
                };
            }

            //
            // Returns the Shader profile string based on the specified Graphics Profile.
            //
            static string ShaderProfileFromGraphicsProfile(GraphicsProfile graphicsProfile)
            {
                return graphicsProfile switch
                {
                    GraphicsProfile.Level_9_1 => "4_0_level_9_1",
                    GraphicsProfile.Level_9_2 => "4_0_level_9_2",
                    GraphicsProfile.Level_9_3 => "4_0_level_9_3",
                    GraphicsProfile.Level_10_0 => "4_0",
                    GraphicsProfile.Level_10_1 => "4_1",
                    GraphicsProfile.Level_11_0 or GraphicsProfile.Level_11_1 => "5_0",

                    _ => throw new ArgumentException("Graphics Profile not supported.", nameof(graphicsProfile))
                };
            }


            //
            // Compiles the Shader source code to byte-code for the specified Shader Model.
            //
            HResult Compile()
            {
                var shaderSourceSpan = shaderSource.GetAsciiSpan();
                var shaderSourceLength = (nuint) shaderSourceSpan.Length;

                ref var noSourceName = ref NullRef<byte>();

                ref var noDefines = ref NullRef<D3DShaderMacro>();
                var noIncludes = default(ComPtr<ID3DInclude>);

                var entryPointSpan = entryPoint.GetUtf8Span();

                var shaderModelSpan = shaderModel.GetUtf8Span();

                HResult result = d3dCompiler.Compile(in shaderSourceSpan[0], shaderSourceLength, in noSourceName,
                                                     in noDefines, noIncludes, in entryPointSpan[0], in shaderModelSpan[0],
                                                     shaderFlags, effectFlags,
                                                     ref bytecode, ref compileErrors);

                if (result.IsFailure || bytecode.Handle is null)
                {
                    // Log compilation errors
                    if (compileErrors.Handle is not null)
                    {
                        bytecodeResult.Error(GetTextFromBlob(compileErrors));
                    }
                }

                return result;
            }

            //
            // Disassembles a blob of Shader byte-code to its textual equivalent in HLSL code.
            //
            string Disassemble(ComPtr<ID3D10Blob> bytecode)
            {
                ref var noComments = ref NullRef<byte>();

                ComPtr<ID3D10Blob> disassembly = default;

                d3dCompiler.Disassemble(bytecode.GetBufferPointer(), bytecode.GetBufferSize(), Flags: 0,
                                        in noComments, ref disassembly);

                string shaderDisassembly = GetTextFromBlob(disassembly);
                disassembly.Dispose();

                return shaderDisassembly;
            }

            //
            // Gets a blob of Shader byte-code with debug and reflection data stripped out.
            //
            ComPtr<ID3D10Blob> Strip(ComPtr<ID3D10Blob> bytecode)
            {
                const uint StripDebugAndReflection = (uint) (CompilerStripFlags.ReflectionData | CompilerStripFlags.DebugInfo);

                var bytecodePointer = bytecode.Handle->GetBufferPointer();
                var bytecodeSize = bytecode.Handle->GetBufferSize();

                ComPtr<ID3D10Blob> strippedBytecode = default;

                HResult result = d3dCompiler.StripShader(bytecodePointer, bytecodeSize,
                                                         StripDebugAndReflection, ref strippedBytecode);
                if (result.IsFailure)
                    return null;

                return strippedBytecode;
            }

            //
            // Updates the reflection information for a Shader byte-code.
            //
            void UpdateReflection(ShaderBytecode shaderBytecode, EffectReflection effectReflection, LoggerResult log)
            {
                var bytecode = shaderBytecode.Data;

                ComPtr<ID3D11ShaderReflection> shaderReflection = Reflect(bytecode);

                SkipInit(out ShaderDesc shaderReflectionDesc);
                shaderReflection.GetDesc(ref shaderReflectionDesc);

                // Adjust the Constant Buffer size, and compute the offsets and sizes of its members
                foreach (var constantBuffer in effectReflection.ConstantBuffers)
                {
                    UpdateConstantBufferReflection(constantBuffer);
                }

                // Constant Buffers
                for (uint i = 0; i < shaderReflectionDesc.ConstantBuffers; ++i)
                {
                    var constantBuffer = ToComPtr(shaderReflection.GetConstantBufferByIndex(i));

                    SkipInit(out ShaderBufferDesc constantBufferDesc);
                    constantBuffer.GetDesc(ref constantBufferDesc);

                    if (constantBufferDesc.Type == D3DCBufferType.D3DCTResourceBindInfo)
                        continue;

                    string constantBufferName = GetUtf8Span(constantBufferDesc.Name).GetString();
                    var linkBuffer = effectReflection.ConstantBuffers.First(buffer => buffer.Name == constantBufferName);

                    ValidateConstantBufferReflection(constantBuffer, ref constantBufferDesc, linkBuffer, log);
                }

                // Bound Resources
                for (uint i = 0; i < shaderReflectionDesc.BoundResources; ++i)
                {
                    SkipInit(out ShaderInputBindDesc boundResourceDesc);
                    shaderReflection.GetResourceBindingDesc(i, ref boundResourceDesc);

                    string linkKeyName = null;
                    string resourceGroup = null;
                    string logicalGroup = null;
                    var elementType = default(EffectTypeDescription);

                    var resourceName = GetUtf8Span(boundResourceDesc.Name).GetString();

                    foreach (var linkResource in effectReflection.ResourceBindings)
                    {
                        if (linkResource.RawName == resourceName && linkResource.Stage == ShaderStage.None)
                        {
                            linkKeyName = linkResource.KeyInfo.KeyName;
                            resourceGroup = linkResource.ResourceGroup;
                            logicalGroup = linkResource.LogicalGroup;
                            elementType = linkResource.ElementType;
                            break;
                        }
                    }

                    if (linkKeyName is null)
                    {
                        log.Error($"Resource [{resourceName}] has no link");
                    }
                    else
                    {
                        var binding = GetResourceBinding(in boundResourceDesc, linkKeyName);
                        binding.Stage = shaderBytecode.Stage;
                        binding.ResourceGroup = resourceGroup;
                        binding.LogicalGroup = logicalGroup;
                        binding.ElementType = elementType;

                        effectReflection.ResourceBindings.Add(binding);
                    }
                }

                shaderReflection.Dispose();

                //
                // Gets reflection information about a Shader from its byte-code.
                //
                ComPtr<ID3D11ShaderReflection> Reflect(byte[] bytecode)
                {
                    HResult result = d3dCompiler.Reflect(in bytecode[0], (nuint) bytecode.Length,
                                                         out ComPtr<ID3D11ShaderReflection> shaderReflection);
                    if (result.IsFailure)
                        result.Throw();

                    return shaderReflection;
                }

                //
                // Given a Constant Buffer description, updates the sizes and offsets of its members,
                // as well as its total size (aligned to 16 bytes).
                //
                void UpdateConstantBufferReflection(EffectConstantBufferDescription reflectionConstantBuffer)
                {
                    // Used to compute Constant Buffer size and member offsets (std140 rule)
                    int constantBufferOffset = 0;

                    // Fill members
                    for (int index = 0; index < reflectionConstantBuffer.Members.Length; index++)
                    {
                        var member = reflectionConstantBuffer.Members[index];

                        // Properly compute size and offset according to DirectX rules
                        var memberSize = ComputeMemberSize(ref member.Type, ref constantBufferOffset);

                        member.Offset = constantBufferOffset;
                        member.Size = memberSize;

                        // Adjust offset for next item
                        constantBufferOffset += memberSize;

                        reflectionConstantBuffer.Members[index] = member;
                    }

                    // Round buffer size to next multiple of 16 bytes
                    reflectionConstantBuffer.Size = (constantBufferOffset + 15) / 16 * 16;
                }

                //
                // Validates the reflection of a Constant Buffer against the expected description.
                //
                void ValidateConstantBufferReflection(ComPtr<ID3D11ShaderReflectionConstantBuffer> constantBufferRaw,
                                                      ref ShaderBufferDesc constantBufferRawDesc,
                                                      EffectConstantBufferDescription constantBuffer,
                                                      LoggerResult log)
                {
                    switch (constantBufferRawDesc.Type)
                    {
                        case D3DCBufferType.D3DCTCbuffer:
                            if (constantBuffer.Type != ConstantBufferType.ConstantBuffer)
                                log.Error($"Invalid Buffer type for \"{constantBuffer.Name}\": {constantBuffer.Type} instead of {ConstantBufferType.ConstantBuffer}");
                            break;

                        case D3DCBufferType.D3DCTTbuffer:
                            if (constantBuffer.Type != ConstantBufferType.TextureBuffer)
                                log.Error($"Invalid Buffer type for \"{constantBuffer.Name}\": {constantBuffer.Type} instead of {ConstantBufferType.TextureBuffer}");
                            break;

                        default:
                            if (constantBuffer.Type != ConstantBufferType.Unknown)
                                log.Error($"Invalid Buffer type for \"{constantBuffer.Name}\": {constantBuffer.Type} instead of {ConstantBufferType.Unknown}");
                            break;
                    }

                    // Constant Buffer variables
                    for (uint i = 0; i < constantBufferRawDesc.Variables; i++)
                    {
                        var variable = ToComPtr(constantBufferRaw.GetVariableByIndex(i));

                        SkipInit(out ShaderVariableDesc variableDescription);
                        variable.GetDesc(ref variableDescription);

                        // NOTE: To force to call the GetType() of ID3D11ShaderReflectionVariable and not the GetType() of System.Object
                        var variableType = ToComPtr(variable.Handle->GetType());

                        SkipInit(out ShaderTypeDesc variableTypeDescription);
                        variableType.GetDesc(ref variableTypeDescription);

                        var variableName = GetUtf8Span(variableDescription.Name).GetString();
                        if (variableTypeDescription.Offset != 0)
                        {
                            log.Error($"Unexpected offset [{variableTypeDescription.Offset}] for variable [{variableName}] in Constant Buffer [{constantBuffer.Name}]");
                        }

                        var binding = constantBuffer.Members[i];

                        // Retrieve Link Member
                        if (binding.RawName != variableName)
                        {
                            log.Error($"Variable [{variableName}] in Constant Buffer [{constantBuffer.Name}] has no link");
                        }
                        else
                        {
                            var parameter = new EffectValueDescription()
                            {
                                Type =
                                {
                                    Class = (EffectParameterClass) variableTypeDescription.Class,
                                    Type = ConvertVariableValueType(variableTypeDescription.Type, log),
                                    Elements = (int) variableTypeDescription.Elements,
                                    RowCount = (byte) variableTypeDescription.Rows,
                                    ColumnCount = (byte) variableTypeDescription.Columns
                                },
                                RawName = variableName,
                                Offset = (int) variableDescription.StartOffset,
                                Size = (int) variableDescription.Size
                            };

                            if (parameter.Offset != binding.Offset ||
                                parameter.Size != binding.Size ||
                                parameter.Type.Elements != binding.Type.Elements ||
                                // Ignore columns/rows if it's a struct (sometimes it contains weird data)
                                ((parameter.Type.Class != EffectParameterClass.Struct) &&
                                 (parameter.Type.RowCount != binding.Type.RowCount || parameter.Type.ColumnCount != binding.Type.ColumnCount)))
                            {
                                log.Error($"Variable [{variableName}] in Constant Buffer [{constantBuffer.Name}] binding doesn't match what was expected");
                            }
                        }
                    }
                    if (constantBuffer.Size != constantBufferRawDesc.Size)
                    {
                        log.Error($"Error precomputing Constant Buffer size for \"{constantBuffer.Name}\": {constantBuffer.Size} instead of {constantBufferRawDesc.Size}");
                    }
                }

                //
                // Computes the size of a member type, including its alignment and array size.
                // It does so recursively for structs, and handles different parameter classes.
                //
                static int ComputeMemberSize(ref EffectTypeDescription memberType, ref int constantBufferOffset)
                {
                    var elementSize = ComputeTypeSize(memberType.Type);
                    int size;
                    int alignment = 4;

                    switch (memberType.Class)
                    {
                        case EffectParameterClass.Struct:
                        {
                            // Fill members
                            size = 0;
                            for (int index = 0; index < memberType.Members.Length; index++)
                            {
                                // Properly compute size and offset according to DX rules
                                var memberSize = ComputeMemberSize(ref memberType.Members[index].Type, ref size);

                                // Align offset and store it as member offset
                                memberType.Members[index].Offset = size;

                                // Adjust offset for next item
                                size += memberSize;
                            }
                            alignment = size;
                            break;
                        }
                        case EffectParameterClass.Scalar:
                        {
                            size = elementSize;
                            break;
                        }
                        case EffectParameterClass.Color:
                        case EffectParameterClass.Vector:
                        {
                            size = elementSize * memberType.ColumnCount;
                            break;
                        }
                        case EffectParameterClass.MatrixColumns:
                        {
                            size = elementSize * (4 * (memberType.ColumnCount - 1) + memberType.RowCount);
                            break;
                        }
                        case EffectParameterClass.MatrixRows:
                        {
                            size = elementSize * (4 * (memberType.RowCount - 1) + memberType.ColumnCount);
                            break;
                        }
                        default:
                            throw new NotImplementedException("An unknown EffectParameterClass was found.");
                    }

                    // Update element size
                    memberType.ElementSize = size;

                    // Array
                    if (memberType.Elements > 0)
                    {
                        var roundedSize = (size + 15) / 16 * 16; // Round up to 16 bytes (size of float4)
                        size += roundedSize * (memberType.Elements - 1);
                        alignment = 16;
                    }

                    // Align to float4 if it is bigger than leftover space in current float4
                    if (constantBufferOffset / 16 != (constantBufferOffset + size - 1) / 16)
                        alignment = 16;

                    // Align offset and store it as member offset
                    constantBufferOffset = (constantBufferOffset + alignment - 1) / alignment * alignment;

                    return size;
                }

                //
                // Computes the size of a type based on its EffectParameterType.
                //
                static int ComputeTypeSize(EffectParameterType type)
                {
                    return type switch
                    {
                        EffectParameterType.Bool or
                        EffectParameterType.Float or
                        EffectParameterType.Int or
                        EffectParameterType.UInt => 4,

                        EffectParameterType.Double => 8,

                        EffectParameterType.Void => 0,

                        _ => throw new NotImplementedException("An unknown EffectParameterType was found.")
                    };
                }

                //
                // Creates a resource binding description from a Shader input binding description.
                //
                EffectResourceBindingDescription GetResourceBinding(ref readonly ShaderInputBindDesc bindingDescriptionRaw, string name)
                {
                    var paramClass = EffectParameterClass.Object;
                    var paramType = EffectParameterType.Void;

                    switch (bindingDescriptionRaw.Type)
                    {
                        case D3DShaderInputType.D3DSitTbuffer:
                            paramType = EffectParameterType.TextureBuffer;
                            paramClass = EffectParameterClass.TextureBuffer;
                            break;

                        case D3DShaderInputType.D3DSitCbuffer:
                            paramType = EffectParameterType.ConstantBuffer;
                            paramClass = EffectParameterClass.ConstantBuffer;
                            break;

                        case D3DShaderInputType.D3DSitTexture:
                            paramClass = EffectParameterClass.ShaderResourceView;
                            paramType = bindingDescriptionRaw.Dimension switch
                            {
                                D3DSrvDimension.D3DSrvDimensionBuffer => EffectParameterType.Buffer,
                                D3DSrvDimension.D3DSrvDimensionTexture1D => EffectParameterType.Texture1D,
                                D3DSrvDimension.D3DSrvDimensionTexture1Darray => EffectParameterType.Texture1DArray,
                                D3DSrvDimension.D3DSrvDimensionTexture2D => EffectParameterType.Texture2D,
                                D3DSrvDimension.D3DSrvDimensionTexture2Darray => EffectParameterType.Texture2DArray,
                                D3DSrvDimension.D3DSrvDimensionTexture2Dms => EffectParameterType.Texture2DMultisampled,
                                D3DSrvDimension.D3DSrvDimensionTexture2Dmsarray => EffectParameterType.Texture2DMultisampledArray,
                                D3DSrvDimension.D3DSrvDimensionTexture3D => EffectParameterType.Texture3D,
                                D3DSrvDimension.D3DSrvDimensionTexturecube => EffectParameterType.TextureCube,
                                D3DSrvDimension.D3DSrvDimensionTexturecubearray => EffectParameterType.TextureCubeArray,
                                _ => EffectParameterType.Void
                            };
                            break;

                        case D3DShaderInputType.D3DSitStructured:
                            paramClass = EffectParameterClass.ShaderResourceView;
                            paramType = EffectParameterType.StructuredBuffer;
                            break;

                        case D3DShaderInputType.D3DSitByteaddress:
                            paramClass = EffectParameterClass.ShaderResourceView;
                            paramType = EffectParameterType.ByteAddressBuffer;
                            break;

                        case D3DShaderInputType.D3DSitUavRwtyped:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = bindingDescriptionRaw.Dimension switch
                            {
                                D3DSrvDimension.D3DSrvDimensionBuffer => EffectParameterType.RWBuffer,
                                D3DSrvDimension.D3DSrvDimensionTexture1D => EffectParameterType.RWTexture1D,
                                D3DSrvDimension.D3DSrvDimensionTexture1Darray => EffectParameterType.RWTexture1DArray,
                                D3DSrvDimension.D3DSrvDimensionTexture2D => EffectParameterType.RWTexture2D,
                                D3DSrvDimension.D3DSrvDimensionTexture2Darray => EffectParameterType.RWTexture2DArray,
                                D3DSrvDimension.D3DSrvDimensionTexture3D => EffectParameterType.RWTexture3D,
                                _ => EffectParameterType.Void
                            };
                            break;

                        case D3DShaderInputType.D3DSitUavRwstructured:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = EffectParameterType.RWStructuredBuffer;
                            break;

                        case D3DShaderInputType.D3DSitUavRwbyteaddress:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = EffectParameterType.RWByteAddressBuffer;
                            break;

                        case D3DShaderInputType.D3DSitUavAppendStructured:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = EffectParameterType.AppendStructuredBuffer;
                            break;

                        case D3DShaderInputType.D3DSitUavConsumeStructured:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = EffectParameterType.ConsumeStructuredBuffer;
                            break;

                        case D3DShaderInputType.D3DSitUavRwstructuredWithCounter:
                            paramClass = EffectParameterClass.UnorderedAccessView;
                            paramType = EffectParameterType.RWStructuredBuffer;
                            break;

                        case D3DShaderInputType.D3DSitSampler:
                            paramClass = EffectParameterClass.Sampler;
                            paramType = EffectParameterType.Sampler;
                            break;
                    }

                    var binding = new EffectResourceBindingDescription()
                    {
                        KeyInfo = { KeyName = name },
                        RawName = GetUtf8Span(bindingDescriptionRaw.Name).GetString(),
                        Class = paramClass,
                        Type = paramType,
                        SlotStart = (int) bindingDescriptionRaw.BindPoint,
                        SlotCount = (int) bindingDescriptionRaw.BindCount
                    };

                    return binding;
                }

                //
                // Converts a D3DShaderVariableType to an EffectParameterType.
                //
                static EffectParameterType ConvertVariableValueType(D3DShaderVariableType type, LoggerResult log)
                {
                    if (MapType(type) is EffectParameterType effectParameterType)
                        return effectParameterType;

                    log.Error($"Type [{type}] from D3DCompiler not supported");
                    return default;

                    //
                    // Maps a D3DShaderVariableType to an EffectParameterType.
                    // Returns null if the type is not supported.
                    //
                    static EffectParameterType? MapType(D3DShaderVariableType type)
                    {
                        return type switch
                        {
                            D3DShaderVariableType.D3DSvtVoid => EffectParameterType.Void,
                            D3DShaderVariableType.D3DSvtBool => EffectParameterType.Bool,
                            D3DShaderVariableType.D3DSvtInt => EffectParameterType.Int,
                            D3DShaderVariableType.D3DSvtFloat => EffectParameterType.Float,
                            D3DShaderVariableType.D3DSvtUint => EffectParameterType.UInt,
                            D3DShaderVariableType.D3DSvtUint8 => EffectParameterType.UInt8,
                            D3DShaderVariableType.D3DSvtDouble => EffectParameterType.Double,

                            _ => null
                        };
                    }
                }
            }

            //
            // Gets text data from a ID3D10Blob as a string.
            //
            static string GetTextFromBlob(ComPtr<ID3D10Blob> blob)
            {
                if (blob.Handle is null)
                    return null;

                var blobBuffer = blob.Handle->Buffer;
                return blobBuffer.GetString();
            }

            //
            // Creates a wrapping ComPtr from the unsafe pointer to a COM object, but without calling AddRef()
            // in the process.
            // This is a convenience method to avoid unnecessary reference counting, as implicit conversions
            // in the ComPtr<T> class will call AddRef() automatically.
            //
            // NOTE: This is a mirror from the ComPtrHelpers type.
            //
            static ComPtr<T> ToComPtr<T>(T* comPtr) where T : unmanaged, IComVtbl<T>
            {
                return new ComPtr<T> { Handle = comPtr };
            }
        }
    }
}

#endif
