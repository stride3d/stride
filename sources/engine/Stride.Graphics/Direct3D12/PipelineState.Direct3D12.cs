// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Linq;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;
using Stride.Shaders;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public unsafe partial class PipelineState
    {
        internal ID3D12PipelineState* CompiledState;
        internal ID3D12RootSignature* RootSignature;
        internal D3DPrimitiveTopology PrimitiveTopology;
        internal bool HasScissorEnabled;
        internal bool IsCompute;
        internal int[] SrvBindCounts;
        internal int[] SamplerBindCounts;

        internal PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription) : base(graphicsDevice)
        {
            if (pipelineStateDescription.RootSignature is null)
                return;

            var tempMemoryAllocations = new List<nint>();

            var effectReflection = pipelineStateDescription.EffectBytecode.Reflection;

            var computeShader = pipelineStateDescription.EffectBytecode.Stages.FirstOrDefault(e => e.Stage == ShaderStage.Compute);
            IsCompute = computeShader != null;

            var rootSignatureParameters = new List<RootParameter>();
            var immutableSamplers = new List<StaticSamplerDesc>();
            SrvBindCounts = new int[pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts.Count];
            SamplerBindCounts = new int[pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts.Count];

            for (int layoutIndex = 0; layoutIndex < pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts.Count; layoutIndex++)
            {
                var layout = pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts[layoutIndex];
                if (layout.Layout == null)
                    continue;

                // TODO D3D12 for now, we don't control register so we simply generate one resource table per shader stage and per descriptor set layout
                //            we should switch to a model where we make sure VS/PS don't overlap for common descriptors so that they can be shared
                var srvDescriptorRanges = new Dictionary<ShaderStage, List<DescriptorRange>>();
                var samplerDescriptorRanges = new Dictionary<ShaderStage, List<DescriptorRange>>();

                int descriptorSrvOffset = 0;
                int descriptorSamplerOffset = 0;

                foreach (var layoutBuilderEntry in layout.Layout.Entries)
                {
                    var isSampler = layoutBuilderEntry.Class == EffectParameterClass.Sampler;

                    FindMatchingResourceBindings(layoutBuilderEntry, isSampler,
                                                 srvDescriptorRanges, ref descriptorSrvOffset,
                                                 samplerDescriptorRanges, ref descriptorSamplerOffset);

                    // Move to next element (mirror what is done in DescriptorSetLayout)
                    if (isSampler)
                    {
                        if (layoutBuilderEntry.ImmutableSampler == null)
                            descriptorSamplerOffset += layoutBuilderEntry.ArraySize;
                    }
                    else
                    {
                        descriptorSrvOffset += layoutBuilderEntry.ArraySize;
                    }
                }

                PrepareDescriptorRanges(srvDescriptorRanges, SrvBindCounts, layoutIndex);
                PrepareDescriptorRanges(samplerDescriptorRanges, SamplerBindCounts, layoutIndex);
            }

            PrepareRootSignatureDescription(rootSignatureParameters, immutableSamplers, out RootSignatureDesc rootSignatureDesc);

            var d3d12 = D3D12.GetApi();

            ID3D10Blob* rootSignatureBytes, errorMessagesBlob;
            HResult result = d3d12.SerializeRootSignature(rootSignatureDesc, D3DRootSignatureVersion.Version1,
                                                          &rootSignatureBytes, &errorMessagesBlob);
            if (result.IsFailure)
                result.Throw();

            ID3D12RootSignature* rootSignature;
            result = NativeDevice->CreateRootSignature(nodeMask: 0, rootSignatureBytes->GetBufferPointer(), rootSignatureBytes->GetBufferSize(),
                                                       SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**) &rootSignature);
            if (result.IsFailure)
                result.Throw();

            // Check if it should use compute pipeline state
            if (IsCompute)
            {
                var nativePipelineStateDescription = new ComputePipelineStateDesc
                {
                    CS = GetShaderBytecode(computeShader.Data),
                    Flags =  PipelineStateFlags.None,
                    PRootSignature = rootSignature
                };

                ID3D12PipelineState* pipelineState;
                result = NativeDevice->CreateComputePipelineState(nativePipelineStateDescription, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(),
                                                                  (void**) &pipelineState);
                if (result.IsFailure)
                    result.Throw();

                CompiledState = pipelineState;
            }
            else
            {
                var nativePipelineStateDescription = new GraphicsPipelineStateDesc
                {
                    InputLayout = PrepareInputLayout(pipelineStateDescription.InputElements),
                    PRootSignature = rootSignature,
                    RasterizerState = CreateRasterizerState(pipelineStateDescription.RasterizerState),
                    BlendState = CreateBlendState(pipelineStateDescription.BlendState),
                    SampleMask = pipelineStateDescription.SampleMask,
                    DSVFormat = (Format) pipelineStateDescription.Output.DepthStencilFormat,
                    DepthStencilState = CreateDepthStencilState(pipelineStateDescription.DepthStencilState),
                    NumRenderTargets = (uint) pipelineStateDescription.Output.RenderTargetCount,
                    // TODO D3D12 hardcoded
                    StreamOutput = new StreamOutputDesc(),
                    PrimitiveTopologyType = GetPrimitiveTopologyType(pipelineStateDescription.PrimitiveType),
                    // TODO D3D12 hardcoded
                    SampleDesc = new SampleDesc(1, 0)
                };

                // Disable depth buffer if no format specified
                if (nativePipelineStateDescription.DSVFormat == Format.FormatUnknown)
                    nativePipelineStateDescription.DepthStencilState.DepthEnable = false;

                var rtvFormats = nativePipelineStateDescription.RTVFormats.AsSpan();
                var renderTargetFormats = MemoryMarshal.CreateReadOnlySpan(ref pipelineStateDescription.Output.RenderTargetFormat0,
                                                                           pipelineStateDescription.Output.RenderTargetCount);

                for (int i = 0; i < renderTargetFormats.Length; ++i)
                    rtvFormats[i] = (Format) renderTargetFormats[i];

                foreach (var stage in pipelineStateDescription.EffectBytecode.Stages)
                {
                    var shaderBytecode = GetShaderBytecode(stage.Data);

                    switch (stage.Stage)
                    {
                        case ShaderStage.Vertex:   nativePipelineStateDescription.VS = shaderBytecode; break;
                        case ShaderStage.Hull:     nativePipelineStateDescription.HS = shaderBytecode; break;
                        case ShaderStage.Domain:   nativePipelineStateDescription.DS = shaderBytecode; break;
                        case ShaderStage.Geometry: nativePipelineStateDescription.GS = shaderBytecode; break;
                        case ShaderStage.Pixel:    nativePipelineStateDescription.PS = shaderBytecode; break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                ID3D12PipelineState* pipelineState;
                result = NativeDevice->CreateGraphicsPipelineState(nativePipelineStateDescription, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(),
                                                                   (void**) &pipelineState);
                if (result.IsFailure)
                    result.Throw();

                CompiledState = pipelineState;
            }

            RootSignature = rootSignature;
            PrimitiveTopology = (D3DPrimitiveTopology) pipelineStateDescription.PrimitiveType;
            HasScissorEnabled = pipelineStateDescription.RasterizerState.ScissorTestEnable;

            FreeAllTempMemoryAllocations();

            /// <summary>
            ///   Analyzes the shader reflection data to find matching resource bindings.
            /// </summary>
            void FindMatchingResourceBindings(DescriptorSetLayoutBuilder.Entry layoutBuilderEntry, bool isSampler,
                                              Dictionary<ShaderStage, List<DescriptorRange>> srvDescriptorRanges,
                                              ref int descriptorSrvOffset,
                                              Dictionary<ShaderStage, List<DescriptorRange>> samplerDescriptorRanges,
                                              ref int descriptorSamplerOffset)
            {
                foreach (var binding in effectReflection.ResourceBindings)
                {
                    if (binding.Stage == ShaderStage.None || binding.KeyInfo.Key != layoutBuilderEntry.Key)
                        continue;

                    var descriptorRangesDictionary = isSampler ? samplerDescriptorRanges : srvDescriptorRanges;
                    if (descriptorRangesDictionary.TryGetValue(binding.Stage, out var descriptorRanges) == false)
                    {
                        descriptorRanges = descriptorRangesDictionary[binding.Stage] = new List<DescriptorRange>();
                    }

                    if (isSampler)
                    {
                        if (layoutBuilderEntry.ImmutableSampler != null)
                        {
                            StaticSamplerDesc samplerDesc = new()
                            {
                                // TODO D3D12 ImmutableSampler should only be a state description instead of a GPU object?
                                ShaderVisibility = GetShaderVisibilityForStage(binding.Stage),
                                ShaderRegister = (uint) binding.SlotStart,
                                RegisterSpace = 0,
                                Filter = (Filter) layoutBuilderEntry.ImmutableSampler.Description.Filter,
                                ComparisonFunc = (ComparisonFunc) layoutBuilderEntry.ImmutableSampler.Description.CompareFunction,
                                BorderColor = ConvertToStaticBorderColor(layoutBuilderEntry.ImmutableSampler.Description.BorderColor),
                                AddressU = (Silk.NET.Direct3D12.TextureAddressMode) layoutBuilderEntry.ImmutableSampler.Description.AddressU,
                                AddressV = (Silk.NET.Direct3D12.TextureAddressMode) layoutBuilderEntry.ImmutableSampler.Description.AddressV,
                                AddressW = (Silk.NET.Direct3D12.TextureAddressMode) layoutBuilderEntry.ImmutableSampler.Description.AddressW,
                                MinLOD = layoutBuilderEntry.ImmutableSampler.Description.MinMipLevel,
                                MaxLOD = layoutBuilderEntry.ImmutableSampler.Description.MaxMipLevel,
                                MipLODBias = layoutBuilderEntry.ImmutableSampler.Description.MipMapLevelOfDetailBias,
                                MaxAnisotropy = (uint) layoutBuilderEntry.ImmutableSampler.Description.MaxAnisotropy
                            };

                            immutableSamplers.Add(samplerDesc);
                        }
                        else
                        {
                            // Add descriptor range
                            var descriptorRange = new DescriptorRange
                            {
                                RangeType = DescriptorRangeType.Sampler,
                                NumDescriptors = (uint) layoutBuilderEntry.ArraySize,
                                BaseShaderRegister = (uint) binding.SlotStart,
                                RegisterSpace = 0,
                                OffsetInDescriptorsFromTableStart = (uint) descriptorSamplerOffset
                            };
                            descriptorRanges.Add(descriptorRange);
                        }
                    }
                    else
                    {
                        var descriptorRangeType = binding.Class switch
                        {
                            EffectParameterClass.ConstantBuffer => DescriptorRangeType.Cbv,
                            EffectParameterClass.ShaderResourceView => DescriptorRangeType.Srv,
                            EffectParameterClass.UnorderedAccessView => DescriptorRangeType.Uav,

                            _ => throw new NotImplementedException()
                        };

                        // Add descriptor range
                        var descriptorRange = new DescriptorRange
                        {
                            RangeType = descriptorRangeType,
                            NumDescriptors = (uint) layoutBuilderEntry.ArraySize,
                            BaseShaderRegister = (uint) binding.SlotStart,
                            RegisterSpace = 0,
                            OffsetInDescriptorsFromTableStart = (uint) descriptorSrvOffset
                        };
                        descriptorRanges.Add(descriptorRange);
                    }
                }
            }

            /// <summary>
            ///   Prepares a set of root parameters of the root signature.
            /// </summary>
            void PrepareDescriptorRanges(Dictionary<ShaderStage, List<DescriptorRange>> descriptionRangesToPrepare,
                                         int[] bindCounts, int layoutIndex)
            {
                foreach (var (stage, descriptorRanges) in descriptionRangesToPrepare)
                {
                    if (descriptorRanges.Count > 0)
                    {
                        var descriptorTableSize = descriptorRanges.Count * Unsafe.SizeOf<DescriptorRange>();
                        var descriptorTableRanges = AllocateTempMemory(descriptorTableSize);

                        CopyDescriptorRanges(descriptorRanges, descriptorTableRanges, descriptorTableSize);

                        var rootParam = new RootParameter
                        {
                            ShaderVisibility = GetShaderVisibilityForStage(stage),
                            ParameterType = RootParameterType.TypeDescriptorTable,
                            DescriptorTable = new()
                            {
                                NumDescriptorRanges = (uint) descriptorRanges.Count,
                                PDescriptorRanges = (DescriptorRange*) descriptorTableRanges
                            }
                        };

                        rootSignatureParameters.Add(rootParam);
                        bindCounts[layoutIndex]++;
                    }
                }
            }

            /// <summary>
            ///   Copies the <see cref="DescriptorRange"/> entries to the specified buffer to be passed to
            ///   the pipeline state description structure.
            /// </summary>
            static void CopyDescriptorRanges(List<DescriptorRange> descriptorRanges,
                                             nint destDescriptorRangesBuffer, int destDescriptorRangesBufferSize)
            {
                var descriptorRangesItems = CollectionsMarshal.AsSpan(descriptorRanges);
                var descriptorRangesData = MemoryMarshal.Cast<DescriptorRange, byte>(descriptorRangesItems);

                var destSpan = new Span<byte>((void*) destDescriptorRangesBuffer, destDescriptorRangesBufferSize);

                descriptorRangesData.CopyTo(destSpan);
            }

            /// <summary>
            ///   Prepares the description structure needed to create the root signature.
            /// </summary>
            void PrepareRootSignatureDescription(List<RootParameter> rootParameters,
                                                 List<StaticSamplerDesc> immutableSamplers,
                                                 out RootSignatureDesc desc)
            {
                var rootParamsBufferSize = rootSignatureParameters.Count * Unsafe.SizeOf<RootParameter>();
                var rootParamsBuffer = AllocateTempMemory(rootParamsBufferSize);

                CopyRootParameters(rootSignatureParameters, rootParamsBuffer, rootParamsBufferSize);

                var staticSamplersBufferSize = rootSignatureParameters.Count * Unsafe.SizeOf<StaticSamplerDesc>();
                var staticSamplersBuffer = AllocateTempMemory(staticSamplersBufferSize);

                CopyStaticSamplers(immutableSamplers, staticSamplersBuffer, staticSamplersBufferSize);

                desc = new RootSignatureDesc
                {
                    Flags = RootSignatureFlags.AllowInputAssemblerInputLayout,
                    NumParameters = (uint) rootSignatureParameters.Count,
                    PParameters = (RootParameter*) rootParamsBuffer,
                    NumStaticSamplers = (uint) immutableSamplers.Count,
                    PStaticSamplers = (StaticSamplerDesc*) staticSamplersBuffer
                };
            }

            /// <summary>
            ///   Copies the <see cref="DescriptorRange"/> entries to the specified buffer to be passed to
            ///   the pipeline state description structure.
            /// </summary>
            static void CopyRootParameters(List<RootParameter> rootParameters,
                                           nint rootParametersBuffer, int rootParametersBufferSize)
            {
                var rootParametersItems = CollectionsMarshal.AsSpan(rootParameters);
                var rootParametersData = MemoryMarshal.Cast<RootParameter, byte>(rootParametersItems);

                var destSpan = new Span<byte>((void*) rootParametersBuffer, rootParametersBufferSize);

                rootParametersData.CopyTo(destSpan);
            }

            /// <summary>
            ///   Copies the <see cref="DescriptorRange"/> entries to the specified buffer to be passed to
            ///   the pipeline state description structure.
            /// </summary>
            static void CopyStaticSamplers(List<StaticSamplerDesc> staticSamplers,
                                           nint destStaticSamplersBuffer, int destStaticSamplersBufferSize)
            {
                var staticSamplersItems = CollectionsMarshal.AsSpan(staticSamplers);
                var staticSamplersData = MemoryMarshal.Cast<StaticSamplerDesc, byte>(staticSamplersItems);

                var destSpan = new Span<byte>((void*) destStaticSamplersBuffer, destStaticSamplersBufferSize);

                staticSamplersData.CopyTo(destSpan);
            }

            /// <summary>
            ///   Returns a <see cref="Silk.NET.Direct3D12.ShaderBytecode"/> structure for the corresponding
            ///   shader bytecode bytes.
            /// </summary>
            Silk.NET.Direct3D12.ShaderBytecode GetShaderBytecode(byte[] bytes)
            {
                var bytecodeBuffer = AllocateTempMemory(bytes.Length);

                var destSpan = new Span<byte>((void*) bytecodeBuffer, bytes.Length);
                bytes.AsSpan().CopyTo(destSpan);

                return new Silk.NET.Direct3D12.ShaderBytecode
                {
                    BytecodeLength = (nuint) bytes.Length,
                    PShaderBytecode = (void*) bytecodeBuffer
                };
            }

            /// <summary>
            ///   Gets the corresponding Direct3D 12 primitive topology type from a Stride PrimitiveType.
            /// </summary>
            static PrimitiveTopologyType GetPrimitiveTopologyType(PrimitiveType primitiveType)
            {
                return primitiveType switch
                {
                    PrimitiveType.Undefined => PrimitiveTopologyType.Undefined,

                    PrimitiveType.PointList => PrimitiveTopologyType.Point,

                    PrimitiveType.LineList or
                    PrimitiveType.LineStrip or
                    PrimitiveType.LineListWithAdjacency or
                    PrimitiveType.LineStripWithAdjacency => PrimitiveTopologyType.Line,

                    PrimitiveType.TriangleList or
                    PrimitiveType.TriangleStrip or
                    PrimitiveType.TriangleListWithAdjacency or
                    PrimitiveType.TriangleStripWithAdjacency => PrimitiveTopologyType.Triangle,

                    >= PrimitiveType.PatchList and < PrimitiveType.PatchList + 32 => PrimitiveTopologyType.Patch,

                    _ => throw new ArgumentOutOfRangeException("pipelineStateDescription.PrimitiveType")
                };
            }

            /// <summary>
            ///   Prepares the input layout description from the input elements.
            /// </summary>
            InputLayoutDesc PrepareInputLayout(InputElementDescription[] inputElements)
            {
                if (inputElements is null || inputElements.Length == 0)
                {
                    return default;
                }

                var inputElementsBufferSize = inputElements.Length * Unsafe.SizeOf<InputElementDesc>();
                var inputElementsBuffer = AllocateTempMemory(inputElementsBufferSize);

                var dstInputElements = (InputElementDesc*) inputElementsBuffer;

                for (int i = 0; i < inputElements.Length; ++i)
                {
                    ref var srcInputElement = ref pipelineStateDescription.InputElements[i];

                    var pSemanticName = SilkMarshal.StringToPtr(srcInputElement.SemanticName);
                    tempMemoryAllocations.Add(pSemanticName);

                    dstInputElements[i] = new InputElementDesc
                    {
                        Format = (Format) srcInputElement.Format,
                        AlignedByteOffset = (uint) srcInputElement.AlignedByteOffset,
                        SemanticName = (byte*) pSemanticName,
                        SemanticIndex = (uint) srcInputElement.SemanticIndex,
                        InputSlot = (uint) srcInputElement.InputSlot,
                        InputSlotClass = (Silk.NET.Direct3D12.InputClassification) srcInputElement.InputSlotClass,
                        InstanceDataStepRate = (uint) srcInputElement.InstanceDataStepRate
                    };
                }

                return new InputLayoutDesc
                {
                    NumElements = (uint) inputElements.Length,
                    PInputElementDescs = dstInputElements
                };
            }

            /// <summary>
            ///   Allocates a temporary block of memory of the specified size that must be freed by the
            ///   end of the <see cref="PipelineState"/> constructor calling <see cref="FreeAllTempMemoryAllocations()"/>.
            /// </summary>
            nint AllocateTempMemory(int byteCount)
            {
                var allocatedBuffer = SilkMarshal.Allocate(byteCount);
                tempMemoryAllocations.Add(allocatedBuffer);

                return allocatedBuffer;
            }

            /// <summary>
            ///   Frees all the temporary memory blocks allocated during the creation and configuration of the
            ///   pipeline state object.
            /// </summary>
            void FreeAllTempMemoryAllocations()
            {
                foreach (var bufferPtr in tempMemoryAllocations)
                    SilkMarshal.Free(bufferPtr);

                tempMemoryAllocations.Clear();
            }

            //
            // Converts a Color4 to its corresponding StaticBorderColor for a Sampler's border color.
            //
            static StaticBorderColor ConvertToStaticBorderColor(Color4 color)
            {
                if (color == Color4.Black)
                {
                    return StaticBorderColor.OpaqueBlack;
                }
                else if (color == Color4.White)
                {
                    return StaticBorderColor.OpaqueWhite;
                }
                else if (color == Color4.TransparentBlack)
                {
                    return StaticBorderColor.TransparentBlack;
                }

                throw new NotSupportedException("Static Samplers can only have opaque black, opaque white or transparent black as border color.");
            }
        }

        protected internal override void OnDestroyed()
        {
            if (RootSignature != null)
                RootSignature->Release();

            if (CompiledState != null)
                CompiledState->Release();

            RootSignature = null;
            CompiledState = null;

            base.OnDestroyed();
        }

        private unsafe BlendDesc CreateBlendState(BlendStateDescription description)
        {
            var nativeDescription = new BlendDesc
            {
                AlphaToCoverageEnable = description.AlphaToCoverageEnable,
                IndependentBlendEnable = description.IndependentBlendEnable
            };

            var renderTargets = &description.RenderTarget0;
            for (int i = 0; i < 8; ++i)
            {
                ref var renderTarget = ref renderTargets[i];
                ref var nativeRenderTarget = ref nativeDescription.RenderTarget[i];

                nativeRenderTarget.BlendEnable = renderTarget.BlendEnable;
                nativeRenderTarget.SrcBlend = (Silk.NET.Direct3D12.Blend) renderTarget.ColorSourceBlend;
                nativeRenderTarget.DestBlend = (Silk.NET.Direct3D12.Blend) renderTarget.ColorDestinationBlend;
                nativeRenderTarget.BlendOp = (BlendOp) renderTarget.ColorBlendFunction;
                nativeRenderTarget.SrcBlendAlpha = (Silk.NET.Direct3D12.Blend) renderTarget.AlphaSourceBlend;
                nativeRenderTarget.DestBlendAlpha = (Silk.NET.Direct3D12.Blend) renderTarget.AlphaDestinationBlend;
                nativeRenderTarget.BlendOpAlpha = (BlendOp) renderTarget.AlphaBlendFunction;
                nativeRenderTarget.RenderTargetWriteMask = (byte) renderTarget.ColorWriteChannels;
            }

            return nativeDescription;
        }

        private RasterizerDesc CreateRasterizerState(RasterizerStateDescription description)
        {
            RasterizerDesc nativeDescription = new()
            {
                CullMode = (Silk.NET.Direct3D12.CullMode) description.CullMode,
                FillMode = (Silk.NET.Direct3D12.FillMode) description.FillMode,
                FrontCounterClockwise = description.FrontFaceCounterClockwise,
                DepthBias = description.DepthBias,
                SlopeScaledDepthBias = description.SlopeScaleDepthBias,
                DepthBiasClamp = description.DepthBiasClamp,
                DepthClipEnable = description.DepthClipEnable,
                //IsScissorEnabled = description.ScissorTestEnable,
                MultisampleEnable = description.MultisampleCount >= MultisampleCount.None,
                AntialiasedLineEnable = description.MultisampleAntiAliasLine,

                ConservativeRaster = ConservativeRasterizationMode.Off,
                ForcedSampleCount = 0
            };

            return nativeDescription;
        }

        private DepthStencilDesc CreateDepthStencilState(DepthStencilStateDescription description)
        {
            DepthStencilDesc nativeDescription = new()
            {
                DepthEnable = description.DepthBufferEnable,
                DepthFunc = (ComparisonFunc) description.DepthBufferFunction,
                DepthWriteMask = description.DepthBufferWriteEnable ? DepthWriteMask.All : DepthWriteMask.Zero,

                StencilEnable = description.StencilEnable,
                StencilReadMask = description.StencilMask,
                StencilWriteMask = description.StencilWriteMask,

                FrontFace =
                {
                    StencilFailOp = (StencilOp) description.FrontFace.StencilFail,
                    StencilPassOp = (StencilOp) description.FrontFace.StencilPass,
                    StencilDepthFailOp = (StencilOp) description.FrontFace.StencilDepthBufferFail,
                    StencilFunc = (ComparisonFunc) description.FrontFace.StencilFunction
                },
                BackFace =
                {
                    StencilFailOp = (StencilOp) description.BackFace.StencilFail,
                    StencilPassOp = (StencilOp) description.BackFace.StencilPass,
                    StencilDepthFailOp = (StencilOp) description.BackFace.StencilDepthBufferFail,
                    StencilFunc = (ComparisonFunc) description.BackFace.StencilFunction
                }
            };

            return nativeDescription;
        }

        private static ShaderVisibility GetShaderVisibilityForStage(ShaderStage stage)
        {
            return stage switch
            {
                ShaderStage.Vertex => ShaderVisibility.Vertex,
                ShaderStage.Hull => ShaderVisibility.Hull,
                ShaderStage.Domain => ShaderVisibility.Domain,
                ShaderStage.Geometry => ShaderVisibility.Geometry,
                ShaderStage.Pixel => ShaderVisibility.Pixel,
                ShaderStage.Compute => ShaderVisibility.All,

                _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
            };
        }
    }
}

#endif
