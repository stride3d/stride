// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Shaders;
#if STRIDE_GRAPHICS_API_VULKAN
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;

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

        internal static unsafe Silk.NET.Vulkan.DescriptorSetLayout CreateNativeDescriptorSetLayout(GraphicsDevice device, IList<DescriptorSetLayoutBuilder.Entry> entries, out uint[] typeCounts)
        {
            var bindings = new Silk.NET.Vulkan.DescriptorSetLayoutBinding[entries.Count];
            var immutableSamplers = new Sampler[entries.Count];

            int usedBindingCount = 0;

            typeCounts = new uint[DescriptorTypeCount];

            fixed (Sampler* immutableSamplersPointer = &immutableSamplers[0])
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];

                    // TODO VULKAN: Special case for unused bindings in PipelineState. Handle more nicely.
                    if (entry.ArraySize == 0)
                        continue;

                    bindings[usedBindingCount] = new Silk.NET.Vulkan.DescriptorSetLayoutBinding
                    {
                        DescriptorType = VulkanConvertExtensions.ConvertDescriptorType(entry.Class, entry.Type),
                        StageFlags = ShaderStageFlags.ShaderStageAll, // TODO VULKAN: Filter?
                        Binding = (uint)i,
                        DescriptorCount = (uint)entry.ArraySize
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
                        bindings[usedBindingCount].PImmutableSamplers = immutableSamplersPointer + i;
                    }

                    typeCounts[(int)bindings[usedBindingCount].DescriptorType] += bindings[usedBindingCount].DescriptorCount;

                    usedBindingCount++;
                }

                var createInfo = new DescriptorSetLayoutCreateInfo
                {
                    SType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)usedBindingCount,
                    PBindings = usedBindingCount > 0 ? (DescriptorSetLayoutBinding*)Core.Interop.Fixed(bindings) : null,
                };
                GetApi().CreateDescriptorSetLayout(device.NativeDevice, &createInfo, null, out var descriptorSetLayout);
                return descriptorSetLayout;
            }
        }
    }
}
#endif
