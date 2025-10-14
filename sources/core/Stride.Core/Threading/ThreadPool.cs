// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Stride.Core.Threading;

/// <summary>
/// Thread pool for scheduling sub-millisecond actions, do not schedule long-running tasks.
/// Can be instantiated and generates less garbage than dotnet's.
/// </summary>
public sealed partial class ThreadPool : IDisposable
{
    private static readonly Logger Logger = GlobalLogger.GetLogger(nameof(ThreadPool));

    /// <summary>
    /// The default instance that the whole process shares, use this one to avoid wasting process memory.
    /// </summary>
    public static ThreadPool Instance = new();

    private static readonly bool SingleCore;
    [ThreadStatic]
    private static bool isWorkedThread;
    /// <summary> Is the thread reading this property a worker thread </summary>
    public static bool IsWorkedThread => isWorkedThread;

    private static readonly ProfilingKey ProcessWorkItemKey = new($"{nameof(ThreadPool)}.ProcessWorkItem");

    private readonly ConcurrentQueue<Work> workItems = new();
    private readonly ISemaphore semaphore;

    private long completionCounter;
    private int workScheduled, threadsBusy;
    private int disposing;
    private int leftToDispose;

    /// <summary> Amount of threads within this pool </summary>
    public readonly int WorkerThreadsCount;
    /// <summary> Amount of work waiting to be taken care of </summary>
    public int WorkScheduled => Volatile.Read(ref workScheduled);
    /// <summary> Amount of work completed </summary>
    public ulong CompletedWork => (ulong)Volatile.Read(ref completionCounter);
    /// <summary> Amount of threads currently executing work items </summary>
    public int ThreadsBusy => Volatile.Read(ref threadsBusy);

    public ThreadPool(int? threadCount = null)
    {
        int spinCount = 70;

        if (RuntimeInformation.ProcessArchitecture is Architecture.Arm or Architecture.Arm64)
        {
            // Dotnet:
            // On systems with ARM processors, more spin-waiting seems to be necessary to avoid perf regressions from incurring
            // the full wait when work becomes available soon enough. This is more noticeable after reducing the number of
            // thread requests made to the thread pool because otherwise the extra thread requests cause threads to do more
            // busy-waiting instead and adding to contention in trying to look for work items, which is less preferable.
            spinCount *= 4;
        }
        try
        {
            semaphore = new DotnetLifoSemaphore(spinCount);
        }
        catch (Exception e)
        {
            // For net6+ this should not happen, logging instead of throwing as this is just a performance regression
            if (Environment.Version.Major >= 6)
                Logger.Warning($"Could not bind to dotnet's Lifo Semaphore, falling back to suboptimal semaphore:\n{e}");

            semaphore = new SemaphoreW(spinCountParam: 70);
        }

        WorkerThreadsCount = threadCount ?? (Environment.ProcessorCount == 1 ? 1 : Environment.ProcessorCount - 1);
        leftToDispose = WorkerThreadsCount;
        for (int i = 0; i < WorkerThreadsCount; i++)
        {
            NewWorker();
        }
    }

    static ThreadPool()
    {
        SingleCore = Environment.ProcessorCount < 2;
    }

    /// <summary>
    /// Queue an action to run on one of the available threads,
    /// it is strongly recommended that the action takes less than a millisecond.
    /// </summary>
    public unsafe void QueueWorkItem([Pooled] Action workItem, int amount = 1)
    {
        // Throw right here to help debugging
        if (workItem == null)
        {
            throw new NullReferenceException(nameof(workItem));
        }

#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);
        ObjectDisposedException.ThrowIf(disposing > 0, this);
#else
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        if (disposing > 0) throw new ObjectDisposedException(ToString());
#endif // NET8_0_OR_GREATER

        Interlocked.Add(ref workScheduled, amount);
        var work = new Work { WorkHandler = &ActionHandler, Data = workItem };
        for (int i = 0; i < amount; i++)
        {
            PooledDelegateHelper.AddReference(workItem);
            workItems.Enqueue(work);
        }
        semaphore.Release(amount);
    }

