// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using SharpDX.Direct3D12;
using Stride.Core.Collections;
using Stride.Core.Threading;

namespace Stride.Graphics
{
    public partial class GraphicsDevice
    {
        // D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT (not exposed by SharpDX)
        internal readonly int ConstantBufferDataPlacementAlignment = 256;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D12;

        internal readonly ConcurrentPool<List<GraphicsResource>> StagingResourceLists = new ConcurrentPool<List<GraphicsResource>>(() => new List<GraphicsResource>());
        internal readonly ConcurrentPool<List<DescriptorHeap>> DescriptorHeapLists = new ConcurrentPool<List<DescriptorHeap>>(() => new List<DescriptorHeap>());

        private bool simulateReset = false;
        private string rendererName;

        private SharpDX.Direct3D12.Device nativeDevice;
        internal CommandQueue NativeCommandQueue;

        internal GraphicsProfile RequestedProfile;
        internal SharpDX.Direct3D.FeatureLevel CurrentFeatureLevel;

        internal CommandQueue NativeCopyCommandQueue;
        internal CommandAllocator NativeCopyCommandAllocator;
        internal GraphicsCommandList NativeCopyCommandList;
        private Fence nativeCopyFence;
        private long nextCopyFenceValue = 1;

        internal CommandAllocatorPool CommandAllocators;
        internal HeapPool SrvHeaps;
        internal HeapPool SamplerHeaps;
        internal const int SrvHeapSize = 2048;
        internal const int SamplerHeapSize = 64;

        internal DescriptorAllocator SamplerAllocator;
        internal DescriptorAllocator ShaderResourceViewAllocator;
        internal DescriptorAllocator UnorderedAccessViewAllocator => ShaderResourceViewAllocator;
        internal DescriptorAllocator DepthStencilViewAllocator;
        internal DescriptorAllocator RenderTargetViewAllocator;

        private SharpDX.Direct3D12.Resource nativeUploadBuffer;
        private IntPtr nativeUploadBufferStart;
        private int nativeUploadBufferOffset;

        internal int SrvHandleIncrementSize;
        internal int SamplerHandleIncrementSize;

        private Fence nativeFence;
        private long lastCompletedFence;
        internal long NextFenceValue = 1;
        private AutoResetEvent fenceEvent = new AutoResetEvent(false);

        // Temporary or destroyed resources kept around until the GPU doesn't need them anymore
        internal Queue<KeyValuePair<long, object>> TemporaryResources = new Queue<KeyValuePair<long, object>>();

        private readonly FastList<SharpDX.Direct3D12.CommandList> nativeCommandLists = new FastList<SharpDX.Direct3D12.CommandList>();

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

                var result = NativeDevice.DeviceRemovedReason;
                if (result == SharpDX.DXGI.ResultCode.DeviceRemoved)
                {
                    return GraphicsDeviceStatus.Removed;
                }

                if (result == SharpDX.DXGI.ResultCode.DeviceReset)
                {
                    return GraphicsDeviceStatus.Reset;
                }

                if (result == SharpDX.DXGI.ResultCode.DeviceHung)
                {
                    return GraphicsDeviceStatus.Hung;
                }

                if (result == SharpDX.DXGI.ResultCode.DriverInternalError)
                {
                    return GraphicsDeviceStatus.InternalError;
                }

                if (result == SharpDX.DXGI.ResultCode.InvalidCall)
                {
                    return GraphicsDeviceStatus.InvalidCall;
                }

                if (result.Code < 0)
                {
                    return GraphicsDeviceStatus.Reset;
                }

