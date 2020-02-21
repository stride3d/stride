// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Storage;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    public partial class PipelineState
    {
        // Effect
        private readonly RootSignature rootSignature;
        private readonly EffectBytecode effectBytecode;
        internal ResourceBinder ResourceBinder;

        private SharpDX.Direct3D11.VertexShader vertexShader;
        private SharpDX.Direct3D11.GeometryShader geometryShader;
        private SharpDX.Direct3D11.PixelShader pixelShader;
        private SharpDX.Direct3D11.HullShader hullShader;
        private SharpDX.Direct3D11.DomainShader domainShader;
        private SharpDX.Direct3D11.ComputeShader computeShader;
        private byte[] inputSignature;

        private readonly SharpDX.Direct3D11.BlendState blendState;
        private readonly uint sampleMask;
        private readonly SharpDX.Direct3D11.RasterizerState rasterizerState;
        private readonly SharpDX.Direct3D11.DepthStencilState depthStencilState;

        private SharpDX.Direct3D11.InputLayout inputLayout;

        private readonly SharpDX.Direct3D.PrimitiveTopology primitiveTopology;
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
            blendState = pipelineStateCache.BlendStateCache.Instantiate(pipelineStateDescription.BlendState);

            this.sampleMask = pipelineStateDescription.SampleMask;
            rasterizerState = pipelineStateCache.RasterizerStateCache.Instantiate(pipelineStateDescription.RasterizerState);
            depthStencilState = pipelineStateCache.DepthStencilStateCache.Instantiate(pipelineStateDescription.DepthStencilState);

            CreateInputLayout(pipelineStateDescription.InputElements);

            primitiveTopology = (SharpDX.Direct3D.PrimitiveTopology)pipelineStateDescription.PrimitiveType;
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
                    nativeDeviceContext.ComputeShader.Set(computeShader);
                if (vertexShader != previousPipeline.vertexShader)
                    nativeDeviceContext.VertexShader.Set(vertexShader);
                if (pixelShader != previousPipeline.pixelShader)
                    nativeDeviceContext.PixelShader.Set(pixelShader);
                if (hullShader != previousPipeline.hullShader)
                    nativeDeviceContext.HullShader.Set(hullShader);
                if (domainShader != previousPipeline.domainShader)
                    nativeDeviceContext.DomainShader.Set(domainShader);
                if (geometryShader != previousPipeline.geometryShader)
                    nativeDeviceContext.GeometryShader.Set(geometryShader);
            }

            if (blendState != previousPipeline.blendState || sampleMask != previousPipeline.sampleMask)
            {
                nativeDeviceContext.OutputMerger.SetBlendState(blendState, nativeDeviceContext.OutputMerger.BlendFactor, sampleMask);
            }

            if (rasterizerState != previousPipeline.rasterizerState)
            {
                nativeDeviceContext.Rasterizer.State = rasterizerState;
            }

            if (depthStencilState != previousPipeline.depthStencilState)
            {
                nativeDeviceContext.OutputMerger.DepthStencilState = depthStencilState;
            }

            if (inputLayout != previousPipeline.inputLayout)
            {
                nativeDeviceContext.InputAssembler.InputLayout = inputLayout;
            }

            if (primitiveTopology != previousPipeline.primitiveTopology)
            {
                nativeDeviceContext.InputAssembler.PrimitiveTopology = primitiveTopology;
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

            inputLayout?.Dispose();

            base.OnDestroyed();
        }

        private void CreateInputLayout(InputElementDescription[] inputElements)
        {
            if (inputElements == null)
                return;

            var nativeInputElements = new SharpDX.Direct3D11.InputElement[inputElements.Length];
            for (int index = 0; index < inputElements.Length; index++)
            {
                var inputElement = inputElements[index];
                nativeInputElements[index] = new SharpDX.Direct3D11.InputElement
                {
                    Slot = inputElement.InputSlot,
                    SemanticName = inputElement.SemanticName,
                    SemanticIndex = inputElement.SemanticIndex,
                    AlignedByteOffset = inputElement.AlignedByteOffset,
                    Format = (SharpDX.DXGI.Format)inputElement.Format,
                };
            }
            inputLayout = new SharpDX.Direct3D11.InputLayout(NativeDevice, inputSignature, nativeInputElements);
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
                        // Note: input signature can be reused when reseting device since it only stores non-GPU data,
                        // so just keep it if it has already been created before.
                        if (inputSignature == null)
                            inputSignature = shaderBytecode;
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
                            // stream out elements
                            var soElements = new List<SharpDX.Direct3D11.StreamOutputElement>();
                            foreach (var streamOutputElement in reflection.ShaderStreamOutputDeclarations)
                            {
                                var soElem = new SharpDX.Direct3D11.StreamOutputElement()
                                {
                                    Stream = streamOutputElement.Stream,
                                    SemanticIndex = streamOutputElement.SemanticIndex,
                                    SemanticName = streamOutputElement.SemanticName,
                                    StartComponent = streamOutputElement.StartComponent,
                                    ComponentCount = streamOutputElement.ComponentCount,
                                    OutputSlot = streamOutputElement.OutputSlot
                                };
                                soElements.Add(soElem);
                            }
                            // TODO GRAPHICS REFACTOR better cache
                            geometryShader = new SharpDX.Direct3D11.GeometryShader(GraphicsDevice.NativeDevice, shaderBytecode, soElements.ToArray(), reflection.StreamOutputStrides, reflection.StreamOutputRasterizedStream);
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

        // Small helper to cache SharpDX graphics objects
        private class GraphicsCache<TSource, TKey, TValue> : IDisposable where TValue : SharpDX.IUnknown
        {
            private object lockObject = new object();

            // Store instantiated objects
            private readonly Dictionary<TKey, TValue> storage = new Dictionary<TKey, TValue>();
            // Used for quick removal
            private readonly Dictionary<TValue, TKey> reverse = new Dictionary<TValue, TKey>();

            private readonly Dictionary<TValue, int> counter = new Dictionary<TValue, int>();

            private readonly Func<TSource, TKey> computeKey;
            private readonly Func<TSource, TValue> computeValue;

            public GraphicsCache(Func<TSource, TKey> computeKey, Func<TSource, TValue> computeValue)
            {
                this.computeKey = computeKey;
                this.computeValue = computeValue;
            }

            public TValue Instantiate(TSource source)
            {
                lock (lockObject)
                {
                    TValue value;
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

            public void Release(TValue value)
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
            return GraphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, typeof(DevicePipelineStateCache), device => new DevicePipelineStateCache(device));
        }

        // Caches
        private class DevicePipelineStateCache : IDisposable
        {
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.VertexShader> VertexShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.PixelShader> PixelShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.GeometryShader> GeometryShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.HullShader> HullShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.DomainShader> DomainShaderCache;
            public readonly GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.ComputeShader> ComputeShaderCache;
            public readonly GraphicsCache<BlendStateDescription, BlendStateDescription, SharpDX.Direct3D11.BlendState> BlendStateCache;
            public readonly GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, SharpDX.Direct3D11.RasterizerState> RasterizerStateCache;
            public readonly GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, SharpDX.Direct3D11.DepthStencilState> DepthStencilStateCache;

            public DevicePipelineStateCache(GraphicsDevice graphicsDevice)
            {
                // Shaders
                VertexShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.VertexShader>(source => source.Id, source => new SharpDX.Direct3D11.VertexShader(graphicsDevice.NativeDevice, source));
                PixelShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.PixelShader>(source => source.Id, source => new SharpDX.Direct3D11.PixelShader(graphicsDevice.NativeDevice, source));
                GeometryShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.GeometryShader>(source => source.Id, source => new SharpDX.Direct3D11.GeometryShader(graphicsDevice.NativeDevice, source));
                HullShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.HullShader>(source => source.Id, source => new SharpDX.Direct3D11.HullShader(graphicsDevice.NativeDevice, source));
                DomainShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.DomainShader>(source => source.Id, source => new SharpDX.Direct3D11.DomainShader(graphicsDevice.NativeDevice, source));
                ComputeShaderCache = new GraphicsCache<ShaderBytecode, ObjectId, SharpDX.Direct3D11.ComputeShader>(source => source.Id, source => new SharpDX.Direct3D11.ComputeShader(graphicsDevice.NativeDevice, source));

                // States
                BlendStateCache = new GraphicsCache<BlendStateDescription, BlendStateDescription, SharpDX.Direct3D11.BlendState>(source => source, source => new SharpDX.Direct3D11.BlendState(graphicsDevice.NativeDevice, CreateBlendState(source)));
                RasterizerStateCache = new GraphicsCache<RasterizerStateDescription, RasterizerStateDescription, SharpDX.Direct3D11.RasterizerState>(source => source, source => new SharpDX.Direct3D11.RasterizerState(graphicsDevice.NativeDevice, CreateRasterizerState(source)));
                DepthStencilStateCache = new GraphicsCache<DepthStencilStateDescription, DepthStencilStateDescription, SharpDX.Direct3D11.DepthStencilState>(source => source, source => new SharpDX.Direct3D11.DepthStencilState(graphicsDevice.NativeDevice, CreateDepthStencilState(source)));
            }

            private unsafe SharpDX.Direct3D11.BlendStateDescription CreateBlendState(BlendStateDescription description)
            {
                var nativeDescription = new SharpDX.Direct3D11.BlendStateDescription();

                nativeDescription.AlphaToCoverageEnable = description.AlphaToCoverageEnable;
                nativeDescription.IndependentBlendEnable = description.IndependentBlendEnable;

                var renderTargets = &description.RenderTarget0;
                for (int i = 0; i < 8; i++)
                {
                    ref var renderTarget = ref renderTargets[i];
                    ref var nativeRenderTarget = ref nativeDescription.RenderTarget[i];
                    nativeRenderTarget.IsBlendEnabled = renderTarget.BlendEnable;
                    nativeRenderTarget.SourceBlend = (SharpDX.Direct3D11.BlendOption)renderTarget.ColorSourceBlend;
                    nativeRenderTarget.DestinationBlend = (SharpDX.Direct3D11.BlendOption)renderTarget.ColorDestinationBlend;
                    nativeRenderTarget.BlendOperation = (SharpDX.Direct3D11.BlendOperation)renderTarget.ColorBlendFunction;
                    nativeRenderTarget.SourceAlphaBlend = (SharpDX.Direct3D11.BlendOption)renderTarget.AlphaSourceBlend;
                    nativeRenderTarget.DestinationAlphaBlend = (SharpDX.Direct3D11.BlendOption)renderTarget.AlphaDestinationBlend;
                    nativeRenderTarget.AlphaBlendOperation = (SharpDX.Direct3D11.BlendOperation)renderTarget.AlphaBlendFunction;
                    nativeRenderTarget.RenderTargetWriteMask = (SharpDX.Direct3D11.ColorWriteMaskFlags)renderTarget.ColorWriteChannels;
                }

                return nativeDescription;
            }

            private SharpDX.Direct3D11.RasterizerStateDescription CreateRasterizerState(RasterizerStateDescription description)
            {
                SharpDX.Direct3D11.RasterizerStateDescription nativeDescription;

                nativeDescription.CullMode = (SharpDX.Direct3D11.CullMode)description.CullMode;
                nativeDescription.FillMode = (SharpDX.Direct3D11.FillMode)description.FillMode;
                nativeDescription.IsFrontCounterClockwise = description.FrontFaceCounterClockwise;
                nativeDescription.DepthBias = description.DepthBias;
                nativeDescription.SlopeScaledDepthBias = description.SlopeScaleDepthBias;
                nativeDescription.DepthBiasClamp = description.DepthBiasClamp;
                nativeDescription.IsDepthClipEnabled = description.DepthClipEnable;
                nativeDescription.IsScissorEnabled = description.ScissorTestEnable;
                nativeDescription.IsMultisampleEnabled = description.MultisampleCount > MultisampleCount.None;
                nativeDescription.IsAntialiasedLineEnabled = description.MultisampleAntiAliasLine;

                return nativeDescription;
            }

            private SharpDX.Direct3D11.DepthStencilStateDescription CreateDepthStencilState(DepthStencilStateDescription description)
            {
                SharpDX.Direct3D11.DepthStencilStateDescription nativeDescription;

                nativeDescription.IsDepthEnabled = description.DepthBufferEnable;
                nativeDescription.DepthComparison = (SharpDX.Direct3D11.Comparison)description.DepthBufferFunction;
                nativeDescription.DepthWriteMask = description.DepthBufferWriteEnable ? SharpDX.Direct3D11.DepthWriteMask.All : SharpDX.Direct3D11.DepthWriteMask.Zero;

                nativeDescription.IsStencilEnabled = description.StencilEnable;
                nativeDescription.StencilReadMask = description.StencilMask;
                nativeDescription.StencilWriteMask = description.StencilWriteMask;

                nativeDescription.FrontFace.FailOperation = (SharpDX.Direct3D11.StencilOperation)description.FrontFace.StencilFail;
                nativeDescription.FrontFace.PassOperation = (SharpDX.Direct3D11.StencilOperation)description.FrontFace.StencilPass;
                nativeDescription.FrontFace.DepthFailOperation = (SharpDX.Direct3D11.StencilOperation)description.FrontFace.StencilDepthBufferFail;
                nativeDescription.FrontFace.Comparison = (SharpDX.Direct3D11.Comparison)description.FrontFace.StencilFunction;

                nativeDescription.BackFace.FailOperation = (SharpDX.Direct3D11.StencilOperation)description.BackFace.StencilFail;
                nativeDescription.BackFace.PassOperation = (SharpDX.Direct3D11.StencilOperation)description.BackFace.StencilPass;
                nativeDescription.BackFace.DepthFailOperation = (SharpDX.Direct3D11.StencilOperation)description.BackFace.StencilDepthBufferFail;
                nativeDescription.BackFace.Comparison = (SharpDX.Direct3D11.Comparison)description.BackFace.StencilFunction;

                return nativeDescription;
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
