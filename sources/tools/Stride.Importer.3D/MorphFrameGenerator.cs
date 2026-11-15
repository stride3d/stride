// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Importer.ThreeD
{
    /// <summary>
    /// Computes the normals and tangents of morph target shapes, so missing normal and tangent deltas can be generated
    /// as the difference between a target's frames and the base shape's frames.
    /// </summary>
    /// <remarks>
    /// The base shape is computed once. A target only recomputes the faces around the vertices it moves and adds
    /// their change to the base sums, so the cost follows the size of the morph rather than the size of the mesh.
    /// </remarks>
    internal sealed class MorphFrameGenerator
    {
        // Vertices at one position whose base normals are closer than this (cosine) are smoothed together; others are hard edges.
        private const float SmoothingNormalCosine = 0.99f;
        // Below this UV area (twice the signed area) a face has no usable tangent direction.
        private const float MinUvDeterminant = 1e-12f;
        // Shorter vectors normalize to zero instead of amplifying rounding noise.
        private const float MinNormalizeLength = 1e-20f;

        private readonly int[] triangles;
        private readonly Vector2[] uvs;
        // Vertices split only by UV seams share normals: the representative vertex at the same position with a similar base normal.
        private readonly int[] smoothingGroup;
        private readonly int[] groupStart;
        private readonly int[] groupVertices;
        private readonly int[] faceStart;
        private readonly int[] vertexFaces;
        private readonly Vector3[] baseFaceNormals;
        private readonly Vector3[] baseFaceTangents;
        private readonly Vector3[] baseNormalSums;
        private readonly Vector3[] baseTangentSums;

        public MorphFrameGenerator(Vector3[] positions, Vector3[] normals, Vector2[] uvs, int[] triangles)
        {
            this.triangles = triangles;
            this.uvs = uvs;
            var vertexCount = positions.Length;
            var faceCount = triangles.Length / 3;

            smoothingGroup = new int[vertexCount];
            var representatives = new Dictionary<Vector3, List<int>>();
            for (int vertex = 0; vertex < vertexCount; vertex++)
            {
                if (!representatives.TryGetValue(positions[vertex], out var candidates))
                    representatives.Add(positions[vertex], candidates = new List<int>());

                smoothingGroup[vertex] = vertex;
                foreach (var candidate in candidates)
                {
                    // Hard edges keep separate normals.
                    if (Vector3.Dot(normals[candidate], normals[vertex]) > SmoothingNormalCosine)
                    {
                        smoothingGroup[vertex] = candidate;
                        break;
                    }
                }
                if (smoothingGroup[vertex] == vertex)
                    candidates.Add(vertex);
            }
            (groupStart, groupVertices) = BuildAdjacency(vertexCount, vertexCount, (add) =>
            {
                for (int vertex = 0; vertex < vertexCount; vertex++)
                    add(smoothingGroup[vertex], vertex);
            });
            (faceStart, vertexFaces) = BuildAdjacency(vertexCount, triangles.Length, (add) =>
            {
                for (int i = 0; i < triangles.Length; i++)
                    add(triangles[i], i / 3);
            });

            baseFaceNormals = new Vector3[faceCount];
            baseNormalSums = new Vector3[vertexCount];
            BaseNormals = new Vector3[vertexCount];
            for (int face = 0; face < faceCount; face++)
            {
                baseFaceNormals[face] = FaceNormal(positions, face);
                for (int corner = 0; corner < 3; corner++)
                    baseNormalSums[smoothingGroup[triangles[face * 3 + corner]]] += baseFaceNormals[face];
            }
            for (int vertex = 0; vertex < vertexCount; vertex++)
                BaseNormals[vertex] = Normalize(baseNormalSums[smoothingGroup[vertex]]);

            if (uvs != null)
            {
                baseFaceTangents = new Vector3[faceCount];
                baseTangentSums = new Vector3[vertexCount];
                BaseTangents = new Vector3[vertexCount];
                for (int face = 0; face < faceCount; face++)
                {
                    baseFaceTangents[face] = FaceTangent(positions, face);
                    for (int corner = 0; corner < 3; corner++)
                        baseTangentSums[triangles[face * 3 + corner]] += baseFaceTangents[face];
                }
                for (int vertex = 0; vertex < vertexCount; vertex++)
                    BaseTangents[vertex] = Orthonormalize(baseTangentSums[vertex], BaseNormals[vertex]);
            }
        }

        public bool CanGenerateTangents => uvs != null;

        /// <summary>
        /// Area-weighted vertex normals of the base shape.
        /// </summary>
        public Vector3[] BaseNormals { get; }

        /// <summary>
        /// Vertex tangents of the base shape from its first UV channel, or <c>null</c> without UVs.
        /// </summary>
        public Vector3[] BaseTangents { get; }

        /// <summary>
        /// Per-thread working memory for <see cref="Generate"/>.
        /// </summary>
        public sealed class Scratch
        {
            internal readonly int[] FaceStamp;
            internal readonly int[] GroupStamp;
            internal readonly int[] VertexStamp;
            internal readonly Vector3[] NormalSumDeltas;
            internal readonly Vector3[] TangentSumDeltas;
            internal readonly List<int> Faces = new();
            internal readonly List<int> Groups = new();
            internal int Stamp;

            internal Scratch(int vertexCount, int faceCount)
            {
                FaceStamp = new int[faceCount];
                GroupStamp = new int[vertexCount];
                VertexStamp = new int[vertexCount];
                NormalSumDeltas = new Vector3[vertexCount];
                TangentSumDeltas = new Vector3[vertexCount];
                Normals = new Vector3[vertexCount];
                Tangents = new Vector3[vertexCount];
            }

            /// <summary>
            /// The vertices whose normal or tangent may differ from the base shape.
            /// </summary>
            public List<int> Vertices { get; } = new();

            /// <summary>
            /// The target's normals, valid for <see cref="Vertices"/>.
            /// </summary>
            public Vector3[] Normals { get; }

            /// <summary>
            /// The target's tangents, valid for <see cref="Vertices"/> when tangents were generated.
            /// </summary>
            public Vector3[] Tangents { get; }

            /// <summary>
            /// Whether <paramref name="vertex"/> is in <see cref="Vertices"/>.
            /// </summary>
            public bool Contains(int vertex)
            {
                return VertexStamp[vertex] == Stamp;
            }
        }

        public Scratch CreateScratch()
        {
            return new Scratch(smoothingGroup.Length, baseFaceNormals.Length);
        }

        /// <summary>
        /// Computes the frames of a target shape around the vertices it moves.
        /// </summary>
        /// <param name="positions">The target's positions.</param>
        /// <param name="movedVertices">The vertices whose target position differs from the base shape.</param>
        /// <param name="tangents">Whether to also compute tangents.</param>
        /// <param name="scratch">Receives the affected vertices and their frames.</param>
        public void Generate(ReadOnlySpan<Vector3> positions, List<int> movedVertices, bool tangents, Scratch scratch)
        {
            var stamp = ++scratch.Stamp;
            scratch.Faces.Clear();
            scratch.Groups.Clear();
            scratch.Vertices.Clear();

            foreach (var vertex in movedVertices)
            {
                for (int i = faceStart[vertex]; i < faceStart[vertex + 1]; i++)
                {
                    var face = vertexFaces[i];
                    if (scratch.FaceStamp[face] != stamp)
                    {
                        scratch.FaceStamp[face] = stamp;
                        scratch.Faces.Add(face);
                    }
                }
            }

            foreach (var face in scratch.Faces)
            {
                var normalDelta = FaceNormal(positions, face) - baseFaceNormals[face];
                var tangentDelta = tangents ? FaceTangent(positions, face) - baseFaceTangents[face] : Vector3.Zero;
                for (int corner = 0; corner < 3; corner++)
                {
                    var vertex = triangles[face * 3 + corner];
                    var group = smoothingGroup[vertex];
                    if (scratch.GroupStamp[group] != stamp)
                    {
                        scratch.GroupStamp[group] = stamp;
                        scratch.NormalSumDeltas[group] = Vector3.Zero;
                        scratch.Groups.Add(group);
                    }
                    scratch.NormalSumDeltas[group] += normalDelta;
                    MarkVertex(scratch, vertex);
                    scratch.TangentSumDeltas[vertex] += tangentDelta;
                }
            }

            // Seam twins share their group's normal even when none of their own faces changed.
            foreach (var group in scratch.Groups)
            {
                var normal = Normalize(baseNormalSums[group] + scratch.NormalSumDeltas[group]);
                for (int i = groupStart[group]; i < groupStart[group + 1]; i++)
                {
                    var vertex = groupVertices[i];
                    MarkVertex(scratch, vertex);
                    scratch.Normals[vertex] = normal;
                }
            }

            if (tangents)
            {
                foreach (var vertex in scratch.Vertices)
                    scratch.Tangents[vertex] = Orthonormalize(baseTangentSums[vertex] + scratch.TangentSumDeltas[vertex], scratch.Normals[vertex]);
            }
        }

        private static void MarkVertex(Scratch scratch, int vertex)
        {
            if (scratch.VertexStamp[vertex] == scratch.Stamp)
                return;
            scratch.VertexStamp[vertex] = scratch.Stamp;
            scratch.TangentSumDeltas[vertex] = Vector3.Zero;
            scratch.Vertices.Add(vertex);
        }

        private Vector3 FaceNormal(ReadOnlySpan<Vector3> positions, int face)
        {
            var a = positions[triangles[face * 3]];
            return Vector3.Cross(positions[triangles[face * 3 + 1]] - a, positions[triangles[face * 3 + 2]] - a);
        }

        private Vector3 FaceTangent(ReadOnlySpan<Vector3> positions, int face)
        {
            int a = triangles[face * 3], b = triangles[face * 3 + 1], c = triangles[face * 3 + 2];
            var uv1 = uvs[b] - uvs[a];
            var uv2 = uvs[c] - uvs[a];
            float determinant = uv1.X * uv2.Y - uv2.X * uv1.Y;
            if (MathF.Abs(determinant) < MinUvDeterminant)
                return Vector3.Zero;
            return ((positions[b] - positions[a]) * uv2.Y - (positions[c] - positions[a]) * uv1.Y) / determinant;
        }

        private static (int[] Start, int[] Items) BuildAdjacency(int keyCount, int itemCount, Action<Action<int, int>> visit)
        {
            var start = new int[keyCount + 1];
            visit((key, _) => start[key + 1]++);
            for (int key = 0; key < keyCount; key++)
                start[key + 1] += start[key];
            var items = new int[itemCount];
            var next = (int[])start.Clone();
            visit((key, item) => items[next[key]++] = item);
            return (start, items);
        }

        private static Vector3 Orthonormalize(Vector3 tangent, Vector3 normal)
        {
            return Normalize(tangent - normal * Vector3.Dot(normal, tangent));
        }

        private static Vector3 Normalize(Vector3 value)
        {
            float length = value.Length();
            return length > MinNormalizeLength && float.IsFinite(length) ? value / length : Vector3.Zero;
        }
    }
}
