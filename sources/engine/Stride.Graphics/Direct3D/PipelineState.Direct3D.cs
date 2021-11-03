// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Core.Storage;
using Stride.Graphics.Direct3D.Extensions;
using Stride.Shaders;

namespace Stride.Graphics
{
    public partial class PipelineState
    {
        // Effect
        private readonly RootSignature rootSignature;
        private readonly EffectBytecode effectBytecode;
        internal ResourceBinder ResourceBinder;

        private ComPtr<ID3D11VertexShader> vertexShader;
        private ComPtr<ID3D11GeometryShader> geometryShader;
        private ComPtr<ID3D11PixelShader> pixelShader;
        private ComPtr<ID3D11HullShader> hullShader;
        private ComPtr<ID3D11DomainShader> domainShader;
        private ComPtr<ID3D11ComputeShader> computeShader;
        private byte[] inputSignature;

        private readonly ComPtr<ID3D11BlendState> blendState;
        private readonly uint sampleMask;
        private readonly ComPtr<ID3D11RasterizerState> rasterizerState;
        private readonly ComPtr<ID3D11DepthStencilState> depthStencilState;

        private ComPtr<ID3D11InputLayout> inputLayout;

        private readonly D3DPrimitiveTopology primitiveTopology;
        // Note: no need to store RTV/DSV formats

        internal PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription) : base(graphicsDevice)
        {
            // First time, build caches
            var pipelineStateCache = GetPipelineStateCache();

            // Effect
            this.rootSignature = pipelineStateDescription.RootSignature;
            this.effectBytecode = pipelineStateDescription.EffectBytecode;
            CreateShaders(pipelineStateCache);
            if (rootSignature != null && effectBytecode != null)
                ResourceBinder.Compile(graphicsDevice, rootSignature.EffectDescriptorSetReflection, this.effectBytecode);

            // TODO: Cache over Effect|RootSignature to create binding operations

            // States
            pipelineStateCache.BlendStateCache.Instantiate(pipelineStateDescription.BlendState);
            this.sampleMask = pipelineStateDescription.SampleMask;
            unsafe
            {
                rasterizerState = DXConvert.ToRasterizeState(pipelineStateCache.RasterizerStateCache.Instantiate(pipelineStateDescription.RasterizerState));
                depthStencilState = DXConvert.ToDepthStencilState(pipelineStateCache.DepthStencilStateCache.Instantiate(pipelineStateDescription.DepthStencilState));

            }


            CreateInputLayout(pipelineStateDescription.InputElements);

            primitiveTopology = (Silk.NET.Core.Native.D3DPrimitiveTopology)pipelineStateDescription.PrimitiveType;
        }

        internal void Apply(CommandList commandList, PipelineState previousPipeline)
        {
            var nativeDeviceContext = commandList.NativeDeviceContext;

            if (rootSignature != previousPipeline.rootSignature)
            {
                //rootSignature.Apply
            }
            unsafe
            {
                if (effectBytecode != previousPipeline.effectBytecode)
                {
                    if (!computeShader.Equals(previousPipeline.computeShader))
                        nativeDeviceContext.Get().CSSetShader(previousPipeline.computeShader, null, 0);
                    if (!vertexShader.Equals(previousPipeline.vertexShader))
                        nativeDeviceContext.Get().VSSetShader(previousPipeline.vertexShader, null, 0);
                    if (!pixelShader.Equals(previousPipeline.pixelShader))
                        nativeDeviceContext.Get().PSSetShader(previousPipeline.pixelShader, null, 0);
                    if (!hullShader.Equals(previousPipeline.hullShader))
                        nativeDeviceContext.Get().HSSetShader(previousPipeline.hullShader,null,0);
                    if (!domainShader.Equals(previousPipeline.domainShader))
                        nativeDeviceContext.Get().DSSetShader(previousPipeline.domainShader,null,0);
                    if (!geometryShader.Equals(previousPipeline.geometryShader))
                        nativeDeviceContext.Get().GSSetShader(previousPipeline.geometryShader,null,0);
                }

                if (!blendState.Equals(previousPipeline.blendState) || sampleMask != previousPipeline.sampleMask)
                {
                    float* bf = null;
                    nativeDeviceContext.Get().OMGetBlendState(null, bf, null);
                    nativeDeviceContext.Get().OMSetBlendState(blendState, bf, sampleMask);
                }

                if (!rasterizerState.Equals(previousPipeline.rasterizerState))
                {
                    nativeDeviceContext.Get().RSSetState(rasterizerState);
                    //nativeDeviceContext.Rasterizer.State = rasterizerState;
                }

                if (!depthStencilState.Equals(previousPipeline.depthStencilState))
                {
                    nativeDeviceContext.Get().OMSetDepthStencilState(depthStencilState, 0);
                }

                if (!inputLayout.Equals(previousPipeline.inputLayout))
                {
                    nativeDeviceContext.Get().IASetInputLayout(inputLayout);
                }

                if (primitiveTopology != previousPipeline.primitiveTopology)
                {
                    nativeDeviceContext.Get().IASetPrimitiveTopology(primitiveTopology);
                }
            }

            
        }

