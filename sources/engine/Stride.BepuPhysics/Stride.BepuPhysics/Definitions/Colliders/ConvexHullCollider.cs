using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
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

        internal override void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose)
        {
            builder.Add(new ConvexHull(GetMeshPoints(), new BufferPool(), out _), localPose, Mass);
        }

        internal Span<System.Numerics.Vector3> GetMeshPoints(bool scale = true)
        {
            if (Hull == null)
                return new();

            int vertCount = 0;
            foreach (var colliderShapeDesc in Hull.Descriptions)
            {
                if (colliderShapeDesc is ConvexHullColliderShapeDesc hullDesc) // This casting nonsense should be replaced once we have a proper asset to host convex shapes
                {
                    for (int mesh = 0; mesh < hullDesc.ConvexHulls.Count; mesh++)
                        for (int hull = 0; hull < hullDesc.ConvexHulls[mesh].Count; hull++)
                            vertCount += hullDesc.ConvexHullsIndices[mesh][hull].Count;
                }
            }

            int outputIndex = 0;
            System.Numerics.Vector3[] output = new System.Numerics.Vector3[vertCount];
            System.Numerics.Vector3 colliderScaling = scale ? Scale.ToNumericVector() : System.Numerics.Vector3.One;
            foreach (var colliderShapeDesc in Hull.Descriptions)
            {
                if (colliderShapeDesc is ConvexHullColliderShapeDesc hullDesc)
                {
                    System.Numerics.Vector3 hullScaling = hullDesc.Scaling.ToNumericVector();
                    for (int mesh = 0; mesh < hullDesc.ConvexHulls.Count; mesh++)
                    {
                        for (var hull = 0; hull < hullDesc.ConvexHulls[mesh].Count; hull++)
                        {
                            var verts = hullDesc.ConvexHulls[mesh][hull];
                            foreach (uint u in hullDesc.ConvexHullsIndices[mesh][hull])
                            {
                                output[outputIndex++] = verts[(int)u].ToNumericVector() * hullScaling * colliderScaling; //We aply scale here because if scale change we rebuilt the container.
                            }
                        }
                    }
                }
            }


            return output;
        }
    }
}