                return GraphicsDeviceStatus.Normal;
            }
        }

        /// <summary>
        ///     Gets the native device.
        /// </summary>
        /// <value>The native device.</value>
        internal SharpDX.Direct3D12.Device NativeDevice
        {
            get
            {
                return nativeDevice;
            }
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
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            if (commandLists == null) throw new ArgumentNullException(nameof(commandLists));
            if (count > commandLists.Length) throw new ArgumentOutOfRangeException(nameof(count));

            var fenceValue = NextFenceValue++;

            // Recycle resources
            for (int index = 0; index < count; index++)
            {
                var commandList = commandLists[index];
                nativeCommandLists.Add(commandList.NativeCommandList);
                RecycleCommandListResources(commandList, fenceValue);
            }

            // Submit and signal fence
            NativeCommandQueue.ExecuteCommandLists(count, nativeCommandLists.Items);
            NativeCommandQueue.Signal(nativeFence, fenceValue);

            ReleaseTemporaryResources();

            nativeCommandLists.Clear();
        }

        public void SimulateReset()
        {
            simulateReset = true;
        }

        private void InitializePostFeatures()
        {
        }

        private string GetRendererName()
        {
            return rendererName;
        }

        /// <summary>
        ///     Initializes the specified device.
        /// </summary>
        /// <param name="graphicsProfiles">The graphics profiles.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            if (nativeDevice != null)
            {
                // Destroy previous device
                ReleaseDevice();
            }

            rendererName = Adapter.NativeAdapter.Description.Description;

            // Profiling is supported through pix markers
            IsProfilingSupported = true;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            bool isDebug = (deviceCreationFlags & DeviceCreationFlags.Debug) != 0;
            if (isDebug)
            {
                SharpDX.Direct3D12.DebugInterface.Get().EnableDebugLayer();
            }

            // Create Device D3D12 with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                var graphicsProfile = graphicsProfiles[index];
                try
                {
                    // D3D12 supports only feature level 11+
                    var level = graphicsProfile.ToFeatureLevel();
                    if (level < SharpDX.Direct3D.FeatureLevel.Level_11_0)
                        level = SharpDX.Direct3D.FeatureLevel.Level_11_0;

                    nativeDevice = new SharpDX.Direct3D12.Device(Adapter.NativeAdapter, level);

                    RequestedProfile = graphicsProfile;
                    CurrentFeatureLevel = level;
                    break;
                }
                catch (Exception)
                {
                    if (index == graphicsProfiles.Length - 1)
                        throw;
                }
            }

            // Describe and create the command queue.
            var queueDesc = new SharpDX.Direct3D12.CommandQueueDescription(SharpDX.Direct3D12.CommandListType.Direct);
            NativeCommandQueue = nativeDevice.CreateCommandQueue(queueDesc);
            //queueDesc.Type = CommandListType.Copy;
            NativeCopyCommandQueue = nativeDevice.CreateCommandQueue(queueDesc);
            TimestampFrequency = NativeCommandQueue.TimestampFrequency;

            SrvHandleIncrementSize = NativeDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            SamplerHandleIncrementSize = NativeDevice.GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);

            if (isDebug)
            {
                var debugDevice = nativeDevice.QueryInterfaceOrNull<DebugDevice>();
                if (debugDevice != null)
                {
                    var infoQueue = debugDevice.QueryInterfaceOrNull<InfoQueue>();
                    if (infoQueue != null)
                    {
                        MessageId[] disabledMessages =
                        {
                            // This happens when render target or depth stencil clear value is diffrent
                            // than provided during resource allocation.
                            MessageId.CleardepthstencilviewMismatchingclearvalue,
                            MessageId.ClearrendertargetviewMismatchingclearvalue,

                            // This occurs when there are uninitialized descriptors in a descriptor table,
                            // even when a shader does not access the missing descriptors.
                            MessageId.InvalidDescriptorHandle,
                            
                            // These happen when capturing with VS diagnostics
                            MessageId.MapInvalidNullRange,
                            MessageId.UnmapInvalidNullRange,
                        };

                        // Disable irrelevant debug layer warnings
                        InfoQueueFilter filter = new InfoQueueFilter
                        {
                            DenyList = new InfoQueueFilterDescription
                            {
                                Ids = disabledMessages
                            }
                        };
                        infoQueue.AddStorageFilterEntries(filter);

                        //infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
                        //infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);

                        infoQueue.Dispose();
                    }
                    debugDevice.Dispose();
                }
            }

            // Prepare pools
            CommandAllocators = new CommandAllocatorPool(this);
            SrvHeaps = new HeapPool(this, SrvHeapSize, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            SamplerHeaps = new HeapPool(this, SamplerHeapSize, DescriptorHeapType.Sampler);

            // Prepare descriptor allocators
            SamplerAllocator = new DescriptorAllocator(this, DescriptorHeapType.Sampler);
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.DepthStencilView);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.RenderTargetView);

            // Prepare copy command list (start it closed, so that every new use start with a Reset)
            NativeCopyCommandAllocator = NativeDevice.CreateCommandAllocator(CommandListType.Direct);
            NativeCopyCommandList = NativeDevice.CreateCommandList(CommandListType.Direct, NativeCopyCommandAllocator, null);
            NativeCopyCommandList.Close();

            // Fence for next frame and resource cleaning
            nativeFence = NativeDevice.CreateFence(0, FenceFlags.None);
            nativeCopyFence = NativeDevice.CreateFence(0, FenceFlags.None);
        }

        internal IntPtr AllocateUploadBuffer(int size, out SharpDX.Direct3D12.Resource resource, out int offset, int alignment = 0)
        {
            // TODO D3D12 thread safety, should we simply use locks?

            // Align
            if (alignment > 0)
                nativeUploadBufferOffset = (nativeUploadBufferOffset + alignment - 1) / alignment * alignment;

            if (nativeUploadBuffer == null || nativeUploadBufferOffset + size > nativeUploadBuffer.Description.Width)
            {
                if (nativeUploadBuffer != null)
                {
                    nativeUploadBuffer.Unmap(0);
                    TemporaryResources.Enqueue(new KeyValuePair<long, object>(NextFenceValue, nativeUploadBuffer));
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                var bufferSize = Math.Max(4 * 1024*1024, size);
                nativeUploadBuffer = NativeDevice.CreateCommittedResource(new HeapProperties(HeapType.Upload), HeapFlags.None, ResourceDescription.Buffer(bufferSize), ResourceStates.GenericRead);
                nativeUploadBufferStart = nativeUploadBuffer.Map(0, new Range());
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = nativeUploadBuffer;
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferStart + offset;
        }

        internal void WaitCopyQueue()
        {
            NativeCommandQueue.ExecuteCommandList(NativeCopyCommandList);
            NativeCommandQueue.Signal(nativeCopyFence, nextCopyFenceValue);
            NativeCommandQueue.Wait(nativeCopyFence, nextCopyFenceValue);
            nextCopyFenceValue++;
        }

        internal void ReleaseTemporaryResources()
        {
            lock (TemporaryResources)
            {
                // Release previous frame resources
                while (TemporaryResources.Count > 0 && IsFenceCompleteInternal(TemporaryResources.Peek().Key))
                {
                    var temporaryResource = TemporaryResources.Dequeue().Value;
                    //temporaryResource.Value.Dispose();
                    var comObject = temporaryResource as SharpDX.ComObject;
                    if (comObject != null)
                        ((SharpDX.IUnknown)comObject).Release();
                    else
                    {
                        var referenceLink = temporaryResource as GraphicsResourceLink;
                        if (referenceLink != null)
                        {
                            referenceLink.ReferenceCount--;
                        }
                    }
                }
            }
        }

        private void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription)
        {
        }

        protected void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        private void ReleaseDevice()
        {
            // Wait for completion of everything queued
            NativeCommandQueue.Signal(nativeFence, NextFenceValue);
            NativeCommandQueue.Wait(nativeFence, NextFenceValue);

            // Release command queue
            NativeCommandQueue.Dispose();
            NativeCommandQueue = null;

            NativeCopyCommandQueue.Dispose();
            NativeCopyCommandQueue = null;

            NativeCopyCommandAllocator.Dispose();
            NativeCopyCommandList.Dispose();

            nativeUploadBuffer.Dispose();

            // Release temporary resources
            ReleaseTemporaryResources();
            nativeFence.Dispose();
            nativeFence = null;
            nativeCopyFence.Dispose();
            nativeCopyFence = null;

            // Release pools
            CommandAllocators.Dispose();
            SrvHeaps.Dispose();
            SamplerHeaps.Dispose();

            // Release allocators
            SamplerAllocator.Dispose();
            ShaderResourceViewAllocator.Dispose();
            DepthStencilViewAllocator.Dispose();
            RenderTargetViewAllocator.Dispose();

            if (IsDebugMode)
            {
                var debugDevice = NativeDevice.QueryInterfaceOrNull<SharpDX.Direct3D12.DebugDevice>();
                if (debugDevice != null)
                {
                    debugDevice.ReportLiveDeviceObjects(SharpDX.Direct3D12.ReportingLevel.Detail);
                    debugDevice.Dispose();
                }
            }

            nativeDevice.Dispose();
            nativeDevice = null;
        }

        internal void OnDestroyed()
        {
        }

        internal long ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            var fenceValue = NextFenceValue++;

            // Submit and signal fence
            NativeCommandQueue.ExecuteCommandList(commandList.NativeCommandList);
            NativeCommandQueue.Signal(nativeFence, fenceValue);

            // Recycle resources
            RecycleCommandListResources(commandList, fenceValue);

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

            // Recycle resources
            foreach (var heap in commandList.SrvHeaps)
            {
                SrvHeaps.RecycleObject(fenceValue, heap);
            }
            commandList.SrvHeaps.Clear();
            DescriptorHeapLists.Release(commandList.SrvHeaps);

            foreach (var heap in commandList.SamplerHeaps)
            {
                SamplerHeaps.RecycleObject(fenceValue, heap);
            }
            commandList.SamplerHeaps.Clear();
            DescriptorHeapLists.Release(commandList.SamplerHeaps);

            commandList.Builder.NativeCommandLists.Enqueue(commandList.NativeCommandList);
            CommandAllocators.RecycleObject(fenceValue, commandList.NativeCommandAllocator);
        }

        internal bool IsFenceCompleteInternal(long fenceValue)
        {
            // Try to avoid checking the fence if possible
            if (fenceValue > lastCompletedFence)
                lastCompletedFence = Math.Max(lastCompletedFence, nativeFence.CompletedValue); // Protect against race conditions

            return fenceValue <= lastCompletedFence;
        }

        internal void WaitForFenceInternal(long fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            // TODO D3D12 in case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue then first one
            lock (nativeFence)
            {
                nativeFence.SetEventOnCompletion(fenceValue, fenceEvent.SafeWaitHandle.DangerousGetHandle());
                fenceEvent.WaitOne();
                lastCompletedFence = fenceValue;
            }
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            var texture = resourceLink.Resource as Texture;
            if (texture != null && texture.Usage == GraphicsResourceUsage.Dynamic)
            {
                // Increase the reference count until GPU is done with the resource
                resourceLink.ReferenceCount++;
                TemporaryResources.Enqueue(new KeyValuePair<long, object>(NextFenceValue, resourceLink));
            }

            var buffer = resourceLink.Resource as Buffer;
            if (buffer != null && buffer.Usage == GraphicsResourceUsage.Dynamic)
            {
                // Increase the reference count until GPU is done with the resource
                resourceLink.ReferenceCount++;
                TemporaryResources.Enqueue(new KeyValuePair<long, object>(NextFenceValue, resourceLink));
            }
        }

        internal abstract class ResourcePool<T> : IDisposable where T : Pageable
        {
            protected readonly GraphicsDevice GraphicsDevice;
            private readonly Queue<KeyValuePair<long, T>> liveObjects = new Queue<KeyValuePair<long, T>>();

            protected ResourcePool(GraphicsDevice graphicsDevice)
            {
                GraphicsDevice = graphicsDevice;
            }

            public void Dispose()
            {
                lock (liveObjects)
                {
                    foreach (var liveObject in liveObjects)
                    {
                        liveObject.Value.Dispose();
                    }
                    liveObjects.Clear();
                }
            }

            public T GetObject()
            {
                // TODO D3D12: SpinLock
                lock (liveObjects)
                {
                    // Check if first allocator is ready for reuse
                    if (liveObjects.Count > 0)
                    {
                        var firstAllocator = liveObjects.Peek();
                        if (firstAllocator.Key <= GraphicsDevice.nativeFence.CompletedValue)
                        {
                            liveObjects.Dequeue();
                            ResetObject(firstAllocator.Value);

                            if (firstAllocator.Value == null)
                            {
                                
                            }

                            return firstAllocator.Value;
                        }
                    }

                    return CreateObject();
                }
            }

            protected abstract T CreateObject();

            protected abstract void ResetObject(T obj);

            public void RecycleObject(long fenceValue, T obj)
            {
                // TODO D3D12: SpinLock
                lock (liveObjects)
                {
                    liveObjects.Enqueue(new KeyValuePair<long, T>(fenceValue, obj));
                }
            }
        }

        internal class CommandAllocatorPool : ResourcePool<CommandAllocator>
        {
            public CommandAllocatorPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
            {
            }

            protected override CommandAllocator CreateObject()
            {
                // No allocator ready to be used, let's create a new one
               return GraphicsDevice.NativeDevice.CreateCommandAllocator(CommandListType.Direct);
            }

            protected override void ResetObject(CommandAllocator obj)
            {
                obj.Reset();
            }
        }

        internal class HeapPool : ResourcePool<DescriptorHeap>
        {
            private readonly int heapSize;
            private readonly DescriptorHeapType heapType;

            public HeapPool(GraphicsDevice graphicsDevice, int heapSize, DescriptorHeapType heapType) : base(graphicsDevice)
            {
                this.heapSize = heapSize;
                this.heapType = heapType;
            }

            protected override DescriptorHeap CreateObject()
            {
                // No allocator ready to be used, let's create a new one
                return GraphicsDevice.NativeDevice.CreateDescriptorHeap(new DescriptorHeapDescription
                {
                    DescriptorCount = heapSize,
                    Flags = DescriptorHeapFlags.ShaderVisible,
                    Type = heapType,
                });
            }

            protected override void ResetObject(DescriptorHeap obj)
            {
            }
        }

        /// <summary>
        /// Allocate descriptor handles. For now a simple bump alloc, but at some point we will have to make a real allocator with free
        /// </summary>
        internal class DescriptorAllocator : IDisposable
        {
            private const int DescriptorPerHeap = 256;

            private GraphicsDevice device;
            private DescriptorHeapType descriptorHeapType;
            private DescriptorHeap currentHeap;
            private CpuDescriptorHandle currentHandle;
            private int remainingHandles;
            private readonly int descriptorSize;

            public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType)
            {
                this.device = device;
                this.descriptorHeapType = descriptorHeapType;
                this.descriptorSize = device.NativeDevice.GetDescriptorHandleIncrementSize(descriptorHeapType);
            }

            public void Dispose()
            {
                currentHeap?.Dispose();
                currentHeap = null;
            }

            public CpuDescriptorHandle Allocate(int count)
            {
                if (currentHeap == null || remainingHandles < count)
                {
                    currentHeap = device.NativeDevice.CreateDescriptorHeap(new DescriptorHeapDescription
                    {
                        Flags = DescriptorHeapFlags.None,
                        Type = descriptorHeapType,
                        DescriptorCount = DescriptorPerHeap,
                        NodeMask = 1,
                    });
                    remainingHandles = DescriptorPerHeap;
                    currentHandle = currentHeap.CPUDescriptorHandleForHeapStart;
                }

                var result = currentHandle;

                currentHandle.Ptr += descriptorSize;
                remainingHandles -= count;

                return result;
            }
        }
    }
}
#endif
