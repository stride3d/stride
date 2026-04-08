// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Represents a set of Descriptors (such as Textures or Buffers) that can be bound together to a graphics pipeline.
/// </summary>
public readonly partial struct DescriptorSet
{
    /// <summary>
    ///   Creates a new Descriptor Set.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device.</param>
    /// <param name="pool">The pool where Descriptor Sets are allocated.</param>
    /// <param name="layout">A description of the Graphics Resources in the Descriptor Set and their layout.</param>
    /// <returns>
    ///   The new Descriptor Set.
    /// </returns>
    public static DescriptorSet New(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout layout)
    {
        return new DescriptorSet(graphicsDevice, pool, layout);
    }

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)

    /// <summary>
    ///   An array of Descriptors in the Descriptor Set used for managing the Graphics Resources,
    ///   as allocated by the Descriptor Pool.
    /// </summary>
    internal readonly DescriptorSetEntry[] HeapObjects;

    /// <summary>
    ///   The start offset in the <see cref="HeapObjects"/> array where the Descriptors for this Descriptor Set begin.
    /// </summary>
    internal readonly int DescriptorStartOffset;


    private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
    {
        HeapObjects = pool.Entries;
        DescriptorStartOffset = pool.Allocate(desc.ElementCount);
    }


    /// <summary>
    ///   Gets a value indicating whether this Descriptor Set is valid (i.e. it has been allocated and has a valid start offset).
    /// </summary>
    public readonly bool IsValid => DescriptorStartOffset != -1;


    /// <summary>
    ///   Sets a Descriptor in the specified slot.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <param name="value">The Descriptor to set.</param>
    public readonly void SetValue(int slot, object value)
    {
        HeapObjects[DescriptorStartOffset + slot].Value = value;
    }

    /// <summary>
    ///   Sets a Shader Resource View on a Graphics Resource in the specified slot.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <param name="shaderResourceView">The Shader Resource View on a Graphics Resource to set.</param>
    public readonly void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
    {
        HeapObjects[DescriptorStartOffset + slot].Value = shaderResourceView;
    }

    /// <summary>
    ///   Sets a Sampler State in the specified slot.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <param name="samplerState">The Sampler State to set.</param>
    public readonly void SetSamplerState(int slot, SamplerState samplerState)
    {
        HeapObjects[DescriptorStartOffset + slot].Value = samplerState;
    }

    /// <summary>
    ///   Sets a Constant Buffer View in the specified slot.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <param name="buffer">The Constant Buffer to set.</param>
    /// <param name="offset">The Constant Buffer View start offset.</param>
    /// <param name="size">The Constant Buffer View size.</param>
    public readonly void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
    {
        HeapObjects[DescriptorStartOffset + slot] = new DescriptorSetEntry(buffer, offset, size);
    }

    /// <summary>
    ///   Sets an Unordered Access View in the specified slot.
    /// </summary>
    /// <param name="slot">The slot index.</param>
    /// <param name="unorderedAccessView">The Unordered Access View to set.</param>
    public readonly void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
    {
        ref var heapObject = ref HeapObjects[DescriptorStartOffset + slot];
        heapObject.Value = unorderedAccessView;
        heapObject.Offset = (unorderedAccessView as Buffer)?.InitialCounterOffset ?? -1;
    }
#endif
}
