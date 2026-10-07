// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// A copy of a soft body's model whose vertices are rewritten every frame from the position of its particles.
/// </summary>
internal sealed class DeformableModel : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Vertex
    {
        public static readonly VertexDeclaration Layout = new(
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Vector3>(),
            VertexElement.Tangent<Vector4>(),
            VertexElement.TextureCoordinate<Vector2>());

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
        public Vector2 TexCoord;
    }

    private static readonly ConditionalWeakTable<Model, Model> SourceOfModel = new();

    private readonly Buffer _vertexBuffer;
    private readonly Buffer _indexBuffer;
    private readonly Vertex[] _vertices;
    private readonly Vector3[] _particles;
    private readonly Vector3[] _positions;
    private readonly Vector3[] _normals;
    private readonly Vector4[] _tangents;

    public Model Model { get; }

    public SoftBodyTopology Topology { get; }

    public DeformableModel(GraphicsDevice device, Model source, SoftBodyTopology topology)
    {
        Topology = topology;
        var mesh = topology.Mesh;
        int vertexCount = mesh.Positions.Length;

        _vertices = new Vertex[vertexCount];
        for (int i = 0; i < vertexCount; i++)
            _vertices[i].TexCoord = mesh.TexCoords[i];
        _particles = new Vector3[topology.ParticleCount];
        _positions = new Vector3[vertexCount];
        _normals = new Vector3[vertexCount];
        _tangents = new Vector4[vertexCount];

        _vertexBuffer = Buffer.Vertex.New(device, bufferSize: vertexCount * Vertex.Layout.VertexStride, usage: GraphicsResourceUsage.Dynamic);
        _indexBuffer = Buffer.Index.New(device, mesh.Indices);

        Model = new Model();
        SourceOfModel.AddOrUpdate(Model, source);
        foreach (var material in source.Materials)
            Model.Materials.Add(material);

        var vertexBinding = new VertexBufferBinding(_vertexBuffer, Vertex.Layout, vertexCount);
        var indexBinding = new IndexBufferBinding(_indexBuffer, true, mesh.Indices.Length);
        foreach (var part in mesh.Parts)
        {
            Model.Meshes.Add(new Mesh
            {
                MaterialIndex = part.MaterialIndex,
                Draw = new MeshDraw
                {
                    PrimitiveType = PrimitiveType.TriangleList,
                    StartLocation = part.FirstIndex,
                    DrawCount = part.IndexCount,
                    VertexBuffers = [vertexBinding],
                    IndexBuffer = indexBinding,
                },
            });
        }
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
        for (int i = 0; i < _vertices.Length; i++)
        {
            ref var vertex = ref _vertices[i];
            vertex.Position = Vector3.TransformCoordinate(_positions[i], worldToLocal);
            // Undoing the scale stretches normals the other way than directions, as the inverse transpose would
            vertex.Normal = SafeNormalize(Vector3.Transform(_normals[i], toLocal) * scale, Topology.Mesh.Normals[i]);
            var tangent = Vector3.Transform((Vector3)_tangents[i], toLocal) / scale;
            tangent -= vertex.Normal * Vector3.Dot(vertex.Normal, tangent);
            vertex.Tangent = new Vector4(SafeNormalize(tangent, (Vector3)Topology.Mesh.Tangents[i]), _tangents[i].W);
            BoundingBox.Merge(ref bounds, ref vertex.Position, out bounds);
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
    public void Upload(CommandList commandList) => _vertexBuffer.SetData<Vertex>(commandList, _vertices.AsSpan());

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
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
    }
}
