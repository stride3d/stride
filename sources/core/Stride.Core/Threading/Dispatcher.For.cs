// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    /// <summary>
    /// Runs <paramref name="action"/> for each index from <paramref name="fromInclusive"/> to <paramref name="toExclusive"/>, on multiple threads.
    /// </summary>
    /// <remarks>Use a <see langword="static"/> lambda and pass what it needs through <paramref name="state"/>, so that the call does not allocate.</remarks>
    /// <param name="fromInclusive">The first index.</param>
    /// <param name="toExclusive">The index after the last one.</param>
    /// <param name="state">The value passed to each <paramref name="action"/> call.</param>
    /// <param name="action">Called with the state and the index.</param>
    public static void For<TState>(int fromInclusive, int toExclusive, TState state, Action<TState, int> action)
    {
        ForBatched(toExclusive - fromInclusive, new ForJob<TState>(fromInclusive, state, action));
    }

    private readonly struct ForJob<TState>(int offset, TState state, Action<TState, int> action) : IBatchJob
    {
        private readonly int offset = offset;
        private readonly TState state = state;
        private readonly Action<TState, int> action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                action(state, offset + i);
            }
        }
    }
}
