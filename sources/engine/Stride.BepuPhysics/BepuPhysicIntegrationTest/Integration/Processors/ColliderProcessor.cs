using System.Collections.Generic;
using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysics.Collidables;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.Games;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ColliderProcessor : EntityProcessor<ColliderComponent>
    {

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            base.OnEntityComponentAdding(entity, component, data);
            component.Container?.ContainerData?.BuildShape();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            base.OnEntityComponentRemoved(entity, component, data);
            component.Container?.ContainerData?.BuildShape();
        }

        public override void Update(GameTime time)
        {
            base.Update(time);
        }
    }

}
