// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_WINDOWS_DESKTOP || XENKO_GRAPHICS_API_DIRECT3D // Need SharpDX
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDX;
using SharpDX.D3DCompiler;
using Xenko.Core.Diagnostics;
using Xenko.Core.Storage;
using Xenko.Rendering;
using Xenko.Graphics;
using ConstantBufferType = Xenko.Shaders.ConstantBufferType;
using ShaderBytecode = Xenko.Shaders.ShaderBytecode;
using ShaderVariableType = SharpDX.D3DCompiler.ShaderVariableType;

namespace Xenko.Shaders.Compiler.Direct3D
{
    internal class ShaderCompiler : IShaderCompiler
    {
        public ShaderBytecodeResult Compile(string shaderSource, string entryPoint, ShaderStage stage, EffectCompilerParameters effectParameters, EffectReflection reflection, string sourceFilename = null)
        {
            var isDebug = effectParameters.Debug;
            var optimLevel = effectParameters.OptimizationLevel;
            var profile = effectParameters.Profile;
            
            var shaderModel = ShaderStageToString(stage) + "_" + ShaderProfileFromGraphicsProfile(profile);

            var shaderFlags = ShaderFlags.None;
            if (isDebug)
            {
                shaderFlags = ShaderFlags.Debug;
            }
            switch (optimLevel)
            {
                case 0:
                    shaderFlags |= ShaderFlags.OptimizationLevel0;
                    break;
                case 1:
                    shaderFlags |= ShaderFlags.OptimizationLevel1;
                    break;
                case 2:
                    shaderFlags |= ShaderFlags.OptimizationLevel2;
                    break;
                case 3:
                    shaderFlags |= ShaderFlags.OptimizationLevel3;
                    break;
            }
            SharpDX.Configuration.ThrowOnShaderCompileError = false;

            // Compile using D3DCompiler
            var compilationResult = SharpDX.D3DCompiler.ShaderBytecode.Compile(shaderSource, entryPoint, shaderModel, shaderFlags, EffectFlags.None, null, null, sourceFilename);

            var byteCodeResult = new ShaderBytecodeResult();

            if (compilationResult.HasErrors || compilationResult.Bytecode == null)
            {
                // Log compilation errors
                byteCodeResult.Error(compilationResult.Message);
            }
            else
            {
                // TODO: Make this optional
                try
                {
                    byteCodeResult.DisassembleText = compilationResult.Bytecode.Disassemble();
                }
                catch (SharpDXException)
                {
                }

                // As effect bytecode binary can changed when having debug infos (with d3dcompiler_47), we are calculating a bytecodeId on the stripped version
                var rawData = compilationResult.Bytecode.Strip(StripFlags.CompilerStripDebugInformation | StripFlags.CompilerStripReflectionData);
                var bytecodeId = ObjectId.FromBytes(rawData);
                byteCodeResult.Bytecode = new ShaderBytecode(bytecodeId, compilationResult.Bytecode.Data) { Stage = stage };

                // If compilation succeed, then we can update reflection.
                UpdateReflection(byteCodeResult.Bytecode, reflection, byteCodeResult);

                if (!string.IsNullOrEmpty(compilationResult.Message))
                {
                    byteCodeResult.Warning(compilationResult.Message);
                }
            }

            return byteCodeResult;
        }

        private void UpdateReflection(ShaderBytecode shaderBytecode, EffectReflection effectReflection, LoggerResult log)
        {
            var shaderReflectionRaw = new SharpDX.D3DCompiler.ShaderReflection(shaderBytecode);
            var shaderReflectionRawDesc = shaderReflectionRaw.Description;

            foreach (var constantBuffer in effectReflection.ConstantBuffers)
            {
                UpdateConstantBufferReflection(constantBuffer);
            }

            // Constant Buffers
            for (int i = 0; i < shaderReflectionRawDesc.ConstantBuffers; ++i)
            {
                var constantBufferRaw = shaderReflectionRaw.GetConstantBuffer(i);
                var constantBufferRawDesc = constantBufferRaw.Description;
                if (constantBufferRawDesc.Type == SharpDX.D3DCompiler.ConstantBufferType.ResourceBindInformation)
                    continue;

                var linkBuffer = effectReflection.ConstantBuffers.First(buffer => buffer.Name == constantBufferRawDesc.Name);

                ValidateConstantBufferReflection(constantBufferRaw, ref constantBufferRawDesc, linkBuffer, log);
            }

            // BoundResources
            for (int i = 0; i < shaderReflectionRawDesc.BoundResources; ++i)
            {
                var boundResourceDesc = shaderReflectionRaw.GetResourceBindingDescription(i);

                string linkKeyName = null;
                string resourceGroup = null;
                string logicalGroup = null;
                var elementType = default(EffectTypeDescription);
                foreach (var linkResource in effectReflection.ResourceBindings)
                {
                    if (linkResource.RawName == boundResourceDesc.Name && linkResource.Stage == ShaderStage.None)
                    {
                        linkKeyName = linkResource.KeyInfo.KeyName;
                        resourceGroup = linkResource.ResourceGroup;
                        logicalGroup = linkResource.LogicalGroup;
                        elementType = linkResource.ElementType;
                        break;
                    }

                }

                if (linkKeyName == null)
                {
                    log.Error($"Resource [{boundResourceDesc.Name}] has no link");
                }
                else
                {

                    var binding = GetResourceBinding(boundResourceDesc, linkKeyName, log);
                    binding.Stage = shaderBytecode.Stage;
                    binding.ResourceGroup = resourceGroup;
                    binding.LogicalGroup = logicalGroup;
                    binding.ElementType = elementType;

                    effectReflection.ResourceBindings.Add(binding);
                }
            }
        }

