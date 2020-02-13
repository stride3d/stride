// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Skyboxes;
using Xenko.Shaders;
using Xenko.Rendering.Voxels;
using Xenko.Rendering.Shadows;
using Xenko.Rendering.Lights;
using Xenko.Engine.Processors;

namespace Xenko.Rendering.Voxels.VoxelGI
{
    /// <summary>
    /// Light renderer for <see cref="LightVoxel"/>.
    /// </summary>
    public class LightVoxelRenderer : LightGroupRendererBase
    {
        private readonly Dictionary<RenderLight, LightVoxelShaderGroup> lightShaderGroupsPerVoxel = new Dictionary<RenderLight, LightVoxelShaderGroup>();
        private PoolListStruct<LightVoxelShaderGroup> pool = new PoolListStruct<LightVoxelShaderGroup>(8, CreateLightVoxelShaderGroup);

        public override Type[] LightTypes { get; } = { typeof(LightVoxel) };

        public LightVoxelRenderer()
        {
            IsEnvironmentLight = true;
        }


        public override void Reset()
        {
            base.Reset();

            foreach (var lightShaderGroup in lightShaderGroupsPerVoxel)
                lightShaderGroup.Value.Reset();

            lightShaderGroupsPerVoxel.Clear();
            pool.Reset();
        }

        /// <inheritdoc/>
        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            foreach (var index in parameters.LightIndices)
            {
                // For now, we allow only one cubemap at once
                var light = parameters.LightCollection[index];

                // Prepare LightVoxelShaderGroup
                LightVoxelShaderGroup lightShaderGroup;
                if (!lightShaderGroupsPerVoxel.TryGetValue(light, out lightShaderGroup))
                {
                    lightShaderGroup = pool.Add();
                    lightShaderGroup.Light = light;

                    lightShaderGroupsPerVoxel.Add(light, lightShaderGroup);
                }
            }

            // Consume all the lights
            parameters.LightIndices.Clear();
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            foreach (var cubemap in lightShaderGroupsPerVoxel)
            {
                shaderEntry.EnvironmentLights.Add(cubemap.Value);
            }
        }

        private static LightVoxelShaderGroup CreateLightVoxelShaderGroup()
        {
            return new LightVoxelShaderGroup(new ShaderMixinGeneratorSource("LightVoxelEffect"));
        }

        private class LightVoxelShaderGroup : LightShaderGroup
        {
            private ValueParameterKey<float> intensityKey;
            private ValueParameterKey<float> specularIntensityKey;

            private PermutationParameterKey<ShaderSource> diffuseMarcherKey;
            private PermutationParameterKey<ShaderSource> specularMarcherKey;
            private PermutationParameterKey<ShaderSourceCollection> attributeSamplersKey;

            public RenderLight Light { get; set; }

            VoxelAttribute traceAttribute = null;

            public LightVoxelShaderGroup(ShaderSource mixin) : base(mixin)
            {
                HasEffectPermutations = true;
                traceAttribute = null;
            }

            ProcessedVoxelVolume GetProcessedVolume()
            {
                var lightVoxel = ((LightVoxel)Light.Type);
                if (lightVoxel.Volume == null)
                {
                    throw new ArgumentNullException("No Voxel Volume Component selected for voxel light.");
                }
                var voxelVolumeProcessor = lightVoxel.Volume.Entity.EntityManager.GetProcessor<VoxelVolumeProcessor>();
                if (voxelVolumeProcessor == null)
                    return null;

                ProcessedVoxelVolume processedVolume = voxelVolumeProcessor.GetProcessedVolumeForComponent(lightVoxel.Volume);
                return processedVolume;
            }

            VoxelAttribute GetTraceAttr()
            {
                var lightVoxel = ((LightVoxel)Light.Type);

                ProcessedVoxelVolume processedVolume = GetProcessedVolume();
                if (processedVolume == null)
                    return null;

                if (processedVolume.OutputAttributes.Count > lightVoxel.AttributeIndex)
                {
                    return processedVolume.OutputAttributes[lightVoxel.AttributeIndex];
                }
                else
                {
                    throw new ArgumentOutOfRangeException("Tried to access attribute index " + lightVoxel.AttributeIndex.ToString() + " (zero-indexed) when the Voxel Volume Component has only " + processedVolume.OutputAttributes.Count.ToString() + " attributes.");
                }
            }
            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);

                traceAttribute = GetTraceAttr();

                intensityKey = LightVoxelShaderKeys.Intensity.ComposeWith(compositionName);
                specularIntensityKey = LightVoxelShaderKeys.SpecularIntensity.ComposeWith(compositionName);

                diffuseMarcherKey = LightVoxelShaderKeys.diffuseMarcher.ComposeWith(compositionName);
                specularMarcherKey = LightVoxelShaderKeys.specularMarcher.ComposeWith(compositionName);
                attributeSamplersKey = MarchAttributesKeys.AttributeSamplers.ComposeWith(compositionName);

                if (traceAttribute != null)
                {
                    if (((LightVoxel)Light.Type).DiffuseMarcher != null)
                        ((LightVoxel)Light.Type).DiffuseMarcher.UpdateMarchingLayout("diffuseMarcher." + compositionName);
                    if (((LightVoxel)Light.Type).SpecularMarcher != null)
                        ((LightVoxel)Light.Type).SpecularMarcher.UpdateMarchingLayout("specularMarcher." + compositionName);
                    traceAttribute.UpdateSamplingLayout("AttributeSamplers[0]." + compositionName);
                }
            }

            public override void ApplyEffectPermutations(RenderEffect renderEffect)
            {
                if (traceAttribute != null)
                {
                    ShaderSourceCollection collection = new ShaderSourceCollection
                    {
                        traceAttribute.GetSamplingShader()
                    };
                    renderEffect.EffectValidator.ValidateParameter(attributeSamplersKey, collection);

                    if (((LightVoxel)Light.Type).DiffuseMarcher != null)
                        renderEffect.EffectValidator.ValidateParameter(diffuseMarcherKey, ((LightVoxel)Light.Type).DiffuseMarcher.GetMarchingShader(0));
                    if (((LightVoxel)Light.Type).SpecularMarcher != null)
                        renderEffect.EffectValidator.ValidateParameter(specularMarcherKey, ((LightVoxel)Light.Type).SpecularMarcher.GetMarchingShader(0));
                }
            }

            public override void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
            {
                base.ApplyViewParameters(context, viewIndex, parameters);

                var lightVoxel = ((LightVoxel)Light.Type);

                if (lightVoxel.Volume == null)
                    return;
                ProcessedVoxelVolume processedVolume = GetProcessedVolume();
                if (processedVolume == null)
                    return;

                var intensity = Light.Intensity;
                var intensityBounceScale = lightVoxel.BounceIntensityScale;
                var specularIntensity = lightVoxel.SpecularIntensityScale * intensity;

                VoxelViewContext viewContext = new VoxelViewContext(processedVolume.passList, viewIndex);
                if (viewContext.IsVoxelView)
                {
                    intensity *= intensityBounceScale / 3.141592f;
                    specularIntensity = 0.0f;
                }

                parameters.Set(intensityKey, intensity);
                parameters.Set(specularIntensityKey, specularIntensity);

                if (traceAttribute != null)
                {
                    lightVoxel.DiffuseMarcher?.ApplyMarchingParameters(parameters);
                    lightVoxel.SpecularMarcher?.ApplyMarchingParameters(parameters);
                    traceAttribute.ApplySamplingParameters(viewContext, parameters);
                }
            }
        }
    }
}

