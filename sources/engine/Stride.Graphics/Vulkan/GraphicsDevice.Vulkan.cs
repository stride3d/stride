// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

using Stride.Core;
using Stride.Core.Threading;

namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        internal int ConstantBufferDataPlacementAlignment;

        internal readonly ConcurrentPool<List<VkDescriptorPool>> DescriptorPoolLists = new ConcurrentPool<List<VkDescriptorPool>>(() => new List<VkDescriptorPool>());
        internal readonly ConcurrentPool<List<Texture>> StagingResourceLists = new ConcurrentPool<List<Texture>>(() => new List<Texture>());

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Vulkan;
        internal GraphicsProfile RequestedProfile;

        private bool simulateReset = false;
        private string rendererName;

        private VkDevice nativeDevice;
        internal VkQueue NativeCommandQueue;
        internal object QueueLock = new object();

        internal ThreadLocal<VkCommandPool> NativeCopyCommandPools;
        private NativeResourceCollector nativeResourceCollector;
        private GraphicsResourceLinkCollector graphicsResourceLinkCollector;

        private VkBuffer nativeUploadBuffer;
        private VkDeviceMemory nativeUploadBufferMemory;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferSize;
        private int nativeUploadBufferOffset;

        private Queue<KeyValuePair<long, VkFence>> nativeFences = new Queue<KeyValuePair<long, VkFence>>();
        private long lastCompletedFence;
        internal long NextFenceValue = 1;

        internal HeapPool DescriptorPools;
        internal const uint MaxDescriptorSetCount = 256;
        internal readonly uint[] MaxDescriptorTypeCounts = new uint[DescriptorSetLayout.DescriptorTypeCount]
        {
            256, // Sampler
            0, // CombinedImageSampler
            512, // SampledImage
            0, // StorageImage
            64, // UniformTexelBuffer
            0, // StorageTexelBuffer
            512, // UniformBuffer
            0, // StorageBuffer
            0, // UniformBufferDynamic
            0, // StorageBufferDynamic
            0 // InputAttachment
        };

        internal Buffer EmptyTexelBufferInt, EmptyTexelBufferFloat;
        internal Texture EmptyTexture;

        internal VkPhysicalDevice NativePhysicalDevice => Adapter.GetPhysicalDevice(IsDebugMode);

        internal VkInstance NativeInstance => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstance;

        internal struct BufferInfo
        {
            public long FenceValue;

            public VkBuffer Buffer;

            public VkDeviceMemory Memory;

            public BufferInfo(long fenceValue, VkBuffer buffer, VkDeviceMemory memory)
            {
                FenceValue = fenceValue;
                Buffer = buffer;
                Memory = memory;
            }
        }

        /// <summary>
        /// The tick frquency of timestamp queries in Hertz.
        /// </summary>
        public long TimestampFrequency { get; private set; }

        /// <summary>
        ///     Gets the status of this device.
        /// </summary>
        /// <value>The graphics device status.</value>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                if (simulateReset)
                {
                    simulateReset = false;
                    return GraphicsDeviceStatus.Reset;
                }

                //var result = NativeDevice.DeviceRemovedReason;
                //if (result == SharpDX.DXGI.ResultCode.DeviceRemoved)
                //{
                //    return GraphicsDeviceStatus.Removed;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DeviceReset)
                //{
                //    return GraphicsDeviceStatus.Reset;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DeviceHung)
                //{
                //    return GraphicsDeviceStatus.Hung;
                //}

                //if (result == SharpDX.DXGI.ResultCode.DriverInternalError)
                //{
                //    return GraphicsDeviceStatus.InternalError;
                //}

                //if (result == SharpDX.DXGI.ResultCode.InvalidCall)
                //{
                //    return GraphicsDeviceStatus.InvalidCall;
                //}

                //if (result.Code < 0)
                //{
                //    return GraphicsDeviceStatus.Reset;
                //}

                return GraphicsDeviceStatus.Normal;
            }
        }

        /// <summary>
        ///     Gets the native device.
        /// </summary>
        /// <value>The native device.</value>
        internal VkDevice NativeDevice
        {
            get { return nativeDevice; }
        }

        /// <summary>
        ///     Marks context as active on the current thread.
        /// </summary>
        public void Begin()
        {
            FrameTriangleCount = 0;
            FrameDrawCalls = 0;
        }

        /// <summary>
        /// Enables profiling.
        /// </summary>
        /// <param name="enabledFlag">if set to <c>true</c> [enabled flag].</param>
        public void EnableProfile(bool enabledFlag)
        {
        }

        /// <summary>
        ///     Unmarks context as active on the current thread.
        /// </summary>
        public void End()
        {
        }

        /// <summary>
        /// Executes a deferred command list.
        /// </summary>
        /// <param name="commandList">The deferred command list.</param>
        public void ExecuteCommandList(CompiledCommandList commandList)
        {
            ExecuteCommandListInternal(commandList);
        }

        /// <summary>
        /// Executes multiple deferred command lists.
        /// </summary>
        /// <param name="count">Number of command lists to execute.</param>
        /// <param name="commandLists">The deferred command lists.</param>
        public unsafe void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            if (commandLists == null) throw new ArgumentNullException(nameof(commandLists));
            if (count > commandLists.Length) throw new ArgumentOutOfRangeException(nameof(count));

            var fenceValue = NextFenceValue++;

            // Create a fence
            var fenceCreateInfo = new VkFenceCreateInfo { sType = VkStructureType.FenceCreateInfo };
            vkCreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, VkFence>(fenceValue, fence));

            // Collect resources
            var commandBuffers = stackalloc VkCommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                commandBuffers[i] = commandLists[i].NativeCommandBuffer;
                RecycleCommandListResources(commandLists[i], fenceValue);
            }

            // Submit commands
            var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = (uint)count,
                pCommandBuffers = commandBuffers,
                waitSemaphoreCount = presentSemaphore != VkSemaphore.Null ? 1U : 0U,
                pWaitSemaphores = &presentSemaphoreCopy,
                pWaitDstStageMask = &pipelineStageFlags,
            };
            vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

            presentSemaphore = VkSemaphore.Null;
            nativeResourceCollector.Release();
            graphicsResourceLinkCollector.Release();
        }

        private void InitializePostFeatures()
        {
        }

        private string GetRendererName()
        {
            return rendererName;
        }

        public void SimulateReset()
        {
            simulateReset = true;
        }

        /// <summary>
        ///     Initializes the specified device.
        /// </summary>
        /// <param name="graphicsProfiles">The graphics profiles.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private unsafe void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice != VkDevice.Null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            vkGetPhysicalDeviceProperties(NativePhysicalDevice, out var physicalDeviceProperties);
            ConstantBufferDataPlacementAlignment = (int)physicalDeviceProperties.limits.minUniformBufferOffsetAlignment;
            TimestampFrequency = (long)(1.0e9 / physicalDeviceProperties.limits.timestampPeriod); // Resolution in nanoseconds

            RequestedProfile = graphicsProfiles.First();

            var queueProperties = vkGetPhysicalDeviceQueueFamilyProperties(NativePhysicalDevice);
            //IsProfilingSupported = queueProperties[0].TimestampValidBits > 0;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            // TODO VULKAN
            // Create Vulkan device based on profile
            float queuePriorities = 0;
            var queueCreateInfo = new VkDeviceQueueCreateInfo
            {
                sType = VkStructureType.DeviceQueueCreateInfo,
                queueFamilyIndex = 0,
                queueCount = 1,
                pQueuePriorities = &queuePriorities,
            };

            var enabledFeature = new VkPhysicalDeviceFeatures
            {
                fillModeNonSolid = true,
                shaderClipDistance = true,
                shaderCullDistance = true,
                samplerAnisotropy = true,
                depthClamp = true,
            };

            var extensionProperties = vkEnumerateDeviceExtensionProperties(NativePhysicalDevice);
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                fixed (VkExtensionProperties* extensionPropertiesPtr = extensionProperties)
                {
                    var namePointer = new IntPtr(extensionPropertiesPtr[index].extensionName);
                    var name = Marshal.PtrToStringAnsi(namePointer);
                    availableExtensionNames.Add(name);
                }
            }

            desiredExtensionNames.Add(KHRSwapchainExtensionName);
            if (!availableExtensionNames.Contains(KHRSwapchainExtensionName))
                throw new InvalidOperationException();

            if (availableExtensionNames.Contains(EXTDebugMarkerExtensionName) && IsDebugMode)
            {
                desiredExtensionNames.Add(EXTDebugMarkerExtensionName);
                IsProfilingSupported = true;
            }

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                var deviceCreateInfo = new VkDeviceCreateInfo
                {
                    sType = VkStructureType.DeviceCreateInfo,
                    queueCreateInfoCount = 1,
                    pQueueCreateInfos = &queueCreateInfo,
                    enabledExtensionCount = (uint)enabledExtensionNames.Length,
                    ppEnabledExtensionNames = enabledExtensionNames.Length > 0 ? (byte**)Core.Interop.Fixed(enabledExtensionNames) : null,
                    pEnabledFeatures = &enabledFeature,
                };

                vkCreateDevice(NativePhysicalDevice, &deviceCreateInfo, null, out nativeDevice);
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }
            }

            vkGetDeviceQueue(nativeDevice, 0, 0, out NativeCommandQueue);

            NativeCopyCommandPools = new ThreadLocal<VkCommandPool>(() =>
            {
                //// Prepare copy command list (start it closed, so that every new use start with a Reset)
                var commandPoolCreateInfo = new VkCommandPoolCreateInfo
                {
                    sType = VkStructureType.CommandPoolCreateInfo,
                    queueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                    flags = VkCommandPoolCreateFlags.ResetCommandBuffer
                };

                vkCreateCommandPool(NativeDevice, &commandPoolCreateInfo, null, out var result);
                return result;
            }, true);

            DescriptorPools = new HeapPool(this);

            nativeResourceCollector = new NativeResourceCollector(this);
            graphicsResourceLinkCollector = new GraphicsResourceLinkCollector(this);

            EmptyTexelBufferInt = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_UInt);
            EmptyTexelBufferFloat = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_Float);
            EmptyTexture = Texture.New2D(this, 1, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource);
        }

        internal unsafe IntPtr AllocateUploadBuffer(int size, out VkBuffer resource, out int offset)
        {
            // TODO D3D12 thread safety, should we simply use locks?
            if (nativeUploadBuffer == VkBuffer.Null || nativeUploadBufferOffset + size > nativeUploadBufferSize)
            {
                if (nativeUploadBuffer != VkBuffer.Null)
                {
                    vkUnmapMemory(NativeDevice, nativeUploadBufferMemory);
                    Collect(nativeUploadBuffer);
                    Collect(nativeUploadBufferMemory);
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                nativeUploadBufferSize = Math.Max(4 * 1024 * 1024, size);

                var bufferCreateInfo = new VkBufferCreateInfo
                {
                    sType = VkStructureType.BufferCreateInfo,
                    size = (ulong)nativeUploadBufferSize,
                    flags = VkBufferCreateFlags.None,
                    usage = VkBufferUsageFlags.TransferSrc,
                };
                vkCreateBuffer(NativeDevice, &bufferCreateInfo, null, out nativeUploadBuffer);
                AllocateMemory(VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                fixed (IntPtr* nativeUploadBufferStartPtr = &nativeUploadBufferStart)
                    vkMapMemory(NativeDevice, nativeUploadBufferMemory, 0, (ulong)nativeUploadBufferSize, VkMemoryMapFlags.None, (void**)nativeUploadBufferStartPtr);
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = nativeUploadBuffer;
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferStart + offset;
        }

        protected unsafe void AllocateMemory(VkMemoryPropertyFlags memoryProperties)
        {
            vkGetBufferMemoryRequirements(nativeDevice, nativeUploadBuffer, out var memoryRequirements);

            if (memoryRequirements.size == 0)
                return;

            var allocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memoryRequirements.size,
            };

            vkGetPhysicalDeviceMemoryProperties(NativePhysicalDevice, out var physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.memoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *(&physicalDeviceMemoryProperties.memoryTypes_0 + i);
                    if ((memoryType.propertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.memoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            fixed (VkDeviceMemory* nativeUploadBufferMemoryPtr = &nativeUploadBufferMemory)
                vkAllocateMemory(NativeDevice, &allocateInfo, null, nativeUploadBufferMemoryPtr);

            vkBindBufferMemory(NativeDevice, nativeUploadBuffer, nativeUploadBufferMemory, 0);
        }

        private void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
        }

        protected void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        private unsafe void ReleaseDevice()
        {
            EmptyTexelBufferInt.Dispose();
            EmptyTexelBufferInt = null;
            EmptyTexelBufferFloat.Dispose();
            EmptyTexelBufferFloat = null;

            EmptyTexture.Dispose();
            EmptyTexture = null;

            // Wait for all queues to be idle
            vkDeviceWaitIdle(nativeDevice);

            // Destroy all remaining fences
            GetCompletedValue();

            // Mark upload buffer for destruction
            if (nativeUploadBuffer != VkBuffer.Null)
            {
                vkUnmapMemory(NativeDevice, nativeUploadBufferMemory);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBuffer);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBufferMemory);

                nativeUploadBuffer = VkBuffer.Null;
                nativeUploadBufferMemory = VkDeviceMemory.Null;
            }

            // Release fenced resources
            nativeResourceCollector.Dispose();
            DescriptorPools.Dispose();

            foreach (var nativeCopyCommandPool in NativeCopyCommandPools.Values)
                vkDestroyCommandPool(nativeDevice, nativeCopyCommandPool, null);
            NativeCopyCommandPools.Dispose();
            NativeCopyCommandPools = null;
            vkDestroyDevice(nativeDevice, null);
        }

        internal void OnDestroyed()
        {
        }

        internal unsafe long ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            //if (nativeUploadBuffer != VkBuffer.Null)
            //{
            //    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
            //    TemporaryResources.Enqueue(new BufferInfo(NextFenceValue, nativeUploadBuffer, nativeUploadBufferMemory));

            //    nativeUploadBuffer = VkBuffer.Null;
            //    nativeUploadBufferMemory = VkDeviceMemory.Null;
            //}

            var fenceValue = NextFenceValue++;

            // Create new fence
            var fenceCreateInfo = new VkFenceCreateInfo { sType = VkStructureType.FenceCreateInfo };
            vkCreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, VkFence>(fenceValue, fence));

            // Collect resources
            RecycleCommandListResources(commandList, fenceValue);

            // Submit commands
            var nativeCommandBufferCopy = commandList.NativeCommandBuffer;
            var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;

            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &nativeCommandBufferCopy,
                waitSemaphoreCount = presentSemaphore != VkSemaphore.Null ? 1U : 0U,
                pWaitSemaphores = &presentSemaphoreCopy,
                pWaitDstStageMask = &pipelineStageFlags,
            };
            vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

            presentSemaphore = VkSemaphore.Null;
            nativeResourceCollector.Release();
            graphicsResourceLinkCollector.Release();

            return fenceValue;
        }

        private void RecycleCommandListResources(CompiledCommandList commandList, long fenceValue)
        {
            // Set fence on staging textures
            foreach (var stagingResource in commandList.StagingResources)
            {
                stagingResource.StagingFenceValue = fenceValue;
            }

            StagingResourceLists.Release(commandList.StagingResources);
            commandList.StagingResources.Clear();

            // Recycle all resources
            foreach (var descriptorPool in commandList.DescriptorPools)
            {
                DescriptorPools.RecycleObject(fenceValue, descriptorPool);
            }
            DescriptorPoolLists.Release(commandList.DescriptorPools);
            commandList.DescriptorPools.Clear();

            commandList.Builder.CommandBufferPool.RecycleObject(fenceValue, commandList.NativeCommandBuffer);
        }

        internal bool IsFenceCompleteInternal(long fenceValue)
        {
            // Try to avoid checking the fence if possible
            if (fenceValue > lastCompletedFence)
            {
                GetCompletedValue();
            }

            return fenceValue <= lastCompletedFence;
        }

        private SpinLock spinLock = new SpinLock();

        internal unsafe long GetCompletedValue()
        {
            bool lockTaken = false;
            try
            {
                spinLock.Enter(ref lockTaken);

                while (nativeFences.Count > 0 && vkGetFenceStatus(NativeDevice, nativeFences.Peek().Value) == VkResult.Success)
                {
                    var fence = nativeFences.Dequeue();
                    vkDestroyFence(NativeDevice, fence.Value, null);
                    lastCompletedFence = Math.Max(lastCompletedFence, fence.Key);
                }

                return lastCompletedFence;
            }
            finally
            {
                if (lockTaken)
                    spinLock.Exit(false);
            }
        }

        internal unsafe void WaitForFenceInternal(long fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            // TODO D3D12 in case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue then first one
            lock (nativeFences)
            {
                while (nativeFences.Count > 0 && nativeFences.Peek().Key <= fenceValue)
                {
                    var fence = nativeFences.Dequeue();
                    var fenceCopy = fence.Value;

                    vkWaitForFences(NativeDevice, 1, &fenceCopy, true, ulong.MaxValue);
                    vkDestroyFence(NativeDevice, fence.Value, null);
                    lastCompletedFence = fenceValue;
                }
            }
        }

        private VkSemaphore presentSemaphore;

        public unsafe VkSemaphore GetNextPresentSemaphore()
        {
            var createInfo = new VkSemaphoreCreateInfo { sType = VkStructureType.SemaphoreCreateInfo };
            vkCreateSemaphore(NativeDevice, &createInfo, null, out presentSemaphore);
            Collect(presentSemaphore);
            return presentSemaphore;
        }

        internal void Collect(NativeResource nativeResource)
        {
            nativeResourceCollector.Add(NextFenceValue, nativeResource);
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            switch (resourceLink.Resource)
            {
                case Texture texture:
                    if (texture.Usage == GraphicsResourceUsage.Dynamic)
                    {
                        // Increase the reference count until GPU is done with the resource
                        resourceLink.ReferenceCount++;
                        graphicsResourceLinkCollector.Add(NextFenceValue, resourceLink);
                    }
                    break;

                case Buffer buffer:
                    if (buffer.Usage == GraphicsResourceUsage.Dynamic)
                    {
                        // Increase the reference count until GPU is done with the resource
                        resourceLink.ReferenceCount++;
                        graphicsResourceLinkCollector.Add(NextFenceValue, resourceLink);
                    }
                    break;

                case QueryPool _:
                    resourceLink.ReferenceCount++;
                    graphicsResourceLinkCollector.Add(NextFenceValue, resourceLink);
                    break;
            }
        }
    }

    internal abstract class ResourcePool<T> : ComponentBase
    {
        protected readonly GraphicsDevice GraphicsDevice;
        private readonly Queue<KeyValuePair<long, T>> liveObjects = new Queue<KeyValuePair<long, T>>();

        protected ResourcePool(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public T GetObject()
        {
            lock (liveObjects)
            {
                // Check if first allocator is ready for reuse
                if (liveObjects.Count > 0)
                {
                    var firstAllocator = liveObjects.Peek();
                    if (firstAllocator.Key <= GraphicsDevice.GetCompletedValue())
                    {
                        liveObjects.Dequeue();
                        ResetObject(firstAllocator.Value);
                        return firstAllocator.Value;
                    }
                }

                return CreateObject();
            }
        }

        public void RecycleObject(long fenceValue, T obj)
        {
            lock (liveObjects)
            {
                liveObjects.Enqueue(new KeyValuePair<long, T>(fenceValue, obj));
            }
        }

        protected abstract T CreateObject();

        protected abstract void ResetObject(T obj);

        protected virtual void DestroyObject(T obj)
        {
        }

        protected override void Destroy()
        {
            lock (liveObjects)
            { 
                foreach (var item in liveObjects)
                {
                    DestroyObject(item.Value);
                }
            }

            base.Destroy();
        }
    }

    internal class CommandBufferPool : ResourcePool<VkCommandBuffer>
    {
        private readonly VkCommandPool commandPool;

        public unsafe CommandBufferPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            var commandPoolCreateInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer
            };

            vkCreateCommandPool(graphicsDevice.NativeDevice, &commandPoolCreateInfo, null, out commandPool);
        }

        protected override unsafe VkCommandBuffer CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var commandBufferAllocationInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                level = VkCommandBufferLevel.Primary,
                commandPool = commandPool,
                commandBufferCount = 1,
            };

            VkCommandBuffer commandBuffer;
            vkAllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);
            return commandBuffer;
        }

        protected override void ResetObject(VkCommandBuffer obj)
        {
            vkResetCommandBuffer(obj, VkCommandBufferResetFlags.None);
        }

        protected override unsafe void Destroy()
        {
            base.Destroy();

            vkDestroyCommandPool(GraphicsDevice.NativeDevice, commandPool, null);
        }
    }

    internal class HeapPool : ResourcePool<VkDescriptorPool>
    {
        public HeapPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        protected override unsafe VkDescriptorPool CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var poolSizes = GraphicsDevice.MaxDescriptorTypeCounts
                .Select((count, index) => new VkDescriptorPoolSize { type = (VkDescriptorType)index, descriptorCount = count })
                .Where(size => size.descriptorCount > 0)
                .ToArray();

            var descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo
            {
                sType = VkStructureType.DescriptorPoolCreateInfo,
                poolSizeCount = (uint)poolSizes.Length,
                pPoolSizes = (VkDescriptorPoolSize*)Core.Interop.Fixed(poolSizes),
                maxSets = GraphicsDevice.MaxDescriptorSetCount,
            };
            vkCreateDescriptorPool(GraphicsDevice.NativeDevice, &descriptorPoolCreateInfo, null, out var descriptorPool);
            return descriptorPool;
        }

        protected override void ResetObject(VkDescriptorPool obj)
        {
            vkResetDescriptorPool(GraphicsDevice.NativeDevice, obj, VkDescriptorPoolResetFlags.None);
        }

        protected override unsafe void DestroyObject(VkDescriptorPool obj)
        {
            vkDestroyDescriptorPool(GraphicsDevice.NativeDevice, obj, null);
        }
    }

    internal struct NativeResource
    {
        public VkDebugReportObjectTypeEXT type;

        public ulong handle;

        public NativeResource(VkDebugReportObjectTypeEXT type, ulong handle)
        {
            this.type = type;
            this.handle = handle;
        }

        public static unsafe implicit operator NativeResource(VkBuffer handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.BufferEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkBufferView handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.BufferViewEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkImage handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.ImageEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkImageView handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.ImageViewEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkDeviceMemory handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.DeviceMemoryEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkSampler handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.SamplerEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkFramebuffer handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.FramebufferEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkSemaphore handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.SemaphoreEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkFence handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.FenceEXT, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkQueryPool handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.QueryPoolEXT, *(ulong*)&handle);
        }

        public unsafe void Destroy(GraphicsDevice device)
        {
            var handleCopy = handle;

            switch (type)
            {
                case VkDebugReportObjectTypeEXT.BufferEXT:
                    vkDestroyBuffer(device.NativeDevice, *(VkBuffer*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.BufferViewEXT:
                    vkDestroyBufferView(device.NativeDevice, *(VkBufferView*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.ImageEXT:
                    vkDestroyImage(device.NativeDevice, *(VkImage*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.ImageViewEXT:
                    vkDestroyImageView(device.NativeDevice, *(VkImageView*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.DeviceMemoryEXT:
                    vkFreeMemory(device.NativeDevice, *(VkDeviceMemory*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.SamplerEXT:
                    vkDestroySampler(device.NativeDevice, *(VkSampler*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.FramebufferEXT:
                    vkDestroyFramebuffer(device.NativeDevice, *(VkFramebuffer*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.SemaphoreEXT:
                    vkDestroySemaphore(device.NativeDevice, *(VkSemaphore*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.FenceEXT:
                    vkDestroyFence(device.NativeDevice, *(VkFence*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.QueryPoolEXT:
                    vkDestroyQueryPool(device.NativeDevice, *(VkQueryPool*)&handleCopy, null);
                    break;
            }
        }
    }

    internal class GraphicsResourceLinkCollector : TemporaryResourceCollector<GraphicsResourceLink>
    {
        public GraphicsResourceLinkCollector(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        protected override void ReleaseObject(GraphicsResourceLink item)
        {
            item.ReferenceCount--;
        }
    }

    internal class NativeResourceCollector : TemporaryResourceCollector<NativeResource>
    {
        public NativeResourceCollector(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        protected override void ReleaseObject(NativeResource item)
        {
            item.Destroy(GraphicsDevice);
        }
    }
    
    internal abstract class TemporaryResourceCollector<T> : IDisposable
    {
        protected readonly GraphicsDevice GraphicsDevice;
        private readonly Queue<KeyValuePair<long, T>> items = new Queue<KeyValuePair<long, T>>();

        protected TemporaryResourceCollector(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public void Add(long fenceValue, T item)
        {
            lock (items)
            {
                items.Enqueue(new KeyValuePair<long, T>(fenceValue, item));
            }
        }

        public void Release()
        {
            lock (items)
            {
                while (items.Count > 0 && GraphicsDevice.IsFenceCompleteInternal(items.Peek().Key))
                {
                    ReleaseObject(items.Dequeue().Value);
                }
            }
        }

        protected abstract void ReleaseObject(T item);

        public void Dispose()
        {
            while (items.Count > 0)
            {
                ReleaseObject(items.Dequeue().Value);
            }
        }
    }
}
#endif
