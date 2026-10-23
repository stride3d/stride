// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Semantics;
using Stride.Rendering;

namespace Stride.BepuPhysics.Soft.Topology;

/// <summary>
/// CPU copy of the vertex streams of a <see cref="Model"/> a soft body deforms, all meshes flattened into the model's space.
/// </summary>
internal sealed class SourceMesh
{
    public required Vector3[] Positions { get; init; }
    public required Vector3[] Normals { get; init; }
    public required Vector4[] Tangents { get; init; }
    public required Vector2[] TexCoords { get; init; }
    public required int[] Indices { get; init; }
    public required Part[] Parts { get; init; }

    /// <summary> Whether triangles face the side they are seen clockwise from, as Stride's default rasterizer state expects </summary>
    public bool Clockwise { get; init; } = true;

    /// <summary> A range of <see cref="Positions"/> and <see cref="Indices"/> coming from one <see cref="Mesh"/> </summary>
    public readonly record struct Part(int FirstVertex, int VertexCount, int FirstIndex, int IndexCount, int MaterialIndex);

    public static SourceMesh FromModel(Model model, IServiceRegistry services)
    {
        var nodeTransforms = NodeTransforms(model);

        int vertexCount = 0, indexCount = 0;
        foreach (var mesh in model.Meshes)
        {
            if (mesh.Draw.PrimitiveType != PrimitiveType.TriangleList)
                throw new NotSupportedException($"Soft bodies only support triangle lists, '{model}' contains a {mesh.Draw.PrimitiveType} mesh");
            vertexCount += mesh.Draw.VertexBuffers[0].Count;
            indexCount += mesh.Draw.IndexBuffer?.Count ?? mesh.Draw.VertexBuffers[0].Count;
        }

        var positions = new Vector3[vertexCount];
        var normals = new Vector3[vertexCount];
        var tangents = new Vector4[vertexCount];
        var texCoords = new Vector2[vertexCount];
        var indices = new int[indexCount];
        var parts = new Part[model.Meshes.Count];

        int firstVertex = 0, firstIndex = 0;
        float windingAgreement = 0f;
        for (int m = 0; m < model.Meshes.Count; m++)
        {
            var mesh = model.Meshes[m];
            mesh.Draw.VertexBuffers[0].AsReadable(services, out var vertices, out int count);

            var meshPositions = positions.AsSpan(firstVertex, count);
            vertices.Copy<PositionSemantic, Vector3>(meshPositions);
            bool hasNormals = vertices.Copy<NormalSemantic, Vector3>(normals.AsSpan(firstVertex, count));
            bool hasTangents = vertices.Copy<TangentSemantic, Vector4>(tangents.AsSpan(firstVertex, count));
            vertices.Copy<TextureCoordinateSemantic, Vector2>(texCoords.AsSpan(firstVertex, count));

            int meshIndexCount;
            var meshIndices = indices.AsSpan(firstIndex);
            if (mesh.Draw.IndexBuffer is { } indexBuffer)
            {
                indexBuffer.AsReadable(services, out var indexHelper, out meshIndexCount);
                indexHelper.CopyTo(meshIndices[..meshIndexCount]);
            }
            else
            {
                meshIndexCount = count;
                for (int i = 0; i < count; i++)
                    meshIndices[i] = i;
            }
            meshIndices = meshIndices[..meshIndexCount];

            if (nodeTransforms is not null && mesh.NodeIndex > 0)
            {
                var transform = nodeTransforms[mesh.NodeIndex];
                transform.Decompose(out var scale, out Quaternion rotation, out _);
                for (int i = 0; i < count; i++)
                {
                    meshPositions[i] = Vector3.TransformCoordinate(meshPositions[i], transform);
                    // Under a non uniform scale, normals stretch the other way than directions; missing frames are computed below
                    if (hasNormals)
                    {
                        ref var normal = ref normals[firstVertex + i];
                        normal = Vector3.Normalize(Vector3.Transform(normal / scale, rotation));
                    }
                    if (hasTangents)
                    {
                        ref var tangent = ref tangents[firstVertex + i];
                        tangent = new Vector4(Vector3.Normalize(Vector3.Transform((Vector3)tangent * scale, rotation)), tangent.W);
                    }
                }
            }

            for (int i = 0; i < meshIndices.Length; i++)
                meshIndices[i] += firstVertex;

            if (hasNormals)
            {
                // Which winding the authored normals agree with, recomputed normals must point the same way
                for (int i = 0; i + 2 < meshIndices.Length; i += 3)
                {
                    var a = positions[meshIndices[i]];
                    var counterClockwiseNormal = Vector3.Cross(positions[meshIndices[i + 1]] - a, positions[meshIndices[i + 2]] - a);
                    windingAgreement += Vector3.Dot(counterClockwiseNormal, normals[meshIndices[i]] + normals[meshIndices[i + 1]] + normals[meshIndices[i + 2]]);
                }
            }
            else
            {
                ComputeNormals(positions, meshIndices, normals);
            }
            if (hasTangents == false)
                ComputeTangents(positions, normals, texCoords, meshIndices, tangents);

            parts[m] = new Part(firstVertex, count, firstIndex, meshIndexCount, mesh.MaterialIndex);
            firstVertex += count;
            firstIndex += meshIndexCount;
        }

        return new SourceMesh
        {
            Positions = positions,
            Normals = normals,
            Tangents = tangents,
            TexCoords = texCoords,
            Indices = indices,
            Parts = parts,
            Clockwise = windingAgreement <= 0f,
        };
    }

