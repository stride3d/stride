// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Shaders;
#if STRIDE_GRAPHICS_API_VULKAN
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    public partial class DescriptorSetLayout
    {
        internal const int DescriptorTypeCount = 11;

#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
        internal readonly DescriptorSetLayoutBuilder Builder;

        internal VkDescriptorSetLayout NativeLayout;

        internal uint[] TypeCounts;

        private DescriptorSetLayout(GraphicsDevice device, DescriptorSetLayoutBuilder builder) : base(device)
        {
            this.Builder = builder;
            Recreate();
        }

        private void Recreate()
        {
            NativeLayout = CreateNativeDescriptorSetLayout(GraphicsDevice, Builder.Entries, out TypeCounts);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            Recreate();
            return true;
        }

        /// <inheritdoc/>
        protected internal override unsafe void OnDestroyed()
        {
            GraphicsDevice.NativeDevice.DestroyDescriptorSetLayout(NativeLayout);
            NativeLayout = VkDescriptorSetLayout.Null;

            base.OnDestroyed();
        }
#endif

        internal static unsafe VkDescriptorSetLayout CreateNativeDescriptorSetLayout(GraphicsDevice device, IList<DescriptorSetLayoutBuilder.Entry> entries, out uint[] typeCounts)
        {
            var bindings = new VkDescriptorSetLayoutBinding[entries.Count];
            var immutableSamplers = new VkSampler[entries.Count];

            int usedBindingCount = 0;

            typeCounts = new uint[DescriptorTypeCount];

            fixed (VkSampler* immutableSamplersPointer = &immutableSamplers[0])
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    // TODO VULKAN: Special case for unused bindings in PipelineState. Handle more nicely.
                    if (entry.ArraySize == 0)
                        continue;

                    bindings[usedBindingCount] = new VkDescriptorSetLayoutBinding
                    {
                        descriptorType = VulkanConvertExtensions.ConvertDescriptorType(entry.Class, entry.Type),
                        stageFlags = VkShaderStageFlags.All, // TODO VULKAN: Filter?
                        binding = (uint)i,
                        descriptorCount = (uint)entry.ArraySize
                    };

                    if (entry.ImmutableSampler != null)
                    {
                        // TODO VULKAN: Handle immutable samplers for DescriptorCount > 1
                        if (entry.ArraySize > 1)
                        {
                            throw new NotImplementedException();
                        }

                        // Remember this, so we can choose the right VkDescriptorType in DescriptorSet.SetShaderResourceView
                        immutableSamplers[i] = entry.ImmutableSampler.NativeSampler;
                        //bindings[i].VkDescriptorType = VkDescriptorType.CombinedImageSampler;
                        bindings[usedBindingCount].pImmutableSamplers = immutableSamplersPointer + i;
                    }

                    typeCounts[(int)bindings[usedBindingCount].descriptorType] += bindings[usedBindingCount].descriptorCount;

                    usedBindingCount++;
                }

                var createInfo = new VkDescriptorSetLayoutCreateInfo
                {
                    sType = VkStructureType.DescriptorSetLayoutCreateInfo,
                    bindingCount = (uint)usedBindingCount,
                    pBindings = usedBindingCount > 0 ? (VkDescriptorSetLayoutBinding*)Core.Interop.Fixed(bindings) : null,
                };
                vkCreateDescriptorSetLayout(device.NativeDevice, &createInfo, null, out var descriptorSetLayout);
                return descriptorSetLayout;
            }
        }
    }
}
#endif
