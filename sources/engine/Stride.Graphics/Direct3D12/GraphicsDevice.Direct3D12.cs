// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Threading;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Stride.Core.Collections;
using Stride.Core.Threading;

namespace Stride.Graphics
{
    public unsafe partial class GraphicsDevice
    {
        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D12;

        internal readonly ConcurrentPool<List<GraphicsResource>> StagingResourceLists = new(() => new List<GraphicsResource>());
        internal readonly ConcurrentPool<List<HeapPtr>> DescriptorHeapLists = new(() => new List<HeapPtr>());

        private bool simulateReset = false;
        private string rendererName;

        private ID3D12Device* nativeDevice;
        internal ID3D12CommandQueue* NativeCommandQueue;

        internal GraphicsProfile RequestedProfile;
        internal D3DFeatureLevel CurrentFeatureLevel;

        internal ID3D12CommandQueue* NativeCopyCommandQueue;
        internal ID3D12CommandAllocator* NativeCopyCommandAllocator;
        internal ID3D12GraphicsCommandList* NativeCopyCommandList;
        private ID3D12Fence* nativeCopyFence;
        private ulong nextCopyFenceValue = 1;

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

        private ID3D12Resource* nativeUploadBuffer;
        private nint nativeUploadBufferStart;
        private int nativeUploadBufferOffset;

        internal int SrvHandleIncrementSize;
        internal int SamplerHandleIncrementSize;

        private readonly object nativeFenceLock = new();
        private ID3D12Fence* nativeFence;
        private ulong lastCompletedFence;
        internal ulong NextFenceValue = 1;
        private readonly AutoResetEvent fenceEvent = new(initialState: false);

        // Temporary or destroyed resources kept around until the GPU doesn't need them anymore
        internal Queue<(ulong FenceValue, object Resource)> TemporaryResources = new();

        private readonly FastList<CommandListPtr> nativeCommandLists = new();

        /// <summary>
        /// The tick frquency of timestamp queries in Hertz.
        /// </summary>
        public ulong TimestampFrequency { get; private set; }

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

                var result = (DeviceRemoveReason) NativeDevice->GetDeviceRemovedReason();

