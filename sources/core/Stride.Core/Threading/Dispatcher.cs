// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// #define PROFILING_SCOPES

using System.Collections.Concurrent;
using System.Diagnostics;
#if PROFILING_SCOPES
using System.Reflection;
#endif // PROFILING_SCOPES
using System.Runtime.CompilerServices;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;

namespace Stride.Core.Threading;

public static class Dispatcher
{
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID
    public static readonly int MaxDegreeOfParallelism = 1;
#else
    public static int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;
#endif

    private static readonly ProfilingKey DispatcherSortKey = new("Dispatcher.Sort");
    private static readonly ProfilingKey DispatcherBatched = new("Dispatcher.Batched");

    public delegate void ValueAction<T>(ref T obj);

    /// <summary>
    /// The call producing the least amount of overhead, other methods are built on top of this one.
    /// </summary>
    /// <param name="items">
    /// The amount of items to process,
    /// this total will be split into multiple batches,
    /// each batch runs <typeparamref name="TJob"/>.<see cref="IBatchJob.Process"/> with the range of items for that batch
    /// </param>
    /// <param name="batchJob">
    /// An object shared across all threads running this job, if TJob is a struct each threads will work off of a unique copy of it
    /// </param>
    /// <exception cref="Exception">If any of the threads executing this job threw an exception, it will be re-thrown in the caller's scope</exception>
    public static unsafe void ForBatched<TJob>(int items, TJob batchJob) where TJob : IBatchJob
    {
        using var _ = Profiler.Begin(DispatcherBatched);

        // This scope's JIT performance is VERY fragile, be careful when tweaking it

        if (items == 0)
            return;

        if (MaxDegreeOfParallelism <= 1 || items == 1)
        {
            batchJob.Process(0, items);
            return;
        }

        int batchCount = Math.Min(MaxDegreeOfParallelism, items);
        uint itemsPerBatch = (uint)((items + (batchCount - 1)) / batchCount);

        // Performs 1/8 to 1/4 better in most cases, performs up to 1/8 worse when the ratio between
        // the duration each individual item takes and the amount of items per batch hits a very narrow sweet-spot.
        // Not entirely sure why yet.
#if FALSE
        if (items / MaxDegreeOfParallelism > 8)
            itemsPerBatch /= 4; // Batches of 2 instead of 8 to allow faster threads to steal more of the work
        else if (items / MaxDegreeOfParallelism > 4)
            itemsPerBatch /= 2; // Batches of 2 instead of 4 to allow faster threads to steal more of the work
#endif

        var batch = BatchState<TJob>.Borrow(itemsPerBatch: itemsPerBatch, endExclusive: (uint)items, references: batchCount, batchJob);
        try
        {
            ThreadPool.Instance.QueueUnsafeWorkItem(batch, &TypeAdapter<TJob>, batchCount - 1);

            ProcessBatch(batchJob, batch);

            // Might as well steal some work instead of just waiting,
            // also helps prevent potential deadlocks from badly threaded code
            while (Volatile.Read(ref batch.ItemsDone) < batch.Total && !batch.Finished.WaitOne(0))
                ThreadPool.Instance.TryCooperate();

            var ex = Interlocked.Exchange(ref batch.ExceptionThrown, null);
            if (ex != null)
                throw ex;
        }
        finally
        {
            batch.Release();
        }
    }

    private static void TypeAdapter<TJob>(object obj) where TJob : IBatchJob
    {
        var batch = obj as BatchState<TJob>; // 'as' and assert instead of direct cast to improve performance
        Debug.Assert(batch is not null);
        try
        {
            ProcessBatch(batch.Job, batch);
        }
        finally
        {
            batch.Release();
        }
    }

