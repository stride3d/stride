// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Shadows;
using Xenko.Shaders;

namespace Xenko.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightPoint"/>.
    /// </summary>
    public class LightPointGroupRenderer : LightGroupRendererShadow
    {
        public override Type[] LightTypes { get; } = { typeof(LightPoint) };

        public override LightShaderGroupDynamic CreateLightShaderGroup(RenderDrawContext context,
                                                                       ILightShadowMapShaderGroupData shadowShaderGroupData)
        {
            return new PointLightShaderGroup(context.RenderContext, shadowShaderGroupData);
        }

        private class PointLightShaderGroup : LightShaderGroupDynamic
        {
            private ValueParameterKey<int> countKey;
            private ValueParameterKey<PointLightData> lightsKey;
            private FastListStruct<PointLightData> lightsData = new FastListStruct<PointLightData>(8);
            private readonly object applyLock = new object();

            public PointLightShaderGroup(RenderContext renderContext, ILightShadowMapShaderGroupData shadowGroupData)
                : base(renderContext, shadowGroupData)
            {
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);

                countKey = DirectLightGroupPerDrawKeys.LightCount.ComposeWith(compositionName);
                lightsKey = LightPointGroupKeys.Lights.ComposeWith(compositionName);
            }

            protected override void UpdateLightCount()
            {
                base.UpdateLightCount();

                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource("LightPointGroup", LightCurrentCount));
                // Old fixed path kept in case we need it again later
                //mixin.Mixins.Add(new ShaderClassSource("LightPointGroup", LightCurrentCount));
                //mixin.Mixins.Add(new ShaderClassSource("DirectLightGroupFixed", LightCurrentCount));
                ShadowGroup?.ApplyShader(mixin);

                ShaderSource = mixin;
            }

            /// <inheritdoc/>
            public override int AddView(int viewIndex, RenderView renderView, int lightCount)
            {
                base.AddView(viewIndex, renderView, lightCount);

                // We allow more lights than LightCurrentCount (they will be culled)
                return lightCount;
            }

            public override void ApplyDrawParameters(RenderDrawContext context, int viewIndex, ParameterCollection parameters, ref BoundingBoxExt boundingBox)
            {
                // TODO THREADING: Make CurrentLights and lightData (thread-) local
                lock (applyLock)
                {
                    currentLights.Clear();
                    var lightRange = lightRanges[viewIndex];
                    for (int i = lightRange.Start; i < lightRange.End; ++i)
                        currentLights.Add(lights[i]);

                    base.ApplyDrawParameters(context, viewIndex, parameters, ref boundingBox);

                    // TODO: Since we cull per object, we could maintain a higher number of allowed light than the shader support (i.e. 4 lights active per object even though the scene has many more of them)
                    // TODO: Octree structure to select best lights quicker
                    var boundingBox2 = (BoundingBox)boundingBox;
                    foreach (var lightEntry in currentLights)
                    {
                        var light = lightEntry.Light;

                        if (light.BoundingBox.Intersects(ref boundingBox2))
                        {
                            var pointLight = (LightPoint)light.Type;
                            lightsData.Add(new PointLightData
                            {
                                PositionWS = light.Position,
                                InvSquareRadius = pointLight.InvSquareRadius,
                                Color = light.Color,
                            });

                            // Did we reach max number of simultaneous lights?
                            // TODO: Still collect everything but sort by importance and remove the rest?
                            if (lightsData.Count >= LightCurrentCount)
                                break;
                        }
                    }

                    parameters.Set(countKey, lightsData.Count);
                    parameters.Set(lightsKey, lightsData.Count, ref lightsData.Items[0]);
                    lightsData.Clear();
                }
            }
        }
    }
}
