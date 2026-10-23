// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal struct OverlapCollectionHandler(BepuSimulation sim, ICollection<CollidableComponent> collection, CollisionMask collisionMask) : ISweepHitHandler
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        collection.Add(sim.GetComponent(collidable));
    }
}

internal struct OverlapArrayHandler(BepuSimulation sim, CollidableComponent[] collection, CollisionMask collisionMask) : ISweepHitHandler
{
    public int Count { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        if (Count >= collection.Length)
            return;

        collection[Count++] = sim.GetComponent(collidable);

        if (Count == collection.Length)
            maximumT = -1f; // We want to notify bepu that we don't care about any subsequent collision, not sure that works in the process breaking out early but whatever
    }
}

internal struct OverlapAnyHandler(BepuSimulation sim, CollisionMask collisionMask) : ISweepHitHandler
{
    public bool Any { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => sim.ShouldPerformPhysicsTest(collisionMask, collidable);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex) => true;

    public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        Any = true;
        maximumT = -1f; // Not sure that even works to let bepu know that it should not compute for more at all
    }
}
