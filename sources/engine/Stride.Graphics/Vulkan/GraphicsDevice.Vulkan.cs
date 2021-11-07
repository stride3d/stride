// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
//using Vortice.Vulkan;
//using static Vortice.Vulkan.Vulkan;

using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Stride.Core;
using Stride.Core.Threading;

using Vk = Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;


namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        internal int ConstantBufferDataPlacementAlignment;

        internal readonly ConcurrentPool<List<Vk.DescriptorPool>> DescriptorPoolLists = new ConcurrentPool<List<Vk.DescriptorPool>>(() => new List<Vk.DescriptorPool>());
        internal readonly ConcurrentPool<List<Texture>> StagingResourceLists = new ConcurrentPool<List<Texture>>(() => new List<Texture>());

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Vulkan;
        internal GraphicsProfile RequestedProfile;

        private bool simulateReset = false;
        private string rendererName;

        private Vk.Device nativeDevice;
        internal Vk.Queue NativeCommandQueue;
        internal object QueueLock = new object();

        internal ThreadLocal<Vk.CommandPool> NativeCopyCommandPools;
        private NativeResourceCollector nativeResourceCollector;
        private GraphicsResourceLinkCollector graphicsResourceLinkCollector;

        private Vk.Buffer nativeUploadBuffer;
        private Vk.DeviceMemory nativeUploadBufferMemory;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferSize;
        private int nativeUploadBufferOffset;

        private Queue<KeyValuePair<long, Vk.Fence>> nativeFences = new Queue<KeyValuePair<long, Vk.Fence>>();
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

        internal Vk.PhysicalDevice NativePhysicalDevice => Adapter.GetPhysicalDevice(IsDebugMode);

        internal Vk.Instance NativeInstance => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstance;

        internal struct BufferInfo
        {
            public long FenceValue;

            public Vk.Buffer Buffer;

            public Vk.DeviceMemory Memory;

            public BufferInfo(long fenceValue, Vk.Buffer buffer, Vk.DeviceMemory memory)
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
        internal Vk.Device NativeDevice
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
            var fenceCreateInfo = new Vk.FenceCreateInfo { SType = Vk.StructureType.FenceCreateInfo };
            GetApi().CreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, Vk.Fence>(fenceValue, fence));

            // Collect resources
            var commandBuffers = stackalloc Vk.CommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                commandBuffers[i] = commandLists[i].NativeCommandBuffer;
                RecycleCommandListResources(commandLists[i], fenceValue);
            }

            // Submit commands
            var pipelineStageFlags = Vk.PipelineStageFlags.PipelineStageBottomOfPipeBit;
            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new Vk.SubmitInfo
            {
                SType = Vk.StructureType.SubmitInfo,
                CommandBufferCount = (uint)count,
                PCommandBuffers = commandBuffers,
                WaitSemaphoreCount = presentSemaphore.Handle != 0 ? 1U : 0U,
                PWaitSemaphores = &presentSemaphoreCopy,
                PWaitDstStageMask = &pipelineStageFlags,
            };
            GetApi().QueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

            presentSemaphore.Handle = 0;
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
            if (nativeDevice.Handle != 0)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            GetApi().GetPhysicalDeviceProperties(NativePhysicalDevice, out var physicalDeviceProperties);
            ConstantBufferDataPlacementAlignment = (int)physicalDeviceProperties.Limits.MinUniformBufferOffsetAlignment;
            TimestampFrequency = (long)(1.0e9 / physicalDeviceProperties.Limits.TimestampPeriod); // Resolution in nanoseconds

            RequestedProfile = graphicsProfiles.First();

            QueueFamilyProperties[] queueProperties = null;
            fixed (QueueFamilyProperties* qp = queueProperties)
                GetApi().GetPhysicalDeviceQueueFamilyProperties(NativePhysicalDevice, null, qp);

            //IsProfilingSupported = queueProperties[0].TimestampValidBits > 0;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            // TODO VULKAN
            // Create Vulkan device based on profile
            float queuePriorities = 0;
            var queueCreateInfo = new Vk.DeviceQueueCreateInfo
            {
                SType = Vk.StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = 0,
                QueueCount = 1,
                PQueuePriorities = &queuePriorities,
            };

            var enabledFeature = new Vk.PhysicalDeviceFeatures
            {
                FillModeNonSolid = true,
                ShaderClipDistance = true,
                ShaderCullDistance = true,
                SamplerAnisotropy = true,
                DepthClamp = true,
            };

            ExtensionProperties[] extensionProperties = null; 
            fixed(ExtensionProperties* ep = extensionProperties)
                GetApi().EnumerateDeviceExtensionProperties(NativePhysicalDevice, "", null, ep);
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                fixed (Vk.ExtensionProperties* extensionPropertiesPtr = extensionProperties)
                {
                    var namePointer = new IntPtr(extensionPropertiesPtr[index].ExtensionName);
                    var name = Marshal.PtrToStringAnsi(namePointer);
                    availableExtensionNames.Add(name);
                }
            }

            desiredExtensionNames.Add(KhrSwapchain.ExtensionName);
            if (!availableExtensionNames.Contains(KhrSwapchain.ExtensionName))
                throw new InvalidOperationException();

            if (availableExtensionNames.Contains(ExtDebugMarker.ExtensionName) && IsDebugMode)
            {
                desiredExtensionNames.Add(ExtDebugMarker.ExtensionName);
                IsProfilingSupported = true;
            }

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                var deviceCreateInfo = new Vk.DeviceCreateInfo
                {
                    SType = Vk.StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = 1,
                    PQueueCreateInfos = &queueCreateInfo,
                    EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                    PpEnabledExtensionNames = enabledExtensionNames.Length > 0 ? (byte**)Core.Interop.Fixed(enabledExtensionNames) : null,
                    PEnabledFeatures = &enabledFeature,
                };

                GetApi().CreateDevice(NativePhysicalDevice, &deviceCreateInfo, null, out nativeDevice);
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }
            }

            GetApi().GetDeviceQueue(nativeDevice, 0, 0, out NativeCommandQueue);

            NativeCopyCommandPools = new ThreadLocal<Vk.CommandPool>(() =>
            {
                //// Prepare copy command list (start it closed, so that every new use start with a Reset)
                var commandPoolCreateInfo = new Vk.CommandPoolCreateInfo
                {
                    SType = Vk.StructureType.CommandPoolCreateInfo,
                    QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                    Flags = Vk.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit
                };

                GetApi().CreateCommandPool(NativeDevice, &commandPoolCreateInfo, null, out var result);
                return result;
            }, true);

            DescriptorPools = new HeapPool(this);

            nativeResourceCollector = new NativeResourceCollector(this);
            graphicsResourceLinkCollector = new GraphicsResourceLinkCollector(this);

            EmptyTexelBufferInt = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_UInt);
            EmptyTexelBufferFloat = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_Float);
            EmptyTexture = Texture.New2D(this, 1, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource);
        }

        internal unsafe IntPtr AllocateUploadBuffer(int size, out Vk.Buffer resource, out int offset)
        {
            // TODO D3D12 thread safety, should we simply use locks?
            if (nativeUploadBuffer.Handle == 0 || nativeUploadBufferOffset + size > nativeUploadBufferSize)
            {
                if (nativeUploadBuffer.Handle != 0)
                {
                    GetApi().UnmapMemory(NativeDevice, nativeUploadBufferMemory);
                    Collect(nativeUploadBuffer);
                    Collect(nativeUploadBufferMemory);
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                nativeUploadBufferSize = Math.Max(4 * 1024 * 1024, size);

                var bufferCreateInfo = new Vk.BufferCreateInfo
                {
                    SType = Vk.StructureType.BufferCreateInfo,
                    Size = (ulong)nativeUploadBufferSize,
                    Flags = 0,
                    Usage = Vk.BufferUsageFlags.BufferUsageTransferSrcBit,
                };
                GetApi().CreateBuffer(NativeDevice, &bufferCreateInfo, null, out nativeUploadBuffer);
                AllocateMemory(Vk.MemoryPropertyFlags.MemoryPropertyHostVisibleBit | Vk.MemoryPropertyFlags.MemoryPropertyHostCoherentBit);

                fixed (IntPtr* nativeUploadBufferStartPtr = &nativeUploadBufferStart)
                    GetApi().MapMemory(NativeDevice, nativeUploadBufferMemory, 0, (ulong)nativeUploadBufferSize, 0, (void**)nativeUploadBufferStartPtr);
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = nativeUploadBuffer;
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferStart + offset;
        }

        protected unsafe void AllocateMemory(Vk.MemoryPropertyFlags memoryProperties)
        {
            GetApi().GetBufferMemoryRequirements(nativeDevice, nativeUploadBuffer, out var memoryRequirements);

            if (memoryRequirements.Size == 0)
                return;

            var allocateInfo = new Vk.MemoryAllocateInfo
            {
                SType = Vk.StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
            };

            GetApi().GetPhysicalDeviceMemoryProperties(NativePhysicalDevice, out var physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.MemoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *(&physicalDeviceMemoryProperties.MemoryTypes.Element0 + i);
                    if ((memoryType.PropertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.MemoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            GetApi().AllocateMemory(NativeDevice, &allocateInfo, null, out nativeUploadBufferMemory);
            GetApi().BindBufferMemory(NativeDevice, nativeUploadBuffer, nativeUploadBufferMemory, 0);
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
            GetApi().DeviceWaitIdle(nativeDevice);

            // Destroy all remaining fences
            GetCompletedValue();

            // Mark upload buffer for destruction
            if (nativeUploadBuffer.Handle != 0)
            {
                GetApi().UnmapMemory(NativeDevice, nativeUploadBufferMemory);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBuffer);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBufferMemory);

                nativeUploadBuffer.Handle = 0;
                nativeUploadBufferMemory.Handle = 0;
            }

            // Release fenced resources
            nativeResourceCollector.Dispose();
            DescriptorPools.Dispose();

            foreach (var nativeCopyCommandPool in NativeCopyCommandPools.Values)
                GetApi().DestroyCommandPool(nativeDevice, nativeCopyCommandPool, null);
            NativeCopyCommandPools.Dispose();
            NativeCopyCommandPools = null;
            GetApi().DestroyDevice(nativeDevice, null);
        }

        internal void OnDestroyed()
        {
        }

        internal unsafe long ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            //if (nativeUploadBuffer != Vk.Buffer.Null)
            //{
            //    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
            //    TemporaryResources.Enqueue(new BufferInfo(NextFenceValue, nativeUploadBuffer, nativeUploadBufferMemory));

            //    nativeUploadBuffer = Vk.Buffer.Null;
            //    nativeUploadBufferMemory = Vk.DeviceMemory.Null;
            //}

            var fenceValue = NextFenceValue++;

            // Create new fence
            var fenceCreateInfo = new Vk.FenceCreateInfo { SType = Vk.StructureType.FenceCreateInfo };
            GetApi().CreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, Vk.Fence>(fenceValue, fence));

            // Collect resources
            RecycleCommandListResources(commandList, fenceValue);

            // Submit commands
            var nativeCommandBufferCopy = commandList.NativeCommandBuffer;
            var pipelineStageFlags = Vk.PipelineStageFlags.PipelineStageBottomOfPipeBit;

            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new Vk.SubmitInfo
            {
                SType = Vk.StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &nativeCommandBufferCopy,
                WaitSemaphoreCount = presentSemaphore.Handle != 0 ? 1U : 0U,
                PWaitSemaphores = &presentSemaphoreCopy,
                PWaitDstStageMask = &pipelineStageFlags,
            };
            GetApi().QueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

            presentSemaphore.Handle = 0;
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

                while (nativeFences.Count > 0 && GetApi().GetFenceStatus(NativeDevice, nativeFences.Peek().Value) == Vk.Result.Success)
                {
                    var fence = nativeFences.Dequeue();
                    GetApi().DestroyFence(NativeDevice, fence.Value, null);
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

                    GetApi().WaitForFences(NativeDevice, 1, &fenceCopy, true, ulong.MaxValue);
                    GetApi().DestroyFence(NativeDevice, fence.Value, null);
                    lastCompletedFence = fenceValue;
                }
            }
        }

        private Vk.Semaphore presentSemaphore;

        public unsafe Vk.Semaphore GetNextPresentSemaphore()
        {
            var createInfo = new Vk.SemaphoreCreateInfo { SType = Vk.StructureType.SemaphoreCreateInfo };
            GetApi().CreateSemaphore(NativeDevice, &createInfo, null, out presentSemaphore);
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

    internal class CommandBufferPool : ResourcePool<Vk.CommandBuffer>
    {
        private readonly Vk.CommandPool commandPool;

        public unsafe CommandBufferPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            var commandPoolCreateInfo = new Vk.CommandPoolCreateInfo
            {
                SType = Vk.StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                Flags = Vk.CommandPoolCreateFlags.CommandPoolCreateResetCommandBufferBit
            };

            GetApi().CreateCommandPool(graphicsDevice.NativeDevice, &commandPoolCreateInfo, null, out commandPool);
        }

        protected override unsafe Vk.CommandBuffer CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var commandBufferAllocationInfo = new Vk.CommandBufferAllocateInfo
            {
                SType = Vk.StructureType.CommandBufferAllocateInfo,
                Level = Vk.CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };

            Vk.CommandBuffer commandBuffer;
            GetApi().AllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);
            return commandBuffer;
        }

        protected override void ResetObject(Vk.CommandBuffer obj)
        {
            GetApi().ResetCommandBuffer(obj, Vk.CommandBufferResetFlags.CommandBufferResetReleaseResourcesBit);
        }

        protected override unsafe void Destroy()
        {
            base.Destroy();

            GetApi().DestroyCommandPool(GraphicsDevice.NativeDevice, commandPool, null);
        }
    }

    internal class HeapPool : ResourcePool<Vk.DescriptorPool>
    {
        public HeapPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        protected override unsafe Vk.DescriptorPool CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var poolSizes = GraphicsDevice.MaxDescriptorTypeCounts
                .Select((count, index) => new Vk.DescriptorPoolSize { Type = (Vk.DescriptorType)index, DescriptorCount = count })
                .Where(size => size.DescriptorCount > 0)
                .ToArray();

            var descriptorPoolCreateInfo = new Vk.DescriptorPoolCreateInfo
            {
                SType = Vk.StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = (Vk.DescriptorPoolSize*)Core.Interop.Fixed(poolSizes),
                MaxSets = GraphicsDevice.MaxDescriptorSetCount,
            };
            GetApi().CreateDescriptorPool(GraphicsDevice.NativeDevice, &descriptorPoolCreateInfo, null, out var descriptorPool);
            return descriptorPool;
        }

        protected override void ResetObject(Vk.DescriptorPool obj)
        {
            GetApi().ResetDescriptorPool(GraphicsDevice.NativeDevice, obj, 0);
        }

        protected override unsafe void DestroyObject(Vk.DescriptorPool obj)
        {
            GetApi().DestroyDescriptorPool(GraphicsDevice.NativeDevice, obj, null);
        }
    }

    internal struct NativeResource
    {
        public Vk.DebugReportObjectTypeEXT type;

        public ulong handle;

        public NativeResource(Vk.DebugReportObjectTypeEXT type, ulong handle)
        {
            this.type = type;
            this.handle = handle;
        }

        public static unsafe implicit operator NativeResource(Vk.Buffer handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeBufferExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.BufferView handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeBufferViewExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.Image handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeImageExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.ImageView handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeImageViewExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.DeviceMemory handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeDeviceMemoryExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.Sampler handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeSamplerExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.Framebuffer handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeFramebufferExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.Semaphore handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeSemaphoreExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.Fence handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeFenceExt, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Vk.QueryPool handle)
        {
            return new NativeResource(Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeQueryPoolExt, *(ulong*)&handle);
        }

        public unsafe void Destroy(GraphicsDevice device)
        {
            var handleCopy = handle;

            switch (type)
            {
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeBufferExt:
                    GetApi().DestroyBuffer(device.NativeDevice, *(Vk.Buffer*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeBufferViewExt:
                    GetApi().DestroyBufferView(device.NativeDevice, *(Vk.BufferView*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeImageExt:
                    GetApi().DestroyImage(device.NativeDevice, *(Vk.Image*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeImageViewExt:
                    GetApi().DestroyImageView(device.NativeDevice, *(Vk.ImageView*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeDeviceMemoryExt:
                    GetApi().FreeMemory(device.NativeDevice, *(Vk.DeviceMemory*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeSamplerExt:
                    GetApi().DestroySampler(device.NativeDevice, *(Vk.Sampler*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeFramebufferExt:
                    GetApi().DestroyFramebuffer(device.NativeDevice, *(Vk.Framebuffer*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeSemaphoreExt:
                    GetApi().DestroySemaphore(device.NativeDevice, *(Vk.Semaphore*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeFenceExt:
                    GetApi().DestroyFence(device.NativeDevice, *(Vk.Fence*)&handleCopy, null);
                    break;
                case Vk.DebugReportObjectTypeEXT.DebugReportObjectTypeQueryPoolExt:
                    GetApi().DestroyQueryPool(device.NativeDevice, *(Vk.QueryPool*)&handleCopy, null);
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
