// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;

using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Core.UnsafeExtensions;
using Stride.Shaders;

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
                ID3D11BlendState* tempBlendState;
                Color4 blendFactor;
                uint tempSampleMask;

                nativeDeviceContext.OMGetBlendState(&tempBlendState, (float*) &blendFactor, &tempSampleMask);

                if (tempBlendState is not null)
                    tempBlendState->Release();

                nativeDeviceContext.OMSetBlendState(blendState, (float*) &blendFactor, sampleMask);
            }

            if (rasterizerState != previousPipeline.rasterizerState)
            {
                nativeDeviceContext.RSSetState(rasterizerState);
            }

            if (depthStencilState != previousPipeline.depthStencilState)
            {
                ID3D11DepthStencilState* tempDepthStencilState;
                uint stencilRef;

                nativeDeviceContext.OMGetDepthStencilState(&tempDepthStencilState, &stencilRef);

                if (tempDepthStencilState is not null)
                    tempDepthStencilState->Release();

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

            ID3D11InputLayout* tempInputLayout;

            HResult result = NativeDevice.CreateInputLayout(nativeInputElements, NumElements: (uint) inputElements.Length,
                                                            in inputSignature[0], (uint) inputSignature.Length, &tempInputLayout);

            if (result.IsFailure)
                result.Throw();

            inputLayout = tempInputLayout;
        }

        private void CreateShaders(DevicePipelineStateCache pipelineStateCache)
        {
            if (effectBytecode is null)
                return;

            foreach (var shaderBytecode in effectBytecode.Stages)
            {
                var reflection = effectBytecode.Reflection;

                // TODO: CACHE Shaders with a bytecode hash
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
        ///   Small helper to cache Direct3D graphics objects.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        private DevicePipelineStateCache GetPipelineStateCache()
        {
            return GraphicsDevice.GetOrCreateSharedData(typeof(DevicePipelineStateCache), device => new DevicePipelineStateCache(device));
        }

        #region Pipeline State cache

        private unsafe class DevicePipelineStateCache : IDisposable
        {
            // Shaders
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11VertexShader>> VertexShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11PixelShader>> PixelShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11GeometryShader>> GeometryShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11HullShader>> HullShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11DomainShader>> DomainShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11ComputeShader>> ComputeShaderCache;

            // States
            public readonly GraphicsCache<BlendStateDescription, BlendStateDescription, ComPtr<ID3D11BlendState>> BlendStateCache;
            public readonly GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, ComPtr<ID3D11RasterizerState>> RasterizerStateCache;
            public readonly GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, ComPtr<ID3D11DepthStencilState>> DepthStencilStateCache;


            public DevicePipelineStateCache(GraphicsDevice graphicsDevice)
            {
                var nativeDevice = graphicsDevice.NativeDevice;

                // Shaders
                var nullClassLinkage = ComPtrHelpers.NullComPtr<ID3D11ClassLinkage>();

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


                static ObjectId GetShaderId(ShaderBytecode shader)
                {
                    return shader.Id;
                }

                ComPtr<ID3D11VertexShader> CreateVertexShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11VertexShader> vertexShader = default;

                    HResult result = nativeDevice.CreateVertexShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref vertexShader);

                    if (result.IsFailure)
                        result.Throw();

                    return vertexShader;
                }

                ComPtr<ID3D11HullShader> CreateHullShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11HullShader> hullShader = default;

                    HResult result = nativeDevice.CreateHullShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref hullShader);

                    if (result.IsFailure)
                        result.Throw();

                    return hullShader;
                }

                ComPtr<ID3D11DomainShader> CreateDomainShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11DomainShader> domainShader = default;

                    HResult result = nativeDevice.CreateDomainShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref domainShader);

                    if (result.IsFailure)
                        result.Throw();

                    return domainShader;
                }

                ComPtr<ID3D11PixelShader> CreatePixelShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11PixelShader> pixelShader = default;

                    HResult result = nativeDevice.CreatePixelShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref pixelShader);

                    if (result.IsFailure)
                        result.Throw();

                    return pixelShader;
                }

                ComPtr<ID3D11ComputeShader> CreateComputeShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11ComputeShader> computeShader = default;

                    HResult result = nativeDevice.CreateComputeShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref computeShader);

                    if (result.IsFailure)
                        result.Throw();

                    return computeShader;
                }

                ComPtr<ID3D11GeometryShader> CreateGeometryShader(ShaderBytecode source)
                {
                    ComPtr<ID3D11GeometryShader> geometryShader = default;

                    HResult result = nativeDevice.CreateGeometryShader(in source.Data[0], (nuint) source.Data.Length, nullClassLinkage, ref geometryShader);

                    if (result.IsFailure)
                        result.Throw();

                    return geometryShader;
                }

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

        private class GraphicsCache<TSource, TKey, TValue>(
            Func<TSource, TKey> computeKey,
            Func<TSource, TValue> computeValue,
            Action<TValue> releaseValue = null) : IDisposable
        {
            private readonly object lockObject = new();

            private readonly Dictionary<TKey, TValue> storage = [];   // Instantiated objects
            private readonly Dictionary<TValue, TKey> reverse = [];   // Reverse lookup for quick removal

            private readonly Dictionary<TValue, int> referenceCount = [];

            private readonly Func<TSource, TKey> computeKey = computeKey;
            private readonly Func<TSource, TValue> computeValue = computeValue;
            private readonly Action<TValue> releaseValue = releaseValue;


            public TValue Instantiate(TSource source)
            {
                lock (lockObject)
                {
                    var key = computeKey(source);

                    if (!storage.TryGetValue(key, out var value))
                    {
                        value = computeValue(source);

                        storage.Add(key, value);
                        reverse.Add(value, key);
                        referenceCount.Add(value, 1);
                    }
                    else
                    {
                        ref int refCount = ref CollectionsMarshal.GetValueRefOrNullRef(referenceCount, value);
                        if (!Unsafe.IsNullRef(ref refCount))
                            refCount++;
                    }

                    return value;
                }
            }

            public void Release(TValue value)
            {
                lock (lockObject)
                {
                    ref int refCount = ref CollectionsMarshal.GetValueRefOrNullRef(referenceCount, value);

                    if (Unsafe.IsNullRef(ref refCount))
                        return;

                    if (--refCount == 0)
                    {
                        referenceCount.Remove(value);
                        if (reverse.TryGetValue(value, out var key))
                        {
                            storage.Remove(key);
                        }
                        reverse.Remove(value);

                        releaseValue?.Invoke(value);
                    }
                }
            }

            public void Dispose()
            {
                lock (lockObject)
                {
                    // Release everything
                    if (releaseValue is not null)
                        foreach (var entry in reverse)
                        {
                            releaseValue(entry.Key);
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
