// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using SharpVulkan;
using System.Collections.Concurrent;

using Xenko.Core;
using Xenko.Core.Threading;
using Semaphore = SharpVulkan.Semaphore;

namespace Xenko.Graphics
{
    public partial class GraphicsDevice
    {
        internal int ConstantBufferDataPlacementAlignment;

        internal readonly ConcurrentPool<List<SharpVulkan.DescriptorPool>> DescriptorPoolLists = new ConcurrentPool<List<SharpVulkan.DescriptorPool>>(() => new List<SharpVulkan.DescriptorPool>());
        internal readonly ConcurrentPool<List<Texture>> StagingResourceLists = new ConcurrentPool<List<Texture>>(() => new List<Texture>());

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Vulkan;
        internal GraphicsProfile RequestedProfile;

        private bool simulateReset = false;
        private string rendererName;

        private Device nativeDevice;
        internal Queue NativeCommandQueue;
        internal object QueueLock = new object();
        internal object PresentLock = new object();

        internal CommandPool NativeCopyCommandPool;
        internal CommandBuffer NativeCopyCommandBuffer;
        private NativeResourceCollector nativeResourceCollector;
        private GraphicsResourceLinkCollector graphicsResourceLinkCollector;

        private SharpVulkan.Buffer nativeUploadBuffer;
        private DeviceMemory nativeUploadBufferMemory;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferSize;
        private int nativeUploadBufferOffset;

        private ConcurrentQueue<KeyValuePair<long, Fence>> nativeFences = new ConcurrentQueue<KeyValuePair<long, Fence>>();
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

        internal Buffer EmptyTexelBuffer;

        internal PhysicalDevice NativePhysicalDevice => Adapter.GetPhysicalDevice(IsDebugMode);

        internal Instance NativeInstance => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstance;

        internal struct BufferInfo
        {
            public long FenceValue;

            public SharpVulkan.Buffer Buffer;

            public DeviceMemory Memory;

            public BufferInfo(long fenceValue, SharpVulkan.Buffer buffer, DeviceMemory memory)
            {
                FenceValue = fenceValue;
                Buffer = buffer;
                Memory = memory;
            }
        }

        /// <summary>
        /// A minimal 1x1 texture used while bigger textures are loading.
        /// </summary>
        public Texture dummyTexture;

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
        internal Device NativeDevice
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
            var fenceCreateInfo = new FenceCreateInfo { StructureType = StructureType.FenceCreateInfo };
            var fence = nativeDevice.CreateFence(ref fenceCreateInfo);
            nativeFences.Enqueue(new KeyValuePair<long, Fence>(fenceValue, fence));

            // Collect resources
            var commandBuffers = stackalloc CommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                commandBuffers[i] = commandLists[i].NativeCommandBuffer;
                RecycleCommandListResources(commandLists[i], fenceValue);
            }

            // Submit commands
            var pipelineStageFlags = PipelineStageFlags.BottomOfPipe;
            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new SubmitInfo
            {
                StructureType = StructureType.SubmitInfo,
                CommandBufferCount = (uint)count,
                CommandBuffers = new IntPtr(commandBuffers),
                WaitSemaphoreCount = presentSemaphore != Semaphore.Null ? 1U : 0U,
                WaitSemaphores = new IntPtr(&presentSemaphoreCopy),
                WaitDstStageMask = new IntPtr(&pipelineStageFlags),
            };
            lock (PresentLock) {
                NativeCommandQueue.Submit(1, &submitInfo, fence);
            }

            presentSemaphore = Semaphore.Null;
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
            if (nativeDevice != Device.Null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;
            SharpVulkan.ResultExtensions.SuppressValidationExceptions = (deviceCreationFlags & DeviceCreationFlags.NoValidationExceptions) != 0;

            PhysicalDeviceProperties physicalDeviceProperties;
            NativePhysicalDevice.GetProperties(out physicalDeviceProperties);
            ConstantBufferDataPlacementAlignment = (int)physicalDeviceProperties.Limits.MinUniformBufferOffsetAlignment;
            TimestampFrequency = (long)(1.0e9 / physicalDeviceProperties.Limits.TimestampPeriod); // Resolution in nanoseconds

            RequestedProfile = graphicsProfiles.Last();

            var queueProperties = NativePhysicalDevice.QueueFamilyProperties;
            //IsProfilingSupported = queueProperties[0].TimestampValidBits > 0;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            // TODO VULKAN
            // Create Vulkan device based on profile
            uint queuePriorities = 0;
            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                StructureType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = 0,
                QueueCount = 1,
                QueuePriorities = new IntPtr(&queuePriorities)
            };

