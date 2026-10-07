// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// Fills the inside of a closed mesh with a regular lattice of particles, the mesh's vertices follow the lattice cell they are in.
/// </summary>
internal static class LatticeBuilder
{
    // Corners of a cell are numbered by their offset on each axis: bit 0 is x, bit 1 is y and bit 2 is z
    private static readonly Tetrahedron[] EvenCellTetrahedra = [new(0, 3, 5, 6), new(1, 0, 3, 5), new(2, 0, 6, 3), new(4, 0, 5, 6), new(7, 3, 6, 5)];
    private static readonly Tetrahedron[] OddCellTetrahedra = [new(1, 2, 7, 4), new(0, 1, 4, 2), new(3, 1, 2, 7), new(5, 1, 7, 4), new(6, 2, 4, 7)];

    /// <param name="mesh"> The surface to fill </param>
    /// <param name="resolution"> The amount of cells along the longest side of the mesh </param>
    /// <param name="inset"> How far inside the bounds of the mesh the outermost particles sit, as a fraction of a cell </param>
    public static SoftBodyTopology Build(SourceMesh mesh, int resolution, float inset)
    {
        resolution = Math.Max(1, resolution);
        var bounds = mesh.ComputeBounds();
        var extent = bounds.Maximum - bounds.Minimum;
        var longest = MathF.Max(extent.X, MathF.Max(extent.Y, extent.Z));
        if (longest <= 0f)
            throw new InvalidOperationException("Cannot build a volumetric soft body from an empty mesh");

        float cellSize = longest / (resolution + 2f * inset);
        var size = new Int3(
            Math.Max(1, (int)MathF.Round(extent.X / cellSize - 2f * inset)),
            Math.Max(1, (int)MathF.Round(extent.Y / cellSize - 2f * inset)),
            Math.Max(1, (int)MathF.Round(extent.Z / cellSize - 2f * inset)));
        var origin = bounds.Center - new Vector3(size.X, size.Y, size.Z) * (cellSize * 0.5f);

        var occupied = Voxelizer.CellCentersInside(mesh.Positions, mesh.Indices, origin, cellSize, size);
        if (Array.IndexOf(occupied, true) < 0)
            throw new InvalidOperationException("The mesh does not enclose any volume at this resolution, it must be closed, or the resolution higher");

        // Particles sit on the corners of occupied cells
        var nodes = new Int3(size.X + 1, size.Y + 1, size.Z + 1);
        var particleOfNode = new int[nodes.X * nodes.Y * nodes.Z];
        Array.Fill(particleOfNode, -1);
        var restPositions = new List<Vector3>();
        var selfCollisionCells = new List<Int3>();
        Span<int> corners = stackalloc int[8];

        for (int z = 0; z < size.Z; z++)
        for (int y = 0; y < size.Y; y++)
        for (int x = 0; x < size.X; x++)
        {
            if (occupied[CellIndex(x, y, z, size)] == false)
                continue;
            for (int corner = 0; corner < 8; corner++)
            {
                var node = new Int3(x + (corner & 1), y + ((corner >> 1) & 1), z + ((corner >> 2) & 1));
                ref int particle = ref particleOfNode[CellIndex(node.X, node.Y, node.Z, nodes)];
                if (particle >= 0)
                    continue;
                particle = restPositions.Count;
                restPositions.Add(origin + new Vector3(node.X, node.Y, node.Z) * cellSize);
                selfCollisionCells.Add(node);
            }
        }

        var edges = new List<Edge>();
        var tetrahedra = new List<Tetrahedron>();
        var knownEdges = new HashSet<long>();
        for (int z = 0; z < size.Z; z++)
        for (int y = 0; y < size.Y; y++)
        for (int x = 0; x < size.X; x++)
        {
            if (occupied[CellIndex(x, y, z, size)] == false)
                continue;

            CornerParticles(x, y, z, nodes, particleOfNode, corners);
            for (int a = 0; a < 8; a++)
            {
                for (int b = a + 1; b < 8; b++)
                {
                    long key = (long)Math.Min(corners[a], corners[b]) << 32 | (uint)Math.Max(corners[a], corners[b]);
                    if (knownEdges.Add(key))
                        edges.Add(new Edge(corners[a], corners[b], int.PopCount(a ^ b) == 1 ? EdgeKind.Stretch : EdgeKind.Shear));
                }
            }

            // Alternating the split keeps the faces shared by neighboring cells cut along the same diagonal
            foreach (var t in ((x + y + z) & 1) == 0 ? EvenCellTetrahedra : OddCellTetrahedra)
                tetrahedra.Add(new Tetrahedron(corners[t.A], corners[t.B], corners[t.C], corners[t.D]));
        }

        // Each vertex is bound to the eight corners of the cell it is in, or of the closest occupied cell when it lies outside the lattice
        var vertexCount = mesh.Positions.Length;
        var bindingParticles = new int[vertexCount * 8];
        var bindingWeights = new float[vertexCount * 8];
        var latticeCoordinates = new Vector3[vertexCount];
        for (int v = 0; v < vertexCount; v++)
        {
            var local = (mesh.Positions[v] - origin) / cellSize;
            var cell = ClosestOccupiedCell(local, size, occupied);
            var t = local - new Vector3(cell.X, cell.Y, cell.Z);
            latticeCoordinates[v] = t;

            CornerParticles(cell.X, cell.Y, cell.Z, nodes, particleOfNode, corners);
            for (int corner = 0; corner < 8; corner++)
            {
                float wx = (corner & 1) != 0 ? t.X : 1f - t.X;
                float wy = (corner & 2) != 0 ? t.Y : 1f - t.Y;
                float wz = (corner & 4) != 0 ? t.Z : 1f - t.Z;
                bindingParticles[v * 8 + corner] = corners[corner];
                bindingWeights[v * 8 + corner] = wx * wy * wz;
            }
        }

        return new SoftBodyTopology
        {
            RestPositions = restPositions.ToArray(),
            SelfCollisionCells = selfCollisionCells.ToArray(),
            Edges = edges.ToArray(),
            Tetrahedra = tetrahedra.ToArray(),
            Spacing = cellSize,
            BindingStride = 8,
            BindingParticles = bindingParticles,
            BindingWeights = bindingWeights,
            LatticeCoordinates = latticeCoordinates,
            Mesh = mesh,
        };
    }

