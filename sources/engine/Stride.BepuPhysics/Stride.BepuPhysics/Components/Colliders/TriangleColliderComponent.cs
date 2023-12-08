using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class TriangleColliderComponent : ColliderComponent
    {
        private Vector3 _a = new(1, 1, 1);
        private Vector3 _b = new(1, 1, 1);
        private Vector3 _c = new(1, 1, 1);

        public Vector3 A
        {
            get => _a;
            set
            {
                _a = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }

        public Vector3 B
        {
            get => _b;
            set
            {
                _b = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }

        public Vector3 C
        {
            get => _c;
            set
            {
                _c = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }
    }
}
