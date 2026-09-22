// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics;
using BepuPhysics.Collidables;
using BepuPhysics.CollisionDetection.CollisionTasks;
using BepuPhysics.Trees;
using BepuUtilities;
using BepuUtilities.Memory;
using NRigidPose = BepuPhysics.RigidPose;

namespace Stride.BepuPhysics.Definitions.Colliders.Voxels;

/// <summary>
/// What <see cref="VoxelShapeHelpers"/> needs from a voxel shape to walk its grid.
/// </summary>
internal unsafe interface IVoxelShape
{
    /// <summary>Bepu type id of the child shape.</summary>
    static abstract int ChildShapeTypeId { get; }

    /// <summary>Most children one cell can report.</summary>
    static abstract int MaxChildrenPerCell { get; }

    /// <summary>How many cells beyond its owner a child can reach, widening every query by as much.</summary>
    static abstract int ChildReach { get; }

    VoxelGridLayout Layout { get; }

    /// <summary>Writes the child indices of a cell, deciding which exist without building their geometry.</summary>
    /// <returns>The number of indices written.</returns>
    int GetCellChildren(int cx, int cy, int cz, Span<int> childIndices);

    /// <summary>Tests one child against a ray in the shape's local space.</summary>
    bool RayTestChild(int childIndex, Vector3 origin, Vector3 direction, out float t, out Vector3 normal);

    /// <summary>Writes a child's convex shape data to <paramref name="destination"/>.</summary>
    /// <returns>The number of bytes written.</returns>
    int WriteChildShapeData(int childIndex, out Vector3 localPosition, void* destination);
}

/// <summary>
/// The grid walks shared by the voxel shapes: bounds, box queries and ray traversal.
/// </summary>
/// <remarks>
/// The regular grid is its own acceleration structure: a box maps to a cell range and a ray steps through cells, so there is no tree to build or refit.
/// </remarks>
internal static unsafe class VoxelShapeHelpers
{
    /// <summary>Direction component under which a ray is treated as parallel to an axis.</summary>
    private const float ParallelEpsilon = 1e-9f;

    public static void ComputeBounds<TShape>(ref TShape shape, Quaternion orientation, out Vector3 min, out Vector3 max)
        where TShape : unmanaged, IVoxelShape
    {
        var grid = shape.Layout;
        var extent = grid.Extent;
        Matrix3x3.CreateFromQuaternion(orientation, out var basis);
        min = new Vector3(float.MaxValue);
        max = new Vector3(float.MinValue);
        for (int i = 0; i < VoxelCell.CornerCount; ++i)
        {
            var corner = grid.Origin + new Vector3(
                (i & 1) != 0 ? extent.X : 0f,
                (i & 2) != 0 ? extent.Y : 0f,
                (i & 4) != 0 ? extent.Z : 0f);
            Matrix3x3.Transform(corner, basis, out var rotated);
            min = Vector3.Min(rotated, min);
            max = Vector3.Max(rotated, max);
        }
    }

    /// <summary>Clamps a local bounding box to the cells it touches; false when it misses the grid.</summary>
    private static bool GetCellRange(in VoxelGridLayout grid, int reach, Vector3 min, Vector3 max, out int x0, out int y0, out int z0, out int x1, out int y1, out int z1)
    {
        var low = (min - grid.Origin) / grid.CellSize;
        var high = (max - grid.Origin) / grid.CellSize;
        x0 = Math.Max((int)MathF.Floor(low.X) - reach, 0);
        y0 = Math.Max((int)MathF.Floor(low.Y) - reach, 0);
        z0 = Math.Max((int)MathF.Floor(low.Z) - reach, 0);
        x1 = Math.Min((int)MathF.Floor(high.X) + reach, grid.CellsX - 1);
        y1 = Math.Min((int)MathF.Floor(high.Y) + reach, grid.CellsY - 1);
        z1 = Math.Min((int)MathF.Floor(high.Z) + reach, grid.CellsZ - 1);
        return x0 <= x1 && y0 <= y1 && z0 <= z1;
    }

