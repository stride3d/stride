using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class SphereColliderComponent : ColliderComponent
    {
        private float _radius = 1f;

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
    }
}
