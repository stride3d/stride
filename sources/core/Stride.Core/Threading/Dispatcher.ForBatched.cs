// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    public static unsafe void ForBatched<T>(int items, T parameter, delegate*<T, int, int, void> executeBatch) where T : class
    {
        var batchedDelegate = new BatchedDelegate<T>(parameter, executeBatch);
        ForBatched(items, batchedDelegate);
    }

    [OverloadResolutionPriority(-1)]
    [Obsolete("This overload is not longer supported. Use one of the other overloads.")]
    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<T, int, int, void> executeBatch)
    {
        var batchedDelegate = new LegacyBatchedDelegate<T>(parameter, executeBatch);
        ForBatched(items, batchedDelegate);
    }

    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<ref readonly T, int, int, void> executeBatch) where T : struct
    {
        var batchedDelegate = new BatchedDelegateRef<T>(parameter, executeBatch);
        ForBatched(items, batchedDelegate);
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void ForBatched(int items, Action<int, int> executeBatch)
    {
        var batchedDelegate = new LegacyBatchedDelegate<Action<int, int>>(executeBatch, &ForBatchedAction);
        ForBatched(items, batchedDelegate);

        static void ForBatchedAction(Action<int, int> parameter, int from, int toExclusive)
        {
            parameter(from, toExclusive);
        }
    }

    private readonly unsafe struct BatchedDelegate<T>(T parameter, delegate*<T, int, int, void> @delegate) : IBatchJob where T : class
    {
        public readonly T Parameter = parameter;
        public readonly delegate*<T, int, int, void> Delegate = @delegate;

        public readonly void Process(int start, int endExclusive)
        {
            Delegate(Parameter, start, endExclusive);
        }
    }

    private readonly unsafe struct BatchedDelegateRef<T>(in T parameter, delegate*<ref readonly T, int, int, void> @delegate) : IBatchJob where T : struct
    {
        public readonly T Parameter = parameter;
        public readonly delegate*<ref readonly T, int, int, void> Delegate = @delegate;

        public void Process(int start, int endExclusive)
        {
            Delegate(in Parameter, start, endExclusive);
        }
    }

    private readonly unsafe struct LegacyBatchedDelegate<T>(T parameter, delegate*<T, int, int, void> @delegate) : IBatchJob
    {
        public readonly T Parameter = parameter;
        public readonly delegate*<T, int, int, void> Delegate = @delegate;

        public readonly void Process(int start, int endExclusive)
        {
            Delegate(Parameter, start, endExclusive);
        }
    }
}
