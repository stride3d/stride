// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
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

            if (rootSignature != null && effectBytecode != null)
                ResourceBinder.Compile(graphicsDevice, rootSignature.EffectDescriptorSetReflection, effectBytecode);

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
                //rootSignature.Apply
            }

            if (effectBytecode != previousPipeline.effectBytecode)
            {
                if (computeShader != previousPipeline.computeShader)
                    nativeDeviceContext->CSSetShader(computeShader, ppClassInstances: null, NumClassInstances: 0);

                if (vertexShader != previousPipeline.vertexShader)
                    nativeDeviceContext->VSSetShader(vertexShader, ppClassInstances: null, NumClassInstances: 0);

                if (pixelShader != previousPipeline.pixelShader)
                    nativeDeviceContext->PSSetShader(pixelShader, ppClassInstances: null, NumClassInstances: 0);

                if (hullShader != previousPipeline.hullShader)
                    nativeDeviceContext->HSSetShader(hullShader, ppClassInstances: null, NumClassInstances: 0);

                if (domainShader != previousPipeline.domainShader)
                    nativeDeviceContext->DSSetShader(domainShader, ppClassInstances: null, NumClassInstances: 0);

                if (geometryShader != previousPipeline.geometryShader)
                    nativeDeviceContext->GSSetShader(geometryShader, ppClassInstances: null, NumClassInstances: 0);
            }

            if (blendState != previousPipeline.blendState || sampleMask != previousPipeline.sampleMask)
            {
                ID3D11BlendState* tempBlendState;
                Color4 blendFactor;
                uint tempSampleMask;

                nativeDeviceContext->OMGetBlendState(&tempBlendState, (float*) &blendFactor, &tempSampleMask);

                if (tempBlendState != null)
                    tempBlendState->Release();

                nativeDeviceContext->OMSetBlendState(blendState, (float*) &blendFactor, sampleMask);
            }

            if (rasterizerState != previousPipeline.rasterizerState)
            {
                nativeDeviceContext->RSSetState(rasterizerState);
            }

            if (depthStencilState != previousPipeline.depthStencilState)
            {
                ID3D11DepthStencilState* tempDepthStencilState;
                uint stencilRef;

                nativeDeviceContext->OMGetDepthStencilState(&tempDepthStencilState, &stencilRef);

                if (tempDepthStencilState != null)
                    tempDepthStencilState->Release();

                nativeDeviceContext->OMSetDepthStencilState(depthStencilState, stencilRef);
            }

            if (inputLayout != previousPipeline.inputLayout)
            {
                nativeDeviceContext->IASetInputLayout(inputLayout);
            }

            if (primitiveTopology != previousPipeline.primitiveTopology)
            {
                nativeDeviceContext->IASetPrimitiveTopology(primitiveTopology);
            }
        }

        protected internal override void OnDestroyed()
        {
            var pipelineStateCache = GetPipelineStateCache();

            if (blendState != null)
                pipelineStateCache.BlendStateCache.Release(blendState);
            if (rasterizerState != null)
                pipelineStateCache.RasterizerStateCache.Release(rasterizerState);
            if (depthStencilState != null)
                pipelineStateCache.DepthStencilStateCache.Release(depthStencilState);

            if (vertexShader != null)
                pipelineStateCache.VertexShaderCache.Release(vertexShader);
            if (pixelShader != null)
                pipelineStateCache.PixelShaderCache.Release(pixelShader);
            if (geometryShader != null)
                pipelineStateCache.GeometryShaderCache.Release(geometryShader);
            if (hullShader != null)
                pipelineStateCache.HullShaderCache.Release(hullShader);
            if (domainShader != null)
                pipelineStateCache.DomainShaderCache.Release(domainShader);
            if (computeShader != null)
                pipelineStateCache.ComputeShaderCache.Release(computeShader);

            if (inputLayout != null)
                inputLayout->Release();

            base.OnDestroyed();
        }

        private void CreateInputLayout(InputElementDescription[] inputElements)
        {
            if (inputElements == null)
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

            HResult result = NativeDevice->CreateInputLayout(nativeInputElements, NumElements: (uint) inputElements.Length,
                                                             in inputSignature[0], (uint) inputSignature.Length, &tempInputLayout);

            if (result.IsFailure)
                result.Throw();

            inputLayout = tempInputLayout;
        }

        private void CreateShaders(DevicePipelineStateCache pipelineStateCache)
        {
            if (effectBytecode == null)
                return;

            foreach (var shaderBytecode in effectBytecode.Stages)
            {
                var reflection = effectBytecode.Reflection;

                // TODO CACHE Shaders with a bytecode hash
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
                        if (reflection.ShaderStreamOutputDeclarations != null && reflection.ShaderStreamOutputDeclarations.Count > 0)
                        {
                            // Stream out elements
                            var streamOutElementCount = reflection.ShaderStreamOutputDeclarations.Count;
                            var streamOutElements = stackalloc SODeclarationEntry[streamOutElementCount];

                            int index = 0;
                            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
                            {
                                var nameLength = Encoding.ASCII.GetByteCount(streamOutputElement.SemanticName);
                                var semanticName = stackalloc byte[nameLength];
                                Encoding.ASCII.GetBytes(streamOutputElement.SemanticName, new Span<byte>(semanticName, nameLength));

                                streamOutElements[index++] = new SODeclarationEntry()
                                {
                                    Stream = (uint) streamOutputElement.Stream,
                                    SemanticIndex = (uint) streamOutputElement.SemanticIndex,
                                    SemanticName = semanticName,
                                    StartComponent = streamOutputElement.StartComponent,
                                    ComponentCount = streamOutputElement.ComponentCount,
                                    OutputSlot = streamOutputElement.OutputSlot
                                };
                            }
                            // TODO GRAPHICS REFACTOR better cache
                            ID3D11GeometryShader* tempGeometryShader;
                            var bufferStrides = MemoryMarshal.Cast<int, uint>(reflection.StreamOutputStrides);

                            HResult result = NativeDevice->CreateGeometryShaderWithStreamOutput(
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
        private class GraphicsCache<TSource, TKey, TValue> : IDisposable
        {
            private readonly object lockObject = new();

            // Store instantiated objects
            private readonly Dictionary<TKey, TValue> storage = new ();
            // Used for quick removal
            private readonly Dictionary<TValue, TKey> reverse = new();

            private readonly Dictionary<TValue, int> referenceCount = new();

            private readonly Func<TSource, TKey> computeKey;
            private readonly Func<TSource, TValue> computeValue;
            private readonly Action<TValue> releaseValue;

            public GraphicsCache(Func<TSource, TKey> computeKey, Func<TSource, TValue> computeValue, Action<TValue> releaseValue = null)
            {
                this.computeKey = computeKey;
                this.computeValue = computeValue;
                this.releaseValue = releaseValue;
            }

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
                // Should we remove it from the cache?
                lock (lockObject)
                {
                    ref int refCount = ref CollectionsMarshal.GetValueRefOrNullRef(referenceCount, value);

                    if (Unsafe.IsNullRef(ref refCount))
                        return;

                    if (--refCount == 0)
                    {
                        referenceCount.Remove(value);
                        reverse.Remove(value);
                        if (reverse.TryGetValue(value, out var key))
                        {
                            storage.Remove(key);
                        }

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

        private DevicePipelineStateCache GetPipelineStateCache()
        {
            return GraphicsDevice.GetOrCreateSharedData(typeof(DevicePipelineStateCache), device => new DevicePipelineStateCache(device));
        }

        // Caches
        private class DevicePipelineStateCache : IDisposable
        {
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11VertexShader>> VertexShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11PixelShader>> PixelShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11GeometryShader>> GeometryShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11HullShader>> HullShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11DomainShader>> DomainShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, Pointer<ID3D11ComputeShader>> ComputeShaderCache;
            public readonly GraphicsCache<BlendStateDescription, BlendStateDescription, Pointer<ID3D11BlendState>> BlendStateCache;
            public readonly GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, Pointer<ID3D11RasterizerState>> RasterizerStateCache;
            public readonly GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, Pointer<ID3D11DepthStencilState>> DepthStencilStateCache;

            public DevicePipelineStateCache(GraphicsDevice graphicsDevice)
            {
                // Shaders
                VertexShaderCache = new(source => source.Id, source => CreateVertexShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                PixelShaderCache = new(source => source.Id, source => CreatePixelShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                GeometryShaderCache = new(source => source.Id, source => CreateGeometryShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                HullShaderCache = new(source => source.Id, source => CreateHullShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                DomainShaderCache = new(source => source.Id, source => CreateDomainShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                ComputeShaderCache = new(source => source.Id, source => CreateComputeShader(graphicsDevice.NativeDevice, source), source => source.Value->Release());

                // States
                BlendStateCache = new(source => source, source => CreateBlendState(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                RasterizerStateCache = new(source => source, source => CreateRasterizerState(graphicsDevice.NativeDevice, source), source => source.Value->Release());
                DepthStencilStateCache = new(source => source, source => CreateDepthStencilState(graphicsDevice.NativeDevice, source), source => source.Value->Release());
            }

            private ID3D11VertexShader* CreateVertexShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11VertexShader* vertexShader;
                HResult result = nativeDevice->CreateVertexShader(in source.Data[0], (nuint) source.Data.Length, null, &vertexShader);

                if (result.IsFailure)
                    result.Throw();

                return vertexShader;
            }

            private ID3D11HullShader* CreateHullShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11HullShader* hullShader;
                HResult result = nativeDevice->CreateHullShader(in source.Data[0], (nuint) source.Data.Length, null, &hullShader);

                if (result.IsFailure)
                    result.Throw();

                return hullShader;
            }

            private ID3D11DomainShader* CreateDomainShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11DomainShader* domainShader;
                HResult result = nativeDevice->CreateDomainShader(in source.Data[0], (nuint) source.Data.Length, null, &domainShader);

                if (result.IsFailure)
                    result.Throw();

                return domainShader;
            }

            private ID3D11PixelShader* CreatePixelShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11PixelShader* pixelShader;
                HResult result = nativeDevice->CreatePixelShader(in source.Data[0], (nuint) source.Data.Length, null, &pixelShader);

                if (result.IsFailure)
                    result.Throw();

                return pixelShader;
            }

            private ID3D11ComputeShader* CreateComputeShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11ComputeShader* computeShader;
                HResult result = nativeDevice->CreateComputeShader(in source.Data[0], (nuint) source.Data.Length, null, &computeShader);

                if (result.IsFailure)
                    result.Throw();

                return computeShader;
            }

            private ID3D11GeometryShader* CreateGeometryShader(ID3D11Device* nativeDevice, ShaderBytecode source)
            {
                ID3D11GeometryShader* geometryShader;
                HResult result = nativeDevice->CreateGeometryShader(in source.Data[0], (nuint) source.Data.Length, null, &geometryShader);

                if (result.IsFailure)
                    result.Throw();

                return geometryShader;
            }

            private ID3D11BlendState* CreateBlendState(ID3D11Device* nativeDevice, BlendStateDescription description)
            {
                var nativeDescription = new BlendDesc
                {
                    AlphaToCoverageEnable = description.AlphaToCoverageEnable,
                    IndependentBlendEnable = description.IndependentBlendEnable
                };

                var renderTargets = &description.RenderTarget0;
                for (int i = 0; i < 8; i++)
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

                ID3D11BlendState* blendState;
                HResult result = nativeDevice->CreateBlendState(in nativeDescription, &blendState);

                if (result.IsFailure)
                    result.Throw();

                return blendState;
            }

            private ID3D11RasterizerState* CreateRasterizerState(ID3D11Device* nativeDevice, RasterizerStateDescription description)
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

                ID3D11RasterizerState* rasterizerState;
                HResult result = nativeDevice->CreateRasterizerState(in nativeDescription, &rasterizerState);

                if (result.IsFailure)
                    result.Throw();

                return rasterizerState;
            }

            private ID3D11DepthStencilState* CreateDepthStencilState(ID3D11Device* nativeDevice, DepthStencilStateDescription description)
            {
                var nativeDescription = new DepthStencilDesc
                {
                    DepthEnable = description.DepthBufferEnable,
                    DepthFunc = (ComparisonFunc)description.DepthBufferFunction,
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

                ID3D11DepthStencilState* depthStencilState;
                HResult result = nativeDevice->CreateDepthStencilState(in nativeDescription, &depthStencilState);

                if (result.IsFailure)
                    result.Throw();

                return depthStencilState;
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
    }
}

#endif
