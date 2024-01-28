using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal struct RayHitsArrayHandler : IRayHitHandler, ISweepHitHandler
{
    private readonly HitInfo[] _array;
    private readonly BepuSimulation _sim;

    public CollisionMask CollisionMask { get; set; }
    public int Count { get; set; }
    public float StoredMax { get; set; }
    public int IndexOfMax { get; set; }

    public RayHitsArrayHandler(BepuSimulation sim, HitInfo[] array, CollisionMask collisionMask)
    {
        _array = array;
        _sim = sim;
        CollisionMask = collisionMask;
        StoredMax = float.NegativeInfinity;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => CollisionMask.AllowTest(collidable, _sim);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true;
    }

    public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
    {
        if (Count < _array.Length)
        {
            if (t > StoredMax)
            {
                StoredMax = t;
                IndexOfMax = Count;
            }

            _array[Count++] = new(ray.Origin + ray.Direction * t, normal, t, _sim.GetContainer(collidable));

            if (Count == _array.Length) // Once the array is filled up, ignore all hits that occur further away than the furthest hit in the array
                maximumT = StoredMax;
        }
        else
        {
            Debug.Assert(t > StoredMax, "maximumT should have prevented this hit from being returned, if this is hit it means that we need to change the above into an 'else if (distance < StoredMax)'");

            _array[IndexOfMax] = new(ray.Origin + ray.Direction * t, normal, t, _sim.GetContainer(collidable));

            // Re-scan to find the new max now that the last one was replaced
            StoredMax = float.NegativeInfinity;
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i].Distance > StoredMax)
                {
                    StoredMax = _array[i].Distance;
                    IndexOfMax = i;
                }
            }

            maximumT = StoredMax;
        }
    }

    public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 normal, CollidableReference collidable)
    {
        if (Count < _array.Length)
        {
            if (t > StoredMax)
            {
                StoredMax = t;
                IndexOfMax = Count;
            }

            _array[Count++] = new(hitLocation, normal, t, _sim.GetContainer(collidable));

            if (Count == _array.Length) // Once the array is filled up, ignore all hits that occur further away than the furthest hit in the array
                maximumT = StoredMax;
        }
        else
        {
            Debug.Assert(t > StoredMax, "maximumT should have prevented this hit from being returned, if this is hit it means that we need to change the above into an 'else if (distance < StoredMax)'");

            _array[IndexOfMax] = new(hitLocation, normal, t, _sim.GetContainer(collidable));

            // Re-scan to find the new max now that the last one was replaced
            StoredMax = float.NegativeInfinity;
            for (int i = 0; i < _array.Length; i++)
            {
                if (_array[i].Distance > StoredMax)
                {
                    StoredMax = _array[i].Distance;
                    IndexOfMax = i;
                }
            }

            maximumT = StoredMax;
        }
    }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        // Right now just ignore the hit;
        // We can't just set info to invalid data, it'll be confusing for users,
        // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
    }
}