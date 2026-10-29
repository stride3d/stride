// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
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
