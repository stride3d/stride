// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Lights;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.LightProbes
{
    /// <summary>
    /// Light probe renderer.
    /// </summary>
    public class LightProbeRenderer : LightGroupRendererBase
    {
        /// <summary>
        /// Property key to access the current collection of <see cref="LightProbeRuntimeData"/> from <see cref="VisibilityGroup.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<LightProbeRuntimeData> CurrentLightProbes = new PropertyKey<LightProbeRuntimeData>("LightProbeRenderer.CurrentLightProbes", typeof(LightProbeRenderer));

        private LightProbeShaderGroupData lightprobeGroup;

        public override Type[] LightTypes { get; } = Type.EmptyTypes;

        public LightProbeRenderer()
        {
            IsEnvironmentLight = true;
        }

        public override void Initialize(RenderContext context)
        {
            base.Initialize(context);

            lightprobeGroup = new LightProbeShaderGroupData(context, this);
        }

        public override void Reset()
        {
            base.Reset();

            lightprobeGroup.Reset();
        }

        public override void SetViews(FastList<RenderView> views)
        {
            base.SetViews(views);

            lightprobeGroup.SetViews(views);
        }

        public override void ProcessLights(ProcessLightsParameters parameters)
        {
            lightprobeGroup.AddView(parameters.ViewIndex, parameters.View, parameters.LightIndices.Count);

            foreach (var index in parameters.LightIndices)
            {
                lightprobeGroup.AddLight(parameters.LightCollection[index], null);
            }

            // Consume all the lights
            parameters.LightIndices.Clear();
        }

        public override void UpdateShaderPermutationEntry(ForwardLightingRenderFeature.LightShaderPermutationEntry shaderEntry)
        {
            shaderEntry.EnvironmentLights.Add(lightprobeGroup);
        }

        private class LightProbeShaderGroupData : LightShaderGroupDynamic
        {
            private readonly LightProbeRenderer lightProbeRenderer;
            private readonly RenderContext renderContext;
            private readonly ShaderSource shaderSourceEnabled;
            private readonly ShaderSource shaderSourceDisabled;

            public LightProbeShaderGroupData(RenderContext renderContext, LightProbeRenderer lightProbeRenderer)
                : base(renderContext, null)
            {
                this.renderContext = renderContext;
                this.lightProbeRenderer = lightProbeRenderer;
                shaderSourceEnabled = new ShaderClassSource("LightProbeShader", 3);
                shaderSourceDisabled = new ShaderClassSource("EnvironmentLight");
            }

            public override void UpdateLayout(string compositionName)
            {
                base.UpdateLayout(compositionName);

                // Setup light probe shader only if there is some light probe data
                // TODO: Just like the ForwardLightingRenderFeature access the LightProcessor, accessing the SceneInstance.LightProbeProcessor is not what we want.
                // Ideally, we should send the data the other way around. Let's fix that together when we refactor the lighting at some point.
                var lightProbeRuntimeData = renderContext.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
                ShaderSource = lightProbeRuntimeData != null ? shaderSourceEnabled : shaderSourceDisabled;
            }
        }
    }
}
