using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class CylinderColliderComponent : ColliderComponent
    {
        private float _radius = 1f;
        private float _length = 1f;
        public float Radius
        {
            get
            {
                return _radius;
            }
            set
            {
                _radius = value;
            }
        }
        public float Length
        {
            get
            {
                return _length;
            }
            set
            {
                _length = value;
            }
        }
        public CylinderColliderComponent()
        {
        }
    }
}
