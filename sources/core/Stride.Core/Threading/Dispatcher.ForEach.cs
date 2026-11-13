// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

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
}
