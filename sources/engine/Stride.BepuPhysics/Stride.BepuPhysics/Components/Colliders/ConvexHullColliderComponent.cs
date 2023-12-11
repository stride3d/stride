using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities.Memory;
using Stride.BepuPhysics.Extensions;
using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using Stride.Physics;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public sealed class ConvexHullColliderComponent : ColliderComponent
    {
        #warning Replace with an explicit reference to hulls once the asset part for hulls is done
        public PhysicsColliderShape? Hull;

        public ConvexHullColliderComponent()
        {
        }

        internal override void AddToCompoundBuilder(IGame game, ref CompoundBuilder builder, RigidPose localPose)
        {
            builder.Add(new ConvexHull(GetMeshPoints(), new BufferPool(), out _), localPose, Mass);
        }

        Span<System.Numerics.Vector3> GetMeshPoints()
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
            System.Numerics.Vector3 entityScaling = Entity.Transform.Scale.ToNumericVector();
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
                                #warning Scaling here means that changing entity scale after the fact doesn't affect the physical shape
                                output[outputIndex++] = verts[(int)u].ToNumericVector() * hullScaling * entityScaling;
                            }
                        }
                    }
                }
            }


            return output;
        }
    }
}
