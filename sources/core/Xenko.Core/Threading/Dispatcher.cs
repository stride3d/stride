// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Xenko.Core.Annotations;
using Xenko.Core.Collections;

namespace Xenko.Core.Threading
{
    public class Dispatcher
    {
#if XENKO_PLATFORM_IOS || XENKO_PLATFORM_ANDROID
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
                    state.StartInclusive = fromInclusive;

                    try
                    {
                        var batchCount = Math.Min(MaxDegreeOfParallelism, count);
                        var batchSize = (count + (batchCount - 1)) / batchCount;

                        // Kick off a worker, then perform work synchronously
                        state.AddReference();
                        Fork(toExclusive, batchSize, MaxDegreeOfParallelism, action, state);

                        // Wait for all workers to finish
                        if (state.ActiveWorkerCount != 0)
                            state.Finished.WaitOne();
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
                    state.StartInclusive = fromInclusive;

                    try
                    {
                        var batchCount = Math.Min(MaxDegreeOfParallelism, count);
                        var batchSize = (count + (batchCount - 1)) / batchCount;

                        // Kick off a worker, then perform work synchronously
                        state.AddReference();
                        Fork(toExclusive, batchSize, MaxDegreeOfParallelism, initializeLocal, action, finalizeLocal, state);

                        // Wait for all workers to finish
                        if (state.ActiveWorkerCount != 0)
                            state.Finished.WaitOne();
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
                    if (state.ActiveWorkerCount != 0)
                        state.Finished.WaitOne();
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
                    if (state.ActiveWorkerCount != 0)
                        state.Finished.WaitOne();
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
            // Other threads already processed all work before this one started. ActiveWorkerCount is already 0
            if (state.StartInclusive >= collection.Count)
            {
                state.Release();
                return;
            }

            // This thread is now actively processing work items, meaning there might be work in progress
            Interlocked.Increment(ref state.ActiveWorkerCount);

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < collection.Count)
            {
                state.AddReference();
                ThreadPool.Instance.QueueWorkItem(() => Fork(collection, batchSize, maxDegreeOfParallelism - 1, action, state));
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < collection.Count)
                {
                    // TODO: Reuse enumerator when processing multiple batches synchronously
                    var start = newStart - batchSize;
                    ExecuteBatch(collection, newStart - batchSize, Math.Min(collection.Count, newStart) - start, action);
                }
            }
            finally
            {
                state.Release();

                // If this was the last batch, signal
                if (Interlocked.Decrement(ref state.ActiveWorkerCount) == 0)
                {
                    state.Finished.Set();
                }
            }
        }

        private static void Fork<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, int batchSize, int maxDegreeOfParallelism, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<KeyValuePair<TKey, TValue>, TLocal> action, [Pooled] Action<TLocal> finalizeLocal, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started. ActiveWorkerCount is already 0
            if (state.StartInclusive >= collection.Count)
            {
                state.Release();
                return;
            }

