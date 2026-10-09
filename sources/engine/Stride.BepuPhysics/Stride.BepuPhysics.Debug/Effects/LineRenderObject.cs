// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.BepuPhysics.Debug.Effects;

/// <summary>
/// A batch of world-space line segments rebuilt every frame and drawn in a single call.
/// </summary>
public sealed class LineRenderObject : RenderObject, IDisposable
{
    private LineVertex[] _vertices = new LineVertex[256];
    private Buffer? _vertexBuffer;
    private bool _uploaded;

    /// <summary>
    /// The number of vertices currently stored, two per segment.
    /// </summary>
    public int VertexCount { get; private set; }

    /// <summary>
    /// Removes all segments.
    /// </summary>
    public void Clear()
    {
        VertexCount = 0;
        _uploaded = false;
    }

    /// <summary>
    /// Adds a segment from <paramref name="start"/> to <paramref name="end"/>.
    /// </summary>
    /// <param name="start">The world-space start of the segment.</param>
    /// <param name="end">The world-space end of the segment.</param>
    /// <param name="color">The color of the segment.</param>
    public void AddLine(Vector3 start, Vector3 end, Color color)
    {
        if (VertexCount + 2 > _vertices.Length)
            Array.Resize(ref _vertices, _vertices.Length * 2);

        _vertices[VertexCount++] = new LineVertex(start, color);
        _vertices[VertexCount++] = new LineVertex(end, color);
        _uploaded = false;
    }

    internal Buffer Upload(CommandList commandList)
    {
        // Render features draw once per stage, the segments only change once per frame
        if (_uploaded && _vertexBuffer is not null)
            return _vertexBuffer;

        if (_vertexBuffer is null || _vertexBuffer.SizeInBytes < VertexCount * LineVertex.Size)
        {
            _vertexBuffer?.Dispose();
            _vertexBuffer = Buffer.Vertex.New(commandList.GraphicsDevice, bufferSize: _vertices.Length * LineVertex.Size, GraphicsResourceUsage.Dynamic);
        }

        _vertexBuffer.SetData(commandList, (ReadOnlySpan<LineVertex>)_vertices.AsSpan(0, VertexCount));
        _uploaded = true;
        return _vertexBuffer;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _vertexBuffer = null;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal readonly struct LineVertex(Vector3 position, Color color)
    {
        public static readonly VertexDeclaration Layout = new(VertexElement.Position<Vector3>(), VertexElement.Color<Color>());
        public static readonly int Size = Layout.VertexStride;

        public readonly Vector3 Position = position;
        public readonly Color Color = color;
    }
}
