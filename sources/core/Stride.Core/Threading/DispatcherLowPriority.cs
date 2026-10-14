// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Stride.Core.Threading;

/// <summary>
/// Built for long-running background jobs,
/// won't monopolize the CPU away from this process or the rest of the user's OS while performing tasks
/// </summary>
public static class DispatcherLowPriority
{
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID
    public static readonly int MaxDegreeOfParallelism = 1;
#else
    public static int MaxDegreeOfParallelism
    {
        get;
        set
        {
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
                Priority = ThreadPriority.BelowNormal,
                IsBackground = true
            };
            thread.Start();
        
            void ThreadScope()
            {
                try
                {
                    foreach (var bs in tasks.GetConsumingEnumerable(cts.Token))
                        bs.Process();
                }
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException && cts.Token.IsCancellationRequested)
                {
                }
            }
        }
    }

    private sealed class BatchState
    {
        private int referenceCount;

        public uint Index, Total, ItemsPerBatch, ItemsDone;

        public ProcessItemRange Job;

        public readonly TaskCompletionSource TaskCompletionSource = new();

        public BatchState(uint itemsPerBatch, uint endExclusive, int references, ProcessItemRange job)
        {
            Index = 0;
            Total = endExclusive;
            ItemsPerBatch = itemsPerBatch;
            ItemsDone = 0;
            referenceCount = references;
            Job = job;
        }

        public void Process()
        {
            try
            {
                uint start, end;
                do
                {
                    end = Interlocked.Add(ref Index, ItemsPerBatch);
                    start = end - ItemsPerBatch;
                    if (start >= Total)
                        return;

                    end = Math.Min(end, Total);

                    Job((int)start, (int)end);
                } while (Interlocked.Add(ref ItemsDone, end - start) < Total);

                TaskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                TaskCompletionSource.SetException(e);
            }
            finally
            {
                var refCount = Interlocked.Decrement(ref referenceCount);
                Debug.Assert(refCount >= 0);
            }
        }
    }
}
