// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using Stride.Core.UnsafeExtensions;
using Stride.Core.Mathematics;
using Stride.Shaders;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    // Type aliases to aid readability in this file
    using DescriptorRangesByShaderStageMap = Dictionary<ShaderStage, List<DescriptorRange>>;


    public unsafe partial class PipelineState
    {
        private ID3D12PipelineState* compiledPipelineState;
        private ID3D12RootSignature* nativeRootSignature;

        /// <summary>
        ///   Gets the internal Direct3D 12 Pipeline State that was compiled from the provided
        ///   <see cref="PipelineStateDescription"/>.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12PipelineState> CompiledState => ToComPtr(compiledPipelineState);

        /// <summary>
        ///   Gets the internal Direct3D 12 Root Signature.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12RootSignature> RootSignature => ToComPtr(nativeRootSignature);

        /// <summary>
        ///   Gets the Direct3D 12 <see cref="D3DPrimitiveTopology"/> indicating how vertices in the rendered geometry
        ///   are to be interpreted to form primitives.
        /// </summary>
        internal D3DPrimitiveTopology PrimitiveTopology { get; }

        /// <summary>
        ///   Gets a value indicating whether to enables scissor testing.
        ///   Pixels outside the active scissor rectangles are culled.
        /// </summary>
        /// <remarks>
        ///   When enabled, only pixels inside the active scissor rectangles configured in the <see cref="CommandList"/> are rendered.
        ///   This is commonly used for UI rendering, partial redraws, or performance optimization.
        /// </remarks>
        internal bool HasScissorEnabled { get; }

        /// <summary>
        ///   Gets a value indicating whether the Pipeline State represents the state of the compute pipeline.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Pipeline State represents the state of the compute pipeline;
        ///   <see langword="false"/> if it represents the state of the graphics pipeline.
        /// </value>
        internal bool IsCompute { get; }

        // Counts of Root Parameters to bind for each Descriptor Set layout
        private readonly int[] srvBindCountPerLayout;
        private readonly int[] samplerBindCountPerLayout;

        /// <summary>
        ///   A map of the number of Root Parameters to bind for each Descriptor Set layout
        ///   (corresponding to Graphics Resource groups (<c>rgroup</c>s) in Effects / Shaders) for
        ///   Descriptors of Shader Resource Views (SRVs).
        /// </summary>
        internal ReadOnlySpan<int> SrvBindCountPerLayout => srvBindCountPerLayout;
        /// <summary>
        ///   A map of the number of Root Parameters to bind for each Descriptor Set layout
        ///   (corresponding to Graphics Resource groups (<c>rgroup</c>s) in Effects / Shaders) for
        ///   Descriptors of Samplers.
        /// </summary>
        internal ReadOnlySpan<int> SamplerBindCountPerLayout => samplerBindCountPerLayout;


        /// <summary>
        ///   Initializes a new instance of the <see cref="PipelineState"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        /// <param name="pipelineStateDescription">A description of the desired graphics pipeline configuration.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   The reflected Effect bytecode in <paramref name="pipelineStateDescription"/> specifies a Shader stage that is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="pipelineStateDescription"/> specifies a <see cref="PrimitiveType"/> that is invalid.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   When configuring the Descriptors for the reflected Effect bytecode in <paramref name="pipelineStateDescription"/>,
        ///   an invalid <see cref="ShaderStage"/> was found.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Static Samplers can only have opaque black, opaque white or transparent black as border color.
        /// </exception>
        /// <exception cref="NotImplementedException">
        ///   The reflected Effect bytecode in <paramref name="pipelineStateDescription"/> specifies an <see cref="EffectParameterClass"/>
        ///   of an unsupported type. Only Shader Resource Views, Unordered Access Views, and Constant Buffer Views
        ///   are supported.
        /// </exception>
        internal PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription) : base(graphicsDevice)
        {
            Debug.Assert(pipelineStateDescription is not null);
            if (pipelineStateDescription.RootSignature is null)
                return;

            var tempMemoryAllocations = new List<nint>();

            var effectReflection = pipelineStateDescription.EffectBytecode.Reflection;

            var computeShader = pipelineStateDescription.EffectBytecode.Stages.FirstOrDefault(e => e.Stage == ShaderStage.Compute);
            IsCompute = computeShader is not null;

            var effectDescriptorSetLayouts = pipelineStateDescription.RootSignature.EffectDescriptorSetReflection.Layouts;

            var rootSignatureParameters = new List<RootParameter>();
            var immutableSamplers = new List<StaticSamplerDesc>();
            srvBindCountPerLayout = new int[effectDescriptorSetLayouts.Count];
            samplerBindCountPerLayout = new int[effectDescriptorSetLayouts.Count];

            // For each Descriptor Set layout in the reflected Shader / Effect bytecode, which should correspond to rgroups or to "Globals"
            for (int layoutIndex = 0; layoutIndex < effectDescriptorSetLayouts.Count; layoutIndex++)
            {
                var layout = effectDescriptorSetLayouts[layoutIndex];
                if (layout.Layout is null)
                    continue;

                // TODO: D3D12: For now, we don't control registers, so we simply generate one resource table per Shader stage and per Descriptor Set layout.
                //              We should switch to a model where we make sure VS/PS don't overlap for common Descriptors so that they can be shared
                var srvDescriptorRanges = new DescriptorRangesByShaderStageMap();
                var samplerDescriptorRanges = new DescriptorRangesByShaderStageMap();

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
                        if (layoutBuilderEntry.ImmutableSampler is null)
                            descriptorSamplerOffset += layoutBuilderEntry.ArraySize;
                    }
                    else
                    {
                        descriptorSrvOffset += layoutBuilderEntry.ArraySize;
                    }
                }

                // Prepare the Root Parameters for the Descriptor Tables containing the Descriptors for this layout,
                // and update the count of the number of Root Parameters to bind for the layout for each of the
                // Descriptor types (SRVs, UAVs, CBVs, etc., or Samplers)
                PrepareDescriptorRanges(srvDescriptorRanges, ref srvBindCountPerLayout[layoutIndex]);
                PrepareDescriptorRanges(samplerDescriptorRanges, ref samplerBindCountPerLayout[layoutIndex]);
            }

            PrepareRootSignatureDescription(rootSignatureParameters, immutableSamplers, out RootSignatureDesc rootSignatureDesc);

            var d3d12 = D3D12.GetApi();

            using ComPtr<ID3D10Blob> rootSignatureBytes = default;
            using ComPtr<ID3D10Blob> errorMessagesBlob = default;

            HResult result = d3d12.SerializeRootSignature(in rootSignatureDesc, D3DRootSignatureVersion.Version1,
                                                          ref rootSignatureBytes.GetPinnableReference(), ref errorMessagesBlob.GetPinnableReference());
            if (result.IsFailure)
                result.Throw();

            result = NativeDevice.CreateRootSignature(nodeMask: 0, rootSignatureBytes.GetBufferPointer(), rootSignatureBytes.GetBufferSize(),
                                                      out ComPtr<ID3D12RootSignature> rootSignature);
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

                result = NativeDevice.CreateComputePipelineState(in nativePipelineStateDescription, out ComPtr<ID3D12PipelineState> pipelineState);

                if (result.IsFailure)
                    result.Throw();

                compiledPipelineState = pipelineState;
            }
            else // Graphics Pipeline State
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
                    // TODO: D3D12: Hardcoded Stream-Output in PipelineState
                    StreamOutput = new StreamOutputDesc(),
                    PrimitiveTopologyType = GetPrimitiveTopologyType(pipelineStateDescription.PrimitiveType),
                    // TODO: D3D12: Hardcoded no Multi-Sampling in PipelineState
                    SampleDesc = new SampleDesc(1, 0)
                };

                // Disable Depth Buffer if no format specified
                if (nativePipelineStateDescription.DSVFormat == Format.FormatUnknown)
                    nativePipelineStateDescription.DepthStencilState.DepthEnable = false;

                var rtvFormats = nativePipelineStateDescription.RTVFormats.AsSpan();
                Debug.Assert(sizeof(PixelFormat) == sizeof(Format));
                Debug.Assert(rtvFormats.Length == pipelineStateDescription.Output.RenderTargetFormats.Length);
                pipelineStateDescription.Output.RenderTargetFormats.As<PixelFormat, Format>().CopyTo(rtvFormats);

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
                            throw new ArgumentOutOfRangeException(nameof(pipelineStateDescription), "Invalid Shader stage specified in the Effect bytecode.");
                    }
                }

                result = NativeDevice.CreateGraphicsPipelineState(in nativePipelineStateDescription, out ComPtr<ID3D12PipelineState> pipelineState);

                if (result.IsFailure)
                    result.Throw();

                compiledPipelineState = pipelineState;
            }

            nativeRootSignature = rootSignature;
            PrimitiveTopology = (D3DPrimitiveTopology) pipelineStateDescription.PrimitiveType;
            HasScissorEnabled = pipelineStateDescription.RasterizerState.ScissorTestEnable;

            FreeAllTempMemoryAllocations();

            //
            // Analyzes the shader reflection data to find matching resource bindings.
            //
            void FindMatchingResourceBindings(DescriptorSetLayoutBuilder.Entry layoutBuilderEntry, bool isSampler,
                                              DescriptorRangesByShaderStageMap srvDescriptorRanges,
                                              ref int descriptorSrvOffset,
                                              DescriptorRangesByShaderStageMap samplerDescriptorRanges,
                                              ref int descriptorSamplerOffset)
            {
                foreach (var binding in effectReflection.ResourceBindings)
                {
                    if (binding.Stage == ShaderStage.None || binding.KeyInfo.Key != layoutBuilderEntry.Key)
                        continue;

                    var descriptorRangesDictionary = isSampler ? samplerDescriptorRanges : srvDescriptorRanges;
                    if (descriptorRangesDictionary.TryGetValue(binding.Stage, out var descriptorRanges) == false)
                    {
                        descriptorRanges = descriptorRangesDictionary[binding.Stage] = [];
                    }

                    if (isSampler)
                    {
                        if (layoutBuilderEntry.ImmutableSampler is not null)
                        {
                            StaticSamplerDesc samplerDesc = new()
                            {
                                // TODO: D3D12: ImmutableSampler should only be a state description instead of a GPU object?
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
                        else // Normal Sampler State
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
                    else // Other bindings: SRVs, UAVs, or CBVs
                    {
                        var descriptorRangeType = binding.Class switch
                        {
                            EffectParameterClass.ConstantBuffer => DescriptorRangeType.Cbv,
                            EffectParameterClass.ShaderResourceView => DescriptorRangeType.Srv,
                            EffectParameterClass.UnorderedAccessView => DescriptorRangeType.Uav,

                            _ => throw new NotImplementedException("Only SRVs, UAVs, and CBVs are supported.")
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

            //
            // Prepares a set of root parameters of the Root Signature.
            //
            void PrepareDescriptorRanges(DescriptorRangesByShaderStageMap descriptionRangesToPrepare,
                                         ref int layoutBindCount)
            {
                foreach ((ShaderStage stage, List<DescriptorRange> descriptorRanges) in descriptionRangesToPrepare)
                {
                    if (descriptorRanges.Count <= 0)
                        continue;

                    // Allocate a Descriptor Table and copy the Descriptors for this ShaderStage
                    var descriptorTableSize = descriptorRanges.Count * sizeof(DescriptorRange);
                    var descriptorTableRanges = AllocateTempMemory(descriptorTableSize);

                    CopyDescriptorRanges(descriptorRanges, descriptorTableRanges, descriptorTableSize);

                    // Create a Root Parameter to reference the Descriptor Table
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

                    // Count how many Root Parameters we need to bind for the current layout and Descriptor type (Sampler, SRVs, etc.)
                    layoutBindCount++;
                }
            }

            //
            // Copies the `DescriptorRange` entries to the specified buffer to be passed to
            // the Pipeline State description structure.
            //
            static void CopyDescriptorRanges(List<DescriptorRange> descriptorRanges,
                                             nint destDescriptorRangesBuffer, int destDescriptorRangesBufferSize)
            {
                var descriptorRangesItems = CollectionsMarshal.AsSpan(descriptorRanges);
                var descriptorRangesData = descriptorRangesItems.Cast<DescriptorRange, byte>();

                var destSpan = new Span<byte>((void*) destDescriptorRangesBuffer, destDescriptorRangesBufferSize);

                descriptorRangesData.CopyTo(destSpan);
            }

            //
            // Prepares the description structure needed to create the Root Signature.
            //
            void PrepareRootSignatureDescription(List<RootParameter> rootParameters,
                                                 List<StaticSamplerDesc> immutableSamplers,
                                                 out RootSignatureDesc desc)
            {
                var rootParamsBufferSize = rootSignatureParameters.Count * sizeof(RootParameter);
                var rootParamsBuffer = AllocateTempMemory(rootParamsBufferSize);

                CopyRootParameters(rootSignatureParameters, rootParamsBuffer, rootParamsBufferSize);

                var staticSamplersBufferSize = rootSignatureParameters.Count * sizeof(StaticSamplerDesc);
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

            //
            // Copies the `RootParameter` entries to the specified buffer to be passed to
            // the Pipeline State description structure.
            //
            static void CopyRootParameters(List<RootParameter> rootParameters,
                                           nint rootParametersBuffer, int rootParametersBufferSize)
            {
                var rootParametersItems = CollectionsMarshal.AsSpan(rootParameters);
                var rootParametersData = rootParametersItems.Cast<RootParameter, byte>();

                var destSpan = new Span<byte>((void*) rootParametersBuffer, rootParametersBufferSize);

                rootParametersData.CopyTo(destSpan);
            }

            //
            // Copies the `StaticSamplerDesc` entries to the specified buffer to be passed to
            // the Pipeline State description structure.
            //
            static void CopyStaticSamplers(List<StaticSamplerDesc> staticSamplers,
                                           nint destStaticSamplersBuffer, int destStaticSamplersBufferSize)
            {
                var staticSamplersItems = CollectionsMarshal.AsSpan(staticSamplers);
                var staticSamplersData = staticSamplersItems.Cast<StaticSamplerDesc, byte>();

                var destSpan = new Span<byte>((void*) destStaticSamplersBuffer, destStaticSamplersBufferSize);

                staticSamplersData.CopyTo(destSpan);
            }

            //
            // Returns a Direct3D 12 Shader Bytecode for the corresponding Shader bytecode bytes.
            //
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

            //
            // Gets the corresponding Direct3D 12 primitive topology type from a Stride PrimitiveType.
            //
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

                    _ => throw new ArgumentOutOfRangeException(nameof(primitiveType), "Invalid PrimitiveType in PipelineStateDescription.")
                };
            }

            //
            // Prepares the input layout description from the input elements.
            //
            InputLayoutDesc PrepareInputLayout(InputElementDescription[] inputElements)
            {
                if (inputElements is null || inputElements.Length == 0)
                {
                    return default;
                }

                var inputElementsBufferSize = inputElements.Length * sizeof(InputElementDesc);
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

            //
            // Creates a Direct3D 12 Blend State description from the provided description.
            //
            BlendDesc CreateBlendState(BlendStateDescription description)
            {
                var nativeDescription = new BlendDesc
                {
                    AlphaToCoverageEnable = description.AlphaToCoverageEnable,
                    IndependentBlendEnable = description.IndependentBlendEnable
                };

                var renderTargets = description.RenderTargets;
                for (int i = 0; i < 8; ++i)
                {
                    ref var renderTarget = ref renderTargets[i];
                    ref var nativeRenderTarget = ref nativeDescription.RenderTarget[i];

                    nativeRenderTarget.BlendEnable = renderTarget.BlendEnable;
                    nativeRenderTarget.SrcBlend = (Silk.NET.Direct3D12.Blend)renderTarget.ColorSourceBlend;
                    nativeRenderTarget.DestBlend = (Silk.NET.Direct3D12.Blend)renderTarget.ColorDestinationBlend;
                    nativeRenderTarget.BlendOp = (BlendOp)renderTarget.ColorBlendFunction;
                    nativeRenderTarget.SrcBlendAlpha = (Silk.NET.Direct3D12.Blend)renderTarget.AlphaSourceBlend;
                    nativeRenderTarget.DestBlendAlpha = (Silk.NET.Direct3D12.Blend)renderTarget.AlphaDestinationBlend;
                    nativeRenderTarget.BlendOpAlpha = (BlendOp)renderTarget.AlphaBlendFunction;
                    nativeRenderTarget.RenderTargetWriteMask = (byte)renderTarget.ColorWriteChannels;
                }

                return nativeDescription;
            }

            //
            // Creates a Direct3D 12 Rasterizer State description from the provided description.
            //
            RasterizerDesc CreateRasterizerState(RasterizerStateDescription description)
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
                    MultisampleEnable = description.MultisampleCount >= MultisampleCount.None,
                    AntialiasedLineEnable = description.MultisampleAntiAliasLine,

                    ConservativeRaster = ConservativeRasterizationMode.Off,
                    ForcedSampleCount = 0
                };

                return nativeDescription;
            }

            //
            // Creates a Direct3D 12 Depth-Stencil State from the provided description.
            //
            DepthStencilDesc CreateDepthStencilState(DepthStencilStateDescription description)
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

            //
            // Allocates a temporary block of memory of the specified size that must be freed by the
            // end of the <see cref="PipelineState"/> constructor calling <see cref="FreeAllTempMemoryAllocations()"/>.
            //
            nint AllocateTempMemory(int byteCount)
            {
                var allocatedBuffer = SilkMarshal.Allocate(byteCount);
                tempMemoryAllocations.Add(allocatedBuffer);

                return allocatedBuffer;
            }

            //
            // Frees all the temporary memory blocks allocated during the creation and configuration of the
            // pipeline state object.
            //
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

            //
            // Returns the Direct3D 12 Shader Visibility for a Stride's ShaderStage.
            //
            static ShaderVisibility GetShaderVisibilityForStage(ShaderStage stage)
            {
                return stage switch
                {
                    ShaderStage.Vertex => ShaderVisibility.Vertex,
                    ShaderStage.Hull => ShaderVisibility.Hull,
                    ShaderStage.Domain => ShaderVisibility.Domain,
                    ShaderStage.Geometry => ShaderVisibility.Geometry,
                    ShaderStage.Pixel => ShaderVisibility.Pixel,
                    ShaderStage.Compute => ShaderVisibility.All,

                    _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, "Invalid ShaderStage.")
                };
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            SafeRelease(ref nativeRootSignature);
            SafeRelease(ref compiledPipelineState);

            base.OnDestroyed(immediately);
        }
    }
}

#endif
