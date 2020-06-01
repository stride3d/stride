// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

//#define PROFILING_SCOPES

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Stride.Core.Annotations;
using Stride.Core.Collections;

namespace Stride.Core.Threading
{
    public class Dispatcher
    {
#if STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_ANDROID
        public static int MaxDegreeOfParallelism = 1;
#else
        public static int MaxDegreeOfParallelism = Environment.ProcessorCount;
#endif

        public delegate void ValueAction<T>(ref T obj);

        public static void For(int fromInclusive, int toExclusive, [Pooled] Action<int> action)
        {
            using (Profile(action))
            {
                if (fromInclusive > toExclusive)
                {
                    var temp = fromInclusive;
                    fromInclusive = toExclusive + 1;
                    toExclusive = temp + 1;
                }

                var count = toExclusive - fromInclusive;
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    ExecuteBatch(fromInclusive, toExclusive, action);
                }
                else
                {
                    var state = BatchState.Acquire();
                    state.WorkDone = state.StartInclusive = fromInclusive;

                    try
                    {
                        var batchCount = Math.Min(MaxDegreeOfParallelism, count);
                        var batchSize = (count + (batchCount - 1)) / batchCount;

                        // Kick off a worker, then perform work synchronously
                        state.AddReference();
                        Fork(toExclusive, batchSize, MaxDegreeOfParallelism, action, state);

                        // Wait for all workers to finish
                        state.WaitCompletion(toExclusive);

                        var ex = Interlocked.Exchange(ref state.ExceptionThrown, null);
                        if (ex != null)
                            throw ex;
                    }
                    finally
                    {
                        state.Release();
                    }
                }
            }
        }

