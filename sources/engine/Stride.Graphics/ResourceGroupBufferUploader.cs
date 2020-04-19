// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core;
using Stride.Shaders;

namespace Stride.Graphics
{
    /// <summary>
    /// Describes how DescriptorSet maps to real resource binding.
    /// This might become a core part of <see cref="Graphics.Effect"/> at some point.
    /// </summary>
    public struct ResourceGroupBufferUploader
    {
        private bool hasResourceRenaming;
        private ResourceGroupBinding[] resourceGroupBindings;

        public void Compile(GraphicsDevice graphicsDevice, EffectDescriptorSetReflection descriptorSetLayouts, EffectBytecode effectBytecode)
        {
            hasResourceRenaming = graphicsDevice.Features.HasResourceRenaming;
            resourceGroupBindings = new ResourceGroupBinding[descriptorSetLayouts.Layouts.Count];
            for (int setIndex = 0; setIndex < descriptorSetLayouts.Layouts.Count; setIndex++)
            {
                var layout = descriptorSetLayouts.Layouts[setIndex].Layout;
                if (layout == null)
                {
                    resourceGroupBindings[setIndex] = new ResourceGroupBinding { ConstantBufferSlot = -1 };
                    continue;
                }

                var resourceGroupBinding = new ResourceGroupBinding();

                for (int resourceIndex = 0; resourceIndex < layout.Entries.Count; resourceIndex++)
                {
                    var layoutEntry = layout.Entries[resourceIndex];

                    if (layoutEntry.Class == EffectParameterClass.ConstantBuffer)
                    {
                        var constantBuffer = effectBytecode.Reflection.ConstantBuffers.First(x => x.Name == layoutEntry.Key.Name);
                        resourceGroupBinding.ConstantBufferSlot = resourceIndex;
                        resourceGroupBinding.ConstantBufferPreallocated = Buffer.Constant.New(graphicsDevice, constantBuffer.Size, graphicsDevice.Features.HasResourceRenaming ? GraphicsResourceUsage.Dynamic : GraphicsResourceUsage.Default);
                    }
                }

                resourceGroupBindings[setIndex] = resourceGroupBinding;
            }
        }

        public void Apply(CommandList commandList, ResourceGroup[] resourceGroups, int resourceGroupsOffset)
        {
            if (resourceGroupBindings.Length == 0)
                return;

            var resourceGroupBinding = Interop.Pin(ref resourceGroupBindings[0]);
            for (int i = 0; i < resourceGroupBindings.Length; i++, resourceGroupBinding = Interop.IncrementPinned(resourceGroupBinding))
            {
                var resourceGroup = resourceGroups[resourceGroupsOffset + i];

                // Upload cbuffer (if not done yet)
                if (resourceGroupBinding.ConstantBufferSlot != -1 && resourceGroup != null && resourceGroup.ConstantBuffer.Data != IntPtr.Zero)
                {
                    var preallocatedBuffer = resourceGroup.ConstantBuffer.Buffer;
                    bool needUpdate = true;
                    if (preallocatedBuffer == null)
                        preallocatedBuffer = resourceGroupBinding.ConstantBufferPreallocated; // If it's preallocated buffer, we always upload
                    else if (resourceGroup.ConstantBuffer.Uploaded)
                        needUpdate = false; // If it's not preallocated and already uploaded, we can avoid uploading it again
                    else
                        resourceGroup.ConstantBuffer.Uploaded = true; // First time it is uploaded

                    if (needUpdate)
                    {
                        if (hasResourceRenaming)
                        {
                            var mappedConstantBuffer = commandList.MapSubresource(preallocatedBuffer, 0, MapMode.WriteDiscard);
                            Utilities.CopyMemory(mappedConstantBuffer.DataBox.DataPointer, resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size);
                            commandList.UnmapSubresource(mappedConstantBuffer);
                        }
                        else
                        {
                            commandList.UpdateSubresource(preallocatedBuffer, 0, new DataBox(resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size, 0));
                        }
                    }

                    resourceGroup.DescriptorSet.SetConstantBuffer(resourceGroupBinding.ConstantBufferSlot, preallocatedBuffer, resourceGroup.ConstantBuffer.Offset, resourceGroup.ConstantBuffer.Size);
                }
            }
        }

        internal struct ResourceGroupBinding
        {
            // Constant buffer
            public int ConstantBufferSlot;
            public Buffer ConstantBufferPreallocated;
        }
    }
}
