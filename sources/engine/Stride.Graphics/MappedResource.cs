// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   A GPU resource mapped for CPU access. This is returned by using <see cref="CommandList.MapSubResource"/>.
/// </summary>
public readonly partial struct MappedResource
{
    /// <summary>
    ///   Initializes a new instance of the <see cref="MappedResource"/> struct.
    /// </summary>
    /// <param name="resource">The Graphics Resource mapped for CPU access.</param>
    /// <param name="subResourceIndex">Index of the mapped sub-resource.</param>
    /// <param name="dataBox">The data box specifying how the data is laid out in memory.</param>
    internal MappedResource(GraphicsResource resource, int subResourceIndex, DataBox dataBox) : this()
    {
        Resource = resource;
        SubResourceIndex = subResourceIndex;
        DataBox = dataBox;
        OffsetInBytes = 0;
        SizeInBytes = -1;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="MappedResource"/> struct.
    /// </summary>
    /// <param name="resource">The Graphics Resource mapped for CPU access.</param>
    /// <param name="subResourceIndex">Index of the mapped sub-resource.</param>
    /// <param name="dataBox">The data box specifying how the data is laid out in memory.</param>
    /// <param name="offsetInBytes">The offset since the beginning of the buffer, in bytes.</param>
    /// <param name="sizeInBytes">The size of the mapped resource, in bytes.</param>
    internal MappedResource(GraphicsResource resource, int subResourceIndex, DataBox dataBox, int offsetInBytes, int sizeInBytes) : this()
    {
        Resource = resource;
        SubResourceIndex = subResourceIndex;
        DataBox = dataBox;
        OffsetInBytes = offsetInBytes;
        SizeInBytes = sizeInBytes;
    }


    /// <summary>
    ///   The resource mapped for CPU access.
    /// </summary>
    public readonly GraphicsResource Resource;

    /// <summary>
    ///   The sub-resource index.
    /// </summary>
    public readonly int SubResourceIndex;

    /// <summary>
    ///   The data box specifying how the data is laid out in memory.
    /// </summary>
    public readonly DataBox DataBox;

    // TODO: These two fields are not used for now. Client code uses DataBox directly. Remove them?

    /// <summary>
    ///   The offset of the mapped resource since the beginning of the buffer, in bytes.
    /// </summary>
    public readonly int OffsetInBytes;

    /// <summary>
    ///   The size of the mapped resource, in bytes.
    /// </summary>
    public readonly int SizeInBytes;
}