    /// <summary>Reports every child overlapping a local bounding box, until the enumerator breaks.</summary>
    public static void EnumerateOverlaps<TShape, TEnumerator>(ref TShape shape, Vector3 min, Vector3 max, ref TEnumerator enumerator)
        where TShape : unmanaged, IVoxelShape
        where TEnumerator : IBreakableForEach<int>
    {
        if (!GetCellRange(shape.Layout, TShape.ChildReach, min, max, out var x0, out var y0, out var z0, out var x1, out var y1, out var z1))
            return;
        Span<int> children = stackalloc int[TShape.MaxChildrenPerCell];
        for (int x = x0; x <= x1; ++x)
        {
            for (int y = y0; y <= y1; ++y)
            {
                for (int z = z0; z <= z1; ++z)
                {
                    var count = shape.GetCellChildren(x, y, z, children);
                    for (int i = 0; i < count; ++i)
                    {
                        if (!enumerator.LoopBody(children[i]))
                            return;
                    }
                }
            }
        }
    }

    public static void FindLocalOverlaps<TShape, TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, ref TOverlaps overlaps)
        where TShape : unmanaged, IVoxelShape
        where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
        where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
    {
        for (int i = 0; i < pairs.Length; ++i)
        {
            ref var pair = ref pairs[i];
            ref var shape = ref Unsafe.AsRef<TShape>(pair.Container);
            ref var subpairOverlaps = ref overlaps.GetOverlapsForPair(i);
            var collector = new OverlapCollector<TSubpairOverlaps>(Unsafe.AsPointer(ref subpairOverlaps), pool);
            EnumerateOverlaps(ref shape, pair.Min, pair.Max, ref collector);
        }
    }

    /// <summary>The sweep query, over the box covering the whole swept volume; conservative, never missing a child.</summary>
    public static void FindLocalOverlaps<TShape, TOverlaps>(Vector3 min, Vector3 max, Vector3 sweep, float maximumT, BufferPool pool, void* overlaps, ref TShape shape)
        where TShape : unmanaged, IVoxelShape
        where TOverlaps : ICollisionTaskSubpairOverlaps
    {
        var sweptOffset = sweep * maximumT;
        var collector = new OverlapCollector<TOverlaps>(overlaps, pool);
        EnumerateOverlaps(ref shape, Vector3.Min(min, min + sweptOffset), Vector3.Max(max, max + sweptOffset), ref collector);
    }

    private readonly struct OverlapCollector<TSubpairOverlaps>(void* overlaps, BufferPool pool) : IBreakableForEach<int>
        where TSubpairOverlaps : ICollisionTaskSubpairOverlaps
    {
        public bool LoopBody(int childIndex)
        {
            Unsafe.AsRef<TSubpairOverlaps>(overlaps).Allocate(pool) = childIndex;
            return true;
        }
    }

    /// <summary>
    /// Walks the cells a ray crosses in order (3D DDA), testing their children; a handler narrowing <paramref name="maximumT"/> ends the walk early.
    /// </summary>
    public static void RayTest<TShape, TRayHitHandler>(ref TShape shape, in NRigidPose pose, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler)
        where TShape : unmanaged, IVoxelShape
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        var grid = shape.Layout;
        Matrix3x3.CreateFromQuaternion(pose.Orientation, out var orientation);
        Matrix3x3.TransformTranspose(ray.Origin - pose.Position, orientation, out var origin);
        Matrix3x3.TransformTranspose(ray.Direction, orientation, out var direction);

        // Stepping works from the grid's minimum corner; children are still tested in the shape's local space.
        var gridOrigin = origin - grid.Origin;
        var cellSize = grid.CellSize;
        if (!TryClipToBox(gridOrigin, direction, grid.Extent, maximumT, out var tEnter, out var tExit))
            return;

        var entry = (gridOrigin + direction * tEnter) / cellSize;
        var x = Math.Clamp((int)MathF.Floor(entry.X), 0, grid.CellsX - 1);
        var y = Math.Clamp((int)MathF.Floor(entry.Y), 0, grid.CellsY - 1);
        var z = Math.Clamp((int)MathF.Floor(entry.Z), 0, grid.CellsZ - 1);

        ComputeAxisStep(gridOrigin.X, direction.X, x, cellSize.X, tEnter, out var stepX, out var tMaxX, out var tDeltaX);
        ComputeAxisStep(gridOrigin.Y, direction.Y, y, cellSize.Y, tEnter, out var stepY, out var tMaxY, out var tDeltaY);
        ComputeAxisStep(gridOrigin.Z, direction.Z, z, cellSize.Z, tEnter, out var stepZ, out var tMaxZ, out var tDeltaZ);

        Span<int> children = stackalloc int[TShape.MaxChildrenPerCell];
        var reach = TShape.ChildReach;
        var t = tEnter;
        while (t <= tExit && t <= maximumT)
        {
            // Cells within reach are tested again from each neighbour; the handler keeps the nearest hit.
            for (int cx = Math.Max(x - reach, 0); cx <= Math.Min(x + reach, grid.CellsX - 1); ++cx)
            {
                for (int cy = Math.Max(y - reach, 0); cy <= Math.Min(y + reach, grid.CellsY - 1); ++cy)
                {
                    for (int cz = Math.Max(z - reach, 0); cz <= Math.Min(z + reach, grid.CellsZ - 1); ++cz)
                        TestCell(ref shape, cx, cy, cz, children, origin, direction, orientation, ray, ref maximumT, ref hitHandler);
                }
            }

            if (tMaxX < tMaxY && tMaxX < tMaxZ)
            {
                x += stepX;
                t = tMaxX;
                tMaxX += tDeltaX;
                if ((uint)x >= (uint)grid.CellsX)
                    return;
            }
            else if (tMaxY < tMaxZ)
            {
                y += stepY;
                t = tMaxY;
                tMaxY += tDeltaY;
                if ((uint)y >= (uint)grid.CellsY)
                    return;
            }
            else
            {
                z += stepZ;
                t = tMaxZ;
                tMaxZ += tDeltaZ;
                if ((uint)z >= (uint)grid.CellsZ)
                    return;
            }
        }
    }

    private static void TestCell<TShape, TRayHitHandler>(ref TShape shape, int cx, int cy, int cz, Span<int> children, Vector3 origin, Vector3 direction, in Matrix3x3 orientation, in RayData ray, ref float maximumT, ref TRayHitHandler hitHandler)
        where TShape : unmanaged, IVoxelShape
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        var count = shape.GetCellChildren(cx, cy, cz, children);
        for (int i = 0; i < count; ++i)
        {
            var childIndex = children[i];
            if (!hitHandler.AllowTest(childIndex))
                continue;
            if (shape.RayTestChild(childIndex, origin, direction, out var hitT, out var normal) && hitT <= maximumT)
            {
                Matrix3x3.Transform(normal, orientation, out normal);
                hitHandler.OnRayHit(ray, ref maximumT, hitT, normal, childIndex);
            }
        }
    }

    public static void RayTest<TShape, TRayHitHandler>(ref TShape shape, in NRigidPose pose, ref RaySource rays, ref TRayHitHandler hitHandler)
        where TShape : unmanaged, IVoxelShape
        where TRayHitHandler : struct, IShapeRayHitHandler
    {
        for (int i = 0; i < rays.RayCount; ++i)
        {
            rays.GetRay(i, out var ray, out var maximumT);
            RayTest(ref shape, pose, *ray, ref *maximumT, ref hitHandler);
        }
    }

    /// <summary>Slab test clipping a ray to the box from zero to <paramref name="max"/>.</summary>
    private static bool TryClipToBox(Vector3 origin, Vector3 direction, Vector3 max, float maximumT, out float tEnter, out float tExit)
    {
        tEnter = 0f;
        tExit = maximumT;
        for (int axis = 0; axis < 3; ++axis)
        {
            var o = origin[axis];
            var d = direction[axis];
            var high = max[axis];
            if (MathF.Abs(d) < ParallelEpsilon)
            {
                if (o < 0f || o > high)
                    return false;
                continue;
            }
            var inverse = 1f / d;
            var t0 = -o * inverse;
            var t1 = (high - o) * inverse;
            if (t0 > t1)
                (t0, t1) = (t1, t0);
            tEnter = MathF.Max(tEnter, t0);
            tExit = MathF.Min(tExit, t1);
            if (tEnter > tExit)
                return false;
        }
        return true;
    }

    private static void ComputeAxisStep(float origin, float direction, int cell, float cellSize, float tEnter, out int step, out float tMax, out float tDelta)
    {
        if (MathF.Abs(direction) < ParallelEpsilon)
        {
            step = 0;
            tMax = float.MaxValue;
            tDelta = float.MaxValue;
            return;
        }
        step = direction > 0 ? 1 : -1;
        tDelta = MathF.Abs(cellSize / direction);
        var boundary = (cell + (step > 0 ? 1 : 0)) * cellSize;
        tMax = (boundary - origin) / direction;
        // Rounding at the entry face can put the first boundary just behind the entry point.
        if (tMax < tEnter)
            tMax = tEnter + tDelta;
    }
}

