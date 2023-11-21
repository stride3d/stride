using System.Linq;
using BepuPhysicIntegrationTest.Integration.Components.Containers;
using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public abstract class ColliderComponent : EntityComponent
    {
        private float _mass = 1f;

        public float Mass
        {
            get => _mass;
            set
            {
                _mass = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }

        internal ContainerComponent? Container => Entity.GetComponentsInParents<ContainerComponent>(true).FirstOrDefault();
    }
}