                return result switch
                {
                    DeviceRemoveReason.DeviceRemoved => GraphicsDeviceStatus.Removed,
                    DeviceRemoveReason.DeviceReset => GraphicsDeviceStatus.Reset,
                    DeviceRemoveReason.DeviceHung => GraphicsDeviceStatus.Hung,
                    DeviceRemoveReason.DriverInternalError => GraphicsDeviceStatus.InternalError,
                    DeviceRemoveReason.InvalidCall => GraphicsDeviceStatus.InvalidCall,

                    < 0 => GraphicsDeviceStatus.Reset,
                    _ => GraphicsDeviceStatus.Normal
                };
            }
        }

        #region Graphics device status codes

        // From DXGI_ERROR constants in Winerror.h
        private enum DeviceRemoveReason : int
        {
            None = 0,   // S_OK -- No error

            DeviceHung = unchecked((int) 0x887A0006),           // DEVICE_HUNG
            DeviceRemoved = unchecked((int) 0x887A0005),        // DEVICE_REMOVED
            DeviceReset = unchecked((int) 0x887A0007),          // DEVICE_RESET
            DriverInternalError = unchecked((int) 0x887A0020),  // DRIVER_INTERNAL_ERROR
            InvalidCall = unchecked((int) 0x887A0001)           // INVALID_CALL
        }

        #endregion

        /// <summary>
        ///     Gets the native device.
        /// </summary>
        /// <value>The native device.</value>
        internal ID3D12Device* NativeDevice => nativeDevice;

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
            ArgumentNullException.ThrowIfNull(commandLists);

            if (count > commandLists.Length)
                throw new ArgumentOutOfRangeException(nameof(count));

            var fenceValue = NextFenceValue++;

            // Recycle resources
            for (int index = 0; index < count; index++)
            {
                var commandList = commandLists[index];
                nativeCommandLists.Add(commandList.NativeCommandList);
                RecycleCommandListResources(commandList, fenceValue);
            }

            // Submit and signal the fence
            var commandListToExecute = (ID3D12CommandList*) nativeCommandLists.Items[0].CommandList;
            NativeCommandQueue->ExecuteCommandLists((uint) count, commandListToExecute);

            HResult result = NativeCommandQueue->Signal(nativeFence, fenceValue);

            if (result.IsFailure)
                result.Throw();

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

            rendererName = Adapter.Description;

            // Profiling is supported through PIX markers
            IsProfilingSupported = true;

            // Command lists are thread-safe and execute deferred
            IsDeferred = true;

            var d3d12 = D3D12.GetApi();

            bool isDebug = deviceCreationFlags.HasFlag(DeviceCreationFlags.Debug);
            if (isDebug)
                EnableDebugLayer();

            HResult result = default;

            // Create the Direct3D 12 Device with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                var graphicsProfile = graphicsProfiles[index];

                var level = graphicsProfile.ToFeatureLevel();

                // D3D12 supports only feature level 11+
                if (level < D3DFeatureLevel.Level110)
                    level = D3DFeatureLevel.Level110;

                var featureLevels = stackalloc D3DFeatureLevel[] { level };
                ID3D12Device* device = null;

                result = d3d12.CreateDevice((IUnknown*) Adapter.NativeAdapter, level, SilkMarshal.GuidPtrOf<ID3D12Device>(),
                                            (void**) &device);

                if (result.IsFailure)
                {
                    if (index == graphicsProfiles.Length - 1)
                        result.Throw();
                    else
                        continue;
                }

                nativeDevice = device;
                RequestedProfile = graphicsProfile;
                CurrentFeatureLevel = level;
                break;
            }

            // Describe and create the command queue
            var queueDesc = new CommandQueueDesc { Type = CommandListType.Direct };

            ID3D12CommandQueue* commandQueue;
            result = nativeDevice->CreateCommandQueue(in queueDesc, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(),
                                                      (void**) &commandQueue);

            if (result.IsFailure)
                result.Throw();

            NativeCommandQueue = commandQueue;

            //queueDesc.Type = CommandListType.Copy;
            ID3D12CommandQueue* copyQueue;
            result = nativeDevice->CreateCommandQueue(in queueDesc, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(),
                                                      (void**)&copyQueue);

            if (result.IsFailure)
                result.Throw();

            NativeCopyCommandQueue = copyQueue;

            ulong timestampFreq;
            NativeCommandQueue->GetTimestampFrequency(&timestampFreq);
            TimestampFrequency = timestampFreq;

            SrvHandleIncrementSize = (int) nativeDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            SamplerHandleIncrementSize = (int) nativeDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);

            if (isDebug)
            {
                ID3D12DebugDevice* debugDevice = null;
                result = nativeDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12DebugDevice>(), (void**) &debugDevice);

                if (result.IsSuccess && debugDevice is not null)
                {
                    ID3D12InfoQueue* infoQueue = null;
                    result = debugDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12InfoQueue>(), (void**) &infoQueue);

                    if (result.IsSuccess && infoQueue is not null)
                    {
                        var disabledMessages = stackalloc Silk.NET.Direct3D12.MessageID[]
                        {
                            // This happens when render target or depth stencil clear value is different
                            // than the provided ones during resource allocation.
                            Silk.NET.Direct3D12.MessageID.CleardepthstencilviewMismatchingclearvalue,
                            Silk.NET.Direct3D12.MessageID.ClearrendertargetviewMismatchingclearvalue,

                            // This occurs when there are uninitialized descriptors in a descriptor table,
                            // even when a shader does not access the missing descriptors.
                            Silk.NET.Direct3D12.MessageID.InvalidDescriptorHandle,

                            // These happen when capturing with VS diagnostics
                            Silk.NET.Direct3D12.MessageID.MapInvalidNullrange,
                            Silk.NET.Direct3D12.MessageID.UnmapInvalidNullrange
                        };

                        // Disable irrelevant debug layer warnings
                        Silk.NET.Direct3D12.InfoQueueFilter filter = new()
                        {
                            DenyList = new Silk.NET.Direct3D12.InfoQueueFilterDesc
                            {
                                NumIDs = 5,
                                PIDList = disabledMessages
                            }
                        };
                        infoQueue->AddStorageFilterEntries(ref filter);

                        //infoQueue->SetBreakOnSeverity(Silk.NET.Direct3D12.MessageSeverity.Error, true);
                        //infoQueue->SetBreakOnSeverity(Silk.NET.Direct3D12.MessageSeverity.Warning, true);

                        infoQueue->Release();
                    }
                    debugDevice->Release();
                }
            }

            // Prepare pools
            CommandAllocators = new CommandAllocatorPool(this);
            SrvHeaps = new HeapPool(this, SrvHeapSize, DescriptorHeapType.CbvSrvUav);
            SamplerHeaps = new HeapPool(this, SamplerHeapSize, DescriptorHeapType.Sampler);

            // Prepare descriptor allocators
            SamplerAllocator = new DescriptorAllocator(this, DescriptorHeapType.Sampler);
            ShaderResourceViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.CbvSrvUav);
            DepthStencilViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.Dsv);
            RenderTargetViewAllocator = new DescriptorAllocator(this, DescriptorHeapType.Rtv);

            // Prepare copy command list (start it closed, so that every new use start with a Reset)
            ID3D12CommandAllocator* commandAllocator;
            result = nativeDevice->CreateCommandAllocator(CommandListType.Direct, SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(),
                                                          (void**) &commandAllocator);

            if (result.IsFailure)
                result.Throw();

            NativeCopyCommandAllocator = commandAllocator;

            ID3D12GraphicsCommandList* commandList;
            result = nativeDevice->CreateCommandList(nodeMask: 0, CommandListType.Direct, commandAllocator, pInitialState: null,
                                                     SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**) &commandList);

            if (result.IsFailure)
                result.Throw();

            NativeCopyCommandList = commandList;

            commandList->Close();

            // Fence for next frame and resource cleaning
            ID3D12Fence* gfxFence, copyFence;
            result = nativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(),
                                               (void**) &gfxFence);
            if (result.IsFailure)
                result.Throw();

            result = nativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(),
                                               (void**) &copyFence);
            if (result.IsFailure)
                result.Throw();

            nativeFence = gfxFence;
            nativeCopyFence = copyFence;

            /// <summary>
            ///   Enables the Direct3D 12 debug layer if available.
            /// </summary>
            void EnableDebugLayer()
            {
                ID3D12Debug* debugInterface;
                HResult result = d3d12.GetDebugInterface(SilkMarshal.GuidPtrOf<ID3D12Debug>(), (void**) &debugInterface);

                if (result.IsSuccess && debugInterface is not null)
                {
                    debugInterface->EnableDebugLayer();
                }
            }
        }

        internal IntPtr AllocateUploadBuffer(int size, out ID3D12Resource* resource, out int offset, int alignment = 0)
        {
            // TODO D3D12 thread safety, should we simply use locks?

            // Align
            if (alignment > 0)
                nativeUploadBufferOffset = (nativeUploadBufferOffset + alignment - 1) / alignment * alignment;

            if (nativeUploadBuffer == null || (ulong)(nativeUploadBufferOffset + size) > nativeUploadBuffer->GetDesc().Width)
            {
                if (nativeUploadBuffer != null)
                {
                    nativeUploadBuffer->Unmap(Subresource: 0, pWrittenRange: null);
                    var nativeUploadBufferPtr = new ResourcePtr(nativeUploadBuffer);
                    TemporaryResources.Enqueue((NextFenceValue, nativeUploadBufferPtr));
                }

                // Allocate new buffer
                // TODO D3D12 recycle old ones (using fences to know when GPU is done with them)
                // TODO D3D12 ResourceStates.CopySource not working?
                var bufferSize = Math.Max(4 * 1024 * 1024, size);

                var heapProperties = new HeapProperties { Type = HeapType.Upload };
                var resourceDesc = new ResourceDesc
                {
                    Dimension = ResourceDimension.Buffer,
                    Width = (ulong) bufferSize,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Alignment = 0,

                    SampleDesc = { Count = 1, Quality = 0 },

                    Format = Format.FormatUnknown,
                    Layout = TextureLayout.LayoutRowMajor,
                    Flags = ResourceFlags.None
                };

                ID3D12Resource* uploadBuffer;
                HResult result = nativeDevice->CreateCommittedResource(in heapProperties, HeapFlags.None, in resourceDesc,
                                                                       ResourceStates.GenericRead, pOptimizedClearValue: null,
                                                                       SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**) &uploadBuffer);
                if (result.IsFailure)
                    result.Throw();

                nativeUploadBuffer = uploadBuffer;

                void* mappedBuffer;
                result = uploadBuffer->Map(Subresource: 0, pReadRange: null, &mappedBuffer);

                if (result.IsFailure)
                    result.Throw();

                nativeUploadBufferStart = (nint) mappedBuffer;
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
            var commandList = (ID3D12CommandList*) NativeCopyCommandList;
            NativeCommandQueue->ExecuteCommandLists(NumCommandLists: 1, in commandList);

            NativeCommandQueue->Signal(nativeCopyFence, nextCopyFenceValue);

            HResult result = NativeCommandQueue->Wait(nativeCopyFence, nextCopyFenceValue);

            if (result.IsFailure)
                result.Throw();

            nextCopyFenceValue++;
        }

        internal void ReleaseTemporaryResources()
        {
            lock (TemporaryResources)
            {
                // Release previous frame resources
                while (TemporaryResources.Count > 0 && IsFenceCompleteInternal(TemporaryResources.Peek().FenceValue))
                {
                    var temporaryResource = TemporaryResources.Dequeue().Resource;

                    if (temporaryResource is ResourcePtr comObject)
                    {
                        comObject.Resource->Release();
                    }
                    else if (temporaryResource is GraphicsResourceLink referenceLink)
                    {
                        referenceLink.ReferenceCount--;
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
            NativeCommandQueue->Signal(nativeFence, NextFenceValue);

            HResult result = NativeCommandQueue->Wait(nativeFence, NextFenceValue);

            if (result.IsFailure)
                result.Throw();

            // Release command queue
            NativeCommandQueue->Release();
            NativeCommandQueue = null;

            NativeCopyCommandQueue->Release();
            NativeCopyCommandQueue = null;

            NativeCopyCommandAllocator->Release();
            NativeCopyCommandList->Release();

            nativeUploadBuffer->Release();

            // Release temporary resources
            ReleaseTemporaryResources();

            nativeFence->Release();
            nativeFence = null;

            nativeCopyFence->Release();
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
                ID3D12DebugDevice* debugDevice = null;
                result = nativeDevice->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12DebugDevice>(), (void**) &debugDevice);

                if (result.IsSuccess && debugDevice != null)
                {
                    debugDevice->ReportLiveDeviceObjects(RldoFlags.Detail);
                    debugDevice->Release();
                }
            }

            nativeDevice->Release();
            nativeDevice = null;
        }

        internal void OnDestroyed()
        {
        }

        internal ulong ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            var fenceValue = NextFenceValue++;

            // Submit and signal fence
            var nativeCommandList = (ID3D12CommandList*) commandList.NativeCommandList;
            NativeCommandQueue->ExecuteCommandLists(NumCommandLists: 1, in nativeCommandList);

            NativeCommandQueue->Signal(nativeFence, fenceValue);

            // Recycle resources
            RecycleCommandListResources(commandList, fenceValue);

            return fenceValue;
        }

        private void RecycleCommandListResources(CompiledCommandList commandList, ulong fenceValue)
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

            var nativeCommandList  = commandList.NativeCommandList;
            commandList.Builder.NativeCommandLists.Enqueue(nativeCommandList);
            CommandAllocators.RecycleObject(fenceValue, commandList.NativeCommandAllocator);
        }

        internal bool IsFenceCompleteInternal(ulong fenceValue)
        {
            // Try to avoid checking the fence if possible
            if (fenceValue > lastCompletedFence)
                lastCompletedFence = Math.Max(lastCompletedFence, nativeFence->GetCompletedValue()); // Protect against race conditions

            return fenceValue <= lastCompletedFence;
        }

        internal void WaitForFenceInternal(ulong fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            // TODO D3D12 in case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue then first one
            lock (nativeFenceLock)
            {
                HResult result = nativeFence->SetEventOnCompletion(fenceValue, (void*) fenceEvent.SafeWaitHandle.DangerousGetHandle());

                if (result.IsFailure)
                    result.Throw();

                fenceEvent.WaitOne();
                lastCompletedFence = fenceValue;
            }
        }

        internal void TagResource(GraphicsResourceLink resourceLink)
        {
            if (resourceLink.Resource is Texture { Usage: GraphicsResourceUsage.Dynamic })
            {
                // Increase the reference count until GPU is done with the resource
                resourceLink.ReferenceCount++;
                TemporaryResources.Enqueue((NextFenceValue, resourceLink));
            }

            if (resourceLink.Resource is Buffer { Usage: GraphicsResourceUsage.Dynamic })
            {
                // Increase the reference count until GPU is done with the resource
                resourceLink.ReferenceCount++;
                TemporaryResources.Enqueue((NextFenceValue, resourceLink));
            }
        }

        internal abstract class ResourcePool<T> : IDisposable
            //where T : IComVtbl<ID3D12Pageable>
            where T : IDisposable
        {
            protected readonly GraphicsDevice GraphicsDevice;
            private readonly Queue<KeyValuePair<ulong, T>> liveObjects = new();

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
                        if (firstAllocator.Key <= GraphicsDevice.nativeFence->GetCompletedValue())
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

            public void RecycleObject(ulong fenceValue, T obj)
            {
                // TODO D3D12: SpinLock
                lock (liveObjects)
                {
                    liveObjects.Enqueue(new KeyValuePair<ulong, T>(fenceValue, obj));
                }
            }
        }

        internal class CommandAllocatorPool : ResourcePool<CommandAllocatorPtr>
        {
            public CommandAllocatorPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
            {
            }

            protected override CommandAllocatorPtr CreateObject()
            {
                // No allocator ready to be used, let's create a new one
                ID3D12CommandAllocator* commandAllocator;
                HResult result = GraphicsDevice.NativeDevice->CreateCommandAllocator(CommandListType.Direct,
                                                                                     SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(),
                                                                                     (void**) &commandAllocator);
                if (result.IsFailure)
                    result.Throw();

                return commandAllocator;
            }

            protected override void ResetObject(CommandAllocatorPtr obj)
            {
                HResult result = obj.Allocator->Reset();

                if (result.IsFailure)
                    result.Throw();
            }
        }

        internal class HeapPool : ResourcePool<HeapPtr>
        {
            private readonly int heapSize;
            private readonly DescriptorHeapType heapType;

            public HeapPool(GraphicsDevice graphicsDevice, int heapSize, DescriptorHeapType heapType) : base(graphicsDevice)
            {
                this.heapSize = heapSize;
                this.heapType = heapType;
            }

            protected override HeapPtr CreateObject()
            {
                // No allocator ready to be used, let's create a new one
                var descriptorHeapDesc = new DescriptorHeapDesc
                {
                    Flags = DescriptorHeapFlags.ShaderVisible,
                    Type = heapType,
                    NumDescriptors = (uint) heapSize
                };

                ID3D12DescriptorHeap* descriptorHeap;
                HResult result = GraphicsDevice.NativeDevice->CreateDescriptorHeap(in descriptorHeapDesc,
                                                                                   SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(),
                                                                                   (void**) &descriptorHeap);
                if (result.IsFailure)
                    result.Throw();

                return descriptorHeap;
            }

            protected override void ResetObject(HeapPtr obj)
            {
            }
        }

        #region CommandAllocatorPtr structure

        // Ancillary struct to store a Command Allocator without messing with its reference count (as ComPtr<T> does).
        internal readonly record struct CommandAllocatorPtr(ID3D12CommandAllocator* Allocator) : IDisposable
        {
            public static implicit operator ID3D12CommandAllocator*(CommandAllocatorPtr allocatorPtr) => allocatorPtr.Allocator;
            public static implicit operator CommandAllocatorPtr(ID3D12CommandAllocator* allocator) => new(allocator);

            public void Dispose()
            {
                if (Allocator != null)
                    Allocator->Release();
            }
        }

        #endregion

        /// <summary>
        /// Allocate descriptor handles. For now a simple bump alloc, but at some point we will have to make a real allocator with free
        /// </summary>
        internal class DescriptorAllocator : IDisposable
        {
            private const int DescriptorPerHeap = 256;

            private GraphicsDevice device;
            private DescriptorHeapType descriptorHeapType;
            private ID3D12DescriptorHeap* currentHeap;
            private CpuDescriptorHandle currentHandle;
            private int remainingHandles;
            private readonly int descriptorSize;

            public DescriptorAllocator(GraphicsDevice device, DescriptorHeapType descriptorHeapType)
            {
                this.device = device;
                this.descriptorHeapType = descriptorHeapType;

                descriptorSize = (int) device.NativeDevice->GetDescriptorHandleIncrementSize(descriptorHeapType);
            }

            public void Dispose()
            {
                if (currentHeap != null)
                    currentHeap->Release();

                currentHeap = null;
            }

            public CpuDescriptorHandle Allocate(int count)
            {
                if (currentHeap == null || remainingHandles < count)
                {
                    var descriptorHeapDesc = new DescriptorHeapDesc
                    {
                        Flags = DescriptorHeapFlags.None,
                        Type = descriptorHeapType,
                        NumDescriptors = DescriptorPerHeap,
                        NodeMask = 1
                    };

                    ID3D12DescriptorHeap* descriptorHeap;
                    HResult result = device.NativeDevice->CreateDescriptorHeap(in descriptorHeapDesc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(),
                                                                               (void**) &descriptorHeap);
                    if (result.IsFailure)
                        result.Throw();

                    currentHeap = descriptorHeap;

                    remainingHandles = DescriptorPerHeap;
                    currentHandle = descriptorHeap->GetCPUDescriptorHandleForHeapStart();
                }

                var resultHandle = currentHandle;

                currentHandle.Ptr += (nuint) descriptorSize;
                remainingHandles -= count;

                return resultHandle;
            }
        }
    }

    #region CommandListPtr structure

    // Ancillary struct to store a Command List without messing with its reference count (as ComPtr<T> does).
    internal readonly unsafe record struct CommandListPtr(ID3D12GraphicsCommandList* CommandList)
    {
        public static implicit operator ID3D12GraphicsCommandList*(CommandListPtr commandListPtr) => commandListPtr.CommandList;
        public static implicit operator CommandListPtr(ID3D12GraphicsCommandList* commandList) => new(commandList);
    }

    #endregion

    #region ResourcePtr structure

    // Ancillary struct to store a Resource without messing with its reference count (as ComPtr<T> does).
    internal readonly unsafe record struct ResourcePtr(ID3D12Resource* Resource)
    {
        public static implicit operator ID3D12Resource*(ResourcePtr resourcePtr) => resourcePtr.Resource;
        public static implicit operator ResourcePtr(ID3D12Resource* resource) => new(resource);
    }

    #endregion
}

#endif