    static void ActionHandler(object param)
    {
        Action action = (Action)param;
        try
        {
            action();
        }
        finally
        {
            PooledDelegateHelper.Release(action);
        }
    }

    /// <summary>
    /// Queue some work item to run on one of the available threads,
    /// it is strongly recommended that the action takes less than a millisecond.
    /// Additionally, the parameter provided must be fixed from this call onward until the action has finished executing
    /// </summary>
    public unsafe void QueueUnsafeWorkItem(object parameter, delegate*<object, void> obj, int amount = 1)
    {
        if (parameter == null)
        {
            throw new NullReferenceException(nameof(parameter));
        }

#if NET8_0_OR_GREATER
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 1);
        ObjectDisposedException.ThrowIf(disposing > 0, this);
#else
        if (amount < 1) throw new ArgumentOutOfRangeException(nameof(amount));
        if (disposing > 0) throw new ObjectDisposedException(ToString());
#endif // NET8_0_OR_GREATER

        Interlocked.Add(ref workScheduled, amount);
        var work = new Work { WorkHandler = obj, Data = parameter };
        for (int i = 0; i < amount; i++)
        {
            workItems.Enqueue(work);
        }
        semaphore.Release(amount);
    }

    /// <summary>
    /// Attempt to steal work from the threadpool to execute it from the calling thread.
    /// If you absolutely have to block inside one of the threadpool's thread for whatever
    /// reason do a busy loop over this function.
    /// </summary>
    public unsafe bool TryCooperate()
    {
        if (workItems.TryDequeue(out var workItem))
        {
            Interlocked.Increment(ref threadsBusy);
            Interlocked.Decrement(ref workScheduled);
            try
            {
                using (Profiler.Begin(ProcessWorkItemKey))
                    workItem.WorkHandler(workItem.Data);
            }
            finally
            {
                Interlocked.Decrement(ref threadsBusy);
                Interlocked.Increment(ref completionCounter);
            }
            return true;
        }

        return false;
    }

    private void NewWorker()
    {
        new Thread(WorkerThreadScope)
        {
            Name = $"{GetType().FullName} thread",
            IsBackground = true,
            Priority = ThreadPriority.Highest,
        }.Start();
    }

    private void WorkerThreadScope()
    {
        isWorkedThread = true;
        try
        {
            do
            {
                while (TryCooperate())
                {

                }

                if (disposing > 0)
                {
                    return;
                }

                semaphore.Wait();
            } while (true);
        }
        finally
        {
            if (disposing == 0)
            {
                NewWorker();
            }
            else
            {
                Interlocked.Decrement(ref leftToDispose);
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposing, 1, 0) == 1)
        {
            return;
        }

        semaphore.Release(WorkerThreadsCount);
        semaphore.Dispose();
        while (Volatile.Read(ref leftToDispose) != 0)
        {
            Thread.Yield();
        }

        // Finish any work left
        while (TryCooperate())
        {

        }
    }

    unsafe struct Work
    {
        public object Data;
        public delegate*<object, void> WorkHandler;
    }

    private interface ISemaphore : IDisposable
    {
        public void Release(int count);
        public void Wait(int timeout = -1);
    }

    private sealed class DotnetLifoSemaphore : ISemaphore
    {
        private readonly IDisposable semaphore;
        private readonly Func<int, bool, bool> wait;
        private readonly Action<int> release;

        public DotnetLifoSemaphore(int spinCount)
        {
            // The semaphore Dotnet uses for its own threadpool is more efficient than what's publicly available,
            // but sadly it is internal - we'll hijack it through reflection
            Type lifoType = Type.GetType("System.Threading.LowLevelLifoSemaphore")!;
            semaphore = (IDisposable)Activator.CreateInstance(lifoType, [0, short.MaxValue, spinCount, new Action(() => { })])!;
            wait = lifoType.GetMethod("Wait", BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate<Func<int, bool, bool>>(semaphore);
            release = lifoType.GetMethod("Release", BindingFlags.Instance | BindingFlags.Public)!.CreateDelegate<Action<int>>(semaphore);
        }

        public void Dispose() => semaphore.Dispose();
        public void Release(int count) => release(count);
        public void Wait(int timeout = -1) => wait(timeout, true);
    }
}
