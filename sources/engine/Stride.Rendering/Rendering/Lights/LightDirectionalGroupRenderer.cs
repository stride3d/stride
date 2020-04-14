// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Shadows;
using Stride.Shaders;

namespace Stride.Rendering.Lights
{
#pragma warning disable 169
#pragma warning restore 169
    /// <summary>
    /// Light renderer for <see cref="LightDirectional"/>.
    /// </summary>
    public class LightDirectionalGroupRenderer : LightGroupRendererShadow
    {
        public override Type[] LightTypes { get; } = { typeof(LightDirectional) };

        public override void Initialize(RenderContext context)
        {
        }

        public override LightShaderGroupDynamic CreateLightShaderGroup(RenderDrawContext context,
                                                                       ILightShadowMapShaderGroupData shadowShaderGroupData)
        {
            return new DirectionalLightShaderGroup(context.RenderContext, shadowShaderGroupData);
        }

        private class DirectionalLightShaderGroup : LightShaderGroupDynamic
        {
            private ValueParameterKey<int> countKey;
            private ValueParameterKey<DirectionalLightData> lightsKey;
            private FastListStruct<DirectionalLightData> lightsData = new FastListStruct<DirectionalLightData>(8);

            public DirectionalLightShaderGroup(RenderContext renderContext, ILightShadowMapShaderGroupData shadowGroupData)
                : base(renderContext, shadowGroupData)
            {
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);
                countKey = DirectLightGroupPerViewKeys.LightCount.ComposeWith(compositionName);
                lightsKey = LightDirectionalGroupKeys.Lights.ComposeWith(compositionName);
            }

            protected override void UpdateLightCount()
            {
                base.UpdateLightCount();

                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("LightDirectionalGroup", LightCurrentCount));
                // Old fixed path kept in case we need it again later
                //mixin.Mixins.Add(new ShaderClassSource("LightDirectionalGroup", LightCurrentCount));
                //mixin.Mixins.Add(new ShaderClassSource("DirectLightGroupFixed", LightCurrentCount));
                ShadowGroup?.ApplyShader(mixin);

                ShaderSource = mixin;
            }

            public override void ApplyViewParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters)
            {
                currentLights.Clear();
                var lightRange = lightRanges[viewIndex];
                for (int i = lightRange.Start; i < lightRange.End; ++i)
                    currentLights.Add(lights[i]);

                base.ApplyViewParameters(context, viewIndex, parameters);

                foreach (var lightEntry in currentLights)
                {
                    var light = lightEntry.Light;
                    lightsData.Add(new DirectionalLightData
                    {
                        DirectionWS = light.Direction,
                        Color = light.Color,
                    });
                }

                parameters.Set(countKey, lightsData.Count);
                parameters.Set(lightsKey, lightsData.Count, ref lightsData.Items[0]);
                lightsData.Clear();
            }
        }
    }
}
