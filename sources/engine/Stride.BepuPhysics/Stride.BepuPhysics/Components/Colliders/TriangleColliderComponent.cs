using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;

#warning I don't think this would have any actual use, you can keep this internal if you want to keep it for debugging purposes

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public sealed class TriangleColliderComponent : ColliderComponent
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
