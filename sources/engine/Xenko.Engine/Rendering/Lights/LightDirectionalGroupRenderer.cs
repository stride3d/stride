// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    public struct DirectionalLightData
    {
#pragma warning disable 169
        public Vector3 DirectionWS;
        private float padding0;
        public Color3 Color;
        private float padding1;
#pragma warning restore 169
    }

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

        class DirectionalLightShaderGroup : LightShaderGroupDynamic
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
                CurrentLights.Clear();
                var lightRange = LightRanges[viewIndex];
                for (int i = lightRange.Start; i < lightRange.End; ++i)
                    CurrentLights.Add(Lights[i]);

                base.ApplyViewParameters(context, viewIndex, parameters);

                foreach (var lightEntry in CurrentLights)
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
