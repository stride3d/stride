// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;

namespace Stride.BepuPhysics.Definitions.Raycast;

internal struct OverlapCollectionHandler : ISweepHitHandler
{
    private readonly BepuSimulation _sim;
    private readonly ICollection<CollidableComponent> _collection;

    public CollisionMask CollisionMask { get; set; }

    public OverlapCollectionHandler(BepuSimulation sim, ICollection<CollidableComponent> collection, CollisionMask collisionMask)
    {
        _sim = sim;
        _collection = collection;
        CollisionMask = collisionMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => CollisionMask.AllowTest(collidable, _sim);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable, int childIndex)
    {
        return true;
    }

    public void OnHit(ref float maximumT, float t, System.Numerics.Vector3 hitLocation, System.Numerics.Vector3 hitNormal, CollidableReference collidable){ }

    public void OnHitAtZeroT(ref float maximumT, CollidableReference collidable)
    {
        _collection.Add(_sim.GetComponent(collidable));
    }
}

internal struct OverlapArrayHandler : ISweepHitHandler
{
    private readonly BepuSimulation _sim;
    private readonly CollidableComponent[] _collection;

    public CollisionMask CollisionMask { get; set; }
    public int Count { get; set; }

    public OverlapArrayHandler(BepuSimulation sim, CollidableComponent[] collection, CollisionMask collisionMask)
    {
        _sim = sim;
        _collection = collection;
        CollisionMask = collisionMask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowTest(CollidableReference collidable) => CollisionMask.AllowTest(collidable, _sim);

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

        _collection[Count++] = _sim.GetComponent(collidable);

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
    public bool AllowTest(CollidableReference collidable) => CollisionMask.AllowTest(collidable, _sim);

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