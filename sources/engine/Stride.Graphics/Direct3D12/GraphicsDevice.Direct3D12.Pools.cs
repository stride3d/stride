// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Threading;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics;

public unsafe partial class GraphicsDevice
{
    /// <summary>
    ///   Represents a pool of reusable Direct3D 12 resources.
    /// </summary>
    /// <typeparam name="T">
    ///   The type of resource managed by the pool. This type must be a COM type implementing the
    ///   <see cref="ID3D12Pageable"/> interface.
    /// </typeparam>
    /// <remarks>
    ///   <para>
    ///     This class provides functionality for managing a pool of reusable Direct3D 12 resources
    ///     to optimize resource allocation and reuse.
    ///   </para>
    ///   <para>
    ///     Derived classes must implement the <see cref="CreateObject"/> and <see cref="ResetObject"/> methods
    ///     to define how resources are created and reset for reuse.
    ///   </para>
    ///   <para>
    ///     This class is thread-safe and ensures proper synchronization when accessing the pool.
    ///   </para>
    /// </remarks>
    internal abstract class ResourcePool<T> : IDisposable
        where T : unmanaged, IComVtbl<ID3D12Pageable>, IComVtbl<T>
    {
        // A queue to hold live objects that will be reused when it is safe to do so
        private readonly Queue<LiveObject> liveObjects = new();

        // Indicates whether the pool should be accessed in a thread-safe manner
        private readonly bool threadSafe;

        /// <summary>
        ///   Gets the Graphics Device associated with the resource pool.
        /// </summary>
        protected GraphicsDevice GraphicsDevice { get; }


        /// <summary>
        ///   Initializes a new instance of the <see cref="ResourcePool{T}"/> class.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device to associate with this resource pool.</param>
        /// <param name="threadSafe">
        ///   A value indicating whether the pool should be accessed in a thread-safe manner.
        ///   If <see langword="true"/>, access to the pool will be synchronized using locks.
        /// </param>
        protected ResourcePool(GraphicsDevice graphicsDevice, bool threadSafe)
        {
            GraphicsDevice = graphicsDevice;
            this.threadSafe = threadSafe;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            using (OptionalLock.Lock(liveObjects, useLock: threadSafe))
            {
                foreach (var (_, liveObject) in liveObjects)
                {
                    liveObject.Dispose();
                }
                liveObjects.Clear();
            }
        }


        /// <summary>
        ///   Retrieves an object from the pool, resetting it for reuse if necessary.
        /// </summary>
        /// <param name="completedFenceValue">
        ///   The fence value to determine if objects to return are safe to use (ready to be reused),
        ///   inticating the GPU is not doing any work with them.
        /// </param>
        /// <returns>
        ///   A COM pointer to the retrieved object. If no reusable object is available, a new
        ///   object is created and returned.
        /// </returns>
        /// <remarks>
        ///   If a previously used object is available and its associated fence value indicates it
        ///   is ready for reuse, the method resets and returns that object.
        ///   Otherwise, a new object is created and returned.
        /// </remarks>
        public ComPtr<T> GetObject(ulong completedFenceValue)
        {
            // TODO: D3D12: SpinLock
            using (OptionalLock.Lock(liveObjects, useLock: threadSafe))
            {
                // Check if first pooled object is ready for reuse
                if (liveObjects.TryPeek(out LiveObject liveObject))
                {
                    if (liveObject.FenceValue <= completedFenceValue)
                    {
                        liveObjects.Dequeue();
                        var reusableObject = liveObject.Object;
                        ResetObject(reusableObject);

                        if (reusableObject.IsNull())
                        {
                            // TODO: Incomplete? CreateObject?
                        }

                        return reusableObject;
                    }
                }

                // No pooled object ready to be used, let's create a new one
                return CreateObject();
            }
        }

        /// <summary>
        ///   Creates and returns a new object.
        /// </summary>
        /// <returns>A new instance of object of type <typeparamref name="T"/>.</returns>
        /// <remarks>
        ///   This method is abstract and must be implemented by a derived class to define how the
        ///   object of type <typeparamref name="T"/> is created.
        /// </remarks>
        protected abstract ComPtr<T> CreateObject();

        /// <summary>
        ///   Resets the specified object to its initial state so it can be reused.
        /// </summary>
        /// <param name="obj">A reference to the object to reset.</param>
        /// <remarks>
        ///   This method is intended to be implemented by derived classes to define the specific
        ///   logic for resetting the object.
        /// </remarks>
        protected abstract void ResetObject(ComPtr<T> obj);

        /// <summary>
        ///   Recycles an object for future reuse once the specified fence value is reached.
        /// </summary>
        /// <param name="fenceValue">The fence value that must be reached before the object can be reused.</param>
        /// <param name="obj">The object to be recycled. This object will be enqueued for reuse.</param>
        /// <remarks>
        ///   This method is thread-safe and ensures that the object is added to the queue for
        ///   reuse in a synchronized manner.
        /// </remarks>
        public void RecycleObject(ulong fenceValue, ComPtr<T> obj)
        {
            // TODO D3D12: SpinLock
            using (OptionalLock.Lock(liveObjects, useLock: threadSafe))
            {
                // Enqueue for reuse when the fence value is reached
                liveObjects.Enqueue(new LiveObject(fenceValue, obj));
            }
        }

        #region LiveObject structure

        /// <summary>
        ///   A live object with an associated fence value.
        /// </summary>
        /// <param name="FenceValue">The fence value that must be reached before the object can be reused.</param>
        /// <param name="Object">The COM pointer to the object.</param>
        private readonly record struct LiveObject(ulong FenceValue, ComPtr<T> Object) : IDisposable
        {
            /// <inheritdoc/>
            public void Dispose()
            {
                if (Object.IsNotNull())
                    Object.Dispose();
            }
        }

        #endregion

        #region OptionalLock structure

        // TODO: Do we want to use spinlock instead? (need to measure impact, not good if too long wait)

        /// <summary>
        ///   Provides a disposable value type that conditionally acquires and releases a monitor lock
        ///   on a specified object, enabling optional locking patterns (for example, based on runtime conditions).
        /// </summary>
        /// <remarks>
        ///   This structure implements the <see cref="IDisposable"/> interface to allow usage within
        ///   a C# <see langword="using"/> statement. When disposed, it releases the lock if it was acquired.
        ///   <para/>
        ///   If used without the <see langword="using"/> statement, calling code should not forget to
        ///   call <see cref="Dispose"/> to release the lock if it was acquired.
        /// </remarks>
        private readonly struct OptionalLock : IDisposable
        {
            private readonly object lockObject;
            private readonly bool locked;

            // Use a private constructor to force usage through the static factory methods
            private OptionalLock(object lockObject, bool locked)
            {
                this.lockObject = lockObject;
                this.locked = locked;
            }

            /// <inheritdoc/>
            public readonly void Dispose()
            {
                if (locked)
                {
                    Monitor.Exit(lockObject);
                }
            }

            /// <summary>
            ///   Acquires an optional lock on the specified object and returns an
            ///   <see cref="OptionalLock"/> instance representing the lock state.
            /// </summary>
            /// <param name="lockObject">
            ///   The object on which to acquire the lock. Cannot be <see langword="null"/>.
            /// </param>
            /// <param name="useLock">
            ///   A value indicating whether to acquire the lock.
            ///   If <see langword="true"/>, the method attempts to enter a monitor lock on <paramref name="lockObject"/>;
            ///   otherwise, no lock is acquired.
            /// </param>
            /// <returns>An <see cref="OptionalLock"/> instance that represents the acquired lock.</returns>
            public static OptionalLock Lock(object lockObject, bool useLock)
            {
                if (useLock)
                {
                    useLock = false;
                    Monitor.Enter(lockObject, ref useLock);
                }
                return new OptionalLock(lockObject, useLock);
            }
        }

        #endregion
    }