        protected internal override void OnDestroyed()
        {
            unsafe
            {

                var pipelineStateCache = GetPipelineStateCache();

                pipelineStateCache.BlendStateCache.Release(new ComPtr<IUnknown>((IUnknown*)blendState.Handle));
                pipelineStateCache.RasterizerStateCache.Release(new ComPtr<IUnknown>((IUnknown*)rasterizerState.Handle));
                pipelineStateCache.DepthStencilStateCache.Release(new ComPtr<IUnknown>((IUnknown*)depthStencilState.Handle));

                pipelineStateCache.VertexShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)vertexShader.Handle));
                pipelineStateCache.PixelShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)pixelShader.Handle));
                pipelineStateCache.GeometryShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)geometryShader.Handle));
                pipelineStateCache.HullShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)hullShader.Handle));
                pipelineStateCache.DomainShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)domainShader.Handle));
                pipelineStateCache.ComputeShaderCache.Release(new ComPtr<IUnknown>((IUnknown*)computeShader.Handle));

                inputLayout.Release();
            }

            base.OnDestroyed();
        }

        private void CreateInputLayout(InputElementDescription[] inputElements)
        {
            if (inputElements == null)
                return;

            var nativeInputElements = new InputElementDesc[inputElements.Length];
            for (int index = 0; index < inputElements.Length; index++)
            {
                var inputElement = inputElements[index];
                nativeInputElements[index] = new InputElementDesc
                {
                    InputSlot = (uint)inputElement.InputSlot,
                    SemanticIndex = (uint)inputElement.AlignedByteOffset,
                    Format = (Format)inputElement.Format,
                };
                var chars = inputElement.SemanticName.ToCharArray();
                for (int i = 0; i < chars.Length; i++)
                {
                    unsafe
                    {
                        nativeInputElements[index].SemanticName[i] = (byte)chars[i];
                    }
                }
               
            }
            unsafe
            {
                uint numel = (uint)nativeInputElements.Length;
                uint lenbc = (uint)inputSignature.Length;
                ID3D11InputLayout* res = null;
                fixed(InputElementDesc* ie = nativeInputElements)
                fixed (void* bytecode = inputSignature)
                    NativeDevice.Get().CreateInputLayout(ie, numel, bytecode, lenbc, &res);
                inputLayout = new ComPtr<ID3D11InputLayout>(res);
                
            }
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
                        vertexShader = DXConvert.ToVSShader(pipelineStateCache.VertexShaderCache.Instantiate(shaderBytecode));
                        // Note: input signature can be reused when reseting device since it only stores non-GPU data,
                        // so just keep it if it has already been created before.
                        if (inputSignature == null)
                            inputSignature = shaderBytecode;
                        break;
                    case ShaderStage.Domain:
                        domainShader = DXConvert.ToDSShader(pipelineStateCache.DomainShaderCache.Instantiate(shaderBytecode));
                        break;
                    case ShaderStage.Hull:
                        hullShader = DXConvert.ToHSShader(pipelineStateCache.HullShaderCache.Instantiate(shaderBytecode));
                        break;
                    case ShaderStage.Geometry:
                        if (reflection.ShaderStreamOutputDeclarations != null && reflection.ShaderStreamOutputDeclarations.Count > 0)
                        {
                            // stream out elements
                            var soElements = new List<SODeclarationEntry>();
                            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
                            {
                                var soElem = new SODeclarationEntry()
                                {
                                    Stream = (uint)streamOutputElement.Stream,
                                    SemanticIndex = (uint)streamOutputElement.SemanticIndex,
                                    //SemanticName = streamOutputElement.SemanticName,
                                    StartComponent = streamOutputElement.StartComponent,
                                    ComponentCount = streamOutputElement.ComponentCount,
                                    OutputSlot = streamOutputElement.OutputSlot
                                };
                                for (int i = 0; i < streamOutputElement.SemanticName.Length; i++)
                                {
                                    unsafe
                                    {
                                        soElem.SemanticName[i] = (byte)streamOutputElement.SemanticName[i];

                                    }
                                }
                                soElements.Add(soElem);
                            }
                            unsafe
                            {
                                // TODO GRAPHICS REFACTOR better cache
                                ID3D11GeometryShader* res = null;
                                var bclen = shaderBytecode.Data.Length;
                                var soElemLen = soElements.Count;
                                var soStridesLen = reflection.StreamOutputStrides.Length;
                                fixed (uint* soStrides = reflection.StreamOutputStrides.Select(x => (uint)x).ToArray())
                                fixed (SODeclarationEntry* soE = soElements.ToArray())
                                fixed (void* pBuff = shaderBytecode.Data)
                                    GraphicsDevice.NativeDevice.Get().CreateGeometryShaderWithStreamOutput(pBuff, (uint)bclen, soE, (uint)soElemLen, soStrides, (uint)soStridesLen, (uint)reflection.StreamOutputRasterizedStream, null, &res);
                                geometryShader = new ComPtr<ID3D11GeometryShader>(res);
                            }
                        }
                        else
                        {
                            geometryShader = DXConvert.ToGSShader(pipelineStateCache.GeometryShaderCache.Instantiate(shaderBytecode));
                        }
                        break;
                    case ShaderStage.Pixel:
                        pixelShader = DXConvert.ToPSShader(pipelineStateCache.PixelShaderCache.Instantiate(shaderBytecode));
                        break;
                    case ShaderStage.Compute:
                        computeShader = DXConvert.ToCSShader(pipelineStateCache.ComputeShaderCache.Instantiate(shaderBytecode));
                        break;
                }
            }
        }

        // Small helper to cache SharpDX graphics objects
        private class GraphicsCache<TSource, TKey, TValue> : IDisposable where TValue : struct
        {
            private object lockObject = new object();

            // Store instantiated objects
            private readonly Dictionary<TKey, ComPtr<IUnknown>> storage = new Dictionary<TKey, ComPtr<IUnknown>>();
            // Used for quick removal
            private readonly Dictionary<ComPtr<IUnknown>, TKey> reverse = new Dictionary<ComPtr<IUnknown>, TKey>();

            private readonly Dictionary<ComPtr<IUnknown>, int> counter = new Dictionary<ComPtr<IUnknown>, int>();

            private readonly Func<TSource, TKey> computeKey;
            private readonly Func<TSource, ComPtr<IUnknown>> computeValue;

            public GraphicsCache(Func<TSource, TKey> computeKey, Func<TSource, ComPtr<IUnknown>> computeValue)
            {
                this.computeKey = computeKey;
                this.computeValue = computeValue;
            }

            public ComPtr<IUnknown> Instantiate(TSource source)
            {
                lock (lockObject)
                {
                    ComPtr<IUnknown> value;
                    var key = computeKey(source);
                    if (!storage.TryGetValue(key, out value))
                    {
                        value = computeValue(source);
                        storage.Add(key, value);
                        reverse.Add(value, key);
                        counter.Add(value, 1);
                    }
                    else
                    {
                        counter[value] = counter[value] + 1;
                    }

                    return value;
                }
            }

            public void Release(ComPtr<IUnknown> value)
            {
                // Should we remove it from the cache?
                lock (lockObject)
                {
                    int refCount;
                    if (!counter.TryGetValue(value, out refCount))
                        return;

                    counter[value] = --refCount;
                    if (refCount == 0)
                    {
                        counter.Remove(value);
                        reverse.Remove(value);
                        TKey key;
                        if (reverse.TryGetValue(value, out key))
                        {
                            storage.Remove(key);
                        }

                        value.Release();
                    }
                }
            }

            public void Dispose()
            {
                lock (lockObject)
                {
                    // Release everything
                    foreach (var entry in reverse)
                    {
                        entry.Key.Release();
                    }

                    reverse.Clear();
                    storage.Clear();
                    counter.Clear();
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
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11VertexShader>> VertexShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11PixelShader>> PixelShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11GeometryShader>> GeometryShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11HullShader>> HullShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11DomainShader>> DomainShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11ComputeShader>> ComputeShaderCache;
            public readonly GraphicsCache<BlendStateDescription, BlendStateDescription, ComPtr<ID3D11BlendState>> BlendStateCache;
            public readonly GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, ComPtr<ID3D11RasterizerState>> RasterizerStateCache;
            public readonly GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, ComPtr<ID3D11DepthStencilState>> DepthStencilStateCache;

            public DevicePipelineStateCache(GraphicsDevice graphicsDevice)
            {
                // Shaders
                VertexShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11VertexShader>>(source => source.Id, source => CreateVertexShader(graphicsDevice.NativeDevice, source));
                PixelShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11PixelShader>>(source => source.Id, source => CreatePixelShader(graphicsDevice.NativeDevice, source));
                GeometryShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11GeometryShader>>(source => source.Id, source => CreateGeometryShader(graphicsDevice.NativeDevice, source));
                HullShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11HullShader>>(source => source.Id, source => CreateHullShader(graphicsDevice.NativeDevice, source));
                DomainShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11DomainShader>>(source => source.Id, source => CreateDomainShader(graphicsDevice.NativeDevice, source));
                ComputeShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, ComPtr<ID3D11ComputeShader>>(source => source.Id, source => CreateComputeShader(graphicsDevice.NativeDevice, source));

                // States
                BlendStateCache = new GraphicsCache<BlendStateDescription, BlendStateDescription, ComPtr<ID3D11BlendState>>(source => source, source => CreateBlendState(graphicsDevice.NativeDevice, source));
                RasterizerStateCache = new GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, ComPtr<ID3D11RasterizerState>>(source => source, source => CreateRasterizerState(graphicsDevice.NativeDevice, source));
                DepthStencilStateCache = new GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, ComPtr<ID3D11DepthStencilState>>(source => source, source => CreateDepthStencilState(graphicsDevice.NativeDevice, source));
            }

            private ComPtr<IUnknown> CreateVertexShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11VertexShader* v = null;
                    fixed(byte* psb = source.Data)
                        nativeDevice.Get().CreateVertexShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }
            private ComPtr<IUnknown> CreateHullShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11HullShader* v = null;
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateHullShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }
            private ComPtr<IUnknown> CreateDomainShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11DomainShader* v = null;
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateDomainShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }
            private ComPtr<IUnknown> CreatePixelShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11PixelShader* v = null;
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreatePixelShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }
            private ComPtr<IUnknown> CreateComputeShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11ComputeShader* v = null;
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateComputeShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }
            private ComPtr<IUnknown> CreateGeometryShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ID3D11GeometryShader* v = null;
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateGeometryShader(psb, (uint)bclen, null, &v);
                    return new ComPtr<IUnknown>((IUnknown*)v);
                }
            }

            private unsafe ComPtr<IUnknown> CreateBlendState(ComPtr<ID3D11Device> nativeDevice, BlendStateDescription description)
            {
                var nativeDescription = new BlendDesc
                {
                    AlphaToCoverageEnable = description.AlphaToCoverageEnable ? 1 : 0,
                    IndependentBlendEnable = description.IndependentBlendEnable ? 1 : 0
                };

                var renderTargets = &description.RenderTarget0;
                for (int i = 0; i < 8; i++)
                {
                    ref var renderTarget = ref renderTargets[i];
                    var nativeRenderTarget = nativeDescription.GetRenderTarget(i);
                    nativeRenderTarget.BlendEnable = renderTarget.BlendEnable ? 1 : 0;
                    nativeRenderTarget.SrcBlend = (Silk.NET.Direct3D11.Blend)renderTarget.ColorSourceBlend;
                    nativeRenderTarget.DestBlend = (Silk.NET.Direct3D11.Blend)renderTarget.ColorDestinationBlend;
                    nativeRenderTarget.BlendOp = (BlendOp)renderTarget.ColorBlendFunction;
                    nativeRenderTarget.SrcBlendAlpha = (Silk.NET.Direct3D11.Blend)renderTarget.AlphaSourceBlend;
                    nativeRenderTarget.DestBlendAlpha = (Silk.NET.Direct3D11.Blend)renderTarget.AlphaDestinationBlend;
                    nativeRenderTarget.BlendOpAlpha = (BlendOp)renderTarget.AlphaBlendFunction;
                    nativeRenderTarget.RenderTargetWriteMask = (byte)renderTarget.ColorWriteChannels;
                }
                ID3D11BlendState* res = null;
                nativeDevice.Get().CreateBlendState(&nativeDescription, &res);
                return new ComPtr<IUnknown>((IUnknown*)res);                
            }

            private unsafe ComPtr<IUnknown> CreateRasterizerState(ComPtr<ID3D11Device> nativeDevice, RasterizerStateDescription description)
            {
                RasterizerDesc nativeDescription;

                nativeDescription.CullMode = (Silk.NET.Direct3D11.CullMode)description.CullMode;
                nativeDescription.FillMode = (Silk.NET.Direct3D11.FillMode)description.FillMode;
                nativeDescription.FrontCounterClockwise = description.FrontFaceCounterClockwise ? 1 : 0;
                nativeDescription.DepthBias = description.DepthBias;
                nativeDescription.SlopeScaledDepthBias = description.SlopeScaleDepthBias;
                nativeDescription.DepthBiasClamp = description.DepthBiasClamp;
                nativeDescription.DepthClipEnable = description.DepthClipEnable ? 1 : 0;
                nativeDescription.ScissorEnable = description.ScissorTestEnable ? 1 : 0;
                nativeDescription.MultisampleEnable = description.MultisampleCount > MultisampleCount.None ? 1 : 0;
                nativeDescription.AntialiasedLineEnable = description.MultisampleAntiAliasLine ? 1 : 0;

                ID3D11RasterizerState* res = null;
                nativeDevice.Get().CreateRasterizerState(&nativeDescription, &res);
                return new ComPtr<IUnknown>((IUnknown*)res);
            }

            private unsafe ComPtr<IUnknown> CreateDepthStencilState(ComPtr<ID3D11Device> nativeDevice, DepthStencilStateDescription description)
            {
                DepthStencilDesc nativeDescription;

                nativeDescription.DepthEnable = description.DepthBufferEnable ? 1 : 0;
                nativeDescription.DepthFunc = (ComparisonFunc)description.DepthBufferFunction;
                nativeDescription.DepthWriteMask = description.DepthBufferWriteEnable ? DepthWriteMask.DepthWriteMaskAll : DepthWriteMask.DepthWriteMaskZero;

                nativeDescription.StencilEnable = description.StencilEnable ? 1 : 0;
                nativeDescription.StencilReadMask = description.StencilMask;
                nativeDescription.StencilWriteMask = description.StencilWriteMask;

                nativeDescription.FrontFace.StencilFailOp = (StencilOp)description.FrontFace.StencilFail;
                nativeDescription.FrontFace.StencilPassOp = (StencilOp)description.FrontFace.StencilPass;
                nativeDescription.FrontFace.StencilDepthFailOp = (StencilOp)description.FrontFace.StencilDepthBufferFail;
                nativeDescription.FrontFace.StencilFunc = (ComparisonFunc)description.FrontFace.StencilFunction;

                nativeDescription.BackFace.StencilFailOp = (StencilOp)description.BackFace.StencilFail;
                nativeDescription.BackFace.StencilPassOp = (StencilOp)description.BackFace.StencilPass;
                nativeDescription.BackFace.StencilDepthFailOp = (StencilOp)description.BackFace.StencilDepthBufferFail;
                nativeDescription.BackFace.StencilFunc = (ComparisonFunc)description.BackFace.StencilFunction;

                ID3D11DepthStencilState* res = null;
                nativeDevice.Get().CreateDepthStencilState(&nativeDescription, &res);
                return new ComPtr<IUnknown>((IUnknown*)res);
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
