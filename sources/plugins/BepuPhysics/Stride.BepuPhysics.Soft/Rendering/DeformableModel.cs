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
/// A copy of a soft body's model whose vertices follow its particles: either rewritten every frame on the CPU, or deformed by
/// the vertex shader from the particles uploaded as a buffer. Both modes share the same stream 0 (the authored vertex bytes,
/// snapshotted at construction), so every authored attribute reaches the pixel shader whatever the mode.
/// </summary>
internal sealed class DeformableModel : IDisposable
{
    private static readonly ConditionalWeakTable<Model, Model> SourceOfModel = new();

    // Per source mesh, built once, mutable buffers swapped by SetMode in place.
    private readonly MeshBuffer[] _meshBuffers;
    private readonly Vector3[] _particles;

    // CPU scratch; empty on GPU mode (reallocated by SetMode when switching to CPU)
    private Vector3[] _positions = [];
    private Vector3[] _normals = [];
    private Vector4[] _tangents = [];

    // GPU state; null on CPU mode (recreated by SetMode when switching to GPU)
    private GpuDeformation? _gpu;

    public Model Model { get; }

    public SoftBodyTopology Topology { get; }

    public SoftBodyDeformationMode Mode => _gpu is null ? SoftBodyDeformationMode.Cpu : SoftBodyDeformationMode.Gpu;

    public DeformableModel(GraphicsDevice device, IServiceRegistry services, Model source, SoftBodyTopology topology, SoftBodyDeformationMode mode)
    {
        Topology = topology;
        var meshPool = topology.Mesh;

        _particles = new Vector3[topology.ParticleCount];

        if (source.Meshes.Count != meshPool.Parts.Length)
            throw new InvalidOperationException($"A deformable model expected {source.Meshes.Count} mesh ranges from the topology, got {meshPool.Parts.Length}.");

        Model = new Model { Skeleton = source.Skeleton };
        SourceOfModel.AddOrUpdate(Model, source);
        foreach (var material in source.Materials)
            Model.Materials.Add(material);

        // The authored vertex bytes are snapshotted once per source mesh, into Working, and live there in both modes:
        // CPU mode rewrites position/normal/tangent in Working every frame; GPU mode uploads Working to a static buffer
        // once per SetMode. Keeping Working alive lets SetMode rebuild either mode without re-reading the source mesh.
        _meshBuffers = new MeshBuffer[source.Meshes.Count];
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

            int positionOffset = FindOffset<PositionSemantic>(declaration, PixelFormat.R32G32B32_Float, sourceMesh.Name);
            int normalOffset = FindOffset<NormalSemantic>(declaration, PixelFormat.R32G32B32_Float, sourceMesh.Name);
            int tangentOffset = FindOffset<TangentSemantic>(declaration, PixelFormat.R32G32B32A32_Float, sourceMesh.Name);

            var readable = new VertexBufferHelper(sourceBinding, services, out _);
            var inner = readable.DataInner;
            var working = new byte[inner.Length];
            inner.CopyTo(working);

            _meshBuffers[m] = new MeshBuffer
            {
                Working = working,
                Declaration = declaration,
                Stride = declaration.VertexStride,
                PositionOffset = positionOffset,
                NormalOffset = normalOffset,
                TangentOffset = tangentOffset,
                FirstVertex = part.FirstVertex,
                VertexCount = part.VertexCount,
                VertexBuffer = null!, // Set by SetMode
            };

            Model.Meshes.Add(new Mesh
            {
                MaterialIndex = sourceMesh.MaterialIndex,
                Draw = new MeshDraw
                {
                    PrimitiveType = sourceMesh.Draw.PrimitiveType,
                    StartLocation = sourceMesh.Draw.StartLocation,
                    DrawCount = sourceMesh.Draw.DrawCount,
                    IndexBuffer = sourceMesh.Draw.IndexBuffer,
                    VertexBuffers = [], // Filled by SetMode
                },
            });
        }