    /// <summary>
    ///   Internal pool of reusable <see cref="ID3D12CommandAllocator"/>s.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device to associate with this resource pool.</param>
    /// <param name="threadSafe">
    ///   A value indicating whether the pool should be accessed in a thread-safe manner.
    ///   If <see langword="true"/>, access to the pool will be synchronized using locks.
    /// </param>
    /// <remarks>
    ///   This class manages the lifecycle of <see cref="ID3D12CommandAllocator"/> instances, creating
    ///   new allocators as needed and resetting them for reuse. It is designed to optimize resource
    ///   usage in scenarios where multiple command allocators are required.
    /// </remarks>
    internal class CommandAllocatorPool(GraphicsDevice graphicsDevice, bool threadSafe)
        : ResourcePool<ID3D12CommandAllocator>(graphicsDevice, threadSafe)
    {
        /// <inheritdoc/>
        protected override ComPtr<ID3D12CommandAllocator> CreateObject()
        {
            // No Command Allocator ready to be used, let's create a new one
            HResult result = GraphicsDevice.NativeDevice.CreateCommandAllocator(CommandListType.Direct, out ComPtr<ID3D12CommandAllocator> commandAllocator);

            if (result.IsFailure)
                result.Throw();

            return commandAllocator;
        }

        /// <inheritdoc/>
        protected override void ResetObject(ComPtr<ID3D12CommandAllocator> obj)
        {
            // Reset the Command Allocator to prepare it for reuse
            HResult result = obj.Reset();

            if (result.IsFailure)
                result.Throw();
        }
    }


    /// <summary>
    ///   Internal pool of reusable <see cref="ID3D12DescriptorHeap"/>s.
    /// </summary>
    /// <param name="graphicsDevice">The Graphics Device to associate with this resource pool.</param>
    /// <param name="threadSafe">
    ///   A value indicating whether the pool should be accessed in a thread-safe manner.
    ///   If <see langword="true"/>, access to the pool will be synchronized using locks.
    /// </param>
    /// <param name="heapSize">The number of Descriptors for the pooled Descriptor heaps.</param>
    /// <param name="heapType">The type of the pooled Descriptor heaps.</param>
    /// <remarks>
    ///   This class manages the lifecycle of <see cref="ID3D12DescriptorHeap"/> instances, creating
    ///   new Descriptor Heaps as needed and resetting them for reuse. It is designed to optimize resource
    ///   usage in scenarios where multiple Descriptor Heaps are required.
    /// </remarks>
    internal class HeapPool(GraphicsDevice graphicsDevice, bool threadSafe, int heapSize, DescriptorHeapType heapType)
        : ResourcePool<ID3D12DescriptorHeap>(graphicsDevice, threadSafe)
    {
        private readonly int heapSize = heapSize;
        private readonly DescriptorHeapType heapType = heapType;


        /// <inheritdoc/>
        protected override ComPtr<ID3D12DescriptorHeap> CreateObject()
        {
            // No heap ready to be used, let's create a new one
            var descriptorHeapDesc = new DescriptorHeapDesc
            {
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = heapType,
                NumDescriptors = (uint) heapSize
            };

            HResult result = GraphicsDevice.NativeDevice.CreateDescriptorHeap(in descriptorHeapDesc,
                                                                              out ComPtr<ID3D12DescriptorHeap> descriptorHeap);
            if (result.IsFailure)
                result.Throw();

            return descriptorHeap;
        }

        /// <inheritdoc/>
        protected override void ResetObject(ComPtr<ID3D12DescriptorHeap> obj) { }
    }
}

#endif
