using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract]
    public sealed class CapsuleCollider : ColliderBase
    {
        private float _radius = 0.35f;
        private float _length = 0.5f;

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
            builder.Add(new Capsule(Radius, Length), localPose, Mass);
        }
    }
}
