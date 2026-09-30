// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuUtilities;
using BepuUtilities.Memory;
using NBodyVelocity = BepuPhysics.BodyVelocity;
using NRigidPose = BepuPhysics.RigidPose;
using Int2 = Stride.Core.Mathematics.Int2;

namespace Stride.BepuPhysics.Definitions.Heightfield;

internal static class HeightfieldCollisionTasks
{
    internal static void Register(CollisionTaskRegistry collisionTasks, SweepTaskRegistry sweepTasks)
    {
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<Sphere, SphereWide>());
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<Capsule, CapsuleWide>());
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<Box, BoxWide>());
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<Triangle, TriangleWide>());
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<Cylinder, CylinderWide>());
        collisionTasks.Register(new ConvexHeightfieldCollisionTask<ConvexHull, ConvexHullWide>());

        collisionTasks.Register(new CompoundHeightfieldCollisionTask<Compound>());
        collisionTasks.Register(new CompoundHeightfieldCollisionTask<BigCompound>());

        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<Sphere, SphereWide, Triangle, TriangleWide>());
        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<Capsule, CapsuleWide, Triangle, TriangleWide>());
        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<Box, BoxWide, Triangle, TriangleWide>());
        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<Cylinder, CylinderWide, Triangle, TriangleWide>());
        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<Triangle, TriangleWide, Triangle, TriangleWide>());
        sweepTasks.Register(new ConvexHeightfieldSweepTaskHeightfield<ConvexHull, ConvexHullWide, Triangle, TriangleWide>());
        sweepTasks.Register(new CompoundHeightfieldSweepTask<Compound, Triangle, TriangleWide>());
        sweepTasks.Register(new CompoundHeightfieldSweepTask<BigCompound, Triangle, TriangleWide>());
    }

    private class ConvexHeightfieldCollisionTask<TConvex, TConvexWide> : CollisionTask
        where TConvex : unmanaged, IConvexShape
        where TConvexWide : struct, IShapeWide<TConvex>
    {
        public ConvexHeightfieldCollisionTask()
        {
            BatchSize = 16;
            ShapeTypeIndexA = TConvex.TypeId;
            ShapeTypeIndexB = HeightfieldShape.TypeId;
            SubtaskGenerator = true;
            PairType = CollisionTaskPairType.BoundsTestedPair;
        }

        public unsafe override void ExecuteBatch<TCallbacks>(ref UntypedList batch, ref CollisionBatcher<TCallbacks> batcher)
        {
            var pairs = batch.Buffer.As<BoundsTestedPair>();
            Unsafe.SkipInit(out ConvexHeightfieldContinuations continuationHandler);
            //We perform all necessary bounding box computations and lookups up front. This helps avoid some instruction pipeline pressure at the cost of some extra data cache requirements.
            //Because of this, you need to be careful with the batch size on this collision task.
            FindLocalOverlaps(ref pairs, batch.Count, batcher.Pool, batcher.Shapes, batcher.Dt, out var overlaps);
            for (int i = 0; i < batch.Count; ++i)
            {
                ref var pairOverlaps = ref overlaps.GetOverlapsForPair(i);
                ref var pairQuery = ref overlaps.GetQueryForPair(i);
                ref var pair = ref pairs[i];
                if (pairOverlaps.Count > 0)
                {
                    ref var compound = ref Unsafe.AsRef<HeightfieldShape>(pair.B);

                    // Overlap is the amount of cell, each cell is split into two triangles
                    int triangles = pairOverlaps.Count;
                    int overlapMax = pairOverlaps.Count;

                    //If there are more overlaps than we can represent in the packed index, just ignore the surplus. This isn't wonderful, but it's better than an access violation.
                    Debug.Assert(triangles < PairContinuation.ExclusiveMaximumChildIndex, "Are there REALLY supposed to be that many overlaps? Might need to expand the packed representation if so.");
                    if (triangles >= PairContinuation.ExclusiveMaximumChildIndex)
                    {
                        triangles = PairContinuation.ExclusiveMaximumChildIndex - 1;
                        overlapMax = triangles;
                    }
                    ref var continuation = ref continuationHandler.CreateContinuation(ref batcher, triangles, pair, pairQuery, out var continuationIndex);

                    int nextContinuationChildIndex = 0;
                    for (int j = 0; j < overlapMax; j += 2)
                    {
                        var cell = new Int2(pairOverlaps.Overlaps[j + 0], pairOverlaps.Overlaps[j + 1]);
                        int childA = 0, childB = 0;
                        compound.GetLocalChild(cell, out var triangle, out var otherTriangle);

                        for (int k = 0; k < 2; k++, triangle = otherTriangle)
                        {
                            var continuationChildIndex = nextContinuationChildIndex++;
                            var subpairContinuation = new PairContinuation(pair.Continuation.PairId, childA, childB,
                                continuationHandler.CollisionContinuationType, continuationIndex, continuationChildIndex);
                            if (batcher.Callbacks.AllowCollisionTesting(pair.Continuation.PairId, childA, childB))
                            {
                                continuationHandler.ConfigureContinuationChild(ref batcher, ref continuation, continuationChildIndex, pair, ShapeTypeIndexA, triangle,
                                    out var compoundChildPose, out var compoundChildType, out var compoundChildShapeData);

                                var convexToChild = compoundChildPose.Position + pair.OffsetB;
                                if (pair.FlipMask < 0)
                                {
                                    //By reversing the order of the parameters, the manifold orientation is flipped. This compensates for the flip induced by order requirements on this task.
                                    batcher.AddDirectly(compoundChildType, ShapeTypeIndexA, compoundChildShapeData, pair.A,
                                        -convexToChild, compoundChildPose.Orientation, pair.OrientationA, pair.SpeculativeMargin, subpairContinuation);
                                }
                                else
                                {
                                    batcher.AddDirectly(ShapeTypeIndexA, compoundChildType, pair.A, compoundChildShapeData,
                                        convexToChild, pair.OrientationA, compoundChildPose.Orientation, pair.SpeculativeMargin, subpairContinuation);
                                }
                            }
                            else
                            {
                                batcher.ProcessUntestedSubpairConvexResult(ref subpairContinuation);
                            }
                        }
                    }
                }
                else
                {
                    batcher.ProcessEmptyResult(ref pair.Continuation);
                }
            }
            overlaps.Dispose(batcher.Pool);
        }

        private static unsafe void FindLocalOverlaps(ref Buffer<BoundsTestedPair> pairs, int pairCount, BufferPool pool, Shapes shapes, float dt, out ConvexCompoundTaskOverlaps overlaps)
        {
            overlaps = new ConvexCompoundTaskOverlaps(pool, pairCount);
            ref var pairsToTest = ref subpairQueries(ref overlaps);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "subpairQueries")]
            static extern ref Buffer<OverlapQueryForPair> subpairQueries(ref ConvexCompoundTaskOverlaps compoundPairOverlaps);

            Unsafe.SkipInit(out Vector3Wide offsetB);
            Unsafe.SkipInit(out QuaternionWide orientationA);
            Unsafe.SkipInit(out QuaternionWide orientationB);
            Unsafe.SkipInit(out Vector3Wide relativeLinearVelocityA);
            Unsafe.SkipInit(out Vector3Wide angularVelocityA);
            Unsafe.SkipInit(out Vector3Wide angularVelocityB);
            Unsafe.SkipInit(out Vector<float> maximumAllowedExpansion);
            Unsafe.SkipInit(out TConvexWide convexWide);
            if (convexWide.InternalAllocationSize > 0)
            {
                var memory = stackalloc byte[convexWide.InternalAllocationSize];
                convexWide.Initialize(new Buffer<byte>(memory, convexWide.InternalAllocationSize));
            }
            for (int i = 0; i < pairCount; i += Vector<float>.Count)
            {
                var count = pairCount - i;
                if (count > Vector<float>.Count)
                    count = Vector<float>.Count;

                //Compute the local bounding boxes using wide operations for the expansion work.
                //Doing quite a bit of gather work (and still quite a bit of scalar work). Very possible that a scalar path could win. TODO: test that.
                //TODO: Now that we're free of NS2.0, the transpose could be intrinsified.
                for (int j = 0; j < count; ++j)
                {
                    var pairIndex = i + j;
                    ref var pair = ref pairs[pairIndex];
                    pairsToTest[pairIndex].Container = pair.B;
                    Vector3Wide.WriteFirst(pair.OffsetB, ref GatherScatter.GetOffsetInstance(ref offsetB, j));
                    QuaternionWide.WriteFirst(pair.OrientationA, ref GatherScatter.GetOffsetInstance(ref orientationA, j));
                    QuaternionWide.WriteFirst(pair.OrientationB, ref GatherScatter.GetOffsetInstance(ref orientationB, j));
                    Vector3Wide.WriteFirst(pair.RelativeLinearVelocityA, ref GatherScatter.GetOffsetInstance(ref relativeLinearVelocityA, j));
                    Vector3Wide.WriteFirst(pair.AngularVelocityA, ref GatherScatter.GetOffsetInstance(ref angularVelocityA, j));
                    Vector3Wide.WriteFirst(pair.AngularVelocityB, ref GatherScatter.GetOffsetInstance(ref angularVelocityB, j));
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref maximumAllowedExpansion), j) = pair.MaximumExpansion;

                    convexWide.WriteSlot(j, Unsafe.AsRef<TConvex>(pair.A));
                }

                QuaternionWide.Conjugate(orientationB, out var inverseOrientationB);
                QuaternionWide.TransformWithoutOverlap(offsetB, inverseOrientationB, out var localOffsetB);
                QuaternionWide.ConcatenateWithoutOverlap(orientationA, inverseOrientationB, out var localOrientationA);
                QuaternionWide.TransformWithoutOverlap(relativeLinearVelocityA, inverseOrientationB, out var localRelativeLinearVelocityA);

                convexWide.GetBounds(ref localOrientationA, count, out var maximumRadius, out var maximumAngularExpansion, out var min, out var max);

                Vector3Wide.Negate(localOffsetB, out var localPositionA);
                BoundingBoxHelpers.ExpandLocalBoundingBoxes(ref min, ref max, Vector<float>.Zero, localPositionA, localRelativeLinearVelocityA, angularVelocityA, angularVelocityB, dt,
                    maximumRadius, maximumAngularExpansion, maximumAllowedExpansion);

                for (int j = 0; j < count; ++j)
                {
                    ref var pairToTest = ref pairsToTest[i + j];
                    Vector3Wide.ReadSlot(ref min, j, out pairToTest.Min);
                    Vector3Wide.ReadSlot(ref max, j, out pairToTest.Max);
                }
            }

            HeightfieldShape.FindLocalOverlaps<ConvexCompoundTaskOverlaps, ConvexCompoundOverlaps>(ref pairsToTest, pool, shapes, ref overlaps);
        }
    }

    private class CompoundHeightfieldCollisionTask<TCompoundA> : CollisionTask
        where TCompoundA : unmanaged, IShape, IBoundsQueryableCompound, ICompoundShape
    {
        public CompoundHeightfieldCollisionTask()
        {
            BatchSize = 16;
            ShapeTypeIndexA = TCompoundA.TypeId;
            ShapeTypeIndexB = HeightfieldShape.TypeId;
            SubtaskGenerator = true;
            PairType = CollisionTaskPairType.BoundsTestedPair;
        }

        public unsafe override void ExecuteBatch<TCallbacks>(ref UntypedList batch, ref CollisionBatcher<TCallbacks> batcher)
        {
            var pairs = batch.Buffer.As<BoundsTestedPair>();
            Unsafe.SkipInit(out CompoundHeightfieldContinuations<TCompoundA> continuationHandler);
            //We perform all necessary bounding box computations and lookups up front. This helps avoid some instruction pipeline pressure at the cost of some extra data cache requirements.
            //Because of this, you need to be careful with the batch size on this collision task.
            FindLocalOverlaps(ref pairs, batch.Count, batcher.Pool, batcher.Shapes, batcher.Dt, out var overlaps);

            for (int pairIndex = 0; pairIndex < batch.Count; ++pairIndex)
            {
                overlaps.GetPairOverlaps(pairIndex, out var pairOverlaps, out var subpairQueries);
                var triangles = pairOverlaps[0].Count;
                for (int j = 1; j < pairOverlaps.Length; ++j)
                {
                    triangles += pairOverlaps[j].Count;
                }

                ref var pair = ref pairs[pairIndex];
                if (triangles > 0)
                {
                    Debug.Assert(triangles < PairContinuation.ExclusiveMaximumChildIndex, "Are there REALLY supposed to be that many overlaps? Might need to expand the packed representation if so.");
                    ref var continuation = ref continuationHandler.CreateContinuation(ref batcher, triangles, ref pairOverlaps, ref subpairQueries, pair, out var continuationIndex);

                    var nextContinuationChildIndex = 0;
                    for (int j = 0; j < pairOverlaps.Length; ++j)
                    {
                        ref var childOverlaps = ref pairOverlaps[j];
                        if (childOverlaps.Count == 0)
                            continue;

                        continuationHandler.GetChildAData(ref batcher, ref continuation, pair, childOverlaps.ChildIndex, out var childPoseA, out var childTypeA, out var childShapeDataA);
                        //Note that we defer the region assignment until after the loop rather than using the triangleCount as the region count.
                        //That's because the user callback could cull some of the subpairs.
                        for (int k = 0; k < childOverlaps.Count; k += 2)
                        {
                            var cell = new Int2(childOverlaps.Overlaps[k + 0], childOverlaps.Overlaps[k + 1]);
                            //Note that we have to take into account whether we flipped the shapes to match the expected memory layout.
                            //The caller expects results according to the submitted pair order, not the batcher's memory layout order.
                            int childA, childB;
                            if (pair.FlipMask < 0)
                            {
                                childA = 0;
                                childB = childOverlaps.ChildIndex;
                            }
                            else
                            {
                                childA = childOverlaps.ChildIndex;
                                childB = 0;
                            }

                            Unsafe.AsRef<HeightfieldShape>(pair.B).GetLocalChild(cell, out var triangle, out var otherTriangle);

                            for (int i = 0; i < 2; i++, triangle = otherTriangle)
                            {
                                int continuationChildIndex = nextContinuationChildIndex;
                                nextContinuationChildIndex++;
                                if (continuationChildIndex >= PairContinuation.ExclusiveMaximumChildIndex)
                                {
                                    //If there are more overlaps than we can represent in the packed index, just ignore the surplus. This isn't wonderful, but it's better than an access violation.
                                    break;
                                }

                                var subpairContinuation = new PairContinuation(pair.Continuation.PairId, childA, childB,
                                    continuationHandler.CollisionContinuationType, continuationIndex, continuationChildIndex);
                                if (batcher.Callbacks.AllowCollisionTesting(pair.Continuation.PairId, childA, childB))
                                {
                                    continuationHandler.ConfigureContinuationChild(ref batcher, ref continuation, continuationChildIndex, pair, childOverlaps.ChildIndex, childTypeA, triangle,
                                        childPoseA, out var childPoseB, out var childTypeB, out var childShapeDataB);

                                    var childAToChildB = pair.OffsetB + childPoseB.Position - childPoseA.Position;
                                    if (pair.FlipMask < 0)
                                    {
                                        //By reversing the order of the parameters, the manifold orientation is flipped. This compensates for the flip induced by order requirements on this task.
                                        batcher.AddDirectly(childTypeB, childTypeA, childShapeDataB, childShapeDataA,
                                            -childAToChildB, childPoseB.Orientation, childPoseA.Orientation, pair.SpeculativeMargin, subpairContinuation);
                                    }
                                    else
                                    {
                                        batcher.AddDirectly(childTypeA, childTypeB, childShapeDataA, childShapeDataB,
                                            childAToChildB, childPoseA.Orientation, childPoseB.Orientation, pair.SpeculativeMargin, subpairContinuation);
                                    }
                                }
                                else
                                {
                                    batcher.ProcessUntestedSubpairConvexResult(ref subpairContinuation);
                                }
                            }
                        }
                    }
                }
                else
                {
                    batcher.ProcessEmptyResult(ref pair.Continuation);
                }
            }
            overlaps.Dispose(batcher.Pool);
            //Note that the triangle lists are not disposed here. Those are handed off to the continuations for further analysis.
        }

        private static unsafe void FindLocalOverlaps(ref Buffer<BoundsTestedPair> pairs, int pairCount, BufferPool pool, Shapes shapes, float dt, out CompoundPairOverlaps overlaps)
        {
            var totalCompoundChildCount = 0;
            for (int i = 0; i < pairCount; ++i)
            {
                totalCompoundChildCount += Unsafe.AsRef<TCompoundA>(pairs[i].A).ChildCount;
            }
            overlaps = new CompoundPairOverlaps(pool, pairCount, totalCompoundChildCount);
            ref var pairsToTest = ref pairQueries(ref overlaps);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "pairQueries")]
            static extern ref Buffer<OverlapQueryForPair> pairQueries(ref CompoundPairOverlaps compoundPairOverlaps);

            //Stack overflows are very possible with larger compounds! Guard against it.
            Buffer<SubpairData> subpairData;
            const int stackallocThreshold = 1024;
            if (totalCompoundChildCount <= stackallocThreshold)
            {
                var memory = stackalloc SubpairData[totalCompoundChildCount];
                subpairData = new Buffer<SubpairData>(memory, totalCompoundChildCount);
            }
            else
            {
                subpairData = new Buffer<SubpairData>(totalCompoundChildCount, pool);
            }
            int nextSubpairIndex = 0;
            for (int i = 0; i < pairCount; ++i)
            {
                ref var pair = ref pairs[i];
                ref var compoundA = ref Unsafe.AsRef<TCompoundA>(pair.A);
                overlaps.CreatePairOverlaps(compoundA.ChildCount);
                for (int j = 0; j < compoundA.ChildCount; ++j)
                {
                    var subpairIndex = nextSubpairIndex++;
                    overlaps.GetOverlapsForPair(subpairIndex).ChildIndex = j;
                    pairsToTest[subpairIndex].Container = pair.B;
                    ref var subpair = ref subpairData[subpairIndex];
                    subpair.Pair = (BoundsTestedPair*)Unsafe.AsPointer(ref pair);
                    subpair.Child = (CompoundChild*)Unsafe.AsPointer(ref compoundA.GetChild(j));
                }
            }

            Unsafe.SkipInit(out Vector3Wide offsetB);
            Unsafe.SkipInit(out QuaternionWide orientationA);
            Unsafe.SkipInit(out QuaternionWide orientationB);
            Unsafe.SkipInit(out Vector3Wide relativeLinearVelocityA);
            Unsafe.SkipInit(out Vector3Wide angularVelocityA);
            Unsafe.SkipInit(out Vector3Wide angularVelocityB);
            Unsafe.SkipInit(out Vector<float> maximumAllowedExpansion);
            Unsafe.SkipInit(out Vector<float> maximumRadius);
            Unsafe.SkipInit(out Vector<float> maximumAngularExpansion);
            Unsafe.SkipInit(out RigidPoseWide localPosesA);
            Unsafe.SkipInit(out Vector3Wide mins);
            Unsafe.SkipInit(out Vector3Wide maxes);
            for (int i = 0; i < totalCompoundChildCount; i += Vector<float>.Count)
            {
                var count = totalCompoundChildCount - i;
                if (count > Vector<float>.Count)
                    count = Vector<float>.Count;

                //Compute the local bounding boxes using wide operations for the expansion work.
                //Doing quite a bit of gather work (and still quite a bit of scalar work). Very possible that a scalar path could win. TODO: test that.
                for (int j = 0; j < count; ++j)
                {
                    var subpairIndex = i + j;
                    ref var subpair = ref subpairData[subpairIndex];
                    Vector3Wide.WriteFirst(subpair.Pair->OffsetB, ref GatherScatter.GetOffsetInstance(ref offsetB, j));
                    QuaternionWide.WriteFirst(subpair.Pair->OrientationA, ref GatherScatter.GetOffsetInstance(ref orientationA, j));
                    QuaternionWide.WriteFirst(subpair.Pair->OrientationB, ref GatherScatter.GetOffsetInstance(ref orientationB, j));
                    Vector3Wide.WriteFirst(subpair.Pair->RelativeLinearVelocityA, ref GatherScatter.GetOffsetInstance(ref relativeLinearVelocityA, j));
                    Vector3Wide.WriteFirst(subpair.Pair->AngularVelocityA, ref GatherScatter.GetOffsetInstance(ref angularVelocityA, j));
                    Vector3Wide.WriteFirst(subpair.Pair->AngularVelocityB, ref GatherScatter.GetOffsetInstance(ref angularVelocityB, j));
                    Unsafe.Add(ref Unsafe.As<Vector<float>, float>(ref maximumAllowedExpansion), j) = subpair.Pair->MaximumExpansion;

                    RigidPoseWide.WriteFirst((*subpair.Child).AsPose(), ref GatherScatter.GetOffsetInstance(ref localPosesA, j));
                }

                QuaternionWide.Conjugate(orientationB, out var toLocalB);
                QuaternionWide.ConcatenateWithoutOverlap(orientationA, toLocalB, out var localOrientationsA);
                QuaternionWide.ConcatenateWithoutOverlap(localPosesA.Orientation, localOrientationsA, out var localChildOrientationsA);
                QuaternionWide.TransformWithoutOverlap(localPosesA.Position, localOrientationsA, out var localOffsetA);
                QuaternionWide.TransformWithoutOverlap(offsetB, toLocalB, out var localOffsetB);
                Vector3Wide.Subtract(localOffsetA, localOffsetB, out var localPositionsA);

                for (int j = 0; j < count; ++j)
                {
                    var shapeIndex = subpairData[i + j].Child->ShapeIndex;
                    QuaternionWide.ReadFirst(GatherScatter.GetOffsetInstance(ref localChildOrientationsA, j), out var localChildOrientationA);
                    ComputeBounds(shapes[shapeIndex.Type], shapeIndex.Index, localChildOrientationA,
                        out GatherScatter.Get(ref maximumRadius, j),
                        out GatherScatter.Get(ref maximumAngularExpansion, j), out Vector3 min, out Vector3 max);
                    Vector3Wide.WriteFirst(min, ref GatherScatter.GetOffsetInstance(ref mins, j));
                    Vector3Wide.WriteFirst(max, ref GatherScatter.GetOffsetInstance(ref maxes, j));

                    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "ComputeBounds")]
                    static extern void ComputeBounds(ShapeBatch shapes,
                        int shapeIndex,
                        Quaternion orientation,
                        out float maximumRadius,
                        out float maximumAngularExpansion,
                        out Vector3 min,
                        out Vector3 max);
                }

                QuaternionWide.TransformWithoutOverlap(relativeLinearVelocityA, toLocalB, out var localRelativeLinearVelocityA);
                Vector3Wide.Length(localOffsetA, out var radiusA);
                BoundingBoxHelpers.ExpandLocalBoundingBoxes(ref mins, ref maxes, radiusA, localPositionsA, localRelativeLinearVelocityA, angularVelocityA, angularVelocityB, dt,
                    maximumRadius, maximumAngularExpansion, maximumAllowedExpansion);

                for (int j = 0; j < count; ++j)
                {
                    ref var pairToTest = ref pairsToTest[i + j];
                    Vector3Wide.ReadSlot(ref mins, j, out pairToTest.Min);
                    Vector3Wide.ReadSlot(ref maxes, j, out pairToTest.Max);
                }
            }
            //Doesn't matter what mesh/compound instance is used for the function; just using it as a source of the function.
            Debug.Assert(totalCompoundChildCount > 0);
            HeightfieldShape.FindLocalOverlaps<CompoundPairOverlaps, ChildOverlapsCollection>(ref pairsToTest, pool, shapes, ref overlaps);

            if (subpairData.Length > stackallocThreshold)
                subpairData.Dispose(pool);
        }

        private struct SubpairData
        {
            public unsafe BoundsTestedPair* Pair;
            public unsafe CompoundChild* Child;
        }
    }

    public struct CompoundPairSweepOverlaps
    {
        Buffer<ChildOverlapsCollection> childOverlaps;
        public readonly int ChildCount;
        public CompoundPairSweepOverlaps(BufferPool pool, int childCount)
        {
            ChildCount = childCount;
            pool.Take(childCount, out childOverlaps);
            //We rely on the length being zero to begin with for lazy initialization.
            childOverlaps.Clear(0, childCount);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref ChildOverlapsCollection GetOverlapsForChild(int pairIndex)
        {
            return ref childOverlaps[pairIndex];
        }

        public void Dispose(BufferPool pool)
        {
            for (int i = 0; i < ChildCount; ++i)
            {
                childOverlaps[i].Dispose(pool);
            }
            pool.Return(ref childOverlaps);
        }
    }

    private class ConvexHeightfieldSweepTaskHeightfield<TConvex, TConvexWide, TChildType, TChildTypeWide> : SweepTask
        where TConvex : unmanaged, IConvexShape
        where TConvexWide : unmanaged, IShapeWide<TConvex>
        where TChildType : unmanaged, IConvexShape
        where TChildTypeWide : unmanaged, IShapeWide<TChildType>
    {
        public ConvexHeightfieldSweepTaskHeightfield()
        {
            ShapeTypeIndexA = TConvex.TypeId;
            ShapeTypeIndexB = HeightfieldShape.TypeId;
        }

        protected unsafe override bool PreorderedTypeSweep<TSweepFilter>(
            void* shapeDataA, Quaternion orientationA, in NBodyVelocity velocityA,
            void* shapeDataB, Vector3 offsetB, Quaternion orientationB, in NBodyVelocity velocityB,
            float maximumT, float minimumProgression, float convergenceThreshold, int maximumIterationCount,
            bool flipRequired, ref TSweepFilter filter, Shapes shapes, SweepTaskRegistry sweepTasks, BufferPool pool, out float t0, out float t1, out Vector3 hitLocation, out Vector3 hitNormal)
        {
            ref var compound = ref Unsafe.AsRef<HeightfieldShape>(shapeDataB);
            t0 = float.MaxValue;
            t1 = float.MaxValue;
            hitLocation = new Vector3();
            hitNormal = new Vector3();
            var task = sweepTasks.GetTask(ShapeTypeIndexA, TChildType.TypeId);
            if (task != null)
            {
                BoundingBoxHelpers.GetLocalBoundingBoxForSweep(ref Unsafe.AsRef<TConvex>(shapeDataA), orientationA, velocityA, offsetB, orientationB, velocityB, maximumT, out var sweep, out var min, out var max);

                ChildOverlapsCollection overlaps = default;
                compound.FindLocalOverlaps(min, max, sweep, maximumT, pool, shapes, ref overlaps);
                for (int i = 0; i < overlaps.Count; i += 2)
                {
                    var cell = new Int2(overlaps.Overlaps[i + 0], overlaps.Overlaps[i + 1]);
                    if (filter.AllowTest(0, 0))
                    {
                        compound.GetPosedLocalChild(cell, out var triangle, out var otherTriangle, out var trianglePose, out var otherTrianglePose);
                        for (int j = 0; j < 2; j++, triangle = otherTriangle, trianglePose = otherTrianglePose)
                        {
                            if (task.Sweep(
                                    shapeDataA, ShapeTypeIndexA, NRigidPose.Identity, orientationA, velocityA,
                                    Unsafe.AsPointer(ref triangle), TChildType.TypeId, trianglePose, offsetB, orientationB, velocityB,
                                    maximumT, minimumProgression, convergenceThreshold, maximumIterationCount,
                                    out var t0Candidate, out var t1Candidate, out var hitLocationCandidate, out var hitNormalCandidate))
                            {
                                //Note that we use t1 to determine whether to accept the new location. In other words, we're choosing to keep sweeps that have the earliest time of intersection.
                                //(t0 is *not* intersecting for any initially separated pair.)
                                if (t1Candidate < t1)
                                {
                                    t0 = t0Candidate;
                                    t1 = t1Candidate;
                                    hitLocation = hitLocationCandidate;
                                    hitNormal = hitNormalCandidate;
                                }
                            }
                        }
                    }
                }
                overlaps.Dispose(pool);
            }
            return t1 < float.MaxValue;
        }

        protected override unsafe bool PreorderedTypeSweep(void* shapeDataA, in NRigidPose localPoseA, Quaternion orientationA, in NBodyVelocity velocityA, void* shapeDataB, in NRigidPose localPoseB, Vector3 offsetB, Quaternion orientationB, in NBodyVelocity velocityB, float maximumT, float minimumProgression, float convergenceThreshold, int maximumIterationCount, out float t0, out float t1, out Vector3 hitLocation, out Vector3 hitNormal)
        {
            throw new NotImplementedException("Compounds can never be nested; this should never be called.");
        }
    }

    private class CompoundHeightfieldSweepTask<TCompoundA, TChildShapeB, TChildShapeWideB> : SweepTask
        where TCompoundA : unmanaged, ICompoundShape
        where TChildShapeB : unmanaged, IConvexShape
        where TChildShapeWideB : unmanaged, IShapeWide<TChildShapeB>
    {
        public CompoundHeightfieldSweepTask()
        {
            ShapeTypeIndexA = TCompoundA.TypeId;
            ShapeTypeIndexB = HeightfieldShape.TypeId;
        }

        protected unsafe override bool PreorderedTypeSweep<TSweepFilter>(
            void* shapeDataA, Quaternion orientationA, in NBodyVelocity velocityA,
            void* shapeDataB, Vector3 offsetB, Quaternion orientationB, in NBodyVelocity velocityB,
            float maximumT, float minimumProgression, float convergenceThreshold, int maximumIterationCount,
            bool flipRequired, ref TSweepFilter filter, Shapes shapes, SweepTaskRegistry sweepTasks, BufferPool pool, out float t0, out float t1, out Vector3 hitLocation, out Vector3 hitNormal)
        {
            ref var compoundB = ref Unsafe.AsRef<HeightfieldShape>(shapeDataB);
            t0 = float.MaxValue;
            t1 = float.MaxValue;
            hitLocation = new Vector3();
            hitNormal = new Vector3();
            ref var compoundA = ref Unsafe.AsRef<TCompoundA>(shapeDataA);

            var overlaps = new CompoundPairSweepOverlaps(pool, compoundA.ChildCount);
            for (int i1 = 0; i1 < compoundA.ChildCount; ++i1)
            {
                ref var child = ref compoundA.GetChild(i1);
                BoundingBoxHelpers.GetLocalBoundingBoxForSweep(
                    child.ShapeIndex, shapes, child.AsPose(), orientationA, velocityA,
                    offsetB, orientationB, velocityB, maximumT, out var sweep, out var min, out var max);
                ref var childOverlaps1 = ref overlaps.GetOverlapsForChild(i1);
                childOverlaps1.ChildIndex = i1;
                compoundB.FindLocalOverlaps(min, max, sweep, maximumT, pool, shapes, ref childOverlaps1);
            }

            for (int i = 0; i < overlaps.ChildCount; ++i)
            {
                ref var childOverlaps = ref overlaps.GetOverlapsForChild(i);
                for (int j = 0; j < childOverlaps.Count; j += 2)
                {
                    var cell = new Int2(childOverlaps.Overlaps[j + 0], childOverlaps.Overlaps[j + 1]);
                    if (filter.AllowTest(flipRequired ? 0 : childOverlaps.ChildIndex, flipRequired ? childOverlaps.ChildIndex : 0))
                    {
                        ref var compoundChild = ref compoundA.GetChild(childOverlaps.ChildIndex);
                        var compoundChildType = compoundChild.ShapeIndex.Type;
                        var task = sweepTasks.GetTask(compoundChildType, TChildShapeB.TypeId);
                        shapes[compoundChildType].GetShapeData(compoundChild.ShapeIndex.Index, out var compoundChildShapeData, out _);

                        compoundB.GetPosedLocalChild(cell, out var triangle, out var otherTriangle, out var trianglePose, out var otherTrianglePose);
                        for (int k = 0; k < 2; k++, triangle = otherTriangle, trianglePose = otherTrianglePose)
                        {
                            if (task.Sweep(
                                    compoundChildShapeData, compoundChildType, compoundChild.AsPose(), orientationA, velocityA,
                                    Unsafe.AsPointer(ref triangle), TChildShapeB.TypeId, trianglePose, offsetB, orientationB, velocityB,
                                    maximumT, minimumProgression, convergenceThreshold, maximumIterationCount,
                                    out var t0Candidate, out var t1Candidate, out var hitLocationCandidate, out var hitNormalCandidate))
                            {
                                //Note that we use t1 to determine whether to accept the new location. In other words, we're choosing to keep sweeps that have the earliest time of intersection.
                                //(t0 is *not* intersecting for any initially separated pair.)
                                if (t1Candidate < t1)
                                {
                                    t0 = t0Candidate;
                                    t1 = t1Candidate;
                                    hitLocation = hitLocationCandidate;
                                    hitNormal = hitNormalCandidate;
                                }
                            }
                        }
                    }
                }
            }
            overlaps.Dispose(pool);
            return t1 < float.MaxValue;
        }

        protected override unsafe bool PreorderedTypeSweep(void* shapeDataA, in NRigidPose localPoseA, Quaternion orientationA, in NBodyVelocity velocityA, void* shapeDataB, in NRigidPose localPoseB, Vector3 offsetB, Quaternion orientationB, in NBodyVelocity velocityB, float maximumT, float minimumProgression, float convergenceThreshold, int maximumIterationCount, out float t0, out float t1, out Vector3 hitLocation, out Vector3 hitNormal)
        {
            throw new NotImplementedException("Compounds and meshes can never be nested; this should never be called.");
        }
    }

    public unsafe struct CompoundHeightfieldContinuations<TCompound>
        where TCompound : struct, ICompoundShape
    {
        public CollisionContinuationType CollisionContinuationType => CollisionContinuationType.CompoundMeshReduction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref CompoundMeshReduction CreateContinuation<TCallbacks>(
            ref CollisionBatcher<TCallbacks> collisionBatcher, int totalChildCount, ref Buffer<ChildOverlapsCollection> pairOverlaps, ref Buffer<OverlapQueryForPair> pairQueries, in BoundsTestedPair pair, out int continuationIndex)
            where TCallbacks : struct, ICollisionCallbacks
        {
            ref var continuation = ref collisionBatcher.CompoundMeshReductions.CreateContinuation(totalChildCount, collisionBatcher.Pool, out continuationIndex);
            //Pass ownership of the triangle and region buffers to the continuation. It'll dispose of the buffer.
            collisionBatcher.Pool.Take(totalChildCount, out continuation.Triangles);
            collisionBatcher.Pool.Take(pairOverlaps.Length, out continuation.ChildManifoldRegions);
            collisionBatcher.Pool.Take(pairOverlaps.Length, out continuation.QueryBounds);
            continuation.RegionCount = pairOverlaps.Length;
            continuation.MeshOrientation = pair.OrientationB;
            continuation.Mesh = pair.B;
            continuation.FindLocalOverlapsThunk = &HeightfieldReductionThunks.FindLocalOverlapsImpl;
            continuation.GetLocalChildThunk = &HeightfieldReductionThunks.GetLocalChildImpl;
            //A flip is required in mesh reduction whenever contacts are being generated as if the triangle is in slot B, which is whenever this pair has *not* been flipped.
            continuation.RequiresFlip = pair.FlipMask == 0;

            //All regions must be assigned ahead of time. Some trailing regions may be empty, so the dispatch may occur before all children are visited in the later loop.
            //That would result in potentially uninitialized values in region counts.
            int nextContinuationChildIndex = 0;
            Debug.Assert(pairOverlaps.Length == pairQueries.Length);
            for (int j = 0; j < pairOverlaps.Length; ++j)
            {
                ref var childOverlaps = ref pairOverlaps[j];
                int trianglesCount = childOverlaps.Count;
                continuation.ChildManifoldRegions[j] = (nextContinuationChildIndex, trianglesCount);
                nextContinuationChildIndex += trianglesCount;
                ref var continuationBounds = ref continuation.QueryBounds[j];
                ref var sourceBounds = ref pairQueries[j];
                continuationBounds.Min = sourceBounds.Min;
                continuationBounds.Max = sourceBounds.Max;
            }
            return ref continuation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetChildAData<TCallbacks>(ref CollisionBatcher<TCallbacks> collisionBatcher, ref CompoundMeshReduction continuation, in BoundsTestedPair pair, int childIndexA,
            out NRigidPose childPoseA, out int childTypeA, out void* childShapeDataA)
            where TCallbacks : struct, ICollisionCallbacks
        {
            ref var compound = ref Unsafe.AsRef<TCompound>(pair.A);
            ref var compoundChild = ref compound.GetChild(childIndexA);
            Compound.GetRotatedChildPose(compoundChild.LocalPosition, compoundChild.LocalOrientation, pair.OrientationA, out childPoseA);
            childTypeA = compoundChild.ShapeIndex.Type;
            collisionBatcher.Shapes[childTypeA].GetShapeData(compoundChild.ShapeIndex.Index, out childShapeDataA, out _);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ConfigureContinuationChild<TCallbacks>(
            ref CollisionBatcher<TCallbacks> collisionBatcher, ref CompoundMeshReduction continuation, int continuationChildIndex, in BoundsTestedPair pair, int childIndexA, int childTypeA, Triangle childTriangle, in NRigidPose childPoseA,
            out NRigidPose childPoseB, out int childTypeB, out void* childShapeDataB)
            where TCallbacks : struct, ICollisionCallbacks
        {
            //Note that the triangles list persists until the continuation completes, which means the memory will be validly accessible for all of the spawned collision tasks.
            //In other words, we can pass a pointer to it to avoid the need for additional batcher shape copying.
            ref var triangle = ref continuation.Triangles[continuationChildIndex];
            childShapeDataB = Unsafe.AsPointer(ref triangle);
            childTypeB = Triangle.TypeId;

            continuation.Triangles[continuationChildIndex] = childTriangle;

            ref var continuationChild = ref continuation.Inner.Children[continuationChildIndex];
            //In meshes, the triangle's vertices already contain the offset, so there is no additional offset.
            childPoseB = new NRigidPose(default, pair.OrientationB);
            if (pair.FlipMask < 0)
            {
                continuationChild.ChildIndexA = 0;
                continuationChild.ChildIndexB = childIndexA;
                continuationChild.OffsetA = childPoseB.Position;
                continuationChild.OffsetB = childPoseA.Position;
            }
            else
            {
                continuationChild.ChildIndexA = childIndexA;
                continuationChild.ChildIndexB = 0;
                continuationChild.OffsetA = childPoseA.Position;
                continuationChild.OffsetB = childPoseB.Position;
            }
        }
    }

    public struct ConvexHeightfieldContinuations
    {
        public CollisionContinuationType CollisionContinuationType => CollisionContinuationType.MeshReduction;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe ref MeshReduction CreateContinuation<TCallbacks>(
            ref CollisionBatcher<TCallbacks> collisionBatcher, int childCount, in BoundsTestedPair pair, in OverlapQueryForPair pairQuery, out int continuationIndex)
            where TCallbacks : struct, ICollisionCallbacks
        {
            ref var continuation = ref collisionBatcher.MeshReductions.CreateContinuation(childCount, collisionBatcher.Pool, out continuationIndex);
            //Pass ownership of the triangle and region buffers to the continuation. It'll dispose of the buffer.
            collisionBatcher.Pool.Take(childCount, out continuation.Triangles);
            continuation.MeshOrientation = pair.OrientationB;
            //A flip is required in mesh reduction whenever contacts are being generated as if the triangle is in slot B, which is whenever this pair has *not* been flipped.
            continuation.RequiresFlip = pair.FlipMask == 0;
            continuation.QueryBounds.Min = pairQuery.Min;
            continuation.QueryBounds.Max = pairQuery.Max;
            continuation.Mesh = pairQuery.Container;
            continuation.FindLocalOverlapsThunk = &HeightfieldReductionThunks.FindLocalOverlapsImpl;
            continuation.GetLocalChildThunk = &HeightfieldReductionThunks.GetLocalChildImpl;
            return ref continuation;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void ConfigureContinuationChild<TCallbacks>(
            ref CollisionBatcher<TCallbacks> collisionBatcher,
            ref MeshReduction continuation,
            int continuationChildIndex,
            in BoundsTestedPair pair,
            int shapeTypeA,
            Triangle childTriangle,
            out NRigidPose childPoseB,
            out int childTypeB,
            out void* childShapeDataB)
            where TCallbacks : struct, ICollisionCallbacks
        {
            //Note that the triangles list persists until the continuation completes, which means the memory will be validly accessible for all of the spawned collision tasks.
            //In other words, we can pass a pointer to it to avoid the need for additional batcher shape copying.
            ref var triangle = ref continuation.Triangles[continuationChildIndex];
            childShapeDataB = Unsafe.AsPointer(ref triangle);
            childTypeB = Triangle.TypeId;

            continuation.Triangles[continuationChildIndex] = childTriangle;

            ref var continuationChild = ref continuation.Inner.Children[continuationChildIndex];
            //Triangles already have their local pose baked into their vertices, so we just need the orientation.
            childPoseB = new NRigidPose(default, pair.OrientationB);
            continuationChild.OffsetA = default;
            continuationChild.OffsetB = default;
            if (pair.FlipMask < 0)
            {
                continuationChild.ChildIndexA = 0;
                continuationChild.ChildIndexB = 0;
            }
            else
            {
                continuationChild.ChildIndexA = 0;
                continuationChild.ChildIndexB = 0;
            }
        }
    }

    private static unsafe class HeightfieldReductionThunks
    {
        // Doing this workaround as we would otherwise have to allocate in continuations, and somehow hook into the disposal logic of continuations,
        // which afaict, is impossible from outside
        [ThreadStatic]
        private static uint counterStart, counterIncr;

        [ThreadStatic]
        private static int[]? buffer;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FindLocalOverlapsImpl(void* mesh, Vector3 min, Vector3 max, BufferPool pool, Shapes shapes, ref MeshReduction.ChildEnumerator enumerator)
        {
            buffer ??= new int[128];
            var heightfieldBufferCollector = new HeightfieldBufferCollector { Enumerator = ref enumerator };
            counterStart = counterIncr;
            Unsafe.AsRef<HeightfieldShape>(mesh).FindLocalOverlaps(min, max, pool, shapes, ref heightfieldBufferCollector);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetLocalChildImpl(void* mesh, int childIndex, out Triangle triangle)
        {
            uint childIndexUint = (uint)childIndex - counterStart;
            uint isNotEven = childIndexUint % 2;
            uint even = childIndexUint - isNotEven;
            var cell = new Int2(buffer![even + 0], buffer[even + 1]);
            Unsafe.AsRef<HeightfieldShape>(mesh).GetLocalChild(cell, out var tri0, out var tri1);
            triangle = isNotEven == 0 ? tri0 : tri1;
        }

        /// <summary>
        /// <see cref="HeightfieldReductionThunks.GetLocalChildImpl"/> needs to fetch indices by pair to act as cells, mesh reduction only provides them one by one.
        /// We have to collect them into another array and access them through there from the index provided
        /// </summary>
        private ref struct HeightfieldBufferCollector : IBreakableForEach<int>
        {
            public required ref MeshReduction.ChildEnumerator Enumerator;
            public bool LoopBody(int i)
            {
                buffer![counterIncr - counterStart] = i;
                counterIncr++;
                return Enumerator.LoopBody((int)(counterIncr - 1));
            }
        }
    }
}
