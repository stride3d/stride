// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// #define PROFILING_SCOPES

using System.Collections.Concurrent;
using System.Diagnostics;
#if PROFILING_SCOPES
using System.Reflection;
#endif // PROFILING_SCOPES
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using Stride.Core.Collections;
using Stride.Core.Diagnostics;

namespace Stride.Core.Threading;

public static class Dispatcher
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
            OnMaxDegreeOfParallelismChanged?.Invoke();
        }
    } = int.TryParse(Environment.GetEnvironmentVariable("STRIDE_MAX_PARALLELISM"), out var envValue) && envValue > 0 
        ? envValue
        : Environment.ProcessorCount;
#endif

    private static readonly ProfilingKey DispatcherSortKey = new("Dispatcher.Sort");
    private static readonly ProfilingKey DispatcherBatched = new("Dispatcher.Batched");

    public static event Action? OnMaxDegreeOfParallelismChanged;
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

        // One helper for now, each helper wakes the next one while items are left (see TypeAdapter).
        // Waking every helper from here costs the caller a system call per sleeping thread before it starts its own share,
        // and on a short job most of them arrive once all the items are taken.
        var batch = BatchState<TJob>.Borrow(itemsPerBatch: itemsPerBatch, endExclusive: (uint)items, references: 2, helpersLeft: batchCount - 2, batchJob);
        try
        {
            ThreadPool.Instance.QueueUnsafeWorkItem(batch, &TypeAdapter<TJob>);

            ProcessBatch(batchJob, batch);

            // Might as well steal some work instead of just waiting,
            // also helps prevent potential deadlocks from badly threaded code
            while (Volatile.Read(ref batch.ItemsDone) < batch.Total && Volatile.Read(ref batch.ExceptionThrown) == null)
                ThreadPool.Instance.TryCooperate();

            var ex = Interlocked.Exchange(ref batch.ExceptionThrown, null);
            if (ex != null)
                ExceptionDispatchInfo.Capture(ex).Throw();
        }
        finally
        {
            batch.Release();
        }
    }

    private static unsafe void TypeAdapter<TJob>(object obj) where TJob : IBatchJob
    {
        var batch = obj as BatchState<TJob>; // 'as' and assert instead of direct cast to improve performance
        Debug.Assert(batch is not null);
        try
        {
            // Wake the next helper before this one's share, only while items are left to take
            if (Volatile.Read(ref batch.Index) < batch.Total && Interlocked.Decrement(ref batch.HelpersLeft) >= 0)
            {
                batch.AddReference();
                ThreadPool.Instance.QueueUnsafeWorkItem(batch, &TypeAdapter<TJob>);
            }

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
            uint start, end;
            do
            {
                end = Interlocked.Add(ref state.Index, state.ItemsPerBatch);
                start = end - state.ItemsPerBatch;
                if (start >= state.Total)
                    break;

                end = Math.Min(end, state.Total);

                job.Process((int)start, (int)end);
            } while (Interlocked.Add(ref state.ItemsDone, end - start) < state.Total);
        }
        catch (Exception e)
        {
            Interlocked.Exchange(ref state.ExceptionThrown, e);
            // Unblock the waiter; it rethrows via ExceptionDispatchInfo. Rethrowing here would
            // escape the worker thread's root callback and fail-fast the process.
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

    [Obsolete("This method will be removed in the future alongside FastList. We will have an alternative using Span<T> which you will be able to fallback to.")]
    public static void Sort<T>(FastList<T> collection, IComparer<T> comparer)
    {
        Sort(collection.Items, 0, collection.Count, comparer);
    }

    /// <summary>
    /// A range of at most this many items is sorted with <see cref="Array.Sort{T}(T[], int, int, IComparer{T})"/> on one thread.
    /// </summary>
    private const int SortSequentialThreshold = 2048;

    /// <summary>
    /// Sorts a range of <paramref name="collection"/>, splitting a range of more than 2,048 items between threads.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="comparer"/> threw inside <see cref="Array.Sort{T}(T[], int, int, IComparer{T})"/>, which sorts every
    /// range of at most 2,048 items; the comparer's exception is the inner exception.
    /// </exception>
    /// <exception cref="Exception">
    /// <paramref name="comparer"/> threw while a larger range was split; it is rethrown once the other threads have stopped.
    /// </exception>
    public static void Sort<T>(T[] collection, int index, int length, IComparer<T> comparer)
    {
        using var _ = Profiler.Begin(DispatcherSortKey);

        if (length <= 0)
            return;

        // A list the parallel sort would sort on one thread anyway: no shared state, queue or wait
        if (length <= SortSequentialThreshold || MaxDegreeOfParallelism <= 1)
        {
            Array.Sort(collection, index, length, comparer);
            return;
        }

        var state = SortState.Acquire(MaxDegreeOfParallelism);

        try
        {
            // Initial partition
            Interlocked.Increment(ref state.OpLeft);
            state.Partitions.Enqueue(new SortRange(index, index + length - 1));

            // Sort recursively
            state.AddReference();
            SortOnThread(collection, comparer, state);

            // Wait for all work to finish
            state.WaitCompletion();

            var ex = Volatile.Read(ref state.ExceptionThrown);
            if (ex != null)
                ExceptionDispatchInfo.Capture(ex).Throw();
        }
        finally
        {
            state.Release();
        }
    }

    private static void SortOnThread<T>(T[] collection, IComparer<T> comparer, SortState state)
    {
        var hasChild = false;
        try
        {
            var sw = new SpinWait();
            while (Volatile.Read(ref state.OpLeft) != 0 && Volatile.Read(ref state.ExceptionThrown) == null)
            {
                if (!state.Partitions.TryDequeue(out var range))
                {
                    // Other threads are still sorting their parts, for well under a millisecond: no Sleep(1), which lasts until
                    // the next timer tick (15.6 ms by default on Windows)
                    sw.SpinOnce(sleep1Threshold: -1);
                    continue;
                }

                if (range.Right - range.Left < SortSequentialThreshold)
                {
                    // Sort small collections sequentially
                    Array.Sort(collection, range.Left, range.Right - range.Left + 1, comparer);
                    Interlocked.Decrement(ref state.OpLeft);
                }
                else
                {
                    var (leftEnd, rightStart) = Partition(collection, range.Left, range.Right, comparer);

                    int delta = -1;
                    // Add work items
                    if (leftEnd > range.Left)
                        delta++;

                    if (range.Right > rightStart)
                        delta++;

                    Interlocked.Add(ref state.OpLeft, delta);

                    if (leftEnd > range.Left)
                        state.Partitions.Enqueue(new SortRange(range.Left, leftEnd));

                    if (range.Right > rightStart)
                        state.Partitions.Enqueue(new SortRange(rightStart, range.Right));


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
        catch (Exception e)
        {
            // Stops the other threads; the caller rethrows it. Rethrowing here would escape the worker thread's root
            // callback and fail-fast the process
            Interlocked.CompareExchange(ref state.ExceptionThrown, e, null);
        }
        finally
        {
            if (Volatile.Read(ref state.OpLeft) == 0)
                state.Finished.Set();
            state.Release();
        }
    }

    /// <summary>
    /// Hoare partition around the median of three: afterwards, items up to <c>LeftEnd</c> are not greater than the pivot,
    /// items from <c>RightStart</c> are not smaller, and items between the two are equal to it, already in place.
    /// </summary>
    private static (int LeftEnd, int RightStart) Partition<T>(T[] collection, int left, int right, IComparer<T> comparer)
    {
        int i = left, j = right;
        var mid = (left + right) / 2;

        if (comparer.Compare(collection[right], collection[left]) < 0)
            Swap(collection, left, right);
        if (comparer.Compare(collection[mid], collection[left]) < 0)
            Swap(collection, left, mid);
        if (comparer.Compare(collection[right], collection[mid]) < 0)
            Swap(collection, mid, right);

        // The value, not the slot: the swaps below can move the item at mid
        var pivot = collection[mid];
        while (i <= j)
        {
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

        return (j, i);
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

        public uint Index, Total, ItemsPerBatch, ItemsDone;

        /// <summary>The helpers that may still be woken, see <see cref="TypeAdapter{TJob}"/>.</summary>
        public int HelpersLeft;

        public TJob Job;

        public Exception? ExceptionThrown;

        public static BatchState<TJob> Borrow(uint itemsPerBatch, uint endExclusive, int references, int helpersLeft, TJob job)
        {
            if (!Pool.TryPop(out var state))
                state = new();

            state.Index = 0;
            state.Total = endExclusive;
            state.ItemsPerBatch = itemsPerBatch;
            state.ItemsDone = 0;
            state.ExceptionThrown = null;
            state.referenceCount = references;
            state.HelpersLeft = helpersLeft;
            state.Job = job;
            return state;
        }

        public void AddReference() => Interlocked.Increment(ref referenceCount);

        public void Release()
        {
            var refCount = Interlocked.Decrement(ref referenceCount);
            if (refCount == 0)
            {
                Job = default; // Clear any references it may hold onto
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

        public Exception? ExceptionThrown;

        public static SortState Acquire(int MaxWorkerCount)
        {
            var state = Pool.Acquire();
            state.referenceCount = 1;
            state.OpLeft = 0;
            state.MaxWorkerCount = MaxWorkerCount;
            state.ExceptionThrown = null;
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
                // An exception leaves ranges in the queue. Clear() allocates a new segment even on an empty queue
                if (!Partitions.IsEmpty)
                    Partitions.Clear();
                Pool.Release(this);
            }
        }

        public void WaitCompletion()
        {
            // Might as well steal some work instead of just waiting,
            // also helps prevent potential deadlocks from badly threaded code
            while (Volatile.Read(ref OpLeft) != 0 && Volatile.Read(ref ExceptionThrown) == null && !Finished.WaitOne(0))
                ThreadPool.Instance.TryCooperate();

            // After an exception, waits for the helpers to stop, so that none writes to the list once Sort has thrown
            if (Volatile.Read(ref ExceptionThrown) != null)
            {
                while (Volatile.Read(ref referenceCount) > 1)
                    ThreadPool.Instance.TryCooperate();
            }
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
