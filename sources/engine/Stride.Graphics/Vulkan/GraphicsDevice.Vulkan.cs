// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.EXT;
using Silk.NET.Vulkan.Extensions.KHR;
using Stride.Core.Threading;

using VK = Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;

namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        internal int ConstantBufferDataPlacementAlignment;

        internal readonly ConcurrentPool<List<VK.DescriptorPool>> DescriptorPoolLists = new ConcurrentPool<List<VK.DescriptorPool>>(() => new List<VK.DescriptorPool>());
        internal readonly ConcurrentPool<List<Texture>> StagingResourceLists = new ConcurrentPool<List<Texture>>(() => new List<Texture>());

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Vulkan;
        internal GraphicsProfile RequestedProfile;

        private bool simulateReset = false;
        private string rendererName;

        private VK.Device nativeDevice;
        internal VK.Queue NativeCommandQueue;
        internal object QueueLock = new object();

        internal ThreadLocal<VK.CommandPool> NativeCopyCommandPools;
        private NativeResourceCollector nativeResourceCollector;
        private GraphicsResourceLinkCollector graphicsResourceLinkCollector;

        private VK.Buffer nativeUploadBuffer;
        private VK.DeviceMemory nativeUploadBufferMemory;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferSize;
        private int nativeUploadBufferOffset;

        private Queue<KeyValuePair<long, VK.Fence>> nativeFences = new();
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

        internal Vk vk;
        internal Buffer EmptyTexelBufferInt, EmptyTexelBufferFloat;
        internal Texture EmptyTexture;

        internal VK.PhysicalDevice NativePhysicalDevice => Adapter.GetPhysicalDevice(IsDebugMode);

        internal VK.Instance NativeInstance => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstance;

        internal struct BufferInfo
        {
            public long FenceValue;

            public VK.Buffer Buffer;

            public VK.DeviceMemory Memory;

            public BufferInfo(long fenceValue, VK.Buffer buffer, VK.DeviceMemory memory)
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
        internal VK.Device NativeDevice
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
            ArgumentNullException.ThrowIfNull(commandLists);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, commandLists.Length);

            var fenceValue = NextFenceValue++;

            // Create a fence
            var fenceCreateInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
            vk.CreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, Fence>(fenceValue, fence));

            // Collect resources
            var commandBuffers = stackalloc VK.CommandBuffer[count];
            for (int i = 0; i < count; i++)
            {
                commandBuffers[i] = commandLists[i].NativeCommandBuffer;
                RecycleCommandListResources(commandLists[i], fenceValue);
            }

            // Submit commands
            var pipelineStageFlags = VK.PipelineStageFlags.BottomOfPipeBit;
            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new SubmitInfo
            {
                SType = StructureType.SubmitInfo,
                CommandBufferCount = (uint)count,
                PCommandBuffers = commandBuffers,
                WaitSemaphoreCount = presentSemaphore.Handle != 0 ? 1U : 0U,
                PWaitSemaphores = &presentSemaphoreCopy,
                PWaitDstStageMask = &pipelineStageFlags,
            };
            vk.QueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

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
            vk = GetApi();
            if (nativeDevice.Handle != 0)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            vk.GetPhysicalDeviceProperties(NativePhysicalDevice, out var physicalDeviceProperties);
            ConstantBufferDataPlacementAlignment = (int)physicalDeviceProperties.Limits.MinUniformBufferOffsetAlignment;
            TimestampFrequency = (long)(1.0e9 / physicalDeviceProperties.Limits.TimestampPeriod); // Resolution in nanoseconds

            RequestedProfile = graphicsProfiles.First();

            //TODO ? IsProfilingSupported = queueProperties[0].TimestampValidBits > 0;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            // TODO VULKAN
            // Create Vulkan device based on profile
            float queuePriorities = 0;
            var queueCreateInfo = new DeviceQueueCreateInfo
            {
                SType = StructureType.DeviceQueueCreateInfo,
                QueueFamilyIndex = 0,
                QueueCount = 1,
                PQueuePriorities = &queuePriorities,
            };

            var enabledFeature = new PhysicalDeviceFeatures
            {
                FillModeNonSolid = true,
                ShaderClipDistance = true,
                ShaderCullDistance = true,
                SamplerAnisotropy = true,
                DepthClamp = true,
            };

            uint extCount = 0;
            vk.EnumerateDeviceExtensionProperties(NativePhysicalDevice, (byte*)null, &extCount, null);
            Span<ExtensionProperties> extensionProperties = stackalloc ExtensionProperties[(int)extCount]; 
            vk.EnumerateDeviceExtensionProperties(NativePhysicalDevice, (byte*)null, &extCount, extensionProperties);
            
            var availableExtensionNames = new List<string>();
            var desiredExtensionNames = new List<string>();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                fixed (VK.ExtensionProperties* extensionPropertiesPtr = extensionProperties)
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
                // fixed yields null if array is empty or null
                fixed (void* fEnabledExtensionNames = enabledExtensionNames) {
                var deviceCreateInfo = new DeviceCreateInfo
                {
                    SType = StructureType.DeviceCreateInfo,
                    QueueCreateInfoCount = 1,
                    PQueueCreateInfos = &queueCreateInfo,
                    EnabledExtensionCount = (uint)enabledExtensionNames.Length,
                    PpEnabledExtensionNames = (byte**)fEnabledExtensionNames,
                    PEnabledFeatures = &enabledFeature,
                };

                    vk.CreateDevice(NativePhysicalDevice, &deviceCreateInfo, null, out nativeDevice);
                }
            }
            finally
            {
                foreach (var enabledExtensionName in enabledExtensionNames)
                {
                    Marshal.FreeHGlobal(enabledExtensionName);
                }
            }

            vk.GetDeviceQueue(nativeDevice, 0, 0, out NativeCommandQueue);

            NativeCopyCommandPools = new ThreadLocal<VK.CommandPool>(() =>
            {
                //// Prepare copy command list (start it closed, so that every new use start with a Reset)
                var commandPoolCreateInfo = new CommandPoolCreateInfo
                {
                    SType = StructureType.CommandPoolCreateInfo,
                    QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                    Flags = CommandPoolCreateFlags.ResetCommandBufferBit
                };

                vk.CreateCommandPool(NativeDevice, &commandPoolCreateInfo, null, out var result);
                return result;
            }, true);

            DescriptorPools = new HeapPool(this);

            nativeResourceCollector = new NativeResourceCollector(this);
            graphicsResourceLinkCollector = new GraphicsResourceLinkCollector(this);

            EmptyTexelBufferInt = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_UInt);
            EmptyTexelBufferFloat = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_Float);
            EmptyTexture = Texture.New2D(this, 1, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource);
        }

        internal unsafe IntPtr AllocateUploadBuffer(int size, out VK.Buffer resource, out int offset)
        {
            // TODO D3D12 thread safety, should we simply use locks?
            if (nativeUploadBuffer.Handle == 0 || nativeUploadBufferOffset + size > nativeUploadBufferSize)
            {
                if (nativeUploadBuffer.Handle != 0)
                {
                    vk.UnmapMemory(NativeDevice, nativeUploadBufferMemory);
                    Collect(nativeUploadBuffer);
                    Collect(nativeUploadBufferMemory);
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                nativeUploadBufferSize = Math.Max(4 * 1024 * 1024, size);

                var bufferCreateInfo = new BufferCreateInfo
                {
                    SType = StructureType.BufferCreateInfo,
                    Size = (ulong)nativeUploadBufferSize,
                    Flags = BufferCreateFlags.None,
                    Usage = BufferUsageFlags.TransferSrcBit,
                };
                vk.CreateBuffer(NativeDevice, &bufferCreateInfo, null, out nativeUploadBuffer);
                AllocateMemory(VK.MemoryPropertyFlags.HostVisibleBit | VK.MemoryPropertyFlags.HostCoherentBit);

                fixed (IntPtr* nativeUploadBufferStartPtr = &nativeUploadBufferStart)
                    vk.MapMemory(NativeDevice, nativeUploadBufferMemory, 0, (ulong)nativeUploadBufferSize, 0, (void**)nativeUploadBufferStartPtr);
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = nativeUploadBuffer;
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferStart + offset;
        }

        protected unsafe void AllocateMemory(VK.MemoryPropertyFlags memoryProperties)
        {
            vk.GetBufferMemoryRequirements(nativeDevice, nativeUploadBuffer, out var memoryRequirements);

            if (memoryRequirements.Size == 0)
                return;

            var allocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
            };

            vk.GetPhysicalDeviceMemoryProperties(NativePhysicalDevice, out var physicalDeviceMemoryProperties);
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

            vk.AllocateMemory(NativeDevice, &allocateInfo, null, out nativeUploadBufferMemory);
            vk.BindBufferMemory(NativeDevice, nativeUploadBuffer, nativeUploadBufferMemory, 0);
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
            vk.DeviceWaitIdle(nativeDevice);

            // Destroy all remaining fences
            GetCompletedValue();

            // Mark upload buffer for destruction
            if (nativeUploadBuffer.Handle != 0)
            {
                vk.UnmapMemory(NativeDevice, nativeUploadBufferMemory);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBuffer);
                nativeResourceCollector.Add(lastCompletedFence, nativeUploadBufferMemory);

                nativeUploadBuffer.Handle = 0;
                nativeUploadBufferMemory.Handle = 0;
            }

            // Release fenced resources
            nativeResourceCollector.Dispose();
            DescriptorPools.Dispose();

            foreach (var nativeCopyCommandPool in NativeCopyCommandPools.Values)
                vk.DestroyCommandPool(nativeDevice, nativeCopyCommandPool, null);
            NativeCopyCommandPools.Dispose();
            NativeCopyCommandPools = null;
            vk.DestroyDevice(nativeDevice, null);
        }

        internal void OnDestroyed()
        {
        }

        internal unsafe long ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            //if (nativeUploadBuffer != VK.Buffer.Null)
            //{
            //    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
            //    TemporaryResources.Enqueue(new BufferInfo(NextFenceValue, nativeUploadBuffer, nativeUploadBufferMemory));

            //    nativeUploadBuffer = VK.Buffer.Null;
            //    nativeUploadBufferMemory = VK.DeviceMemory.Null;
            //}

            var fenceValue = NextFenceValue++;

            // Create new fence
            var fenceCreateInfo = new FenceCreateInfo { SType = StructureType.FenceCreateInfo };
            vk.CreateFence(nativeDevice, &fenceCreateInfo, null, out var fence);
            nativeFences.Enqueue(new KeyValuePair<long, Fence>(fenceValue, fence));

            // Collect resources
            RecycleCommandListResources(commandList, fenceValue);

            // Submit commands
            var nativeCommandBufferCopy = commandList.NativeCommandBuffer;
            var pipelineStageFlags = VK.PipelineStageFlags.BottomOfPipeBit;

            var presentSemaphoreCopy = presentSemaphore;
            var submitInfo = new VK.SubmitInfo
            {
                SType = VK.StructureType.SubmitInfo,
                CommandBufferCount = 1,
                PCommandBuffers = &nativeCommandBufferCopy,
                WaitSemaphoreCount = presentSemaphore.Handle != 0 ? 1U : 0U,
                PWaitSemaphores = &presentSemaphoreCopy,
                PWaitDstStageMask = &pipelineStageFlags,
            };
            vk.QueueSubmit(NativeCommandQueue, 1, &submitInfo, fence);

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

                while (nativeFences.Count > 0 && vk.GetFenceStatus(NativeDevice, nativeFences.Peek().Value) == VK.Result.Success)
                {
                    var fence = nativeFences.Dequeue();
                    vk.DestroyFence(NativeDevice, fence.Value, null);
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

                    vk.WaitForFences(NativeDevice, 1, &fenceCopy, true, ulong.MaxValue);
                    vk.DestroyFence(NativeDevice, fence.Value, null);
                    lastCompletedFence = fenceValue;
                }
            }
        }

        private VK.Semaphore presentSemaphore;

        public unsafe VK.Semaphore GetNextPresentSemaphore()
        {
            var createInfo = new SemaphoreCreateInfo { SType = StructureType.SemaphoreCreateInfo };
            vk.CreateSemaphore(NativeDevice, &createInfo, null, out presentSemaphore);
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

    internal class GraphicsResourceLinkCollector(GraphicsDevice graphicsDevice) : TemporaryResourceCollector<GraphicsResourceLink>(graphicsDevice)
    {
        protected override void ReleaseObject(GraphicsResourceLink item)
        {
            item.ReferenceCount--;
        }
    }

    internal class NativeResourceCollector(GraphicsDevice graphicsDevice) : TemporaryResourceCollector<NativeResource>(graphicsDevice)
    {
        protected override void ReleaseObject(NativeResource item)
        {
            item.Destroy(GraphicsDevice);
        }
    }
    
    internal abstract class TemporaryResourceCollector<T>(GraphicsDevice graphicsDevice) : IDisposable
    {
        protected readonly GraphicsDevice GraphicsDevice = graphicsDevice;
        private readonly Queue<KeyValuePair<long, T>> items = new();

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
