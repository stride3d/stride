// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System;
using System.Threading;

namespace SubSystem.Threading
{
    public sealed partial class ThreadPool
    {
        [StructLayout(LayoutKind.Sequential)] // enforce layout so that padding reduces false sharing
        public sealed class WorkQueue
        {
            private readonly ConcurrentQueue<object> workItems = new ConcurrentQueue<object>();
            private readonly ThreadPool Pool;
            private readonly WorkStealingQueueList Work = new WorkStealingQueueList();

            private readonly PaddingFalseSharing pad1;

            private volatile int numOutstandingThreadRequests = 0;

            private readonly PaddingFalseSharing pad2;



            public WorkQueue( ThreadPool pool ) => Pool = pool;
            private ThreadLocalQueue GetOrCreateThreadLocals() => ThreadLocalQueue.threadLocals ?? CreateThreadLocals();



            [ MethodImpl( MethodImplOptions.NoInlining ) ]
            private ThreadLocalQueue CreateThreadLocals()
            {
                Debug.Assert( ThreadLocalQueue.threadLocals == null );

                return ThreadLocalQueue.threadLocals = new ThreadLocalQueue( this );
            }



            public void EnsureThreadRequested()
            {
                //
                // If we have not yet requested #procs threads, then request a new thread.
                //
                // CoreCLR: Note that there is a separate count in the VM which has already been incremented
                // by the VM by the time we reach this point.
                //
                int count = numOutstandingThreadRequests;
                while( count < Environment.ProcessorCount )
                {
                    int prev = Interlocked.CompareExchange( ref numOutstandingThreadRequests, count + 1, count );
                    if( prev == count )
                    {
                        Pool.RequestWorker();
                        break;
                    }

                    count = prev;
                }
            }



            public void MarkThreadRequestSatisfied()
            {
                //
                // One of our outstanding thread requests has been satisfied.
                // Decrement the count so that future calls to EnsureThreadRequested will succeed.
                //
                // CoreCLR: Note that there is a separate count in the VM which has already been decremented
                // by the VM by the time we reach this point.
                //
                int count = numOutstandingThreadRequests;
                while( count > 0 )
                {
                    int prev = Interlocked.CompareExchange( ref numOutstandingThreadRequests, count - 1, count );
                    if( prev == count )
                    {
                        break;
                    }

                    count = prev;
                }
            }



            public void Enqueue( object callback )
            {
                Debug.Assert( ( callback is Action ) ^ ( callback is Task ) );
                workItems.Enqueue( callback );
                EnsureThreadRequested();
            }



            private object Dequeue( ThreadLocalQueue tl, ref bool missedSteal )
            {
                WorkStealingQueue localWsq = tl.workStealingQueue;
                object callback;

                if( ( callback = localWsq.LocalPop() ) == null && // first try the local queue
                    ! workItems.TryDequeue( out callback ) ) // then try the global queue
                {
                    // finally try to steal from another thread's local queue
                    WorkStealingQueue[] queues = Work.Queues;
                    int c = queues.Length;
                    Debug.Assert( c > 0, "There must at least be a queue for this thread." );
                    int maxIndex = c - 1;
                    int i = tl.random.Next( c );
                    while( c > 0 )
                    {
                        i = ( i < maxIndex ) ? i + 1 : 0;
                        WorkStealingQueue otherQueue = queues[ i ];
                        if( otherQueue != localWsq && otherQueue.CanSteal )
                        {
                            callback = otherQueue.TrySteal( ref missedSteal );
                            if( callback != null )
                            {
                                break;
                            }
                        }

                        c--;
                    }
                }

                return callback;
            }



