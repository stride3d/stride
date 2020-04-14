// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Graphics
{
    public struct VertexElementWithOffset
    {
        public VertexElement VertexElement;
        public int Offset;
        public int Size;

        public VertexElementWithOffset(VertexElement vertexElement, int offset, int size)
        {
            VertexElement = vertexElement;
            Offset = offset;
            Size = size;
        }
    }
}
