// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.Direct3D.Compilers;
using Stride.Core.Diagnostics;
using Stride.Core.Storage;
using Stride.Graphics;

using static Stride.Core.UnsafeExtensions.StringMarshal;
using static Stride.Core.UnsafeExtensions.UnsafeUtilities;

namespace Stride.Shaders.Compiler.Direct3D
{
    internal unsafe class ShaderCompiler : IShaderCompiler
    {
        // D3DCOMPILE constants from d3dcompiler.h in the Windows SDK for Windows 10.0.22621.0

        public const uint D3DCOMPILE_DEBUG = (1 << 0);
        public const uint D3DCOMPILE_SKIP_OPTIMIZATION = (1 << 2);
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL0 = (1 << 14);
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL1 = 0;
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL2 = ((1 << 14) | (1 << 15));
        public const uint D3DCOMPILE_OPTIMIZATION_LEVEL3 = (1 << 15);


        public ShaderBytecodeResult Compile(string shaderSource, string entryPoint, ShaderStage stage,
                                            EffectCompilerParameters effectParameters, EffectReflection reflection,
                                            string sourceFilename = null)
        {
            var isDebug = effectParameters.Debug;
            var optimLevel = effectParameters.OptimizationLevel;
            var profile = effectParameters.Profile;

            var shaderModel = ShaderStageToString(stage) + "_" + ShaderProfileFromGraphicsProfile(profile);

            uint effectFlags = 0;
            uint shaderFlags = 0;

            if (isDebug)
            {
                shaderFlags = D3DCOMPILE_DEBUG | D3DCOMPILE_SKIP_OPTIMIZATION;
            }
            switch (optimLevel)
            {
                case 0: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL0; break;
                case 1: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL1; break;
                case 2: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL2; break;
                case 3: shaderFlags |= D3DCOMPILE_OPTIMIZATION_LEVEL3; break;
            }

            var shaderBytes = shaderSource.GetAsciiSpan();

            ID3D10Blob* byteCode = null;
            ID3D10Blob* compileErrors = null;
            var byteCodeResult = new ShaderBytecodeResult();

            HResult result = Compile(shaderSource, entryPoint, shaderModel, shaderFlags, effectFlags,
                                     &byteCode, &compileErrors, byteCodeResult);

            if (result.IsSuccess && byteCode != null)
            {
                // TODO: Make this optional
                byteCodeResult.DisassembleText = Disassemble(byteCode);

                // As effect bytecode binary can change when having debug info (with d3dcompiler_47), we are calculating a bytecodeId on the stripped version
                var strippedByteCode = Strip(byteCode);

                var bytecodeId = ObjectId.FromBytes(strippedByteCode->Buffer);
                byteCodeResult.Bytecode = new ShaderBytecode(bytecodeId, byteCode->Buffer.ToArray()) { Stage = stage };

                Free(strippedByteCode);

                // If compilation succeeded, then we can update reflection
                UpdateReflection(byteCodeResult.Bytecode, reflection, byteCodeResult);

                if (compileErrors != null)
                {
                    byteCodeResult.Warning(GetTextFromBlob(compileErrors));
                }
            }

            Free(byteCode);
            Free(compileErrors);

            return byteCodeResult;

            /// <summary>
            ///   Compiles shader source code to bytecode for the specified shader model.
            /// </summary>
            static HResult Compile(string shaderSource, string entryPoint, string shaderModel,
                                   uint shaderFlags, uint effectFlags,
                                   ID3D10Blob** bytecode, ID3D10Blob** errorMessages,
                                   ShaderBytecodeResult bytecodeResult)
            {
                HResult result;

                var d3dCompiler = D3DCompiler.GetApi();

                var shaderSourceSpan = shaderSource.GetUtf8Span();

                fixed (sbyte* pShaderSource = shaderSourceSpan)
                fixed (sbyte* pEntryPoint = entryPoint.GetUtf8Span())
                fixed (sbyte* pShaderModel = shaderModel.GetUtf8Span())
                fixed (sbyte* pShaderSourceName = nameof(shaderSource).GetUtf8Span())
                {
                    result = d3dCompiler.Compile(pShaderSource, (nuint) shaderSourceSpan.Length, (byte*) pShaderSourceName,
                                                 pDefines: null, pInclude: null, (byte*) pEntryPoint, (byte*) pShaderModel, shaderFlags, effectFlags,
                                                 bytecode, errorMessages);
                }

                if (result.IsFailure || bytecode == null)
                {
                    // Log compilation errors
                    if (errorMessages is not null)
                    {
                        bytecodeResult.Error(GetTextFromBlob(*errorMessages));
                    }
                }

                return result;
            }

            /// <summary>
            ///   Disassembles a blob of shader bytecode to its textual equivalent in HLSL code.
            /// </summary>
            string Disassemble(ID3D10Blob* byteCode)
            {
                ID3D10Blob* disassembly = null;

                var d3dCompiler = D3DCompiler.GetApi();

                d3dCompiler.Disassemble(byteCode->GetBufferPointer(), byteCode->GetBufferSize(), Flags: 0,
                                        szComments: in NullRef<byte>(), ref disassembly);

                string shaderDisassembly = GetTextFromBlob(disassembly);
                Free(disassembly);
                return shaderDisassembly;
            }

            /// <summary>
            ///   Gets a blob of shader bytecode with debug and reflection data stripped out.
            /// </summary>
            ID3D10Blob* Strip(ID3D10Blob* byteCode)
            {
                ID3D10Blob* strippedByteCode = null;

                const uint StripDebugAndReflection = (uint) (CompilerStripFlags.ReflectionData | CompilerStripFlags.DebugInfo);

                var d3dCompiler = D3DCompiler.GetApi();

                HResult result = d3dCompiler.StripShader(byteCode->GetBufferPointer(), byteCode->GetBufferSize(),
                                                         StripDebugAndReflection, ref strippedByteCode);
                if (result.IsFailure)
                    return null;

                return strippedByteCode;
            }

            /// <summary>
            ///   Gets text data from a <see cref="ID3D10Blob"/> as a <see cref="string"/>.
            /// </summary>
            static string GetTextFromBlob(ID3D10Blob* blob)
            {
                return blob is not null
                    ? blob->Buffer.As<byte, sbyte>().GetString()
                    : null;
            }

            /// <summary>
            ///   Releases a DirectX Blob safely.
            /// </summary>
            static void Free(ID3D10Blob* blob)
            {
                if (blob is not null)
                    blob->Release();
            }
        }

