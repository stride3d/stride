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
/// the vertex shader from the particles uploaded as a buffer. Both modes draw the authored vertex layout as stream 0,
/// so every authored attribute reaches the pixel shader whatever the mode.
/// </summary>
internal sealed class DeformableModel : IDisposable
{
    // How far the vertices of a surface are pushed along their normal, as a fraction of the radius of its particles
    private const float SurfaceOffsetFraction = 0.1f;

    // Below this, the scale of an entity on an axis is taken as this to turn a world distance into one of its space
    private const float MinimumScale = 1e-6f;

    private static readonly ConditionalWeakTable<Model, Model> SourceOfModel = new();

    // Per source mesh, built once, mutable buffers swapped by SetMode in place.
    private readonly MeshBuffer[] _meshBuffers;
    private readonly Vector3[] _particles;

    // CPU scratch; empty on GPU mode (reallocated by SetMode when switching to CPU)
    private Vector3[] _positions = [];
    private Vector3[] _normals = [];
    private Vector4[] _tangents = [];
    private Vector3[] _frames = []; // Three per particle, for the smooth deformation of a lattice

    // GPU state; null on CPU mode (recreated by SetMode when switching to GPU)
    private GpuDeformation? _gpu;

    public Model Model { get; }

    public SoftBodyTopology Topology { get; }

    public SoftBodyDeformationMode Mode => _gpu is null ? SoftBodyDeformationMode.Cpu : SoftBodyDeformationMode.Gpu;

    /// <summary> How the vertices follow the particles of a lattice, <see cref="SoftBodyDeformationQuality.Fast"/> for a cloth, which has none </summary>
    public SoftBodyDeformationQuality Quality { get; private set; }

    /// <param name="mode"> <see cref="SoftBodyDeformationMode.Cpu"/> or <see cref="SoftBodyDeformationMode.Gpu"/> </param>
    /// <param name="quality"> <see cref="SoftBodyDeformationQuality.Fast"/> or <see cref="SoftBodyDeformationQuality.Smooth"/> </param>
    public DeformableModel(GraphicsDevice device, IServiceRegistry services, Model source, SoftBodyTopology topology, SoftBodyDeformationMode mode, SoftBodyDeformationQuality quality)
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

        // Working holds the vertices of each source mesh as authored. CPU mode rewrites its positions, normals and tangents
        // every frame; GPU mode writes their rest pose there and uploads it to a static buffer, at each SetMode.
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

            // A mesh may come without normals or tangents, the importer only writes the ones its source file has
            int positionOffset = FindOffset<PositionSemantic>(declaration, sourceMesh.Name, required: true);
            int normalOffset = FindOffset<NormalSemantic>(declaration, sourceMesh.Name, required: false);
            int tangentOffset = FindOffset<TangentSemantic>(declaration, sourceMesh.Name, required: false);

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

