// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// Turns the vertices of a surface into particles, its edges into stretch constraints and each pair of adjacent triangles into a bend constraint.
/// </summary>
internal static class ClothBuilder
{
    public static SoftBodyTopology Build(SourceMesh mesh)
    {
        var bounds = mesh.ComputeBounds();
        var weldDistance = MathF.Max(1e-6f, (bounds.Maximum - bounds.Minimum).Length() * 1e-5f);

        // Vertices duplicated for UV seams, flat shading or a back face become a single particle
        var particleOfVertex = new int[mesh.Positions.Length];
        var restPositions = new List<Vector3>();
        var particleOfPosition = new Dictionary<(long, long, long), int>();
        for (int v = 0; v < mesh.Positions.Length; v++)
        {
            var position = mesh.Positions[v];
            var key = ((long)MathF.Round(position.X / weldDistance), (long)MathF.Round(position.Y / weldDistance), (long)MathF.Round(position.Z / weldDistance));
            if (particleOfPosition.TryGetValue(key, out int particle) == false)
            {
                particle = restPositions.Count;
                particleOfPosition.Add(key, particle);
                restPositions.Add(position);
            }
            particleOfVertex[v] = particle;
        }

        // For each edge, the vertex opposite to it in the first triangle that used it
        var oppositeOfEdge = new Dictionary<long, int>();
        var edges = new List<Edge>();
        var bendPairs = new HashSet<long>();
        var indices = mesh.Indices;
        Span<int> triangle = stackalloc int[3];
        for (int t = 0; t + 2 < indices.Length; t += 3)
        {
            triangle[0] = particleOfVertex[indices[t]];
            triangle[1] = particleOfVertex[indices[t + 1]];
            triangle[2] = particleOfVertex[indices[t + 2]];
            if (triangle[0] == triangle[1] || triangle[1] == triangle[2] || triangle[0] == triangle[2])
                continue;

            for (int i = 0; i < 3; i++)
            {
                int a = triangle[i], b = triangle[(i + 1) % 3], opposite = triangle[(i + 2) % 3];
                long key = Key(a, b);
                if (oppositeOfEdge.TryGetValue(key, out int otherOpposite))
                {
                    // Second triangle on this edge, or a back face sharing all three particles
                    if (otherOpposite != opposite)
                        bendPairs.Add(Key(otherOpposite, opposite));
                }
                else
                {
                    oppositeOfEdge.Add(key, opposite);
                    edges.Add(new Edge(a, b, EdgeKind.Stretch));
                }
            }
        }

        float totalLength = 0f;
        foreach (var edge in edges)
            totalLength += Vector3.Distance(restPositions[edge.A], restPositions[edge.B]);
        float spacing = edges.Count > 0 ? totalLength / edges.Count : 1f;

        foreach (var pair in bendPairs)
        {
            if (oppositeOfEdge.ContainsKey(pair) == false)
                edges.Add(new Edge((int)(pair >> 32), (int)(pair & 0xFFFFFFFF), EdgeKind.Bend));
        }

        var selfCollisionCells = new Int3[restPositions.Count];
        for (int p = 0; p < restPositions.Count; p++)
        {
            var cell = (restPositions[p] - bounds.Minimum) / spacing;
            selfCollisionCells[p] = new Int3((int)MathF.Round(cell.X), (int)MathF.Round(cell.Y), (int)MathF.Round(cell.Z));
        }

        var bindingWeights = new float[mesh.Positions.Length];
        Array.Fill(bindingWeights, 1f);

        return new SoftBodyTopology
        {
            RestPositions = restPositions.ToArray(),
            SelfCollisionCells = selfCollisionCells,
            Edges = edges.ToArray(),
            Tetrahedra = [],
            Spacing = spacing,
            BindingStride = 1,
            BindingParticles = particleOfVertex,
            BindingWeights = bindingWeights,
            Mesh = mesh,
        };
    }

    private static long Key(int a, int b) => (long)Math.Min(a, b) << 32 | (uint)Math.Max(a, b);
}
