// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// Finds which cells of a regular grid have their center inside a triangle mesh.
/// </summary>
/// <remarks>
/// Rays are cast through every row of cell centers along each of the three axes, a center is inside when it has an odd number of crossings before it.
/// A cell is kept when at least two of the three axes agree, which tolerates small holes and badly welded seams in the mesh.
/// </remarks>
internal static class Voxelizer
{
    /// <summary> Cell (x, y, z) is at <c>(z * size.Y + y) * size.X + x</c> </summary>
    public static bool[] CellCentersInside(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, Vector3 origin, float cellSize, Int3 size)
    {
        var votes = new byte[size.X * size.Y * size.Z];
        for (int axis = 0; axis < 3; axis++)
            VoteAlong(axis, positions, indices, origin, cellSize, size, votes);

        var inside = new bool[votes.Length];
        for (int i = 0; i < votes.Length; i++)
            inside[i] = votes[i] >= 2;
        return inside;
    }

    /// <summary> Finds the cells of the grid that a triangle of the mesh goes through, laid out as <see cref="CellCentersInside"/> does </summary>
    /// <remarks> The cells on the border of the grid reach out to the bounds of the mesh, for the surface lying outside of the grid </remarks>
    public static bool[] CellsOnSurface(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, Vector3 origin, float cellSize, Int3 size)
    {
        var onSurface = new bool[size.X * size.Y * size.Z];
        Vector3 meshMin = Vector3.Zero, meshMax = new Vector3(size.X, size.Y, size.Z) * cellSize;
        foreach (var position in positions)
        {
            meshMin = Vector3.Min(meshMin, position - origin);
            meshMax = Vector3.Max(meshMax, position - origin);
        }

        float invCell = 1f / cellSize;
        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            var a = positions[indices[t]] - origin;
            var b = positions[indices[t + 1]] - origin;
            var c = positions[indices[t + 2]] - origin;
            var first = CellOf(Vector3.Min(a, Vector3.Min(b, c)) * invCell, size);
            var last = CellOf(Vector3.Max(a, Vector3.Max(b, c)) * invCell, size);

            for (int z = first.Z; z <= last.Z; z++)
            for (int y = first.Y; y <= last.Y; y++)
            for (int x = first.X; x <= last.X; x++)
            {
                ref bool cell = ref onSurface[(z * size.Y + y) * size.X + x];
                if (cell)
                    continue;
                var min = new Vector3(x == 0 ? meshMin.X : x * cellSize, y == 0 ? meshMin.Y : y * cellSize, z == 0 ? meshMin.Z : z * cellSize);
                var max = new Vector3(
                    x == size.X - 1 ? meshMax.X : (x + 1) * cellSize,
                    y == size.Y - 1 ? meshMax.Y : (y + 1) * cellSize,
                    z == size.Z - 1 ? meshMax.Z : (z + 1) * cellSize);
                var center = (min + max) * 0.5f;
                cell = TriangleTouchesBox(a - center, b - center, c - center, (max - min) * 0.5f);
            }
        }
        return onSurface;

