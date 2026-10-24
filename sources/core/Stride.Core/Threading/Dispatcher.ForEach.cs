// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Collections;

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static void ForEach<T>(T[] collection, Action<T> action) => ForEach<T, T[]>(collection, action);

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static void ForEach<T>(List<T> collection, Action<T> action) => ForEach<T, List<T>>(collection, action);

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static void ForEach<T>(FastCollection<T> collection, Action<T> action) => ForEach<T, FastCollection<T>>(collection, action);

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static void ForEach<T>(ConcurrentCollector<T> collection, Action<T> action) => ForEach<T, ConcurrentCollector<T>>(collection, action);

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void ForEach<T>(ConcurrentCollector<T> collection, ValueAction<T> action)
    {
        var parameters = (action, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList(ref readonly (ValueAction<T> action, ConcurrentCollector<T> collection) parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.action(ref parameters.collection.Items[i]);
            }
        }
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
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

    private readonly unsafe struct ForEachParametersRef<T, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref readonly TParameter, T, void> action) : IBatchJob
        where TList : IReadOnlyList<T>
        where TParameter : struct
    {
        public readonly TList Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, T, void> Action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                Action(in Parameter, Collection[i]);
            }
        }
    }

    public static unsafe void ForEach<T, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref readonly TParameter, T, void> action)
        where TList : IReadOnlyList<T>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachParametersRef<T, TList, TParameter>(collection, in parameter, action));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void ForEach<T, TList>(TList collection, Action<T> action) where TList : IReadOnlyList<T>
    {
        var parameters = (action, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList(ref readonly (Action<T> action, TList collection) parameters, int from, int toExclusive)
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

    private readonly unsafe struct ForEachLocalParametersRef<TItem, TLocal, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, TItem, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TList : IReadOnlyList<TItem>
        where TParameter : struct
    {
        public readonly TList Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<ref readonly TParameter, TItem, TLocal, void> Action = action;
        public readonly delegate*<ref readonly TParameter, TLocal, void> FinalizeLocal = finalizeLocal;

        public void Process(int start, int endExclusive)
        {
            TLocal local = default!;
            try
            {
                if (InitializeLocal != null)
                {
                    local = InitializeLocal(in Parameter);
                }

                for (int i = start; i < endExclusive; i++)
                {
                    Action(in Parameter, Collection[i], local);
                }
            }
            finally
            {
                if (FinalizeLocal != null)
                {
                    FinalizeLocal(in Parameter, local);
                }
            }
        }
    }

    public static unsafe void ForEach<TItem, TLocal, TList, TParameter>(TList collection, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, TItem, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal = null)
        where TList : IReadOnlyList<TItem>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachLocalParametersRef<TItem, TLocal, TList, TParameter>(collection, in parameter, initializeLocal, action, finalizeLocal));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void ForEach<TItem, TLocal, TList>(TList collection, Func<TLocal> initializeLocal, Action<TItem, TLocal> action, Action<TLocal>? finalizeLocal = null) where TList : IReadOnlyList<TItem>
    {
        var parameters = (initializeLocal, action, finalizeLocal, collection);
        ForBatched(collection.Count, parameters, &ForEachList);

        static void ForEachList(ref readonly (Func<TLocal> initializeLocal, Action<TItem, TLocal> action, Action<TLocal>? finalizeLocal, TList collection) parameters, int from, int toExclusive)
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
}
