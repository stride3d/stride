// Copyright (c) Stride contributors (https://stride3d.net) and Tebjan Halm
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;

namespace Stride.Engine.Processors
{
    public class InstanceProcessor : EntityProcessor<InstanceComponent>
    {
        public InstanceProcessor() 
            : base (typeof(TransformComponent)) // Requires TransformComponent
        {
            // After TransformProcessor but before InstancingProcessor
            Order = -110;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] InstanceComponent component, [NotNull] InstanceComponent data)
        {
            if (component.Master == null)
            {
                component.Master = FindMasterInParents(component.Entity);
            }
        }

        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] InstanceComponent component, [NotNull] InstanceComponent data)
        {
            component.DisconnectInstancing();
        }

        private InstancingComponent FindMasterInParents(Entity entity)
        {
            var parent = entity?.GetParent();

            while (parent != null)
            {
                var ic = parent.Get<InstancingComponent>();

                if (ic != null)
                    return ic;

                parent = parent.GetParent();
            }

            return null;
        }
    }
}
