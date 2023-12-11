using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public sealed class CylinderColliderComponent : ColliderComponent
    {
        private float _radius = 1f;
        private float _length = 1f;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        public float Length
        {
            get => _length;
            set
            {
                _length = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        internal override void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose)
        {
            builder.Add(new Cylinder(Radius, Length), localPose, Mass);
        }
    }
}