            /// <summary>
            /// Dispatches work items to this thread.
            /// </summary>
            /// <returns>
            /// <c>true</c> if this thread did as much work as was available or its quantum expired.
            /// <c>false</c> if this thread stopped working early.
            /// </returns>
            public bool Dispatch()
            {
                WorkQueue outerWorkQueue = Pool.s_workQueue;

                //
                // Update our records to indicate that an outstanding request for a thread has now been fulfilled.
                // From this point on, we are responsible for requesting another thread if we stop working for any
                // reason, and we believe there might still be work in the queue.
                //
                // CoreCLR: Note that if this thread is aborted before we get a chance to request another one, the VM will
                // record a thread request on our behalf.  So we don't need to worry about getting aborted right here.
                //
                outerWorkQueue.MarkThreadRequestSatisfied();

                // Has the desire for logging changed since the last time we entered?
                /*outerWorkQueue.loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, FrameworkEventSource.Keywords.ThreadPool | FrameworkEventSource.Keywords.ThreadTransfer);*/

                //
                // Assume that we're going to need another thread if this one returns to the VM.  We'll set this to
                // false later, but only if we're absolutely certain that the queue is empty.
                //
                bool needAnotherThread = true;
                try
                {
                    //
                    // Set up our thread-local data
                    //
                    // Use operate on workQueue local to try block so it can be enregistered
                    WorkQueue workQueue = outerWorkQueue;
                    ThreadLocalQueue tl = workQueue.GetOrCreateThreadLocals();

                    // Start on clean ExecutionContext and SynchronizationContext
                    /*currentThread._executionContext = null;
                    currentThread._synchronizationContext = null;*/

                    //
                    // Loop until our quantum expires or there is no work.
                    //
                    while( true )
                    {
                        bool missedSteal = false;
                        // Use operate on workItem local to try block so it can be enregistered
                        object workItem = workQueue.Dequeue( tl, ref missedSteal );

                        if( workItem == null )
                        {
                            //
                            // No work.
                            // If we missed a steal, though, there may be more work in the queue.
                            // Instead of looping around and trying again, we'll just request another thread.  Hopefully the thread
                            // that owns the contended work-stealing queue will pick up its own workitems in the meantime,
                            // which will be more efficient than this thread doing it anyway.
                            //
                            needAnotherThread = missedSteal;

                            // Tell the VM we're returning normally, not because Hill Climbing asked us to return.
                            return true;
                        }

                        //
                        // If we found work, there may be more work.  Ask for another thread so that the other work can be processed
                        // in parallel.  Note that this will only ask for a max of #procs threads, so it's safe to call it for every dequeue.
                        //
                        workQueue.EnsureThreadRequested();

                        //
                        // Execute the workitem outside of any finally blocks, so that it can be aborted if needed.
                        //
                        Debug.Assert( workItem is Action );
                        ( (Action) workItem ).Invoke();

                        // Release refs
                        workItem = null;

                        //
                        // Notify the VM that we executed this workitem.  This is also our opportunity to ask whether Hill Climbing wants
                        // us to return the thread to the pool or not.
                        //
                        if( ! Pool.NotifyWorkItemComplete() )
                            return false;
                    }
                }
                finally
                {
                    //
                    // If we are exiting for any reason other than that the queue is definitely empty, ask for another
                    // thread to pick up where we left off.
                    //
                    if( needAnotherThread )
                        outerWorkQueue.EnsureThreadRequested();
                }
            }



            private class WorkStealingQueueList
            {
                private volatile WorkStealingQueue[] _queues = new WorkStealingQueue[ 0 ];
                public WorkStealingQueue[] Queues => _queues;



                public void Add( WorkStealingQueue queue )
                {
                    Debug.Assert( queue != null );
                    while( true )
                    {
                        WorkStealingQueue[] oldQueues = _queues;
                        Debug.Assert( Array.IndexOf( oldQueues, queue ) == - 1 );

                        var newQueues = new WorkStealingQueue[ oldQueues.Length + 1 ];
                        Array.Copy( oldQueues, newQueues, oldQueues.Length );
                        newQueues[ newQueues.Length - 1 ] = queue;
                        if( Interlocked.CompareExchange( ref _queues, newQueues, oldQueues ) == oldQueues )
                        {
                            break;
                        }
                    }
                }



                public void Remove( WorkStealingQueue queue )
                {
                    Debug.Assert( queue != null );
                    while( true )
                    {
                        WorkStealingQueue[] oldQueues = _queues;
                        if( oldQueues.Length == 0 )
                        {
                            return;
                        }

                        int pos = Array.IndexOf( oldQueues, queue );
                        if( pos == - 1 )
                        {
                            Debug.Fail( "Should have found the queue" );
                            return;
                        }

                        var newQueues = new WorkStealingQueue[ oldQueues.Length - 1 ];
                        if( pos == 0 )
                        {
                            Array.Copy( oldQueues, 1, newQueues, 0, newQueues.Length );
                        }
                        else if( pos == oldQueues.Length - 1 )
                        {
                            Array.Copy( oldQueues, newQueues, newQueues.Length );
                        }
                        else
                        {
                            Array.Copy( oldQueues, newQueues, pos );
                            Array.Copy( oldQueues, pos + 1, newQueues, pos, newQueues.Length - pos );
                        }

                        if( Interlocked.CompareExchange( ref _queues, newQueues, oldQueues ) == oldQueues )
                        {
                            break;
                        }
                    }
                }
            }



            private class WorkStealingQueue
            {
                private const int INITIAL_SIZE = 32;
                private volatile object[] m_array = new object[ INITIAL_SIZE ];
                private volatile int m_mask = INITIAL_SIZE - 1;

                #if DEBUG
                // in debug builds, start at the end so we exercise the index reset logic.
                private const int START_INDEX = int.MaxValue;
                #else
            private const int START_INDEX = 0;
                #endif

                private volatile int m_headIndex = START_INDEX;
                private volatile int m_tailIndex = START_INDEX;

                private SpinLock m_foreignLock = new SpinLock( enableThreadOwnerTracking: false );

                public object LocalPop() => m_headIndex < m_tailIndex ? LocalPopCore() : null;