/// <summary>
/// A voxel grid presented to the narrow phase as a box filling each solid cell.
/// </summary>
internal unsafe struct VoxelBoxShape<TSource> : IHomogeneousCompoundShape<Box, BoxWide>, IVoxelShape
    where TSource : unmanaged, IVoxelDensitySource
{
    public VoxelGridData<TSource> GridData;

    public static int TypeId => VoxelShapeTypeIds<TSource>.Box;
    public static int ChildShapeTypeId => Box.Id;
    public static int MaxChildrenPerCell => 1;
    public static int ChildReach => 0;

    public readonly VoxelGridLayout Layout => GridData.Layout;

    /// <summary>The size of the child index space, one per cell whether solid or not.</summary>
    public readonly int ChildCount => GridData.CellCount;

    public static ShapeBatch CreateShapeBatch(BufferPool pool, int initialCapacity, Shapes shapeBatches)
        => new HomogeneousCompoundShapeBatch<VoxelBoxShape<TSource>, Box, BoxWide>(pool, initialCapacity);

    public readonly int GetCellChildren(int cx, int cy, int cz, Span<int> childIndices)
    {
        if (!GridData.CellIsSolid(cx, cy, cz))
            return 0;
        childIndices[0] = GridData.CellIndex(cx, cy, cz);
        return 1;
    }

    public readonly void GetLocalChild(int childIndex, out Box childShape)
        => childShape = new Box(GridData.CellSize.X, GridData.CellSize.Y, GridData.CellSize.Z);

    public readonly void GetPosedLocalChild(int childIndex, out Box childShape, out NRigidPose childPose)
    {
        GetLocalChild(childIndex, out childShape);
        GridData.DecomposeCell(childIndex, out var cx, out var cy, out var cz);
        childPose = new NRigidPose(GridData.CellCentre(cx, cy, cz));
    }

    public readonly void GetLocalChild(int childIndex, ref BoxWide childShapeWide)
    {
        var half = GridData.CellSize * 0.5f;
        GatherScatter.GetFirst(ref childShapeWide.HalfWidth) = half.X;
        GatherScatter.GetFirst(ref childShapeWide.HalfHeight) = half.Y;
        GatherScatter.GetFirst(ref childShapeWide.HalfLength) = half.Z;
    }

    public readonly bool RayTestChild(int childIndex, Vector3 origin, Vector3 direction, out float t, out Vector3 normal)
    {
        GetPosedLocalChild(childIndex, out var box, out var pose);
        return box.RayTest(pose, origin, direction, out t, out normal);
    }

    public readonly int WriteChildShapeData(int childIndex, out Vector3 localPosition, void* destination)
    {
        GetPosedLocalChild(childIndex, out var box, out var pose);
        localPosition = pose.Position;
        Unsafe.Write(destination, box);
        return sizeof(Box);
    }

    public void ComputeBounds(Quaternion orientation, out Vector3 min, out Vector3 max)
        => VoxelShapeHelpers.ComputeBounds(ref this, orientation, out min, out max);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, in RayData ray, ref float maximumT, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ray, ref maximumT, ref hitHandler);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, ref RaySource rays, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ref rays, ref hitHandler);

    public readonly void FindLocalOverlaps<TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
        where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
        where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelBoxShape<TSource>, TOverlaps, TSubpairOverlaps>(ref pairs, pool, ref overlaps);

    public void FindLocalOverlaps<TOverlaps>(Vector3 min, Vector3 max, Vector3 sweep, float maximumT, BufferPool pool, Shapes shapes, void* overlaps)
        where TOverlaps : ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelBoxShape<TSource>, TOverlaps>(min, max, sweep, maximumT, pool, overlaps, ref this);

    public void FindLocalOverlaps<TEnumerator>(Vector3 min, Vector3 max, BufferPool pool, Shapes shapes, ref TEnumerator enumerator)
        where TEnumerator : IBreakableForEach<int>
        => VoxelShapeHelpers.EnumerateOverlaps(ref this, min, max, ref enumerator);

    /// <summary>The samples belong to the <see cref="VoxelCollider"/>.</summary>
    public readonly void Dispose(BufferPool pool)
    {
    }
}

