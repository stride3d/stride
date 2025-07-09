// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

#if STRIDE_GRAPHICS_API_DIRECT3D12

namespace Stride.Graphics;

public partial class DescriptorSetLayout
{
    internal const int IMMUTABLE_SAMPLER_BINDING_OFFSET = -1;

    private readonly int[] bindingOffsets;

    internal int SrvCount { get; private set; }

    internal int SamplerCount { get; private set; }

    internal ReadOnlySpan<int> BindingOffsets => bindingOffsets;


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
