using System.Linq;
using Stride.BepuPhysics.Components.Containers;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    [AllowMultipleComponents]
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

        [DataMemberIgnore]
        internal ContainerComponent? Container => Entity?.GetComponentsInParents<ContainerComponent>(true).FirstOrDefault();
    }
}
