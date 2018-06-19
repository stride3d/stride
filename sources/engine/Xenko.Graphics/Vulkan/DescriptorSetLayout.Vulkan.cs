// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Shaders;
#if XENKO_GRAPHICS_API_VULKAN
using SharpVulkan;

namespace Xenko.Graphics
{
    public partial class DescriptorSetLayout
    {
        internal const int DescriptorTypeCount = 11;

#if !XENKO_GRAPHICS_NO_DESCRIPTOR_COPIES
        internal readonly DescriptorSetLayoutBuilder Builder;

        internal SharpVulkan.DescriptorSetLayout NativeLayout;

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
            NativeLayout = SharpVulkan.DescriptorSetLayout.Null;

            base.OnDestroyed();
        }
#endif

        internal static unsafe SharpVulkan.DescriptorSetLayout CreateNativeDescriptorSetLayout(GraphicsDevice device, IList<DescriptorSetLayoutBuilder.Entry> entries, out uint[] typeCounts)
        {
            var bindings = new DescriptorSetLayoutBinding[entries.Count];
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

                    bindings[usedBindingCount] = new DescriptorSetLayoutBinding
                    {
                        DescriptorType = VulkanConvertExtensions.ConvertDescriptorType(entry.Class, entry.Type),
                        StageFlags = ShaderStageFlags.All, // TODO VULKAN: Filter?
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

                        // Remember this, so we can choose the right DescriptorType in DescriptorSet.SetShaderResourceView
                        immutableSamplers[i] = entry.ImmutableSampler.NativeSampler;
                        //bindings[i].DescriptorType = DescriptorType.CombinedImageSampler;
                        bindings[usedBindingCount].ImmutableSamplers = new IntPtr(immutableSamplersPointer + i);
                    }

                    typeCounts[(int)bindings[usedBindingCount].DescriptorType] += bindings[usedBindingCount].DescriptorCount;

                    usedBindingCount++;
                }

                var createInfo = new DescriptorSetLayoutCreateInfo
                {
                    StructureType = StructureType.DescriptorSetLayoutCreateInfo,
                    BindingCount = (uint)usedBindingCount,
                    Bindings = usedBindingCount > 0 ? new IntPtr(Interop.Fixed(bindings)) : IntPtr.Zero
                };
                return device.NativeDevice.CreateDescriptorSetLayout(ref createInfo);
            }
        }
    }
}
#endif
