// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using BepuPhysics.Collidables;

namespace Stride.BepuPhysics.Definitions.Colliders.Voxels;

/// <summary>Counts shared by every voxel grid.</summary>
internal static class VoxelCell
{
    /// <summary>Number of corners of a cell.</summary>
    public const int CornerCount = 8;

    /// <summary>Number of edges of a cell.</summary>
    public const int EdgeCount = 12;

    /// <summary>Most marching cubes triangles one cell produces.</summary>
    public const int MaxMarchingCubesTrianglesPerCell = 5;

    /// <summary>Most surface nets triangles one cell owns: a quad for each of its three minimum-corner edges.</summary>
    public const int MaxSurfaceNetsTrianglesPerCell = 6;
}

/// <summary>Where the cells of a voxel grid are in local space: cell (cx, cy, cz) spans <c>Origin + [cx, cx + 1] * CellSize</c>.</summary>
internal readonly record struct VoxelGridLayout(Vector3 Origin, Vector3 CellSize, int CellsX, int CellsY, int CellsZ)
{
    public Vector3 Extent => new Vector3(CellsX, CellsY, CellsZ) * CellSize;
}

/// <summary>
/// A voxel density field and the geometry derived from it, stored inside Bepu's shape pools.
/// </summary>
/// <remarks>n cells per axis read n + 1 samples per axis.</remarks>
internal struct VoxelGridData<TSource> where TSource : unmanaged, IVoxelDensitySource
{
    private const int CornerCount = VoxelCell.CornerCount;
    private const int EdgeCount = VoxelCell.EdgeCount;
    private const int MaxMarchingCubesTrianglesPerCell = VoxelCell.MaxMarchingCubesTrianglesPerCell;

    /// <summary>Case index of a cell whose corners are all solid.</summary>
    private const int SolidCase = 0;

    /// <summary>Case index of a cell whose corners are all air.</summary>
    private const int AirCase = (1 << CornerCount) - 1;

    /// <summary>Entries per case in <see cref="TriangleTable"/>.</summary>
    private const int TableStride = 16;

    /// <summary>Density difference under which an edge is treated as flat, taking its midpoint.</summary>
    private const float FlatEdgeEpsilon = 1e-6f;

    public TSource Source;

    /// <summary>Local position of sample (0, 0, 0).</summary>
    public Vector3 Origin;

    /// <summary>Size of one cell on each axis, the collider's cell size times the entity scale.</summary>
    public Vector3 CellSize;

    /// <summary>Density at or above which a sample is solid.</summary>
    public float IsoLevel;

    public bool InvertWinding;

    /// <summary>Reads the samples on the faces of the grid as air, closing the volume.</summary>
    public bool SealBorder;

    public float SphereRadius;

    public readonly int SamplesX => Source.SamplesX;
    public readonly int SamplesY => Source.SamplesY;
    public readonly int SamplesZ => Source.SamplesZ;

    public readonly int CellsX => SamplesX - 1;
    public readonly int CellsY => SamplesY - 1;
    public readonly int CellsZ => SamplesZ - 1;
    public readonly int CellCount => CellsX * CellsY * CellsZ;

    /// <summary>Size of the whole grid in local space.</summary>
    public readonly Vector3 Extent => new Vector3(CellsX, CellsY, CellsZ) * CellSize;

    public readonly VoxelGridLayout Layout => new(Origin, CellSize, CellsX, CellsY, CellsZ);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int CellIndex(int cx, int cy, int cz) => (cx * CellsY + cy) * CellsZ + cz;

    public readonly void DecomposeCell(int cellIndex, out int cx, out int cy, out int cz)
    {
        cz = cellIndex % CellsZ;
        var remainder = cellIndex / CellsZ;
        cy = remainder % CellsY;
        cx = remainder / CellsY;
    }

    /// <summary>Density a sealed border reads: 0, or one below a non-positive iso level.</summary>
    private readonly float SealedDensity => IsoLevel > 0f ? 0f : IsoLevel - 1f;

