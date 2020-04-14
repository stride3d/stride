// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Rendering;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    /// <summary>
    /// Allocator for resource groups.
    /// </summary>
    /// <note>Non thread-safe. You should have one such allocator per thread.</note>
    public class ResourceGroupAllocator : ComponentBase
    {
        private readonly GraphicsDevice graphicsDevice;
        private readonly GraphicsResourceAllocator allocator;
        private readonly List<DescriptorPool> descriptorPools = new List<DescriptorPool>();
        private readonly List<BufferPool> bufferPools = new List<BufferPool>();

        private readonly List<ResourceGroup> resourceGroupPool = new List<ResourceGroup>();
        private int currentResourceGroupPoolIndex = 0;

        private DescriptorPool currentDescriptorPool;
        private int currentDescriptorPoolIndex = -1;

        private BufferPool currentBufferPool;
        private int currentBufferPoolIndex = -1;

        private CommandList commandList;

        public ResourceGroupAllocator(GraphicsResourceAllocator allocator, CommandList commandList)
        {
            this.allocator = allocator;
            this.commandList = commandList;
            this.graphicsDevice = commandList.GraphicsDevice;

            SetupNextDescriptorPool();
        }

        protected override void Destroy()
        {
            foreach (var descriptorPool in descriptorPools)
                descriptorPool.Dispose();
            descriptorPools.Clear();
            currentDescriptorPool = null;

            foreach (var bufferPool in bufferPools)
                bufferPool.Dispose();
            bufferPools.Clear();
            currentBufferPool = null;

            base.Destroy();
        }

        public void Reset(CommandList commandList)
        {
            this.commandList = commandList;

            foreach (var descriptorPool in descriptorPools)
            {
                descriptorPool.Reset();
            }

            foreach (var bufferPool in bufferPools)
            {
                bufferPool.Reset();
            }

            currentResourceGroupPoolIndex = -1;

            currentDescriptorPool = descriptorPools[0];
            currentDescriptorPoolIndex = 0;

            currentBufferPool = null;
            currentBufferPoolIndex = -1;
        }

        public void Flush()
        {
            currentBufferPool?.Unmap();
        }

        public ResourceGroup AllocateResourceGroup()
        {
            ResourceGroup resourceGroup;
            if (++currentResourceGroupPoolIndex >= resourceGroupPool.Count)
            {
                resourceGroupPool.Add(resourceGroup = new ResourceGroup());
            }
            else
            {
                resourceGroup = resourceGroupPool[currentResourceGroupPoolIndex];

                // Clear object
                resourceGroup.DescriptorSet = default(DescriptorSet);
                resourceGroup.ConstantBuffer = default(BufferPoolAllocationResult);
            }
            return resourceGroup;
        }

        public void PrepareResourceGroup(ResourceGroupLayout resourceGroupLayout, BufferPoolAllocationType constantBufferAllocationType, ResourceGroup resourceGroup)
        {
            if (resourceGroup == null)
                throw new InvalidOperationException();

            resourceGroup.DescriptorSet = DescriptorSet.New(graphicsDevice, currentDescriptorPool, resourceGroupLayout.DescriptorSetLayout);
            if (!resourceGroup.DescriptorSet.IsValid)
            {
                SetupNextDescriptorPool();
                resourceGroup.DescriptorSet = DescriptorSet.New(graphicsDevice, currentDescriptorPool, resourceGroupLayout.DescriptorSetLayout);
            }

            if (resourceGroupLayout.ConstantBufferSize > 0)
            {
                if (currentBufferPool == null || !currentBufferPool.CanAllocate(resourceGroupLayout.ConstantBufferSize))
                {
                    SetupNextBufferPool();
                }

                currentBufferPool.Allocate(graphicsDevice, resourceGroupLayout.ConstantBufferSize, constantBufferAllocationType, ref resourceGroup.ConstantBuffer);
            }
        }

        private void SetupNextBufferPool()
        {
            Flush();

            currentBufferPoolIndex++;
            if (currentBufferPoolIndex >= bufferPools.Count)
            {
                bufferPools.Add(currentBufferPool = BufferPool.New(allocator, graphicsDevice, 1024 * 1024));
            }
            else
            {
                currentBufferPool = bufferPools[currentBufferPoolIndex];
            }

            currentBufferPool.Map(commandList);
        }

        private void SetupNextDescriptorPool()
        {
            currentDescriptorPoolIndex++;
            if (currentDescriptorPoolIndex >= descriptorPools.Count)
            {
                descriptorPools.Add(currentDescriptorPool = DescriptorPool.New(
                    graphicsDevice,
                    new[]
                    {
                        new DescriptorTypeCount(EffectParameterClass.ConstantBuffer, 16384),
                        new DescriptorTypeCount(EffectParameterClass.ShaderResourceView, 65536),
                        new DescriptorTypeCount(EffectParameterClass.UnorderedAccessView, 4096),
                        new DescriptorTypeCount(EffectParameterClass.Sampler, 16384),
                    }));
            }
            else
            {
                currentDescriptorPool = descriptorPools[currentDescriptorPoolIndex];
            }
        }
    }
}
