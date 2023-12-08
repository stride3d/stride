using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class CapsuleColliderComponent : ColliderComponent
    {
        private float _radius = 1f;
        private float _length = 1f;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }

        public float Length
        {
            get => _length;
            set
            {
                _length = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }
    }
}