        static Int3 CellOf(Vector3 cell, Int3 size) => new(
            Math.Clamp((int)MathF.Floor(cell.X), 0, size.X - 1),
            Math.Clamp((int)MathF.Floor(cell.Y), 0, size.Y - 1),
            Math.Clamp((int)MathF.Floor(cell.Z), 0, size.Z - 1));
    }

    /// <summary> Whether a triangle touches the box of half size <paramref name="half"/> centered on the origin </summary>
    private static bool TriangleTouchesBox(Vector3 a, Vector3 b, Vector3 c, Vector3 half)
    {
        // They are apart when an axis separates them: one of the box's, the triangle's normal, or the cross product of an edge of each
        Vector3 ab = b - a, bc = c - b, ca = a - c;
        if (Separates(Vector3.UnitX, a, b, c, half) || Separates(Vector3.UnitY, a, b, c, half) || Separates(Vector3.UnitZ, a, b, c, half)
            || Separates(Vector3.Cross(ab, bc), a, b, c, half))
            return false;

        foreach (var edge in (ReadOnlySpan<Vector3>)[ab, bc, ca])
        {
            if (Separates(new Vector3(0f, -edge.Z, edge.Y), a, b, c, half)
                || Separates(new Vector3(edge.Z, 0f, -edge.X), a, b, c, half)
                || Separates(new Vector3(-edge.Y, edge.X, 0f), a, b, c, half))
                return false;
        }
        return true;
    }

    private static bool Separates(Vector3 axis, Vector3 a, Vector3 b, Vector3 c, Vector3 half)
    {
        float pa = Vector3.Dot(axis, a), pb = Vector3.Dot(axis, b), pc = Vector3.Dot(axis, c);
        float reach = half.X * MathF.Abs(axis.X) + half.Y * MathF.Abs(axis.Y) + half.Z * MathF.Abs(axis.Z);
        return MathF.Min(pa, MathF.Min(pb, pc)) > reach || MathF.Max(pa, MathF.Max(pb, pc)) < -reach;
    }

    private static void VoteAlong(int axis, ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, Vector3 origin, float cellSize, Int3 size, byte[] votes)
    {
        // The ray runs along 'axis', rows are laid out on the two other axes
        int uAxis = (axis + 1) % 3, vAxis = (axis + 2) % 3;
        int rowsU = size[uAxis], rowsV = size[vAxis], length = size[axis];

        // Offsetting the rows by an odd fraction of a cell keeps them off the edges and vertices of axis aligned geometry
        float jitterU = cellSize * 1.37e-3f, jitterV = cellSize * 2.91e-3f;
        float invCell = 1f / cellSize;

        var crossings = new List<float>?[rowsU * rowsV];
        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            var a = positions[indices[t]];
            var b = positions[indices[t + 1]];
            var c = positions[indices[t + 2]];
            float au = a[uAxis] - origin[uAxis] - jitterU, av = a[vAxis] - origin[vAxis] - jitterV;
            float bu = b[uAxis] - origin[uAxis] - jitterU, bv = b[vAxis] - origin[vAxis] - jitterV;
            float cu = c[uAxis] - origin[uAxis] - jitterU, cv = c[vAxis] - origin[vAxis] - jitterV;

            float area = (bu - au) * (cv - av) - (cu - au) * (bv - av);
            if (MathF.Abs(area) < 1e-20f)
                continue; // Parallel to the ray

            int minU = Math.Max(0, (int)MathF.Ceiling(MathF.Min(au, MathF.Min(bu, cu)) * invCell - 0.5f));
            int maxU = Math.Min(rowsU - 1, (int)MathF.Floor(MathF.Max(au, MathF.Max(bu, cu)) * invCell - 0.5f));
            int minV = Math.Max(0, (int)MathF.Ceiling(MathF.Min(av, MathF.Min(bv, cv)) * invCell - 0.5f));
            int maxV = Math.Min(rowsV - 1, (int)MathF.Floor(MathF.Max(av, MathF.Max(bv, cv)) * invCell - 0.5f));

            float invArea = 1f / area;
            for (int u = minU; u <= maxU; u++)
            {
                float pu = (u + 0.5f) * cellSize;
                for (int v = minV; v <= maxV; v++)
                {
                    float pv = (v + 0.5f) * cellSize;
                    float wa = ((bu - pu) * (cv - pv) - (cu - pu) * (bv - pv)) * invArea;
                    float wb = ((cu - pu) * (av - pv) - (au - pu) * (cv - pv)) * invArea;
                    float wc = 1f - wa - wb;
                    if (wa < 0f || wb < 0f || wc < 0f)
                        continue;

                    float hit = wa * a[axis] + wb * b[axis] + wc * c[axis] - origin[axis];
                    (crossings[u * rowsV + v] ??= new()).Add(hit * invCell - 0.5f);
                }
            }
        }

        Span<int> cell = stackalloc int[3];
        for (int u = 0; u < rowsU; u++)
        {
            for (int v = 0; v < rowsV; v++)
            {
                if (crossings[u * rowsV + v] is not { Count: >= 2 } row)
                    continue;

                row.Sort();
                cell[uAxis] = u;
                cell[vAxis] = v;
                // An odd count means the mesh is open along this row, the last crossing leads nowhere
                for (int i = 0; i + 1 < row.Count; i += 2)
                {
                    int from = Math.Max(0, (int)MathF.Ceiling(row[i]));
                    int to = Math.Min(length - 1, (int)MathF.Floor(row[i + 1]));
                    for (int w = from; w <= to; w++)
                    {
                        cell[axis] = w;
                        votes[(cell[2] * size.Y + cell[1]) * size.X + cell[0]]++;
                    }
                }
            }
        }
    }
}
