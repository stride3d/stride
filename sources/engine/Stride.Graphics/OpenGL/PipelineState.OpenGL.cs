// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Storage;
using Stride.Shaders;
using Stride.Core.Extensions;
#if STRIDE_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
using PrimitiveTypeGl = OpenTK.Graphics.ES30.PrimitiveType;
#else
using OpenTK.Graphics.OpenGL;
using PrimitiveTypeGl = OpenTK.Graphics.OpenGL.PrimitiveType;
#endif

namespace Stride.Graphics
{
    public partial class PipelineState
    {
        internal readonly BlendState BlendState;
        internal readonly DepthStencilState DepthStencilState;

        internal readonly RasterizerState RasterizerState;

        internal readonly EffectProgram EffectProgram;

        internal readonly PrimitiveTypeGl PrimitiveType;
        internal readonly VertexAttrib[] VertexAttribs;
        internal ResourceBinder ResourceBinder;

        private PipelineState(GraphicsDevice graphicsDevice, PipelineStateDescription pipelineStateDescription) : base(graphicsDevice)
        {
            // First time, build caches
            var pipelineStateCache = GetPipelineStateCache();

            var depthClampEmulation = !pipelineStateDescription.RasterizerState.DepthClipEnable && !graphicsDevice.HasDepthClamp;

            // Store states
            BlendState = new BlendState(pipelineStateDescription.BlendState, pipelineStateDescription.Output.RenderTargetCount > 0);
            RasterizerState = new RasterizerState(pipelineStateDescription.RasterizerState);
            DepthStencilState = new DepthStencilState(pipelineStateDescription.DepthStencilState, pipelineStateDescription.Output.DepthStencilFormat != PixelFormat.None);

            PrimitiveType = pipelineStateDescription.PrimitiveType.ToOpenGL();

            // Compile effect
            var effectBytecode = pipelineStateDescription.EffectBytecode;
            EffectProgram = effectBytecode != null ? pipelineStateCache.EffectProgramCache.Instantiate(Tuple.Create(effectBytecode, depthClampEmulation)) : null;

            var rootSignature = pipelineStateDescription.RootSignature;
            if (rootSignature != null && effectBytecode != null)
                ResourceBinder.Compile(graphicsDevice, rootSignature.EffectDescriptorSetReflection, effectBytecode);

            // Vertex attributes
            if (pipelineStateDescription.InputElements != null)
            {
                var vertexAttribs = new List<VertexAttrib>();
                foreach (var inputElement in pipelineStateDescription.InputElements)
                {
                    // Query attribute name from effect
                    var attributeName = "a_" + inputElement.SemanticName + inputElement.SemanticIndex;
                    int attributeIndex;
                    if (!EffectProgram.Attributes.TryGetValue(attributeName, out attributeIndex))
                        continue;

                    var vertexElementFormat = VertexAttrib.ConvertVertexElementFormat(inputElement.Format);
                    vertexAttribs.Add(new VertexAttrib(
                        inputElement.InputSlot,
                        attributeIndex,
                        vertexElementFormat.Size,
                        vertexElementFormat.Type,
                        vertexElementFormat.Normalized,
                        inputElement.AlignedByteOffset));
                }

                VertexAttribs = pipelineStateCache.VertexAttribsCache.Instantiate(vertexAttribs.ToArray());
            }
        }

        internal void Apply(CommandList commandList, PipelineState previousPipeline)
        {
            // Apply states
            if (BlendState != previousPipeline.BlendState || commandList.NewBlendFactor != commandList.BoundBlendFactor)
                BlendState.Apply(commandList, previousPipeline.BlendState);
            if (RasterizerState != previousPipeline.RasterizerState)
                RasterizerState.Apply(commandList);
            if (DepthStencilState != previousPipeline.DepthStencilState || commandList.NewStencilReference != commandList.BoundStencilReference)
                DepthStencilState.Apply(commandList);
        }

        protected internal override void OnDestroyed()
        {
            var pipelineStateCache = GetPipelineStateCache();

            if (EffectProgram != null)
                pipelineStateCache.EffectProgramCache.Release(EffectProgram);
            if (VertexAttribs != null)
                pipelineStateCache.VertexAttribsCache.Release(VertexAttribs);

            base.OnDestroyed();
        }

        struct VertexAttribsKey : IEquatable<VertexAttribsKey>
        {
            public VertexAttrib[] Attribs;
            public int Hash;

            public VertexAttribsKey(VertexAttrib[] attribs)
            {
                Attribs = attribs;
                Hash = ArrayExtensions.ComputeHash(attribs);
            }

            public bool Equals(VertexAttribsKey other)
            {
                return Hash == other.Hash && ArrayExtensions.ArraysEqual(Attribs, other.Attribs);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is VertexAttribsKey && Equals((VertexAttribsKey)obj);
            }

            public override int GetHashCode()
            {
                return Hash;
            }

            public static bool operator ==(VertexAttribsKey left, VertexAttribsKey right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(VertexAttribsKey left, VertexAttribsKey right)
            {
                return !left.Equals(right);
            }
        }

        // Small helper to cache SharpDX graphics objects
        class GraphicsCache<TSource, TKey, TValue>
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

                        var graphicsResource = value as IReferencable;
                        graphicsResource?.Release();
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
                        var graphicsResource = entry.Key as IReferencable;
                        graphicsResource?.Release();
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
            public readonly GraphicsCache<Tuple<EffectBytecode, bool>, EffectBytecode, EffectProgram> EffectProgramCache;
            public readonly GraphicsCache<VertexAttrib[], VertexAttribsKey, VertexAttrib[]> VertexAttribsCache;

            public DevicePipelineStateCache(GraphicsDevice graphicsDevice)
            {
                EffectProgramCache = new GraphicsCache<Tuple<EffectBytecode, bool>, EffectBytecode, EffectProgram>(source => source.Item1, source => new EffectProgram(graphicsDevice, source.Item1, source.Item2));
                VertexAttribsCache = new GraphicsCache<VertexAttrib[], VertexAttribsKey, VertexAttrib[]>(source => new VertexAttribsKey(source), source => source);
            }

            public void Dispose()
            {
                EffectProgramCache.Dispose();
                VertexAttribsCache.Dispose();
            }
        }
    }
}
#endif
