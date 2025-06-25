// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public readonly partial struct MappedResource
{
    /// <summary>
    /// A GPU resource mapped for CPU access. This is returned by using <see cref="CommandList.MapSubresource"/>
    /// </summary>
    internal MappedResource(GraphicsResource resource, int subResourceIndex, DataBox dataBox) : this()
    {
        Resource = resource;
        SubResourceIndex = subResourceIndex;
        DataBox = dataBox;
        OffsetInBytes = 0;
        SizeInBytes = -1;
    }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappedResource"/> struct.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="subResourceIndex">Index of the sub resource.</param>
        /// <param name="dataBox">The data box.</param>
        /// <param name="offsetInBytes">Offset since the beginning of the buffer.</param>
        /// <param name="sizeInBytes">Size of the mapped resource.</param>
        /// <summary>
        /// The resource mapped.
        /// </summary>
        /// <summary>
        /// The subresource index.
        /// </summary>
        /// <summary>
        /// The data box
        /// </summary>
        /// <summary>
        /// the offset of the mapped resource since the beginning of the buffer
        /// </summary>
        /// <summary>
        /// the size of the mapped resource
        /// </summary>
    internal MappedResource(GraphicsResource resource, int subResourceIndex, DataBox dataBox, int offsetInBytes, int sizeInBytes) : this()
    {
        Resource = resource;
        SubResourceIndex = subResourceIndex;
        DataBox = dataBox;
        OffsetInBytes = offsetInBytes;
        SizeInBytes = sizeInBytes;
    }


    public readonly GraphicsResource Resource;

    public readonly int SubResourceIndex;

    public readonly DataBox DataBox;

    public readonly int OffsetInBytes;

    public readonly int SizeInBytes;
}