    private static void ProcessBatch<TJob>(TJob job, BatchState<TJob> state) where TJob : IBatchJob
    {
        try
        {
            for (uint start; (start = Interlocked.Add(ref state.Index, state.ItemsPerBatch) - state.ItemsPerBatch) < state.Total;)
            {
                uint end = Math.Min(start + state.ItemsPerBatch, state.Total);

                job.Process((int)start, (int)end);

                if (Interlocked.Add(ref state.ItemsDone, state.ItemsPerBatch) >= state.Total)
                {
                    state.Finished.Set();
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Interlocked.Exchange(ref state.ExceptionThrown, e);
            throw;
        }
    }

    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<T, int, int, void> executeBatch)
    {
        var batchedDelegate = new BatchedDelegate<T>
        {
            Param = parameter,
            Delegate = executeBatch,
        };
        ForBatched(items, batchedDelegate);
    }

    public static unsafe void ForBatched<T>(int items, ref T parameter, delegate*<ref T, int, int, void> executeBatch)
    {
        var batchedDelegate = new BatchedDelegateRef<T>
        {
            Param = parameter,
            Delegate = executeBatch,
        };
        ForBatched(items, batchedDelegate);
    }

    public static unsafe void ForBatched(int items, [Pooled] Action<int, int> executeBatch)
    {
        var batchedDelegate = new BatchedDelegate<Action<int, int>>
        {
            Param = executeBatch,
            Delegate = &ForBatchedAction,
        };
        ForBatched(items, batchedDelegate);

        static void ForBatchedAction(Action<int, int> parameter, int from, int toExclusive)
        {
            parameter(from, toExclusive);
        }
    }

    public static unsafe void For(int fromInclusive, int toExclusive, [Pooled] Action<int> action)
    {
        var parameters = (action, fromInclusive);
        ForBatched(toExclusive - fromInclusive, parameters, &ForWrapped);

        static void ForWrapped((Action<int> action, int start) parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.action(parameters.start + i);
            }
        }
    }

