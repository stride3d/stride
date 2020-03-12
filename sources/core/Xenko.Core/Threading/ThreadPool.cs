// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Xenko.Core.Annotations;

namespace Xenko.Core.Threading
{
    /// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();
		private readonly ParameterizedThreadStart cachedTaskLoop;
		private readonly bool ManyCore;
        
		/// <summary>
		/// Linked-list like collection of threads that are waiting for work.
		/// Low contention for pool threads.
		/// </summary>
		private volatile LinkedIdleThread idleThreads;

		private readonly LightConcurrentQueue<Action> queue = new LightConcurrentQueue<Action>();

		public ThreadPool()
		{
			// Cache delegate to avoid pointless allocation
			cachedTaskLoop = ProcessWorkItems;
			// No point in having more threads than processors
			int maxThreads = (Environment.ProcessorCount < 2) ? 1 : (Environment.ProcessorCount - 1);
			ManyCore = maxThreads >= 8;
			for( int i = 0; i < maxThreads; i++ )
			{
				NewThread(null);
			}
		}
		
		void NewThread(LinkedIdleThread node)
		{
			new Thread(cachedTaskLoop)
			{
				Name = $"{GetType().FullName} thread",
				IsBackground = true,
				Priority = ThreadPriority.Highest
			}.Start(node);
		}
		
		public void QueueWorkItem([NotNull, Pooled] Action workItem, int amount = 1)
		{
			// Throw right here to help debugging
			if(workItem == null)
			{
				throw new NullReferenceException(nameof(workItem));
			}

			if (amount < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(amount));
			}
			
