using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract]
    public sealed class TriangleCollider : ColliderBase
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
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        public Vector3 B
        {
            get => _b;
            set
            {
                _b = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        public Vector3 C
        {
            get => _c;
            set
            {
                _c = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        internal override void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose)
        {
            builder.Add(new Triangle(A.ToNumericVector(), B.ToNumericVector(), C.ToNumericVector()), localPose, Mass);
        }
    }
}