        SetMode(device, mode, retired: []);
    }

    /// <summary>
    /// Switches the buffers and draws of the meshes to <paramref name="mode"/>, in place: the model and its meshes stay, so
    /// does whatever follows them (model component, skeleton bodies, node links). The previous mode's GPU buffers go to
    /// <paramref name="retired"/>, to dispose of once the renderer no longer uses them.
    /// </summary>
    public void SetMode(GraphicsDevice device, SoftBodyDeformationMode mode, ICollection<IDisposable> retired)
    {
        bool built = _gpu is not null || (_meshBuffers.Length > 0 && _meshBuffers[0].VertexBuffer is not null);
        if (built && mode == Mode)
            return;

        if (_gpu is not null)
            retired.Add(_gpu);
        foreach (var buffer in _meshBuffers)
        {
            if (buffer.VertexBuffer is { } vb)
                retired.Add(vb);
        }
        _gpu = null;

        bool gpu = mode == SoftBodyDeformationMode.Gpu;
        int vertexCount = Topology.Mesh.Positions.Length;

        if (gpu)
        {
            _gpu = new GpuDeformation(device, Topology);
            _positions = [];
            _normals = [];
            _tangents = [];
        }
        else
        {
            _positions = new Vector3[vertexCount];
            _normals = new Vector3[vertexCount];
            _tangents = new Vector4[vertexCount];
        }

        for (int m = 0; m < _meshBuffers.Length; m++)
        {
            var buffer = _meshBuffers[m];

            // CPU mode rewrites Working every frame, so the buffer is Dynamic and is uploaded each frame from the snapshot.
            // GPU mode uploads the snapshot once at creation and never touches it again, so the buffer is Default (static).
            var vertexBuffer = gpu
                ? Buffer.Vertex.New(device, (ReadOnlySpan<byte>)buffer.Working, GraphicsResourceUsage.Default)
                : Buffer.Vertex.New(device, bufferSize: buffer.Working.Length, usage: GraphicsResourceUsage.Dynamic);
            var vertexBinding = new VertexBufferBinding(vertexBuffer, buffer.Declaration, buffer.VertexCount);
            buffer.VertexBuffer = vertexBuffer;
            buffer.Binding = vertexBinding;

            // GPU mode: stream 1 carries the per-vertex binding to the particles; the authored declaration (stream 0) keeps
            // its slice of the global vertex pool, so the input assembler finds the binding at the same vertex id as the authored data.
            VertexBufferBinding[] vertexBuffers = gpu
                ? [vertexBinding, new VertexBufferBinding(_gpu!.BindingBuffer, _gpu.BindingLayout, buffer.VertexCount, vertexOffset: _gpu.BindingLayout.VertexStride * buffer.FirstVertex)]
                : [vertexBinding];

            var mesh = Model.Meshes[m];
            mesh.Draw.VertexBuffers = vertexBuffers;
            if (gpu)
                _gpu!.Bind(mesh);
            else
                GpuDeformation.Unbind(mesh);
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

        if (_gpu is not null)
        {
            // The vertex shader does the per-vertex deformation; here we only hand over the particles and the bounds.
            var gpuWorldToLocal = Matrix.Invert(world);
            float surfaceOffset = body.Collider is Colliders.ParticleCollider cloth ? cloth.ActualRadius * 0.1f : 0f;
            world.Decompose(out var gpuScale, out Quaternion _, out _);
            float localSurfaceOffset = surfaceOffset * MathF.Max(MathF.Max(1f / MathF.Max(gpuScale.X, 1e-6f), 1f / MathF.Max(gpuScale.Y, 1e-6f)), 1f / MathF.Max(gpuScale.Z, 1e-6f));
            var gpuBounds = _gpu.Update(_particles, gpuWorldToLocal, surfaceOffset, localSurfaceOffset);
            var gpuSphere = BoundingSphere.FromBox(gpuBounds);
            foreach (var mesh in Model.Meshes)
            {
                mesh.BoundingBox = gpuBounds;
                mesh.BoundingSphere = gpuSphere;
            }
            Model.BoundingBox = gpuBounds;
            Model.BoundingSphere = gpuSphere;
            return;
        }

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
        var invScale = new Vector3(1f / scale.X, 1f / scale.Y, 1f / scale.Z);
        var boundsMin = new Vector3(float.MaxValue);
        var boundsMax = new Vector3(float.MinValue);
        var sourceNormals = Topology.Mesh.Normals;
        var sourceTangents = Topology.Mesh.Tangents;

        foreach (var buffer in _meshBuffers)
        {
            // Working keeps the source attributes we never touch, written once at construction; here we only overwrite position, normal and tangent.
            ref byte workingBase = ref MemoryMarshal.GetArrayDataReference(buffer.Working);
            ref Vector3 positionBase = ref MemoryMarshal.GetArrayDataReference(_positions);
            ref Vector3 normalBase = ref MemoryMarshal.GetArrayDataReference(_normals);
            ref Vector4 tangentBase = ref MemoryMarshal.GetArrayDataReference(_tangents);
            ref Vector3 sourceNormalBase = ref MemoryMarshal.GetArrayDataReference(sourceNormals);
            ref Vector4 sourceTangentBase = ref MemoryMarshal.GetArrayDataReference(sourceTangents);
            int stride = buffer.Stride;
            int firstVertex = buffer.FirstVertex;
            int positionOffset = buffer.PositionOffset;
            int normalOffset = buffer.NormalOffset;
            int tangentOffset = buffer.TangentOffset;
            int count = buffer.VertexCount;
            for (int i = 0; i < count; i++)
            {
                int mergedId = firstVertex + i;
                int byteStart = i * stride;

                var position = Vector3.TransformCoordinate(Unsafe.Add(ref positionBase, mergedId), worldToLocal);
                // Undoing the scale stretches normals the other way than directions, as the inverse transpose would
                var normal = SafeNormalize(Vector3.Transform(Unsafe.Add(ref normalBase, mergedId), toLocal) * scale, Unsafe.Add(ref sourceNormalBase, mergedId));
                var rawTangent = Unsafe.Add(ref tangentBase, mergedId);
                var raw = Vector3.Transform((Vector3)rawTangent, toLocal) * invScale;
                raw -= normal * Vector3.Dot(normal, raw);
                var tangent = new Vector4(SafeNormalize(raw, (Vector3)Unsafe.Add(ref sourceTangentBase, mergedId)), rawTangent.W);

                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + positionOffset), position);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + normalOffset), normal);
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + tangentOffset), tangent);

                boundsMin = Vector3.Min(boundsMin, position);
                boundsMax = Vector3.Max(boundsMax, position);
            }
        }

        var bounds = new BoundingBox(boundsMin, boundsMax);

        var sphere = BoundingSphere.FromBox(bounds);
        foreach (var mesh in Model.Meshes)
        {
            mesh.BoundingBox = bounds;
            mesh.BoundingSphere = sphere;
        }
        Model.BoundingBox = bounds;
        Model.BoundingSphere = sphere;
    }

    /// <summary> Sends the vertices or particles computed by <see cref="Deform"/> to the GPU </summary>
    public void Upload(CommandList commandList)
    {
        if (_gpu is not null)
        {
            _gpu.Upload(commandList);
            return;
        }

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
            buffer.VertexBuffer?.Dispose();
        _gpu?.Dispose();
    }

    private sealed class MeshBuffer
    {
        public required byte[] Working;
        public required VertexDeclaration Declaration;
        public required int Stride;
        public required int PositionOffset;
        public required int NormalOffset;
        public required int TangentOffset;
        public required int FirstVertex;
        public required int VertexCount;
        public required Buffer VertexBuffer; // Mutable: swapped by SetMode
        public VertexBufferBinding Binding;  // Mutable: swapped by SetMode
    }
}
