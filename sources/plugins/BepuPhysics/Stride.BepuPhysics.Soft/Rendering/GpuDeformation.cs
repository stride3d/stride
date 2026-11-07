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
/// The GPU side of a <see cref="DeformableModel"/>: the mesh at rest with how each vertex is bound to the particles, uploaded once,
/// and the particles, uploaded every frame for the <c>SoftBodyDeformation</c> shaders.
/// </summary>
internal sealed class GpuDeformation : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct LatticeVertex
    {
        public static readonly VertexDeclaration Layout = new(
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Vector3>(),
            VertexElement.Tangent<Vector4>(),
            VertexElement.TextureCoordinate<Vector2>(),
            new VertexElement("SOFTBODYCORNERS", 0, PixelFormat.R32G32B32A32_UInt),
            new VertexElement("SOFTBODYCORNERS", 1, PixelFormat.R32G32B32A32_UInt),
            new VertexElement("SOFTBODYCELL", PixelFormat.R32G32B32_Float));

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
        public Vector2 TexCoord;
        public UInt4 Corners0;
        public UInt4 Corners1;
        public Vector3 Cell;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SurfaceVertex
    {
        public static readonly VertexDeclaration Layout = new(
            VertexElement.Position<Vector3>(),
            VertexElement.Normal<Vector3>(),
            VertexElement.Tangent<Vector4>(),
            VertexElement.TextureCoordinate<Vector2>(),
            new VertexElement("SOFTBODYPARTICLE", PixelFormat.R32_UInt),
            new VertexElement("SOFTBODYRING", PixelFormat.R32G32_UInt));

        public Vector3 Position;
        public Vector3 Normal;
        public Vector4 Tangent;
        public Vector2 TexCoord;
        public uint Particle;
        public uint RingStart;
        public uint RingCount;
    }

    /// <summary> A triangle around a vertex, as <c>SoftBodySurfaceDeformation.RingTriangle</c> reads it </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal readonly record struct RingTriangle(uint Next, uint Previous, float NextWeight, float PreviousWeight);

    private static readonly ShaderClassSource LatticeShader = new("SoftBodyLatticeDeformation");
    private static readonly ShaderClassSource SurfaceShader = new("SoftBodySurfaceDeformation");

    private readonly Buffer _particleBuffer;
    private readonly Buffer? _ringBuffer;
    private readonly Vector4[] _particles;
    private readonly float _latticeMargin;

    public Buffer VertexBuffer { get; }

    public VertexDeclaration Layout { get; }

    public GpuDeformation(GraphicsDevice device, SoftBodyTopology topology)
    {
        _particles = new Vector4[topology.ParticleCount];
        _particleBuffer = Buffer.New<Vector4>(device, topology.ParticleCount, BufferFlags.ShaderResource | BufferFlags.StructuredBuffer, GraphicsResourceUsage.Dynamic);

        var mesh = topology.Mesh;
        if (topology.LatticeCoordinates is { } coordinates)
        {
            var vertices = new LatticeVertex[mesh.Positions.Length];
            var bindings = MemoryMarshal.Cast<int, uint>(topology.BindingParticles.AsSpan());
            float extrapolation = 0f;
            for (int v = 0; v < vertices.Length; v++)
            {
                var corners = bindings.Slice(v * 8, 8);
                var cell = coordinates[v];
                vertices[v] = new LatticeVertex
                {
                    Position = mesh.Positions[v],
                    Normal = mesh.Normals[v],
                    Tangent = mesh.Tangents[v],
                    TexCoord = mesh.TexCoords[v],
                    Corners0 = new UInt4(corners[0], corners[1], corners[2], corners[3]),
                    Corners1 = new UInt4(corners[4], corners[5], corners[6], corners[7]),
                    Cell = cell,
                };
                var outside = Vector3.Max(-cell, cell - Vector3.One);
                extrapolation = MathF.Max(extrapolation, MathF.Max(outside.X, MathF.Max(outside.Y, outside.Z)));
            }

            // A vertex inside its cell stays within the bounds of the particles; one beyond the lattice by a fraction of a cell, given cells stretched up to twice their size
            _latticeMargin = extrapolation * topology.Spacing * 2f;
            Layout = LatticeVertex.Layout;
            VertexBuffer = Buffer.Vertex.New(device, MemoryMarshal.AsBytes(vertices.AsSpan()));
        }
        else
        {
            var (ring, ringStarts) = BuildRings(topology);
            var vertices = new SurfaceVertex[mesh.Positions.Length];
            for (int v = 0; v < vertices.Length; v++)
            {
                vertices[v] = new SurfaceVertex
                {
                    Position = mesh.Positions[v],
                    Normal = mesh.Normals[v],
                    Tangent = mesh.Tangents[v],
                    TexCoord = mesh.TexCoords[v],
                    Particle = (uint)topology.BindingParticles[v],
                    RingStart = (uint)ringStarts[v],
                    RingCount = (uint)(ringStarts[v + 1] - ringStarts[v]),
                };
            }

            Layout = SurfaceVertex.Layout;
            VertexBuffer = Buffer.Vertex.New(device, MemoryMarshal.AsBytes(vertices.AsSpan()));
            // A structured buffer cannot be empty
            _ringBuffer = Buffer.Structured.New(device, MemoryMarshal.AsBytes(ring.Length > 0 ? ring.AsSpan() : [default]), Marshal.SizeOf<RingTriangle>());
        }
    }

    /// <summary> Makes <paramref name="mesh"/> deformed by the vertex shader from the buffers of this deformation </summary>
    public void Bind(Mesh mesh)
    {
        mesh.Parameters.Set(StrideEffectBaseKeys.ExtensionPostVertexStageShader, _ringBuffer is null ? LatticeShader : SurfaceShader);
        mesh.Parameters.Set(SoftBodyDeformationKeys.Particles, _particleBuffer);
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
        }

        var margin = _latticeMargin + MathF.Abs(localSurfaceOffset);
        return new BoundingBox(bounds.Minimum - new Vector3(margin), bounds.Maximum + new Vector3(margin));
    }

    /// <summary> Sends the particles taken by <see cref="Update"/> to the GPU </summary>
    public void Upload(CommandList commandList) => _particleBuffer.SetData<Vector4>(commandList, _particles.AsSpan());

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
        VertexBuffer.Dispose();
        _particleBuffer.Dispose();
        _ringBuffer?.Dispose();
    }
}
