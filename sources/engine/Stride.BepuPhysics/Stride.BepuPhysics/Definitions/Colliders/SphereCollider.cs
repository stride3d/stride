using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract]
    public sealed class SphereCollider : ColliderBase
    {
        private float _radius = 0.5f;

        public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        internal override void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose)
        {
            builder.Add(new Sphere(Radius), localPose, Mass);
        }
    }
}
