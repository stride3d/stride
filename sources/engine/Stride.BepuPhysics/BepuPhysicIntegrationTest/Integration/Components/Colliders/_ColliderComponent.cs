using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Components.Simulations;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public abstract class ColliderComponent : SyncScript
    {
        public float Mass { get; set; } = 1f;

        internal ContainerComponent Container => Entity.GetInMeOrParents<ContainerComponent>();
        
        public override void Start()
        {
            base.Start();
        }

    }
}
