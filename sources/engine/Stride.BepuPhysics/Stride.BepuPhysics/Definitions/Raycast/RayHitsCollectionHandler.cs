using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    internal struct RayHitsCollectionHandler : IRayHitHandler, ISweepHitHandler
    {
        private ICollection<HitInfo> _collection;
        private readonly BepuSimulation _sim;

        public CollisionMask CollisionMask { get; set; }

        public RayHitsCollectionHandler(BepuSimulation sim, ICollection<HitInfo> collection, CollisionMask collisionMask)
        {
            _collection = collection;
            CollisionMask = collisionMask;
            this._sim = sim;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable) => TestHandler.AllowTest(_sim, CollisionMask, collidable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            _collection.Add(new(ray.Origin + ray.Direction * t, normal, t, collidable.GetContainerFromCollidable(_sim)));
        }

        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            _collection.Add(new(hitLocation, hitNormal, t, collidable.GetContainerFromCollidable(_sim)));
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            // Right now just ignore the hit;
            // We can't just set info to invalid data, it'll be confusing for users,
            // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
        }
    }
}
