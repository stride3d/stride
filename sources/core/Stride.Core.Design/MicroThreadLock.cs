// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Stride.Core.Annotations;
using Stride.Core.MicroThreading;

namespace Stride.Core
{
    /// <summary>
    /// An hybrid lock that allows to do asynchrounous work when acquired from a <see cref="MicroThread"/>, and still allow to await for acquisition out of a
    /// microthread. This lock support re-entrancy.
    /// </summary>
    public class MicroThreadLock : IDisposable
    {
        private readonly MicroThreadLocal<MicroThreadAsyncLock> asyncLocks = new MicroThreadLocal<MicroThreadAsyncLock>();
        private readonly Queue<MicroThreadLockBase> lockQueue = new Queue<MicroThreadLockBase>();
        private readonly object syncLock = new object();
        private int currentSyncLockThread;
        private MicroThreadSyncLock currentSyncLock;
        private bool isDisposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(MicroThreadLock));
            isDisposed = true;
        }

        /// <summary>
        /// Reserves the lock in order to use synchronous locking. The lock will be bound to the calling thread and therefore should be released on the same thread.
        /// </summary>
        /// <returns>A task that completes when the lock is reserved. The result of the task is an <see cref="ISyncLockable"/> object allowing to do the lock.</returns>
        public async Task<ISyncLockable> ReserveSyncLock()
        {
            if (isDisposed) throw new ObjectDisposedException(nameof(MicroThreadLock));

            // If we already acquired the lock in this thread, we're just re-entering
            if (currentSyncLockThread == Thread.CurrentThread.ManagedThreadId)
            {
                var currentLock = currentSyncLock;
                // Reentering will happen in the Lock() method
                return currentLock;
            }

            // Select the proper type of lock depending on whether we're in a micro-thread or not.
            var newLock = new MicroThreadSyncLock(this);
            AcquireOrEnqueue(newLock);
            await newLock.Acquired;

            // In the case of sync, we need to register in the proper thread, so call to Register() is defered to later.
            return newLock;
        }

        /// <summary>
        /// Acquires an asynchronous lock. The lock will be tied to the current <see cref="MicroThread"/> to allow re-entrancy.
        /// </summary>
        /// <returns>A task that completes when the lock is acquired.</returns>
        /// <remarks>This way of acquiring the lock is only valid when in a <see cref="MicroThread"/>.</remarks>
        [ItemNotNull]
        public async Task<IDisposable> LockAsync()
        {
            if (Scheduler.CurrentMicroThread == null) throw new InvalidOperationException($"Aynchronous lock can only be acquired from a micro-thread. Use {nameof(ReserveSyncLock)}.");
            if (isDisposed) throw new ObjectDisposedException(nameof(MicroThreadLock));

            // If we already acquired the lock in this micro-thread, we're just re-entering
            if (asyncLocks.IsValueCreated && asyncLocks.Value != null)
            {
                var currentLock = asyncLocks.Value;
                currentLock.Reenter();
                return currentLock;
            }

            // Select the proper type of lock depending on whether we're in a micro-thread or not.
            var newLock = new MicroThreadAsyncLock(this);
            AcquireOrEnqueue(newLock);
            await newLock.Acquired;

            // In the case of async we can register immediately after acquiring. For sync, we need to register in the proper thread.
            newLock.Register();

            return newLock;
        }

        private void AcquireOrEnqueue(MicroThreadLockBase lockToAcquire)
        {
            lock (lockQueue)
            {
                if (lockQueue.Count == 0)
                {
                    // Nothing else is in the queue, let's acquire immediately.
                    lockToAcquire.Acquire();
                }
                // Let's enqueue this new lock so it can be notified by the previous lock when it can be acquired.
                lockQueue.Enqueue(lockToAcquire);
            }
        }

        private abstract class MicroThreadLockBase : IDisposable
        {
            protected readonly MicroThreadLock MicroThreadLock;
            private readonly TaskCompletionSource<int> acquisition;
            private int reentrancy;

            protected MicroThreadLockBase(MicroThreadLock microThreadLock)
            {
                MicroThreadLock = microThreadLock;
                acquisition = new TaskCompletionSource<int>();
            }

            public Task Acquired => acquisition.Task;

            public virtual void Dispose()
            {
                if (reentrancy == 0)
                    throw new InvalidOperationException("Trying to dispose a lock that has already been released.");

                --reentrancy;
                if (reentrancy == 0)
                {
                    Release();
                    lock (MicroThreadLock.lockQueue)
                    {
                        // Remove ourself from the queue.
                        var thisLock = MicroThreadLock.lockQueue.Dequeue();
                        if (thisLock != this) throw new InvalidOperationException("The first lock in the queue was not the current lock");
                        // If another lock is waiting, let's acquire it
                        if (MicroThreadLock.lockQueue.Count > 0)
                        {
                            var nextLock = MicroThreadLock.lockQueue.Peek();
                            nextLock.Acquire();
                        }
                    }
                }
            }

            internal void Acquire()
            {
                if (reentrancy != 0) throw new InvalidOperationException("Trying to enter a lock that has already been entered");
                ++reentrancy;
                acquisition.SetResult(0);
            }

            internal virtual void Reenter()
            {
                if (!acquisition.Task.IsCompleted) throw new InvalidOperationException("Trying to reenter a lock that has not yet been acquired");
                ++reentrancy;
            }

            internal abstract void Release();
        }

        private class MicroThreadAsyncLock : MicroThreadLockBase
        {
            public MicroThreadAsyncLock(MicroThreadLock microThreadLock)
                : base(microThreadLock)
            {
            }

            internal void Register()
            {
                MicroThreadLock.asyncLocks.Value = this;
            }

            internal override void Release()
            {
                MicroThreadLock.asyncLocks.Value = null;
            }
        }

        private class MicroThreadSyncLock : MicroThreadLockBase, ISyncLockable
        {
            private bool locked;

            public MicroThreadSyncLock(MicroThreadLock microThreadLock)
                : base(microThreadLock)
            {
            }

            public override void Dispose()
            {
                Monitor.Exit(MicroThreadLock.syncLock);
                base.Dispose();
            }

            internal override void Reenter()
            {
                Monitor.Enter(MicroThreadLock.syncLock);
                base.Reenter();
            }

            internal void Take()
            {
                if (MicroThreadLock.currentSyncLockThread != 0)
                    throw new InvalidOperationException("Trying to lock while another thread owns the lock.");

                MicroThreadLock.currentSyncLockThread = Thread.CurrentThread.ManagedThreadId;
                MicroThreadLock.currentSyncLock = this;
            }

            internal override void Release()
            {
                if (MicroThreadLock.currentSyncLockThread != Thread.CurrentThread.ManagedThreadId)
                    throw new InvalidOperationException("Trying to unlock while another thread owns the lock.");

                MicroThreadLock.currentSyncLockThread = 0;
                MicroThreadLock.currentSyncLock = null;
            }

            [NotNull]
            public IDisposable Lock()
            {
                if (!locked)
                {
                    // We register here because we are in the proper thread.
                    Take();
                    Monitor.Enter(MicroThreadLock.syncLock);
                }
                else
                {
                    Reenter();
                }
                locked = true;
                return this;
            }
        }
    }
}
