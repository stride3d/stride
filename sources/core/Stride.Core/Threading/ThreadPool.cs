// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Threading;
using Stride.Core.Annotations;

namespace Stride.Core.Threading
{
	using System.Threading.Tasks;



	/// <summary>
    /// Thread pool for scheduling actions.
    /// </summary>
    internal class ThreadPool
    {
        public static readonly ThreadPool Instance = new ThreadPool();
        
		private readonly ParameterizedThreadStart cachedTaskLoop;
		private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
		private readonly object lockObj = new object();
		private readonly SubSystem.Threading.ThreadPool tp = new SubSystem.Threading.ThreadPool();
		
		private int itemCount;

        public ThreadPool()
		{
			// Cache delegate to avoid pointless allocation
			/*cachedTaskLoop = ProcessWorkItems;
			var threads = Environment.ProcessorCount > 2 ? Environment.ProcessorCount - 1 : 1;
			for(int i = 0; i < threads; i++)
            {
				NewThread();
			}*/
		}
		
		public void QueueWorkItem([NotNull, Pooled] Action workItem, int amount = 1)
		{
			// Throw right here to help debugging
			if(workItem == null)
			{
				throw new NullReferenceException(nameof(workItem));
			}

			if(amount < 1)
			{
				throw new ArgumentOutOfRangeException(nameof(amount));
			}

			/*Interlocked.Add(ref itemCount, amount);*/
			
			for(int i = 0; i < amount; i++)
			{
				PooledDelegateHelper.AddReference(workItem);
				tp.QueueUserWorkItem( () =>
				{
					Action a = (Action)workItem;
					try
					{
						a();
					}
					finally
					{
						PooledDelegateHelper.Release(a);
					}
				} );
				//queue.Enqueue(workItem);
			}

			/*lock(lockObj)
			{
				Monitor.Pulse(lockObj);
			}*/
		}

		private void ProcessWorkItems(object paramObj)
		{
			try
			{
				while(true)
				{
					Action action;
					var sw = new SpinWait();
					while(queue.TryDequeue(out action) == false)
					{
						if(Volatile.Read(ref itemCount) == 0)
						{
							lock( lockObj )
							{
								if(Volatile.Read(ref itemCount) == 0)
								{
									Monitor.Wait(lockObj);
									// We've got work to deal with, pulse other threads
									Monitor.Pulse(lockObj);
								}
								continue;
							}
						}

						if( sw.NextSpinWillYield )
						{
							sw = new SpinWait();
						}
						// Spin for a while to catch more incoming work 
						sw.SpinOnce();
					}

					Interlocked.Decrement(ref itemCount);
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
				NewThread();
			}
		}
		
		void NewThread()
		{
			new Thread(cachedTaskLoop)
			{
				Name = $"{GetType().FullName} thread",
				IsBackground = true,
				Priority = ThreadPriority.Highest
			}.Start();
		}
    }
}