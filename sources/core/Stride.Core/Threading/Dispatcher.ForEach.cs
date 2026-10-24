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

    private readonly unsafe struct ForEachParameters<T, TList, TParameter>(TList collection, TParameter parameter, delegate*<TParameter, T, void> action) : IBatchJob
        where TList : IReadOnlyList<T>
        where TParameter : class
    {
        public readonly TList Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, T, void> Action = action;

        public readonly void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                Action(Parameter, Collection[i]);
            }
        }
    }

    public static unsafe void ForEach<T, TList, TParameter>(TList collection, TParameter parameter, delegate*<TParameter, T, void> action)
        where TList : IReadOnlyList<T>
        where TParameter : class
    {
        ForBatched(collection.Count, new ForEachParameters<T, TList, TParameter>(collection, parameter, action));
    }

    private unsafe struct ForEachParametersRef<T, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref TParameter, T, void> action) : IBatchJob
        where TList : IReadOnlyList<T>
        where TParameter : struct
    {
        public readonly TList Collection = collection;
        public TParameter Parameter = parameter;
        public readonly delegate*<ref TParameter, T, void> Action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                Action(ref Parameter, Collection[i]);
            }
        }
    }

    public static unsafe void ForEach<T, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref TParameter, T, void> action)
        where TList : IReadOnlyList<T>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachParametersRef<T, TList, TParameter>(collection, in parameter, action));
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

    private readonly unsafe struct ForEachLocalParameters<TItem, TLocal, TList, TParameter>(TList collection, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, TItem, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TList : IReadOnlyList<TItem>
        where TParameter : class
    {
        public readonly TList Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<TParameter, TItem, TLocal, void> Action = action;
        public readonly delegate*<TParameter, TLocal, void> FinalizeLocal = finalizeLocal;

        public readonly void Process(int start, int endExclusive)
        {
            TLocal local = default!;
            try
            {
                if (InitializeLocal != null)
                {
                    local = InitializeLocal(Parameter);
                }

                for (int i = start; i < endExclusive; i++)
                {
                    Action(Parameter, Collection[i], local);
                }
            }
            finally
            {
                if (FinalizeLocal != null)
                {
                    FinalizeLocal(Parameter, local);
                }
            }
        }
    }

    public static unsafe void ForEach<TItem, TLocal, TList, TParameter>(TList collection, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, TItem, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal = null)
        where TList : IReadOnlyList<TItem>
        where TParameter : class
    {
        ForBatched(collection.Count, new ForEachLocalParameters<TItem, TLocal, TList, TParameter>(collection, parameter, initializeLocal, action, finalizeLocal));
    }

    private unsafe struct ForEachLocalParametersRef<TItem, TLocal, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref TParameter, TLocal> initializeLocal, delegate*<ref TParameter, TItem, TLocal, void> action, delegate*<ref TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TList : IReadOnlyList<TItem>
        where TParameter : struct
    {
        public readonly TList Collection = collection;
        public TParameter Parameter = parameter;
        public readonly delegate*<ref TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<ref TParameter, TItem, TLocal, void> Action = action;
        public readonly delegate*<ref TParameter, TLocal, void> FinalizeLocal = finalizeLocal;

        public void Process(int start, int endExclusive)
        {
            TLocal local = default!;
            try
            {
                if (InitializeLocal != null)
                {
                    local = InitializeLocal(ref Parameter);
                }

                for (int i = start; i < endExclusive; i++)
                {
                    Action(ref Parameter, Collection[i], local);
                }
            }
            finally
            {
                if (FinalizeLocal != null)
                {
                    FinalizeLocal(ref Parameter, local);
                }
            }
        }
    }

    public static unsafe void ForEach<TItem, TLocal, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref TParameter, TLocal> initializeLocal, delegate*<ref TParameter, TItem, TLocal, void> action, delegate*<ref TParameter, TLocal, void> finalizeLocal = null)
        where TList : IReadOnlyList<TItem>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachLocalParametersRef<TItem, TLocal, TList, TParameter>(collection, in parameter, initializeLocal, action, finalizeLocal));
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
