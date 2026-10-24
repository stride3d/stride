// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    private readonly unsafe struct ForEachEnumeratedParameters<TItem, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, TItem, void> action) : IBatchJob
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : class
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, TItem, void> Action = action;

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

    /// <summary>
    /// Executes an action on multiple threads for each item in an enumerable collection using a shared reference parameter.
    /// </summary>
    /// <remarks>
    /// This method has poor time complexity because each batch enumerates from the beginning of the collection. Use it only when <typeparamref name="TCollection"/> does not implement <see cref="IReadOnlyList{TItem}"/>; otherwise, use the corresponding <see cref="ForEach{TItem, TList, TParameter}"/> overload instead.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items in the collection.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <typeparam name="TParameter">The reference type passed to each action invocation.</typeparam>
    /// <param name="collection">The collection whose items are processed.</param>
    /// <param name="parameter">The shared parameter instance provided to each action invocation.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter and the item.</param>
    public static unsafe void ForEachEnumerated<TItem, TCollection, TParameter>(TCollection collection, TParameter parameter, delegate*<TParameter, TItem, void> action)
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : class
    {
        ForBatched(collection.Count, new ForEachEnumeratedParameters<TItem, TCollection, TParameter>(collection, parameter, action));
    }

    private readonly unsafe struct ForEachEnumeratedParametersRef<TItem, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, TItem, void> action) : IBatchJob
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : struct
    {
        public readonly TCollection Collection = collection;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, TItem, void> Action = action;

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

    /// <summary>
    /// Executes an action on multiple threads for each item in an enumerable collection using a shared value parameter.
    /// </summary>
    /// <remarks>
    /// This method has poor time complexity because each batch enumerates from the beginning of the collection. Use it only when <typeparamref name="TCollection"/> does not implement <see cref="IReadOnlyList{TItem}"/>; otherwise, use the corresponding <see cref="ForEach{TItem, TList, TParameter}"/> overload instead.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items in the collection.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <typeparam name="TParameter">The value type passed by readonly reference to each action invocation.</typeparam>
    /// <param name="collection">The collection whose items are processed.</param>
    /// <param name="parameter">The shared parameter value provided by readonly reference to each action invocation. It is copied twice.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter by readonly reference and the item.</param>
    public static unsafe void ForEachEnumerated<TItem, TCollection, TParameter>(TCollection collection, in TParameter parameter, delegate*<ref readonly TParameter, TItem, void> action)
        where TCollection : IReadOnlyCollection<TItem>
        where TParameter : struct
    {
        ForBatched(collection.Count, new ForEachEnumeratedParametersRef<TItem, TCollection, TParameter>(collection, in parameter, action));
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

    /// <summary>
    /// Executes an action on multiple threads for each item in an enumerable collection, using a local value initialized and finalized for each batch.
    /// </summary>
    /// <remarks>
    /// This method has poor time complexity because each batch enumerates from the beginning of the collection. Use it only when <typeparamref name="TCollection"/> does not implement <see cref="IReadOnlyList{TItem}"/>; otherwise, use the corresponding <see cref="ForEach{TItem, TLocal, TList, TParameter}"/> overload instead.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items in the collection.</typeparam>
    /// <typeparam name="TLocal">The local value type used while processing a batch.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <typeparam name="TParameter">The reference type shared with the initialization, action, and finalization callbacks.</typeparam>
    /// <param name="collection">The collection whose items are processed.</param>
    /// <param name="parameter">The shared parameter instance provided to each callback invocation.</param>
    /// <param name="initializeLocal">A function pointer that initializes the local value for a batch, or null to use the default value.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter, the item, and the batch's local value.</param>
    /// <param name="finalizeLocal">An optional function pointer invoked to finalize the local value after processing a batch.</param>
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

    /// <summary>
    /// Executes an action on multiple threads for each item in an enumerable collection, using a local value initialized and finalized for each batch.
    /// </summary>
    /// <remarks>
    /// This method has poor time complexity because each batch enumerates from the beginning of the collection. Use it only when <typeparamref name="TCollection"/> does not implement <see cref="IReadOnlyList{TItem}"/>; otherwise, use the corresponding <see cref="ForEach{TItem, TLocal, TList, TParameter}"/> overload instead.
    /// </remarks>
    /// <typeparam name="TItem">The type of the items in the collection.</typeparam>
    /// <typeparam name="TLocal">The local value type used while processing a batch.</typeparam>
    /// <typeparam name="TCollection">The collection type.</typeparam>
    /// <typeparam name="TParameter">The value type shared by readonly reference with the initialization, action, and finalization callbacks.</typeparam>
    /// <param name="collection">The collection whose items are processed.</param>
    /// <param name="parameter">The shared parameter value provided by readonly reference to each callback invocation. It is copied twice.</param>
    /// <param name="initializeLocal">A function pointer that initializes the local value for a batch, or null to use the default value.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter by readonly reference, the item, and the batch's local value.</param>
    /// <param name="finalizeLocal">An optional function pointer invoked to finalize the local value after processing a batch.</param>
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

        static void ForEachDict(ref readonly (Action<KeyValuePair<TKey, TValue>> action, Dictionary<TKey, TValue> collection) parameters, int from, int toExclusive)
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

        static void ForEachDict(ref readonly (Func<TLocal> initializeLocal, Action<KeyValuePair<TKey, TValue>, TLocal> action, Action<TLocal>? finalizeLocal, Dictionary<TKey, TValue> collection) parameters, int from, int toExclusive)
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
