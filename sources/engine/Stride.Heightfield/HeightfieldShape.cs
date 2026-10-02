// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using Int2 = Stride.Core.Mathematics.Int2;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.Heightfield;

/// <summary>
/// A collider shape representing a subdivided plane made up of triangles whose vertices are offset based on a user provided function.
/// It holds very little data, as both triangles and vertices are not concrete data, rather they are emitted during queries.
/// </summary>
public unsafe struct HeightfieldShape : IShape
{
    #warning todo, better scheme for this
    public static int TypeId => 32;

    /// <summary>Distance between individual samples, Size / <see cref="Subdivision"/></summary>
    public float SampleInterval;

    /// <summary>
    /// How many individual height samples the heightfield is made up of on one axis,
    /// amount of samples in total would be <see cref="Subdivision"/>^2.
    /// </summary>
    /// <remarks>
    /// Note that the amount of quads along an axis would be (<see cref="Subdivision"/>-1) as each quad bridges between two samples.
    /// </remarks>
    public int Subdivision;

    /// <summary>
    /// The rock bottom, or lower bounds of this field. Assumed to be constant. May be negative
    /// </summary>
    public float MinHeight;

    /// <summary>
    /// The apex, or the highest bounds of this field. Assumed to be constant. May be negative
    /// </summary>
    public float MaxHeight;

    /// <summary>A GCHandle pointing to a <see cref="IHeightfieldSampler"/></summary>
    public GCHandle Sampler;

    /// <summary>The GCHandle of <see cref="CoarseBlocksAddress"/></summary>
    public GCHandle CoarseBlocksGCHandle;

    /// <summary>
    /// <see cref="MinHeight"/> and <see cref="MaxHeight"/> define the bounding box of this heightfield, and these coarse blocks define sub-bounding boxes that encompass <see cref="CoarseBlocksSubdivision"/>^2 heightfield points.
    /// They are laid out in [x + y * <see cref="CoarseBlockInterval"/>] and may extend slightly farther than the heightfield points when <see cref="Subdivision"/> is
    /// not evenly divisible by <see cref="CoarseBlocksSubdivision"/>
    /// </summary>
    public HeightRange* CoarseBlocksAddress;

    /// <summary>
    /// Amount of coarse blocks along one axis.
    /// <see cref="CoarseBlocksAddress"/> holds <see cref="CoarseBlocksSubdivision"/>^2 blocks.
    /// Each block overlaps <see cref="CoarseBlockInterval"/>^2 height samples
    /// </summary>
    public int CoarseBlocksSubdivision;