                private object LocalPopCore()
                {
                    while( true )
                    {
                        int tail = m_tailIndex;
                        if( m_headIndex >= tail )
                        {
                            return null;
                        }

                        // Decrement the tail using a fence to ensure subsequent read doesn't come before.
                        tail--;
                        Interlocked.Exchange( ref m_tailIndex, tail );

                        // If there is no interaction with a take, we can head down the fast path.
                        if( m_headIndex <= tail )
                        {
                            int idx = tail & m_mask;
                            object obj = Volatile.Read( ref m_array[ idx ] );

                            // Check for nulls in the array.
                            if( obj == null ) continue;

                            m_array[ idx ] = null;
                            return obj;
                        }
                        else
                        {
                            // Interaction with takes: 0 or 1 elements left.
                            bool lockTaken = false;
                            try
                            {
                                m_foreignLock.Enter( ref lockTaken );

                                if( m_headIndex <= tail )
                                {
                                    // Element still available. Take it.
                                    int idx = tail & m_mask;
                                    object obj = Volatile.Read( ref m_array[ idx ] );

                                    // Check for nulls in the array.
                                    if( obj == null ) continue;

                                    m_array[ idx ] = null;
                                    return obj;
                                }
                                else
                                {
                                    // If we encountered a race condition and element was stolen, restore the tail.
                                    m_tailIndex = tail + 1;
                                    return null;
                                }
                            }
                            finally
                            {
                                if( lockTaken )
                                    m_foreignLock.Exit( useMemoryBarrier: false );
                            }
                        }
                    }
                }



                public bool CanSteal => m_headIndex < m_tailIndex;



                public object TrySteal( ref bool missedSteal )
                {
                    while( true )
                    {
                        if( CanSteal )
                        {
                            bool taken = false;
                            try
                            {
                                m_foreignLock.TryEnter( ref taken );
                                if( taken )
                                {
                                    // Increment head, and ensure read of tail doesn't move before it (fence).
                                    int head = m_headIndex;
                                    Interlocked.Exchange( ref m_headIndex, head + 1 );

                                    if( head < m_tailIndex )
                                    {
                                        int idx = head & m_mask;
                                        object obj = Volatile.Read( ref m_array[ idx ] );

                                        // Check for nulls in the array.
                                        if( obj == null ) continue;

                                        m_array[ idx ] = null;
                                        return obj;
                                    }
                                    else
                                    {
                                        // Failed, restore head.
                                        m_headIndex = head;
                                    }
                                }
                            }
                            finally
                            {
                                if( taken )
                                    m_foreignLock.Exit( useMemoryBarrier: false );
                            }

                            missedSteal = true;
                        }

                        return null;
                    }
                }
            }



            /// <summary>
            /// Holds a WorkStealingQueue, and removes it from the list when this object is no longer referenced.
            /// </summary>
            private class ThreadLocalQueue
            {
                [ ThreadStatic ] public static ThreadLocalQueue threadLocals;

                public readonly WorkQueue workQueue;
                public readonly WorkStealingQueue workStealingQueue;
                public FastRandom random = new FastRandom( Thread.CurrentThread.ManagedThreadId ); // mutable struct, do not copy or make readonly



                public ThreadLocalQueue( WorkQueue tpq )
                {
                    workQueue = tpq;
                    workStealingQueue = new WorkStealingQueue();
                    workQueue.Work.Add( workStealingQueue );
                }



                ~ThreadLocalQueue()
                {
                    // Transfer any pending workitems into the global queue so that they will be executed by another thread
                    if( null != workStealingQueue )
                    {
                        object cb;
                        while( ( cb = workStealingQueue.LocalPop() ) != null )
                        {
                            Debug.Assert( null != cb );
                            workQueue.Enqueue( cb );
                        }

                        workQueue.Work.Remove( workStealingQueue );
                    }
                }
            }



            // Simple random number generator. We don't need great randomness, we just need a little and for it to be fast.
            private struct FastRandom // xorshift prng
            {
                private uint _w, _x, _y, _z;



                public FastRandom( int seed )
                {
                    _x = (uint) seed;
                    _w = 88675123;
                    _y = 362436069;
                    _z = 521288629;
                }



                public int Next( int maxValue )
                {
                    Debug.Assert( maxValue > 0 );

                    uint t = _x ^ ( _x << 11 );
                    _x = _y;
                    _y = _z;
                    _z = _w;
                    _w = _w ^ ( _w >> 19 ) ^ ( t ^ ( t >> 8 ) );

                    return (int) ( _w % (uint) maxValue );
                }
            }
        }



        /// <summary>Padding structure used to minimize false sharing</summary>
        [ StructLayout( LayoutKind.Explicit, Size = PaddingFalseSharing.CACHE_LINE_SIZE - sizeof(int) ) ]
        public struct PaddingFalseSharing
        {
            /// <summary>A size greater than or equal to the size of the most common CPU cache lines.</summary>
            #if TARGET_ARM64
                public const int CACHE_LINE_SIZE = 128;
            #else
            public const int CACHE_LINE_SIZE = 64;
            #endif
        }
    }
}