/// <summary>
/// A voxel grid presented to the narrow phase as a sphere inscribed in each solid cell.
/// </summary>
internal unsafe struct VoxelSphereShape<TSource> : IHomogeneousCompoundShape<Sphere, SphereWide>, IVoxelShape
    where TSource : unmanaged, IVoxelDensitySource
{
    public VoxelGridData<TSource> GridData;

    public static int TypeId => VoxelShapeTypeIds<TSource>.Sphere;
    public static int ChildShapeTypeId => Sphere.Id;
    public static int MaxChildrenPerCell => 1;
    public static int ChildReach => 0;

    public readonly VoxelGridLayout Layout => GridData.Layout;

    /// <summary>The size of the child index space, one per cell whether solid or not.</summary>
    public readonly int ChildCount => GridData.CellCount;

    public static ShapeBatch CreateShapeBatch(BufferPool pool, int initialCapacity, Shapes shapeBatches)
        => new HomogeneousCompoundShapeBatch<VoxelSphereShape<TSource>, Sphere, SphereWide>(pool, initialCapacity);

    public readonly int GetCellChildren(int cx, int cy, int cz, Span<int> childIndices)
    {
        if (!GridData.CellIsSolid(cx, cy, cz))
            return 0;
        childIndices[0] = GridData.CellIndex(cx, cy, cz);
        return 1;
    }

    public readonly void GetLocalChild(int childIndex, out Sphere childShape)
        => childShape = new Sphere(GridData.SphereRadius);

    public readonly void GetPosedLocalChild(int childIndex, out Sphere childShape, out NRigidPose childPose)
    {
        GetLocalChild(childIndex, out childShape);
        GridData.DecomposeCell(childIndex, out var cx, out var cy, out var cz);
        childPose = new NRigidPose(GridData.CellCentre(cx, cy, cz));
    }

    public readonly void GetLocalChild(int childIndex, ref SphereWide childShapeWide)
        => GatherScatter.GetFirst(ref childShapeWide.Radius) = GridData.SphereRadius;

    public readonly bool RayTestChild(int childIndex, Vector3 origin, Vector3 direction, out float t, out Vector3 normal)
    {
        GetPosedLocalChild(childIndex, out var sphere, out var pose);
        return sphere.RayTest(pose, origin, direction, out t, out normal);
    }

    public readonly int WriteChildShapeData(int childIndex, out Vector3 localPosition, void* destination)
    {
        GetPosedLocalChild(childIndex, out var sphere, out var pose);
        localPosition = pose.Position;
        Unsafe.Write(destination, sphere);
        return sizeof(Sphere);
    }

    public void ComputeBounds(Quaternion orientation, out Vector3 min, out Vector3 max)
        => VoxelShapeHelpers.ComputeBounds(ref this, orientation, out min, out max);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, in RayData ray, ref float maximumT, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ray, ref maximumT, ref hitHandler);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, ref RaySource rays, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ref rays, ref hitHandler);

    public readonly void FindLocalOverlaps<TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
        where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
        where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelSphereShape<TSource>, TOverlaps, TSubpairOverlaps>(ref pairs, pool, ref overlaps);

    public void FindLocalOverlaps<TOverlaps>(Vector3 min, Vector3 max, Vector3 sweep, float maximumT, BufferPool pool, Shapes shapes, void* overlaps)
        where TOverlaps : ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelSphereShape<TSource>, TOverlaps>(min, max, sweep, maximumT, pool, overlaps, ref this);

    public void FindLocalOverlaps<TEnumerator>(Vector3 min, Vector3 max, BufferPool pool, Shapes shapes, ref TEnumerator enumerator)
        where TEnumerator : IBreakableForEach<int>
        => VoxelShapeHelpers.EnumerateOverlaps(ref this, min, max, ref enumerator);

    /// <summary>The samples belong to the <see cref="VoxelCollider"/>.</summary>
    public readonly void Dispose(BufferPool pool)
    {
    }
}

