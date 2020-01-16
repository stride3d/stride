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
        
		/// <summary>
		/// Linked-list like collection of threads that are waiting for work.
		/// Low contention for pool threads.
		/// </summary>
		private volatile LinkedIdleThread idleThreads;
		/// <summary>
		/// Linked-list like collection of work that can be
		/// de-queued from any thread when they are done working.
		/// High contention for pool threads.
		/// </summary>
		private volatile LinkedWork sharedWorkStack;
		
		public ThreadPool()
		{
			// Cache delegate to avoid pointless allocation
			cachedTaskLoop = ProcessWorkItems;
			// Inconsistent performances when more threads are trying to be woken up than there is processors
			int maxThreads = (Environment.ProcessorCount < 2) ? 1 : (Environment.ProcessorCount - 1);
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
		
		public void QueueWorkItem([NotNull, Pooled] Action workItem)
		{
			// Throw right here to help debugging
			if(workItem == null)
			{
				throw new NullReferenceException(nameof(workItem));
			}
			
			PooledDelegateHelper.AddReference(workItem);

			LinkedWork newSharedNode = null;
			while(true)
			{
				// Are all threads busy ?
				LinkedIdleThread node = idleThreads;
				if(node == null)
				{
					if(newSharedNode == null)
					{
						newSharedNode = new LinkedWork(workItem);
						if(idleThreads != null)
							continue;
					}

					// Schedule it on the shared stack
					newSharedNode.Previous = Interlocked.Exchange(ref sharedWorkStack, newSharedNode);
					newSharedNode.PreviousIsValid = true;
					break;
				}
				// Schedule this work item on latest idle thread
				
				while(node.PreviousIsValid == false)
				{
					// Spin while invalid, should be extremely short
				}

				// Try take this thread
				if(Interlocked.CompareExchange(ref idleThreads, node.Previous, node) != node)
					continue; // Latest idle threads changed, try again
				
				// Wakeup thread and schedule work
				// The order those two lines are laid out in is essential !
				Interlocked.Exchange(ref node.Work, workItem);
				node.MRE.Set();
				break;
			}
		}

		private void ProcessWorkItems(object nodeObj)
		{
			// nodeObj is non-null when a thread caught an exception and had to throw,
			// the thread created another one and passed its node obj to us.
			LinkedIdleThread node = nodeObj == null ? new LinkedIdleThread(new ManualResetEventSlim(true)) : (LinkedIdleThread)nodeObj;
			try
			{
				while(true)
				{
					Action action;
					LinkedWork workNode = sharedWorkStack;
					if(workNode != null)
					{
						if(TryTakeFromSharedNonBlocking(out var tempAction, workNode))
						{
							action = tempAction;
						}
						else
						{
							// We have shared work to do but failed to retrieve it, try again
							continue;
						}
					}
					else
					{
						// Should we notify system that this thread is ready to work?
						// This has to also work for when a thread takes the place of another one when restoring
						// from an exception for example.
						// If the mre was set and we took the work, this node definitely is dequeued, re-queue it 
						if(node.MRE.IsSet && Volatile.Read(ref node.Work) == null)
						{
							// Notify that we're waiting for work
							node.MRE.Reset();
							node.PreviousIsValid = false;
							node.Previous = Interlocked.Exchange(ref idleThreads, node);
							node.PreviousIsValid = true;
						}
					
						// Wait for work
						SpinWait sw = new SpinWait();
						while (true)
						{
							if(node.MRE.IsSet)
							{
								// Work has been scheduled for this thread specifically, take it
								action = Interlocked.Exchange(ref node.Work, null);
								break;
							}
							if(TryTakeFromSharedNonBlocking(out var tempAction, sharedWorkStack))
							{
								action = tempAction; 
								break; // We successfully dequeued this node from the shared stack, quit loop and process action
							}
						
							// Wait for work
							if (sw.NextSpinWillYield)
							{
								// Wait for work to be scheduled specifically to this thread
								node.MRE.Wait();
								action = Interlocked.Exchange(ref node.Work, null);
								break;
							}
						
							sw.SpinOnce();
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
		
		/// <summary>
		/// Attempt to remove the latest action scheduled on the shared stack,
		/// returns work only if there was any work AND the item was successfully
		/// removed from the stack without having to block.
		/// </summary>
		bool TryTakeFromSharedNonBlocking(out Action a, LinkedWork nodeToProcess)
		{
			if(nodeToProcess != null)
			{
				while(nodeToProcess.PreviousIsValid == false)
				{
					// Spin while invalid, should be extremely short
				}

				if(Interlocked.CompareExchange(ref sharedWorkStack, nodeToProcess.Previous, nodeToProcess) == nodeToProcess)
				{
					a = nodeToProcess.Work;
					return true;
				}
			}

			a = null;
			return false;
		}

		private class LinkedIdleThread
		{
			public Action Work;
			public readonly ManualResetEventSlim MRE;
			public volatile LinkedIdleThread Previous;
			public volatile bool PreviousIsValid;
			
			public LinkedIdleThread(ManualResetEventSlim MREParam)
			{
				MRE = MREParam;
			}
		}
		
		private class LinkedWork
		{
			public readonly Action Work;
			public volatile LinkedWork Previous;
			public volatile bool PreviousIsValid;
			
			public LinkedWork(Action workParam)
			{
				Work = workParam;
			}
		}
    }
}