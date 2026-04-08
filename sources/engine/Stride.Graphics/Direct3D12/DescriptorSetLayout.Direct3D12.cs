// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Shaders;

#if STRIDE_GRAPHICS_API_DIRECT3D12

namespace Stride.Graphics;

public partial class DescriptorSetLayout
{
    /// <summary>
    ///   Predefined binding slot for immutable Sampler Descriptors (like the ones defined in Shaders).
    /// </summary>
    internal const int IMMUTABLE_SAMPLER_BINDING_OFFSET = -1;

    private readonly int[] bindingOffsets;

    /// <summary>
    ///   Gets the number of Shader Resource Views (SRVs) and Unordered Access Views (UAVs) in the Descriptor Set layout.
    /// </summary>
    internal int SrvCount { get; private set; }

    /// <summary>
    ///   Gets the number of Samplers in the Descriptor Set layout.
    /// </summary>
    internal int SamplerCount { get; private set; }

    /// <summary>
    ///   Gets a mapping the binding slots with the binding offset of the Descriptor in each slot.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The binding offset refers to the offset from the starting handle (either <see cref="SrvStart"/>
    ///     or <see cref="SamplerStart"/>) where the Descriptor of each slot is located.
    ///   </para>
    ///   <para>
    ///     For the binding slots that represent immutable Samplers (like those defined in the Shader itself),
    ///     the binding offset is <c>-1</c>.
    ///   </para>
    ///   <para>
    ///     The offsets in this array starts with the binding offsets of the Samplers, then the
    ///     binding offsets of Shader Resource Views (SRVs), Unordered Access Views (UAVs),
    ///     and Constant Buffer Views (CBVs) follows.
    ///   </para>
    /// </remarks>
    internal ReadOnlySpan<int> BindingOffsets => bindingOffsets;


    /// <summary>
    ///   Initializes a new instance of the <see cref="DescriptorSetLayout"/> class.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    /// <param name="builder">
    ///   A <see cref="DescriptorSetLayoutBuilder"/> object containing the definitions of the bound Graphics Resources.
    /// </param>
    private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
    {
        bindingOffsets = new int[builder.ElementCount];

        int currentBindingOffset = 0;
        foreach (var entry in builder.Entries)
        {
            // We will both setup BindingOffsets and increment SamplerCount/SrvCount at the same time
            if (entry.Class == EffectParameterClass.Sampler)
            {
                for (int i = 0; i < entry.ArraySize; ++i)
                    bindingOffsets[currentBindingOffset++] = entry.ImmutableSampler is not null
                        ? IMMUTABLE_SAMPLER_BINDING_OFFSET
                        : SamplerCount++ * device.SamplerHandleIncrementSize;
            }
            else // SRV or UAV
            {
                for (int i = 0; i < entry.ArraySize; ++i)
                    bindingOffsets[currentBindingOffset++] = SrvCount++ * device.SrvHandleIncrementSize;
            }
        }
    }
}

#endif
