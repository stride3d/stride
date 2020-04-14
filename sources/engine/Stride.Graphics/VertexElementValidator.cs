// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    internal class VertexElementValidator
    {
        internal static int GetVertexStride(VertexElement[] elements)
        {
            int num2 = 0;
            for (int i = 0; i < elements.Length; i++)
                num2 += elements[i].Format.SizeInBytes();
            return num2;
        }

        internal static void Validate(int vertexStride, VertexElement[] elements)
        {
            if (vertexStride <= 0)
            {
                throw new ArgumentOutOfRangeException("vertexStride");
            }
            if ((vertexStride & 3) != 0)
            {
                throw new ArgumentException("VertexElementOffsetNotMultipleFour");
            }
            int[] numArray = new int[vertexStride];
            for (int i = 0; i < vertexStride; i++)
            {
                numArray[i] = -1;
            }
            int totalOffset = 0;
            for (int j = 0; j < elements.Length; j++)
            {
                int offset = elements[j].AlignedByteOffset;
                if (offset == VertexElement.AppendAligned)
                {
                    if (j == 0)
                    {
                        offset = 0;
                    }
                    else
                    {
                        offset = totalOffset + elements[j - 1].Format.SizeInBytes();
                    }
                }
                totalOffset = offset;
                int typeSize = elements[j].Format.SizeInBytes();
                if ((offset < 0) || ((offset + typeSize) > vertexStride))
                {
                    throw new ArgumentException("VertexElementOutsideStride");
                }
                if ((offset & 3) != 0)
                {
                    throw new ArgumentException("VertexElementOffsetNotMultipleFour");
                }
                for (int k = 0; k < j; k++)
                {
                    if (elements[j].SemanticName == elements[k].SemanticName && elements[j].SemanticIndex == elements[k].SemanticIndex)
                    {
                        throw new ArgumentException("DuplicateVertexElement");
                    }
                }
                for (int m = offset; m < (offset + typeSize); m++)
                {
                    if (numArray[m] >= 0)
                    {
                        throw new ArgumentException("VertexElementsOverlap");
                    }
                    numArray[m] = j;
                }
            }
        }
    }
}
