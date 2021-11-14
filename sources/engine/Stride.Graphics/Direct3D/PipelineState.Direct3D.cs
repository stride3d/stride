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

        private VertexShader vertexShader;
        private GeometryShader geometryShader;
        private PixelShader pixelShader;
        private HullShader hullShader;
        private DomainShader domainShader;
        private ComputeShader computeShader;
        private byte[] inputSignature;

        private readonly BlendState blendState;
        private readonly uint sampleMask;
        private readonly RasterizerState rasterizerState;
        private readonly DepthStencilState depthStencilState;

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
                rasterizerState = pipelineStateCache.RasterizerStateCache.Instantiate(pipelineStateDescription.RasterizerState) as RasterizerState;
                depthStencilState = pipelineStateCache.DepthStencilStateCache.Instantiate(pipelineStateDescription.DepthStencilState) as DepthStencilState;

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
                    if (computeShader != previousPipeline.computeShader && previousPipeline.computeShader != null)
                        nativeDeviceContext.Get().CSSetShader(previousPipeline.computeShader.GetData().Handle, null, 0);
                    if (vertexShader != previousPipeline.vertexShader && previousPipeline.vertexShader != null)
                        nativeDeviceContext.Get().VSSetShader(previousPipeline.vertexShader.GetData().Handle, null, 0);
                    if (pixelShader != previousPipeline.pixelShader && previousPipeline.pixelShader != null)
                        nativeDeviceContext.Get().PSSetShader(previousPipeline.pixelShader.GetData().Handle, null, 0);
                    if (hullShader != previousPipeline.hullShader && previousPipeline.hullShader != null)
                        nativeDeviceContext.Get().HSSetShader(previousPipeline.hullShader.GetData().Handle, null,0);
                    if (domainShader != previousPipeline.domainShader && previousPipeline.domainShader != null)
                        nativeDeviceContext.Get().DSSetShader(previousPipeline.domainShader.GetData().Handle, null,0);
                    if (geometryShader != previousPipeline.geometryShader && previousPipeline.geometryShader != null)
                        nativeDeviceContext.Get().GSSetShader(previousPipeline.geometryShader.GetData().Handle, null,0);
                }

                if ((blendState != previousPipeline.blendState && previousPipeline.blendState != null) || sampleMask != previousPipeline.sampleMask)
                {
                    float* bf = null;
                    nativeDeviceContext.Get().OMGetBlendState(null, bf, null);
                    nativeDeviceContext.Get().OMSetBlendState(blendState.GetData().Handle, bf, sampleMask);
                }

                if (rasterizerState != previousPipeline.rasterizerState && previousPipeline.rasterizerState != null)
                {
                    nativeDeviceContext.Get().RSSetState(rasterizerState.GetData().Handle);
                    //nativeDeviceContext.Rasterizer.State = rasterizerState;
                }

                if (depthStencilState != previousPipeline.depthStencilState && previousPipeline.depthStencilState != null)
                {
                    nativeDeviceContext.Get().OMSetDepthStencilState(depthStencilState.GetData().Handle, 0);
                }

                if (!inputLayout.Equals(previousPipeline.inputLayout) && previousPipeline.inputLayout.Handle != null)
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

                pipelineStateCache.BlendStateCache.Release(blendState);
                pipelineStateCache.RasterizerStateCache.Release(rasterizerState);
                pipelineStateCache.DepthStencilStateCache.Release(depthStencilState);

                pipelineStateCache.VertexShaderCache.Release(vertexShader);
                pipelineStateCache.PixelShaderCache.Release(pixelShader);
                pipelineStateCache.GeometryShaderCache.Release(geometryShader);
                pipelineStateCache.HullShaderCache.Release(hullShader);
                pipelineStateCache.DomainShaderCache.Release(domainShader);
                pipelineStateCache.ComputeShaderCache.Release(computeShader);

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
                var chars = inputElement.SemanticName.ToCharArray().Select(x => (byte)x).ToArray();
                unsafe
                {
                    fixed(byte* charsPtr = chars)
                        nativeInputElements[index].SemanticName = charsPtr;
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
                        vertexShader = pipelineStateCache.VertexShaderCache.Instantiate(shaderBytecode) as VertexShader;
                        // Note: input signature can be reused when reseting device since it only stores non-GPU data,
                        // so just keep it if it has already been created before.
                        if (inputSignature == null)
                            inputSignature = shaderBytecode;
                        break;
                    case ShaderStage.Domain:
                        domainShader = pipelineStateCache.DomainShaderCache.Instantiate(shaderBytecode) as DomainShader;
                        break;
                    case ShaderStage.Hull:
                        hullShader = pipelineStateCache.HullShaderCache.Instantiate(shaderBytecode) as HullShader;
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
                                geometryShader = new(new ComPtr<ID3D11GeometryShader>(res));
                            }
                        }
                        else
                        {
                            geometryShader = pipelineStateCache.GeometryShaderCache.Instantiate(shaderBytecode) as GeometryShader;
                        }
                        break;
                    case ShaderStage.Pixel:
                        pixelShader = pipelineStateCache.PixelShaderCache.Instantiate(shaderBytecode) as PixelShader;
                        break;
                    case ShaderStage.Compute:
                        computeShader = pipelineStateCache.ComputeShaderCache.Instantiate(shaderBytecode) as ComputeShader;
                        break;
                }
            }
        }

        public class ElementWrapper<T>
        {
            public T data;
        }

        // Small helper to cache SharpDX graphics objects
        private class GraphicsCache<TSource, TKey, TValue> : IDisposable
        {
            private object lockObject = new object();

            // Store instantiated objects
            private readonly Dictionary<TKey, GfxBox<TValue>> storage = new ();
            // Used for quick removal
            private readonly Dictionary<GfxBox<TValue>, TKey> reverse = new();

            private readonly Dictionary<GfxBox<TValue>, int> counter = new();

            private readonly Func<TSource, TKey> computeKey;
            private readonly Func<TSource, GfxBox<TValue>> computeValue;

            public GraphicsCache(Func<TSource, TKey> computeKey, Func<TSource, GfxBox<TValue>> computeValue)
            {
                this.computeKey = computeKey;
                this.computeValue = computeValue;
            }

            public GfxBox<TValue> Instantiate(TSource source)
            {
                lock (lockObject)
                {
                    GfxBox<TValue> value;
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

            public void Release(GfxBox<TValue> value)
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

            private VertexShader CreateVertexShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11VertexShader> v = new();
                    fixed(byte* psb = source.Data)
                        nativeDevice.Get().CreateVertexShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }
            private HullShader CreateHullShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11HullShader> v = new();
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateHullShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }
            private DomainShader CreateDomainShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11DomainShader> v = new();
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateDomainShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }
            private PixelShader CreatePixelShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11PixelShader> v = new();
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreatePixelShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }
            private ComputeShader CreateComputeShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11ComputeShader> v = new();
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateComputeShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }
            private GeometryShader CreateGeometryShader(ComPtr<ID3D11Device> nativeDevice, ShaderBytecode source)
            {
                unsafe
                {
                    var bclen = source.Data.Length;
                    ComPtr<ID3D11GeometryShader> v = new();
                    fixed (byte* psb = source.Data)
                        nativeDevice.Get().CreateGeometryShader(psb, (uint)bclen, null, ref v.Handle);
                    return new(v);
                }
            }

            private unsafe BlendState CreateBlendState(ComPtr<ID3D11Device> nativeDevice, BlendStateDescription description)
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
                ComPtr<ID3D11BlendState> res = new();
                nativeDevice.Get().CreateBlendState(&nativeDescription, ref res.Handle);
                return new(res);                
            }

            private unsafe RasterizerState CreateRasterizerState(ComPtr<ID3D11Device> nativeDevice, RasterizerStateDescription description)
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

                ComPtr<ID3D11RasterizerState> res = new();
                nativeDevice.Get().CreateRasterizerState(&nativeDescription, ref res.Handle);
                return new(res);
            }

            private unsafe DepthStencilState CreateDepthStencilState(ComPtr<ID3D11Device> nativeDevice, DepthStencilStateDescription description)
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

                ComPtr<ID3D11DepthStencilState> res = new();
                nativeDevice.Get().CreateDepthStencilState(&nativeDescription, ref res.Handle);
                return new(res);
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
