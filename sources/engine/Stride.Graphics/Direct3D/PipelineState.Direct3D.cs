// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;

using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Core.UnsafeExtensions;
using Stride.Shaders;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class PipelineState
    {
        // Effect
        private readonly RootSignature rootSignature;
        private readonly EffectBytecode effectBytecode;
        internal ResourceBinder ResourceBinder;

        private ID3D11VertexShader* vertexShader;
        private ID3D11GeometryShader* geometryShader;
        private ID3D11PixelShader* pixelShader;
        private ID3D11HullShader* hullShader;
        private ID3D11DomainShader* domainShader;
        private ID3D11ComputeShader* computeShader;
        private byte[] inputSignature;

        private readonly ID3D11BlendState* blendState;
        private readonly uint sampleMask;
        private readonly ID3D11RasterizerState* rasterizerState;
        private readonly ID3D11DepthStencilState* depthStencilState;

        private ID3D11InputLayout* inputLayout;

        private readonly D3DPrimitiveTopology primitiveTopology;

        // NOTE: No need to store RTV/DSV formats


        /// <summary>
        ///   Initializes a new instance of the <see cref="PipelineState"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        /// <param name="pipelineStateDescription">A description of the Pipeline State to create.</param>
        internal PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription)
            : base(graphicsDevice)
        {
            // First time, build caches
            var pipelineStateCache = GetPipelineStateCache();

            // Effect
            rootSignature = pipelineStateDescription.RootSignature;
            effectBytecode = pipelineStateDescription.EffectBytecode;

            CreateShaders(pipelineStateCache);

            if (rootSignature is not null && effectBytecode is not null)
                ResourceBinder.Compile(rootSignature.EffectDescriptorSetReflection, effectBytecode);

            // TODO: Cache over Effect|RootSignature to create binding operations

            // States
            blendState = pipelineStateCache.BlendStateCache.Instantiate(pipelineStateDescription.BlendState);

            sampleMask = pipelineStateDescription.SampleMask;
            rasterizerState = pipelineStateCache.RasterizerStateCache.Instantiate(pipelineStateDescription.RasterizerState);
            depthStencilState = pipelineStateCache.DepthStencilStateCache.Instantiate(pipelineStateDescription.DepthStencilState);

            CreateInputLayout(pipelineStateDescription.InputElements);

            primitiveTopology = (D3DPrimitiveTopology) pipelineStateDescription.PrimitiveType;
        }

        /// <summary>
        ///   Applies the current Pipeline State to the specified Command List, if it has changed since the last time it was applied.
        /// </summary>
        /// <param name="commandList">The Command List.</param>
        /// <param name="previousPipeline">The previous state of the graphics pipeline.</param>
        internal void Apply(CommandList commandList, PipelineState previousPipeline)
        {
            var nativeDeviceContext = commandList.NativeDeviceContext;

            if (rootSignature != previousPipeline.rootSignature)
            {
                //rootSignature.Apply // Not a concept in Direct3D 11, it is applied through the EffectBytecode
            }

            if (effectBytecode != previousPipeline.effectBytecode)
            {
                if (computeShader != previousPipeline.computeShader)
                    nativeDeviceContext.CSSetShader(computeShader, ppClassInstances: null, NumClassInstances: 0);

                if (vertexShader != previousPipeline.vertexShader)
                    nativeDeviceContext.VSSetShader(vertexShader, ppClassInstances: null, NumClassInstances: 0);

                if (pixelShader != previousPipeline.pixelShader)
                    nativeDeviceContext.PSSetShader(pixelShader, ppClassInstances: null, NumClassInstances: 0);

                if (hullShader != previousPipeline.hullShader)
                    nativeDeviceContext.HSSetShader(hullShader, ppClassInstances: null, NumClassInstances: 0);

                if (domainShader != previousPipeline.domainShader)
                    nativeDeviceContext.DSSetShader(domainShader, ppClassInstances: null, NumClassInstances: 0);

                if (geometryShader != previousPipeline.geometryShader)
                    nativeDeviceContext.GSSetShader(geometryShader, ppClassInstances: null, NumClassInstances: 0);
            }

            if (blendState != previousPipeline.blendState || sampleMask != previousPipeline.sampleMask)
            {
                SkipInit(out Color4 blendFactor);
                var blendFactorFloats = blendFactor.AsSpan<Color4, float>(4);

                nativeDeviceContext.OMGetBlendState(ppBlendState: null, blendFactorFloats, pSampleMask: (uint*) null);
                nativeDeviceContext.OMSetBlendState(blendState, blendFactorFloats, sampleMask);
            }

            if (rasterizerState != previousPipeline.rasterizerState)
            {
                nativeDeviceContext.RSSetState(rasterizerState);
            }

            if (depthStencilState != previousPipeline.depthStencilState)
            {
                SkipInit(out uint stencilRef);

                nativeDeviceContext.OMGetDepthStencilState(ppDepthStencilState: null, ref stencilRef);
                nativeDeviceContext.OMSetDepthStencilState(depthStencilState, stencilRef);
            }

            if (inputLayout != previousPipeline.inputLayout)
            {
                nativeDeviceContext.IASetInputLayout(inputLayout);
            }

            if (primitiveTopology != previousPipeline.primitiveTopology)
            {
                nativeDeviceContext.IASetPrimitiveTopology(primitiveTopology);
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            var pipelineStateCache = GetPipelineStateCache();

            if (blendState is not null)
                pipelineStateCache.BlendStateCache.Release(blendState);
            if (rasterizerState is not null)
                pipelineStateCache.RasterizerStateCache.Release(rasterizerState);
            if (depthStencilState is not null)
                pipelineStateCache.DepthStencilStateCache.Release(depthStencilState);

            if (vertexShader is not null)
                pipelineStateCache.VertexShaderCache.Release(vertexShader);
            if (pixelShader is not null)
                pipelineStateCache.PixelShaderCache.Release(pixelShader);
            if (geometryShader is not null)
                pipelineStateCache.GeometryShaderCache.Release(geometryShader);
            if (hullShader is not null)
                pipelineStateCache.HullShaderCache.Release(hullShader);
            if (domainShader is not null)
                pipelineStateCache.DomainShaderCache.Release(domainShader);
            if (computeShader is not null)
                pipelineStateCache.ComputeShaderCache.Release(computeShader);

            if (inputLayout is not null)
                inputLayout->Release();

            base.OnDestroyed();
        }

        /// <summary>
        ///   Creates a ID3D11InputLayout for the graphics pipeline based on the provided Input Element Descriptions.
        /// </summary>
        /// <param name="inputElements">
        ///   <para>
        ///     An array of <see cref="InputElementDescription"/> objects that define the input elements for the layout.
        ///     Each element specifies the semantic name, index, format, input slot, and byte offset for a vertex attribute.
        ///   </para>
        ///   <para>
        ///     If this parameter is <see langword="null"/>, the method exits without performing any operations.
        ///   </para>
        /// </param>
        private void CreateInputLayout(InputElementDescription[] inputElements)
        {
            if (inputElements is null)
                return;

            var nativeInputElements = stackalloc InputElementDesc[inputElements.Length];

            for (int index = 0; index < inputElements.Length; index++)
            {
                ref var inputElement = ref inputElements[index];

                var nameLength = Encoding.ASCII.GetByteCount(inputElement.SemanticName);
                var semanticName = stackalloc byte[nameLength];
                Encoding.ASCII.GetBytes(inputElement.SemanticName, new Span<byte>(semanticName, nameLength));

                nativeInputElements[index] = new()
                {
                    InputSlot = (uint) inputElement.InputSlot,
                    SemanticName = semanticName,
                    SemanticIndex = (uint) inputElement.SemanticIndex,
                    AlignedByteOffset = (uint) inputElement.AlignedByteOffset,
                    Format = (Format) inputElement.Format
                };
            }

            ComPtr<ID3D11InputLayout> tempInputLayout = default;

            HResult result = NativeDevice.CreateInputLayout(nativeInputElements, NumElements: (uint) inputElements.Length,
                                                            in inputSignature[0], (uint) inputSignature.Length, ref tempInputLayout);
            if (result.IsFailure)
                result.Throw();

            inputLayout = tempInputLayout;
        }

        /// <summary>
        ///   Creates and initializes the Shaders for the current Effect set in the graphics pipeline, and
        ///   caches them in the specified Pipeline State cache.
        /// </summary>
        /// <param name="pipelineStateCache">
        ///   The cache used to store and retrieve Pipeline State objects, including Shader instances.
        ///   If the Effect bytecode is <see langword="null"/>, the method exits without performing any operations.
        /// </param>
        private void CreateShaders(PipelineStateCache pipelineStateCache)
        {
            if (effectBytecode is null)
                return;

            foreach (var shaderBytecode in effectBytecode.Stages)
            {
                var reflection = effectBytecode.Reflection;

                // TODO: CACHE Shaders with a bytecode hash
                // TODO: Stale comment? Is it not what GraphicsCache does?
                switch (shaderBytecode.Stage)
                {
                    case ShaderStage.Vertex:
                        vertexShader = pipelineStateCache.VertexShaderCache.Instantiate(shaderBytecode);

                        // NOTE: Input signature can be reused when reseting device since it only stores non-GPU data,
                        //       so just keep it if it has already been created before.
                        inputSignature ??= shaderBytecode;
                        break;

                    case ShaderStage.Domain:
                        domainShader = pipelineStateCache.DomainShaderCache.Instantiate(shaderBytecode);
                        break;

                    case ShaderStage.Hull:
                        hullShader = pipelineStateCache.HullShaderCache.Instantiate(shaderBytecode);
                        break;

                    case ShaderStage.Geometry:
                        if (reflection.ShaderStreamOutputDeclarations?.Count > 0)
                        {
                            // Stream-out elements
                            var streamOutElementCount = reflection.ShaderStreamOutputDeclarations.Count;
                            var streamOutElements = stackalloc SODeclarationEntry[streamOutElementCount];

                            int index = 0;
                            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
                            {
                                var nameLength = Encoding.ASCII.GetByteCount(streamOutputElement.SemanticName);
                                var semanticName = stackalloc byte[nameLength];
                                Encoding.ASCII.GetBytes(streamOutputElement.SemanticName, new Span<byte>(semanticName, nameLength));

                                streamOutElements[index++] = new SODeclarationEntry
                                {
                                    Stream = (uint) streamOutputElement.Stream,
                                    SemanticIndex = (uint) streamOutputElement.SemanticIndex,
                                    SemanticName = semanticName,
                                    StartComponent = streamOutputElement.StartComponent,
                                    ComponentCount = streamOutputElement.ComponentCount,
                                    OutputSlot = streamOutputElement.OutputSlot
                                };
                            }
                            // TODO: GRAPHICS REFACTOR: better cache
                            ID3D11GeometryShader* tempGeometryShader;
                            var bufferStrides = reflection.StreamOutputStrides.AsSpan<int, uint>();

                            HResult result = NativeDevice.CreateGeometryShaderWithStreamOutput(
                                in shaderBytecode.Data[0], (uint) shaderBytecode.Data.Length,
                                streamOutElements, (uint) streamOutElementCount,
                                in bufferStrides[0], (uint) bufferStrides.Length,
                                (uint) reflection.StreamOutputRasterizedStream, pClassLinkage: null,
                                &tempGeometryShader);

                            if (result.IsFailure)
                                result.Throw();

                            geometryShader = tempGeometryShader;
                        }
                        else
                        {
                            geometryShader = pipelineStateCache.GeometryShaderCache.Instantiate(shaderBytecode);
                        }
                        break;

                    case ShaderStage.Pixel:
                        pixelShader = pipelineStateCache.PixelShaderCache.Instantiate(shaderBytecode);
                        break;

                    case ShaderStage.Compute:
                        computeShader = pipelineStateCache.ComputeShaderCache.Instantiate(shaderBytecode);
                        break;
                }
            }
        }

        /// <summary>
        ///   Gets the shared <see cref="PipelineStateCache"/> for the current Graphics Device, or
        ///   creates a new one if it does not exist.
        /// </summary>
        /// <returns>A Pipeline State cache shared for the current Graphics Device.</returns>
        private PipelineStateCache GetPipelineStateCache()
        {
            return GraphicsDevice.GetOrCreateSharedData(typeof(PipelineStateCache), device => new PipelineStateCache(device));
        }

        #region Pipeline State cache

        /// <summary>
        ///   Provides a caching mechanism for Direct3D 11 Pipeline State objects and Shaders.
        /// </summary>
        /// <remarks>
        ///   The <see cref="PipelineStateCache"/> class manages caches for various Direct3D 11 Pipeline State objects,
        ///   including Shaders and State descriptions. This allows efficient reuse of Pipeline State objects and
        ///   reduces overhead associated with their creation.
        /// </remarks>
        [DebuggerDisplay("", Name = $"{nameof(GraphicsDevice)}::{nameof(PipelineStateCache)}")]
        private unsafe class PipelineStateCache : IDisposable
        {
            // Shaders
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11VertexShader> VertexShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11PixelShader> PixelShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11GeometryShader> GeometryShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11HullShader> HullShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11DomainShader> DomainShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ID3D11ComputeShader> ComputeShaderCache;

            // States
            public readonly GraphicsCache<BlendStateDescription, BlendStateDescription, ID3D11BlendState> BlendStateCache;
            public readonly GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, ID3D11RasterizerState> RasterizerStateCache;
            public readonly GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, ID3D11DepthStencilState> DepthStencilStateCache;


            /// <summary>
            ///   Initializes a new instance of the <see cref="PipelineStateCache"/> class.
            /// </summary>
            /// <param name="graphicsDevice">
            ///   The Graphics Device associated with this cache. This is used to create and manage
            ///   Pipeline State objects and Shaders.
            /// </param>
            public PipelineStateCache(GraphicsDevice graphicsDevice)
            {
                var nativeDevice = graphicsDevice.NativeDevice;

                // Shaders
                var nullClassLinkage = NullComPtr<ID3D11ClassLinkage>();

                VertexShaderCache   = new(GetShaderId, CreateVertexShader,   static vs => vs.Release());
                PixelShaderCache    = new(GetShaderId, CreatePixelShader,    static ps => ps.Release());
                GeometryShaderCache = new(GetShaderId, CreateGeometryShader, static gs => gs.Release());
                HullShaderCache     = new(GetShaderId, CreateHullShader,     static hs => hs.Release());
                DomainShaderCache   = new(GetShaderId, CreateDomainShader,   static ds => ds.Release());
                ComputeShaderCache  = new(GetShaderId, CreateComputeShader,  static cs => cs.Release());

                // States
                BlendStateCache        = new(static state => state, CreateBlendState,        static bs => bs.Release());
                RasterizerStateCache   = new(static state => state, CreateRasterizerState,   static rs => rs.Release());
                DepthStencilStateCache = new(static state => state, CreateDepthStencilState, static dss => dss.Release());


                //
                // Gets the unique identifier for a Shader bytecode.
                //
                static ObjectId GetShaderId(ShaderBytecode shader)
                {
                    return shader.Id;
                }

                //
                // Creates a Direct3D 11 Vertex Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11VertexShader> CreateVertexShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11VertexShader> vertexShader = default;

                    HResult result = nativeDevice.CreateVertexShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref vertexShader);

                    if (result.IsFailure)
                        result.Throw();

                    return vertexShader;
                }

                //
                // Creates a Direct3D 11 Hull Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11HullShader> CreateHullShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11HullShader> hullShader = default;

                    HResult result = nativeDevice.CreateHullShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref hullShader);

                    if (result.IsFailure)
                        result.Throw();

                    return hullShader;
                }

                //
                // Creates a Direct3D 11 Domain Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11DomainShader> CreateDomainShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11DomainShader> domainShader = default;

                    HResult result = nativeDevice.CreateDomainShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref domainShader);

                    if (result.IsFailure)
                        result.Throw();

                    return domainShader;
                }

                //
                // Creates a Direct3D 11 Pixel Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11PixelShader> CreatePixelShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11PixelShader> pixelShader = default;

                    HResult result = nativeDevice.CreatePixelShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref pixelShader);

                    if (result.IsFailure)
                        result.Throw();

                    return pixelShader;
                }

                //
                // Creates a Direct3D 11 Compute Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11ComputeShader> CreateComputeShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11ComputeShader> computeShader = default;

                    HResult result = nativeDevice.CreateComputeShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref computeShader);

                    if (result.IsFailure)
                        result.Throw();

                    return computeShader;
                }

                //
                // Creates a Direct3D 11 Geometry Shader from the provided Shader bytecode.
                //
                ComPtr<ID3D11GeometryShader> CreateGeometryShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11GeometryShader> geometryShader = default;

                    HResult result = nativeDevice.CreateGeometryShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref geometryShader);

                    if (result.IsFailure)
                        result.Throw();

                    return geometryShader;
                }

                //
                // Creates a Direct3D 11 Blend State from the provided description.
                //
                ComPtr<ID3D11BlendState> CreateBlendState(BlendStateDescription description)
                {
                    var nativeDescription = new BlendDesc
                    {
                        AlphaToCoverageEnable = description.AlphaToCoverageEnable,
                        IndependentBlendEnable = description.IndependentBlendEnable
                    };

                    var renderTargetCount = description.RenderTargets.Count;
                    ref var renderTargets = ref description.RenderTargets;
                    for (int i = 0; i < renderTargetCount; i++)
                    {
                        ref var renderTarget = ref renderTargets[i];
                        ref var nativeRenderTarget = ref nativeDescription.RenderTarget[i];

                        nativeRenderTarget.BlendEnable = renderTarget.BlendEnable;
                        nativeRenderTarget.SrcBlend = (Silk.NET.Direct3D11.Blend) renderTarget.ColorSourceBlend;
                        nativeRenderTarget.DestBlend = (Silk.NET.Direct3D11.Blend) renderTarget.ColorDestinationBlend;
                        nativeRenderTarget.BlendOp = (BlendOp) renderTarget.ColorBlendFunction;
                        nativeRenderTarget.SrcBlendAlpha = (Silk.NET.Direct3D11.Blend) renderTarget.AlphaSourceBlend;
                        nativeRenderTarget.DestBlendAlpha = (Silk.NET.Direct3D11.Blend) renderTarget.AlphaDestinationBlend;
                        nativeRenderTarget.BlendOpAlpha = (BlendOp) renderTarget.AlphaBlendFunction;
                        nativeRenderTarget.RenderTargetWriteMask = (byte) renderTarget.ColorWriteChannels;
                    }

                    ComPtr<ID3D11BlendState> blendState = default;

                    HResult result = nativeDevice.CreateBlendState(in nativeDescription, ref blendState);

                    if (result.IsFailure)
                        result.Throw();

                    return blendState;
                }

                //
                // Creates a Direct3D 11 Rasterizer State from the provided description.
                //
                ComPtr<ID3D11RasterizerState> CreateRasterizerState(RasterizerStateDescription description)
                {
                    var nativeDescription = new RasterizerDesc
                    {
                        CullMode = (Silk.NET.Direct3D11.CullMode) description.CullMode,
                        FillMode = (Silk.NET.Direct3D11.FillMode) description.FillMode,
                        FrontCounterClockwise = description.FrontFaceCounterClockwise,
                        DepthBias = description.DepthBias,
                        SlopeScaledDepthBias = description.SlopeScaleDepthBias,
                        DepthBiasClamp = description.DepthBiasClamp,
                        DepthClipEnable = description.DepthClipEnable,
                        ScissorEnable = description.ScissorTestEnable,
                        MultisampleEnable = description.MultisampleCount > MultisampleCount.None,
                        AntialiasedLineEnable = description.MultisampleAntiAliasLine
                    };

                    ComPtr<ID3D11RasterizerState> rasterizerState = default;

                    HResult result = nativeDevice.CreateRasterizerState(in nativeDescription, ref rasterizerState);

                    if (result.IsFailure)
                        result.Throw();

                    return rasterizerState;
                }

                //
                // Creates a Direct3D 11 Depth-Stencil State from the provided description.
                //
                ComPtr<ID3D11DepthStencilState> CreateDepthStencilState(DepthStencilStateDescription description)
                {
                    var nativeDescription = new DepthStencilDesc
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

                    ComPtr<ID3D11DepthStencilState> depthStencilState = default;

                    HResult result = nativeDevice.CreateDepthStencilState(in nativeDescription, ref depthStencilState);

                    if (result.IsFailure)
                        result.Throw();

                    return depthStencilState;
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                VertexShaderCache.Dispose();
                PixelShaderCache.Dispose();
                GeometryShaderCache.Dispose();
                HullShaderCache.Dispose();
                DomainShaderCache.Dispose();
                ComputeShaderCache.Dispose();
                BlendStateCache.Dispose();
                RasterizerStateCache.Dispose();
                DepthStencilStateCache.Dispose();
            }
        }

        #endregion

        #region GraphicsCache

        /// <summary>
        ///   Small helper class to cache Direct3D graphics objects.
        /// </summary>
        /// <typeparam name="TSource">The type of the source object for which to cache the Direct3D object.</typeparam>
        /// <typeparam name="TKey">The type of the key used to uniquely identify the cached object.</typeparam>
        /// <typeparam name="TValue">The type of the cached Direct3D object.</typeparam>
        /// <param name="computeKey">A function to compute the key from the source object.</param>
        /// <param name="computeValue">A function to compute the Direct3D object from the source object.</param>
        /// <param name="releaseValue">Optional action to release the cached Direct3D object when it is no longer needed.</param>
        private class GraphicsCache<TSource, TKey, TValue>(
            Func<TSource, TKey> computeKey,
            Func<TSource, ComPtr<TValue>> computeValue,
            Action<ComPtr<TValue>> releaseValue = null) : IDisposable

            where TKey : notnull
            where TSource : notnull
            where TValue : unmanaged, IComVtbl<TValue>, IComVtbl<ID3D11DeviceChild>
        {
            private readonly object lockObject = new();

            // Instantiated objects
            private readonly Dictionary<TKey, ComPtr<TValue>> storage = [];
            // Reverse lookup for quick removal
            private readonly Dictionary<ComPtr<TValue>, TKey> reverse = new(comparer: ComPtrEqualityComparer<TValue>.Default);

            // Reference count for each cached object
            private readonly Dictionary<ComPtr<TValue>, int> referenceCount = new(comparer: ComPtrEqualityComparer<TValue>.Default);

            private readonly Func<TSource, TKey> computeKey = computeKey;
            private readonly Func<TSource, ComPtr<TValue>> computeValue = computeValue;
            private readonly Action<ComPtr<TValue>> releaseValue = releaseValue;


            /// <summary>
            ///   Instantiates a new value or retrieves an existing one based on the specified source.
            /// </summary>
            /// <param name="source">The source object to cache, along with its associated Direct3D object.</param>
            /// <returns>The Direct3D object associated with the provided source object to cache.</returns>
            /// <remarks>
            ///   This method ensures thread-safe access to the underlying storage.
            ///   <para>
            ///     If the key corresponding to the source does not exist, a new value is computed, added to the storage, and its reference count is initialized to 1.
            ///     <br/>
            ///     If the key already exists, the reference count of the associated value is incremented.
            ///   </para>
            /// </remarks>
            public ComPtr<TValue> Instantiate(TSource source)
            {
                lock (lockObject)
                {
                    var key = computeKey(source);

                    if (!storage.TryGetValue(key, out ComPtr<TValue> value))
                    {
                        // New value: Add it to the cache
                        value = computeValue(source);

                        storage.Add(key, value);
                        reverse.Add(value, key);
                        referenceCount.Add(value, 1);
                    }
                    else
                    {
                        // Old value: Increment reference count
                        ref int refCount = ref CollectionsMarshal.GetValueRefOrNullRef(referenceCount, value);
                        if (!Unsafe.IsNullRef(ref refCount))
                            refCount++;
                    }

                    return value;
                }
            }

            /// <summary>
            ///   Releases a reference to the specified Direct3D object, potentially removing it from the cache if no references remain.
            /// </summary>
            /// <param name="value">The Direct3D object to release. Must be a valid value that exists in the cache.</param>
            /// <remarks>
            ///   This method decreases the reference count for the given value. If the reference count reaches zero, the value
            ///   is removed from the cache, along with any associated keys. If a release action is defined, it will be invoked
            ///   for the value being removed.
            /// </remarks>
            public void Release(TValue* value)
                // NOTE: We use ToComPtr to ensure that the reference count is not incremented inadvertently by the implicit conversion to ComPtr<T>
                => Release(ToComPtr(value));

            /// <inheritdoc cref="Release(TValue*)"/>
            public void Release(ComPtr<TValue> value)
            {
                lock (lockObject)
                {
                    ref int refCount = ref CollectionsMarshal.GetValueRefOrNullRef(referenceCount, value);

                    if (Unsafe.IsNullRef(ref refCount))
                        return;

                    // If the reference count reaches 0, no one is using this value anymore. We can remove it from the cache
                    if (--refCount == 0)
                    {
                        referenceCount.Remove(value);
                        if (reverse.TryGetValue(value, out TKey key))
                        {
                            storage.Remove(key);
                        }
                        reverse.Remove(value);

                        releaseValue?.Invoke(value);
                    }
                }
            }

            /// <inheritdoc/>
            public void Dispose()
            {
                lock (lockObject)
                {
                    // Release everything
                    if (releaseValue is not null)
                        foreach ((ComPtr<TValue> value, _) in reverse)
                        {
                            releaseValue(value);
                        }

                    reverse.Clear();
                    storage.Clear();
                    referenceCount.Clear();
                }
            }
        }

        #endregion
    }
}

#endif
