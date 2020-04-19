// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
// Some parts of this file were inspired by AsyncEx (https://github.com/StephenCleary/AsyncEx)
/*
The MIT License (MIT)
https://opensource.org/licenses/MIT

Copyright (c) 2014 StephenCleary

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

// Original idea from Stephen Toub: http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx

namespace Stride.Core.Threading
{
    /// <summary>
    /// A mutual exclusion lock that is compatible with async. Note that this lock is <b>not</b> recursive!
    /// </summary>
    [DebuggerDisplay("Id = {Id}, Taken = {taken}")]
    [DebuggerTypeProxy(typeof(DebugView))]
    public sealed class AsyncLock
    {
        /// <summary>
        /// Whether the lock is taken by a task.
        /// </summary>
        private bool taken;

        /// <summary>
        /// The queue of TCSs that other tasks are awaiting to acquire the lock.
        /// </summary>
        private readonly IAsyncWaitQueue<IDisposable> queue;

        /// <summary>
        /// A task that is completed with the key object for this lock.
        /// </summary>
        private readonly Task<IDisposable> cachedKeyTask;

        /// <summary>
        /// The semi-unique identifier for this instance. This is 0 if the id has not yet been created.
        /// </summary>
        private int id;

        /// <summary>
        /// The object used for mutual exclusion.
        /// </summary>
        private readonly object mutex;

        /// <summary>
        /// Creates a new async-compatible mutual exclusion lock.
        /// </summary>
        public AsyncLock()
            : this(new DefaultAsyncWaitQueue<IDisposable>())
        {
        }

        /// <summary>
        /// Creates a new async-compatible mutual exclusion lock using the specified wait queue.
        /// </summary>
        /// <param name="queue">The wait queue used to manage waiters.</param>
        public AsyncLock(IAsyncWaitQueue<IDisposable> queue)
        {
            this.queue = queue;
            cachedKeyTask = Task.FromResult((IDisposable)new Key(this));
            mutex = new object();
        }

        /// <summary>
        /// Gets a semi-unique identifier for this asynchronous lock.
        /// </summary>
        public int Id => IdManager<AsyncLock>.GetId(ref id);

        /// <summary>
        /// Asynchronously acquires the lock. Returns a disposable that releases the lock when disposed.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the lock. If this is already set, then this method will attempt to take the lock immediately (succeeding if the lock is currently available).</param>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> LockAsync(CancellationToken cancellationToken)
        {
            Task<IDisposable> ret;
            lock (mutex)
            {
                if (!taken)
                {
                    // If the lock is available, take it immediately.
                    taken = true;
                    ret = cachedKeyTask;
                }
                else
                {
                    // Wait for the lock to become available or cancellation.
                    ret = queue.Enqueue(mutex, cancellationToken);
                }

                //Enlightenment.Trace.AsyncLock_TrackLock(this, ret);
            }

            return new AwaitableDisposable<IDisposable>(ret);
        }

        /// <summary>
        /// Synchronously acquires the lock. Returns a disposable that releases the lock when disposed. This method may block the calling thread.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the lock. If this is already set, then this method will attempt to take the lock immediately (succeeding if the lock is currently available).</param>
        public IDisposable Lock(CancellationToken cancellationToken)
        {
            Task<IDisposable> enqueuedTask;
            lock (mutex)
            {
                if (!taken)
                {
                    taken = true;
                    return cachedKeyTask.Result;
                }

                enqueuedTask = queue.Enqueue(mutex, cancellationToken);
            }

            // WaitAndUnwrapException
            return enqueuedTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously acquires the lock. Returns a disposable that releases the lock when disposed.
        /// </summary>
        /// <returns>A disposable that releases the lock when disposed.</returns>
        public AwaitableDisposable<IDisposable> LockAsync()
        {
            return LockAsync(CancellationToken.None);
        }

        /// <summary>
        /// Synchronously acquires the lock. Returns a disposable that releases the lock when disposed. This method may block the calling thread.
        /// </summary>
        public IDisposable Lock()
        {
            return Lock(CancellationToken.None);
        }

        /// <summary>
        /// Releases the lock.
        /// </summary>
        internal void ReleaseLock()
        {
            IDisposable finish = null;
            lock (mutex)
            {
                //Enlightenment.Trace.AsyncLock_Unlocked(this);
                if (queue.IsEmpty)
                    taken = false;
                else
                    finish = queue.Dequeue(cachedKeyTask.Result);
            }
            finish?.Dispose();
        }

        /// <summary>
        /// The disposable which releases the lock.
        /// </summary>
        private sealed class Key : IDisposable
        {
            /// <summary>
            /// The lock to release.
            /// </summary>
            private readonly AsyncLock asyncLock;

            /// <summary>
            /// Creates the key for a lock.
            /// </summary>
            /// <param name="asyncLock">The lock to release. May not be <c>null</c>.</param>
            public Key(AsyncLock asyncLock)
            {
                this.asyncLock = asyncLock;
            }

            /// <summary>
            /// Release the lock.
            /// </summary>
            public void Dispose()
            {
                asyncLock.ReleaseLock();
            }
        }

        // ReSharper disable UnusedMember.Local
        [DebuggerNonUserCode]
        private sealed class DebugView
        {
            private readonly AsyncLock mutex;

            public DebugView(AsyncLock mutex)
            {
                this.mutex = mutex;
            }

            public int Id => mutex.Id;

            public bool Taken => mutex.taken;

            public IAsyncWaitQueue<IDisposable> WaitQueue => mutex.queue;
        }
        // ReSharper restore UnusedMember.Local
    }
}
