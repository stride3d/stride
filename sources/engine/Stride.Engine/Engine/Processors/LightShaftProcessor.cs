// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Rendering;
using Xenko.Rendering.Images;
using Xenko.Rendering.Lights;

namespace Xenko.Engine.Processors
{
    public class LightShaftProcessor : EntityProcessor<LightShaftComponent, LightShaftProcessor.AssociatedData>, IEntityComponentRenderProcessor
    {
        private readonly List<RenderLightShaft> activeLightShafts = new List<RenderLightShaft>();

        /// <inheritdoc/>
        public VisibilityGroup VisibilityGroup { get; set; }

        protected internal override void OnSystemAdd()
        {
            base.OnSystemAdd();

            VisibilityGroup.Tags.Set(LightShafts.CurrentLightShafts, activeLightShafts);
        }

        protected internal override void OnSystemRemove()
        {
            VisibilityGroup.Tags.Set(LightShafts.CurrentLightShafts, null);

            base.OnSystemRemove();
        }

        /// <inheritdoc />
        protected override AssociatedData GenerateComponentData(Entity entity, LightShaftComponent component)
        {
            return new AssociatedData
            {
                Component = component,
                LightComponent = entity.Get<LightComponent>(),
            };
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, LightShaftComponent component, AssociatedData associatedData)
        {
            return component == associatedData.Component &&
                   entity.Get<LightComponent>() == associatedData.LightComponent;
        }

        /// <inheritdoc />
        public override void Update(GameTime time)
        {
            activeLightShafts.Clear();

            // Get processors
            var lightProcessor = EntityManager.GetProcessor<LightProcessor>();
            if (lightProcessor == null)
                return;

            var lightShaftBoundingVolumeProcessor = EntityManager.GetProcessor<LightShaftBoundingVolumeProcessor>();
            if (lightShaftBoundingVolumeProcessor == null)
                return;

            foreach (var pair in ComponentDatas)
            {
                if (!pair.Key.Enabled)
                    continue;

                var lightShaft = pair.Value;
                if (lightShaft.LightComponent == null)
                    continue;

                var light = lightProcessor.GetRenderLight(lightShaft.LightComponent);
                if (light == null)
                    continue;

                var directLight = light.Type as IDirectLight;
                if (directLight == null)
                    continue;

                var boundingVolumes = lightShaftBoundingVolumeProcessor.GetBoundingVolumesForComponent(lightShaft.Component);
                if (boundingVolumes == null)
                    continue;

                activeLightShafts.Add(new RenderLightShaft
                {
                    Light = light,
                    Light2 = directLight,
                    SampleCount = lightShaft.Component.SampleCount,
                    DensityFactor = lightShaft.Component.DensityFactor,
                    BoundingVolumes = boundingVolumes,
                    SeparateBoundingVolumes = lightShaft.Component.SeparateBoundingVolumes,
                });
            }
        }

        public class AssociatedData
        {
            public LightShaftComponent Component;
            public LightComponent LightComponent;
        }
    }
}