        public static void For<TLocal>(int fromInclusive, int toExclusive, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<int, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                if (fromInclusive > toExclusive)
                {
                    var temp = fromInclusive;
                    fromInclusive = toExclusive + 1;
                    toExclusive = temp + 1;
                }

                var count = toExclusive - fromInclusive;
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    ExecuteBatch(fromInclusive, toExclusive, initializeLocal, action, finalizeLocal);
                }
                else
                {
                    var state = BatchState.Acquire();
                    state.WorkDone = state.StartInclusive = fromInclusive;

                    try
                    {
                        var batchCount = Math.Min(MaxDegreeOfParallelism, count);
                        var batchSize = (count + (batchCount - 1)) / batchCount;

                        // Kick off a worker, then perform work synchronously
                        state.AddReference();
                        Fork(toExclusive, batchSize, MaxDegreeOfParallelism, initializeLocal, action, finalizeLocal, state);

                        // Wait for all workers to finish
                        state.WaitCompletion(toExclusive);

                        var ex = Interlocked.Exchange(ref state.ExceptionThrown, null);
                        if (ex != null)
                            throw ex;
                    }
                    finally
                    {
                        state.Release();
                    }
                }
            }
        }
        
        public static void ForEach<TItem, TLocal>([NotNull] IReadOnlyList<TItem> collection, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<TItem, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
        {
            For(0, collection.Count, initializeLocal, (i, local) => action(collection[i], local), finalizeLocal);
        }

        public static void ForEach<T>([NotNull] IReadOnlyList<T> collection, [Pooled] Action<T> action)
        {
            For(0, collection.Count, i => action(collection[i]));
        }

        public static void ForEach<T>([NotNull] List<T> collection, [Pooled] Action<T> action)
        {
            For(0, collection.Count, i => action(collection[i]));
        }

        public static void ForEach<TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, [Pooled] Action<KeyValuePair<TKey, TValue>> action)
        {
            if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
            {
                ExecuteBatch(collection, 0, collection.Count, action);
            }
            else
            {
                var state = BatchState.Acquire();

                try
                {
                    var batchCount = Math.Min(MaxDegreeOfParallelism, collection.Count);
                    var batchSize = (collection.Count + (batchCount - 1)) / batchCount;

                    // Kick off a worker, then perform work synchronously
                    state.AddReference();
                    Fork(collection, batchSize, MaxDegreeOfParallelism, action, state);

                    // Wait for all workers to finish
                    state.WaitCompletion(collection.Count);

                    var ex = Interlocked.Exchange(ref state.ExceptionThrown, null);
                    if (ex != null)
                        throw ex;
                }
                finally
                {
                    state.Release();
                }
            }
        }

        public static void ForEach<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<KeyValuePair<TKey, TValue>, TLocal> action, [Pooled] Action<TLocal> finalizeLocal = null)
        {
            if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
            {
                ExecuteBatch(collection, 0, collection.Count, initializeLocal, action, finalizeLocal);
            }
            else
            {
                var state = BatchState.Acquire();

                try
                {
                    var batchCount = Math.Min(MaxDegreeOfParallelism, collection.Count);
                    var batchSize = (collection.Count + (batchCount - 1)) / batchCount;

                    // Kick off a worker, then perform work synchronously
                    state.AddReference();
                    Fork(collection, batchSize, MaxDegreeOfParallelism, initializeLocal, action, finalizeLocal, state);

                    // Wait for all workers to finish
                    state.WaitCompletion(collection.Count);

                    var ex = Interlocked.Exchange(ref state.ExceptionThrown, null);
                    if (ex != null)
                        throw ex;
                }
                finally
                {
                    state.Release();
                }
            }
        }

        public static void ForEach<T>([NotNull] FastCollection<T> collection, [Pooled] Action<T> action)
        {
            For(0, collection.Count, i => action(collection[i]));
        }

        public static void ForEach<T>([NotNull] FastList<T> collection, [Pooled] Action<T> action)
        {
            For(0, collection.Count, i => action(collection.Items[i]));
        }

        public static void ForEach<T>([NotNull] ConcurrentCollector<T> collection, [Pooled] Action<T> action)
        {
            For(0, collection.Count, i => action(collection.Items[i]));
        }

        public static void ForEach<T>([NotNull] FastList<T> collection, [Pooled] ValueAction<T> action)
        {
            For(0, collection.Count, i => action(ref collection.Items[i]));
        }

        public static void ForEach<T>([NotNull] ConcurrentCollector<T> collection, [Pooled] ValueAction<T> action)
        {
            For(0, collection.Count, i => action(ref collection.Items[i]));
        }

        private static void Fork<TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, int batchSize, int maxDegreeOfParallelism, [Pooled] Action<KeyValuePair<TKey, TValue>> action, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started.
            if (state.StartInclusive >= collection.Count)
            {
                state.Release();
                return;
            }

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < collection.Count)
            {
                int workToSchedule = maxDegreeOfParallelism - 1;
                for (int i = 0; i < workToSchedule; i++)
                {
                    state.AddReference();
                }
                ThreadPool.Instance.QueueWorkItem(() => Fork(collection, batchSize, 0, action, state), workToSchedule);
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < collection.Count)
                {
                    try
                    {
                        // TODO: Reuse enumerator when processing multiple batches synchronously
                        var start = newStart - batchSize;
                        ExecuteBatch(collection, newStart - batchSize, Math.Min(collection.Count, newStart) - start, action);
                    }
                    finally
                    {
                        if (Interlocked.Add(ref state.WorkDone, batchSize) >= collection.Count)
                        {
                             // Don't wait for other threads to wake up and signal the BatchState, release as soon as work is finished
                            state.Finished.Set();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref state.ExceptionThrown, e);
                throw;
            }
            finally
            {
                state.Release();
            }
        }

        private static void Fork<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, int batchSize, int maxDegreeOfParallelism, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<KeyValuePair<TKey, TValue>, TLocal> action, [Pooled] Action<TLocal> finalizeLocal, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started.
            if (state.StartInclusive >= collection.Count)
            {
                state.Release();
                return;
            }

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < collection.Count)
            {
                int workToSchedule = maxDegreeOfParallelism - 1;
                for (int i = 0; i < workToSchedule; i++)
                {
                    state.AddReference();
                }
                ThreadPool.Instance.QueueWorkItem(() => Fork(collection, batchSize, 0, initializeLocal, action, finalizeLocal, state), workToSchedule);
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < collection.Count)
                {
                    try
                    {
                        // TODO: Reuse enumerator when processing multiple batches synchronously
                        var start = newStart - batchSize;
                        ExecuteBatch(collection, newStart - batchSize, Math.Min(collection.Count, newStart) - start, initializeLocal, action, finalizeLocal);
                    }
                    finally
                    {
                        if (Interlocked.Add(ref state.WorkDone, batchSize) >= collection.Count)
                        {
                             // Don't wait for other threads to wake up and signal the BatchState, release as soon as work is finished
                            state.Finished.Set();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref state.ExceptionThrown, e);
                throw;
            }
            finally
            {
                state.Release();
            }
        }

        private static void ExecuteBatch(int fromInclusive, int toExclusive, [Pooled] Action<int> action)
        {
            for (var i = fromInclusive; i < toExclusive; i++)
            {
                action(i);
            }
        }

        private static void ExecuteBatch<TLocal>(int fromInclusive, int toExclusive, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<int, TLocal> action, [Pooled] Action<TLocal> finalizeLocal)
        {
            var local = default(TLocal);
            try
            {
                if (initializeLocal != null)
                {
                    local = initializeLocal();
                }

                for (var i = fromInclusive; i < toExclusive; i++)
                {
                    action(i, local);
                }
            }
            finally
            {
                finalizeLocal?.Invoke(local);
            }
        }

        private static void Fork(int endExclusive, int batchSize, int maxDegreeOfParallelism, [Pooled] Action<int> action, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started.
            if (state.StartInclusive >= endExclusive)
            {
                state.Release();
                return;
            }

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < endExclusive)
            {
                int workToSchedule = maxDegreeOfParallelism - 1;
                for (int i = 0; i < workToSchedule; i++)
                {
                    state.AddReference();
                }
                ThreadPool.Instance.QueueWorkItem(() => Fork(endExclusive, batchSize, 0, action, state), workToSchedule);
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < endExclusive)
                {
                    try
                    {
                        ExecuteBatch(newStart - batchSize, Math.Min(endExclusive, newStart), action);
                    }
                    finally
                    {
                        if (Interlocked.Add(ref state.WorkDone, batchSize) >= endExclusive)
                        {
                             // Don't wait for other threads to wake up and signal the BatchState, release as soon as work is finished
                            state.Finished.Set();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref state.ExceptionThrown, e);
                throw;
            }
            finally
            {
                state.Release();
            }
        }

        private static void Fork<TLocal>(int endExclusive, int batchSize, int maxDegreeOfParallelism, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<int, TLocal> action, [Pooled] Action<TLocal> finalizeLocal, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started.
            if (state.StartInclusive >= endExclusive)
            {
                state.Release();
                return;
            }

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < endExclusive)
            {
                int workToSchedule = maxDegreeOfParallelism - 1;
                for (int i = 0; i < workToSchedule; i++)
                {
                    state.AddReference();
                }
                ThreadPool.Instance.QueueWorkItem(() => Fork(endExclusive, batchSize, 0, initializeLocal, action, finalizeLocal, state), workToSchedule);
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < endExclusive)
                {
                    try
                    {
                        ExecuteBatch(newStart - batchSize, Math.Min(endExclusive, newStart), initializeLocal, action, finalizeLocal);
                    }
                    finally
                    {
                        if (Interlocked.Add(ref state.WorkDone, batchSize) >= endExclusive)
                        {
                            // Don't wait for other threads to wake up and signal the BatchState, release as soon as work is finished
                            state.Finished.Set();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Interlocked.Exchange(ref state.ExceptionThrown, e);
                throw;
            }
            finally
            {
                state.Release();
            }
        }

        private static void ExecuteBatch<TKey, TValue>([NotNull] Dictionary<TKey, TValue> dictionary, int offset, int count, [Pooled] Action<KeyValuePair<TKey, TValue>> action)
        {
            var enumerator = dictionary.GetEnumerator();
            var index = 0;

            // Skip to offset
            while (index < offset && enumerator.MoveNext())
            {
                index++;
            }

            // Process batch
            while (index < offset + count && enumerator.MoveNext())
            {
                action(enumerator.Current);
                index++;
            }
        }

        private static void ExecuteBatch<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> dictionary, int offset, int count, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<KeyValuePair<TKey, TValue>, TLocal> action, [Pooled] Action<TLocal> finalizeLocal)
        {
            var local = default(TLocal);
            try
            {
                if (initializeLocal != null)
                {
                    local = initializeLocal();
                }

                var enumerator = dictionary.GetEnumerator();
                var index = 0;

                // Skip to offset
                while (index < offset && enumerator.MoveNext())
                {
                    index++;
                }

                // Process batch
                while (index < offset + count && enumerator.MoveNext())
                {
                    action(enumerator.Current, local);
                    index++;
                }
            }
            finally
            {
                finalizeLocal?.Invoke(local);
            }
        }

        public static void Sort<T>([NotNull] ConcurrentCollector<T> collection, IComparer<T> comparer)
        {
            Sort(collection.Items, 0, collection.Count, comparer);
        }

        public static void Sort<T>([NotNull] FastList<T> collection, IComparer<T> comparer)
        {
            Sort(collection.Items, 0, collection.Count, comparer);
        }

        public static void Sort<T>(T[] collection, int index, int length, IComparer<T> comparer)
        {
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

        private static void SortOnThread<T>(T[] collection, IComparer<T> comparer, [NotNull] SortState state)
        {
            const int sequentialThreshold = 2048;

            var hasChild = false;
            try
            {
                var sw = new SpinWait();
                while (Volatile.Read(ref state.OpLeft) != 0)
                {
                    if (state.Partitions.TryDequeue(out var range) == false)
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
                if(Volatile.Read(ref state.OpLeft) == 0)
                    state.Finished.Set();
                state.Release();
            }
        }

        private static int Partition<T>([NotNull] T[] collection, int left, int right, [NotNull] IComparer<T> comparer)
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
        private static void Swap<T>([NotNull] T[] collection, int i, int j)
        {
            var temp = collection[i];
            collection[i] = collection[j];
            collection[j] = temp;
        }

        private class BatchState
        {
            private static readonly ConcurrentPool<BatchState> Pool = new ConcurrentPool<BatchState>(() => new BatchState());

            private int referenceCount;

            public readonly ManualResetEvent Finished = new ManualResetEvent(false);

            public int StartInclusive;

            public int WorkDone;

            public Exception ExceptionThrown;

            [NotNull]
            public static BatchState Acquire()
            {
                var state = Pool.Acquire();
                state.referenceCount = 1;
                state.StartInclusive = 0;
                state.WorkDone = 0;
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
                    Pool.Release(this);
                }
            }
            
            public void WaitCompletion(int end)
            {
                // Might as well steal some work instead of just waiting,
                // also helps prevent potential deadlocks from badly threaded code
                while(WorkDone < end && Finished.WaitOne(0) == false)
                    ThreadPool.Instance.TryCooperate();
            }
        }

        private struct SortRange
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
            private static readonly ConcurrentPool<SortState> Pool = new ConcurrentPool<SortState>(() => new SortState());

            private int referenceCount;

            public readonly ManualResetEvent Finished = new ManualResetEvent(false);

            public readonly ConcurrentQueue<SortRange> Partitions = new ConcurrentQueue<SortRange>();

            public int MaxWorkerCount;

            public int OpLeft;

            [NotNull]
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
                while(Volatile.Read(ref OpLeft) != 0 && Finished.WaitOne(0) == false)
                    ThreadPool.Instance.TryCooperate();
            }
        }

        private class DispatcherNode
        {
            public MethodBase Caller;
            public int Count;
            public TimeSpan TotalTime;
        }
#if PROFILING_SCOPES
        private static ConcurrentDictionary<MethodInfo, DispatcherNode> nodes = new ConcurrentDictionary<MethodInfo, DispatcherNode>();
#endif
        private struct ProfilingScope : IDisposable
        {
#if PROFILING_SCOPES
            public Stopwatch Stopwatch;
            public Delegate Action;
#endif
            public void Dispose()
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

        private static ProfilingScope Profile(Delegate action)
        {
            var result = new ProfilingScope();
#if PROFILING_SCOPES
            result.Action = action;
            result.Stopwatch = new Stopwatch();
            result.Stopwatch.Start();
#endif
            return result;
        }
    }
}
