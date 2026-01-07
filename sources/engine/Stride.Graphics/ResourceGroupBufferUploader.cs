// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Shaders;

namespace Stride.Graphics
{
    /// <summary>
    ///   Describes how a Descriptor Set maps to real resource bindings.
    /// </summary>
    /// <remarks>
    ///   This might become a core part of <see cref="Effect"/> at some point.
    /// </remarks>
    /// <seealso cref="ResourceGroup"/>
    public struct ResourceGroupBufferUploader
    {
        /// <inheritdoc cref="GraphicsDeviceFeatures.HasResourceRenaming"/>
        private bool hasResourceRenaming;

        private ResourceGroupBinding[] resourceGroupBindings;

        private const int INVALID_CONSTANT_BUFFER_SLOT = -1;


        /// <summary>
        ///   Compiles a set of Constant Buffers to bind for an Effect, pre-allocating them as needed.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device used to create and manage GPU resources.</param>
        /// <param name="descriptorSetLayouts">
        ///   The Descriptor Set layouts that define the structure of resource bindings for the Effect.
        /// </param>
        /// <param name="effectBytecode">
        ///   The bytecode of the Effect, including reflection data used to resolve Constant Buffers
        ///   and other resources.
        /// </param>
        public void Compile(GraphicsDevice graphicsDevice,
                            EffectDescriptorSetReflection descriptorSetLayouts,
                            EffectBytecode effectBytecode)
        {
            hasResourceRenaming = graphicsDevice.Features.HasResourceRenaming;

            resourceGroupBindings = new ResourceGroupBinding[descriptorSetLayouts.Layouts.Count];

            for (int setIndex = 0; setIndex < descriptorSetLayouts.Layouts.Count; setIndex++)
            {
                var layout = descriptorSetLayouts.Layouts[setIndex].Layout;
                if (layout is null)
                {
                    // If the layout is null, it means that this set is not used in the Effect
                    resourceGroupBindings[setIndex] = new ResourceGroupBinding { ConstantBufferSlot = INVALID_CONSTANT_BUFFER_SLOT };
                    continue;
                }

                var resourceGroupBinding = new ResourceGroupBinding();

                for (int resourceIndex = 0; resourceIndex < layout.Entries.Count; resourceIndex++)
                {
                    var layoutEntry = layout.Entries[resourceIndex];

                    if (layoutEntry.Class == EffectParameterClass.ConstantBuffer)
                    {
                        // For Constant Buffers, we need to create a preallocated Buffer
                        var constantBuffer = effectBytecode.Reflection.ConstantBuffers.First(x => x.Name == layoutEntry.Key.Name);

                        var bufferUsage = hasResourceRenaming ? GraphicsResourceUsage.Dynamic : GraphicsResourceUsage.Default;

                        resourceGroupBinding.ConstantBufferSlot = resourceIndex;
                        resourceGroupBinding.PreAllocatedConstantBuffer = Buffer.Constant.New(graphicsDevice, constantBuffer.Size, bufferUsage);
                    }
                }

                resourceGroupBindings[setIndex] = resourceGroupBinding;
            }
        }

        /// <summary>
        ///   Applies the specified resource groups to the given Command List,
        ///   binding their Graphics Resources and uploading Constant Buffers as needed.
        /// </summary>
        /// <param name="commandList">The Command List to which the resource groups will be applied.</param>
        /// <param name="resourceGroups">
        ///   An array of resource groups containing the resources to be bound.
        ///   Each resource group may include Constant Buffers, Textures, or other GPU resources.
        /// </param>
        /// <param name="resourceGroupsOffset">
        ///   The starting index in the <paramref name="resourceGroups"/> array from which
        ///   to begin applying resource groups.
        /// </param>
        /// <remarks>
        ///   If a resource group contains a Constant Buffer that has not yet been uploaded,
        ///   the Buffer is uploaded before being bound.
        /// </remarks>
        public readonly unsafe void Apply(CommandList commandList, ResourceGroup[] resourceGroups, int resourceGroupsOffset)
        {
            if (resourceGroupBindings?.Length is null or 0)
                return;

            ref var resourceGroupBinding = ref MemoryMarshal.GetArrayDataReference(resourceGroupBindings);
            for (int i = 0; i < resourceGroupBindings.Length; i++, resourceGroupBinding = ref Unsafe.Add(ref resourceGroupBinding, 1))
            {
                var resourceGroup = resourceGroups[resourceGroupsOffset + i];

                // Upload the Constant Buffer (if not done yet)
                if (resourceGroupBinding.ConstantBufferSlot != INVALID_CONSTANT_BUFFER_SLOT &&
                    resourceGroup is not null &&
                    resourceGroup.ConstantBuffer.Data != IntPtr.Zero)
                {
                    var preAllocatedBuffer = resourceGroup.ConstantBuffer.Buffer;

                    bool needUpdate = true;
                    if (preAllocatedBuffer is null)
                        // If it's a pre-allocated Buffer, we always upload
                        preAllocatedBuffer = resourceGroupBinding.PreAllocatedConstantBuffer;

                    else if (resourceGroup.ConstantBuffer.Uploaded)
                        // If it is not pre-allocated and it is already uploaded, we can avoid uploading it again
                        needUpdate = false;
                    else
                        // First time it is uploaded
                        resourceGroup.ConstantBuffer.Uploaded = true;

                    if (needUpdate)
                    {
                        if (hasResourceRenaming)
                        {
                            var mappedConstantBuffer = commandList.MapSubResource(preAllocatedBuffer, subResourceIndex: 0, MapMode.WriteDiscard);

                            MemoryUtilities.CopyWithAlignmentFallback((void*) mappedConstantBuffer.DataBox.DataPointer,
                                                                      (void*) resourceGroup.ConstantBuffer.Data,
                                                                      (uint) resourceGroup.ConstantBuffer.Size);

                            commandList.UnmapSubResource(mappedConstantBuffer);
                        }
                        else
                        {
                            var dataBox = new DataBox(resourceGroup.ConstantBuffer.Data, resourceGroup.ConstantBuffer.Size, slicePitch: 0);
                            commandList.UpdateSubResource(preAllocatedBuffer, subResourceIndex: 0, dataBox);
                        }
                    }

                    // Bind the pre-allocated Constant Buffer to the resource group
                    resourceGroup.DescriptorSet.SetConstantBuffer(resourceGroupBinding.ConstantBufferSlot,
                                                                  preAllocatedBuffer,
                                                                  resourceGroup.ConstantBuffer.Offset,
                                                                  resourceGroup.ConstantBuffer.Size);
                }
            }
        }

        /// <summary>
        ///   Releases all pre-allocated Constant Buffers associated with the current
        ///   resource group bindings and clears the bindings.
        /// </summary>
        public void Clear()
        {
            if (resourceGroupBindings is null)
                return;

            for (int i = 0; i < resourceGroupBindings.Length; i++)
            {
                ref var binding = ref resourceGroupBindings[i];
                binding.PreAllocatedConstantBuffer?.Dispose();
            }

            resourceGroupBindings = null;
        }

        #region ResourceGroupBinding

        private struct ResourceGroupBinding
        {
            public int ConstantBufferSlot;
            public Buffer PreAllocatedConstantBuffer;
        }

        #endregion
    }
}
