// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using Stride.Core;
using Stride.Core.Threading;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class GraphicsDevice
    {
        internal readonly int ConstantBufferDataPlacementAlignment = D3D12.ConstantBufferDataPlacementAlignment;

        private const GraphicsPlatform GraphicPlatform = GraphicsPlatform.Direct3D12;

        /// <summary>
        ///   Concurrent pool for lists of Graphics Resources that are used for staging operations.
        /// </summary>
        internal readonly ConcurrentPool<List<GraphicsResource>> StagingResourceLists = new(() => []);
        /// <summary>
        ///   Concurrent pool for lists of native Direct3D 12 Descriptor Heaps.
        /// </summary>
        internal readonly ConcurrentPool<List<ComPtr<ID3D12DescriptorHeap>>> DescriptorHeapLists = new(() => []);

        private bool simulateReset = false;
        private string rendererName;

        private ID3D12Device* nativeDevice;
        private ID3D12CommandQueue* nativeCommandQueue;

        /// <summary>
        ///   Gets the internal Direct3D 12 Device.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        public ComPtr<ID3D12Device> NativeDevice => ToComPtr(nativeDevice);

        /// <summary>
        ///   Gets the internal Direct3D 12 Command Queue.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12CommandQueue> NativeCommandQueue => ToComPtr(nativeCommandQueue);

        /// <summary>
        ///   The requested graphics profile for the Graphics Device.
        /// </summary>
        internal GraphicsProfile RequestedProfile;
        /// <summary>
        ///   The actual D3D feature level that the Graphics Device is using.
        /// </summary>
        internal D3DFeatureLevel CurrentFeatureLevel;

        private ID3D12CommandQueue* nativeCopyCommandQueue;

        /// <summary>
        ///   Gets the internal Direct3D 12 Command Queue used for copy commands.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12CommandQueue> NativeCopyCommandQueue => ToComPtr(nativeCopyCommandQueue);

        private ID3D12CommandAllocator* nativeCopyCommandAllocator;

        /// <summary>
        ///   Gets the internal Direct3D 12 Command Allocator used for copy commands.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12CommandAllocator> NativeCopyCommandAllocator => ToComPtr(nativeCopyCommandAllocator);

        private ID3D12GraphicsCommandList* nativeCopyCommandList;

        /// <summary>
        ///   Gets the internal Direct3D 12 Command List used for copy commands.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12GraphicsCommandList> NativeCopyCommandList => ToComPtr(nativeCopyCommandList);

        // Fence used to synchronize the Copy Command Queue
        private ID3D12Fence* nativeCopyFence;
        private ulong nextCopyFenceValue = 1;

        /// <summary>
        ///   Represents the size, in bytes, of the resource heap dedicated to Shader Resource Views.
        /// </summary>
        internal const int SrvHeapSize = 2048;
        /// <summary>
        ///   Represents the size, in bytes, of the resource heap dedicated to Samplers.
        /// </summary>
        internal const int SamplerHeapSize = 64;

        /// <summary>
        ///   A pool used to manage and reuse Command Allocators.
        /// </summary>
        /// <remarks>
        ///   This class provides functionality to allocate and recycle Command Allocators efficiently.
        ///   It helps reduce the overhead of creating new allocators by reusing existing ones.
        /// </remarks>
        internal CommandAllocatorPool CommandAllocators;
        /// <summary>
        ///   A pool used to manage and reuse Descriptor heaps intended for SRVs and UAVs.
        /// </summary>
        /// <remarks>
        ///   This class provides functionality to allocate and recycle Descriptor heaps for SRVs and UAVs efficiently.
        ///   It helps reduce the overhead of creating new heaps by reusing existing ones.
        /// </remarks>
        internal HeapPool SrvHeaps;
        /// <summary>
        ///   A pool used to manage and reuse Descriptor heaps intended for Samplers.
        /// </summary>
        /// <remarks>
        ///   This class provides functionality to allocate and recycle Descriptor heaps for Samplers efficiently.
        ///   It helps reduce the overhead of creating new heaps by reusing existing ones.
        /// </remarks>
        internal HeapPool SamplerHeaps;

        /// <summary>
        ///   Allocator for Sampler Descriptors.
        /// </summary>
        internal DescriptorAllocator SamplerAllocator;
        /// <summary>
        ///   Allocator for Descriptors for Shader Resource Views (SRVs).
        /// </summary>
        internal DescriptorAllocator ShaderResourceViewAllocator;
        /// <summary>
        ///   Allocator for Descriptors for Unordered Access Views (UAVs).
        /// </summary>
        internal DescriptorAllocator UnorderedAccessViewAllocator => ShaderResourceViewAllocator;
        /// <summary>
        ///   Allocator for Descriptors for Depth-Stencil Views (DSVs).
        /// </summary>
        internal DescriptorAllocator DepthStencilViewAllocator;
        /// <summary>
        ///   Allocator for Descriptors for Render Target Views (RTVs).
        /// </summary>
        internal DescriptorAllocator RenderTargetViewAllocator;

        // Buffer prepared for uploading data from the CPU so it can later be copied from that Buffer
        // to a destination Graphics Resource on the GPU.
        // It's a single large Buffer that is reused for every upload operation. Functions like a bump allocator
        // where the application can request a certain size and it is carved out of the large Buffer and returned.
        private ID3D12Resource* nativeUploadBuffer;
        private nint nativeUploadBufferMappedAddress;   // Start address of the upload buffer mapped to CPU memory
        private int nativeUploadBufferOffset;           // Offset in bytes from the start of the upload buffer where data can be written

        /// <summary>
        ///   The size in bytes of a Descriptor in a Descriptor heap for Shader Resource Views (SRVs), Unordered Acess Views (UAVs), etc.
        /// </summary>
        internal int SrvHandleIncrementSize;
        /// <summary>
        ///   The size in bytes of a Descriptor in a Descriptor heap for Samplers.
        /// </summary>
        internal int SamplerHandleIncrementSize;

        // Lock object for the graphics-related commands fence
        private readonly object nativeFenceLock = new();
        // Fence used to synchronize the Graphics Command Queue
        private ID3D12Fence* nativeFence;
        private ulong lastCompletedFence;

        /// <summary>
        ///   The next fence value used to synchronize the Graphics Command Queue operations.
        /// </summary>
        internal ulong NextFenceValue = 1;

        // An event used to signal when the fence has been completed
        private readonly AutoResetEvent fenceEvent = new(initialState: false);

        /// <summary>
        ///   Temporary or destroyed Graphics Resources that are kept around until the GPU doesn't need them anymore.
        /// </summary>
        internal Queue<(ulong FenceValue, object Resource)> TemporaryResources = new();

        /// <summary>
        ///   Gets the tick frquency of timestamp queries, in hertz.
        /// </summary>
        public long TimestampFrequency { get; private set; }

        /// <summary>
        ///   Gets the current status of the Graphics Device.
        /// </summary>
        public GraphicsDeviceStatus GraphicsDeviceStatus
        {
            get
            {
                if (simulateReset)
                {
                    simulateReset = false;
                    return GraphicsDeviceStatus.Reset;
                }

                var result = (DxgiConstants.DeviceRemoveReason) nativeDevice->GetDeviceRemovedReason();

                return result switch
                {
                    DxgiConstants.DeviceRemoveReason.DeviceRemoved => GraphicsDeviceStatus.Removed,
                    DxgiConstants.DeviceRemoveReason.DeviceReset => GraphicsDeviceStatus.Reset,
                    DxgiConstants.DeviceRemoveReason.DeviceHung => GraphicsDeviceStatus.Hung,
                    DxgiConstants.DeviceRemoveReason.DriverInternalError => GraphicsDeviceStatus.InternalError,
                    DxgiConstants.DeviceRemoveReason.InvalidCall => GraphicsDeviceStatus.InvalidCall,

                    < 0 => GraphicsDeviceStatus.Reset,
                    _ => GraphicsDeviceStatus.Normal
                };
            }
        }


        /// <summary>
        ///   Marks the Graphics Device Context as <strong>active</strong> on the current thread.
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
        public void EnableProfile(bool enabledFlag) { }  // TODO: Implement profiling with PIX markers? Currently, profiling is only implemented for OpenGL

        /// <summary>
        ///   Marks the Graphics Device Context as <strong>inactive</strong> on the current thread.
        /// </summary>
        public void End() { }

        /// <summary>
        ///   Executes a Compiled Command List.
        /// </summary>
        /// <param name="commandList">The Compiled Command List to execute.</param>
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been recorded for execution on the Graphics Device
        ///   at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
        /// </remarks>
        public void ExecuteCommandList(CompiledCommandList commandList)
        {
            ExecuteCommandListInternal(commandList);
        }

        /// <summary>
        ///   Executes multiple Compiled Command Lists.
        /// </summary>
        /// <param name="count">The number of Compiled Command Lists to execute.</param>
        /// <param name="commandLists">The Compiled Command Lists to execute.</param>
        /// <exception cref="ArgumentNullException"><paramref name="commandLists"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="count"/> is greater than the length of <paramref name="commandLists"/>.
        /// </exception>
        /// <remarks>
        ///   A Compiled Command List is a list of commands that have been recorded for execution on the Graphics Device
        ///   at a later time. This method executes the commands in the list. This is known as <em>deferred execution</em>.
        /// </remarks>
        public void ExecuteCommandLists(int count, CompiledCommandList[] commandLists)
        {
            ArgumentNullException.ThrowIfNull(commandLists);

            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, commandLists.Length);

            var fenceValue = NextFenceValue++;

            // Recycle resources
            var commandListToExecute = stackalloc ID3D12CommandList*[count];
            for (int index = 0; index < count; index++)
            {
                var commandList = commandLists[index];
                commandListToExecute[index] = commandList.NativeCommandList.AsComPtr<ID3D12GraphicsCommandList, ID3D12CommandList>();
                RecycleCommandListResources(commandList, fenceValue);
            }

            // Submit and signal the fence
            nativeCommandQueue->ExecuteCommandLists((uint) count, commandListToExecute);

            HResult result = nativeCommandQueue->Signal(nativeFence, fenceValue);

            if (result.IsFailure)
                result.Throw();

            ReleaseTemporaryResources();
        }

        /// <summary>
        ///   Sets the Graphics Device to simulate a situation in which the device is lost and then reset.
        /// </summary>
        public void SimulateReset()
        {
            simulateReset = true;
        }

        /// <summary>
        ///   Initializes the platform-specific features of the Graphics Device once it has been fully initialized.
        /// </summary>
        private partial void InitializePostFeatures() { }

        private partial string GetRendererName() => rendererName;

        /// <summary>
        ///   Initialize the platform-specific implementation of the Graphics Device.
        /// </summary>
        /// <param name="graphicsProfiles">A non-<see langword="null"/> list of the graphics profiles to try, in order of preference.</param>
        /// <param name="deviceCreationFlags">The device creation flags.</param>
        /// <param name="windowHandle">The window handle.</param>
        private partial void InitializePlatformDevice(GraphicsProfile[] graphicsProfiles, DeviceCreationFlags deviceCreationFlags, object windowHandle)
        {
            Debug.Assert(graphicsProfiles is not null && graphicsProfiles.Length > 0, "Graphics profiles must be provided and cannot be empty.");

            if (nativeDevice is not null)
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

            // The Debug Layer must be initialized before creating the device
            if (IsDebugMode)
                EnableDebugLayer();

            HResult result = default;

            // Create the Direct3D 12 Device with feature Level based on profile
            for (int index = 0; index < graphicsProfiles.Length; index++)
            {
                // Map GraphicsProfiles to D3D12 FeatureLevels
                var graphicsProfile = graphicsProfiles[index];
                var featureLevel = graphicsProfile.ToFeatureLevel();

                // D3D12 supports only feature level 11+
                if (featureLevel < D3DFeatureLevel.Level110)
                    featureLevel = D3DFeatureLevel.Level110;

                result = d3d12.CreateDevice(Adapter.NativeAdapter.AsIUnknown(), featureLevel, out ComPtr<ID3D12Device> device);

                if (result.IsFailure)
                {
                    if (index == graphicsProfiles.Length - 1)
                        result.Throw();
                    else
                        continue;
                }

                nativeDevice = device.DisposeBy(this);

                RequestedProfile = graphicsProfile;
                CurrentFeatureLevel = featureLevel;
                break;
            }

            // Describe and create the direct (graphics) command queue
            var queueDesc = new CommandQueueDesc { Type = CommandListType.Direct };

            result = nativeDevice->CreateCommandQueue(in queueDesc, out ComPtr<ID3D12CommandQueue> commandQueue);

            if (result.IsFailure)
                result.Throw();

            nativeCommandQueue = commandQueue.DisposeBy(this);

            // Describe and create the copy command queue
            queueDesc.Type = CommandListType.Copy;
            result = nativeDevice->CreateCommandQueue(in queueDesc, out ComPtr<ID3D12CommandQueue> copyQueue);

            if (result.IsFailure)
                result.Throw();

            nativeCopyCommandQueue = copyQueue.DisposeBy(this);

            // Get the tick frequency of the timestamp queries
            ulong timestampFreq = default;
            nativeCommandQueue->GetTimestampFrequency(ref timestampFreq);
            TimestampFrequency = (long) timestampFreq;

            // Cache the descriptor handle increment sizes
            SrvHandleIncrementSize = (int) nativeDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
            SamplerHandleIncrementSize = (int) nativeDevice->GetDescriptorHandleIncrementSize(DescriptorHeapType.Sampler);

            if (IsDebugMode)
            {
                // Query the debug device interface to disable some irrelevant warnings
                result = nativeDevice->QueryInterface(out ComPtr<ID3D12DebugDevice> debugDevice);

                if (result.IsSuccess && debugDevice.IsNotNull())
                {
                    result = debugDevice.QueryInterface(out ComPtr<ID3D12InfoQueue> infoQueue);

                    if (result.IsSuccess && infoQueue.IsNotNull())
                    {
                        var disabledMessages = stackalloc MessageID[]
                        {
                            // These happens when a Render Target's or Depth-Stencil Buffer's clear values are different
                            // than the provided ones during resource allocation
                            MessageID.CleardepthstencilviewMismatchingclearvalue,
                            MessageID.ClearrendertargetviewMismatchingclearvalue,

                            // This occurs when there are uninitialized Descriptors in a Descriptor Table,
                            // even when a Shader does not access the missing descriptors
                            MessageID.InvalidDescriptorHandle,

                            // These happen when capturing with VS diagnostics
                            MessageID.MapInvalidNullrange,
                            MessageID.UnmapInvalidNullrange
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
                        infoQueue.AddStorageFilterEntries(ref filter);

                        //infoQueue.SetBreakOnSeverity(Silk.NET.Direct3D12.MessageSeverity.Error, true);
                        //infoQueue.SetBreakOnSeverity(Silk.NET.Direct3D12.MessageSeverity.Warning, true);

                        infoQueue.Release();
                    }
                    debugDevice.Release();
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
            result = nativeDevice->CreateCommandAllocator(CommandListType.Copy, out ComPtr<ID3D12CommandAllocator> commandAllocator);

            if (result.IsFailure)
                result.Throw();

            nativeCopyCommandAllocator = commandAllocator.DisposeBy(this);

            result = nativeDevice->CreateCommandList(nodeMask: 0, CommandListType.Copy, commandAllocator, pInitialState: ref NullRef<ID3D12PipelineState>(),
                                                     out ComPtr<ID3D12GraphicsCommandList> commandList);
            if (result.IsFailure)
                result.Throw();

            nativeCopyCommandList = commandList.DisposeBy(this);

            commandList.Close();

            // Fences for next frame and resource cleaning
            result = nativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, out ComPtr<ID3D12Fence> gfxFence);

            if (result.IsFailure)
                result.Throw();

            result = nativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, out ComPtr<ID3D12Fence> copyFence);

            if (result.IsFailure)
                result.Throw();

            nativeFence = gfxFence.DisposeBy(this);
            nativeCopyFence = copyFence.DisposeBy(this);

            //
            // Enables the Direct3D 12 debug layer if available.
            //
            void EnableDebugLayer()
            {
                HResult result = d3d12.GetDebugInterface(out ComPtr<ID3D12Debug> debugInterface);

                if (result.IsSuccess && debugInterface.IsNotNull())
                {
                    debugInterface.EnableDebugLayer();
                    debugInterface.Release();
                }
            }
        }

        /// <summary>
        ///   Allocates (or reuses) a Buffer prepared for uploading data from the CPU so it can later
        ///   be copied from that Buffer to a destination Graphics Resource on the GPU.
        /// </summary>
        /// <param name="size">The size of the requested upload buffer, in bytes.</param>
        /// <param name="resource">When the method returns, contains a pointer to the allocated upload Buffer.</param>
        /// <param name="offset">
        ///   When the method returns, contains the offset in bytes from the start of <paramref name="resource"/>
        ///   where data can be written to.
        /// </param>
        /// <param name="alignment">Optional alignment requisites on the returned writeable address.</param>
        /// <returns>
        ///   A pointer to the mapped Buffer memory that can be written. It already takes into account the
        ///   <paramref name="offset"/>.
        /// </returns>
        internal IntPtr AllocateUploadBuffer(int size, out ComPtr<ID3D12Resource> resource, out int offset, int alignment = 0)
        {
            // TODO: D3D12: Thread safety, should we simply use locks?

            Debug.Assert(size > 0, "Size must be greater than zero.");
            Debug.Assert(alignment >= 0, "Alignment must be zero or greater.");

            // Ensure the correct alignment for the offset in the current upload buffer
            if (alignment > 0)
                nativeUploadBufferOffset = (nativeUploadBufferOffset + alignment - 1) / alignment * alignment;

            // If we have no upload buffer ready or its size is insufficient
            if (nativeUploadBuffer is null || (ulong)(nativeUploadBufferOffset + size) > nativeUploadBuffer->GetDesc().Width)
            {
                // Unmap any upload buffer and put it in temporary resources that may be disposed in the future
                if (nativeUploadBuffer is not null)
                {
                    nativeUploadBuffer->Unmap(Subresource: 0, pWrittenRange: null);

                    TemporaryResources.Enqueue((NextFenceValue, ToComPtr(nativeUploadBuffer)));
                    // TODO: Keep a separate temporary resource list for COM pointers to avoid boxing
                }

                // Allocate new buffer
                // TODO: D3D12: Recycle old ones (using fences to know when GPU is done with them)
                // TODO: D3D12: ResourceStates.CopySource not working?

                var bufferSize = Math.Max(4 * 1024 * 1024, size); // 4 MB minimum size

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

                HResult result = nativeDevice->CreateCommittedResource(in heapProperties, HeapFlags.None, in resourceDesc,
                                                                       ResourceStates.GenericRead, pOptimizedClearValue: null,
                                                                       out ComPtr<ID3D12Resource> uploadBuffer);
                if (result.IsFailure)
                    result.Throw();

                nativeUploadBuffer = uploadBuffer.DisposeBy(this);

                void* mappedBufferAddress = null;
                result = uploadBuffer.Map(Subresource: 0, pReadRange: in NullRef<Silk.NET.Direct3D12.Range>(), ref mappedBufferAddress);

                if (result.IsFailure)
                    result.Throw();

                nativeUploadBufferMappedAddress = (nint) mappedBufferAddress;
                nativeUploadBufferOffset = 0;
            }

            // Bump allocate
            resource = ToComPtr(nativeUploadBuffer);
            offset = nativeUploadBufferOffset;
            nativeUploadBufferOffset += size;
            return nativeUploadBufferMappedAddress + offset;
        }

        /// <summary>
        ///   Waits for the Command Queue for copy commands to complete execution of the current Command List.
        /// </summary>
        /// <remarks>
        ///   This method ensures that all commands submitted to the copy queue are fully executed
        ///   before proceeding. It signals the associated fence and waits for its completion,
        ///   throwing an exception if the operation fails.
        /// </remarks>
        internal void WaitCopyQueue()
        {
            var commandList = (ID3D12CommandList*) nativeCopyCommandList;
            nativeCopyCommandQueue->ExecuteCommandLists(NumCommandLists: 1, in commandList);

            nativeCopyCommandQueue->Signal(nativeCopyFence, nextCopyFenceValue);

            HResult result = nativeCopyCommandQueue->Wait(nativeCopyFence, nextCopyFenceValue);

            if (result.IsFailure)
                result.Throw();

            nextCopyFenceValue++;
        }

        /// <summary>
        ///   Releases and removes Graphics Resources that were marked for deletion but put temporarily on hold
        ///   until the GPU is done with them.
        /// </summary>
        /// <remarks>
        ///   This method removes and releases the resources if they are determined to be complete based
        ///   on their associated fence values. If they have been signaled as complete, they are released.
        ///   This is performed in a thread-safe manner.
        /// </remarks>
        internal void ReleaseTemporaryResources()
        {
            lock (TemporaryResources)
            {
                // Release previous frame resources
                while (TemporaryResources.Count > 0 && IsFenceCompleteInternal(TemporaryResources.Peek().FenceValue))
                {
                    var temporaryResource = TemporaryResources.Dequeue().Resource;

                    if (temporaryResource is ComPtr<ID3D12Resource> resource)
                    {
                        resource.Release();
                    }
                    else if (temporaryResource is GraphicsResourceLink referenceLink)
                    {
                        referenceLink.ReferenceCount--;
                    }
                }
            }
        }

        /// <summary>
        ///   Makes Direct3D 12-specific adjustments to the Pipeline State objects created by the Graphics Device.
        /// </summary>
        /// <param name="pipelineStateDescription">A Pipeline State description that can be modified and adjusted.</param>
        private partial void AdjustDefaultPipelineStateDescription(ref PipelineStateDescription pipelineStateDescription) { }

        /// <summary>
        ///   Releases the platform-specific Graphics Device and all its associated resources.
        /// </summary>
        protected partial void DestroyPlatformDevice()
        {
            ReleaseDevice();
        }

        /// <summary>
        ///   Disposes the Direct3D 12 Device and all its associated resources.
        /// </summary>
        private void ReleaseDevice()
        {
            // Wait for completion of everything queued
            nativeCommandQueue->Signal(nativeFence, NextFenceValue);

            HResult result = nativeCommandQueue->Wait(nativeFence, NextFenceValue);

            if (result.IsFailure)
                result.Throw();

            nativeCopyCommandQueue->Signal(nativeCopyFence, nextCopyFenceValue);

            result = nativeCopyCommandQueue->Wait(nativeCopyFence, nextCopyFenceValue);

            if (result.IsFailure)
                result.Throw();

            // Release command queue
            SafeRelease(ref nativeCommandQueue);
            NativeCommandQueue.RemoveDisposeBy(this);

            SafeRelease(ref nativeCopyCommandQueue);
            NativeCopyCommandQueue.RemoveDisposeBy(this);
            SafeRelease(ref nativeCopyCommandAllocator);
            NativeCopyCommandAllocator.RemoveDisposeBy(this);
            SafeRelease(ref nativeCopyCommandList);
            NativeCopyCommandList.RemoveDisposeBy(this);

            SafeRelease(ref nativeUploadBuffer);

            // Release temporary resources
            ReleaseTemporaryResources();

            SafeRelease(ref nativeFence);
            ToComPtr(nativeFence).RemoveDisposeBy(this);
            SafeRelease(ref nativeCopyFence);
            ToComPtr(nativeCopyFence).RemoveDisposeBy(this);

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
                result = nativeDevice->QueryInterface(out ComPtr<ID3D12DebugDevice> debugDevice);

                if (result.IsSuccess && debugDevice.IsNotNull())
                {
                    debugDevice.ReportLiveDeviceObjects(RldoFlags.Detail);
                    debugDevice.Release();
                }
            }

            SafeRelease(ref nativeDevice);
            NativeDevice.RemoveDisposeBy(this);
        }

        /// <summary>
        ///   Called when the Graphics Device is being destroyed.
        /// </summary>
        internal void OnDestroyed()
        {
        }


        /// <summary>
        ///   Executes a Compiled Command List.
        /// </summary>
        /// <param name="commandList">
        ///   The Compiled Command List to execute.
        /// </param>
        /// <returns>
        ///   The fence value associated with the execution of the Command List.
        ///   This value can be used to track its completion.
        /// </returns>
        internal ulong ExecuteCommandListInternal(CompiledCommandList commandList)
        {
            var fenceValue = NextFenceValue++;

            // Submit and signal fence
            var nativeCommandList = commandList.NativeCommandList.AsComPtr<ID3D12GraphicsCommandList, ID3D12CommandList>();
            nativeCommandQueue->ExecuteCommandLists(NumCommandLists: 1, ref nativeCommandList);

            nativeCommandQueue->Signal(nativeFence, fenceValue);

            // Recycle resources
            RecycleCommandListResources(commandList, fenceValue);

            return fenceValue;
        }

        /// <summary>
        ///   Recycles the resources associated with a Compiled Command List, making them available for reuse.
        /// </summary>
        /// <param name="commandList">The Compiled Command List whose resources are to be recycled.</param>
        /// <param name="fenceValue">
        ///   The fence value associated with the Command List, used to track resource usage and ensure proper
        ///   synchronization.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     This method releases and clears staging Graphics Resources, Descriptor heaps, and other
        ///     resources associated with the specified Compiled Command List.
        ///   </para>
        ///   <para>
        ///     It also enqueues the internal native Command List for reuse and recycles the Command Allocator.
        ///   </para>
        ///   <para>
        ///     Callers should ensure that the specified <paramref name="fenceValue"/> accurately reflects
        ///     the point at which the resources are no longer in use to avoid synchronization issues.
        ///   </para>
        /// </remarks>
        private void RecycleCommandListResources(CompiledCommandList commandList, ulong fenceValue)
        {
            // Set fence on staging textures
            foreach (var stagingResource in commandList.StagingResources)
            {
                stagingResource.StagingFenceValue = fenceValue;
            }

            StagingResourceLists.Release(commandList.StagingResources);
            commandList.StagingResources.Clear();

            // Recycle resources (SRVs, UAVs, Samplers, etc.)
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

        /// <summary>
        ///   Determines whether the specified fence value has been completed by the graphics Command Queue.
        /// </summary>
        /// <param name="fenceValue">The fence value to check for completion.</param>
        /// <returns>
        ///   <see langword="true"/> if the specified fence value has been completed;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   This method checks the completion status of a fence value by comparing it to the last
        ///   known completed fence value. It ensures thread safety by updating the last completed
        ///   fence value when necessary.
        /// </remarks>
        internal bool IsFenceCompleteInternal(ulong fenceValue)
        {
            // Try to avoid checking the fence if possible
            if (fenceValue > lastCompletedFence)
                lastCompletedFence = Math.Max(lastCompletedFence, nativeFence->GetCompletedValue()); // Protect against race conditions

            return fenceValue <= lastCompletedFence;
        }

        /// <summary>
        ///   Waits for the specified fence value to be signaled by the graphics Command Queue,
        ///   blocking the calling thread if necessary.
        /// </summary>
        /// <param name="fenceValue">
        ///   The fence value to wait for. Must be greater than the last completed fence value.
        /// </param>
        /// <remarks>
        ///   <para>
        ///     This method ensures that the specified fence value has been reached before continuing execution.
        ///     If the fence value is already complete, the method returns immediately. Otherwise, it blocks the
        ///     calling thread until the fence value is signaled.
        ///   </para>
        ///   <para>
        ///     Note that this method uses a lock to synchronize access to the underlying native fence,
        ///     which may cause contention in multithreaded scenarios if multiple threads are waiting
        ///     on different fence values.
        ///   </para>
        /// </remarks>
        internal void WaitForFenceInternal(ulong fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            // TODO: D3D12: in case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue then first one
            lock (nativeFenceLock)
            {
                var waitHandle = fenceEvent.SafeWaitHandle.DangerousGetHandle();
                HResult result = nativeFence->SetEventOnCompletion(fenceValue, (void*) waitHandle);

                if (result.IsFailure)
                    result.Throw();

                fenceEvent.WaitOne();
                lastCompletedFence = fenceValue;
            }
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
            Debug.Assert(resourceLink is { Resource: Texture or Buffer }, "Resource link cannot be null, and must be a Texture or a Buffer.");

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
    }
}

#endif