        SetMode(device, mode, quality, retired: []);
    }

    /// <summary>
    /// Switches the buffers and draws of the meshes to <paramref name="mode"/>, in place: the model and its meshes stay, so
    /// does whatever follows them (model component, skeleton bodies, node links). The previous mode's GPU buffers go to
    /// <paramref name="retired"/>, to dispose of once the renderer no longer uses them.
    /// </summary>
    public void SetMode(GraphicsDevice device, SoftBodyDeformationMode mode, SoftBodyDeformationQuality quality, ICollection<IDisposable> retired)
    {
        bool built = _gpu is not null || (_meshBuffers.Length > 0 && _meshBuffers[0].VertexBuffer is not null);
        if (built && IsIn(mode, quality))
            return;
        Quality = QualityFor(quality);
        bool smooth = Quality == SoftBodyDeformationQuality.Smooth;

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
            _gpu = new GpuDeformation(device, Topology, smooth);
            _positions = [];
            _normals = [];
            _tangents = [];
            _frames = [];
        }
        else
        {
            _positions = new Vector3[vertexCount];
            _normals = new Vector3[vertexCount];
            _tangents = new Vector4[vertexCount];
            _frames = smooth ? new Vector3[Topology.ParticleCount * 3] : [];
        }

        for (int m = 0; m < _meshBuffers.Length; m++)
        {
            var buffer = _meshBuffers[m];

            // CPU mode uploads Working every frame, to a dynamic buffer. GPU mode uploads it once, with the rest pose
            // the vertex shader deforms, to a static one.
            if (gpu)
                WriteRestPose(buffer);
            var vertexBuffer = gpu
                ? Buffer.Vertex.New(device, (ReadOnlySpan<byte>)buffer.Working, GraphicsResourceUsage.Default)
                : Buffer.Vertex.New(device, bufferSize: buffer.Working.Length, usage: GraphicsResourceUsage.Dynamic);
            var vertexBinding = new VertexBufferBinding(vertexBuffer, buffer.Declaration, buffer.VertexCount);
            buffer.VertexBuffer = vertexBuffer;

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

    /// <summary> Whether the model is already deformed this way, with the quality a body of its kind gets for <paramref name="quality"/> </summary>
    public bool IsIn(SoftBodyDeformationMode mode, SoftBodyDeformationQuality quality) => mode == Mode && QualityFor(quality) == Quality;

    // Only lattices have something to smooth
    private SoftBodyDeformationQuality QualityFor(SoftBodyDeformationQuality quality)
    {
        return quality == SoftBodyDeformationQuality.Smooth && Topology.LatticeNeighbors is not null ? SoftBodyDeformationQuality.Smooth : SoftBodyDeformationQuality.Fast;
    }

    /// <summary>
    /// Writes the position, normal and tangent of the rest pose over the vertices of a mesh, as the topology has them: in the
    /// space of the model, where the authored bytes are in the space of the node of their mesh, and whatever the last CPU
    /// deformation left there.
    /// </summary>
    private void WriteRestPose(MeshBuffer buffer)
    {
        var rest = Topology.Mesh;
        ref byte workingBase = ref MemoryMarshal.GetArrayDataReference(buffer.Working);
        for (int i = 0; i < buffer.VertexCount; i++)
        {
            int mergedId = buffer.FirstVertex + i;
            int byteStart = i * buffer.Stride;
            Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + buffer.PositionOffset), rest.Positions[mergedId]);
            if (buffer.NormalOffset >= 0)
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + buffer.NormalOffset), rest.Normals[mergedId]);
            if (buffer.TangentOffset >= 0)
                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + buffer.TangentOffset), (Vector3)rest.Tangents[mergedId]);
        }
    }

    /// <summary> Where the three floats <see cref="Deform"/> writes for a semantic are in a vertex, -1 when the mesh has none </summary>
    private static int FindOffset<TSemantic>(VertexDeclaration declaration, string meshName, bool required) where TSemantic : ISemantic
    {
        string semanticName = TSemantic.Name;
        foreach (var entry in declaration.EnumerateWithOffsets())
        {
            if (entry.VertexElement.SemanticName != semanticName || entry.VertexElement.SemanticIndex != 0)
                continue;

            // Only x, y and z are written: the fourth float of a wider element, such as the handedness of a tangent, stays as authored
            var format = entry.VertexElement.Format;
            if (format is not (PixelFormat.R32G32B32_Float or PixelFormat.R32G32B32A32_Float))
                throw new NotSupportedException($"Soft body deformation writes {semanticName} as {PixelFormat.R32G32B32_Float} or {PixelFormat.R32G32B32A32_Float}, '{meshName}' has {format}.");
            return entry.Offset;
        }

        if (required)
            throw new NotSupportedException($"Soft body deformation expected {semanticName} in '{meshName}', the vertex declaration has none.");
        return -1;
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
            float surfaceOffset = body.Collider is Colliders.ParticleCollider cloth ? cloth.ActualRadius * SurfaceOffsetFraction : 0f;
            // A mirrored entity has a negative scale on an axis
            world.Decompose(out var gpuScale, out Quaternion _, out _);
            float smallestScale = MathF.Min(MathF.Abs(gpuScale.X), MathF.Min(MathF.Abs(gpuScale.Y), MathF.Abs(gpuScale.Z)));
            float localSurfaceOffset = surfaceOffset / MathF.Max(smallestScale, MinimumScale);
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

        // Positions, normals and tangents are first found in world space
        if (Quality == SoftBodyDeformationQuality.Smooth)
        {
            Topology.ComputeParticleFrames(_particles, _frames);
            for (int v = 0; v < _positions.Length; v++)
                Topology.DeformSmooth(v, _particles, _frames, out _positions[v], out _normals[v], out _tangents[v]);
        }
        else if (Topology.LatticeCoordinates is { } coordinates)
        {
            Topology.Deform(_particles, _positions);
            DeformFrames(coordinates);
        }
        else
        {
            Topology.Deform(_particles, _positions);
            RebuildFrames();

            // Pushes the two faces of a double sided model apart, just enough for them not to fight over the same depth
            if (body.Collider is Colliders.ParticleCollider particle)
            {
                var offset = particle.ActualRadius * SurfaceOffsetFraction;
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

                Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + positionOffset), position);
                if (normalOffset >= 0)
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + normalOffset), normal);
                if (tangentOffset >= 0)
                {
                    var tangent = Vector3.Transform((Vector3)Unsafe.Add(ref tangentBase, mergedId), toLocal) * invScale;
                    tangent -= normal * Vector3.Dot(normal, tangent);
                    tangent = SafeNormalize(tangent, (Vector3)Unsafe.Add(ref sourceTangentBase, mergedId));
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref workingBase, byteStart + tangentOffset), tangent);
                }

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

    /// <summary>
    /// The vertices of a mesh, laid out as its vertex declaration says: as last computed by <see cref="Deform"/> on the CPU,
    /// or the rest pose the vertex shader deforms on the GPU
    /// </summary>
    internal ReadOnlySpan<byte> VertexBytes(int meshIndex) => _meshBuffers[meshIndex].Working;

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
        // Where the three floats written for each semantic are in a vertex, -1 for the ones the mesh does not have
        public required int PositionOffset;
        public required int NormalOffset;
        public required int TangentOffset;
        public required int FirstVertex;
        public required int VertexCount;
        public required Buffer VertexBuffer; // Mutable: swapped by SetMode
    }
}