    /// <summary>Density of one sample; the coordinates must be inside the grid.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly float DensityAt(int x, int y, int z)
    {
        if (SealBorder && (x == 0 || y == 0 || z == 0 || x == SamplesX - 1 || y == SamplesY - 1 || z == SamplesZ - 1))
            return SealedDensity;
        return Source.Density(x, y, z);
    }

    /// <summary>Whether a cell carries a box or sphere child: the mean of its corners is at or above the iso level.</summary>
    /// <remarks>The mean keeps the blocky surface within half a cell of the iso-surface, on either side.</remarks>
    public readonly bool CellIsSolid(int cx, int cy, int cz)
    {
        if ((uint)cx >= (uint)CellsX || (uint)cy >= (uint)CellsY || (uint)cz >= (uint)CellsZ)
            return false;
        var sum = 0f;
        for (int corner = 0; corner < CornerCount; ++corner)
            sum += CornerDensity(cx, cy, cz, corner);
        return sum / CornerCount >= IsoLevel;
    }

    public readonly Vector3 CellCentre(int cx, int cy, int cz)
        => Origin + new Vector3(cx + 0.5f, cy + 0.5f, cz + 0.5f) * CellSize;

    /// <summary>
    /// The eight corner offsets of a cell, three bytes each, in the order <see cref="TriangleTable"/> indexes them.
    /// </summary>
    private static ReadOnlySpan<byte> CubeCorners =>
    [
        0, 0, 0,  1, 0, 0,  1, 0, 1,  0, 0, 1,
        0, 1, 0,  1, 1, 0,  1, 1, 1,  0, 1, 1,
    ];

    /// <summary>The two corners each edge of a cell connects.</summary>
    private static ReadOnlySpan<byte> EdgeCorners =>
    [
        0, 1, 1, 2, 2, 3, 3, 0,
        4, 5, 5, 6, 6, 7, 7, 4,
        0, 4, 1, 5, 2, 6, 3, 7,
    ];

    private readonly float CornerDensity(int cx, int cy, int cz, int corner)
    {
        var offset = corner * 3;
        return DensityAt(cx + CubeCorners[offset], cy + CubeCorners[offset + 1], cz + CubeCorners[offset + 2]);
    }

    private static Vector3 CornerOffset(int corner)
    {
        var offset = corner * 3;
        return new Vector3(CubeCorners[offset], CubeCorners[offset + 1], CubeCorners[offset + 2]);
    }

    /// <summary>Direction in which the density of a cell grows, towards the solid side.</summary>
    private readonly Vector3 CellGradient(int cx, int cy, int cz)
    {
        var gradient = Vector3.Zero;
        for (int corner = 0; corner < CornerCount; ++corner)
            gradient += (CornerOffset(corner) * 2f - Vector3.One) * CornerDensity(cx, cy, cz, corner);
        return gradient;
    }

    /// <summary>Orients a triangle so it collides from <paramref name="outward"/>, or from the other side with <see cref="InvertWinding"/>.</summary>
    /// <remarks>Bepu triangles collide on the side where cross(C - A, B - A) points.</remarks>
    private readonly Triangle Orient(Triangle triangle, Vector3 outward)
    {
        var facing = Vector3.Cross(triangle.C - triangle.A, triangle.B - triangle.A);
        if (Vector3.Dot(facing, outward) < 0 != InvertWinding)
            (triangle.B, triangle.C) = (triangle.C, triangle.B);
        return triangle;
    }

    /// <summary>Marching cubes case of a cell: bit i is set when corner i is air.</summary>
    public readonly int CubeIndex(int cx, int cy, int cz)
    {
        var cubeIndex = 0;
        for (int corner = 0; corner < CornerCount; ++corner)
        {
            if (CornerDensity(cx, cy, cz, corner) < IsoLevel)
                cubeIndex |= 1 << corner;
        }
        return cubeIndex;
    }