    public BoundingBox ComputeBounds()
    {
        var bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int i = 0; i < Positions.Length; i++)
            BoundingBox.Merge(ref bounds, ref Positions[i], out bounds);
        return bounds;
    }

    /// <summary> Area weighted vertex normals, <paramref name="normals"/> is only written to for vertices referenced by <paramref name="indices"/> </summary>
    public static void ComputeNormals(ReadOnlySpan<Vector3> positions, ReadOnlySpan<int> indices, Span<Vector3> normals, bool clockwise = true)
    {
        for (int i = 0; i < indices.Length; i++)
            normals[indices[i]] = default;

        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            var faceNormal = Vector3.Cross(positions[b] - positions[a], positions[c] - positions[a]);
            if (clockwise)
                faceNormal = -faceNormal;
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }

        for (int i = 0; i < indices.Length; i++)
        {
            ref var normal = ref normals[indices[i]];
            var length = normal.Length();
            if (length > 1e-12f && MathF.Abs(length - 1f) > 1e-6f)
                normal /= length;
        }
    }

    public static void ComputeTangents(ReadOnlySpan<Vector3> positions, ReadOnlySpan<Vector3> normals, ReadOnlySpan<Vector2> texCoords, ReadOnlySpan<int> indices, Span<Vector4> tangents)
    {
        for (int i = 0; i + 2 < indices.Length; i += 3)
        {
            int a = indices[i], b = indices[i + 1], c = indices[i + 2];
            var edge1 = positions[b] - positions[a];
            var edge2 = positions[c] - positions[a];
            var uv1 = texCoords[b] - texCoords[a];
            var uv2 = texCoords[c] - texCoords[a];
            var determinant = uv1.X * uv2.Y - uv2.X * uv1.Y;
            var tangent = MathF.Abs(determinant) > 1e-12f ? (edge1 * uv2.Y - edge2 * uv1.Y) / determinant : edge1;
            tangents[a] += new Vector4(tangent, 0f);
            tangents[b] += new Vector4(tangent, 0f);
            tangents[c] += new Vector4(tangent, 0f);
        }

        for (int i = 0; i < indices.Length; i++)
        {
            int v = indices[i];
            var normal = normals[v];
            var tangent = (Vector3)tangents[v];
            tangent -= normal * Vector3.Dot(normal, tangent);
            if (tangent.LengthSquared() < 1e-12f)
                tangent = Vector3.Cross(normal, MathF.Abs(normal.X) < 0.9f ? Vector3.UnitX : Vector3.UnitY);
            tangents[v] = new Vector4(Vector3.Normalize(tangent), 1f);
        }
    }

    private static Matrix[]? NodeTransforms(Model model)
    {
        if (model.Skeleton is null)
            return null;

        var nodes = model.Skeleton.Nodes;
        var transforms = new Matrix[nodes.Length];
        transforms[0] = Matrix.Identity; // Same as the colliders, the root node is the model's own space
        for (int i = 1; i < nodes.Length; i++)
        {
            var node = nodes[i];
            Matrix.Transformation(ref node.Transform.Scale, ref node.Transform.Rotation, ref node.Transform.Position, out var local);
            transforms[i] = node.ParentIndex >= 0 ? local * transforms[node.ParentIndex] : local;
        }
        return transforms;
    }
}
