// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

// D3D11 version

/// <summary>
///   Defines the Graphics Resources (Descriptors) that need to be bound together, their layout, types, and other associated metadata.
///   <br/>
///   This description is used to allocate a <see cref="DescriptorSet"/>.
/// </summary>
public partial class DescriptorSetLayout : GraphicsResourceBase
{
    /// <summary>
    ///   Creates a new Descriptor Set Layout.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    /// <param name="builder">
    ///   A <see cref="DescriptorSetLayoutBuilder"/> that defines the bound Graphics Resources of the Descriptor Set.
    /// </param>
    /// <returns>The new <see cref="DescriptorSetLayout"/>.</returns>
    public static DescriptorSetLayout New(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
    {
        return new DescriptorSetLayout(device, builder);
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)

    /// <summary>
    ///   The number of elements in the Descriptor Set Layout.
    /// </summary>
    /// <remarks>
    ///   This is not just the number of <see cref="Entries"/>, but the total number of elements across all entries,
    ///   as some of the entries can be Arrays with several elements.
    /// </remarks>
    internal readonly int ElementCount;

    /// <summary>
    ///   The entries that define the bindings and layout of the Descriptor Set.
    /// </summary>
    internal readonly DescriptorSetLayoutBuilder.Entry[] Entries;


    private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
    {
        ElementCount = builder.ElementCount;
        Entries = builder.Entries.ToArray();
    }
#endif
}
