// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Engine;

namespace Stride.Rendering.LightProbes
{
    public class LightProbeProcessor : EntityProcessor<LightProbeComponent>, IEntityComponentRenderProcessor
    {
        private bool needPositionUpdate = false;

        public LightProbeProcessor() : base(typeof(TransformComponent))
        {
        }

        /// <inheritdoc/>
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// Light probe runtime data is auto-computed when lightprobes are added/removed.  If you move them at runtime, please call this method.
        /// </summary>
        /// <remarks>
        /// This will also update coefficients.
        /// </remarks>
        public void UpdateLightProbePositions()
        {
            VisibilityGroup.Tags.Set(LightProbeRenderer.CurrentLightProbes, null);
            needPositionUpdate = false;

            // Initial load
            try
            {
                // Collect LightProbes
                var lightProbes = new FastList<LightProbeComponent>();

                foreach (var lightProbe in ComponentDatas)
                {
                    lightProbes.Add(lightProbe.Key);
                }

                // Need at least 4 light probes to form a tetrahedron
                if (lightProbes.Count < 4)
                    return;

                VisibilityGroup.Tags.Set(LightProbeRenderer.CurrentLightProbes, LightProbeGenerator.GenerateRuntimeData(lightProbes));
            }
            catch
            {
                // Allow failures
                // TODO: Log
            }
        }

        /// <summary>
        /// Updates only the coefficients of the light probes (from <see cref="LightProbeComponent.Coefficients"/> to <see cref="LightProbeRuntimeData.Coefficients"/>).
        /// </summary>
        public void UpdateLightProbeCoefficients()
        {
            var runtimeData = VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
            if (runtimeData == null)
                return;

            LightProbeGenerator.UpdateCoefficients(runtimeData);
        }

        public override void Draw(RenderContext context)
        {
            if (needPositionUpdate)
            {
                UpdateLightProbePositions();
            }
        }

        protected override void OnEntityComponentAdding(Entity entity, LightProbeComponent component, LightProbeComponent data)
        {
            needPositionUpdate = true;
        }

        protected override void OnEntityComponentRemoved(Entity entity, LightProbeComponent component, LightProbeComponent data)
        {
            needPositionUpdate = true;
        }
    }
}
