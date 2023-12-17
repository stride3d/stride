using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;
using Stride.BepuPhysics.Configurations;
using Stride.BepuPhysics.Extensions;

namespace Stride.BepuPhysics.Definitions.Raycast
{
    internal struct RayHitsArrayHandler : IRayHitHandler, ISweepHitHandler
    {
        private HitInfo[] _array;
        private readonly BepuSimulation _sim;

        public byte CollisionMask { get; set; }
        public int Count { get; set; }

        public RayHitsArrayHandler(BepuSimulation sim, HitInfo[] array, byte collisionMask)
        {
            _array = array;
            CollisionMask = collisionMask;
            this._sim = sim;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable)
        {
            var result = collidable.GetContainerFromCollidable(_sim);
            if (result == null)
                return true;

            var a = CollisionMask;
            var b = result.ColliderGroupMask;
            var com = a & b;
            return com == a || com == b && com != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool AllowTest(CollidableReference collidable, int childIndex)
        {
            return true;
        }

        public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
        {
            if (Count < _array.Length)
            {
                _array[Count++] = new()
                {
                    Container = collidable.GetContainerFromCollidable(_sim) ?? throw new NullReferenceException(collidable.ToString()),
                    Normal = normal,
                    Distance = t,
                    Point = ray.Origin + ray.Direction * t
                };
                Array.Sort(_array, 0, Count);
            }
            else if (_array[Count - 1].Distance > t) // The array is sorted, so last is guaranteed to always be the furthest away.
            {
                // Although this comparison may be unnecessary as setting maximumT below guarantees that any subsequent hit is closer than the previous one ...
                // Something to validate when we have time
                _array[Count - 1] = new()
                {
                    Container = collidable.GetContainerFromCollidable(_sim) ?? throw new NullReferenceException(collidable.ToString()),
                    Normal = normal,
                    Distance = t,
                    Point = ray.Origin + ray.Direction * t
                };
                Array.Sort(_array);
            }

            if (Count >= _array.Length)
                maximumT = t;
        }

        public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
        {
            if (Count < _array.Length)
            {
                _array[Count++] = new()
                {
                    Container = collidable.GetContainerFromCollidable(_sim) ?? throw new NullReferenceException(collidable.ToString()),
                    Normal = hitNormal,
                    Distance = t,
                    Point = hitLocation
                };
                Array.Sort(_array, 0, Count);
            }
            else if (_array[Count - 1].Distance > t) // The array is sorted, so last is guaranteed to always be the furthest away.
            {
                // Although this comparison may be unnecessary as setting maximumT below guarantees that any subsequent hit is closer than the previous one ...
                // Something to validate when we have time
                _array[Count - 1] = new()
                {
                    Container = collidable.GetContainerFromCollidable(_sim) ?? throw new NullReferenceException(collidable.ToString()),
                    Normal = hitNormal,
                    Distance = t,
                    Point = hitLocation
                };
                Array.Sort(_array);
            }

            if (Count >= _array.Length)
                maximumT = t;
        }

        public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
        {
            // Right now just ignore the hit;
            // We can't just set info to invalid data, it'll be confusing for users,
            // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
        }
    }
}