			SpinWait sw = new SpinWait();
			while (amount > 0)
			{
				PooledDelegateHelper.AddReference(workItem);
				LinkedIdleThread node;
				if (ManyCore)
				{
					// Spin a bit to wait for threads
					while ((node = idleThreads) == null && sw.NextSpinWillYield == false)
					{
						sw.SpinOnce();
					}
				}
				else
				{
					node = idleThreads;
				}


				if (node != null)
				{
					// Try schedule idle thread
					if (Interlocked.CompareExchange(ref idleThreads, node.Previous, node) != node)
						continue; // Latest idle threads changed, try again

					// Wakeup thread and schedule work
					// The order those two lines are laid out in is essential !
					Interlocked.Exchange(ref node.Work, workItem);
					node.MRE.Set();
				}
				else
				{
					queue.Enqueue(workItem);
				}
				amount--;
			}
		}

		private void ProcessWorkItems(object nodeObj)
		{
			// nodeObj is non-null when a thread caught an exception and had to throw,
			// the thread created another one and passed its node obj to us.
			LinkedIdleThread node = nodeObj == null ? new LinkedIdleThread(new ManualResetEventSlim(true)) : (LinkedIdleThread)nodeObj;
			try
			{
				while (true)
				{
					Action action;
					if (queue.TryDequeue(out action) == false)
					{
						// Should we notify system that this thread is ready to work?
						// This has to also work for when a thread takes the place of another one when restoring
						// from an exception for example.
						// If the mre was set and we took the work, this node definitely is dequeued, re-queue it 
						SpinWait sw = new SpinWait();
						if (node.MRE.IsSet && Volatile.Read(ref node.Work) == null)
						{
							// Notify that we're waiting for work
							node.MRE.Reset();
							while (true)
							{
								node.Previous = idleThreads;
								if (Interlocked.CompareExchange(ref idleThreads, node, node.Previous) == node.Previous)
								{
									break;
								}
								sw.SpinOnce();
							}
						}

						// Wait for work
						sw = new SpinWait();
						while (true)
						{
							if (node.MRE.IsSet)
							{
								// Work has been scheduled for this thread specifically, take it
								action = Interlocked.Exchange(ref node.Work, null);
								break;
							}
							else if (queue.TryDequeue(out action))
							{
								break; // We successfully dequeued this node from the shared stack, quit loop and process action
							}
							else if (sw.NextSpinWillYield)
							{
								// Spun for enough time, go to sleep and wait for work to be scheduled specifically to this thread
								node.MRE.Wait();
								action = Interlocked.Exchange(ref node.Work, null);
								break;
							}
							else
							{
								sw.SpinOnce();
							}
						}
					}

					try
					{
						action();
					}
					finally
					{
						PooledDelegateHelper.Release(action);
					}
				}
			}
			finally
			{
				// We must keep up the amount of threads that the system handles.
				// Spawn a new one as this one is about to abort because of an exception. 
				NewThread(node);
			}
		}

		private class LinkedIdleThread
		{
			public Action Work;
			public readonly ManualResetEventSlim MRE;
			public volatile LinkedIdleThread Previous;
			
			public LinkedIdleThread(ManualResetEventSlim MREParam)
			{
				MRE = MREParam;
			}
		}

		/// <summary>
        /// Standard queue implementation supporting lock-free concurrent reading and writing, blocks r/w when resizing.
        /// Slower than dotnet's conc-queue but only allocates on resize which is better for more complex program's overall performances.
        /// </summary>
        private class LightConcurrentQueue<T> where T : class
        {
            private const int INITIAL_SIZE = 4;
            private T[] array = new T[INITIAL_SIZE];
            private volatile uint max = INITIAL_SIZE;
            private int rHead, wHead, count;
            private int lockState;

            public void Enqueue(T val)
            {
                if (val is null)
                    throw new ArgumentNullException(nameof(val));

                do
                {
	                int size;
	                try
	                {
		                PreventResizeLock();

		                size = array.Length;

		                var i = ((uint)Interlocked.Increment(ref wHead) - 1) % (uint)size;
		                if (Interlocked.CompareExchange(ref array[i], val, null) == null)
		                {
			                Interlocked.Increment(ref count);
			                return;
		                }

		                // If non-null it hasn't been dequeued yet we need to grow this array.
		                // Release lock then AttemptGrow.
	                }
	                finally
	                {
		                ReleaseLock();
	                }

	                AttemptGrow(size);
                } while (true);
            }
            
            public bool TryDequeue(out T output)
            {
                var sw = new SpinWait();
                
                // Remove one from count if there is any left,
                // guarantees that increasing the head and taking the item is a valid operation
                do
                {
                    int v = count;
                    if (v == 0)
                    {
                        output = default;
                        return false;
                    }
                    // Spinning right before comp-exchange improves performances by a lot,
                    // not exactly sure why as I tested cases with spinning:
                    // 'after compxchg', 'after compxchg' + 'when v == 0', 'before rwLock xchg'
                    // and none of those cases, which have a fairly evident explanation,
                    // demonstrate such a large increase in performances.
                    sw.SpinOnce();
                    if (Interlocked.CompareExchange(ref count, v - 1, v) == v)
                        break;
                } while (true);

                try
                {
                    PreventResizeLock();
                    
                    // We are guaranteed to have at least one item as we decreased count above
                    var i = ((uint) Interlocked.Increment(ref rHead) - 1) % max;
                    while ((output = Interlocked.Exchange(ref array[i], null)) == null)
                    {
                        // Most cases won't run the spinwait
                        sw.SpinOnce();
                    }

                    return true;
                }
                finally
                {
                    ReleaseLock();
                }
            }
            
            private void PreventResizeLock()
            {
                var sw = new SpinWait();
                while (Interlocked.Increment(ref lockState) < 0)
                {
                    Interlocked.Decrement(ref lockState);
                    while(Volatile.Read(ref lockState) < 0)
                        sw.SpinOnce();
                }
            }
            
            private void ReleaseLock()
            {
                Interlocked.Decrement(ref lockState);
            }
            
            private void AttemptGrow(int previousSize)
            {
                // Add some padding to avoid wrapping around when we comp-exchange before a thread decreases the lock state
                // I highly doubt any computer running this logic will ever grow past half int.MaxValue threads
                const int offset = (int.MaxValue/2);
                
                SpinWait sw = new SpinWait();
                // Prevent multiple resize by ensuring that the lock is positive before negating
                do
                {
                    int v = lockState;
                    if (v < 0)
                        return; // Another thread is already attempting to grow, exit
                    
                    if (Interlocked.CompareExchange(ref lockState, v - offset, v) == v)
                        break; // We're blocking all new read and write now
                    
                    if(previousSize != max)
                        return;
                    
                    sw.SpinOnce();
                } while (true);
                
                try
                {
                    // lockState is now negative
                    
                    // Let's wait for other threads to exit the lock before resizing 
                    while (Volatile.Read(ref lockState) != -offset)
                    {
                        sw.SpinOnce();
                    }

                    // lockState is still being manipulated by reader and writer but BlockUntilResizeFinished
                    // prevents them from reading or writing to the array beyond this point.
                
                    if(previousSize != max)
                        return;

                    // Double the size to keep '% array.Length' from creating bad sequences when heads are near uint.MaxValue
                    T[] newArray = new T[max * 2];

                    var oldMax = max;
                    var readHead = ((uint) Volatile.Read(ref rHead)) % oldMax;
                    uint length = oldMax - readHead;
                    // Place all items after head at the start of the new array
                    Array.Copy(array, readHead, newArray, 0, length);
                    if (readHead != 0)
                    {
                        // Move all items from 0 to writeHead at the end of the array
                        Array.Copy(array, 0, newArray, length, readHead);
                    }

                    max = (uint)newArray.Length;
                    Interlocked.Exchange(ref array, newArray);
                    Interlocked.Exchange(ref rHead, 0);
                    for (int i = 0; i < newArray.Length; i++)
                    {
                        if (newArray[i] == null)
                        {
                            Interlocked.Exchange(ref wHead, i);
                            break;
                        }
                    }
                }
                finally
                {
                    // reset the lock to its previous state: unlocked
                    Interlocked.Add(ref lockState, offset);
                }
            }
        }
    }
}