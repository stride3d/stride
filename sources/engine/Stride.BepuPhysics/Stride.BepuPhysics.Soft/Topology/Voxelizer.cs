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
