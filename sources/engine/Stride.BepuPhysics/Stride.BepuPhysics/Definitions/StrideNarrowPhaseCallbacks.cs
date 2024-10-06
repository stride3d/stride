// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using Stride.BepuPhysics.Definitions.Contacts;

namespace Stride.BepuPhysics.Definitions;

internal struct StrideNarrowPhaseCallbacks(BepuSimulation Simulation, ContactEventsManager contactEvents, CollidableProperty<MaterialProperties> collidableMaterials) : INarrowPhaseCallbacks
{
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
    public unsafe bool ConfigureContactManifold<TManifold>(int workerIndex, CollidablePair pair, ref TManifold manifold, out PairMaterialProperties pairMaterial) where TManifold : unmanaged, IContactManifold<TManifold>
    {
        //For the purposes of this demo, we'll use multiplicative blending for the friction and choose spring properties according to which collidable has a higher maximum recovery velocity.
        var a = collidableMaterials[pair.A];
        var b = collidableMaterials[pair.B];
        pairMaterial.FrictionCoefficient = a.FrictionCoefficient * b.FrictionCoefficient;
        pairMaterial.MaximumRecoveryVelocity = MathF.Max(a.MaximumRecoveryVelocity, b.MaximumRecoveryVelocity);
        pairMaterial.SpringSettings = pairMaterial.MaximumRecoveryVelocity == a.MaximumRecoveryVelocity ? a.SpringSettings : b.SpringSettings;
        contactEvents.HandleManifold(workerIndex, pair, ref manifold);

        if (a.IsTrigger || b.IsTrigger)
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool ConfigureContactManifold(int workerIndex, CollidablePair pair, int childIndexA, int childIndexB, ref ConvexContactManifold manifold)
    {
        return true;
    }

    public void Dispose()
    {
    }
}
