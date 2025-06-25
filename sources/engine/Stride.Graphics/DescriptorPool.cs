// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   A pool where the application can allocate Descriptors that are grouped together in <see cref="DescriptorSet"/>s.
/// </summary>
public partial class DescriptorPool : GraphicsResourceBase
{
    /// <summary>
    ///   Creates a new Descriptor Pool with space for the specified counts of each Descriptor type.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device.</param>
    /// <param name="counts">
    ///   A list of <see cref="DescriptorTypeCount"/> structures indicating the number of each type of Descriptor
    ///   that need to be allocated.
    /// </param>
    /// <returns>The new Descriptor Pool.</returns>
    public static DescriptorPool New(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts)
    {
        return new DescriptorPool(graphicsDevice, counts);
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)

    /// <summary>
    ///   The Descriptors allocated in this Descriptor Pool, along with their offset and size.
    /// </summary>
    internal DescriptorSetEntry[] Entries;

    // Current allocation offset in the Entries array
    private int descriptorAllocationOffset;


    private DescriptorPool(GraphicsDevice graphicsDevice, DescriptorTypeCount[] counts)
    {
        // For now, we put everything together so let's compute total count
        var totalCount = 0;
        foreach (var count in counts)
        {
            totalCount += count.Count;
        }

        Entries = new DescriptorSetEntry[totalCount];
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        Entries = null;
        base.Destroy();
    }

    /// <summary>
    ///   Clears the Descriptor Pool, resetting all allocated Descriptors.
    /// </summary>
    public void Reset()
    {
        Array.Clear(Entries, 0, descriptorAllocationOffset);
        descriptorAllocationOffset = 0;
    }

    /// <summary>
    ///   Tries to allocate space for a number of Descriptor in the Descriptor Pool.
    /// </summary>
    /// <param name="size">The number of Descriptors to allocate.</param>
    /// <returns>
    ///   The offset in the <see cref="Entries"/> array where the allocated Descriptors start,
    ///   or <c>-1</c> if there is not enough space in the Descriptor Pool to allocate the requested number of Descriptors.
    /// </returns>
    internal int Allocate(int size)
    {
        if (descriptorAllocationOffset + size > Entries.Length)
            return -1;

        var result = descriptorAllocationOffset;
        descriptorAllocationOffset += size;
        return result;
    }

#endif
}