    /// <summary>Local point where the iso-surface crosses one edge of a cell.</summary>
    private readonly Vector3 EdgePosition(int cx, int cy, int cz, int edge)
    {
        var cornerA = EdgeCorners[edge * 2];
        var cornerB = EdgeCorners[edge * 2 + 1];
        var densityA = CornerDensity(cx, cy, cz, cornerA);
        var delta = CornerDensity(cx, cy, cz, cornerB) - densityA;
        var t = MathF.Abs(delta) > FlatEdgeEpsilon ? Math.Clamp((IsoLevel - densityA) / delta, 0f, 1f) : 0.5f;
        var cell = new Vector3(cx, cy, cz);
        return Origin + (cell + Vector3.Lerp(CornerOffset(cornerA), CornerOffset(cornerB), t)) * CellSize;
    }

    /// <summary>Number of triangles of a marching cubes case.</summary>
    public static int MarchingCubesTriangleCount(int cubeIndex)
    {
        if (cubeIndex is SolidCase or AirCase)
            return 0;
        var offset = cubeIndex * TableStride;
        for (int slot = 0; slot < MaxMarchingCubesTrianglesPerCell; ++slot)
        {
            if (TriangleTable[offset + slot * 3] < 0)
                return slot;
        }
        return MaxMarchingCubesTrianglesPerCell;
    }

    /// <summary>The <paramref name="slot"/>th marching cubes triangle of a cell whose case is already known.</summary>
    public readonly bool TryGetMarchingCubesTriangle(int cx, int cy, int cz, int cubeIndex, int slot, out Triangle triangle)
    {
        triangle = default;
        if (cubeIndex is SolidCase or AirCase || slot >= MaxMarchingCubesTrianglesPerCell)
            return false;
        var offset = cubeIndex * TableStride + slot * 3;
        var e0 = TriangleTable[offset];
        if (e0 < 0)
            return false;
        var a = EdgePosition(cx, cy, cz, e0);
        var b = EdgePosition(cx, cy, cz, TriangleTable[offset + 1]);
        var c = EdgePosition(cx, cy, cz, TriangleTable[offset + 2]);
        triangle = Orient(new Triangle(a, b, c), -CellGradient(cx, cy, cz));
        return true;
    }

    /// <summary>
    /// Whether the edge leaving a cell's minimum corner along <paramref name="axis"/> changes sign, and the four cells around it exist.
    /// </summary>
    public readonly bool SurfaceNetsEdgeStraddles(int cx, int cy, int cz, int axis, out bool solidAtOrigin)
    {
        solidAtOrigin = DensityAt(cx, cy, cz) >= IsoLevel;

        // The quad joins the cells one step back on the two other axes; they do not exist at the low border.
        int nx = cx, ny = cy, nz = cz;
        switch (axis)
        {
            case 0:
                if (cy < 1 || cz < 1)
                    return false;
                nx = cx + 1;
                break;
            case 1:
                if (cz < 1 || cx < 1)
                    return false;
                ny = cy + 1;
                break;
            default:
                if (cx < 1 || cy < 1)
                    return false;
                nz = cz + 1;
                break;
        }
        return solidAtOrigin != (DensityAt(nx, ny, nz) >= IsoLevel);
    }

    /// <summary>The surface nets vertex of a cell: the mean of the iso-surface crossings on its edges.</summary>
    public readonly bool TryGetSurfaceNetsVertex(int cx, int cy, int cz, out Vector3 vertex)
    {
        vertex = default;
        if ((uint)cx >= (uint)CellsX || (uint)cy >= (uint)CellsY || (uint)cz >= (uint)CellsZ)
            return false;
        var cubeIndex = CubeIndex(cx, cy, cz);
        if (cubeIndex is SolidCase or AirCase)
            return false;
        var sum = Vector3.Zero;
        var count = 0;
        for (int edge = 0; edge < EdgeCount; ++edge)
        {
            var airA = (cubeIndex & (1 << EdgeCorners[edge * 2])) != 0;
            var airB = (cubeIndex & (1 << EdgeCorners[edge * 2 + 1])) != 0;
            if (airA == airB)
                continue;
            sum += EdgePosition(cx, cy, cz, edge);
            ++count;
        }
        vertex = sum / count;
        return true;
    }

