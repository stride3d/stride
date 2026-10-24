// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    /// <summary>
    /// Executes a batch-processing callback on multiple threads over a specified number of items using a shared reference parameter.
    /// </summary>
    /// <typeparam name="T">The reference type passed to each batch callback invocation.</typeparam>
    /// <param name="items">The total number of items to process.</param>
    /// <param name="parameter">The shared parameter instance provided to each batch callback invocation.</param>
    /// <param name="executeBatch">A function pointer that processes a batch of items, receiving the shared parameter and two integers that identify the batch range.</param>
    public static unsafe void ForBatched<T>(int items, T parameter, delegate*<T, int, int, void> executeBatch) where T : class
    {
        ForBatched(items, new BatchedDelegate<T>(parameter, executeBatch));
    }

    [OverloadResolutionPriority(-1)]
    [Obsolete("This overload is not longer supported. Use one of the other overloads.")]
    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<T, int, int, void> executeBatch)
    {
        ForBatched(items, new LegacyBatchedDelegate<T>(parameter, executeBatch));
    }

    /// <summary>
    /// Executes a batch-processing callback on multiple threads over a specified number of items using a shared value parameter.
    /// </summary>
    /// <typeparam name="T">The value type passed by readonly reference to each batch callback invocation.</typeparam>
    /// <param name="items">The total number of items to process.</param>
    /// <param name="parameter">The shared parameter value provided by readonly reference to each batch callback invocation. It is copied twice.</param>
    /// <param name="executeBatch">A function pointer that processes a batch of items, receiving the shared parameter by readonly reference and two integers that identify the batch range.</param>
    public static unsafe void ForBatched<T>(int items, in T parameter, delegate*<ref readonly T, int, int, void> executeBatch) where T : struct
    {
        ForBatched(items, new BatchedDelegateRef<T>(parameter, executeBatch));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void ForBatched(int items, Action<int, int> executeBatch)
    {
        ForBatched(items, new LegacyBatchedDelegate<Action<int, int>>(executeBatch, &ForBatchedAction));

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
