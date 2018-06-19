// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Rendering;

namespace Xenko.Engine.Processors
{
    public class LightShaftBoundingVolumeProcessor : EntityProcessor<LightShaftBoundingVolumeComponent>
    {
        private Dictionary<LightShaftComponent, List<Data>> volumesPerLightShaft = new Dictionary<LightShaftComponent, List<Data>>();
        private bool isDirty;

        public override void Update(GameTime time)
        {
            if (isDirty)
            {
                UpdateVolumesPerLightShaft();
                isDirty = false;
            }
        }

        public IReadOnlyList<Data> GetBoundingVolumesForComponent(LightShaftComponent component)
        {
            List<Data> data;
            if (!volumesPerLightShaft.TryGetValue(component, out data))
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

        private void UpdateVolumesPerLightShaft()
        {
            volumesPerLightShaft.Clear();

            foreach (var pair in ComponentDatas)
            {
                if (!pair.Key.Enabled)
                    continue;

                var lightShaft = pair.Key.LightShaft;
                if (lightShaft == null)
                    continue;

                List<Data> data;
                if (!volumesPerLightShaft.TryGetValue(lightShaft, out data))
                    volumesPerLightShaft.Add(lightShaft, data = new List<Data>());

                data.Add(new Data
                {
                    Component = pair.Key,
                });
            }
        }

        public class Data
        {
            public LightShaftBoundingVolumeComponent Component;
            public Matrix World => Component.Entity.Transform.WorldMatrix;
            public Model Model => Component.Model;
        }
    }
}