    /// <summary>
    /// The <paramref name="slot"/>th surface nets triangle owned by a cell: slots 0 to 5 are the two triangles of the quads on its X, Y and Z edges.
    /// </summary>
    public readonly bool TryGetSurfaceNetsTriangle(int cx, int cy, int cz, int slot, out Triangle triangle)
    {
        triangle = default;
        var axis = slot / 2;
        if (!SurfaceNetsEdgeStraddles(cx, cy, cz, axis, out var solidAtOrigin))
            return false;

        // The four cells around the edge as a cycle: (0, 0), (0, -1), (-1, -1), (-1, 0) on the two other axes.
        Span<Vector3> quad = stackalloc Vector3[4];
        for (int i = 0; i < quad.Length; ++i)
        {
            var stepA = i >= 2 ? -1 : 0;
            var stepB = i is 1 or 2 ? -1 : 0;
            int qx = cx, qy = cy, qz = cz;
            switch (axis)
            {
                case 0:
                    qy += stepA;
                    qz += stepB;
                    break;
                case 1:
                    qz += stepA;
                    qx += stepB;
                    break;
                default:
                    qx += stepA;
                    qy += stepB;
                    break;
            }
            if (!TryGetSurfaceNetsVertex(qx, qy, qz, out quad[i]))
                return false;
        }

        triangle = (slot & 1) != 0
            ? new Triangle(quad[0], quad[2], quad[3])
            : new Triangle(quad[0], quad[1], quad[2]);

        // The air is on the side of the edge's sample that is not solid.
        var outward = Vector3.Zero;
        outward[axis] = solidAtOrigin ? 1f : -1f;
        triangle = Orient(triangle, outward);
        return true;
    }

    /// <summary>
    /// The standard marching cubes table: <see cref="TableStride"/> entries per case, edge indices in threes, ended by -1.
    /// </summary>
    private static ReadOnlySpan<sbyte> TriangleTable =>
    [
        -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1,
        3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1,
        3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1, 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1, 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1,
        8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1, 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1,
        3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1, 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1, 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1, 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1, 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1, 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1,
        9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1, 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1,
        10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1, 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1, 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1, 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1, 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1, 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1, 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1, 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1,
        7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1, 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1, 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1,
        9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1, 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1, 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1, 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1,
        1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1, 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1, 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1,
        2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1,
        6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1, 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1, 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1, 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1,
        5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1, 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1,
        6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1, 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1, 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1,
        3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1, 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1,
        8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1, 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1, 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1, 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1,
        10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1, 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1,
        1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1, 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1, 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1,
        10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1, 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1, 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1, 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1,
        9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1, 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1, 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1, 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1, 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1, 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1, 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1, 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1, 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1, 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1, 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1, 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1, 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1,
        8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1, 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1, 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1,
        10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1,
        7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1, 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1,
        10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1, 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1, 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1, 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1,
        6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1, 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1, 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1,
        6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1, 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1, 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1, 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1,
        8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1, 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1,
        8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1, 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1, 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1, 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1,
        9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1, 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1, 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1,
        7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1, 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1, 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1, 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1,
        9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1, 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1, 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1, 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1,
        6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1, 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1, 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1, 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1, 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1, 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1, 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1,
        5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1, 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1, 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1, 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1, 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1, 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1, 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1, 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1,
        11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1, 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1, 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1,
        2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1, 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1, 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1,
        1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1, 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1, 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1, 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1, 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1,
        2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1, 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1, 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1, 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1, 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1, 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1, 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1,
        8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1, 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1, 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1, 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1, 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1, 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1,
        4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1, 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1, 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1, 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1,
        2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1, 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1, 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1, 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1, 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1, 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1, 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1, 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1, 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1, 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1, 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1, 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
        1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
    ];
}
