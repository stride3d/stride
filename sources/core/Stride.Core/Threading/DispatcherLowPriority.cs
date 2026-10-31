// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stride.Core.Threading;

/// <summary>
/// Built for long-running background jobs,
/// won't monopolize the CPU away from this process or the rest of the user's OS while performing tasks
/// </summary>
public static class DispatcherLowPriority
{
    /// <summary>
    /// Set the amount of threads in the pool
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Value is less than or equal to zero</exception>
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID
    public static readonly int MaxDegreeOfParallelism = 1;
#else
    public static int MaxDegreeOfParallelism
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0);
            field = value;
            
            lock (poolLock)
            {
                ResizePool(value);
            }
        }
    } = int.TryParse(Environment.GetEnvironmentVariable("STRIDE_MAX_PARALLELISM"), out var envValue) && envValue > 0 
        ? envValue
        : Environment.ProcessorCount;
#endif

    private static readonly BlockingCollection<BatchState> tasks = new();
    private static readonly Lock poolLock = new();
    private static Stack<CancellationTokenSource>? poolThreadsCts;
    private static readonly bool unreliablePriority = (OperatingSystem.IsLinux() || OperatingSystem.IsWindows()) == false;

    /// <summary>
    /// Caller-defined method called by one of the threads scheduled by a <see cref="ForBatchedAsync"/> call,
    /// caller will loop through the range of items described by the two parameters
    /// </summary>
    public delegate void ProcessItemRange(int from, int toExclusive);

    /// <inheritdoc cref="DispatcherLowPriority"/>
    /// <param name="items">The amount of items to process</param>
    /// <param name="processItemsRange">The method running in parallel</param>
    /// <returns>A task to await for completion</returns>
    public static Task ForBatchedAsync(int items, ProcessItemRange processItemsRange)
    {
        if (items == 0)
            return Task.CompletedTask;

        if (poolThreadsCts == null)
        {
            lock (poolLock)
            {
                ResizePool(MaxDegreeOfParallelism);
            }
        }

        int batchCount = Math.Min(MaxDegreeOfParallelism, items);
        uint itemsPerBatch = (uint)((items + (batchCount - 1)) / batchCount);

        if (unreliablePriority)
        {
            // We'll need to yield between batches, split items further
            itemsPerBatch /= 4;
        }

        var batch = new BatchState(itemsPerBatch: itemsPerBatch, endExclusive: (uint)items, references: batchCount, processItemsRange);
        for (int i = 0; i < batchCount; i++)
            tasks.Add(batch);

        return batch.TaskCompletionSource.Task;
    }

    private static void ResizePool(int maxDegreeOfParallelism)
    {
        poolThreadsCts ??= new Stack<CancellationTokenSource>();
        while (poolThreadsCts.Count > maxDegreeOfParallelism)
        {
            var cts = poolThreadsCts.Pop();
            cts.Cancel();
        }

        while (poolThreadsCts.Count < maxDegreeOfParallelism)
        {
            var cts = new CancellationTokenSource();
            var index = poolThreadsCts.Count;
            poolThreadsCts.Push(cts);
            var thread = new Thread(ThreadScope)
            {
                Name = $"{nameof(DispatcherLowPriority)} #{index}",
                Priority = ThreadPriority.BelowNormal, // Effectively windows only
                IsBackground = true
            };
            thread.Start();
        
            void ThreadScope()
            {
                if (OperatingSystem.IsLinux())
                    LinuxNice.SetLowPriority();

                try
                {
                    foreach (var bs in tasks.GetConsumingEnumerable(cts.Token))
                        bs.Process();
                }
                catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
                {
                }
            }
        }
    }

    private sealed class BatchState(uint itemsPerBatch, uint endExclusive, int references, ProcessItemRange job)
    {
        private bool interrupted;
        private int referenceCount = references;

        private uint index, itemsDone;

        public readonly TaskCompletionSource TaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Process()
        {
            try
            {
                uint start, end;
                do
                {
                    if (unreliablePriority)
                    {
                        for (int i = 0; i < 4 && Thread.Yield(); i++)
                        {
                            // For non windows/linux OS, we'll yield a bunch between batches
                            // to let other threads take over
                        }
                    }

                    end = Interlocked.Add(ref index, itemsPerBatch);
                    start = end - itemsPerBatch;
                    if (start >= endExclusive)
                        return;

                    end = Math.Min(end, endExclusive);

                    job((int)start, (int)end);
                } while (Interlocked.Add(ref itemsDone, end - start) < endExclusive && Volatile.Read(ref interrupted) == false);

                TaskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                TaskCompletionSource.TrySetException(e);
                Volatile.Write(ref interrupted, true);
            }
            finally
            {
                var refCount = Interlocked.Decrement(ref referenceCount);
                Debug.Assert(refCount >= 0);
            }
        }
    }
    
    private static class LinuxNice
    {
        [DllImport("libc", EntryPoint = "gettid")]
        private static extern int GetTid();

        [DllImport("libc", EntryPoint = "setpriority", SetLastError = true)]
        private static extern int SetPriority(int which, int who, int nice);

        public static void SetLowPriority()
        {
            // https://man7.org/linux/man-pages/man2/setpriority.2.html
            const int lowPriority = 19; // [-20,19], -20 = high, 19 = low
            const int prioProcess = 0; // 0 = Calling process
            
            if (!OperatingSystem.IsLinux())
                throw new PlatformNotSupportedException();

            if (SetPriority(prioProcess, GetTid(), lowPriority) != 0)
                throw new Win32Exception(Marshal.GetLastPInvokeError());
        }
    }
}
