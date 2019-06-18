// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Graphics
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

        public Buffer GetPreallocatedConstantBuffer(int setIndex)
        {
            return resourceGroupBindings[setIndex].ConstantBufferPreallocated;
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
                    var buffer = resourceGroup.ConstantBuffer.Buffer;

                    if (buffer == null)
                    {
                        // If it's preallocated buffer, we always upload
                        buffer = resourceGroupBinding.ConstantBufferPreallocated;

                        if (hasResourceRenaming)
                        {
                            var mappedConstantBuffer = commandList.MapSubresource(buffer, 0, MapMode.WriteDiscard);
                            Utilities.CopyMemory(mappedConstantBuffer.DataBox.DataPointer, resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size);
                            commandList.UnmapSubresource(mappedConstantBuffer);
                        }
                        else
                        {
                            commandList.UpdateSubresource(buffer, 0, new DataBox(resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size, 0));
                        }
                    }
                    else if (resourceGroup.ConstantBuffer.TrySetUploaded())
                    {
                        // If it's not preallocated and already uploaded, we can avoid uploading it again.
                        // We need to upload it immediately, since might not be the first command list to be executed.
                        if (hasResourceRenaming)
                        {
                            var mappedConstantBuffer = commandList.GraphicsDevice.MapSubresource(buffer, 0, MapMode.WriteDiscard);
                            Utilities.CopyMemory(mappedConstantBuffer.DataBox.DataPointer, resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size);
                            commandList.GraphicsDevice.UnmapSubresource(mappedConstantBuffer);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                    }
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
