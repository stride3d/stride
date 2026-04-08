// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Represents a <see cref="VertexElement"/> with additional metadata, including its offset and size.
/// </summary>
/// <param name="vertexElement"><inheritdoc cref="VertexElement" path="/summary" /></param>
/// <param name="offset"><inheritdoc cref="Offset" path="/summary" /></param>
/// <param name="size"><inheritdoc cref="Size" path="/summary" /></param>
public struct VertexElementWithOffset(VertexElement vertexElement, int offset, int size)
{
    /// <summary>
    ///   The Vertex Element structure that describes a vertex attribute.
    /// </summary>
    public VertexElement VertexElement = vertexElement;

    /// <summary>
    ///   The offset in bytes from the start of the vertex to this element.
    /// </summary>
    public int Offset = offset;

    /// <summary>
    ///   The size in bytes of this element within the vertex structure.
    /// </summary>
    public int Size = size;
}
