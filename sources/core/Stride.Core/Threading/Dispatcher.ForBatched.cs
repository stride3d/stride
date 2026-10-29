// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
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

    public static unsafe void ForBatched(int items, Action<int, int> executeBatch)
    {
        var batchedDelegate = new BatchedDelegate<Action<int, int>>
        {
            Param = executeBatch,
            Delegate = &ForBatchedAction,
        };
        ForBatched(items, batchedDelegate);

        static void ForBatchedAction(Action<int, int> parameter, int from, int toExclusive)
        {
            parameter(from, toExclusive);
        }
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
