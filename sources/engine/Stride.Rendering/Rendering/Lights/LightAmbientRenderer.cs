// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightAmbient"/>.
    /// </summary>
    public class LightAmbientRenderer : LightGroupRendererBase
    {
        private LightAmbientShaderGroup lightShaderGroup = new LightAmbientShaderGroup();

        public override Type[] LightTypes { get; } = { typeof(LightAmbient) };

        public LightAmbientRenderer()
        {
            IsEnvironmentLight = true;
        }

        public override void Reset()
        {
            base.Reset();

            lightShaderGroup.Reset();
        }

        public override void SetViews(FastList<RenderView> views)
        {
            base.SetViews(views);

            // Make sure array is big enough for all render views
            Array.Resize(ref lightShaderGroup.AmbientColor, views.Count);
        }

        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            // Sum contribution from all lights
            var ambientColor = new Color3();
            foreach (var index in parameters.LightIndices)
            {
                var light = parameters.LightCollection[index];
                ambientColor += light.Color;
            }

            // Consume all the lights
            parameters.LightIndices.Clear();

            // Store ambient sum for this view
            lightShaderGroup.AmbientColor[parameters.ViewIndex] = ambientColor;
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            // Always merge ambient lighting code to avoid shader permutations
            shaderEntry.EnvironmentLights.Add(lightShaderGroup);
        }

        private class LightAmbientShaderGroup : LightShaderGroup
        {
            internal Color3[] AmbientColor;

            private ValueParameterKey<Color3> ambientLightKey;
            public LightAmbientShaderGroup()
                : base(new ShaderClassSource("LightSimpleAmbient"))
            {
            }

            public override void Reset()
            {
                base.Reset();
                if (AmbientColor != null)
                {
                    for (int i = 0; i < AmbientColor.Length; i++)
                    {
                        AmbientColor[i] = new Color3(0.0f);
                    }
                }
            }

            public override void UpdateLayout(string compositionName)
            {
                ambientLightKey = LightSimpleAmbientKeys.AmbientLight.ComposeWith(compositionName);
            }

            public override void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
            {
                base.ApplyViewParameters(context, viewIndex, parameters);

                parameters.Set(ambientLightKey, ref AmbientColor[viewIndex]);
            }
        }
    }
}