            var enabledFeature = new PhysicalDeviceFeatures
            {
                FillModeNonSolid = true,
                ShaderClipDistance = true,
                ShaderCullDistance = true,
                SamplerAnisotropy = true,
                DepthClamp = true,
            };

            var extensionProperties = NativePhysicalDevice.GetDeviceExtensionProperties();
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var namePointer = new IntPtr(Interop.Fixed(ref extensionProperties[index].ExtensionName));
                var name = Marshal.PtrToStringAnsi(namePointer);
                availableExtensionNames.Add(name);
            }

            desiredExtensionNames.Add("VK_KHR_swapchain");
            if (!availableExtensionNames.Contains("VK_KHR_swapchain"))
                throw new InvalidOperationException();

            if (availableExtensionNames.Contains("VK_EXT_debug_marker") && IsDebugMode)
            {
                desiredExtensionNames.Add("VK_EXT_debug_marker");
                IsProfilingSupported = true;
            }

            var enabledExtensionNames = desiredExtensionNames.Select(Marshal.StringToHGlobalAnsi).ToArray();

            try
            {
                var deviceCreateInfo = new DeviceCreateInfo
                {
                    StructureType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = 1,
                    QueueCreateInfos = new IntPtr(&queueCreateInfo),
                    EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                    EnabledExtensionNames = enabledExtensionNames.Length > 0 ? new IntPtr(Interop.Fixed(enabledExtensionNames)) : IntPtr.Zero,
                    EnabledFeatures = new IntPtr(&enabledFeature)
                };

                nativeDevice = NativePhysicalDevice.CreateDevice(ref deviceCreateInfo);
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }
            }

            NativeCommandQueue = nativeDevice.GetQueue(0, 0);

            //// Prepare copy command list (start it closed, so that every new use start with a Reset)
            var commandPoolCreateInfo = new CommandPoolCreateInfo
            {
                StructureType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                Flags = CommandPoolCreateFlags.ResetCommandBuffer
            };
            NativeCopyCommandPool = NativeDevice.CreateCommandPool(ref commandPoolCreateInfo);

            var commandBufferAllocationInfo = new CommandBufferAllocateInfo
            {
                StructureType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = NativeCopyCommandPool,
                CommandBufferCount = 1
            };
            CommandBuffer nativeCommandBuffer;
            NativeDevice.AllocateCommandBuffers(ref commandBufferAllocationInfo, &nativeCommandBuffer);
            NativeCopyCommandBuffer = nativeCommandBuffer;

            DescriptorPools = new HeapPool(this);

            nativeResourceCollector = new NativeResourceCollector(this);
            graphicsResourceLinkCollector = new GraphicsResourceLinkCollector(this);

            EmptyTexelBuffer = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_Float);

            dummyTexture = Texture.New2D(this, 1, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        }
        internal unsafe IntPtr AllocateUploadBuffer(int size, out SharpVulkan.Buffer resource, out int offset)
        {
            // TODO D3D12 thread safety, should we simply use locks?
            if (nativeUploadBuffer == SharpVulkan.Buffer.Null || nativeUploadBufferOffset + size > nativeUploadBufferSize)
            {
                if (nativeUploadBuffer != SharpVulkan.Buffer.Null)
                {
                    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
                    Collect(nativeUploadBuffer);
                    Collect(nativeUploadBufferMemory);
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                nativeUploadBufferSize = Math.Max(4 * 1024 * 1024, size);

                var bufferCreateInfo = new BufferCreateInfo
                {
                    StructureType = StructureType.BufferCreateInfo,
                    Size = (ulong)nativeUploadBufferSize,
                    Flags = BufferCreateFlags.None,
                    Usage = BufferUsageFlags.TransferSource,
                };
                nativeUploadBuffer = NativeDevice.CreateBuffer(ref bufferCreateInfo);
                AllocateMemory(MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent);

                nativeUploadBufferStart = NativeDevice.MapMemory(nativeUploadBufferMemory, 0, (ulong)nativeUploadBufferSize, MemoryMapFlags.None);
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = nativeUploadBuffer;
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferStart + offset;
        }

        protected unsafe void AllocateMemory(MemoryPropertyFlags memoryProperties)
        {
            MemoryRequirements memoryRequirements;
            NativeDevice.GetBufferMemoryRequirements(nativeUploadBuffer, out memoryRequirements);

            if (memoryRequirements.Size == 0)
                return;

            var allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
            };

            PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
            NativePhysicalDevice.GetMemoryProperties(out physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.MemoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *((MemoryType*)&physicalDeviceMemoryProperties.MemoryTypes + i);
                    if ((memoryType.PropertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.MemoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            nativeUploadBufferMemory = NativeDevice.AllocateMemory(ref allocateInfo);

            NativeDevice.BindBufferMemory(nativeUploadBuffer, nativeUploadBufferMemory, 0);
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
            EmptyTexelBuffer.Dispose();
            EmptyTexelBuffer = null;

            // Wait for all queues to be idle
            nativeDevice.WaitIdle();

            // Destroy all remaining fences
            GetCompletedValue();

            // Mark upload buffer for destruction
            if (nativeUploadBuffer != SharpVulkan.Buffer.Null)
            {
                NativeDevice.UnmapMemory(nativeUploadBufferMemory);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBuffer);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBufferMemory);

                nativeUploadBuffer = SharpVulkan.Buffer.Null;
                nativeUploadBufferMemory = DeviceMemory.Null;
            }

            // Release fenced resources
            nativeResourceCollector.Dispose();
            DescriptorPools.Dispose();
            if( dummyTexture != null ) dummyTexture.Dispose();

            nativeDevice.DestroyCommandPool(NativeCopyCommandPool);
            nativeDevice.Destroy();
        }

        internal void OnDestroyed()
        {
        }

        internal unsafe long ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            //if (nativeUploadBuffer != SharpVulkan.Buffer.Null)
            //{
            //    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
            //    TemporaryResources.Enqueue(new BufferInfo(NextFenceValue, nativeUploadBuffer, nativeUploadBufferMemory));

            //    nativeUploadBuffer = SharpVulkan.Buffer.Null;
            //    nativeUploadBufferMemory = DeviceMemory.Null;
            //}

            var fenceValue = NextFenceValue++;

            // Create new fence
            var fenceCreateInfo = new FenceCreateInfo { StructureType = StructureType.FenceCreateInfo };
            var fence = nativeDevice.CreateFence(ref fenceCreateInfo);
            nativeFences.Enqueue(new KeyValuePair<long, Fence>(fenceValue, fence));

            // Collect resources
            RecycleCommandListResources(commandList, fenceValue);

            // Submit commands
            var nativeCommandBufferCopy = commandList.NativeCommandBuffer;
            var pipelineStageFlags = PipelineStageFlags.BottomOfPipe;

            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new SubmitInfo
            {
                StructureType = StructureType.SubmitInfo,
                CommandBufferCount = 1,
                CommandBuffers = new IntPtr(&nativeCommandBufferCopy),
                WaitSemaphoreCount = presentSemaphore != Semaphore.Null ? 1U : 0U,
                WaitSemaphores = new IntPtr(&presentSemaphoreCopy),
                WaitDstStageMask = new IntPtr(&pipelineStageFlags),
            };
            lock (PresentLock) {
                NativeCommandQueue.Submit(1, &submitInfo, fence);
            }

            presentSemaphore = Semaphore.Null;
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

        internal unsafe long GetCompletedValue()
        {
            while (nativeFences.TryDequeue(out KeyValuePair<long, Fence> fence)) {
                if (NativeDevice.GetFenceStatus(fence.Value) == Result.Success) {
                    NativeDevice.DestroyFence(fence.Value);
                    lastCompletedFence = Math.Max(lastCompletedFence, fence.Key);
                } else {
                    // not ready, put back
                    nativeFences.Enqueue(fence);
                    break;
                }
            }

            return lastCompletedFence;
        }

        internal unsafe void WaitForFenceInternal(long fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            while (nativeFences.TryDequeue(out KeyValuePair<long, Fence> fence)) {
                if (fence.Key <= fenceValue) {
                    var fenceCopy = fence.Value;
                    NativeDevice.WaitForFences(1, &fenceCopy, true, ulong.MaxValue);
                    NativeDevice.DestroyFence(fence.Value);
                    lastCompletedFence = fenceValue;
                } else {
                    // not ready, put it back
                    nativeFences.Enqueue(fence);
                    break;
                }
            }
        }

        private Semaphore presentSemaphore;

        public unsafe Semaphore GetNextPresentSemaphore()
        {
            var createInfo = new SemaphoreCreateInfo { StructureType = StructureType.SemaphoreCreateInfo };
            presentSemaphore = NativeDevice.CreateSemaphore(ref createInfo);
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
        private readonly ConcurrentQueue<KeyValuePair<long, T>> liveObjects = new ConcurrentQueue<KeyValuePair<long, T>>();

        protected ResourcePool(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public T GetObject()
        {
            // Check if first allocator is ready for reuse
            if (liveObjects.TryDequeue(out KeyValuePair<long, T> firstAllocator)) {
                if (firstAllocator.Key <= GraphicsDevice.GetCompletedValue()) {                    
                    ResetObject(firstAllocator.Value);
                    return firstAllocator.Value;
                }
                // not ready yet, put it back
                liveObjects.Enqueue(firstAllocator);
            }
            return CreateObject();
        }

        public void RecycleObject(long fenceValue, T obj)
        {
            liveObjects.Enqueue(new KeyValuePair<long, T>(fenceValue, obj));
        }

        protected abstract T CreateObject();

        protected abstract void ResetObject(T obj);

        protected virtual void DestroyObject(T obj)
        {
        }

        protected override void Destroy()
        {
            foreach (var item in liveObjects)
            {
                DestroyObject(item.Value);
            }

            base.Destroy();
        }
    }

    internal class CommandBufferPool : ResourcePool<CommandBuffer>
    {
        private readonly CommandPool commandPool;

        public unsafe CommandBufferPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            var commandPoolCreateInfo = new CommandPoolCreateInfo
            {
                StructureType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                Flags = CommandPoolCreateFlags.ResetCommandBuffer
            };

            commandPool = graphicsDevice.NativeDevice.CreateCommandPool(ref commandPoolCreateInfo);
        }

        protected override unsafe CommandBuffer CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var commandBufferAllocationInfo = new CommandBufferAllocateInfo
            {
                StructureType = StructureType.CommandBufferAllocateInfo,
                Level = CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };

            CommandBuffer commandBuffer;
            GraphicsDevice.NativeDevice.AllocateCommandBuffers(ref commandBufferAllocationInfo, &commandBuffer);
            return commandBuffer;
        }

        protected override void ResetObject(CommandBuffer obj)
        {
            obj.Reset(CommandBufferResetFlags.None);
        }

        protected override unsafe void Destroy()
        {
            base.Destroy();

            GraphicsDevice.NativeDevice.DestroyCommandPool(commandPool);
        }
    }

    internal class HeapPool : ResourcePool<SharpVulkan.DescriptorPool>
    {
        public HeapPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
        }

        protected override unsafe SharpVulkan.DescriptorPool CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var poolSizes = GraphicsDevice.MaxDescriptorTypeCounts
                .Select((count, index) => new DescriptorPoolSize { Type = (DescriptorType)index, DescriptorCount = count })
                .Where(size => size.DescriptorCount > 0)
                .ToArray();

            var descriptorPoolCreateInfo = new DescriptorPoolCreateInfo
            {
                StructureType = StructureType.DescriptorPoolCreateInfo,
                PoolSizeCount = (uint)poolSizes.Length,
                PoolSizes = new IntPtr(Interop.Fixed(poolSizes)),
                MaxSets = GraphicsDevice.MaxDescriptorSetCount,
            };
            return GraphicsDevice.NativeDevice.CreateDescriptorPool(ref descriptorPoolCreateInfo);
        }

        protected override void ResetObject(SharpVulkan.DescriptorPool obj)
        {
            GraphicsDevice.NativeDevice.ResetDescriptorPool(obj, DescriptorPoolResetFlags.None);
        }

        protected override unsafe void DestroyObject(SharpVulkan.DescriptorPool obj)
        {
            GraphicsDevice.NativeDevice.DestroyDescriptorPool(obj);
        }
    }

    internal struct NativeResource
    {
        public DebugReportObjectType type;

        public ulong handle;

        public NativeResource(DebugReportObjectType type, ulong handle)
        {
            this.type = type;
            this.handle = handle;
        }

        public static unsafe implicit operator NativeResource(SharpVulkan.Buffer handle)
        {
            return new NativeResource(DebugReportObjectType.Buffer, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(BufferView handle)
        {
            return new NativeResource(DebugReportObjectType.BufferView, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(SharpVulkan.Image handle)
        {
            return new NativeResource(DebugReportObjectType.Image, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(ImageView handle)
        {
            return new NativeResource(DebugReportObjectType.ImageView, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(DeviceMemory handle)
        {
            return new NativeResource(DebugReportObjectType.DeviceMemory, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Sampler handle)
        {
            return new NativeResource(DebugReportObjectType.Sampler, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Framebuffer handle)
        {
            return new NativeResource(DebugReportObjectType.Framebuffer, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Semaphore handle)
        {
            return new NativeResource(DebugReportObjectType.Semaphore, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(Fence handle)
        {
            return new NativeResource(DebugReportObjectType.Fence, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(SharpVulkan.QueryPool handle)
        {
            return new NativeResource(DebugReportObjectType.QueryPool, *(ulong*)&handle);
        }

        public unsafe void Destroy(GraphicsDevice device)
        {
            var handleCopy = handle;

            switch (type)
            {
                case DebugReportObjectType.Buffer:
                    device.NativeDevice.DestroyBuffer(*(SharpVulkan.Buffer*)&handleCopy);
                    break;
                case DebugReportObjectType.BufferView:
                    device.NativeDevice.DestroyBufferView(*(BufferView*)&handleCopy);
                    break;
                case DebugReportObjectType.Image:
                    device.NativeDevice.DestroyImage(*(SharpVulkan.Image*)&handleCopy);
                    break;
                case DebugReportObjectType.ImageView:
                    device.NativeDevice.DestroyImageView(*(ImageView*)&handleCopy);
                    break;
                case DebugReportObjectType.DeviceMemory:
                    device.NativeDevice.FreeMemory(*(DeviceMemory*)&handleCopy);
                    break;
                case DebugReportObjectType.Sampler:
                    device.NativeDevice.DestroySampler(*(Sampler*)&handleCopy);
                    break;
                case DebugReportObjectType.Framebuffer:
                    device.NativeDevice.DestroyFramebuffer(*(Framebuffer*)&handleCopy);
                    break;
                case DebugReportObjectType.Semaphore:
                    device.NativeDevice.DestroySemaphore(*(Semaphore*)&handleCopy);
                    break;
                case DebugReportObjectType.Fence:
                    device.NativeDevice.DestroyFence(*(Fence*)&handleCopy);
                    break;
                case DebugReportObjectType.QueryPool:
                    device.NativeDevice.DestroyQueryPool(*(SharpVulkan.QueryPool*)&handleCopy);
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
        private readonly ConcurrentQueue<KeyValuePair<long, T>> items = new ConcurrentQueue<KeyValuePair<long, T>>();

        protected TemporaryResourceCollector(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public void Add(long fenceValue, T item)
        {
            items.Enqueue(new KeyValuePair<long, T>(fenceValue, item));
        }

        public void Release()
        {
            while (items.TryDequeue(out KeyValuePair<long, T> item))
            {
                if (GraphicsDevice.IsFenceCompleteInternal(item.Key)) {
                    ReleaseObject(item.Value);
                } else {
                    // not ready, put it back
                    items.Enqueue(item);
                    break;
                }
            }
        }

        protected abstract void ReleaseObject(T item);

        public void Dispose()
        {
            while (items.TryDequeue(out KeyValuePair<long, T> item))
            {
                ReleaseObject(item.Value);
            }
        }
    }
}
#endif
