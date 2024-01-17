using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.BepuPhysics.Components.Containers.Interfaces;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    internal struct OverlapCollectionHandler : ISweepHitHandler
    {
        private readonly BepuSimulation _sim;
        private readonly ICollection<IContainer> _collection;

        public CollisionMask CollisionMask { get; set; }

        public OverlapCollectionHandler(BepuSimulation sim, ICollection<IContainer> collection, CollisionMask collisionMask)
        {
            _sim = sim;
            _collection = collection;
            CollisionMask = collisionMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable) => TestHandler.AllowTest(_sim, CollisionMask, collidable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            _collection.Add(collidable.GetContainerFromCollidable(_sim));
        }
    }

    internal struct OverlapArrayHandler : ISweepHitHandler
    {
        private readonly BepuSimulation _sim;
        private readonly IContainer[] _collection;

        public CollisionMask CollisionMask { get; set; }
        public int Count { get; set; }

        public OverlapArrayHandler(BepuSimulation sim, IContainer[] collection, CollisionMask collisionMask)
        {
            _sim = sim;
            _collection = collection;
            CollisionMask = collisionMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable) => TestHandler.AllowTest(_sim, CollisionMask, collidable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            if (Count >= _collection.Length)
                return;

            _collection[Count++] = collidable.GetContainerFromCollidable(_sim);

            if (Count == _collection.Length)
                maximumT = -1f; // We want to notify bepu that we don't care about any subsequent collision, not sure that works in the process breaking out early but whatever
        }
    }

    internal struct OverlapAnyHandler : ISweepHitHandler
    {
        private readonly BepuSimulation _sim;

        public CollisionMask CollisionMask { get; set; }
        public bool Any { get; set; }

        public OverlapAnyHandler(BepuSimulation sim, CollisionMask collisionMask)
        {
            _sim = sim;
            CollisionMask = collisionMask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable) => TestHandler.AllowTest(_sim, CollisionMask, collidable);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            Any = true;
            maximumT = -1f; // Not sure that even works to let bepu know that it should not compute for more at all
        }
    }
}