        private EffectResourceBindingDescription GetResourceBinding(SharpDX.D3DCompiler.InputBindingDescription bindingDescriptionRaw, string name, LoggerResult log)
        {
            var paramClass = EffectParameterClass.Object;
            var paramType = EffectParameterType.Void;

            switch (bindingDescriptionRaw.Type)
            {
                case SharpDX.D3DCompiler.ShaderInputType.TextureBuffer:
                    paramType = EffectParameterType.TextureBuffer;
                    paramClass = EffectParameterClass.TextureBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.ConstantBuffer:
                    paramType = EffectParameterType.ConstantBuffer;
                    paramClass = EffectParameterClass.ConstantBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.Texture:
                    paramClass = EffectParameterClass.ShaderResourceView;
                    switch (bindingDescriptionRaw.Dimension)
                    {
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Buffer:
                            paramType = EffectParameterType.Buffer;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture1D:
                            paramType = EffectParameterType.Texture1D;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture1DArray:
                            paramType = EffectParameterType.Texture1DArray;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D:
                            paramType = EffectParameterType.Texture2D;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray:
                            paramType = EffectParameterType.Texture2DArray;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampled:
                            paramType = EffectParameterType.Texture2DMultisampled;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DMultisampledArray:
                            paramType = EffectParameterType.Texture2DMultisampledArray;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture3D:
                            paramType = EffectParameterType.Texture3D;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.TextureCube:
                            paramType = EffectParameterType.TextureCube;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.TextureCubeArray:
                            paramType = EffectParameterType.TextureCubeArray;
                            break;
                    }
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.Structured:
                    paramClass = EffectParameterClass.ShaderResourceView;
                    paramType = EffectParameterType.StructuredBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.ByteAddress:
                    paramClass = EffectParameterClass.ShaderResourceView;
                    paramType = EffectParameterType.ByteAddressBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewRWTyped:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    switch (bindingDescriptionRaw.Dimension)
                    {
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Buffer:
                            paramType = EffectParameterType.RWBuffer;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture1D:
                            paramType = EffectParameterType.RWTexture1D;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture1DArray:
                            paramType = EffectParameterType.RWTexture1DArray;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D:
                            paramType = EffectParameterType.RWTexture2D;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture2DArray:
                            paramType = EffectParameterType.RWTexture2DArray;
                            break;
                        case SharpDX.Direct3D.ShaderResourceViewDimension.Texture3D:
                            paramType = EffectParameterType.RWTexture3D;
                            break;
                    }
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewRWStructured:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    paramType = EffectParameterType.RWStructuredBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewRWByteAddress:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    paramType = EffectParameterType.RWByteAddressBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewAppendStructured:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    paramType = EffectParameterType.AppendStructuredBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewConsumeStructured:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    paramType = EffectParameterType.ConsumeStructuredBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.UnorderedAccessViewRWStructuredWithCounter:
                    paramClass = EffectParameterClass.UnorderedAccessView;
                    paramType = EffectParameterType.RWStructuredBuffer;
                    break;
                case SharpDX.D3DCompiler.ShaderInputType.Sampler:
                    paramClass = EffectParameterClass.Sampler;
                    paramType = EffectParameterType.Sampler;
                    break;
            }

            var binding = new EffectResourceBindingDescription()
                {
                    KeyInfo =
                        {
                            KeyName = name,
                        },
                    RawName = bindingDescriptionRaw.Name,
                    Class = paramClass,
                    Type = paramType,
                    SlotStart = bindingDescriptionRaw.BindPoint,
                    SlotCount = bindingDescriptionRaw.BindCount,
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

        private void ValidateConstantBufferReflection(ConstantBuffer constantBufferRaw, ref ConstantBufferDescription constantBufferRawDesc, EffectConstantBufferDescription constantBuffer, LoggerResult log)
        {
            switch (constantBufferRawDesc.Type)
            {
                case SharpDX.D3DCompiler.ConstantBufferType.ConstantBuffer:
                    if (constantBuffer.Type != ConstantBufferType.ConstantBuffer)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.ConstantBuffer}");
                    break;
                case SharpDX.D3DCompiler.ConstantBufferType.TextureBuffer:
                    if (constantBuffer.Type != ConstantBufferType.TextureBuffer)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.TextureBuffer}");
                    break;
                default:
                    if (constantBuffer.Type != ConstantBufferType.Unknown)
                        log.Error($"Invalid buffer type for {constantBuffer.Name}: {constantBuffer.Type} instead of {ConstantBufferType.Unknown}");
                    break;
            }

            // ConstantBuffers variables
            for (int i = 0; i < constantBufferRawDesc.VariableCount; i++)
            {
                var variable = constantBufferRaw.GetVariable(i);
                var variableType = variable.GetVariableType();
                var variableDescription = variable.Description;
                var variableTypeDescription = variableType.Description;

                if (variableTypeDescription.Offset != 0)
                {
                    log.Error($"Unexpected offset [{variableTypeDescription.Offset}] for variable [{variableDescription.Name}] in constant buffer [{constantBuffer.Name}]");
                }

                var binding = constantBuffer.Members[i];
                // Retrieve Link Member
                if (binding.RawName != variableDescription.Name)
                {
                    log.Error($"Variable [{variableDescription.Name}] in constant buffer [{constantBuffer.Name}] has no link");
                }
                else
                {
                    var parameter = new EffectValueDescription()
                    {
                        Type =
                        {
                            Class = (EffectParameterClass)variableTypeDescription.Class,
                            Type = ConvertVariableValueType(variableTypeDescription.Type, log),
                            Elements = variableTypeDescription.ElementCount,
                            RowCount = (byte)variableTypeDescription.RowCount,
                            ColumnCount = (byte)variableTypeDescription.ColumnCount,
                        },
                        RawName = variableDescription.Name,
                        Offset = variableDescription.StartOffset,
                        Size = variableDescription.Size,
                    };

                    if (parameter.Offset != binding.Offset
                        || parameter.Size != binding.Size
                        || parameter.Type.Elements != binding.Type.Elements
                        || ((parameter.Type.Class != EffectParameterClass.Struct) && // Ignore columns/rows if it's a struct (sometimes it contains weird data)
                               (parameter.Type.RowCount != binding.Type.RowCount || parameter.Type.ColumnCount != binding.Type.ColumnCount)))
                    {
                        log.Error($"Variable [{variableDescription.Name}] in constant buffer [{constantBuffer.Name}] binding doesn't match what was expected");
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
            switch (type)
            {
                case EffectParameterType.Bool:
                case EffectParameterType.Float:
                case EffectParameterType.Int:
                case EffectParameterType.UInt:
                    return 4;
                case EffectParameterType.Double:
                    return 8;
                case EffectParameterType.Void:
                    return 0;
                default:
                    throw new NotImplementedException();
            }
        }

        private static string ShaderStageToString(ShaderStage stage)
        {
            string shaderStageText;
            switch (stage)
            {
                case ShaderStage.Compute:
                    shaderStageText = "cs";
                    break;
                case ShaderStage.Vertex:
                    shaderStageText = "vs";
                    break;
                case ShaderStage.Hull:
                    shaderStageText = "hs";
                    break;
                case ShaderStage.Domain:
                    shaderStageText = "ds";
                    break;
                case ShaderStage.Geometry:
                    shaderStageText = "gs";
                    break;
                case ShaderStage.Pixel:
                    shaderStageText = "ps";
                    break;
                default:
                    throw new ArgumentException("Stage not supported", "stage");
            }
            return shaderStageText;
        }

        private static string ShaderProfileFromGraphicsProfile(GraphicsProfile graphicsProfile)
        {
            switch (graphicsProfile)
            {
                case GraphicsProfile.Level_9_1:
                    return "4_0_level_9_1";
                case GraphicsProfile.Level_9_2:
                    return "4_0_level_9_2";
                case GraphicsProfile.Level_9_3:
                    return "4_0_level_9_3";
                case GraphicsProfile.Level_10_0:
                    return "4_0";
                case GraphicsProfile.Level_10_1:
                    return "4_1";
                case GraphicsProfile.Level_11_0:
                case GraphicsProfile.Level_11_1:
                    return "5_0";
            }
            throw new ArgumentException("graphicsProfile");
        }
        private static readonly Dictionary<ShaderVariableType, EffectParameterType> MapTypes = new Dictionary<ShaderVariableType,EffectParameterType>()
            {
                {ShaderVariableType.Void                                 , EffectParameterType.Void                          },
                {ShaderVariableType.Bool                                 , EffectParameterType.Bool                          },
                {ShaderVariableType.Int                                  , EffectParameterType.Int                           },
                {ShaderVariableType.Float                                , EffectParameterType.Float                         },
                {ShaderVariableType.UInt                                 , EffectParameterType.UInt                          },
                {ShaderVariableType.UInt8                                , EffectParameterType.UInt8                         },
                {ShaderVariableType.Double                               , EffectParameterType.Double                        },
            };

        private EffectParameterType ConvertVariableValueType(ShaderVariableType type, LoggerResult log)
        {
            EffectParameterType effectParameterType;
            if (!MapTypes.TryGetValue(type, out effectParameterType))
            {
                log.Error($"Type [{type}] from D3DCompiler not supported");
            }
            return effectParameterType;
        }
    }
}
#endif
