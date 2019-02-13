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

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightSkybox"/>.
    /// </summary>
    public class LightSkyboxRenderer : LightGroupRendererBase
    {
        private readonly Dictionary<RenderLight, LightSkyBoxShaderGroup> lightShaderGroupsPerSkybox = new Dictionary<RenderLight, LightSkyBoxShaderGroup>();
        private PoolListStruct<LightSkyBoxShaderGroup> pool = new PoolListStruct<LightSkyBoxShaderGroup>(8, CreateLightSkyBoxShaderGroup);

        public override Type[] LightTypes { get; } = { typeof(LightSkybox) };

        public LightSkyboxRenderer()
        {
            IsEnvironmentLight = true;
        }

        /// <param name="viewCount"></param>
        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();

            foreach (var lightShaderGroup in lightShaderGroupsPerSkybox)
                lightShaderGroup.Value.Reset();

            lightShaderGroupsPerSkybox.Clear();
            pool.Reset();
        }

        /// <inheritdoc/>
        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            foreach (var index in parameters.LightIndices)
            {
                // For now, we allow only one cubemap at once
                var light = parameters.LightCollection[index];

                // Prepare LightSkyBoxShaderGroup
                LightSkyBoxShaderGroup lightShaderGroup;
                if (!lightShaderGroupsPerSkybox.TryGetValue(light, out lightShaderGroup))
                {
                    lightShaderGroup = pool.Add();
                    lightShaderGroup.Light = light;

                    lightShaderGroupsPerSkybox.Add(light, lightShaderGroup);
                }
            }

            // Consume all the lights
            parameters.LightIndices.Clear();
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            // TODO: Some kind of sort?

            foreach (var cubemap in lightShaderGroupsPerSkybox)
            {
                shaderEntry.EnvironmentLights.Add(cubemap.Value);
            }
        }

        private static LightSkyBoxShaderGroup CreateLightSkyBoxShaderGroup()
        {
            return new LightSkyBoxShaderGroup(new ShaderMixinGeneratorSource("LightSkyboxEffect"));
        }

        private class LightSkyBoxShaderGroup : LightShaderGroup
        {
            private static readonly ShaderClassSource EmptyComputeEnvironmentColorSource = new ShaderClassSource("IComputeEnvironmentColor");

            private ValueParameterKey<float> intensityKey;
            private ValueParameterKey<Matrix> skyMatrixKey;
            private PermutationParameterKey<ShaderSource> lightDiffuseColorKey;
            private PermutationParameterKey<ShaderSource> lightSpecularColorKey;
            private ValueParameterKey<Color3> sphericalColorsKey;
            private ObjectParameterKey<Texture> specularCubeMapkey;
            private ValueParameterKey<float> specularMipCountKey;

            public RenderLight Light { get; set; }

            public LightSkyBoxShaderGroup(ShaderSource mixin) : base(mixin)
            {
                HasEffectPermutations = true;
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);

                intensityKey = LightSkyboxShaderKeys.Intensity.ComposeWith(compositionName);
                skyMatrixKey = LightSkyboxShaderKeys.SkyMatrix.ComposeWith(compositionName);
                lightDiffuseColorKey = LightSkyboxShaderKeys.LightDiffuseColor.ComposeWith(compositionName);
                lightSpecularColorKey = LightSkyboxShaderKeys.LightSpecularColor.ComposeWith(compositionName);

                sphericalColorsKey = SphericalHarmonicsEnvironmentColorKeys.SphericalColors.ComposeWith("lightDiffuseColor." + compositionName);
                specularCubeMapkey = RoughnessCubeMapEnvironmentColorKeys.CubeMap.ComposeWith("lightSpecularColor." + compositionName);
                specularMipCountKey = RoughnessCubeMapEnvironmentColorKeys.MipCount.ComposeWith("lightSpecularColor." + compositionName);
            }

            public override void ApplyEffectPermutations(RenderEffect renderEffect)
            {
                var lightSkybox = (LightSkybox)Light.Type;
                var skybox = lightSkybox.Skybox;

                var diffuseParameters = skybox.DiffuseLightingParameters;
                var specularParameters = skybox.SpecularLightingParameters;

                var lightDiffuseColorShader = diffuseParameters.Get(SkyboxKeys.Shader) ?? EmptyComputeEnvironmentColorSource;
                var lightSpecularColorShader = specularParameters.Get(SkyboxKeys.Shader) ?? EmptyComputeEnvironmentColorSource;

                renderEffect.EffectValidator.ValidateParameter(lightDiffuseColorKey, lightDiffuseColorShader);
                renderEffect.EffectValidator.ValidateParameter(lightSpecularColorKey, lightSpecularColorShader);
            }

            public override void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
            {
                base.ApplyViewParameters(context, viewIndex, parameters);

                var lightSkybox = ((LightSkybox)Light.Type);
                var skybox = lightSkybox.Skybox;

                var intensity = Light.Intensity;

                var skyMatrix = Matrix.Invert(Matrix.RotationQuaternion(lightSkybox.Rotation));

                var diffuseParameters = skybox.DiffuseLightingParameters;
                var specularParameters = skybox.SpecularLightingParameters;

                var specularCubemap = specularParameters.Get(SkyboxKeys.CubeMap);
                int specularCubemapLevels = 0;
                if (specularCubemap != null)
                {
                    specularCubemapLevels = specularCubemap.MipLevels;
                }
                var sphericalColors = diffuseParameters.GetValues(SphericalHarmonicsEnvironmentColorKeys.SphericalColors);

                // global parameters
                parameters.Set(intensityKey, intensity);
                parameters.Set(skyMatrixKey, skyMatrix);

                // This need to be working with new system
                parameters.Set(sphericalColorsKey, sphericalColors);
                parameters.Set(specularCubeMapkey, specularCubemap);
                parameters.Set(specularMipCountKey, specularCubemapLevels);
            }
        }
    }
}
