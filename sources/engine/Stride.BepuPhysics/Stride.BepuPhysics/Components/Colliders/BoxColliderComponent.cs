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
    public class BoxColliderComponent : ColliderComponent
    {
        private Vector3 _size = new(1, 1, 1);

        public Vector3 Size
        {
            get => _size;
            set
            {
                _size = value;
                if (Container?.ContainerData?.Exist == true)
                    Container?.ContainerData.BuildOrUpdateContainer();
            }
        }
    }
}