        private void UpdateReflection(ShaderBytecode shaderBytecode, EffectReflection effectReflection, LoggerResult log)
        {
            var d3dCompiler = D3DCompiler.GetApi();

            var byteCode = shaderBytecode.Data;

            ID3D11ShaderReflection* shaderReflection = Reflect(byteCode);

            ShaderDesc shaderReflectionDesc;
            shaderReflection->GetDesc(&shaderReflectionDesc);

            foreach (var constantBuffer in effectReflection.ConstantBuffers)
            {
                UpdateConstantBufferReflection(constantBuffer);
            }

            // Constant Buffers
            for (uint i = 0; i < shaderReflectionDesc.ConstantBuffers; ++i)
            {
                ID3D11ShaderReflectionConstantBuffer* constantBuffer = shaderReflection->GetConstantBufferByIndex(i);

                ShaderBufferDesc constantBufferDesc;
                constantBuffer->GetDesc(&constantBufferDesc);

                if (constantBufferDesc.Type == D3DCBufferType.D3DCTResourceBindInfo)
                    continue;

                string constantBufferName = SilkMarshal.PtrToString((nint) constantBufferDesc.Name);
                var linkBuffer = effectReflection.ConstantBuffers.First(buffer => buffer.Name == constantBufferName);

                ValidateConstantBufferReflection(constantBuffer, ref constantBufferDesc, linkBuffer, log);
            }

            // Bound Resources
            for (uint i = 0; i < shaderReflectionDesc.BoundResources; ++i)
            {
                ShaderInputBindDesc boundResourceDesc;
                shaderReflection->GetResourceBindingDesc(i, &boundResourceDesc);

                string linkKeyName = null;
                string resourceGroup = null;
                string logicalGroup = null;
                var elementType = default(EffectTypeDescription);

                var resourceName = SilkMarshal.PtrToString((nint) boundResourceDesc.Name);

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
                    var binding = GetResourceBinding(boundResourceDesc, linkKeyName);
                    binding.Stage = shaderBytecode.Stage;
                    binding.ResourceGroup = resourceGroup;
                    binding.LogicalGroup = logicalGroup;
                    binding.ElementType = elementType;

                    effectReflection.ResourceBindings.Add(binding);
                }
            }

            if (shaderReflection is not null)
                shaderReflection->Release();

            /// <summary>
            ///   Gets reflection information about a shader from its shader bytecode.
            /// </summary>
            ID3D11ShaderReflection* Reflect(byte[] byteCode)
            {
                ID3D11ShaderReflection* shaderReflection = null;

                HResult result = d3dCompiler.Reflect(in byteCode[0], (nuint) byteCode.Length,
                                                     SilkMarshal.GuidPtrOf<ID3D11ShaderReflection>(), (void**) &shaderReflection);

                if (result.IsFailure)
                    result.Throw();

                return shaderReflection;
            }
        }