    /// <summary>
    /// Amount of <see cref="Subdivision"/> along one axis per blocks.
    /// Each block overlaps <see cref="CoarseBlockInterval"/>^2 height samples
    /// </summary>
    public int CoarseBlockInterval;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly ref HeightRange BlockOf(int cellX, int cellZ)
    {
        return ref CoarseBlocksAddress[cellZ / CoarseBlockInterval * CoarseBlocksSubdivision + cellX / CoarseBlockInterval];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly void GetCellCorners(Int2 cell, out Vector3 p00, out Vector3 p10, out Vector3 p01, out Vector3 p11)
    {
        Span<Sample> samples = stackalloc Sample[4]
        {
            new(cell, 0),
            new(cell + new Int2(1, 0), 0),
            new(cell + new Int2(0, 1), 0),
            new(cell + new Int2(1, 1), 0),
        };

        ((IHeightfieldSampler)Sampler.Target!).FillSamples(samples);

        float x0 = cell.X * SampleInterval;
        float z0 = cell.Y * SampleInterval;
        float x1 = x0 + SampleInterval;
        float z1 = z0 + SampleInterval;

        p00 = new Vector3(x0, samples[0].Height, z0);
        p10 = new Vector3(x1, samples[1].Height, z0);
        p01 = new Vector3(x0, samples[2].Height, z1);
        p11 = new Vector3(x1, samples[3].Height, z1);
    }

    public readonly void GetLocalChild(Int2 cell, out Triangle tri0, out Triangle tri1)
    {
        // TODO: Perf
        GetCellCorners(cell, out var p00, out var p10, out var p01, out var p11);
        (tri0.A, tri0.B, tri0.C) = (p00, p10, p01);
        (tri1.A, tri1.B, tri1.C) = (p01, p10, p11);
    }

    public readonly void GetPosedLocalChild(Int2 cell, out Triangle tri0, out Triangle tri1, out NRigidPose childPoseA, out NRigidPose childPoseB)
    {
        GetLocalChild(cell, out tri0, out tri1);

        childPoseA = (tri0.A + tri0.B + tri0.C) * (1f / 3f);
        childPoseB = (tri1.A + tri1.B + tri1.C) * (1f / 3f);

        tri0.A -= childPoseA.Position;
        tri0.B -= childPoseA.Position;
        tri0.C -= childPoseA.Position;

        tri1.A -= childPoseB.Position;
        tri1.B -= childPoseB.Position;
        tri1.C -= childPoseB.Position;
    }

    public readonly void ComputeBounds(Quaternion orientation, out Vector3 min, out Vector3 max)
    {
        // TODO: Perf

        var localMin = new Vector3(0, MinHeight, 0);
        var localMax = new Vector3(Subdivision * SampleInterval, MaxHeight, Subdivision * SampleInterval);
        if (orientation == Quaternion.Identity)
        {
            min = localMin;
            max = localMax;
            return;
        }

        Matrix3x3.CreateFromQuaternion(orientation, out var r);
        min = new Vector3(float.MaxValue);
        max = new Vector3(-float.MaxValue);
        for (int i = 0; i < 8; i++)
        {
            var corner = new Vector3(
                (i & 1) != 0 ? localMax.X : localMin.X,
                (i & 2) != 0 ? localMax.Y : localMin.Y,
                (i & 4) != 0 ? localMax.Z : localMin.Z);
            Matrix3x3.Transform(corner, r, out var t);
            min = Vector3.Min(min, t);
            max = Vector3.Max(max, t);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool Clip(float minX, float maxX, float minZ, float maxZ, out int sX0, out int sX1, out int sZ0, out int sZ1)
    {
        // TODO: Perf

        sX0 = sX1 = sZ0 = sZ1 = 0;
        float exMinX = 0, exMaxX = Subdivision * SampleInterval;
        float exMinZ = 0, exMaxZ = Subdivision * SampleInterval;
        float nx = MathF.Max(minX, exMinX), xx = MathF.Min(maxX, exMaxX);
        float nz = MathF.Max(minZ, exMinZ), zz = MathF.Min(maxZ, exMaxZ);
        if (xx < nx || zz < nz)
            return false;

        float invCell = 1f / SampleInterval;
        sX0 = Math.Clamp((int)MathF.Floor(nx * invCell), 0, Subdivision - 1);
        sX1 = Math.Clamp((int)MathF.Floor(xx * invCell), 0, Subdivision - 1);
        sZ0 = Math.Clamp((int)MathF.Floor(nz * invCell), 0, Subdivision - 1);
        sZ1 = Math.Clamp((int)MathF.Floor(zz * invCell), 0, Subdivision - 1);
        return true;
    }

    public static void FindLocalOverlaps<TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
        where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
        where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
    {
        var enumerator = new ShapeTreeOverlapEnumerator<TSubpairOverlaps> { Pool = pool };
        for (int i = 0; i < pairs.Length; ++i)
        {
            ref var pair = ref pairs[i];
            ref var heightfield = ref Unsafe.AsRef<HeightfieldShape>(pair.Container);

            enumerator.Overlaps = ref overlaps.GetOverlapsForPair(i);
            heightfield.EnumerateChildrenInAabb(pair.Min, pair.Max, ref enumerator);
        }
    }

    public readonly void FindLocalOverlaps<TEnumerator>(Vector3 min, Vector3 max, BufferPool pool, Shapes shapes, ref TEnumerator enumerator)
        where TEnumerator : IBreakableForEach<int>, allows ref struct
    {
        EnumerateChildrenInAabb(min, max, ref enumerator);
    }

    public readonly void RayTest<TRayHitHandler>(in NRigidPose pose, in RayData ray, ref float maximumT, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        Matrix3x3.CreateFromQuaternion(pose.Orientation, out var orientation);
        MarchRay(ray, pose, orientation, ref maximumT, ref hitHandler);
    }

    public readonly void RayTest<TRayHitHandler>(in NRigidPose pose, ref RaySource rays, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        Matrix3x3.CreateFromQuaternion(pose.Orientation, out var orientation);
        for (int i = 0; i < rays.RayCount; ++i)
        {
            rays.GetRay(i, out var ray, out var maxT);
            MarchRay(*ray, pose, orientation, ref *maxT, ref hitHandler);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void EnumerateChildrenInAabb<TEnumerator>(Vector3 min, Vector3 max, ref TEnumerator enumerator)
        where TEnumerator : IBreakableForEach<int>, allows ref struct
    {
        if (Clip(min.X, max.X, min.Z, max.Z, out int sX0, out int sX1, out int sZ0, out int sZ1) == false)
            return;

        if (max.Y < MinHeight || min.Y > MaxHeight)
            return;

        for (int cz = sZ0; cz <= sZ1; ++cz)
        {
            int blockRowBase = cz / CoarseBlockInterval * CoarseBlocksSubdivision;

            // No need to swap block if we're still in its range
            int currentBlockX = -1;
            float blockMin = 0f, blockMax = 0f;
            for (int cx = sX0; cx <= sX1; ++cx)
            {
                int bx = cx / CoarseBlockInterval;
                if (bx != currentBlockX)
                {
                    ref var range = ref CoarseBlocksAddress[blockRowBase + bx];
                    blockMin = range.MinHeight;
                    blockMax = range.MaxHeight;
                    currentBlockX = bx;
                }

                // TODO: Perf, skip to next block
                if (max.Y < blockMin || min.Y > blockMax)
                    continue;

                if (!enumerator.LoopBody(cx)) return;
                if (!enumerator.LoopBody(cz)) return;
            }
        }
    }

    internal readonly void FindLocalOverlaps<TOverlaps>(Vector3 min, Vector3 max, Vector3 sweep, float maximumT, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
        where TOverlaps : ICollisionTaskSubpairOverlaps
    {
        // TODO: Perf

        if (sweep.X < 0)
            min.X += sweep.X * maximumT;
        else
            max.X += sweep.X * maximumT;
        if (sweep.Y < 0)
            min.Y += sweep.Y * maximumT;
        else
            max.Y += sweep.Y * maximumT;
        if (sweep.Z < 0)
            min.Z += sweep.Z * maximumT;
        else
            max.Z += sweep.Z * maximumT;

        if (Clip(min.X, max.X, min.Z, max.Z, out int cx0, out int cx1, out int cz0, out int cz1) == false)
            return;

        if (max.Y < MinHeight || min.Y > MaxHeight)
            return;

        for (int cz = cz0; cz <= cz1; ++cz)
        {
            int blockRowBase = cz / CoarseBlockInterval * CoarseBlocksSubdivision;
            int currentBlockX = -1;
            float blockMin = 0f, blockMax = 0f;
            for (int cx = cx0; cx <= cx1; ++cx)
            {
                int bx = cx / CoarseBlockInterval;
                if (bx != currentBlockX)
                {
                    ref var range = ref CoarseBlocksAddress[blockRowBase + bx];
                    blockMin = range.MinHeight;
                    blockMax = range.MaxHeight;
                    currentBlockX = bx;
                }
                // TODO: Perf, skip to next block
                if (max.Y < blockMin || min.Y > blockMax)
                    continue;

                overlaps.Allocate(pool) = cx;
                overlaps.Allocate(pool) = cz;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void MarchRay<TRayHitHandler>(in RayData ray, in NRigidPose pose, Matrix3x3 orientation, ref float maximumT, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        // We're doing DDA obviously

        // TODO: Perf

        Matrix3x3.TransformTranspose(ray.Origin - pose.Position, orientation, out var localOrigin);
        Matrix3x3.TransformTranspose(ray.Direction, orientation, out var localDir);

        float invDirX = localDir.X != 0 ? 1f / localDir.X : 0f;
        float invDirZ = localDir.Z != 0 ? 1f / localDir.Z : 0f;
        float exMinX = 0, exMaxX = Subdivision * SampleInterval;
        float exMinZ = 0, exMaxZ = Subdivision * SampleInterval;

        if (localDir.X == 0 && (localOrigin.X < exMinX || localOrigin.X > exMaxX))
            return; // Parallel and outside in X
        if (localDir.Z == 0 && (localOrigin.Z < exMinZ || localOrigin.Z > exMaxZ))
            return; // Parallel and outside in Z

        float tx1 = localDir.X != 0 ? (exMinX - localOrigin.X) * invDirX : float.NegativeInfinity;
        float tx2 = localDir.X != 0 ? (exMaxX - localOrigin.X) * invDirX : float.PositiveInfinity;
        float tz1 = localDir.Z != 0 ? (exMinZ - localOrigin.Z) * invDirZ : float.NegativeInfinity;
        float tz2 = localDir.Z != 0 ? (exMaxZ - localOrigin.Z) * invDirZ : float.PositiveInfinity;
        float tEnter = MathF.Max(MathF.Min(tx1, tx2), MathF.Min(tz1, tz2));
        float tExit = MathF.Min(MathF.Max(tx1, tx2), MathF.Max(tz1, tz2));
        if (tEnter > tExit || tExit < 0)
            return;

        float tCur = MathF.Max(tEnter, 0f);
        if (tCur >= maximumT)
            return; // Already starts past the limits of this ray

        float yAtEntry = localOrigin.Y + localDir.Y * tCur;
        if ((localDir.Y >= 0 && yAtEntry > MaxHeight) || (localDir.Y <= 0 && yAtEntry < MinHeight))
            return; // Looking from outside and away vertically

        float inBoundsX = localOrigin.X + localDir.X * tCur;
        float inBoundsZ = localOrigin.Z + localDir.Z * tCur;
        int coordX = Math.Clamp((int)MathF.Floor(inBoundsX / SampleInterval), 0, Subdivision - 1);
        int coordZ = Math.Clamp((int)MathF.Floor(inBoundsZ / SampleInterval), 0, Subdivision - 1);
        int stepX = MathF.Sign(localDir.X);
        int stepZ = MathF.Sign(localDir.Z);

        float tMaxX, tMaxZ, tDeltaX, tDeltaZ;
        if (stepX > 0)
        {
            tMaxX = ((coordX + 1) * SampleInterval - localOrigin.X) * invDirX;
            tDeltaX = SampleInterval * invDirX;
        }
        else if (stepX < 0)
        {
            tMaxX = (coordX * SampleInterval - localOrigin.X) * invDirX;
            tDeltaX = -SampleInterval * invDirX;
        }
        else
        {
            tMaxX = float.MaxValue;
            tDeltaX = 0f;
        }

        if (stepZ > 0)
        {
            tMaxZ = ((coordZ + 1) * SampleInterval - localOrigin.Z) * invDirZ;
            tDeltaZ = SampleInterval * invDirZ;
        }
        else if (stepZ < 0)
        {
            tMaxZ = (coordZ * SampleInterval - localOrigin.Z) * invDirZ;
            tDeltaZ = -SampleInterval * invDirZ;
        }
        else
        {
            tMaxZ = float.MaxValue;
            tDeltaZ = 0f;
        }

        float tCellEnter = tCur;
        while (coordX >= 0 && coordX < Subdivision && coordZ >= 0 && coordZ < Subdivision)
        {
            float tCellExit = MathF.Min(MathF.Min(tMaxX, tMaxZ), MathF.Min(tExit, maximumT));
            if (tCellEnter > tCellExit)
                break;

            if (SegmentIntersectsBlock(coordX, coordZ, localOrigin.Y, localDir.Y, tCellEnter, tCellExit))
                TestCell(new Int2(coordX, coordZ), localOrigin, localDir, ray, orientation, ref maximumT, ref hitHandler);

            if (tMaxX < tMaxZ)
            {
                coordX += stepX;
                tCellEnter = tMaxX;
                tMaxX += tDeltaX;
            }
            else
            {
                coordZ += stepZ;
                tCellEnter = tMaxZ;
                tMaxZ += tDeltaZ;
            }

            if (tCellEnter > tExit || tCellEnter >= maximumT)
                break;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly bool SegmentIntersectsBlock(int sampleCoordX, int sampleCoordZ, float originY, float dirY, float t0, float t1)
    {
        ref var range = ref BlockOf(sampleCoordX, sampleCoordZ);
        float y0 = originY + dirY * t0;
        // edge case to handle infinite t1
        float y1 = dirY == 0 ? y0 : originY + dirY * t1;
        float segMin = MathF.Min(y0, y1);
        float segMax = MathF.Max(y0, y1);
        return segMax >= range.MinHeight && segMin <= range.MaxHeight;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private readonly void TestCell<TRayHitHandler>(Int2 cornerCoord, Vector3 origin, Vector3 dir, in RayData ray, Matrix3x3 orientation, ref float maximumT, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        GetLocalChild(cornerCoord, out var tri0, out var tri1);
        if (Triangle.RayTest(tri0.A, tri0.B, tri0.C, origin, dir, out float t0, out var normal0) && t0 <= maximumT)
        {
            Matrix3x3.Transform(normal0, orientation, out normal0);
            normal0 = Vector3.Normalize(normal0);
            hitHandler.OnRayHit(ray, ref maximumT, t0, normal0, 0);
        }
        if (Triangle.RayTest(tri1.A, tri1.B, tri1.C, origin, dir, out float t1, out var normal1) && t1 <= maximumT)
        {
            Matrix3x3.Transform(normal1, orientation, out normal1);
            normal1 = Vector3.Normalize(normal1);
            hitHandler.OnRayHit(ray, ref maximumT, t1, normal1, 0);
        }
    }

    public static ShapeBatch CreateShapeBatch(BufferPool pool, int initialCapacity, Shapes shapeBatches)
    {
        return new HeightfieldBatch(pool, initialCapacity);
    }

    private class HeightfieldBatch : ShapeBatch<HeightfieldShape>
    {
        public HeightfieldBatch(BufferPool pool, int initialShapeCount) : base(pool, initialShapeCount)
        {
            Compound = true;
        }

        protected override void Dispose(int index, BufferPool pool)
        {
        }

        protected override void RemoveAndDisposeChildren(int index, Shapes shapes, BufferPool pool)
        {
            // Meshes and other single-type containers don't have any shape-registered children.
        }

        public override void ComputeBounds(ref BoundingBoxBatcher batcher)
        {
            ExecuteHeightfieldBatch(ref batcher, this);
        }

        private static void ExecuteHeightfieldBatch(ref BoundingBoxBatcher batcher, HeightfieldBatch shapeBatch)
        {
            ref var batch = ref batches(ref batcher)[HeightfieldShape.TypeId];
            ref var activeSet = ref bodies(ref batcher).ActiveSet;
            for (int i = 0; i < batch.Count; ++i)
            {
                var shapeIndex = batch.ShapeIndices[i];
                ref var motionState = ref batch.MotionStates[i];
                var bodyIndex = batch.Continuations[i].BodyIndex;
                ref var collidable = ref activeSet.Collidables[bodyIndex];
                shapeBatch[shapeIndex].ComputeBounds(motionState.Pose.Orientation, out var min, out var max);
                //Working on the assumption that dynamic meshes are extremely rare, and that dynamic meshes with extremely high angular velocity are even rarer,
                //we're just going to use a simplistic upper bound for angular expansion. This simplifies the mesh bounding box calculation quite a bit (no dot products).
                var absMin = Vector3.Abs(min);
                var absMax = Vector3.Abs(max);
                var maximumRadius = Vector3.Max(absMin, absMax).Length();

                var minimumComponents = Vector3.Min(absMin, absMax);
                var minimumRadius = MathHelper.Min(minimumComponents.X, MathHelper.Min(minimumComponents.Y, minimumComponents.Z));
                var maximumAngularExpansion = maximumRadius - minimumRadius;

                //BoundingBoxBatcher is responsible for updating the bounding box AND speculative margin.
                //In order to know how much we're allowed to expand the bounding box, we need to know the speculative margin.
                //It's defined by the velocity of the body, and bounded by the body's minimum and maximum.
                var angularBoundsExpansion = BoundingBoxHelpers.GetAngularBoundsExpansion(motionState.Velocity.Angular.Length(), dt(ref batcher), maximumRadius, maximumAngularExpansion);
                var speculativeMargin = motionState.Velocity.Linear.Length() * (dt(ref batcher) + angularBoundsExpansion);
                speculativeMargin = MathF.Max(collidable.MinimumSpeculativeMargin, MathF.Min(collidable.MaximumSpeculativeMargin, speculativeMargin));
                collidable.SpeculativeMargin = speculativeMargin;
                var maximumAllowedExpansion = collidable.Continuity.AllowExpansionBeyondSpeculativeMargin ? float.MaxValue : speculativeMargin;
                BoundingBoxHelpers.GetBoundsExpansion(motionState.Velocity.Linear, dt(ref batcher), angularBoundsExpansion, out var minExpansion, out var maxExpansion);
                var broadcastMaximumBoundsExpansion = new Vector3(maximumAllowedExpansion);
                minExpansion = Vector3.Max(-broadcastMaximumBoundsExpansion, minExpansion);
                maxExpansion = Vector3.Min(broadcastMaximumBoundsExpansion, maxExpansion);

                broadPhase(ref batcher).GetActiveBoundsPointers(collidable.BroadPhaseIndex, out var minPointer, out var maxPointer);
                *minPointer = motionState.Pose.Position + (min + minExpansion);
                *maxPointer = motionState.Pose.Position + (max + maxExpansion);
            }

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "dt")]
            static extern ref float dt(ref BoundingBoxBatcher boundingBoxBatcher);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "bodies")]
            static extern ref Bodies bodies(ref BoundingBoxBatcher boundingBoxBatcher);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "batches")]
            static extern ref Buffer<BoundingBoxBatch> batches(ref BoundingBoxBatcher boundingBoxBatcher);

            [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "broadPhase")]
            static extern ref BroadPhase broadPhase(ref BoundingBoxBatcher boundingBoxBatcher);
        }

        public override void ComputeBounds(int shapeIndex, Quaternion orientation, out Vector3 min, out Vector3 max)
        {
            this[shapeIndex].ComputeBounds(orientation, out min, out max);
        }
        public override void RayTest<TRayHitHandler>(int shapeIndex, in NRigidPose pose, in RayData ray, ref float maximumT, BufferPool pool, ref TRayHitHandler hitHandler)
        {
            this[shapeIndex].RayTest(pose, ray, ref maximumT, pool, ref hitHandler);
        }

        public override void RayTest<TRayHitHandler>(int shapeIndex, in NRigidPose pose, ref RaySource rays, BufferPool pool, ref TRayHitHandler hitHandler)
        {
            this[shapeIndex].RayTest(pose, ref rays, pool, ref hitHandler);
        }
    }

    private ref struct ShapeTreeOverlapEnumerator<TSubpairOverlaps> : IBreakableForEach<int> where TSubpairOverlaps : ICollisionTaskSubpairOverlaps
    {
        public BufferPool Pool;
        public ref TSubpairOverlaps Overlaps;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool LoopBody(int i)
        {
            Overlaps.Allocate(Pool) = i;
            return true;
        }
    }
}
