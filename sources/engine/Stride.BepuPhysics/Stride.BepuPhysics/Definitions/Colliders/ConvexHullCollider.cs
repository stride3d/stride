using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Physics;

namespace Stride.BepuPhysics.Definitions.Colliders
{
    [DataContract]
    public sealed class ConvexHullCollider : ColliderBase
    {
        private Vector3 _scale = new(1, 1, 1);
        public PhysicsColliderShape? Hull { get; set; }

        public Vector3 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                Container?.ContainerData?.TryUpdateContainer();
            }
        }

        internal override void AddToCompoundBuilder(IGame game, BepuSimulation simulation, ref CompoundBuilder builder, RigidPose localPose)
        {
#warning maybe don't rely on cache actually, instead cache the convexhull struct itself ? See if that can be reused
            var data = game.Services.GetService<BepuShapeCacheSystem>().BorrowHull(this);
            var points = MemoryMarshal.Cast<VertexPosition3, System.Numerics.Vector3>(data.Vertices);

            if (_scale != Vector3.One) // Bepu doesn't support scaling on the collider itself, we have to create a temporary array and scale the points before passing it on
            {
                var copy = points.ToArray();
                var scaleAsNumerics = _scale.ToNumericVector();
                for (int i = 0; i < copy.Length; i++)
                {
                    copy[i] *= scaleAsNumerics;
                }

                points = copy;
            }

            builder.Add(new ConvexHull(points, simulation.BufferPool, out _), localPose, Mass);
        }
    }
}
