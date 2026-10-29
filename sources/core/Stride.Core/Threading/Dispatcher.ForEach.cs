// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Stride.Core.Collections;

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    /// <summary>
    /// Runs <paramref name="action"/> for each item of <paramref name="collection"/>, on multiple threads.
    /// </summary>
    /// <remarks>Use a <see langword="static"/> lambda and pass what it needs through <paramref name="state"/>, so that the call does not allocate.</remarks>
    /// <param name="collection">The items to process.</param>
    /// <param name="state">The value passed to each <paramref name="action"/> call.</param>
    /// <param name="action">Called with the state and the item.</param>
    public static void ForEach<T, TState>(IReadOnlyList<T> collection, TState state, Action<TState, T> action)
    {
        // Collections backed by an array are read through a span, without an interface call per item
        switch (collection)
        {
            case T[] array:
                ForBatched(array.Length, new ForEachArrayJob<T, TState>(array, state, action));
                break;
            case List<T> list:
                ForBatched(list.Count, new ForEachListJob<T, TState>(list, state, action));
                break;
            case ConcurrentCollector<T> collector:
                ForBatched(collector.Count, new ForEachArrayJob<T, TState>(collector.Items, state, action));
                break;
            default:
                ForBatched(collection.Count, new ForEachJob<T, TState>(collection, state, action));
                break;
        }
    }

    private readonly struct ForEachArrayJob<T, TState>(T[] items, TState state, Action<TState, T> action) : IBatchJob
    {
        private readonly T[] items = items;
        private readonly TState state = state;
        private readonly Action<TState, T> action = action;

        public void Process(int start, int endExclusive)
        {
            // ReadOnlySpan, since Span throws on a covariant array
            foreach (var item in new ReadOnlySpan<T>(items, start, endExclusive - start))
            {
                action(state, item);
            }
        }
    }

    private readonly struct ForEachListJob<T, TState>(List<T> list, TState state, Action<TState, T> action) : IBatchJob
    {
        private readonly List<T> list = list;
        private readonly TState state = state;
        private readonly Action<TState, T> action = action;

        public void Process(int start, int endExclusive)
        {
            foreach (var item in CollectionsMarshal.AsSpan(list)[start..endExclusive])
            {
                action(state, item);
            }
        }
    }

    private readonly struct ForEachJob<T, TState>(IReadOnlyList<T> collection, TState state, Action<TState, T> action) : IBatchJob
    {
        private readonly IReadOnlyList<T> collection = collection;
        private readonly TState state = state;
        private readonly Action<TState, T> action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                action(state, collection[i]);
            }
        }
    }

    public static void ForEach<T>(T[] collection, Action<T> action) => ForEach<T, T[]>(collection, action);

    public static void ForEach<T>(List<T> collection, Action<T> action) => ForEach<T, List<T>>(collection, action);

    public static void ForEach<T>(FastCollection<T> collection, Action<T> action) => ForEach<T, FastCollection<T>>(collection, action);

    public static void ForEach<T>(ConcurrentCollector<T> collection, Action<T> action) => ForEach<T, ConcurrentCollector<T>>(collection, action);

    public static unsafe void ForEach<T>(ConcurrentCollector<T> collection, ValueAction<T> action)
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

    public static void ForEach<T, TLocal>(ConcurrentCollector<T> collection, Func<TLocal> initializeLocal, Action<T, TLocal> action, Action<TLocal>? finalizeLocal = null)
    {
        ForEach<T, TLocal, ConcurrentCollector<T>>(collection, initializeLocal, action, finalizeLocal);
    }

    public static unsafe void ForEach<T, TList>(TList collection, Action<T> action) where TList : IReadOnlyList<T>
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

    public static unsafe void ForEach<TItem, TLocal, TList>(TList collection, Func<TLocal> initializeLocal, Action<TItem, TLocal> action, Action<TLocal>? finalizeLocal = null) where TList : IReadOnlyList<TItem>
    {
        var parameters = (initializeLocal, action, finalizeLocal, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList((Func<TLocal> initializeLocal, Action<TItem, TLocal> action, Action<TLocal>? finalizeLocal, TList collection) parameters, int from, int toExclusive)
        {
            TLocal local = default!;
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

    public static unsafe void ForEach<TKey, TValue>(Dictionary<TKey, TValue> collection, Action<KeyValuePair<TKey, TValue>> action)
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

    public static unsafe void ForEach<TKey, TValue, TLocal>(Dictionary<TKey, TValue> collection, Func<TLocal> initializeLocal, Action<KeyValuePair<TKey, TValue>, TLocal> action, Action<TLocal>? finalizeLocal = null)
        where TKey : notnull
    {
        var parameters = (initializeLocal, action, finalizeLocal, collection);
        ForBatched(collection.Count, parameters, &ForEachDict);

        static void ForEachDict((Func<TLocal> initializeLocal, Action<KeyValuePair<TKey, TValue>, TLocal> action, Action<TLocal>? finalizeLocal, Dictionary<TKey, TValue> collection) parameters, int from, int toExclusive)
        {
            using var enumerator = parameters.collection.GetEnumerator();

            for (int i = 0; i < from; i++) // Skip to the start of our batch
            {
                enumerator.MoveNext();
            }

            TLocal local = default!;
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
}