    public static unsafe void For<TLocal>(int fromInclusive, int toExclusive, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<int, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
    {
        var parameters = (initializeLocal, action, finalizeLocal, fromInclusive);
        ForBatched(toExclusive - fromInclusive, parameters, &ForWrapped);

        static void ForWrapped((Func<TLocal> initializeLocal, Action<int, TLocal> action, Action<TLocal> finalizeLocal, int start) parameters, int from, int toExclusive)
        {
            TLocal local = default;
            try
            {
                if (parameters.initializeLocal != null)
                {
                    local = parameters.initializeLocal.Invoke();
                }

                for (int i = from; i < toExclusive; i++)
                {
                    parameters.action(parameters.start + i, local);
                }
            }
            finally
            {
                parameters.finalizeLocal?.Invoke(local);
            }
        }
    }

    public static void ForEach<T>(T[] collection, [Pooled] Action<T> action)
    {
        ForEach<T, T[]>(collection, action);
    }

    public static void ForEach<T>(ConcurrentCollector<T> collection, [Pooled] Action<T> action)
    {
        ForEach<T, ConcurrentCollector<T>>(collection, action);
    }

    public static void ForEach<T>(List<T> collection, [Pooled] Action<T> action)
    {
        ForEach<T, List<T>>(collection, action);
    }

    public static void ForEach<T, TLocal>(ConcurrentCollector<T> collection, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<T, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
    {
        ForEach<T, TLocal, ConcurrentCollector<T>>(collection, initializeLocal, action, finalizeLocal);
    }

    public static void ForEach<T>(FastCollection<T> collection, [Pooled] Action<T> action)
    {
        ForEach<T, FastCollection<T>>(collection, action);
    }

    public static unsafe void ForEach<T>(ConcurrentCollector<T> collection, [Pooled] ValueAction<T> action)
    {
        var parameters = (action, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList((ValueAction<T> action, ConcurrentCollector<T> collection) parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.action(ref parameters.collection.Items[i]);
            }
        }
    }

    public static unsafe void ForEach<T, TList>(TList collection, [Pooled] Action<T> action) where TList : IReadOnlyList<T>
    {
        var parameters = (action, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList((Action<T> action, TList collection) parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.action(parameters.collection[i]);
            }
        }
    }

    public static unsafe void ForEach<TItem, TLocal, TList>(TList collection, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<TItem, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null) where TList : IReadOnlyList<TItem>
    {
        var parameters = (initializeLocal, action, finalizeLocal, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList((Func<TLocal> initializeLocal, Action<TItem, TLocal> action, Action<TLocal> finalizeLocal, TList collection) parameters, int from, int toExclusive)
        {
            TLocal local = default;
            try
            {
                if (parameters.initializeLocal != null)
                {
                    local = parameters.initializeLocal.Invoke();
                }

                for (int i = from; i < toExclusive; i++)
                {
                    parameters.action(parameters.collection[i], local);
                }
            }
            finally
            {
                parameters.finalizeLocal?.Invoke(local);
            }
        }
    }

    public static unsafe void ForEach<TKey, TValue>(Dictionary<TKey, TValue> collection, [Pooled] Action<KeyValuePair<TKey, TValue>> action)
        where TKey : notnull
    {
        var parameters = (action, collection);
        ForBatched(collection.Count, parameters, &ForEachDict);

        static void ForEachDict((Action<KeyValuePair<TKey, TValue>> action, Dictionary<TKey, TValue> collection) parameters, int from, int toExclusive)
        {
            using var enumerator = parameters.collection.GetEnumerator();

            // Skip to offset
            for (int i = 0; i < from; i++)
            {
                enumerator.MoveNext();
            }

            // Process batch
            for (int i = from; i < toExclusive && enumerator.MoveNext(); i++)
            {
                parameters.action(enumerator.Current);
            }
        }
    }

    public static unsafe void ForEach<TKey, TValue, TLocal>(Dictionary<TKey, TValue> collection, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<KeyValuePair<TKey, TValue>, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
        where TKey : notnull
    {
        var parameters = (initializeLocal, action, finalizeLocal, collection);
        ForBatched(collection.Count, parameters, &ForEachDict);

        static void ForEachDict((Func<TLocal> initializeLocal, Action<KeyValuePair<TKey, TValue>, TLocal> action, Action<TLocal> finalizeLocal, Dictionary<TKey, TValue> collection) parameters, int from, int toExclusive)
        {
            using var enumerator = parameters.collection.GetEnumerator();

            for (int i = 0; i < from; i++) // Skip to the start of our batch
            {
                enumerator.MoveNext();
            }

            TLocal local = default;
            try
            {
                if (parameters.initializeLocal != null)
                    local = parameters.initializeLocal.Invoke();

                for (int i = from; i < toExclusive && enumerator.MoveNext(); i++)
                {
                    parameters.action(enumerator.Current, local);
                }
            }
            finally
            {
                parameters.finalizeLocal?.Invoke(local);
            }
        }
    }

    public static void Sort<T>(ConcurrentCollector<T> collection, IComparer<T> comparer)
    {
        Sort(collection.Items, 0, collection.Count, comparer);
    }

    public static void Sort<T>(FastList<T> collection, IComparer<T> comparer)
    {
        Sort(collection.Items, 0, collection.Count, comparer);
    }

    public static void Sort<T>(T[] collection, int index, int length, IComparer<T> comparer)
    {
        using var _ = Profiler.Begin(DispatcherSortKey);

        if (length <= 0)
            return;

        var state = SortState.Acquire(MaxDegreeOfParallelism);

        try
        {
            // Initial partition
            Interlocked.Increment(ref state.OpLeft);
            state.Partitions.Enqueue(new SortRange(index, length - 1));

            // Sort recursively
            state.AddReference();
            SortOnThread(collection, comparer, state);

            // Wait for all work to finish
            state.WaitCompletion();
        }
        finally
        {
            state.Release();
        }
    }

    private static void SortOnThread<T>(T[] collection, IComparer<T> comparer, SortState state)
    {
        const int sequentialThreshold = 2048;

        var hasChild = false;
        try
        {
            var sw = new SpinWait();
            while (Volatile.Read(ref state.OpLeft) != 0)
            {
                if (!state.Partitions.TryDequeue(out var range))
                {
                    sw.SpinOnce();
                    continue;
                }

                if (range.Right - range.Left < sequentialThreshold)
                {
                    // Sort small collections sequentially
                    Array.Sort(collection, range.Left, range.Right - range.Left + 1, comparer);
                    Interlocked.Decrement(ref state.OpLeft);
                }
                else
                {
                    var pivot = Partition(collection, range.Left, range.Right, comparer);

                    int delta = -1;
                    // Add work items
                    if (pivot - 1 > range.Left)
                        delta++;

                    if (range.Right > pivot + 1)
                        delta++;

                    Interlocked.Add(ref state.OpLeft, delta);

                    if (pivot - 1 > range.Left)
                        state.Partitions.Enqueue(new SortRange(range.Left, pivot - 1));


                    if (range.Right > pivot + 1)
                        state.Partitions.Enqueue(new SortRange(pivot + 1, range.Right));


                    // Add a new worker if necessary
                    if (!hasChild)
                    {
                        var w = Interlocked.Decrement(ref state.MaxWorkerCount);
                        if (w >= 0)
                        {
                            state.AddReference();
                            ThreadPool.Instance.QueueWorkItem(() => SortOnThread(collection, comparer, state));
                        }
                        hasChild = true;
                    }
                }
            }
        }
        finally
        {
            if (Volatile.Read(ref state.OpLeft) == 0)
                state.Finished.Set();
            state.Release();
        }
    }

    private static int Partition<T>(T[] collection, int left, int right, IComparer<T> comparer)
    {
        int i = left, j = right;
        var mid = (left + right) / 2;

        if (comparer.Compare(collection[right], collection[left]) < 0)
            Swap(collection, left, right);
        if (comparer.Compare(collection[mid], collection[left]) < 0)
            Swap(collection, left, mid);
        if (comparer.Compare(collection[right], collection[mid]) < 0)
            Swap(collection, mid, right);

        while (i <= j)
        {
            var pivot = collection[mid];

            while (comparer.Compare(collection[i], pivot) < 0)
            {
                i++;
            }

            while (comparer.Compare(collection[j], pivot) > 0)
            {
                j--;
            }

            if (i <= j)
            {
                Swap(collection, i++, j--);
            }
        }

        return mid;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void Swap<T>(T[] collection, int i, int j)
    {
        (collection[j], collection[i]) = (collection[i], collection[j]);
    }

    /// <summary>
    /// An implementation of a job running in batches.
    /// Implementing this as a struct improves performance as the JIT would have an easier time inlining the call.
    /// Implementing this as a class would provide more utility as this object would be shared across all threads,
    /// allowing for interlocked operations and other communication between threads.
    /// </summary>
    public interface IBatchJob
    {
        /// <summary>
        /// Execute this job over a range of items
        /// </summary>
        /// <param name="start">the start of the range</param>
        /// <param name="endExclusive">the end of the range, iterate as long as i &lt; endExclusive</param>
        void Process(int start, int endExclusive);
    }

    private sealed class BatchState<TJob> where TJob : IBatchJob
    {
        private static readonly ConcurrentStack<BatchState<TJob>> Pool = new();

        private int referenceCount;

        public readonly ManualResetEvent Finished = new(false);

        public uint Index, Total, ItemsPerBatch, ItemsDone;

        public TJob Job;

        public Exception? ExceptionThrown;

        public static BatchState<TJob> Borrow(uint itemsPerBatch, uint endExclusive, int references, TJob job)
        {
            if (!Pool.TryPop(out var state))
                state = new();

            state.Index = 0;
            state.Total = endExclusive;
            state.ItemsPerBatch = itemsPerBatch;
            state.ItemsDone = 0;
            state.ExceptionThrown = null;
            state.referenceCount = references;
            state.Job = job;
            return state;
        }

        public void Release()
        {
            var refCount = Interlocked.Decrement(ref referenceCount);
            if (refCount == 0)
            {
                Job = default; // Clear any references it may hold onto
                Finished.Reset();
                Pool.Push(this);
            }
            Debug.Assert(refCount >= 0);
        }
    }

    struct BatchedDelegateRef<T> : IBatchJob
    {
        public T Param;
        public unsafe delegate*<ref T, int, int, void> Delegate;

        public unsafe void Process(int start, int endExclusive)
        {
            Delegate(ref Param, start, endExclusive);
        }
    }

    struct BatchedDelegate<T> : IBatchJob
    {
        public T Param;
        public unsafe delegate*<T, int, int, void> Delegate;

        public readonly unsafe void Process(int start, int endExclusive)
        {
            Delegate(Param, start, endExclusive);
        }
    }

    private readonly struct SortRange
    {
        public readonly int Left;

        public readonly int Right;

        public SortRange(int left, int right)
        {
            Left = left;
            Right = right;
        }
    }

    private class SortState
    {
        private static readonly ConcurrentPool<SortState> Pool = new(() => new SortState());

        private int referenceCount;

        public readonly ManualResetEvent Finished = new(false);

        public readonly ConcurrentQueue<SortRange> Partitions = new();

        public int MaxWorkerCount;

        public int OpLeft;

        public static SortState Acquire(int MaxWorkerCount)
        {
            var state = Pool.Acquire();
            state.referenceCount = 1;
            state.OpLeft = 0;
            state.MaxWorkerCount = MaxWorkerCount;
            state.Finished.Reset();
            return state;
        }

        public void AddReference()
        {
            Interlocked.Increment(ref referenceCount);
        }

        public void Release()
        {
            if (Interlocked.Decrement(ref referenceCount) == 0)
            {
                Pool.Release(this);
            }
        }

        public void WaitCompletion()
        {
            // Might as well steal some work instead of just waiting,
            // also helps prevent potential deadlocks from badly threaded code
            while (Volatile.Read(ref OpLeft) != 0 && !Finished.WaitOne(0))
                ThreadPool.Instance.TryCooperate();
        }
    }

#if PROFILING_SCOPES
    private class DispatcherNode
    {
        public MethodBase Caller;
        public int Count;
        public TimeSpan TotalTime;
    }
    private static ConcurrentDictionary<MethodInfo, DispatcherNode> nodes = new ConcurrentDictionary<MethodInfo, DispatcherNode>();
#endif
    private struct ProfilingScope : IDisposable
    {
#if PROFILING_SCOPES
        public Stopwatch Stopwatch;
        public Delegate Action;
#endif
        public readonly void Dispose()
        {
#if PROFILING_SCOPES
            Stopwatch.Stop();
            var elapsed = Stopwatch.Elapsed;

            DispatcherNode node;
            if (!nodes.TryGetValue(Action.Method, out node))
            {
                int skipFrames = 1;
                MethodBase caller = null;

                do
                {
                    caller = new StackFrame(skipFrames++, true).GetMethod();
                }
                while (caller.DeclaringType == typeof(Dispatcher));
                
                node = nodes.GetOrAdd(Action.Method, key => new DispatcherNode());
                node.Caller = caller;
            }

            node.Count++;
            node.TotalTime += elapsed;

            if (node.Count % 500 == 0)
            {
                Console.WriteLine($"[{node.Count}] {node.Caller.DeclaringType.Name}.{node.Caller.Name}: {node.TotalTime.TotalMilliseconds / node.Count}");
            }
#endif
        }
    }
}
