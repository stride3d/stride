// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;

namespace Stride.BepuPhysics.Definitions.SimTests;


interface IOverlapCollector
{
    public bool Full { get; }

    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>;
}

internal unsafe struct SpanManifoldCollector(OverlapInfoStack* Ptr, int Length, BepuSimulation BepuSimulation) : IOverlapCollector
{
    public int Head;

    public bool Full => Head == Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        if (Head >= Length)
            return;

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

    public bool Full => Head == Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void OnPairCompleted<TManifold>(BepuSimulation simulation, CollidableReference reference, ref TManifold manifold) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        if (Head >= Length)
            return;

        for (int i = 0; i < manifold.Count; ++i)
        {
            if (manifold.GetDepth(i) < 0)
                continue;

            var component = BepuSimulation.GetComponent(reference);
            if (component.CollidableCount > 1 && AlreadyCollected(component))
                break; // A collidable spanning multiple bodies is reported once, whichever of its bodies overlap

            Ptr[Head++] = new(reference, component.Versioning);
            break;
        }
    }

    private bool AlreadyCollected(CollidableComponent component)
    {
        for (int i = 0; i < Head; i++)
        {
            if (ReferenceEquals(BepuSimulation.GetComponent(Ptr[i].Reference), component))
                return true;
        }
        return false;
    }
}

internal readonly struct CollectionCollector(ICollection<OverlapInfo> Collection) : IOverlapCollector
{
    public bool Full => false;

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