        private EffectResourceBindingDescription GetResourceBinding(ShaderInputBindDesc bindingDescriptionRaw, string name)
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
                KeyInfo =
                {
                    KeyName = name
                },
                RawName = SilkMarshal.PtrToString((nint) bindingDescriptionRaw.Name),
                Class = paramClass,
                Type = paramType,
                SlotStart = (int) bindingDescriptionRaw.BindPoint,
                SlotCount = (int) bindingDescriptionRaw.BindCount
            };

            return binding;
        }

        private void UpdateConstantBufferReflection(EffectConstantBufferDescription reflectionConstantBuffer)
        {
            // Used to compute constant buffer size and member offsets (std140 rule)
            int constantBufferOffset = 0;

            // Fill members
            for (int index = 0; index < reflectionConstantBuffer.Members.Length; index++)
            {
                var member = reflectionConstantBuffer.Members[index];

                // Properly compute size and offset according to DX rules
                var memberSize = ComputeMemberSize(ref member.Type, ref constantBufferOffset);

                // Store size/offset info
                member.Offset = constantBufferOffset;
                member.Size = memberSize;

                // Adjust offset for next item
                constantBufferOffset += memberSize;

                reflectionConstantBuffer.Members[index] = member;
            }

            // Round buffer size to next multiple of 16
            reflectionConstantBuffer.Size = (constantBufferOffset + 15) / 16 * 16;
        }

        private void ValidateConstantBufferReflection(ID3D11ShaderReflectionConstantBuffer* constantBufferRaw, ref ShaderBufferDesc constantBufferRawDesc, EffectConstantBufferDescription constantBuffer, LoggerResult log)
        {
            switch (constantBufferRawDesc.Type)
            {
                case D3DCBufferType.D3DCTCbuffer:
                    if (constantBuffer.Type != ConstantBufferType.ConstantBuffer)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.ConstantBuffer}");
                    break;

                case D3DCBufferType.D3DCTTbuffer:
                    if (constantBuffer.Type != ConstantBufferType.TextureBuffer)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.TextureBuffer}");
                    break;

                default:
                    if (constantBuffer.Type != ConstantBufferType.Unknown)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.Unknown}");
                    break;
            }

            // ConstantBuffers variables
            for (uint i = 0; i < constantBufferRawDesc.Variables; i++)
            {
                var variable = constantBufferRaw->GetVariableByIndex(i);

                ShaderVariableDesc variableDescription;
                variable->GetDesc(&variableDescription);

                var variableType = variable->GetType();

                ShaderTypeDesc variableTypeDescription;
                variableType->GetDesc(&variableTypeDescription);

                var variableName = SilkMarshal.PtrToString((nint) variableDescription.Name);
                if (variableTypeDescription.Offset != 0)
                {
                    log.Error($"Unexpected offset [{variableTypeDescription.Offset}] for variable [{variableName}] in constant buffer [{constantBuffer.Name}]");
                }

                var binding = constantBuffer.Members[i];

                // Retrieve Link Member
                if (binding.RawName != variableName)
                {
                    log.Error($"Variable [{variableName}] in constant buffer [{constantBuffer.Name}] has no link");
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
                        ((parameter.Type.Class != EffectParameterClass.Struct) && // Ignore columns/rows if it's a struct (sometimes it contains weird data)
                         (parameter.Type.RowCount != binding.Type.RowCount || parameter.Type.ColumnCount != binding.Type.ColumnCount)))
                    {
                        log.Error($"Variable [{variableName}] in constant buffer [{constantBuffer.Name}] binding doesn't match what was expected");
                    }
                }
            }
            if (constantBuffer.Size != constantBufferRawDesc.Size)
            {
                log.Error($"Error precomputing buffer size for {constantBuffer.Name}: {constantBuffer.Size} instead of {constantBufferRawDesc.Size}");
            }
        }

        private static int ComputeMemberSize(ref EffectTypeDescription memberType, ref int constantBufferOffset)
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
                    throw new NotImplementedException();
            }

            // Update element size
            memberType.ElementSize = size;

            // Array
            if (memberType.Elements > 0)
            {
                var roundedSize = (size + 15) / 16 * 16; // Round up to vec4
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

        private static int ComputeTypeSize(EffectParameterType type)
        {
            return type switch
            {
                EffectParameterType.Bool or
                EffectParameterType.Float or
                EffectParameterType.Int or
                EffectParameterType.UInt => 4,

                EffectParameterType.Double => 8,

                EffectParameterType.Void => 0,

                _ => throw new NotImplementedException()
            };
        }

        private static string ShaderStageToString(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Compute => "cs",
                ShaderStage.Vertex => "vs",
                ShaderStage.Hull => "hs",
                ShaderStage.Domain => "ds",
                ShaderStage.Geometry => "gs",
                ShaderStage.Pixel => "ps",

                _ => throw new ArgumentException("Stage not supported", nameof(stage))
            };
        }

        private static string ShaderProfileFromGraphicsProfile(GraphicsProfile graphicsProfile)
        {
            return graphicsProfile switch
            {
                GraphicsProfile.Level_9_1 => "4_0_level_9_1",
                GraphicsProfile.Level_9_2 => "4_0_level_9_2",
                GraphicsProfile.Level_9_3 => "4_0_level_9_3",
                GraphicsProfile.Level_10_0 => "4_0",
                GraphicsProfile.Level_10_1 => "4_1",
                GraphicsProfile.Level_11_0 or GraphicsProfile.Level_11_1 => "5_0",

                _ => throw new ArgumentException(nameof(graphicsProfile))
            };
        }

        private static EffectParameterType ConvertVariableValueType(D3DShaderVariableType type, LoggerResult log)
        {
            if (MapType(type) is not EffectParameterType effectParameterType)
            {
                log.Error($"Type [{type}] from D3DCompiler not supported");
                return default;
            }
            else return effectParameterType;

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
}

#endif
