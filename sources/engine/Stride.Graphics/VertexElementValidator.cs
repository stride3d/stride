// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Provides methods for validating and calculating properties of Vertex Elements used to define
///   the layout of Vertex Declarations.
/// </summary>
/// <seealso cref="VertexElement"/>
/// <seealso cref="VertexDeclaration"/>
internal static class VertexElementValidator
{
    /// <summary>
    ///   Calculates the total stride, in bytes, of a vertex based on its elements.
    /// </summary>
    /// <param name="elements">
    ///   An array of <see cref="VertexElement"/>s representing the components of the vertex. Each element
    ///   defines a format and size.
    /// </param>
    /// <returns>The total stride, in bytes, of the vertex, calculated as the sum of the sizes of all elements.</returns>
    internal static int GetVertexStride(VertexElement[] elements)
    {
        int stride = 0;

        for (int i = 0; i < elements.Length; i++)
            stride += elements[i].Format.SizeInBytes;

        return stride;
    }

    /// <summary>
    ///   Validates the configuration of vertex elements that form a Vertex Declaration.
    /// </summary>
    /// <param name="vertexStride">The size, in bytes, of a single vertex. Must be a positive value and a multiple of 4.</param>
    /// <param name="elements">
    ///   An array of <see cref="VertexElement"/> objects representing the layout of vertex attributes. Cannot contain
    ///   overlapping elements or duplicate semantic names and indices.
    /// </param>
    /// <remarks>
    ///   This method ensures that the Vertex Declaration adheres to alignment and size constraints
    ///   required for proper rendering. It checks for overlapping elements, duplicate semantic names and indices,
    ///   and ensures that all offsets are aligned to 4-byte boundaries.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException">
    ///   <paramref name="vertexStride"/> is not a positive number greater than zero.
    /// </exception>
    /// <exception cref="ArgumentException">
    ///   Thrown if:
    ///   <list type="bullet">
    ///     <item><paramref name="vertexStride"/> is not a multiple of 4 bytes.</item>
    ///     <item>Any Vertex Element extends beyond the bounds of the vertex stride.</item>
    ///     <item>Any Vertex Element has an offset that is not a multiple of 4 bytes.</item>
    ///     <item>Duplicate Vertex Elements are found with the same semantic name and index.</item>
    ///     <item>Any Vertex Element overlaps with another element.</item>
    ///   </list>
    /// </exception>
    internal static void Validate(int vertexStride, VertexElement[] elements)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(vertexStride);

        if ((vertexStride & 3) != 0)
            throw new ArgumentException($"The vertex stride is not a multiple of 4 bytes", nameof(vertexStride));

        // For checking overlaps, we use an array to track occupied bytes
        var overlapCheckArray = new int[vertexStride];
        for (int i = 0; i < overlapCheckArray.Length; i++)
            overlapCheckArray[i] = -1;

        int totalOffset = 0;
        for (int elementIndex = 0; elementIndex < elements.Length; elementIndex++)
        {
            // Compute the offset for this element
            int elementOffset = elements[elementIndex].AlignedByteOffset;
            if (elementOffset == VertexElement.AppendAligned)
            {
                elementOffset = elementIndex == 0 ? 0 : totalOffset + elements[elementIndex - 1].Format.SizeInBytes;
            }
            totalOffset = elementOffset;

            // Validate the element offset
            int typeSize = elements[elementIndex].Format.SizeInBytes;
            if (elementOffset == VertexElement.AppendAligned || (elementOffset + typeSize) > vertexStride)
            {
                throw new ArgumentException($"The {nameof(VertexElement)}'s offset and size makes it extend beyond the vertex stride", nameof(elements));
            }
            if ((elementOffset & 3) != 0)
            {
                throw new ArgumentException($"The offset of the {nameof(VertexElement)} is not a multiple of 4 bytes", nameof(elements));
            }

            // Check for duplicate semantic names and indices
            for (int i = 0; i < elementIndex; i++)
            {
                if (elements[elementIndex].SemanticName.Equals(elements[i].SemanticName) &&
                    elements[elementIndex].SemanticIndex == elements[i].SemanticIndex)
                {
                    throw new ArgumentException($"Duplicate {nameof(VertexElement)}s found with the same semantic name and index", nameof(elements));
                }
            }

            // Check for overlap with existing elements
            for (int i = elementOffset; i < (elementOffset + typeSize); i++)
            {
                // Check for overlap with existing elements
                if (overlapCheckArray[i] >= 0)
                {
                    throw new ArgumentException($"A {nameof(VertexElement)} overlaps with another element. Check the element's offset", nameof(elements));
                }
                // Mark this byte as occupied by this element
                overlapCheckArray[i] = elementIndex;
            }
        }
    }
}
