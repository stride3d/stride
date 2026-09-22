// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuUtilities;
using BepuUtilities.Memory;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders.Voxels;

/// <summary>
/// Hands the collision batcher the box or sphere children of a voxel shape, written into its shape cache as they are requested.
/// </summary>
internal unsafe struct ConvexVoxelContinuations<TShape> : IConvexCompoundContinuationHandler<NonconvexReduction>
    where TShape : unmanaged, IVoxelShape
{
    public readonly CollisionContinuationType CollisionContinuationType => CollisionContinuationType.NonconvexReduction;

    public ref NonconvexReduction CreateContinuation<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, int childCount, in BoundsTestedPair pair, in OverlapQueryForPair pairQuery, out int continuationIndex)
        where TCallbacks : struct, ICollisionCallbacks
        => ref collisionBatcher.NonconvexReductions.CreateContinuation(childCount, collisionBatcher.Pool, out continuationIndex);

    /// <summary>Bytes of shape data the largest box or sphere child needs.</summary>
    private const int MaxChildDataSize = 16;

    /// <summary>Writes one child of the voxel shape into the batcher's shape cache.</summary>
    public static void GetChildData<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, in BoundsTestedPair pair, int shapeTypeA, int childIndexB,
        out NRigidPose childPoseB, out int childTypeB, out void* childShapeDataB)
        where TCallbacks : struct, ICollisionCallbacks
    {
        ref var shape = ref Unsafe.AsRef<TShape>(pair.B);
        var childData = stackalloc byte[MaxChildDataSize];
        var size = shape.WriteChildShapeData(childIndexB, out var localPosition, childData);
        QuaternionEx.TransformWithoutOverlap(localPosition, pair.OrientationB, out childPoseB.Position);
        childPoseB.Orientation = Quaternion.Identity;
        childTypeB = TShape.ChildShapeTypeId;
        collisionBatcher.CacheShapeB(shapeTypeA, childTypeB, childData, size, out childShapeDataB);
    }

    public void ConfigureContinuationChild<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, ref NonconvexReduction continuation, int continuationChildIndex, in BoundsTestedPair pair, int shapeTypeA, int childIndexB,
        out NRigidPose childPoseB, out int childTypeB, out void* childShapeDataB)
        where TCallbacks : struct, ICollisionCallbacks
    {
        ref var continuationChild = ref continuation.Children[continuationChildIndex];
        GetChildData(ref collisionBatcher, pair, shapeTypeA, childIndexB, out childPoseB, out childTypeB, out childShapeDataB);
        if (pair.FlipMask < 0)
        {
            continuationChild.ChildIndexA = childIndexB;
            continuationChild.ChildIndexB = 0;
            continuationChild.OffsetA = childPoseB.Position;
            continuationChild.OffsetB = default;
        }
        else
        {
            continuationChild.ChildIndexA = 0;
            continuationChild.ChildIndexB = childIndexB;
            continuationChild.OffsetA = default;
            continuationChild.OffsetB = childPoseB.Position;
        }
    }
}

/// <summary>
/// <see cref="ConvexVoxelContinuations{TShape}"/> for a compound on the A side.
/// </summary>
internal unsafe struct CompoundVoxelContinuations<TCompoundA, TShape> : ICompoundPairContinuationHandler<NonconvexReduction>
    where TCompoundA : ICompoundShape
    where TShape : unmanaged, IVoxelShape
{
    public readonly CollisionContinuationType CollisionContinuationType => CollisionContinuationType.NonconvexReduction;

    public ref NonconvexReduction CreateContinuation<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, int totalChildCount, ref Buffer<ChildOverlapsCollection> pairOverlaps, ref Buffer<OverlapQueryForPair> pairQueries, in BoundsTestedPair pair, out int continuationIndex)
        where TCallbacks : struct, ICollisionCallbacks
        => ref collisionBatcher.NonconvexReductions.CreateContinuation(totalChildCount, collisionBatcher.Pool, out continuationIndex);

    public void GetChildAData<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, ref NonconvexReduction continuation, in BoundsTestedPair pair, int childIndexA,
        out NRigidPose childPoseA, out int childTypeA, out void* childShapeDataA)
        where TCallbacks : struct, ICollisionCallbacks
    {
        ref var compoundA = ref Unsafe.AsRef<TCompoundA>(pair.A);
        ref var compoundChildA = ref compoundA.GetChild(childIndexA);
        Compound.GetRotatedChildPose(compoundChildA.AsPose(), pair.OrientationA, out childPoseA);
        childTypeA = compoundChildA.ShapeIndex.Type;
        collisionBatcher.Shapes[childTypeA].GetShapeData(compoundChildA.ShapeIndex.Index, out childShapeDataA, out _);
    }

    public void ConfigureContinuationChild<TCallbacks>(
        ref CollisionBatcher<TCallbacks> collisionBatcher, ref NonconvexReduction continuation, int continuationChildIndex, in BoundsTestedPair pair, int childIndexA, int childTypeA, int childIndexB, in NRigidPose childPoseA,
        out NRigidPose childPoseB, out int childTypeB, out void* childShapeDataB)
        where TCallbacks : struct, ICollisionCallbacks
    {
        ref var continuationChild = ref continuation.Children[continuationChildIndex];
        ConvexVoxelContinuations<TShape>.GetChildData(ref collisionBatcher, pair, childTypeA, childIndexB, out childPoseB, out childTypeB, out childShapeDataB);
        if (pair.FlipMask < 0)
        {
            continuationChild.ChildIndexA = childIndexB;
            continuationChild.ChildIndexB = childIndexA;
            continuationChild.OffsetA = childPoseB.Position;
            continuationChild.OffsetB = childPoseA.Position;
        }
        else
        {
            continuationChild.ChildIndexA = childIndexA;
            continuationChild.ChildIndexB = childIndexB;
            continuationChild.OffsetA = childPoseA.Position;
            continuationChild.OffsetB = childPoseB.Position;
        }
    }
}
