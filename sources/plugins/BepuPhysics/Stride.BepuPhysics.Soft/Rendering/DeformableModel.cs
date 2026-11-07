// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Semantics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// A copy of a soft body's model whose vertex positions, normals and tangents are rewritten every frame from the
/// position of its particles. Every other attribute of the source vertex buffer (texture coordinates, colors, extra
/// UVs...) is kept as the source authored it.
/// </summary>
internal sealed class DeformableModel : IDisposable
{
    private static readonly ConditionalWeakTable<Model, Model> SourceOfModel = new();

    // One entry per source Mesh, in source.Meshes order
    private readonly MeshBuffer[] _meshBuffers;
    private readonly Vector3[] _particles;
    private readonly Vector3[] _positions;
    private readonly Vector3[] _normals;
    private readonly Vector4[] _tangents;

    public Model Model { get; }

    public SoftBodyTopology Topology { get; }

    public DeformableModel(GraphicsDevice device, IServiceRegistry services, Model source, SoftBodyTopology topology)
    {
        Topology = topology;
        var meshPool = topology.Mesh;
        int vertexCount = meshPool.Positions.Length;

        _particles = new Vector3[topology.ParticleCount];
        _positions = new Vector3[vertexCount];
        _normals = new Vector3[vertexCount];
        _tangents = new Vector4[vertexCount];

        if (source.Meshes.Count != meshPool.Parts.Length)
            throw new InvalidOperationException($"A deformable model expected {source.Meshes.Count} mesh ranges from the topology, got {meshPool.Parts.Length}.");

        _meshBuffers = new MeshBuffer[source.Meshes.Count];
        Model = new Model();
        SourceOfModel.AddOrUpdate(Model, source);
        foreach (var material in source.Materials)
            Model.Materials.Add(material);

        for (int m = 0; m < source.Meshes.Count; m++)
        {
            var sourceMesh = source.Meshes[m];
            var part = meshPool.Parts[m];
            if (sourceMesh.Draw.VertexBuffers.Length != 1)
                throw new NotSupportedException($"Soft bodies only support source meshes with a single vertex stream, '{sourceMesh.Name}' has {sourceMesh.Draw.VertexBuffers.Length}.");

            var sourceBinding = sourceMesh.Draw.VertexBuffers[0];
            var declaration = sourceBinding.Declaration;
            if (sourceBinding.Count != part.VertexCount)
                throw new InvalidOperationException($"Soft body topology reports {part.VertexCount} vertices for '{sourceMesh.Name}', the source binding has {sourceBinding.Count}.");

            int stride = declaration.VertexStride;
            int positionOffset = FindOffset<PositionSemantic>(declaration, PixelFormat.R32G32B32_Float, sourceMesh.Name);
            int normalOffset = FindOffset<NormalSemantic>(declaration, PixelFormat.R32G32B32_Float, sourceMesh.Name);
            int tangentOffset = FindOffset<TangentSemantic>(declaration, PixelFormat.R32G32B32A32_Float, sourceMesh.Name);

            // A template snapshot of the source bytes, re-copied to the working buffer before each deformation so attributes we
            // do not touch stay authored. DataInner accounts for the binding offset and length.
            var readable = new VertexBufferHelper(sourceBinding, services, out _);
            var inner = readable.DataInner;
            var template = new byte[inner.Length];
            inner.CopyTo(template);

            var working = new byte[template.Length];
            template.AsSpan().CopyTo(working);

            var vertexBuffer = Buffer.Vertex.New(device, bufferSize: template.Length, usage: GraphicsResourceUsage.Dynamic);
            var vertexBinding = new VertexBufferBinding(vertexBuffer, declaration, part.VertexCount);

            _meshBuffers[m] = new MeshBuffer
            {
                Template = template,
                Working = working,
                VertexBuffer = vertexBuffer,
                Binding = vertexBinding,
                Stride = stride,
                PositionOffset = positionOffset,
                NormalOffset = normalOffset,
                TangentOffset = tangentOffset,
                FirstVertex = part.FirstVertex,
                VertexCount = part.VertexCount,
            };

            Model.Meshes.Add(new Mesh
            {
                MaterialIndex = sourceMesh.MaterialIndex,
                Draw = new MeshDraw
                {
                    PrimitiveType = sourceMesh.Draw.PrimitiveType,
                    StartLocation = sourceMesh.Draw.StartLocation,
                    DrawCount = sourceMesh.Draw.DrawCount,
                    VertexBuffers = [vertexBinding],
                    IndexBuffer = sourceMesh.Draw.IndexBuffer,
                },
            });
        }
    }

    private static int FindOffset<TSemantic>(VertexDeclaration declaration, PixelFormat expected, string meshName) where TSemantic : ISemantic
    {
        string semanticName = TSemantic.Name;
        foreach (var entry in declaration.EnumerateWithOffsets())
        {
            if (entry.VertexElement.SemanticName != semanticName || entry.VertexElement.SemanticIndex != 0)
                continue;
            if (entry.VertexElement.Format != expected)
                throw new NotSupportedException($"Soft body deformation writes {semanticName} as {expected}, '{meshName}' has {entry.VertexElement.Format}.");
            return entry.Offset;
        }
        throw new NotSupportedException($"Soft body deformation expected {semanticName} in '{meshName}', the vertex declaration has none.");
    }

