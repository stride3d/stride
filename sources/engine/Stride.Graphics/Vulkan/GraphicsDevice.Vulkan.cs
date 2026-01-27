// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Stride.Core;
using Stride.Core.Threading;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

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
        private VkDeviceApi nativeDeviceApi;
        internal VkQueue NativeCommandQueue;
        internal object QueueLock = new object();

        internal ThreadLocal<CommandBufferPool> NativeCopyCommandPools;
        private NativeResourceCollector nativeResourceCollector;
        private GraphicsResourceLinkCollector graphicsResourceLinkCollector;

        private VkBuffer nativeUploadBuffer;
        private VkDeviceMemory nativeUploadBufferMemory;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferSize;
        private int nativeUploadBufferOffset;
        private object nativeUploadBufferLock = new();

        internal FenceHelper FrameFence;
        internal FenceHelper CommandListFence;
        internal FenceHelper CopyFence;
        internal ulong LastGPUSyncCopyFenceToCommandFence;

        internal HeapPool DescriptorPools;
        internal const uint MaxDescriptorSetCount = 256;
        internal readonly uint[] MaxDescriptorTypeCounts =
        [
            256, // Sampler
            0, // CombinedImageSampler
            512, // SampledImage
            64, // StorageImage
            64, // UniformTexelBuffer
            64, // StorageTexelBuffer
            512, // UniformBuffer
            64, // StorageBuffer
            0, // UniformBufferDynamic
            0, // StorageBufferDynamic
            0 // InputAttachment
        ];

        internal Buffer EmptyTexelBufferInt, EmptyTexelBufferFloat;
        internal Texture EmptyTexture;

        internal VkPhysicalDevice NativePhysicalDevice => Adapter.GetPhysicalDevice(IsDebugMode);

        internal VkInstance NativeInstance => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstance;
        internal VkInstanceApi NativeInstanceApi => GraphicsAdapterFactory.GetInstance(IsDebugMode).NativeInstanceApi;

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
        ///     Gets the native device API.
        /// </summary>
        /// <value>The native device API.</value>
        internal VkDeviceApi NativeDeviceApi
        {
            get { return nativeDeviceApi; }
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
        ///   Enables or disables profiling.
        /// </summary>
        /// <param name="enabledFlag"><see langword="true"/> to enable profiling; <see langword="false"/> to disable it.</param>
        public void EnableProfile(bool enabledFlag)
        {
        }

        /// <summary>
        ///     Unmarks context as active on the current thread.
        /// </summary>
        public unsafe void End()
        {
            lock (QueueLock)
            {
                // Add a dependency between command list fence and frame fence
                var commandListFenceValue = CommandListFence.NextFenceValue;
                var frameFenceValue = FrameFence.NextFenceValue++;

                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    waitSemaphoreValueCount = 1,
                    pWaitSemaphoreValues = &commandListFenceValue,
                    signalSemaphoreValueCount = 1,
                    pSignalSemaphoreValues = &frameFenceValue,
                };

                var commandListSemaphore = CommandListFence.Semaphore;
                var frameSemaphore = FrameFence.Semaphore;
                var pipelineStageFlags = VkPipelineStageFlags.BottomOfPipe;
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    waitSemaphoreCount = 1,
                    pWaitSemaphores = &commandListSemaphore,
                    pWaitDstStageMask = &pipelineStageFlags,
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &frameSemaphore,
                };

                CheckResult(NativeDeviceApi.vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, VkFence.Null));
            }
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

            var commandBuffers = stackalloc VkCommandBuffer[count];
            for (int i = 0; i < count; i++)
                commandBuffers[i] = commandLists[i].NativeCommandBuffer;

            ulong nextCommandListFenceValue;
            lock (QueueLock)
            {
                var commandListFenceValue = CommandListFence.NextFenceValue++;
                nextCommandListFenceValue = commandListFenceValue + 1;
                // Make sure all copies are done as well
                var copyFenceValue = CopyFence.NextFenceValue;
                var waitFenceValues = stackalloc ulong[] { commandListFenceValue, copyFenceValue };

                // Do we need to wait for CopyFence?
                var semaphoreCount = copyFenceValue > LastGPUSyncCopyFenceToCommandFence ? 2 : 1;
                // Remember that we waited 
                LastGPUSyncCopyFenceToCommandFence = copyFenceValue;

                // Submit commands
                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    waitSemaphoreValueCount = 2,
                    pWaitSemaphoreValues = &waitFenceValues[0],
                    signalSemaphoreValueCount = 1,
                    pSignalSemaphoreValues = &nextCommandListFenceValue,
                };

                var semaphores = stackalloc VkSemaphore[] { CommandListFence.Semaphore, CopyFence.Semaphore };
                var pipelineStageFlags = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.BottomOfPipe };
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    commandBufferCount = (uint)count,
                    pCommandBuffers = commandBuffers,
                    waitSemaphoreCount = 2,
                    pWaitSemaphores = &semaphores[0],
                    pWaitDstStageMask = &pipelineStageFlags[0],
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &semaphores[0],
                };

                CheckResult(NativeDeviceApi.vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, VkFence.Null));
            }

            // Collect resources
            for (int i = 0; i < count; i++)
            {
                RecycleCommandListResources(commandLists[i], nextCommandListFenceValue);
            }

            nativeResourceCollector.Release();
            graphicsResourceLinkCollector.Release();
        }

        internal void CheckResult(VkResult vkResult, [CallerArgumentExpression("vkResult")] string call = null)
        {
            if (vkResult != VkResult.Success)
                throw new InvalidOperationException($"Vulkan call {call} returned {vkResult}");
        }

        /// <summary>
        ///   Initializes the platform-specific features of the Graphics Device once it has been fully initialized.
        /// </summary>
        private unsafe partial void InitializePostFeatures()
        {
        }

        private partial string GetRendererName() => rendererName;

        public void SimulateReset()
        {
            simulateReset = true;
        }

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private unsafe partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice != VkDevice.Null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.Description;

            NativeInstanceApi.vkGetPhysicalDeviceProperties(NativePhysicalDevice, out var physicalDeviceProperties);
            ConstantBufferDataPlacementAlignment = (int)physicalDeviceProperties.limits.minUniformBufferOffsetAlignment;
            TimestampFrequency = (long)(1.0e9 / physicalDeviceProperties.limits.timestampPeriod); // Resolution in nanoseconds

            // Configure descriptor type max counts
            void SetMaxDescriptorTypeCount(VkDescriptorType type, uint limit)
                => MaxDescriptorTypeCounts[(int)type] = Math.Min(MaxDescriptorTypeCounts[(int)type], limit);

            SetMaxDescriptorTypeCount(VkDescriptorType.Sampler, physicalDeviceProperties.limits.maxDescriptorSetSamplers);
            SetMaxDescriptorTypeCount(VkDescriptorType.CombinedImageSampler, 0); // Not defined.
            SetMaxDescriptorTypeCount(VkDescriptorType.SampledImage, physicalDeviceProperties.limits.maxDescriptorSetSampledImages);
            SetMaxDescriptorTypeCount(VkDescriptorType.StorageImage, physicalDeviceProperties.limits.maxDescriptorSetStorageImages);
            SetMaxDescriptorTypeCount(VkDescriptorType.UniformTexelBuffer, physicalDeviceProperties.limits.maxDescriptorSetSampledImages); // No individual limit
            SetMaxDescriptorTypeCount(VkDescriptorType.StorageTexelBuffer, physicalDeviceProperties.limits.maxDescriptorSetStorageImages); // No individual limit
            SetMaxDescriptorTypeCount(VkDescriptorType.UniformBuffer, physicalDeviceProperties.limits.maxDescriptorSetUniformBuffers);
            SetMaxDescriptorTypeCount(VkDescriptorType.StorageBuffer, physicalDeviceProperties.limits.maxDescriptorSetStorageBuffers);
            SetMaxDescriptorTypeCount(VkDescriptorType.UniformBufferDynamic, physicalDeviceProperties.limits.maxDescriptorSetUniformBuffersDynamic);
            SetMaxDescriptorTypeCount(VkDescriptorType.StorageBufferDynamic, physicalDeviceProperties.limits.maxDescriptorSetStorageBuffersDynamic);
            SetMaxDescriptorTypeCount(VkDescriptorType.InputAttachment, physicalDeviceProperties.limits.maxDescriptorSetInputAttachments);

            RequestedProfile = graphicsProfiles.First();

            NativeInstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(NativePhysicalDevice, out uint queueFamilyCount);
            Span<VkQueueFamilyProperties> queueFamilies = stackalloc VkQueueFamilyProperties[(int)queueFamilyCount];
            NativeInstanceApi.vkGetPhysicalDeviceQueueFamilyProperties(NativePhysicalDevice, queueFamilies);
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

            NativeInstanceApi.vkGetPhysicalDeviceFeatures(NativePhysicalDevice, out var deviceFeatures);

            if (deviceFeatures.shaderStorageImageReadWithoutFormat)
            {
                enabledFeature.shaderStorageImageReadWithoutFormat = true;
            }

            if (deviceFeatures.shaderStorageImageWriteWithoutFormat)
            {
                enabledFeature.shaderStorageImageWriteWithoutFormat = true;
            }

            Span<VkUtf8String> supportedExtensionProperties = stackalloc VkUtf8String[]
            {
                VK_KHR_SWAPCHAIN_EXTENSION_NAME,
                VK_EXT_DEBUG_MARKER_EXTENSION_NAME,
            };

            var availableExtensionProperties = GetAvailableExtensionProperties(supportedExtensionProperties);
            ValidateExtensionPropertiesAvailability(availableExtensionProperties);
            var desiredExtensionProperties = new HashSet<VkUtf8String>
            {
                VK_KHR_SWAPCHAIN_EXTENSION_NAME
            };

            if (availableExtensionProperties.Contains(VK_EXT_DEBUG_MARKER_EXTENSION_NAME) && IsDebugMode)
            {
                desiredExtensionProperties.Add(VK_EXT_DEBUG_MARKER_EXTENSION_NAME);
                IsProfilingSupported = true;
            }

            var timelineSemaphoreFeatures = new VkPhysicalDeviceTimelineSemaphoreFeatures();
            timelineSemaphoreFeatures.sType = VkStructureType.PhysicalDeviceTimelineSemaphoreFeatures;
            timelineSemaphoreFeatures.timelineSemaphore = VkBool32.True;

            using VkStringArray ppEnabledExtensionNames = new(desiredExtensionProperties);
            var deviceCreateInfo = new VkDeviceCreateInfo
            {
                sType = VkStructureType.DeviceCreateInfo,
                pNext = &timelineSemaphoreFeatures,
                queueCreateInfoCount = 1,
                pQueueCreateInfos = &queueCreateInfo,
                enabledExtensionCount = ppEnabledExtensionNames.Length,
                ppEnabledExtensionNames = ppEnabledExtensionNames,
                pEnabledFeatures = &enabledFeature,
            };

            CheckResult(NativeInstanceApi.vkCreateDevice(NativePhysicalDevice, in deviceCreateInfo, null, out nativeDevice));

            nativeDeviceApi = GetApi(NativeInstance, NativeDevice);

            NativeDeviceApi.vkGetDeviceQueue(nativeDevice, 0, 0, out NativeCommandQueue);

            NativeCopyCommandPools = new(() => new CommandBufferPool(this, false), true);

            DescriptorPools = new HeapPool(this, true);

            // Fence for next frame and resource cleaning
            FrameFence = new(this);
            CopyFence = new(this);
            CommandListFence = new(this);
            CommandListFence.NextFenceValue = 0; // start at 0 for command list (we wait for previous command list signal and 0 is already set by default)

            nativeResourceCollector = new NativeResourceCollector(this);
            graphicsResourceLinkCollector = new GraphicsResourceLinkCollector(this);

            EmptyTexelBufferInt = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_UInt);
            EmptyTexelBufferFloat = Buffer.Typed.New(this, 1, PixelFormat.R32G32B32A32_Float);
            EmptyTexture = Texture.New2D(this, 1, 1, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource);
        }

        private unsafe HashSet<VkUtf8String> GetAvailableExtensionProperties(Span<VkUtf8String> supportedExtensionProperties)
        {
            var availableExtensionProperties = new HashSet<VkUtf8String>();
            NativeInstanceApi.vkEnumerateDeviceExtensionProperties(NativePhysicalDevice, out uint propertyCount).CheckResult();
            Span<VkExtensionProperties> extensionProperties = stackalloc VkExtensionProperties[(int)propertyCount];
            NativeInstanceApi.vkEnumerateDeviceExtensionProperties(NativePhysicalDevice, extensionProperties).CheckResult();

            for (int index = 0; index < extensionProperties.Length; index++)
            {
                var properties = extensionProperties[index];
                var name = new VkUtf8String(properties.extensionName);
                var indexOfExtensionName = supportedExtensionProperties.IndexOf(name);

                if (indexOfExtensionName >= 0)
                    availableExtensionProperties.Add(supportedExtensionProperties[indexOfExtensionName]);
            }

            return availableExtensionProperties;
        }

        private static void ValidateExtensionPropertiesAvailability(HashSet<VkUtf8String> availableExtensionProperties)
        {
            if (!availableExtensionProperties.Contains(VK_KHR_SWAPCHAIN_EXTENSION_NAME))
            {
                string extensionName = Encoding.UTF8.GetString(VK_KHR_SWAPCHAIN_EXTENSION_NAME);

                throw new NotSupportedException($"Required Vulkan extension {extensionName} is not supported by the current physical device.");
            }
        }

        internal unsafe IntPtr AllocateUploadBuffer(int size, out VkBuffer resource, out int offset)
        {
            lock (nativeUploadBufferLock)
            {
                if (nativeUploadBuffer == VkBuffer.Null || nativeUploadBufferOffset + size > nativeUploadBufferSize)
                {
                    if (nativeUploadBuffer != VkBuffer.Null)
                    {
                        NativeDeviceApi.vkUnmapMemory(NativeDevice, nativeUploadBufferMemory);
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
                    CheckResult(NativeDeviceApi.vkCreateBuffer(NativeDevice, &bufferCreateInfo, null, out nativeUploadBuffer));
                    AllocateMemory(VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);

                    fixed (IntPtr* nativeUploadBufferStartPtr = &nativeUploadBufferStart)
                        NativeDeviceApi.vkMapMemory(NativeDevice, nativeUploadBufferMemory, 0, (ulong)nativeUploadBufferSize, VkMemoryMapFlags.None, (void**)nativeUploadBufferStartPtr);
                    nativeUploadBufferOffset = 0;
                }

                // Bump allocate
                resource = nativeUploadBuffer;
                offset = nativeUploadBufferOffset;
                nativeUploadBufferOffset += size;

                return nativeUploadBufferStart + offset;
            }
        }

        internal unsafe ulong ExecuteAndWaitCopyQueueGPU(VkCommandBuffer commandBuffer)
        {
            lock (QueueLock)
            {
                var copyFenceValue = CopyFence.NextFenceValue++;
                var nextCopyFenceValue = copyFenceValue + 1;

                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    signalSemaphoreValueCount = 1,
                    pSignalSemaphoreValues = &nextCopyFenceValue,
                };
                var copySemaphore = CopyFence.Semaphore;
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    commandBufferCount = 1,
                    pCommandBuffers = &commandBuffer,
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &copySemaphore,
                };
                
                CheckResult(NativeDeviceApi.vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, VkFence.Null));

                return nextCopyFenceValue;
            }
        }

        protected unsafe void AllocateMemory(VkMemoryPropertyFlags memoryProperties)
        {
            NativeDeviceApi.vkGetBufferMemoryRequirements(nativeDevice, nativeUploadBuffer, out var memoryRequirements);

            if (memoryRequirements.size == 0)
                return;

            var allocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memoryRequirements.size,
            };

            NativeInstanceApi.vkGetPhysicalDeviceMemoryProperties(NativePhysicalDevice, out var physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.memoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *(&physicalDeviceMemoryProperties.memoryTypes[0] + i);
                    if ((memoryType.propertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.memoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            NativeDeviceApi.vkAllocateMemory(NativeDevice, &allocateInfo, null, out nativeUploadBufferMemory);
            NativeDeviceApi.vkBindBufferMemory(NativeDevice, nativeUploadBuffer, nativeUploadBufferMemory, 0);
        }

        /// <summary>
        ///   Makes Vulkan-specific adjustments to the Pipeline State objects created by the Graphics Device.
        /// </summary>
        /// <param name="pipelineStateDescription">A Pipeline State description that can be modified and adjusted.</param>
        private partial void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
        }

        /// <summary>
        ///   Releases the platform-specific Graphics Device and all its associated resources.
        /// </summary>
        protected partial void DestroyPlatformDevice()
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
            CheckResult(NativeDeviceApi.vkDeviceWaitIdle(nativeDevice));

            // Mark upload buffer for destruction
            if (nativeUploadBuffer != VkBuffer.Null)
            {
                NativeDeviceApi.vkUnmapMemory(NativeDevice, nativeUploadBufferMemory);
                nativeResourceCollector.Add(FrameFence.LastCompletedFence, nativeUploadBuffer);
                nativeResourceCollector.Add(FrameFence.LastCompletedFence, nativeUploadBufferMemory);

                nativeUploadBuffer = VkBuffer.Null;
                nativeUploadBufferMemory = VkDeviceMemory.Null;
            }

            // Release fenced resources
            nativeResourceCollector.Dispose();
            DescriptorPools.Dispose();

            FrameFence.Dispose();
            CopyFence.Dispose();
            CommandListFence.Dispose();

            foreach (var nativeCopyCommandPool in NativeCopyCommandPools.Values)
                nativeCopyCommandPool.Dispose();
            NativeCopyCommandPools.Dispose();
            NativeCopyCommandPools = null;
            NativeDeviceApi.vkDestroyDevice(nativeDevice, null);
        }

        internal void OnDestroyed(bool immediately = false)
        {
        }

        internal unsafe ulong ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            //if (nativeUploadBuffer != VkBuffer.Null)
            //{
            //    NativeDevice.UnmapMemory(nativeUploadBufferMemory);
            //    TemporaryResources.Enqueue(new BufferInfo(NextFenceValue, nativeUploadBuffer, nativeUploadBufferMemory));

            //    nativeUploadBuffer = VkBuffer.Null;
            //    nativeUploadBufferMemory = VkDeviceMemory.Null;
            //}

            ulong nextCommandListFenceValue;
            lock (QueueLock)
            {
                var commandListFenceValue = CommandListFence.NextFenceValue++;
                nextCommandListFenceValue = commandListFenceValue + 1;
                // Make sure all copies are done as well
                var copyFenceValue = CopyFence.NextFenceValue;
                var waitFenceValues = stackalloc ulong[] { commandListFenceValue, copyFenceValue };

                // Do we need to wait for CopyFence?
                var semaphoreCount = copyFenceValue > LastGPUSyncCopyFenceToCommandFence ? 2 : 1;
                // Remember that we waited 
                LastGPUSyncCopyFenceToCommandFence = copyFenceValue;

                // Submit commands
                var timelineInfo = new VkTimelineSemaphoreSubmitInfo
                {
                    sType = VkStructureType.TimelineSemaphoreSubmitInfo,
                    waitSemaphoreValueCount = 2,
                    pWaitSemaphoreValues = &waitFenceValues[0],
                    signalSemaphoreValueCount = 1,
                    pSignalSemaphoreValues = &nextCommandListFenceValue,
                };

                var semaphores = stackalloc VkSemaphore[] { CommandListFence.Semaphore, CopyFence.Semaphore };
                var pipelineStageFlags = stackalloc VkPipelineStageFlags[] { VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.BottomOfPipe };
                var nativeCommandBufferCopy = commandList.NativeCommandBuffer;
                var submitInfo = new VkSubmitInfo
                {
                    sType = VkStructureType.SubmitInfo,
                    pNext = &timelineInfo,
                    commandBufferCount = 1,
                    pCommandBuffers = &nativeCommandBufferCopy,
                    waitSemaphoreCount = 2,
                    pWaitSemaphores = &semaphores[0],
                    pWaitDstStageMask = &pipelineStageFlags[0],
                    signalSemaphoreCount = 1,
                    pSignalSemaphores = &semaphores[0],
                };
                
                CheckResult(NativeDeviceApi.vkQueueSubmit(NativeCommandQueue, 1, &submitInfo, VkFence.Null));
            }

            // Collect resources
            RecycleCommandListResources(commandList, nextCommandListFenceValue);

            return nextCommandListFenceValue;
        }

        private void RecycleCommandListResources(CompiledCommandList commandList, ulong commandListFenceValue)
        {
            // Set fence on staging textures
            foreach (var stagingResource in commandList.StagingResources)
            {
                stagingResource.CommandListFenceValue = commandListFenceValue;
            }

            StagingResourceLists.Release(commandList.StagingResources);
            commandList.StagingResources.Clear();

            // Recycle all resources
            foreach (var descriptorPool in commandList.DescriptorPools)
            {
                DescriptorPools.RecycleObject(commandListFenceValue, descriptorPool);
            }
            DescriptorPoolLists.Release(commandList.DescriptorPools);
            commandList.DescriptorPools.Clear();

            commandList.Builder.CommandBufferPool.RecycleObject(commandListFenceValue, commandList.NativeCommandBuffer);
        }

        internal void Collect(NativeResource nativeResource)
        {
            nativeResourceCollector.Add(FrameFence.NextFenceValue, nativeResource);
        }

        /// <summary>
        ///   Tags a Graphics Resource as no having alive references, meaning it should be safe to dispose it
        ///   or discard its contents during the next <see cref="CommandList.MapSubResource"/> or <c>SetData</c> operation.
        /// </summary>
        /// <param name="resourceLink">
        ///   A <see cref="GraphicsResourceLink"/> object identifying the Graphics Resource along some related allocation information.
        /// </param>
        internal partial void TagResourceAsNotAlive(GraphicsResourceLink resourceLink)
        {
            switch (resourceLink.Resource)
            {
                case Texture texture:
                    if (texture.Usage == GraphicsResourceUsage.Dynamic)
                    {
                        // Increase the reference count until GPU is done with the resource
                        resourceLink.ReferenceCount++;
                        graphicsResourceLinkCollector.Add(FrameFence.NextFenceValue, resourceLink);
                    }
                    break;

                case Buffer buffer:
                    if (buffer.Usage == GraphicsResourceUsage.Dynamic)
                    {
                        // Increase the reference count until GPU is done with the resource
                        resourceLink.ReferenceCount++;
                        graphicsResourceLinkCollector.Add(FrameFence.NextFenceValue, resourceLink);
                    }
                    break;

                case QueryPool _:
                    resourceLink.ReferenceCount++;
                    graphicsResourceLinkCollector.Add(FrameFence.NextFenceValue, resourceLink);
                    break;
            }
        }

        internal unsafe struct FenceHelper : IDisposable
        {
            private GraphicsDevice graphicsDevice;
            public VkSemaphore Semaphore;
            public ulong NextFenceValue = 1;
            public ulong LastCompletedFence;

            public FenceHelper(GraphicsDevice graphicsDevice)
            {
                this.graphicsDevice = graphicsDevice;
                var timelineInfo = new VkSemaphoreTypeCreateInfo { sType = VkStructureType.SemaphoreTypeCreateInfo, semaphoreType = VkSemaphoreType.Timeline };
                var createInfo = new VkSemaphoreCreateInfo { sType = VkStructureType.SemaphoreCreateInfo, pNext = &timelineInfo };
                graphicsDevice.CheckResult(graphicsDevice.NativeDeviceApi.vkCreateSemaphore(graphicsDevice.NativeDevice, &createInfo, null, out Semaphore));
            }

            internal ulong GetCompletedValue()
            {
                ulong result = 0;
                graphicsDevice.NativeDeviceApi.vkGetSemaphoreCounterValue(graphicsDevice.NativeDevice, Semaphore, &result);
                return result;
            }

            internal bool IsFenceCompleteInternal(ulong fenceValue)
            {
                // Try to avoid checking the fence if possible
                if (fenceValue > LastCompletedFence)
                    LastCompletedFence = Math.Max(LastCompletedFence, GetCompletedValue()); // Protect against race conditions

                return fenceValue <= LastCompletedFence;
            }

            internal void WaitForFenceCPUInternal(ulong fenceValue)
            {
                if (IsFenceCompleteInternal(fenceValue))
                    return;

                // TODO D3D12 in case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue then first one
                //lock (Fence)
                {
                    fixed (VkSemaphore* semaphore = &Semaphore)
                    {
                        var waitInfo = new VkSemaphoreWaitInfo
                        {
                            sType = VkStructureType.SemaphoreWaitInfo,
                            semaphoreCount = 1,
                            pSemaphores = semaphore,
                            pValues = &fenceValue,
                        };
                        graphicsDevice.NativeDeviceApi.vkWaitSemaphores(graphicsDevice.NativeDevice, &waitInfo, ulong.MaxValue);
                        LastCompletedFence = fenceValue;
                    }
                }
            }

            public void Dispose()
            {
                graphicsDevice.NativeDeviceApi.vkDestroySemaphore(graphicsDevice.NativeDevice, Semaphore);
            }
        }
    }

    internal abstract class ResourcePool<T> : ComponentBase
    {
        protected readonly GraphicsDevice GraphicsDevice;
        private readonly Queue<KeyValuePair<ulong, T>> liveObjects = new();
        private readonly bool threadsafe;

        protected ResourcePool(GraphicsDevice graphicsDevice, bool threadsafe)
        {
            GraphicsDevice = graphicsDevice;
            this.threadsafe = threadsafe;
        }

        public T GetObject(ulong completedFenceValue)
        {
            using (OptionalLock.Lock(liveObjects, threadsafe))
            {
                // Check if first allocator is ready for reuse
                if (liveObjects.Count > 0)
                {
                    var firstAllocator = liveObjects.Peek();
                    if (firstAllocator.Key <= completedFenceValue)
                    {
                        liveObjects.Dequeue();
                        ResetObject(firstAllocator.Value);
                        return firstAllocator.Value;
                    }
                }

                return CreateObject();
            }
        }

        public void RecycleObject(ulong fenceValue, T obj)
        {
            using (OptionalLock.Lock(liveObjects, threadsafe))
            {
                liveObjects.Enqueue(new KeyValuePair<ulong, T>(fenceValue, obj));
            }
        }

        protected abstract T CreateObject();

        protected abstract void ResetObject(T obj);

        protected virtual void DestroyObject(T obj)
        {
        }

        protected override void Destroy()
        {
            using (OptionalLock.Lock(liveObjects, threadsafe))
            { 
                foreach (var item in liveObjects)
                {
                    DestroyObject(item.Value);
                }
            }

            base.Destroy();
        }

        // TODO: do we want to use spinlock instead? (need to measure impact, not good if too long wait)
        private struct OptionalLock : IDisposable
        {
            private readonly object lockObject;
            private readonly bool locked;

            // Use a private constructor to force usage through the static factory methods
            private OptionalLock(object lockObject, bool locked)
            {
                this.lockObject = lockObject;
                this.locked = locked;
            }

            public void Dispose()
            {
                if (locked)
                {
                    Monitor.Exit(lockObject);
                }
            }

            // Factory method for a locked scope
            public static OptionalLock Lock(object lockObject, bool useLock)
            {
                // TODO: do we want to use spinlock instead?
                if (useLock)
                {
                    useLock = false;
                    Monitor.Enter(lockObject, ref useLock);
                }
                return new OptionalLock(lockObject, useLock);
            }
        }
    }

    internal class CommandBufferPool : ResourcePool<VkCommandBuffer>
    {
        private readonly VkCommandPool commandPool;

        public unsafe CommandBufferPool(GraphicsDevice graphicsDevice, bool threadsafe) : base(graphicsDevice, threadsafe)
        {
            var commandPoolCreateInfo = new VkCommandPoolCreateInfo
            {
                sType = VkStructureType.CommandPoolCreateInfo,
                queueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                flags = VkCommandPoolCreateFlags.ResetCommandBuffer
            };

            GraphicsDevice.CheckResult(graphicsDevice.NativeDeviceApi.vkCreateCommandPool(graphicsDevice.NativeDevice, &commandPoolCreateInfo, null, out commandPool));
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
            GraphicsDevice.NativeDeviceApi.vkAllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);
            return commandBuffer;
        }

        protected override void ResetObject(VkCommandBuffer obj)
        {
            GraphicsDevice.NativeDeviceApi.vkResetCommandBuffer(obj, VkCommandBufferResetFlags.None);
        }

        protected override unsafe void Destroy()
        {
            base.Destroy();

            GraphicsDevice.NativeDeviceApi.vkDestroyCommandPool(GraphicsDevice.NativeDevice, commandPool, null);
        }
    }

    internal class HeapPool : ResourcePool<VkDescriptorPool>
    {
        public HeapPool(GraphicsDevice graphicsDevice, bool threadsafe) : base(graphicsDevice, threadsafe)
        {
        }

        protected override unsafe VkDescriptorPool CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var poolSizes = GraphicsDevice.MaxDescriptorTypeCounts
                .Select((count, index) => new VkDescriptorPoolSize { type = (VkDescriptorType)index, descriptorCount = count })
                .Where(size => size.descriptorCount > 0)
                .ToArray();

            fixed (VkDescriptorPoolSize* fPoolSizes = poolSizes) { // null if array is empty or null
                var descriptorPoolCreateInfo = new VkDescriptorPoolCreateInfo
                {
                    sType = VkStructureType.DescriptorPoolCreateInfo,
                    poolSizeCount = (uint)poolSizes.Length,
                    pPoolSizes = fPoolSizes,
                    maxSets = GraphicsDevice.MaxDescriptorSetCount,
                };
                GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateDescriptorPool(GraphicsDevice.NativeDevice, &descriptorPoolCreateInfo, null, out var descriptorPool));
                return descriptorPool;
            }
        }

        protected override void ResetObject(VkDescriptorPool obj)
        {
            GraphicsDevice.NativeDeviceApi.vkResetDescriptorPool(GraphicsDevice.NativeDevice, obj, VkDescriptorPoolResetFlags.None);
        }

        protected override unsafe void DestroyObject(VkDescriptorPool obj)
        {
            GraphicsDevice.NativeDeviceApi.vkDestroyDescriptorPool(GraphicsDevice.NativeDevice, obj, null);
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
            return new NativeResource(VkDebugReportObjectTypeEXT.Buffer, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkBufferView handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.BufferView, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkImage handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.Image, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkImageView handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.ImageView, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkDeviceMemory handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.DeviceMemory, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkSampler handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.Sampler, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkFramebuffer handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.Framebuffer, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkSemaphore handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.Semaphore, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkFence handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.Fence, *(ulong*)&handle);
        }

        public static unsafe implicit operator NativeResource(VkQueryPool handle)
        {
            return new NativeResource(VkDebugReportObjectTypeEXT.QueryPool, *(ulong*)&handle);
        }

        public unsafe void Destroy(GraphicsDevice device)
        {
            var handleCopy = handle;

            switch (type)
            {
                case VkDebugReportObjectTypeEXT.Buffer:
                    device.NativeDeviceApi.vkDestroyBuffer(device.NativeDevice, *(VkBuffer*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.BufferView:
                    device.NativeDeviceApi.vkDestroyBufferView(device.NativeDevice, *(VkBufferView*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.Image:
                    device.NativeDeviceApi.vkDestroyImage(device.NativeDevice, *(VkImage*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.ImageView:
                    device.NativeDeviceApi.vkDestroyImageView(device.NativeDevice, *(VkImageView*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.DeviceMemory:
                    device.NativeDeviceApi.vkFreeMemory(device.NativeDevice, *(VkDeviceMemory*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.Sampler:
                    device.NativeDeviceApi.vkDestroySampler(device.NativeDevice, *(VkSampler*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.Framebuffer:
                    device.NativeDeviceApi.vkDestroyFramebuffer(device.NativeDevice, *(VkFramebuffer*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.Semaphore:
                    device.NativeDeviceApi.vkDestroySemaphore(device.NativeDevice, *(VkSemaphore*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.Fence:
                    device.NativeDeviceApi.vkDestroyFence(device.NativeDevice, *(VkFence*)&handleCopy, null);
                    break;
                case VkDebugReportObjectTypeEXT.QueryPool:
                    device.NativeDeviceApi.vkDestroyQueryPool(device.NativeDevice, *(VkQueryPool*)&handleCopy, null);
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
        private readonly Queue<KeyValuePair<ulong, T>> items = new();

        protected TemporaryResourceCollector(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public void Add(ulong frameFenceValue, T item)
        {
            lock (items)
            {
                items.Enqueue(new KeyValuePair<ulong, T>(frameFenceValue, item));
            }
        }

        public void Release()
        {
            lock (items)
            {
                while (items.Count > 0 && GraphicsDevice.FrameFence.IsFenceCompleteInternal(items.Peek().Key))
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