    private static int CellIndex(int x, int y, int z, Int3 size) => (z * size.Y + y) * size.X + x;

    private static void CornerParticles(int x, int y, int z, Int3 nodes, int[] particleOfNode, Span<int> corners)
    {
        for (int corner = 0; corner < 8; corner++)
            corners[corner] = particleOfNode[CellIndex(x + (corner & 1), y + ((corner >> 1) & 1), z + ((corner >> 2) & 1), nodes)];
    }

    private static Int3 ClosestOccupiedCell(Vector3 local, Int3 size, bool[] occupied)
    {
        var start = new Int3(
            Math.Clamp((int)MathF.Floor(local.X), 0, size.X - 1),
            Math.Clamp((int)MathF.Floor(local.Y), 0, size.Y - 1),
            Math.Clamp((int)MathF.Floor(local.Z), 0, size.Z - 1));
        if (occupied[CellIndex(start.X, start.Y, start.Z, size)])
            return start;

        // Grow a shell around the starting cell until it contains occupied cells, then keep the one whose center is closest
        int maxRing = Math.Max(size.X, Math.Max(size.Y, size.Z));
        for (int ring = 1; ring <= maxRing; ring++)
        {
            var best = new Int3(-1, -1, -1);
            float bestDistance = float.MaxValue;
            for (int z = Math.Max(0, start.Z - ring); z <= Math.Min(size.Z - 1, start.Z + ring); z++)
            for (int y = Math.Max(0, start.Y - ring); y <= Math.Min(size.Y - 1, start.Y + ring); y++)
            for (int x = Math.Max(0, start.X - ring); x <= Math.Min(size.X - 1, start.X + ring); x++)
            {
                if (Math.Max(Math.Abs(x - start.X), Math.Max(Math.Abs(y - start.Y), Math.Abs(z - start.Z))) != ring || occupied[CellIndex(x, y, z, size)] == false)
                    continue;
                float distance = Vector3.DistanceSquared(local, new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = new Int3(x, y, z);
                }
            }

            if (best.X >= 0)
                return best;
        }

        throw new InvalidOperationException("No occupied cell");
    }
}
