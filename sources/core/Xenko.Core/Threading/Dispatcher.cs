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
        public static int MaxDegreeOfParallelism = Environment.ProcessorCount;
#endif
        public delegate TLocal InitializeLocal<TParam, TLocal>(ref TParam param);
        public delegate void ProcessItem<TParam, TItem>(ref TParam param, TItem item);
        public delegate void ProcessItem<TParam, TItem, TLocal>(ref TParam param, TItem item, TLocal local);
        public delegate void ProcessKeyValue<TParam, TItem>(ref TParam param, ref TItem item);
        public delegate void ProcessKeyValue<TParam, TItem, TLocal>(ref TParam param, ref TItem item, TLocal local);
        public delegate void ActionRef<TParam>(ref TParam param);
        delegate void ActionRef<TParam, TLocal>(ref TParam param, ref TLocal local);
        
        /// <summary>
        /// Avoid using this version if you are capturing variables within the action,
        /// use those which allows you to pass the parameter as an argument instead.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For(int fromInclusive, int toExclusive, Action<int> action)
        {
            using (Profile(action))
            {
                int count = toExclusive - fromInclusive;
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(toExclusive));
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    object param = null;
                    ExecuteBatchInt<object, object>(fromInclusive, toExclusive, ref param, action, null, null);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref Action<int> a, ref (int start, int end) r)
                    {
                        object param = null;
                        ExecuteBatchInt<object, object>(r.start, r.end, ref param, a, null, null);
                    }, action);
                    PrepareFork(fromInclusive, toExclusive, context);
                }
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TParam>(int fromInclusive, int toExclusive, TParam parameter, ProcessItem<TParam, int> action)
        {
            using (Profile(action))
            {
                int count = toExclusive - fromInclusive;
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(toExclusive));
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    ExecuteBatchInt<TParam, object>(fromInclusive, toExclusive, ref parameter, action, null, null);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (ProcessItem<TParam, int> action, TParam param) a, ref (int start, int end) r)
                    {
                        ExecuteBatchInt<TParam, object>(r.start, r.end, ref a.param, a.action, null, null);
                    }, (action, parameter));
                    PrepareFork(fromInclusive, toExclusive, context);
                }
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TLocal>(int fromInclusive, int toExclusive, Action<int, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                int count = toExclusive - fromInclusive;
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(toExclusive));
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    object param = null;
                    ExecuteBatchInt<object, TLocal>(fromInclusive, toExclusive, ref param, action, initializeLocal, finalizeLocal);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (Func<TLocal> initializeLocal, Action<int, TLocal> action, Action<TLocal> finalizeLocal) a, ref (int start, int end) r)
                    {
                        object param = null;
                        ExecuteBatchInt<object, TLocal>(r.start, r.end, ref param, a.action, a.initializeLocal, a.finalizeLocal);
                    }, (initializeLocal, action, finalizeLocal));
                    PrepareFork(fromInclusive, toExclusive, context);
                }
            }
        }

        /// <exception cref="ArgumentOutOfRangeException"><paramref name="toExclusive"/> is smaller than <paramref name="fromInclusive"/></exception>
        public static void For<TLocal, TParam>(int fromInclusive, int toExclusive, TParam param, ProcessItem<TParam, int, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                int count = toExclusive - fromInclusive;
                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(toExclusive));
                if (count == 0)
                    return;

                if (MaxDegreeOfParallelism <= 1 || count == 1)
                {
                    ExecuteBatchInt<TParam, TLocal>(fromInclusive, toExclusive, ref param, action, initializeLocal, finalizeLocal);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (TParam param, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, int, TLocal> action, ProcessItem<TParam, TLocal> finalizeLocal) a, ref (int start, int end) r)
                    {
                        ExecuteBatchInt<TParam, TLocal>(r.start, r.end, ref a.param, a.action, a.initializeLocal, a.finalizeLocal);
                    }, (param, initializeLocal, action, finalizeLocal));
                    PrepareFork(fromInclusive, toExclusive, context);
                }
            }
        }

        public static void ForEach<TItem>([NotNull] IReadOnlyList<TItem> enumerable, Action<TItem> action)
        {
            using (Profile(action))
            {
                LenientForEach<object, TItem, object>(enumerable, null, action, null, null);
            }
        }

        public static void ForEach<TParam, TItem>([NotNull] IReadOnlyList<TItem> enumerable, TParam param, ProcessItem<TParam, TItem> action)
        {
            using (Profile(action))
            {
                LenientForEach<TParam, TItem, object>(enumerable, param, action, null, null);
            }
        }

        public static void ForEach<TParam, TItem, TLocal>([NotNull] IReadOnlyList<TItem> enumerable, TParam param, ProcessItem<TParam, TItem, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                LenientForEach<TParam, TItem, TLocal>(enumerable, param, action, initializeLocal, finalizeLocal);
            }
        }

        public static void ForEach<TItem, TLocal>([NotNull] IReadOnlyList<TItem> enumerable, Action<TItem, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                LenientForEach<object, TItem, TLocal>(enumerable, null, action, initializeLocal, finalizeLocal);
            }
        }

        private static void LenientForEach<TParam, TItem, TLocal>([NotNull] IReadOnlyList<TItem> list, TParam parameter, object action, object initializeLocal, object finalizeLocal)
        {
            int count = list.Count;
            if (MaxDegreeOfParallelism <= 1 || count <= 1)
            {
                ExecuteBatch<TParam, TItem, TLocal>(0, count, list, ref parameter, action, initializeLocal, finalizeLocal);
            }
            else
            {
                var context = AcquireCapture(delegate(ref (IEnumerable<TItem> enumerable, TParam parameter, object action, object initializeLocal, object finalizeLocal) a, ref (int start, int end) r)
                {
                    ExecuteBatch<TParam, TItem, TLocal>(r.start, r.end, a.enumerable, ref a.parameter, a.action, a.initializeLocal, a.finalizeLocal);
                }, (enumerable: list, parameter, action, initializeLocal, finalizeLocal));
                PrepareFork(0, count, context);
            }
        }

        public static void ForEachKVP<TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, ActionRef<KeyValuePair<TKey, TValue>> action)
        {
            using (Profile(action))
            {
                if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
                {
                    object param = null;
                    ExecuteBatchKvP<object, TKey, TValue, object>(0, collection.Count, collection, ref param, action, null, null);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (Dictionary<TKey, TValue> collection, ActionRef<KeyValuePair<TKey, TValue>> action) a, ref (int start, int end) r)
                    {
                        object param = null;
                        ExecuteBatchKvP<object, TKey, TValue, object>(r.start, r.end, a.collection, ref param, a.action, null, null);
                    }, (collection, action));
                    PrepareFork(0, collection.Count, context);
                }
            }
        }

        public static void ForEachKVP<TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, ProcessItem<KeyValuePair<TKey, TValue>, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
                {
                    object param = null;
                    ExecuteBatchKvP<object, TKey, TValue, TLocal>(0, collection.Count, collection, ref param, action, initializeLocal, finalizeLocal);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (Dictionary<TKey, TValue> collection, ProcessItem<KeyValuePair<TKey, TValue>, TLocal> action, Func<TLocal> initializeLocal, Action<TLocal> finalizeLocal) a, ref (int start, int end) r)
                    {
                        object param = null;
                        ExecuteBatchKvP<object, TKey, TValue, TLocal>(r.start, r.end, a.collection, ref param, a.action, a.initializeLocal, a.finalizeLocal);
                    }, (collection, action, initializeLocal, finalizeLocal));
                    PrepareFork(0, collection.Count, context);
                }
            }
        }

        public static void ForEachKVP<TParam, TKey, TValue>([NotNull] Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>> action)
        {
            using (Profile(action))
            {
                if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
                {
                    ExecuteBatchKvP<TParam, TKey, TValue, object>(0, collection.Count, collection, ref param, action, null, null);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>> action) a, ref (int start, int end) r)
                    {
                        ExecuteBatchKvP<TParam, TKey, TValue, object>(r.start, r.end, a.collection, ref a.param, a.action, null, null);
                    }, (collection, param, action));
                    PrepareFork(0, collection.Count, context);
                }
            }
        }

        public static void ForEachKVP<TParam, TKey, TValue, TLocal>([NotNull] Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal = null)
        {
            using (Profile(action))
            {
                if (MaxDegreeOfParallelism <= 1 || collection.Count <= 1)
                {
                    ExecuteBatchKvP<TParam, TKey, TValue, TLocal>(0, collection.Count, collection, ref param, action, initializeLocal, finalizeLocal);
                }
                else
                {
                    var context = AcquireCapture(delegate(ref (Dictionary<TKey, TValue> collection, TParam param, ProcessKeyValue<TParam, KeyValuePair<TKey, TValue>, TLocal> action, InitializeLocal<TParam, TLocal> initializeLocal, ProcessItem<TParam, TLocal> finalizeLocal) a, ref (int start, int end) r)
                    {
                        ExecuteBatchKvP<TParam, TKey, TValue, TLocal>(r.start, r.end, a.collection, ref a.param, a.action, a.initializeLocal, a.finalizeLocal);
                    }, (collection, param, action, initializeLocal, finalizeLocal));
                    PrepareFork(0, collection.Count, context);
                }
            }
        }

        private static void PrepareFork(int fromInclusive, int toExclusive, ICachedDelegateCapture<(int, int)> executeBatch)
        {
            int count = toExclusive - fromInclusive;
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(toExclusive));
            if (count == 0)
                return;

            var tCount = Math.Min(MaxDegreeOfParallelism, count);
            var batchSize = count / tCount;
            batchSize = batchSize > 1 ? batchSize / 2 : batchSize;
            var state = BatchState.Acquire(tCount, fromInclusive, toExclusive, batchSize, executeBatch);
            ThreadPool.Instance.DispatchJob(state, tCount - 1);

            state.Work();

            // Wait for job to finish
            state.Finished.Wait();
        }

        private static void ExecuteBatchInt<TParam, TLocal>(int fromInclusive, int toExclusive, ref TParam parameter, object action, object initializeLocal, object finalizeLocal)
        {
            var local = default(TLocal);
            try
            {
                switch (initializeLocal)
                {
                    case InitializeLocal<TParam, TLocal> fpl: local = fpl(ref parameter); break;
                    case Func<TLocal> fl: local = fl(); break;
                    case object o: throw new ArgumentException(initializeLocal.GetType().ToString());
                }

                var proxy = new ActionProxy<TParam, int, TLocal>(action);
                for (var i = fromInclusive; i < toExclusive; i++)
                {
                    proxy.Invoke(ref parameter, ref i, local);
                }
            }
            finally
            {
                switch (finalizeLocal)
                {
                    case ProcessItem<TParam, TLocal> apl: apl(ref parameter, local); break;
                    case Action<TLocal> al: al(local); break;
                    case object o: throw new ArgumentException(finalizeLocal.GetType().ToString());
                }
            }
        }

        private static void ExecuteBatch<TParam, TItem, TLocal>(int fromInclusive, int toExclusive, IEnumerable<TItem> enumerable, ref TParam parameter, object action, object initializeLocal, object finalizeLocal)
        {
            var local = default(TLocal);
            try
            {
                switch (initializeLocal)
                {
                    case InitializeLocal<TParam, TLocal> fpl: local = fpl(ref parameter); break;
                    case Func<TLocal> fl: local = fl(); break;
                    case object o: throw new ArgumentException(initializeLocal.GetType().ToString());
                }

                if (enumerable is FastList<TItem> fastList)
                    enumerable = fastList.Items;
                else if (enumerable is ConcurrentCollector<TItem> collector)
                    enumerable = collector.Items;

                var proxy = new ActionProxy<TParam, TItem, TLocal>(action);
                switch (enumerable)
                {
                    case TItem[] array:
                    {
                        for (var i = fromInclusive; i < toExclusive; i++)
                        {
                            proxy.Invoke(ref parameter, ref array[i], local);
                        }

                        break;
                    }
                    case IReadOnlyList<TItem> iList:
                    {
                        for (var i = fromInclusive; i < toExclusive; i++)
                        {
                            var item = iList[i];
                            proxy.Invoke(ref parameter, ref item, local);
                        }

                        break;
                    }
                    default: throw new ArgumentException(enumerable.GetType().ToString());
                }
            }
            finally
            {
                switch (finalizeLocal)
                {
                    case ProcessItem<TParam, TLocal> apl: apl(ref parameter, local); break;
                    case Action<TLocal> al: al(local); break;
                    case object o: throw new ArgumentException(finalizeLocal.GetType().ToString());
                }
            }
        }

        private static void ExecuteBatchKvP<TParam, TKey, TValue, TLocal>(int fromInclusive, int toExclusive, [NotNull] Dictionary<TKey, TValue> dictionary, ref TParam parameter, object action, object initializeLocal, object finalizeLocal)
        {
            var local = default(TLocal);
            try
            {
                switch (initializeLocal)
                {
                    case InitializeLocal<TParam, TLocal> fpl: local = fpl(ref parameter); break;
                    case Func<TLocal> fl: local = fl(); break;
                    case object o: throw new ArgumentException(initializeLocal.GetType().ToString());
                }

                var enumerator = dictionary.GetEnumerator();
                var index = 0;
                enumerator.MoveNext();

                // Skip to offset
                while (index < fromInclusive)
                {
                    index++;
                    enumerator.MoveNext();
                }

                var proxy = new ActionProxy<TParam, KeyValuePair<TKey, TValue>, TLocal>(action);
                // Process batch
                while (index < toExclusive)
                {
                    var c = enumerator.Current;
                    proxy.Invoke(ref parameter, ref c, local);
                    index++;
                    enumerator.MoveNext();
                }
            }
            finally
            {
                switch (finalizeLocal)
                {
                    case ProcessItem<TParam, TLocal> apl: apl(ref parameter, local); break;
                    case Action<TLocal> al: al(local); break;
                    case object o: throw new ArgumentException(finalizeLocal.GetType().ToString());
                }
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
                            ThreadPool.Instance.QueueWorkItem(() => Sort(collection, maxDegreeOfParallelism - 1, comparer, state));
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

        private class BatchState : ThreadPool.IConcurrentJob
        {
            private static readonly ConcurrentFixedPool<BatchState> pool = new ConcurrentFixedPool<BatchState>(64);
            
            public readonly ManualResetEventSlim Finished = new ManualResetEventSlim(false);
            
            private int startInclusive;
            private int amountFinishedInclusive;
            private int referenceCount;
            private int toExclusive;
            private int batchSize;
            private ICachedDelegateCapture<(int, int)> job;

            [NotNull]
            public static BatchState Acquire(int referenceCount, int fromInclusive, int toExclusive, int batchSize, ICachedDelegateCapture<(int, int)> job)
            {
                var state = pool.Pop();
                Interlocked.Exchange(ref state.referenceCount, referenceCount);
                Interlocked.Exchange(ref state.startInclusive, fromInclusive);
                Interlocked.Exchange(ref state.amountFinishedInclusive, fromInclusive);
                Interlocked.Exchange(ref state.toExclusive, toExclusive);
                Interlocked.Exchange(ref state.batchSize, batchSize);
                if (Interlocked.Exchange(ref state.job, job) != null)
                    throw new Exception("NON-NULL JOB");
                state.Finished.Reset();
                return state;
            }

            public void Work()
            {
                try
                {
                    int newStart;
                    while ((newStart = Interlocked.Add(ref startInclusive, batchSize)) - batchSize < toExclusive)
                    {
                        var range = (newStart - batchSize, Math.Min(toExclusive, newStart));
                        job.Invoke(ref range);
                        if (Interlocked.Add(ref amountFinishedInclusive, batchSize) >= toExclusive)
                        {
                            Finished.Set();
                            break;
                        }
                    }
                }
                finally
                {
                    if (Interlocked.Decrement(ref referenceCount) == 0)
                    {
                        job.Recycle();
                        Interlocked.Exchange(ref job, null);
                        pool.TryPush(this);
                    }
                }
            }
        }

        private static CachedDelegateCapture<TConst, (int, int)> AcquireCapture<TConst>(ActionRef<TConst, (int start, int end)> actionParam, TConst constantParam)
        {
            return CachedDelegateCapture<TConst, (int, int)>.Acquire(actionParam, ref constantParam);
        }

        private class CachedDelegateCapture<TConst, TParam> : ICachedDelegateCapture<TParam>
        {
            private static readonly ConcurrentFixedPool<CachedDelegateCapture<TConst, TParam>> pool = new ConcurrentFixedPool<CachedDelegateCapture<TConst, TParam>>(8);
            private ActionRef<TConst, TParam> action;
            private TConst constant;

            public static CachedDelegateCapture<TConst, TParam> Acquire(ActionRef<TConst, TParam> actionParam, ref TConst constantParam)
            {
                var output = pool.Pop();
                output.action = actionParam;
                output.constant = constantParam;
                return output;
            }

            public void Invoke(ref TParam param)
            {
                action(ref constant, ref param);
            }

            public void Recycle()
            {
                action = null;
                constant = default;
                pool.TryPush(this);
            }
        }

        private interface ICachedDelegateCapture<T>
        {
            void Invoke(ref T param);
            void Recycle();
        }

        private struct ActionProxy<TParam, TItem, TLocal>
        {
            private readonly int selectedAction;
            private readonly Action<TItem> ai;
            private readonly Action<TItem, TLocal> ail;
            private readonly ProcessItem<TParam, TItem> api;
            private readonly ProcessItem<TParam, TItem, TLocal> apil;
            private readonly ProcessKeyValue<TParam, TItem> apkv;
            private readonly ProcessKeyValue<TParam, TItem, TLocal> apkvl;
            private readonly ActionRef<TItem> ari;
            private readonly ActionRef<TItem, TLocal> aril;

            public ActionProxy(object action)
            {
                this = default;
                switch (action)
                {
                    case Action<TItem> a:
                        ai = a;
                        selectedAction = 1;
                        break;
                    case Action<TItem, TLocal> a:
                        ail = a;
                        selectedAction = 2;
                        break;
                    case ProcessItem<TParam, TItem> a:
                        api = a;
                        selectedAction = 3;
                        break;
                    case ProcessItem<TParam, TItem, TLocal> a:
                        apil = a;
                        selectedAction = 4;
                        break;
                    case ProcessKeyValue<TParam, TItem> a:
                        apkv = a;
                        selectedAction = 5;
                        break;
                    case ProcessKeyValue<TParam, TItem, TLocal> a:
                        apkvl = a;
                        selectedAction = 6;
                        break;
                    case ActionRef<TItem> a:
                        ari = a;
                        selectedAction = 7;
                        break;
                    case ActionRef<TItem, TLocal> a:
                        aril = a;
                        selectedAction = 8;
                        break;
                    default: throw new ArgumentException(action.GetType().ToString());
                }
            }

            public void Invoke(ref TParam parameter, ref TItem item, TLocal local)
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