// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    /// <summary>
    /// Splits <paramref name="items"/> into batches and runs <paramref name="executeBatch"/> on each batch, on multiple threads.
    /// </summary>
    /// <remarks>Use a <see langword="static"/> lambda and pass what it needs through <paramref name="state"/>, so that the call does not allocate.</remarks>
    /// <param name="items">The number of items to process.</param>
    /// <param name="state">The value passed to each <paramref name="executeBatch"/> call.</param>
    /// <param name="executeBatch">Called with the state and the range of a batch, from inclusive to exclusive.</param>
    public static void ForBatched<TState>(int items, TState state, Action<TState, int, int> executeBatch)
    {
        ForBatched(items, new DelegateBatchJob<TState>(state, executeBatch));
    }

    private readonly struct DelegateBatchJob<TState>(TState state, Action<TState, int, int> action) : IBatchJob
    {
        private readonly TState state = state;
        private readonly Action<TState, int, int> action = action;

        public void Process(int start, int endExclusive) => action(state, start, endExclusive);
    }

    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<T, int, int, void> executeBatch)
    {
        var batchedDelegate = new BatchedDelegate<T>
        {
            Param = parameter,
            Delegate = executeBatch,
        };
        ForBatched(items, batchedDelegate);
    }

    public static unsafe void ForBatched<T>(int items, ref T parameter, delegate*<ref T, int, int, void> executeBatch)
    {
        var batchedDelegate = new BatchedDelegateRef<T>
        {
            Param = parameter,
            Delegate = executeBatch,
        };
        ForBatched(items, batchedDelegate);
    }

    private struct BatchedDelegateRef<T> : IBatchJob
    {
        public T Param;
        public unsafe delegate*<ref T, int, int, void> Delegate;

        public unsafe void Process(int start, int endExclusive)
        {
            Delegate(ref Param, start, endExclusive);
        }
    }

    private struct BatchedDelegate<T> : IBatchJob
    {
        public T Param;
        public unsafe delegate*<T, int, int, void> Delegate;

        public readonly unsafe void Process(int start, int endExclusive)
        {
            Delegate(Param, start, endExclusive);
        }
    }
}
