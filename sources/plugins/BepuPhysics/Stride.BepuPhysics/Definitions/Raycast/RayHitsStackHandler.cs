// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal unsafe struct RayHitsStackHandler(HitInfoStack* Ptr, int Length, BepuSimulation sim, CollisionMask collisionMask) : IRayHitHandler, ISweepHitHandler, IShapeRayHitHandler
{
    public CollidableReference ShapeHandled { get; init; }

    public int Head { get; private set; }
    private float _storedMax = float.NegativeInfinity;
    private int _indexOfMax;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(int childIndex) => true;

    public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
    {
        InsertHit(ray.Origin + ray.Direction * t, ref maximumT, t, normal, collidable, childIndex);
    }

    public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 normal, CollidableReference collidable)
    {
        InsertHit(hitLocation, ref maximumT, t, normal, collidable, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void InsertHit(Vector3 hitLocation, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
    {
        if (Head < Length)
        {
            if (t > _storedMax)
            {
                _storedMax = t;
                _indexOfMax = Head;
            }

            Ptr[Head++] = GenerateHitInfo(hitLocation, normal, t, collidable, sim, childIndex);

            if (Head == Length) // Once the array is filled up, ignore all hits that occur further away than the furthest hit in the array
                maximumT = _storedMax;
        }
        else
        {
            Ptr[_indexOfMax] = GenerateHitInfo(hitLocation, normal, t, collidable, sim, childIndex);

            // Re-scan to find the new max now that the last one was replaced
            _storedMax = float.NegativeInfinity;
            for (int i = 0; i < Length; i++)
            {
                if (Ptr[i].Distance > _storedMax)
                {
                    _storedMax = Ptr[i].Distance;
                    _indexOfMax = i;
                }
            }

            maximumT = _storedMax;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static HitInfoStack GenerateHitInfo(Vector3 location, Vector3 normal, float t, CollidableReference collidable, BepuSimulation sim, int childIndex) => new(new(collidable, sim.GetComponent(collidable).Versioning), location.ToStride(), normal.ToStride(), t, childIndex);

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        // Right now just ignore the hit;
        // We can't just set info to invalid data, it'll be confusing for users,
        // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
    }

    public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, int childIndex)
    {
        Debug.Assert(ShapeHandled.Packed != 0);
        InsertHit(ray.Origin + ray.Direction * t, ref maximumT, t, normal, ShapeHandled, -1);
    }
}