            // This thread is now actively processing work items, meaning there might be work in progress
            Interlocked.Increment(ref state.ActiveWorkerCount);

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < collection.Count)
            {
                state.AddReference();
                ThreadPool.Instance.QueueWorkItem(() => Fork(collection, batchSize, maxDegreeOfParallelism - 1, initializeLocal, action, finalizeLocal, state));
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < collection.Count)
                {
                    // TODO: Reuse enumerator when processing multiple batches synchronously
                    var start = newStart - batchSize;
                    ExecuteBatch(collection, newStart - batchSize, Math.Min(collection.Count, newStart) - start, initializeLocal, action, finalizeLocal);
                }
            }
            finally
            {
                state.Release();

                // If this was the last batch, signal
                if (Interlocked.Decrement(ref state.ActiveWorkerCount) == 0)
                {
                    state.Finished.Set();
                }
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
            // Other threads already processed all work before this one started. ActiveWorkerCount is already 0
            if (state.StartInclusive >= endExclusive)
            {
                state.Release();
                return;
            }

            // This thread is now actively processing work items, meaning there might be work in progress
            Interlocked.Increment(ref state.ActiveWorkerCount);

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < endExclusive)
            {
                state.AddReference();
                ThreadPool.Instance.QueueWorkItem(() => Fork(endExclusive, batchSize, maxDegreeOfParallelism - 1, action, state));
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < endExclusive)
                {
                    ExecuteBatch(newStart - batchSize, Math.Min(endExclusive, newStart), action);
                }
            }
            finally
            {
                state.Release();

                // If this was the last batch, signal
                if (Interlocked.Decrement(ref state.ActiveWorkerCount) == 0)
                {
                    state.Finished.Set();
                }
            }
        }

        private static void Fork<TLocal>(int endExclusive, int batchSize, int maxDegreeOfParallelism, [Pooled] Func<TLocal> initializeLocal, [Pooled] Action<int, TLocal> action, [Pooled] Action<TLocal> finalizeLocal, [NotNull] BatchState state)
        {
            // Other threads already processed all work before this one started. ActiveWorkerCount is already 0
            if (state.StartInclusive >= endExclusive)
            {
                state.Release();
                return;
            }

            // This thread is now actively processing work items, meaning there might be work in progress
            Interlocked.Increment(ref state.ActiveWorkerCount);

            // Kick off another worker if there's any work left
            if (maxDegreeOfParallelism > 1 && state.StartInclusive + batchSize < endExclusive)
            {
                state.AddReference();
                ThreadPool.Instance.QueueWorkItem(() => Fork(endExclusive, batchSize, maxDegreeOfParallelism - 1, initializeLocal, action, finalizeLocal, state));
            }

            try
            {
                // Process batches synchronously as long as there are any
                int newStart;
                while ((newStart = Interlocked.Add(ref state.StartInclusive, batchSize)) - batchSize < endExclusive)
                {
                    ExecuteBatch(newStart - batchSize, Math.Min(endExclusive, newStart), initializeLocal, action, finalizeLocal);
                }
            }
            finally
            {
                state.Release();

                // If this was the last batch, signal
                if (Interlocked.Decrement(ref state.ActiveWorkerCount) == 0)
                {
                    state.Finished.Set();
                }
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

            var state = SortState.Acquire();

            try
            {
                // Initial partition
                state.Partitions.Enqueue(new SortRange(index, length - 1));

                // Sort recursively
                state.AddReference();
                Sort(collection, MaxDegreeOfParallelism, comparer, state);

                // Wait for all work to finish
                if (state.ActiveWorkerCount != 0)
                    state.Finished.WaitOne();
            }
            finally
            {
                state.Release();
            }
        }

        private static void Sort<T>(T[] collection, int maxDegreeOfParallelism, IComparer<T> comparer, [NotNull] SortState state)
        {
            const int sequentialThreshold = 2048;

            // Other threads already processed all work before this one started. ActiveWorkerCount is already 0
            if (state.Partitions.IsEmpty)
            {
                state.Release();
                return;
            }

            // This thread is now actively processing work items, meaning there might be work in progress
            Interlocked.Increment(ref state.ActiveWorkerCount);

            var hasChild = false;

            try
            {
                SortRange range;
                while (state.Partitions.TryDequeue(out range))
                {
                    if (range.Right - range.Left < sequentialThreshold)
                    {
                        // Sort small collections sequentially
                        Array.Sort(collection, range.Left, range.Right - range.Left + 1, comparer);
                    }
                    else
                    {
                        var pivot = Partition(collection, range.Left, range.Right, comparer);

                        // Add work items
                        if (pivot - 1 > range.Left)
                            state.Partitions.Enqueue(new SortRange(range.Left, pivot - 1));

                        if (range.Right > pivot + 1)
                            state.Partitions.Enqueue(new SortRange(pivot + 1, range.Right));

                        // Add a new worker if necessary
                        if (maxDegreeOfParallelism > 1 && !hasChild)
                        {
                            state.AddReference();
                            Fork(collection, maxDegreeOfParallelism, comparer, state);
                            hasChild = true;
                        }
                    }
                }
            }
            finally
            {
                state.Release();

                if (Interlocked.Decrement(ref state.ActiveWorkerCount) == 0)
                {
                    state.Finished.Set();
                }
            }
        }

        private static void Fork<T>(T[] collection, int maxDegreeOfParallelism, IComparer<T> comparer, SortState state)
        {
            ThreadPool.Instance.QueueWorkItem(() => Sort(collection, maxDegreeOfParallelism - 1, comparer, state));
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

            public int ActiveWorkerCount;

            [NotNull]
            public static BatchState Acquire()
            {
                var state = Pool.Acquire();
                state.referenceCount = 1;
                state.ActiveWorkerCount = 0;
                state.StartInclusive = 0;
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

            public int ActiveWorkerCount;

            [NotNull]
            public static SortState Acquire()
            {
                var state = Pool.Acquire();
                state.referenceCount = 1;
                state.ActiveWorkerCount = 0;
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
        }

        private class DispatcherNode
        {
            public MethodBase Caller;
            public int Count;
            public TimeSpan TotalTime;
        }

        private static ConcurrentDictionary<MethodInfo, DispatcherNode> nodes = new ConcurrentDictionary<MethodInfo, DispatcherNode>();

        private struct ProfilingScope : IDisposable
        {
#if false
            public Stopwatch Stopwatch;
            public Delegate Action;
#endif
            public void Dispose()
            {
#if false
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
#if false
            result.Action = action;
            result.Stopwatch = new Stopwatch();
            result.Stopwatch.Start();
#endif
            return result;
        }
    }
}