    /// <summary> The model a deformable model was copied from, or <paramref name="model"/> itself when it is not one, such as on a cloned entity </summary>
    public static Model SourceOf(Model model) => SourceOfModel.TryGetValue(model, out var source) ? source : model;

    /// <summary> Computes the vertices from the current particles, in the space of <paramref name="world"/>; safe to run for several models at once </summary>
    public void Deform(SoftBodyComponent body, Matrix world)
    {
        body.CopyParticlePositions(_particles);
        Topology.Deform(_particles, _positions);

        // Normals and tangents are first found in world space
        if (Topology.LatticeCoordinates is { } coordinates)
        {
            DeformFrames(coordinates);
        }
        else
        {
            RebuildFrames();

            // Pushes the two faces of a double sided model apart, just enough for them not to fight over the same depth
            if (body.Collider is Colliders.ParticleCollider particle)
            {
                var offset = particle.ActualRadius * 0.1f;
                for (int i = 0; i < _positions.Length; i++)
                    _positions[i] += _normals[i] * offset;
            }
        }

        var worldToLocal = Matrix.Invert(world);
        world.Decompose(out var scale, out Quaternion rotation, out _);
        var toLocal = Quaternion.Conjugate(rotation);
        var bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        var sourceNormals = Topology.Mesh.Normals;
        var sourceTangents = Topology.Mesh.Tangents;

        foreach (var buffer in _meshBuffers)
        {
            // Template copy restores every attribute the source authored; the writes below only overwrite position, normal and tangent.
            buffer.Template.AsSpan().CopyTo(buffer.Working);
            var working = buffer.Working.AsSpan();
            int stride = buffer.Stride;
            int firstVertex = buffer.FirstVertex;
            for (int i = 0; i < buffer.VertexCount; i++)
            {
                int mergedId = firstVertex + i;
                int byteStart = i * stride;

                var position = Vector3.TransformCoordinate(_positions[mergedId], worldToLocal);
                // Undoing the scale stretches normals the other way than directions, as the inverse transpose would
                var normal = SafeNormalize(Vector3.Transform(_normals[mergedId], toLocal) * scale, sourceNormals[mergedId]);
                var raw = Vector3.Transform((Vector3)_tangents[mergedId], toLocal) / scale;
                raw -= normal * Vector3.Dot(normal, raw);
                var tangent = new Vector4(SafeNormalize(raw, (Vector3)sourceTangents[mergedId]), _tangents[mergedId].W);

                MemoryMarshal.Write(working.Slice(byteStart + buffer.PositionOffset, 12), in position);
                MemoryMarshal.Write(working.Slice(byteStart + buffer.NormalOffset, 12), in normal);
                MemoryMarshal.Write(working.Slice(byteStart + buffer.TangentOffset, 16), in tangent);

                BoundingBox.Merge(ref bounds, ref position, out bounds);
            }
        }

        var sphere = BoundingSphere.FromBox(bounds);
        foreach (var mesh in Model.Meshes)
        {
            mesh.BoundingBox = bounds;
            mesh.BoundingSphere = sphere;
        }
        Model.BoundingBox = bounds;
        Model.BoundingSphere = sphere;
    }

    /// <summary> Sends the vertices computed by <see cref="Deform"/> to the GPU </summary>
    public void Upload(CommandList commandList)
    {
        foreach (var buffer in _meshBuffers)
            buffer.VertexBuffer.SetData<byte>(commandList, buffer.Working.AsSpan());
    }

    /// <summary> Carries the authored normals and tangents along the deformation of the lattice cell each vertex is in </summary>
    private void DeformFrames(Vector3[] coordinates)
    {
        for (int v = 0; v < coordinates.Length; v++)
            Topology.DeformFrame(v, _particles, out _normals[v], out _tangents[v]);
    }

    /// <summary> Recomputes normals and tangents from the deformed triangles, for surfaces whose vertices are the particles themselves </summary>
    private void RebuildFrames()
    {
        var mesh = Topology.Mesh;
        Array.Clear(_tangents);
        foreach (var part in mesh.Parts)
        {
            var indices = mesh.Indices.AsSpan(part.FirstIndex, part.IndexCount);
            SourceMesh.ComputeNormals(_positions, indices, _normals, mesh.Clockwise);
            SourceMesh.ComputeTangents(_positions, _normals, mesh.TexCoords, indices, _tangents);
        }
    }

    private static Vector3 SafeNormalize(Vector3 value, Vector3 fallback)
    {
        var length = value.Length();
        return length > 1e-12f ? value / length : fallback;
    }

    public void Dispose()
    {
        foreach (var buffer in _meshBuffers)
            buffer.VertexBuffer.Dispose();
    }

    private sealed class MeshBuffer
    {
        public required byte[] Template;
        public required byte[] Working;
        public required Buffer VertexBuffer;
        public required VertexBufferBinding Binding;
        public required int Stride;
        public required int PositionOffset;
        public required int NormalOffset;
        public required int TangentOffset;
        public required int FirstVertex;
        public required int VertexCount;
    }
}
