// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Threading;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics;

public unsafe partial class GraphicsDevice
{
    /// <summary>
    ///   Provides helper functionality for managing and synchronizing Direct3D 12 fence operations,
    ///   enabling CPU-GPU synchronization and signaling within a Graphics Device context.
    /// </summary>
    /// <remarks>
    ///   This structure implements the <see cref="IDisposable"/> interface to allow usage within
    ///   a C# <see langword="using"/> statement. When disposed, it releases the internal Direct3D 12 fence.
    ///   <para/>
    ///   If used without the <see langword="using"/> statement, calling code should not forget to
    ///   call <see cref="Dispose"/> to release the Direct3D 12 fence.
    /// </remarks>
    internal struct FenceHelper : IDisposable
    {
        private readonly object lockObject = new();

        /// <summary>
        ///   Gets the internal Direct3D 12 Fence.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        public readonly ComPtr<ID3D12Fence> Fence => ToComPtr(fence);
        private readonly ID3D12Fence* fence;

        /// <summary>
        ///   The next fence value used to synchronize operations with the <see cref="FenceHelper"/>.
        /// </summary>
        public ulong NextFenceValue = 1;
        /// <summary>
        ///   The last completed fence value signaled by the GPU.
        /// </summary>
        public ulong LastCompletedFence;

        // An event used to signal when the fence has been completed
        [ThreadStatic]
        private static AutoResetEvent fenceEvent;


        /// <summary>
        ///   Initializes a new instance of the <see cref="FenceHelper"/> structure, creating
        ///   a Direct3D 12 fence for GPU synchronization.
        /// </summary>
        /// <param name="graphicsDevice">
        ///   The Graphics Device used to create the underlying Direct3D 12 fence. Cannot be <see langword="null"/>.
        /// </param>
        /// <remarks>The fence is initialized with a value of 0.</remarks>
        public FenceHelper(GraphicsDevice graphicsDevice)
        {
            // Fences for next frame and resource cleaning
            HResult result = graphicsDevice.nativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, out ComPtr<ID3D12Fence> gfxFence);

            if (result.IsFailure)
                result.Throw();

            fence = gfxFence.Handle;
        }


        /// <summary>
        ///   Determines whether the specified fence value has been completed by a Command Queue.
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
            if (fenceValue > LastCompletedFence)
                LastCompletedFence = Math.Max(LastCompletedFence, fence->GetCompletedValue()); // Protect against race conditions

            return fenceValue <= LastCompletedFence;
        }

        /// <summary>
        ///   Waits for the specified fence value to be signaled by a Command Queue,
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
        ///     which may cause contention in multi-threaded scenarios if multiple threads are waiting
        ///     on different fence values.
        ///   </para>
        /// </remarks>
        internal void WaitForFenceCPUInternal(ulong fenceValue)
        {
            if (IsFenceCompleteInternal(fenceValue))
                return;

            // TODO: D3D12: In case of concurrency, this lock could end up blocking too long a second thread with lower fenceValue than the first one
            lock (lockObject)
            {
                var localFenceEvent = fenceEvent ??= new AutoResetEvent(initialState: false);

                fence->SetEventOnCompletion(fenceValue, (void*) localFenceEvent.GetSafeWaitHandle().DangerousGetHandle());
                localFenceEvent.WaitOne();
                LastCompletedFence = fenceValue;
            }
        }

        /// <summary>
        ///   Sets the fence to the specified value from the GPU (i.e., using a Command Queue).
        /// </summary>
        /// <param name="nativeCommandQueue">The Command Queue to signal the fence.</param>
        /// <param name="fenceValue">The fence value to set.</param>
        internal readonly void Signal(ID3D12CommandQueue* nativeCommandQueue, ulong fenceValue)
        {
            HResult result = nativeCommandQueue->Signal(fence, fenceValue);

            if (result.IsFailure)
                result.Throw();
        }

        /// <summary>
        ///   Queues a GPU-side wait, and returns immediately.
        ///   A GPU-side wait is where the GPU waits until the specified fence reaches or exceeds the specified value.
        /// </summary>
        /// <param name="nativeCommandQueue">The Command Queue to wait the fence.</param>
        /// <param name="fenceValue">The fence value to wait for.</param>
        internal readonly void Wait(ID3D12CommandQueue* nativeCommandQueue, ulong fenceValue)
        {
            HResult result = nativeCommandQueue->Wait(fence, fenceValue);

            if (result.IsFailure)
                result.Throw();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            fence->Release();
        }
    }
}
#endif
