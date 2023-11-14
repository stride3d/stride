using BepuPhysicIntegrationTest.Integration.Components.Colliders;
using Stride.Core.Annotations;
using Stride.Engine;

namespace BepuPhysicIntegrationTest.Integration.Processors
{
    public class ColliderProcessor : EntityProcessor<ColliderComponent>
    {

        public ColliderProcessor()
        {
            Order = 10010;
        }

        protected override void OnEntityComponentAdding(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            component.Container?.ContainerData?.BuildShape();
        }
        protected override void OnEntityComponentRemoved(Entity entity, [NotNull] ColliderComponent component, [NotNull] ColliderComponent data)
        {
            component.Container?.ContainerData?.BuildShape();
        }

    }

}
