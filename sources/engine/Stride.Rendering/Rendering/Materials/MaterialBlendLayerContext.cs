// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Context used by a layer to store intermediate information (streams used, shading models...etc.)
    /// </summary>
    internal class MaterialBlendLayerContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="MaterialBlendLayerContext"/>.
        /// </summary>
        /// <param name="context">The material generator context</param>
        /// <param name="parentLayerContext">The parent layer context</param>
        /// <param name="blendMap">The blend map used for this layer</param>
        public MaterialBlendLayerContext(MaterialGeneratorContext context, MaterialBlendLayerContext parentLayerContext, IComputeScalar blendMap)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            Context = context;
            Parent = parentLayerContext;
            BlendMap = blendMap;

            Children = new List<MaterialBlendLayerContext>();
            ShadingModels = new MaterialShadingModelCollection();

            ContextPerStage = new Dictionary<MaterialShaderStage, MaterialBlendLayerPerStageContext>();
            foreach (MaterialShaderStage stage in Enum.GetValues(typeof(MaterialShaderStage)))
            {
                ContextPerStage[stage] = new MaterialBlendLayerPerStageContext();
            }

            PendingPixelLayerContext = new MaterialBlendLayerPerStageContext();
        }

        public MaterialGeneratorContext Context { get; }

        public MaterialBlendLayerContext Parent { get; }

        public IComputeScalar BlendMap { get; }

        public Dictionary<MaterialShaderStage, MaterialBlendLayerPerStageContext> ContextPerStage { get; }

        public List<MaterialBlendLayerContext> Children { get; }

        public MaterialShadingModelCollection ShadingModels { get; }

        internal MaterialBlendLayerPerStageContext PendingPixelLayerContext { get; }

        internal IComputeScalar BlendMapForShadingModel { get; set; }

        internal int ShadingModelCount { get; set; }

        public MaterialBlendLayerPerStageContext GetContextPerStage(MaterialShaderStage stage)
        {
            return ContextPerStage[stage];
        }

        public void SetStreamBlend(MaterialShaderStage stage, IComputeScalar blendMap)
        {
            SetStream(stage, MaterialBlendLayer.BlendStream, blendMap, MaterialKeys.BlendMap, MaterialKeys.BlendValue, null);
        }

        public void SetStream(MaterialShaderStage stage, string stream, IComputeNode computeNode, ObjectParameterKey<Texture> defaultTexturingKey, ParameterKey defaultValueKey, Color? defaultTextureValue)
        {
            if (defaultValueKey == null) throw new ArgumentNullException(nameof(defaultValueKey));
            if (computeNode == null)
            {
                return;
            }

            var streamType = MaterialStreamType.Float;
            if (defaultValueKey.PropertyType == typeof(Vector4) || defaultValueKey.PropertyType == typeof(Color4))
            {
                streamType = MaterialStreamType.Float4;
            }
            else if (defaultValueKey.PropertyType == typeof(Vector3) || defaultValueKey.PropertyType == typeof(Color3))
            {
                streamType = MaterialStreamType.Float3;
            }
            else if (defaultValueKey.PropertyType == typeof(Vector2) || defaultValueKey.PropertyType == typeof(Half2))
            {
                streamType = MaterialStreamType.Float2;
            }
            else if (defaultValueKey.PropertyType == typeof(float))
            {
                streamType = MaterialStreamType.Float;
            }
            else
            {
                throw new NotSupportedException("ParameterKey type [{0}] is not supported by SetStream".ToFormat(defaultValueKey.PropertyType));
            }

            var classSource = computeNode.GenerateShaderSource(Context, new MaterialComputeColorKeys(defaultTexturingKey, defaultValueKey, defaultTextureValue));
            SetStream(stage, stream, streamType, classSource);
        }

        public void SetStream(MaterialShaderStage stage, string stream, MaterialStreamType streamType, ShaderSource classSource)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            // Blend stream is not part of the stream used
            if (stream != MaterialBlendLayer.BlendStream)
            {
                GetContextPerStage(stage).Streams.Add(stream);
            }

            string channel;
            switch (streamType)
            {
                case MaterialStreamType.Float:
                    channel = "r";
                    break;
                case MaterialStreamType.Float2:
                    channel = "rg";
                    break;
                case MaterialStreamType.Float3:
                    channel = "rgb";
                    break;
                case MaterialStreamType.Float4:
                    channel = "rgba";
                    break;
                default:
                    throw new NotSupportedException("StreamType [{0}] is not supported".ToFormat(streamType));
            }

            var mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("MaterialSurfaceSetStreamFromComputeColor", stream, channel));
            mixin.AddComposition("computeColorSource", classSource);

            GetContextPerStage(stage).ShaderSources.Add(mixin);
        }

        public ShaderSource GenerateStreamInitializers(MaterialShaderStage stage)
        {
            // Early exit if nothing to do
            var stageContext = GetContextPerStage(stage);
            if (stageContext.StreamInitializers.Count == 0 && stageContext.ShaderSources.Count == 0 && stage != MaterialShaderStage.Pixel)
            {
                return null;
            }

            var mixin = new ShaderMixinSource();

            // the basic streams contained by every materials
            mixin.Mixins.Add(new ShaderClassSource("MaterialStream"));

            // the streams coming from the material layers
            foreach (var streamInitializer in stageContext.StreamInitializers)
            {
                mixin.Mixins.Add(new ShaderClassSource(streamInitializer));
            }
            stageContext.StreamInitializers.Clear();

            // the streams specific to a stage
            // TODO: Use StreamInitializers instead of streams initializers hardcoded in MaterialPixelShadingStream.ResetStream
            if (stage == MaterialShaderStage.Pixel)
                mixin.Mixins.Add(new ShaderClassSource("MaterialPixelShadingStream"));

            return mixin;
        }

        public ShaderSource ComputeShaderSource(MaterialShaderStage stage)
        {
            var stageContext = GetContextPerStage(stage);
            return stageContext.ComputeShaderSource();
        }
    }
}
