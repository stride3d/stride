// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

#if STRIDE_GRAPHICS_API_DIRECT3D12

namespace Stride.Graphics
{
    public partial class DescriptorSetLayout
    {
        internal int SrvCount;
        internal int SamplerCount;

        // Need to remap for proper separation of samplers from the rest
        internal int[] BindingOffsets;

        private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder)
        {
            BindingOffsets = new int[builder.ElementCount];
            int currentBindingOffset = 0;
            foreach (var entry in builder.Entries)
            {
                // We will both setup BindingOffsets and increment SamplerCount/SrvCount at the same time
                if (entry.Class == EffectParameterClass.Sampler)
                {
                    for (int i = 0; i < entry.ArraySize; ++i)
                        BindingOffsets[currentBindingOffset++] = entry.ImmutableSampler != null ? -1 : SamplerCount++ * device.SamplerHandleIncrementSize;
                }
                else
                {
                    for (int i = 0; i < entry.ArraySize; ++i)
                        BindingOffsets[currentBindingOffset++] = SrvCount++ * device.SrvHandleIncrementSize;
                }
            }
        }
    }
}
#endif
