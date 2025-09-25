// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions.Contacts;

namespace Stride.BepuPhysics.Definitions;

internal struct StrideNarrowPhaseCallbacks(BepuSimulation Simulation, ContactEventsManager contactEvents, CollidableProperty<MaterialProperties> collidableMaterials) : INarrowPhaseCallbacks
{
#if DEBUG
    [ThreadStatic] private static int configuredChildIndex, configuredManifold;
#endif

    public void Initialize(Simulation simulation)
    {
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidableReference a, CollidableReference b, ref float speculativeMargin)
    {
        return a.Mobility == CollidableMobility.Dynamic || b.Mobility == CollidableMobility.Dynamic;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AllowContactGeneration(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB)
    {
        const int DEFAULT_DISTANCE = 1;

        var matA = collidableMaterials[pair.A];
        var matB = collidableMaterials[pair.B];

        if (Simulation.CollisionMatrix.Get(matA.Layer, matB.Layer) == false)
            return false;

        if (matA.CollisionGroup.Id == matB.CollisionGroup.Id && matA.CollisionGroup.Id != 0)
        {
            int differenceA = matA.CollisionGroup.IndexA - matB.CollisionGroup.IndexA;
            int differenceB = matA.CollisionGroup.IndexB - matB.CollisionGroup.IndexB;
            int differenceC = matA.CollisionGroup.IndexC - matB.CollisionGroup.IndexC;

            if (differenceA is >= -DEFAULT_DISTANCE and <= DEFAULT_DISTANCE
                && differenceB is >= -DEFAULT_DISTANCE and <= DEFAULT_DISTANCE
                && differenceC is >= -DEFAULT_DISTANCE and <= DEFAULT_DISTANCE)
            {
                return false;
            }
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        //For the purposes of this demo, we'll use multiplicative blending for the friction and choose spring properties according to which collidable has a higher maximum recovery velocity.
        var a = collidableMaterials[pair.A];
        var b = collidableMaterials[pair.B];
        pairMaterial.FrictionCoefficient = a.FrictionCoefficient * b.FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MathF.Max(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity);
        pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == a.MaximumRecoveryVelocity ? a.SpringSettings : b.SpringSettings;

#if DEBUG
        // Validate that all manifolds have been stored through the other ConfigureContactManifold,
        // previously we would store the manifold from here as well, leading to duplicates
        if (manifold.Count != 0)
        {
            ++configuredManifold;
            Debug.Assert(configuredChildIndex == configuredManifold);
        }
#endif

        if (a.IsTrigger || b.IsTrigger)
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        contactEvents.StoreManifold(workerIndex, pair, childIndexA, childIndexB, ref manifold);
        #if DEBUG
        Debug.Assert(manifold.Count > 0);
        configuredChildIndex++;
        #endif
        return true;
    }

    public void Dispose()
    {
    }
}
