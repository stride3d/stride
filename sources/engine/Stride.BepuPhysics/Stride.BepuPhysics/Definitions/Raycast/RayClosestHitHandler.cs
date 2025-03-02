// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.Trees;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal struct RayClosestHitHandler(BepuSimulation sim, CollisionMask collisionMask) : IRayHitHandler, ISweepHitHandler
{
    public HitInfo? HitInformation { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true;
    }

    public void OnRayHit(in RayData ray, ref float maximumT, float t, Vector3 normal, CollidableReference collidable, int childIndex)
    {
        HitInformation = new(ray.Origin + ray.Direction * t, normal, t, sim.GetComponent(collidable), childIndex);
        maximumT = t;
    }

    public void OnHit(ref float maximumT, float t, Vector3 hitLocation, Vector3 hitNormal, CollidableReference collidable)
    {
        HitInformation = new(hitLocation, hitNormal, t, sim.GetComponent(collidable), -1);
        maximumT = t;
    }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        // Right now just ignore the hit;
        // We can't just set info to invalid data, it'll be confusing for users,
        // but we might need to find a way to notify that the shape at its resting pose is already intersecting.
    }
}
