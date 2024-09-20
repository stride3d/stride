// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal unsafe struct RayHitsStackHandler(HitInfoStack* Ptr, int Length, BepuSimulation sim, CollisionMask collisionMask) : IRayHitHandler, ISweepHitHandler
{
    public int Head { get; private set; }
    private float storedMax = float.NegativeInfinity;
    private int indexOfMax;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
    {
        if (Head < Length)
        {
            if (t > storedMax)
            {
                storedMax = t;
                indexOfMax = Head;
            }

            Ptr[Head++] = GenerateHitInfo((ray.Origin + ray.Direction * t), normal, t, collidable, sim, childIndex);

            if (Head == Length) // Once the array is filled up, ignore all hits that occur further away than the furthest hit in the array
                maximumT = storedMax;
        }
        else
        {
            Debug.Assert(t > storedMax, "maximumT should have prevented this hit from being returned, if this is hit it means that we need to change the above into an 'else if (distance < StoredMax)'");

            Ptr[indexOfMax] = GenerateHitInfo((ray.Origin + ray.Direction * t), normal, t, collidable, sim, childIndex);

            // Re-scan to find the new max now that the last one was replaced
            storedMax = float.NegativeInfinity;
            for (int i = 0; i < Length; i++)
            {
                if (Ptr[i].Distance > storedMax)
                {
                    storedMax = Ptr[i].Distance;
                    indexOfMax = i;
                }
            }

            maximumT = storedMax;
        }
    }

    public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 normal, CollidableReference collidable)
    {
        if (Head < Length)
        {
            if (t > storedMax)
            {
                storedMax = t;
                indexOfMax = Head;
            }

            Ptr[Head++] = GenerateHitInfo(hitLocation, normal, t, collidable, sim, -1);

            if (Head == Length) // Once the array is filled up, ignore all hits that occur further away than the furthest hit in the array
                maximumT = storedMax;
        }
        else
        {
            Debug.Assert(t > storedMax, "maximumT should have prevented this hit from being returned, if this is hit it means that we need to change the above into an 'else if (distance < StoredMax)'");

            Ptr[indexOfMax] = GenerateHitInfo(hitLocation, normal, t, collidable, sim, -1);

            // Re-scan to find the new max now that the last one was replaced
            storedMax = float.NegativeInfinity;
            for (int i = 0; i < Length; i++)
            {
                if (Ptr[i].Distance > storedMax)
                {
                    storedMax = Ptr[i].Distance;
                    indexOfMax = i;
                }
            }

            maximumT = storedMax;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static HitInfoStack GenerateHitInfo(Vector3 location, Vector3 normal, float t, CollidableReference collidable, BepuSimulation sim, int childIndex) => new(new(collidable, sim.GetComponent(collidable).Versioning), location.ToStride(), normal.ToStride(), t, childIndex);

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        // Right now just ignore the hit;
        // We can't just set info to invalid data, it'll be confusing for users,
        // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
    }
}
