// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        // The amount of threads in the pool + the one who called the dispatch function
        public static int MaxDegreeOfParallelism => ThreadPool.Instance.PoolSize + 1;
#endif
        public delegate TLocal InitializeLocal<TParam, TLocal>(ref TParam param);
        public delegate void ProcessItem<TParam, TItem>(ref TParam param, TItem item);
        public delegate void ProcessItem<TParam, TItem, TLocal>(ref TParam param, TItem item, TLocal local);
        public delegate void ProcessKeyValue<TParam, TItem>(ref TParam param, ref TItem item);
        public delegate void ProcessKeyValue<TParam, TItem, TLocal>(ref TParam param, ref TItem item, TLocal local);
        public delegate void ActionRef<TParam>(ref TParam param);
        delegate void ActionRef<TParam, TLocal>(ref TParam param, ref TLocal local);
        
        // Uncomment to log to console the ratio of threads working to threads scheduled
        // should be investigated later to find out if there are jobs that fail to profit
        // from multi-threading.
        // Note that it can also be caused by other threads being busy on other dispatched
        // jobs.
        //#define LOG_EFFICIENCY
        #if LOG_EFFICIENCY
        private static int threadTookJob;
        private static int totalJobsScheduled;
        private static double efficiency;
        private static Stopwatch sw = Stopwatch.StartNew();
        #endif

        /// <summary>
        /// Avoid using this version if you are capturing variables within the action,
        /// use those which allows you to pass the parameter as an argument instead.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For(int fromInclusive, int toExclusive, Action<int> action)
        {
            using (Profile(action))
            {
                object param = null;
                ForLenient<object, object>(fromInclusive, toExclusive, ref param, action, null, null);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TParam>(int fromInclusive, int toExclusive, TParam parameter, ProcessItem<TParam, int> action)
        {
            using (Profile(action))
            {
                ForLenient<TParam, object>(fromInclusive, toExclusive, ref parameter, action, null, null);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TLocal>(int fromInclusive, int toExclusive, Action<int, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                object param = null;
                ForLenient<object, TLocal>(fromInclusive, toExclusive, ref param, action, initializeLocal, finalizeLocal);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TLocal, TParam>(int fromInclusive, int toExclusive, TParam param, ProcessItem<TParam, int, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                ForLenient<TParam, TLocal>(fromInclusive, toExclusive, ref param, action, initializeLocal, finalizeLocal);
            }
        }

        public static void ForEach<TItem>([NotNull] IReadOnlyList<TItem> enumerable, Action<TItem> action)
        {
            using (Profile(action))
            {
                ForEachLenient<object, TItem, object>(enumerable, null, action, null, null);
            }
        }

        public static void ForEach<TParam, TItem>([NotNull] IReadOnlyList<TItem> enumerable, TParam param, ProcessItem<TParam, TItem> action)
        {
            using (Profile(action))
            {
                ForEachLenient<TParam, TItem, object>(enumerable, param, action, null, null);
            }
        }

        public static void ForEach<TParam, TItem, TLocal>([NotNull] IReadOnlyList<TItem> enumerable, TParam param, ProcessItem<TParam, TItem, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                ForEachLenient<TParam, TItem, TLocal>(enumerable, param, action, initializeLocal, finalizeLocal);
            }
        }

        public static void ForEach<TItem, TLocal>([NotNull] IReadOnlyList<TItem> enumerable, Action<TItem, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                ForEachLenient<object, TItem, TLocal>(enumerable, null, action, initializeLocal, finalizeLocal);
            }
        }

        public static void ForEachKVP<TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, ActionRef<KeyValuePair<TKey, TValue>> action)
        {
            using (Profile(action))
            {
                object param = null;
                ForEachKVPLenient<object, TKey, TValue, object>(collection, ref param, action, null, null);
            }
        }

        public static void ForEachKVP<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, ProcessItem<KeyValuePair<TKey, TValue>, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                object param = null;
                ForEachKVPLenient<object, TKey, TValue, TLocal>(collection, ref param, action, initializeLocal, finalizeLocal);
            }
        }

        public static void ForEachKVP<TParam, TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>> action)
        {
            using (Profile(action))
            {
                ForEachKVPLenient<TParam, TKey, TValue, object>(collection, ref param, action, null, null);
            }
        }

        public static void ForEachKVP<TParam, TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                ForEachKVPLenient<TParam, TKey, TValue, TLocal>(collection, ref param, action, initializeLocal, finalizeLocal);
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        private static void ForLenient<TParam, TLocal>(int fromInclusive, int toExclusive, ref TParam param, object action, object initializeLocal, object finalizeLocal)
        {
            var context = IntContext<TParam, TLocal>.Acquire(ref param, action, initializeLocal, finalizeLocal);
            PrepareFork(fromInclusive, toExclusive, context);
        }

        private static void ForEachLenient<TParam, TItem, TLocal>([NotNull] IReadOnlyList<TItem> list, TParam parameter, object action, object initializeLocal, object finalizeLocal)
        {
            var context = ListContext<TParam, TItem, TLocal>.Acquire(list, ref parameter, action, initializeLocal, finalizeLocal);
            PrepareFork(0, list.Count, context);
        }

        private static void ForEachKVPLenient<TParam, TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, ref TParam param, object action, object initializeLocal, object finalizeLocal)
        {
            int count = collection.Count;
            var context = KVPContext<TParam, TKey, TValue, TLocal>.Acquire(collection, ref param, action, initializeLocal, finalizeLocal);
            PrepareFork(0, count, context);
        }
        
        private static void PrepareFork(int fromInclusive, int toExclusive, IContext context)
        {
            int count = toExclusive - fromInclusive;
            if (count < 0)
            {
                context.Recycle();
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            }
            if (count == 0)
            {
                context.Recycle();
                return;
            }

            if (MaxDegreeOfParallelism <= 1 || count <= 1)
            {
                context.Work(fromInclusive, toExclusive);
                context.Recycle();
                return;
            }
            
            var tCount = Math.Min(MaxDegreeOfParallelism, count);
            var batchSize = count / tCount;
            batchSize = batchSize >= 2 ? batchSize / 2 : batchSize;
            
            // 'tCount' threads Work() -> ReleaseRef(). Add one more to prevent Recycling before we have waited for it to be finished
            var refCount = tCount + 1;
            var state = BatchState.Acquire(refCount, fromInclusive, toExclusive, batchSize, context);
            try
            {
                ThreadPool.Instance.DispatchJob(state, tCount - 1);

                state.Work();
                
                // By now, all the jobs scheduled are either being
                // worked on by other threads or already done.
                
                #if LOG_EFFICIENCY
                Interlocked.Add(ref threadTookJob, Volatile.Read(ref state.successfulWork));
                Interlocked.Add(ref totalJobsScheduled, tCount);
                if (sw.ElapsedMilliseconds > 1000)
                {
                    sw.Restart();
                    var w = Interlocked.Exchange(ref threadTookJob, 0);
                    var g = Interlocked.Exchange(ref totalJobsScheduled, 0);
                    var newEfficiency = (double)w / g;
                    Interlocked.Exchange(ref efficiency, newEfficiency);
                    System.Console.WriteLine($"{newEfficiency} : {w} / {g}");
                }
                #endif

                // Wait for other threads to finish their job
                while (Volatile.Read(ref state.Finished) == 0)
                    continue;
            }
            finally
            {
                state.ForceReleaseRef();
            }

            // Context already recycled through BatchState, no need to do so here
            return;
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
                            var context = SortContext<T>.Acquire(collection, maxDegreeOfParallelism - 1, comparer, state);
                            ThreadPool.Instance.DispatchJob(context);
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


        private class SortContext<T> : ThreadPool.IConcurrentJob
        {
            private static readonly ConcurrentFixedPool<SortContext<T>> pool = new ConcurrentFixedPool<SortContext<T>>(64, () => new SortContext<T>());

            private T[] collection;
            private int maxDegreeOfParallelism;
            private IComparer<T> comparer;
            private SortState state;

            public static SortContext<T> Acquire(T[] collection, int maxDegreeOfParallelism, IComparer<T> comparer, SortState state)
            {
                var v = pool.Pop();
                v.collection = collection;
                v.maxDegreeOfParallelism = maxDegreeOfParallelism;
                v.comparer = comparer;
                v.state = state;
                return v;
            }

            public void Work()
            {
                Sort(collection, maxDegreeOfParallelism, comparer, state);
                collection = null;
                comparer = null;
                state = null;
                pool.TryPush(this);
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
        
        
        private interface IContext
        {
            void Work(int fromInclusive, int toExclusive);
            void Recycle();
        }

        private abstract class Context<T, TParam, TItem, TLocal> : IContext where T : Context<T, TParam, TItem, TLocal>, new()
        {
            private static readonly ConcurrentFixedPool<T> pool = new ConcurrentFixedPool<T>(64, () => new T());

            protected TParam Param;
            
            private int recycled;
            private object initializeLocal;
            private object finalizeLocal;
            
            private int selectedAction;
            private Action<TItem> ai;
            private Action<TItem, TLocal> ail;
            private ProcessItem<TParam, TItem> api;
            private ProcessItem<TParam, TItem, TLocal> apil;
            private ProcessKeyValue<TParam, TItem> apkv;
            private ProcessKeyValue<TParam, TItem, TLocal> apkvl;
            private ActionRef<TItem> ari;
            private ActionRef<TItem, TLocal> aril;
            
            /// <summary> Retrieves a pooled instance of this type </summary>
            protected static T Pop(ref TParam param, object action, object initializeLocal, object finalizeLocal)
            {
                var v = pool.Pop();
                Interlocked.Exchange(ref v.recycled, 0);
                v.Param = param;
                v.initializeLocal = initializeLocal;
                v.finalizeLocal = finalizeLocal;
                switch (action)
                {
                    case Action<TItem> a:
                        v.ai = a;
                        v.selectedAction = 1;
                        break;
                    case Action<TItem, TLocal> a:
                        v.ail = a;
                        v.selectedAction = 2;
                        break;
                    case ProcessItem<TParam, TItem> a:
                        v.api = a;
                        v.selectedAction = 3;
                        break;
                    case ProcessItem<TParam, TItem, TLocal> a:
                        v.apil = a;
                        v.selectedAction = 4;
                        break;
                    case ProcessKeyValue<TParam, TItem> a:
                        v.apkv = a;
                        v.selectedAction = 5;
                        break;
                    case ProcessKeyValue<TParam, TItem, TLocal> a:
                        v.apkvl = a;
                        v.selectedAction = 6;
                        break;
                    case ActionRef<TItem> a:
                        v.ari = a;
                        v.selectedAction = 7;
                        break;
                    case ActionRef<TItem, TLocal> a:
                        v.aril = a;
                        v.selectedAction = 8;
                        break;
                    default: throw new ArgumentException(action.GetType().ToString());
                }

                return v;
            }
            
            /// <summary> Initializes locals and call the provided action on all items within the given range </summary>
            public void Work(int fromInclusive, int toExclusive)
            {
                var local = default(TLocal);
                try
                {
                    switch (initializeLocal)
                    {
                        case InitializeLocal<TParam, TLocal> fpl: local = fpl(ref Param); break;
                        case Func<TLocal> fl: local = fl(); break;
                        case object o: throw new ArgumentException(initializeLocal.GetType().ToString());
                    }

                    ProcessRange(fromInclusive, toExclusive, local);
                }
                finally
                {
                    switch (finalizeLocal)
                    {
                        case ProcessItem<TParam, TLocal> apl: apl(ref Param, local); break;
                        case Action<TLocal> al: al(local); break;
                        case object o: throw new ArgumentException(finalizeLocal.GetType().ToString());
                    }
                }
            }
            
            /// <summary>
            /// Loop over items of a collection and call <see cref="InvokeAction"/> on each of them
            /// </summary>
            protected abstract void ProcessRange(int fromInclusive, int toExclusive, TLocal local);
            
            /// <summary> Call the action provided to the dispatcher </summary>
            protected void InvokeAction(ref TParam parameter, ref TItem item, TLocal local)
            {
                switch (selectedAction)
                {
                    case 1: ai(item); break;
                    case 2: ail(item, local); break;
                    case 3: api(ref parameter, item); break;
                    case 4: apil(ref parameter, item, local); break;
                    case 5: apkv(ref parameter, ref item); break;
                    case 6: apkvl(ref parameter, ref item, local); break;
                    case 7: ari(ref item); break;
                    case 8: aril(ref item, ref local); break;
                    default: throw new ArgumentException(selectedAction.ToString());
                }
            }
            
            /// <summary> Push this class back onto the pool to be used later </summary>
            public virtual void Recycle()
            {
                Param = default;
                initializeLocal = null;
                finalizeLocal = null;
                selectedAction = 0;
                Interlocked.Exchange(ref recycled, 1);
                if (this is T v)
                {
                    pool.TryPush(v);
                    return;
                }
                throw new InvalidOperationException();
            }
            
            ~Context()
            {
                if (Volatile.Read(ref recycled) == 0)
                    throw new InvalidOperationException($"Recycle() not called for {typeof(T)} !");
            }
        }

        private class IntContext<TParam, TLocal> : Context<IntContext<TParam, TLocal>, TParam, int, TLocal>
        {
            public static IntContext<TParam, TLocal> Acquire(ref TParam param, object action, object initializeLocal, object finalizeLocal)
            {
                return Pop(ref param, action, initializeLocal, finalizeLocal);
            }

            protected override void ProcessRange(int fromInclusive, int toExclusive, TLocal local)
            {
                for (int i = fromInclusive; i < toExclusive; i++)
                {
                    InvokeAction(ref Param, ref i, local);
                }
            }
        }

        private class ListContext<TParam, TItem, TLocal> : Context<ListContext<TParam, TItem, TLocal>, TParam, TItem, TLocal>
        {
            private IReadOnlyList<TItem> collection;
            public static ListContext<TParam, TItem, TLocal> Acquire(IReadOnlyList<TItem> collection, ref TParam param, object action, object initializeLocal, object finalizeLocal)
            {
                var output = Pop(ref param, action, initializeLocal, finalizeLocal);
                
                if (collection is FastList<TItem> fastList)
                    collection = fastList.Items;
                else if (collection is ConcurrentCollector<TItem> collector)
                    collection = collector.Items;
                
                output.collection = collection;
                return output;
            }

            protected override void ProcessRange(int fromInclusive, int toExclusive, TLocal local)
            {
                var coll = collection;
                switch (coll)
                {
                    case TItem[] array:
                    {
                        for (int i = fromInclusive; i < toExclusive; i++)
                        {
                            InvokeAction(ref Param, ref array[i], local);
                        }

                        break;
                    }
                    case IReadOnlyList<TItem> iList:
                    {
                        for (int i = fromInclusive; i < toExclusive; i++)
                        {
                            var item = iList[i];
                            InvokeAction(ref Param, ref item, local);
                        }

                        break;
                    }
                    default: throw new ArgumentException(coll?.GetType().ToString());
                }
            }

            public override void Recycle()
            {
                collection = null;
                base.Recycle();
            }
        }

        private class KVPContext<TParam, TKey, TValue, TLocal> : Context<KVPContext<TParam, TKey, TValue, TLocal>, TParam, KeyValuePair<TKey, TValue>, TLocal>
        {
            private Dictionary<TKey, TValue> collection;
            public static KVPContext<TParam, TKey, TValue, TLocal> Acquire(Dictionary<TKey, TValue> collection, ref TParam param, object action, object initializeLocal, object finalizeLocal)
            {
                var output = Pop(ref param, action, initializeLocal, finalizeLocal);
                output.collection = collection;
                return output;
            }

            protected override void ProcessRange(int fromInclusive, int toExclusive, TLocal local)
            {
                var enumerator = collection.GetEnumerator();
                var index = 0;
                enumerator.MoveNext();

                // Skip to offset
                while (index < fromInclusive)
                {
                    index++;
                    enumerator.MoveNext();
                }

                // Process batch
                while (index < toExclusive)
                {
                    var kvp = enumerator.Current;
                    InvokeAction(ref Param, ref kvp, local);
                    index++;
                    enumerator.MoveNext();
                }
            }

            public override void Recycle()
            {
                collection = null;
                base.Recycle();
            }
        }


        private class BatchState : ThreadPool.IConcurrentJob
        {
            private static readonly ConcurrentFixedPool<BatchState> pool = new ConcurrentFixedPool<BatchState>(64, () => new BatchState());
            
            public int Finished;
            
            private int startInclusive;
            private int amountFinishedInclusive;
            private int referenceCount;
            private int toExclusive;
            private int batchSize;
            public int successfulWork;
            private IContext context;

            [NotNull]
            public static BatchState Acquire(int referenceCount, int fromInclusive, int toExclusive, int batchSize, IContext context)
            {
                var state = pool.Pop();
                state.successfulWork = 0;
                state.referenceCount = referenceCount;
                state.startInclusive = fromInclusive;
                state.amountFinishedInclusive = fromInclusive;
                state.toExclusive = toExclusive;
                state.batchSize = batchSize;
                state.context = context;
                state.Finished = 0;
                return state;
            }

            public void Work()
            {
                bool worked = false;
                try
                {
                    int newStart;
                    while ((newStart = Interlocked.Add(ref startInclusive, batchSize)) - batchSize < toExclusive)
                    {
                        worked = true;
                        context.Work(newStart - batchSize, Math.Min(toExclusive, newStart));
                        if (Interlocked.Add(ref amountFinishedInclusive, batchSize) >= toExclusive)
                        {
                            Interlocked.Exchange(ref Finished, 1);
                            // Context is guaranteed to not be used anymore, recycle it here to put it back
                            // asap in the pool for other operations to use it
                            Interlocked.Exchange(ref context, null).Recycle();
                            break;
                        }
                    }
                }
                finally
                {
                    if (worked)
                        Interlocked.Increment(ref successfulWork);
                    ForceReleaseRef();
                }
            }

            public void ForceReleaseRef()
            {
                if (Interlocked.Decrement(ref referenceCount) == 0)
                {
                    pool.TryPush(this);
                }
            }

            ~BatchState()
            {
                if (Volatile.Read(ref referenceCount) != 0)
                    throw new BatchNotRecycledException();
            }

            private class BatchNotRecycledException : Exception
            {
                
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
    }
}