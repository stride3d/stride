// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    private readonly unsafe struct ForEachEnumeratedParameters<T, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, T, void> action) : IBatchJob
        where TCollection : IReadOnlyCollection<T>
        where TParameter : class
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, T, void> Action = action;

        public readonly void Process(int start, int endExclusive)
        {
            using var enumerator = Collection.GetEnumerator();

            // Skip to offset
            for (int i = 0; i < start; i++)
            {
                enumerator.MoveNext();
            }

            // Process batch
            for (int i = start; i < endExclusive && enumerator.MoveNext(); i++)
            {
                Action(Parameter, enumerator.Current);
            }
        }
    }

    public static unsafe void ForEachEnumerated<T, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, T, void> action)
        where TCollection : IReadOnlyCollection<T>
        where TParameter : class
    {
        ForBatched(collection.Count, new ForEachEnumeratedParameters<T, TCollection, TParameter>(collection, parameter, action));
    }

    private readonly unsafe struct ForEachEnumeratedParametersRef<T, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, T, void> action) : IBatchJob
        where TCollection : IReadOnlyCollection<T>
        where TParameter : struct
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, T, void> Action = action;

        public void Process(int start, int endExclusive)
        {
            using var enumerator = Collection.GetEnumerator();

            for (int i = 0; i < start; i++)
            {
                enumerator.MoveNext();
            }

            for (int i = start; i < endExclusive && enumerator.MoveNext(); i++)
            {
                Action(in Parameter, enumerator.Current);
            }
        }
    }

    public static unsafe void ForEachEnumerated<T, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, T, void> action)
        where TCollection : IReadOnlyCollection<T>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachEnumeratedParametersRef<T, TCollection, TParameter>(collection, in parameter, action));
    }

    private readonly unsafe struct ForEachEnumeratedLocalParameters<TItem, TLocal, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, TItem, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : class
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<TParameter, TItem, TLocal, void> Action = action;
        public readonly delegate*<TParameter, TLocal, void> FinalizeLocal = finalizeLocal;

        public readonly void Process(int start, int endExclusive)
        {
            using var enumerator = Collection.GetEnumerator();

            for (int i = 0; i < start; i++)
            {
                enumerator.MoveNext();
            }

            TLocal local = default!;
            try
            {
                if (InitializeLocal != null)
                {
                    local = InitializeLocal(Parameter);
                }

                for (int i = start; i < endExclusive && enumerator.MoveNext(); i++)
                {
                    Action(Parameter, enumerator.Current, local);
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

    public static unsafe void ForEachEnumerated<TItem, TLocal, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, TItem, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal = null)
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : class
    {
        ForBatched(collection.Count, new ForEachEnumeratedLocalParameters<TItem, TLocal, TCollection, TParameter>(collection, parameter, initializeLocal, action, finalizeLocal));
    }

    private readonly unsafe struct ForEachEnumeratedLocalParametersRef<TItem, TLocal, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, TItem, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : struct
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<ref readonly TParameter, TItem, TLocal, void> Action = action;
        public readonly delegate*<ref readonly TParameter, TLocal, void> FinalizeLocal = finalizeLocal;

        public void Process(int start, int endExclusive)
        {
            using var enumerator = Collection.GetEnumerator();

            for (int i = 0; i < start; i++)
            {
                enumerator.MoveNext();
            }

            TLocal local = default!;
            try
            {
                if (InitializeLocal != null)
                {
                    local = InitializeLocal(in Parameter);
                }

                for (int i = start; i < endExclusive && enumerator.MoveNext(); i++)
                {
                    Action(in Parameter, enumerator.Current, local);
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

    public static unsafe void ForEachEnumerated<TItem, TLocal, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, TItem, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal = null)
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachEnumeratedLocalParametersRef<TItem, TLocal, TCollection, TParameter>(collection, in parameter, initializeLocal, action, finalizeLocal));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a ForEachEnumerated instead.")]
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

    [Obsolete("Delegate pooling is not longer supported. Use a ForEachEnumerated instead.")]
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
