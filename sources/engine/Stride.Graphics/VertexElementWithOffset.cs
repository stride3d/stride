// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public struct VertexElementWithOffset(VertexElement vertexElement, int offset, int size)
{
    public VertexElement VertexElement = vertexElement;

    public int Offset = offset;

    public int Size = size;
}
