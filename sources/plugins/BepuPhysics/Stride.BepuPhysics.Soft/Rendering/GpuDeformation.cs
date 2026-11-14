// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Shaders;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Soft.Rendering;

/// <summary>
/// The GPU side of a <see cref="DeformableModel"/>: per-vertex binding to the particles in a second vertex stream alongside the
/// authored mesh (stream 0), and the particles themselves uploaded every frame for the <c>SoftBodyDeformation</c> shaders.
/// </summary>
/// <remarks>
/// The authored vertex buffer is stream 0, carrying every semantic the mesh declared (position, normal, tangent, texture
/// coordinates, colours, extra UVs, bone indices and weights...); <see cref="DeformableModel"/> only rewrites its position,
/// normal and tangent as the rest pose of the topology, in the space of the model. The binding stream only adds the semantics
/// the deformation shaders read: <c>SOFTBODYCORNERS0/1</c> + <c>SOFTBODYCELL</c> for a volumetric lattice, or <c>SOFTBODYPARTICLE</c>
/// + <c>SOFTBODYRING</c> for a cloth. The vertex shader overwrites <c>Position</c>, <c>meshNormal</c> and <c>meshTangent</c>
/// from the particles; every other authored attribute reaches the pixel shader through the usual semantics.
/// </remarks>
internal sealed class GpuDeformation : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LatticeBinding
    {
        public static readonly VertexDeclaration Layout = new(
            new VertexElement("SOFTBODYCORNERS", 0, PixelFormat.R32G32B32A32_UInt),
            new VertexElement("SOFTBODYCORNERS", 1, PixelFormat.R32G32B32A32_UInt),
            new VertexElement("SOFTBODYCELL", PixelFormat.R32G32B32_Float));

        public UInt4 Corners0;
        public UInt4 Corners1;
        public Vector3 Cell;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SurfaceBinding
    {
        public static readonly VertexDeclaration Layout = new(
            new VertexElement("SOFTBODYPARTICLE", PixelFormat.R32_UInt),
            new VertexElement("SOFTBODYRING", PixelFormat.R32G32_UInt));

        public uint Particle;
        public uint RingStart;
        public uint RingCount;
    }

    /// <summary> A triangle around a vertex, as <c>SoftBodySurfaceDeformation.RingTriangle</c> reads it </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct RingTriangle(uint Next, uint Previous, float NextWeight, float PreviousWeight);

    private static readonly ShaderClassSource LatticeShader = new("SoftBodyLatticeDeformation");
    private static readonly ShaderClassSource SmoothLatticeShader = new("SoftBodySmoothLatticeDeformation");
    private static readonly ShaderClassSource SurfaceShader = new("SoftBodySurfaceDeformation");

    private readonly Buffer _particleBuffer;
    private readonly Buffer? _ringBuffer;
    private readonly Buffer? _frameBuffer; // Three vectors per particle, for the smooth deformation of a lattice
    private readonly SoftBodyTopology _topology;
    private readonly Vector4[] _frames = []; // As the shader reads them
    private readonly Vector4[] _particles;
    private readonly Vector3[] _localParticles = []; // For the bounds of a lattice whose vertices reach past its particles
    private readonly float _latticeReach; // How far past its particles, in times the longest step between two of them

    /// <summary> Stream 1 buffer: per-vertex binding to the particles. Alongside the authored vertex buffer (stream 0) in each mesh's VertexBuffers. </summary>
    public Buffer BindingBuffer { get; }

    /// <summary> Vertex declaration of the binding stream (stream 1) </summary>
    public VertexDeclaration BindingLayout { get; }

    /// <param name="smooth"> For a lattice, whether its vertices blend the frames of the particles rather than the particles alone </param>
    public GpuDeformation(GraphicsDevice device, SoftBodyTopology topology, bool smooth)
    {
        _topology = topology;
        _particles = new Vector4[topology.ParticleCount];
        if (smooth && topology.LatticeNeighbors is not null)
        {
            _frames = new Vector4[topology.ParticleCount * 3];
            _frameBuffer = Buffer.New<Vector4>(device, _frames.Length, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);
        }
        _particleBuffer = Buffer.New<Vector4>(device, topology.ParticleCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);

        var mesh = topology.Mesh;
        if (topology.LatticeCoordinates is { } coordinates)
        {
            var bindings = new LatticeBinding[mesh.Positions.Length];
            var raw = MemoryMarshal.Cast<int, uint>(topology.BindingParticles.AsSpan());
            for (int v = 0; v < bindings.Length; v++)
            {
                var corners = raw.Slice(v * 8, 8);
                var cell = coordinates[v];
                bindings[v] = new LatticeBinding
                {
                    Corners0 = new UInt4(corners[0], corners[1], corners[2], corners[3]),
                    Corners1 = new UInt4(corners[4], corners[5], corners[6], corners[7]),
                    Cell = cell,
                };
                _latticeReach = MathF.Max(_latticeReach, _frameBuffer is not null ? SmoothReach(cell) : InterpolationReach(cell));
            }

            if (_latticeReach > 0f)
                _localParticles = new Vector3[topology.ParticleCount];
            BindingLayout = LatticeBinding.Layout;
            BindingBuffer = Buffer.Vertex.New(device, MemoryMarshal.AsBytes(bindings.AsSpan()));
        }
        else
        {
            var (ring, ringStarts) = BuildRings(topology);
            var bindings = new SurfaceBinding[mesh.Positions.Length];
            for (int v = 0; v < bindings.Length; v++)
            {
                bindings[v] = new SurfaceBinding
                {
                    Particle = (uint)topology.BindingParticles[v],
                    RingStart = (uint)ringStarts[v],
                    RingCount = (uint)(ringStarts[v + 1] - ringStarts[v]),
                };
            }

            BindingLayout = SurfaceBinding.Layout;
            BindingBuffer = Buffer.Vertex.New(device, MemoryMarshal.AsBytes(bindings.AsSpan()));
            // A structured buffer cannot be empty
            _ringBuffer = Buffer.Structured.New(device, MemoryMarshal.AsBytes(ring.Length > 0 ? ring.AsSpan() : [default]), Marshal.SizeOf<RingTriangle>());
        }
    }

    /// <summary> Makes <paramref name="mesh"/> deformed by the vertex shader from the buffers of this deformation </summary>
    public void Bind(Mesh mesh)
    {
        mesh.Parameters.Set(StrideEffectBaseKeys.ExtensionPostVertexStageShader, _ringBuffer is not null ? SurfaceShader : _frameBuffer is not null ? SmoothLatticeShader : LatticeShader);
        mesh.Parameters.Set(SoftBodyDeformationKeys.Particles, _particleBuffer);
        mesh.Parameters.Set(SoftBodySmoothLatticeDeformationKeys.Frames, _frameBuffer);
        if (_ringBuffer is not null)
            mesh.Parameters.Set(SoftBodySurfaceDeformationKeys.Ring, _ringBuffer);
    }

    /// <summary> Makes <paramref name="mesh"/> drawn from its vertex buffer again </summary>
    public static void Unbind(Mesh mesh)
    {
        // Setting rather than removing, so the permutation counter of the parameters moves
        mesh.Parameters.Set(StrideEffectBaseKeys.ExtensionPostVertexStageShader, null);
        mesh.Parameters.Set(SoftBodyDeformationKeys.Particles, null);
        mesh.Parameters.Set(SoftBodySurfaceDeformationKeys.Ring, null);
        mesh.Parameters.Set(SoftBodySmoothLatticeDeformationKeys.Frames, null);
    }

    /// <summary> Takes the particles for the next upload and returns their bounds in the space of <paramref name="worldToLocal"/>, grown to contain the vertices </summary>
    /// <param name="surfaceOffset"> How far along their normal the vertices of a surface are pushed, in world space </param>
    /// <param name="localSurfaceOffset"> The same distance in the space of <paramref name="worldToLocal"/>, at most </param>
    public BoundingBox Update(ReadOnlySpan<Vector3> particles, in Matrix worldToLocal, float surfaceOffset, float localSurfaceOffset)
    {
        var bounds = new BoundingBox(new Vector3(float.MaxValue), new Vector3(float.MinValue));
        for (int i = 0; i < particles.Length; i++)
        {
            _particles[i] = new Vector4(particles[i], surfaceOffset);
            var local = Vector3.TransformCoordinate(particles[i], worldToLocal);
            BoundingBox.Merge(ref bounds, ref local, out bounds);
            if (_localParticles.Length > 0)
                _localParticles[i] = local;
        }

        if (_frameBuffer is not null)
            _topology.ComputeParticleFrames(particles, _frames);

        var margin = _latticeReach * LongestStep() + MathF.Abs(localSurfaceOffset);
        return new BoundingBox(bounds.Minimum - new Vector3(margin), bounds.Maximum + new Vector3(margin));
    }

    /// <summary> The longest distance between two neighbors of the lattice as it is now, in the space of the bounds </summary>
    private float LongestStep()
    {
        if (_localParticles.Length == 0)
            return 0f;

        var neighbors = _topology.LatticeNeighbors!;
        float longest = 0f;
        for (int particle = 0; particle < _localParticles.Length; particle++)
        {
            // The neighbor after it on each axis: every pair is seen once
            for (int after = 1; after < 6; after += 2)
            {
                int neighbor = neighbors[particle * 6 + after];
                if (neighbor >= 0)
                    longest = MathF.Max(longest, Vector3.DistanceSquared(_localParticles[particle], _localParticles[neighbor]));
            }
        }
        return MathF.Sqrt(longest);
    }

    /// <summary>
    /// How far past the particles of its cell the interpolation puts a vertex at <paramref name="cell"/>, at most,
    /// in times the longest edge of the cell: nothing inside the cell, where it stays between its corners.
    /// </summary>
    internal static float InterpolationReach(Vector3 cell)
    {
        // Beyond the cell on an axis, the vertex goes on along the edges of that axis, themselves extrapolated on the axes not walked yet
        var outside = Vector3.Max(Vector3.Zero, Vector3.Max(-cell, cell - Vector3.One));
        return outside.X * (1f + 2f * outside.Y) * (1f + 2f * outside.Z) + outside.Y * (1f + 2f * outside.Z) + outside.Z;
    }

    /// <summary>
    /// How far past the particles of its cell the smooth deformation puts a vertex at <paramref name="cell"/>, at most,
    /// in times the longest step between two particles: each corner moves it by its frame, weighted as the corner is.
    /// </summary>
    internal static float SmoothReach(Vector3 cell)
    {
        return AlongAxis(cell.X) + AlongAxis(cell.Y) + AlongAxis(cell.Z);

        static float AlongAxis(float t)
        {
            float c = Math.Clamp(t, 0f, 1f);
            float weight = c * c * (3f - 2f * c);
            return (1f - weight) * MathF.Abs(t) + weight * MathF.Abs(t - 1f);
        }
    }

    /// <summary> Sends the particles taken by <see cref="Update"/> to the GPU </summary>
    public void Upload(CommandList commandList)
    {
        _particleBuffer.SetData<Vector4>(commandList, _particles.AsSpan());
        _frameBuffer?.SetData<Vector4>(commandList, _frames.AsSpan());
    }

    /// <summary>
    /// For each vertex of a surface, the triangles around it as the CPU deformation sums them: normals from the cross product of the edges,
    /// pointing the way the mesh's winding does, and tangents from the texture coordinates of the triangle.
    /// </summary>
    /// <returns> The triangles of all vertices one after the other, and where those of each vertex start, plus the total at the end </returns>
    internal static (RingTriangle[] Ring, int[] Starts) BuildRings(SoftBodyTopology topology)
    {
        var mesh = topology.Mesh;
        var indices = mesh.Indices;
        var particleOfVertex = topology.BindingParticles;
        var starts = new int[mesh.Positions.Length + 1];
        foreach (var part in mesh.Parts)
        {
            for (int i = part.FirstIndex; i + 2 < part.FirstIndex + part.IndexCount; i += 3)
            {
                starts[indices[i] + 1]++;
                starts[indices[i + 1] + 1]++;
                starts[indices[i + 2] + 1]++;
            }
        }
        for (int v = 0; v < mesh.Positions.Length; v++)
            starts[v + 1] += starts[v];

        var ring = new RingTriangle[starts[^1]];
        var filled = starts[..^1];
        Span<int> corners = stackalloc int[3];
        Span<float> weights = stackalloc float[3];
        foreach (var part in mesh.Parts)
        {
            for (int i = part.FirstIndex; i + 2 < part.FirstIndex + part.IndexCount; i += 3)
            {
                int a = indices[i], b = indices[i + 1], c = indices[i + 2];

                // The tangent of the triangle is a weighted sum of its edges from a; the weights sum to zero, so it is the same sum from any corner
                var uv1 = mesh.TexCoords[b] - mesh.TexCoords[a];
                var uv2 = mesh.TexCoords[c] - mesh.TexCoords[a];
                var determinant = uv1.X * uv2.Y - uv2.X * uv1.Y;
                var (weightB, weightC) = MathF.Abs(determinant) > 1e-12f ? (uv2.Y / determinant, -uv1.Y / determinant) : (1f, 0f);
                weights[0] = -(weightB + weightC);
                weights[1] = weightB;
                weights[2] = weightC;

                // Clockwise triangles face the other way than the cross product of their edges
                corners[0] = a;
                corners[1] = mesh.Clockwise ? c : b;
                corners[2] = mesh.Clockwise ? b : c;
                if (mesh.Clockwise)
                    (weights[1], weights[2]) = (weights[2], weights[1]);

                for (int k = 0; k < 3; k++)
                {
                    int next = (k + 1) % 3, previous = (k + 2) % 3;
                    ring[filled[corners[k]]++] = new RingTriangle(
                        (uint)particleOfVertex[corners[next]], (uint)particleOfVertex[corners[previous]], weights[next], weights[previous]);
                }
            }
        }

        return (ring, starts);
    }

    public void Dispose()
    {
        BindingBuffer.Dispose();
        _particleBuffer.Dispose();
        _ringBuffer?.Dispose();
        _frameBuffer?.Dispose();
    }
}