/// <summary>
/// A voxel grid presented to the narrow phase as the triangles of its iso-surface, by marching cubes or surface nets.
/// </summary>
internal unsafe struct VoxelTriangleShape<TSource> : IHomogeneousCompoundShape<Triangle, TriangleWide>, IVoxelShape
    where TSource : unmanaged, IVoxelDensitySource
{
    /// <summary>Child indices per cell: <c>childIndex = cellIndex * SlotsPerCell + slot</c>, enough for either algorithm.</summary>
    public const int SlotsPerCell = VoxelCell.MaxSurfaceNetsTrianglesPerCell;

    public VoxelGridData<TSource> GridData;

    /// <summary>Surface nets when set, marching cubes otherwise.</summary>
    public bool SurfaceNets;

    public static int TypeId => VoxelShapeTypeIds<TSource>.Triangle;
    public static int ChildShapeTypeId => Triangle.Id;
    public static int MaxChildrenPerCell => SlotsPerCell;

    /// <summary>A surface nets quad joins the vertices of the four cells around an edge, so it reaches into the neighbours.</summary>
    public static int ChildReach => 1;

    public readonly VoxelGridLayout Layout => GridData.Layout;

    /// <summary>The size of the child index space, <see cref="SlotsPerCell"/> per cell whether used or not.</summary>
    public readonly int ChildCount => GridData.CellCount * SlotsPerCell;

    public static ShapeBatch CreateShapeBatch(BufferPool pool, int initialCapacity, Shapes shapeBatches)
        => new HomogeneousCompoundShapeBatch<VoxelTriangleShape<TSource>, Triangle, TriangleWide>(pool, initialCapacity);

    public readonly int GetCellChildren(int cx, int cy, int cz, Span<int> childIndices)
    {
        var firstChild = GridData.CellIndex(cx, cy, cz) * SlotsPerCell;
        var count = 0;
        if (SurfaceNets)
        {
            for (int axis = 0; axis < 3; ++axis)
            {
                if (!GridData.SurfaceNetsEdgeStraddles(cx, cy, cz, axis, out _))
                    continue;
                childIndices[count++] = firstChild + axis * 2;
                childIndices[count++] = firstChild + axis * 2 + 1;
            }
        }
        else
        {
            var triangles = VoxelGridData<TSource>.MarchingCubesTriangleCount(GridData.CubeIndex(cx, cy, cz));
            for (int slot = 0; slot < triangles; ++slot)
                childIndices[count++] = firstChild + slot;
        }
        return count;
    }

    /// <summary>The triangle in one slot of a cell; false for an unused slot.</summary>
    public readonly bool TryGetTriangle(int cx, int cy, int cz, int slot, out Triangle triangle)
        => SurfaceNets
            ? GridData.TryGetSurfaceNetsTriangle(cx, cy, cz, slot, out triangle)
            : GridData.TryGetMarchingCubesTriangle(cx, cy, cz, GridData.CubeIndex(cx, cy, cz), slot, out triangle);

    public readonly void GetLocalChild(int childIndex, out Triangle childShape)
    {
        GridData.DecomposeCell(childIndex / SlotsPerCell, out var cx, out var cy, out var cz);
        // An unused slot leaves a degenerate triangle, which collides with nothing.
        TryGetTriangle(cx, cy, cz, childIndex % SlotsPerCell, out childShape);
    }

    public readonly void GetPosedLocalChild(int childIndex, out Triangle childShape, out NRigidPose childPose)
    {
        GetLocalChild(childIndex, out childShape);
        var centroid = (childShape.A + childShape.B + childShape.C) / 3f;
        childPose = new NRigidPose(centroid);
        childShape.A -= centroid;
        childShape.B -= centroid;
        childShape.C -= centroid;
    }

    public readonly void GetLocalChild(int childIndex, ref TriangleWide childShapeWide)
    {
        GetLocalChild(childIndex, out var triangle);
        Vector3Wide.WriteFirst(triangle.A, ref childShapeWide.A);
        Vector3Wide.WriteFirst(triangle.B, ref childShapeWide.B);
        Vector3Wide.WriteFirst(triangle.C, ref childShapeWide.C);
    }

    public readonly bool RayTestChild(int childIndex, Vector3 origin, Vector3 direction, out float t, out Vector3 normal)
    {
        GetLocalChild(childIndex, out var triangle);
        return Triangle.RayTest(triangle.A, triangle.B, triangle.C, origin, direction, out t, out normal);
    }

    public readonly int WriteChildShapeData(int childIndex, out Vector3 localPosition, void* destination)
    {
        GetPosedLocalChild(childIndex, out var triangle, out var pose);
        localPosition = pose.Position;
        Unsafe.Write(destination, triangle);
        return sizeof(Triangle);
    }

    public void ComputeBounds(Quaternion orientation, out Vector3 min, out Vector3 max)
        => VoxelShapeHelpers.ComputeBounds(ref this, orientation, out min, out max);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, in RayData ray, ref float maximumT, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ray, ref maximumT, ref hitHandler);

    public void RayTest<TRayHitHandler>(in NRigidPose pose, ref RaySource rays, BufferPool pool, ref TRayHitHandler hitHandler)
        where TRayHitHandler : struct, IShapeRayHitHandler
        => VoxelShapeHelpers.RayTest(ref this, pose, ref rays, ref hitHandler);

    public readonly void FindLocalOverlaps<TOverlaps, TSubpairOverlaps>(ref Buffer<OverlapQueryForPair> pairs, BufferPool pool, Shapes shapes, ref TOverlaps overlaps)
        where TOverlaps : struct, ICollisionTaskOverlaps<TSubpairOverlaps>
        where TSubpairOverlaps : struct, ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelTriangleShape<TSource>, TOverlaps, TSubpairOverlaps>(ref pairs, pool, ref overlaps);

    public void FindLocalOverlaps<TOverlaps>(Vector3 min, Vector3 max, Vector3 sweep, float maximumT, BufferPool pool, Shapes shapes, void* overlaps)
        where TOverlaps : ICollisionTaskSubpairOverlaps
        => VoxelShapeHelpers.FindLocalOverlaps<VoxelTriangleShape<TSource>, TOverlaps>(min, max, sweep, maximumT, pool, overlaps, ref this);

    public void FindLocalOverlaps<TEnumerator>(Vector3 min, Vector3 max, BufferPool pool, Shapes shapes, ref TEnumerator enumerator)
        where TEnumerator : IBreakableForEach<int>
        => VoxelShapeHelpers.EnumerateOverlaps(ref this, min, max, ref enumerator);

    /// <summary>The samples belong to the <see cref="VoxelCollider"/>.</summary>
    public readonly void Dispose(BufferPool pool)
    {
    }
}
