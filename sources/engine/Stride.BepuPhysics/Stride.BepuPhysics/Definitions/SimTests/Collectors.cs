// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.SimTests;


interface IOverlapCollector
{
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>;
}

internal unsafe struct SpanManifoldCollector(OverlapInfoStack* Ptr, int Length, BepuSimulation BepuSimulation) : IOverlapCollector
{
    public int Head;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        if (Head >= Length)
            return;

#warning should short circuit the whole overlap test in the caller as soon as head is filled
        CollidableComponent? collidable = null;
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) < 0)
                continue;

            collidable ??= BepuSimulation.GetComponent(reference);
            Ptr[Head++] = new(new(reference, collidable.Versioning), manifold.GetNormal(i).ToStride(), manifold.GetDepth(i));
            if (Head >= Length)
                return;
        }
    }
}

internal unsafe struct SpanCollidableCollector(CollidableStack* Ptr, int Length, BepuSimulation BepuSimulation) : IOverlapCollector
{
    public int Head;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        if (Head >= Length)
            return;

#warning should short circuit the whole overlap test in the caller as soon as head is filled
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) < 0)
                continue;

            Ptr[Head++] = new(reference, BepuSimulation.GetComponent(reference).Versioning);
            break;
        }
    }
}

internal readonly struct CollectionCollector(ICollection<OverlapInfo> Collection) : IOverlapCollector
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) >= 0)
                Collection.Add(new (simulation.GetComponent(reference), manifold.GetNormal(i).ToStride(), manifold.GetDepth(i)));
        }
    }
}
