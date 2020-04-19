// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace Stride.Engine.Processors
{
    public class LightShaftBoundingVolumeProcessor : EntityProcessor<LightShaftBoundingVolumeComponent>
    {
        private Dictionary<LightShaftComponent, List<RenderLightShaftBoundingVolume>> volumesPerLightShaft = new Dictionary<LightShaftComponent, List<RenderLightShaftBoundingVolume>>();
        private bool isDirty;

        public override void Update(GameTime time)
        {
            RegenerateVolumesPerLightShaft();
        }

        public IReadOnlyList<RenderLightShaftBoundingVolume> GetBoundingVolumesForComponent(LightShaftComponent component)
        {
            if (!volumesPerLightShaft.TryGetValue(component, out var data))
                return null;
            return data;
        }

        protected override void OnEntityComponentAdding(Entity entity, LightShaftBoundingVolumeComponent component, LightShaftBoundingVolumeComponent data)
        {
            component.LightShaftChanged += ComponentOnLightShaftChanged;
            component.ModelChanged += ComponentOnModelChanged;
            component.EnabledChanged += ComponentOnEnabledChanged;
            isDirty = true;
        }

        protected override void OnEntityComponentRemoved(Entity entity, LightShaftBoundingVolumeComponent component, LightShaftBoundingVolumeComponent data)
        {
            component.LightShaftChanged -= ComponentOnLightShaftChanged;
            component.ModelChanged -= ComponentOnModelChanged;
            component.EnabledChanged -= ComponentOnEnabledChanged;
            isDirty = true;
        }

        private void ComponentOnEnabledChanged(object sender, EventArgs eventArgs)
        {
            isDirty = true;
        }

        private void ComponentOnModelChanged(object sender, EventArgs eventArgs)
        {
            isDirty = true;
        }

        private void ComponentOnLightShaftChanged(object sender, EventArgs eventArgs)
        {
            isDirty = true;
        }

        private void RegenerateVolumesPerLightShaft()
        {
            // Clear
            if (isDirty)
            {
                volumesPerLightShaft.Clear();
            }
            // Keep existing collections
            else
            {
                foreach (var lightShaft in volumesPerLightShaft)
                {
                    lightShaft.Value.Clear();
                }
            }

            foreach (var pair in ComponentDatas)
            {
                if (!pair.Key.Enabled)
                    continue;

                var lightShaft = pair.Key.LightShaft;
                if (lightShaft == null)
                    continue;

                List<RenderLightShaftBoundingVolume> data;
                if (!volumesPerLightShaft.TryGetValue(lightShaft, out data))
                    volumesPerLightShaft.Add(lightShaft, data = new List<RenderLightShaftBoundingVolume>());

                data.Add(new RenderLightShaftBoundingVolume
                {
                    World = pair.Key.Entity.Transform.WorldMatrix,
                    Model = pair.Key.Model,
                });
            }

            isDirty = false;
        }
    }
}
