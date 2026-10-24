// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    private readonly unsafe struct ForParameters<TParameter>(int start, TParameter parameter, delegate*<TParameter, int, void> action) : IBatchJob
        where TParameter : class
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, int, void> Action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                Action(Parameter, Start + i);
            }
        }
    }

    /// <summary>
    /// Executes an action on multiple threads for each item in a specified range using a shared reference parameter.
    /// </summary>
    /// <typeparam name="TParameter">The reference type passed to each action invocation.</typeparam>
    /// <param name="fromInclusive">The index of the first item to process.</param>
    /// <param name="toExclusive">The index immediately after the last item to process.</param>
    /// <param name="parameter">The shared parameter instance provided to each action invocation.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter and the item's index.</param>
    public static unsafe void For<TParameter>(int fromInclusive, int toExclusive, TParameter parameter, delegate*<TParameter, int, void> action)
        where TParameter : class
    {
        ForBatched(toExclusive - fromInclusive, new ForParameters<TParameter>(fromInclusive, parameter, action));
    }

    private readonly unsafe struct ForParametersRef<TParameter>(int start, in TParameter parameter, delegate*<ref readonly TParameter, int, void> action) : IBatchJob
        where TParameter : struct
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, int, void> Action = action;

        public void Process(int start, int endExclusive)
        {
            for (int i = start; i < endExclusive; i++)
            {
                Action(in Parameter, Start + i);
            }
        }
    }

    /// <summary>
    /// Executes an action on multiple threads for each item in a specified range using a shared value parameter.
    /// </summary>
    /// <typeparam name="TParameter">The value type passed by readonly reference to each action invocation.</typeparam>
    /// <param name="fromInclusive">The index of the first item to process.</param>
    /// <param name="toExclusive">The index immediately after the last item to process.</param>
    /// <param name="parameter">The shared parameter value provided by readonly reference to each action invocation. It is copied twice.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter by readonly reference and the item's index.</param>
    public static unsafe void For<TParameter>(int fromInclusive, int toExclusive, in TParameter parameter, delegate*<ref readonly TParameter, int, void> action)
        where TParameter : struct
    {
        ForBatched(toExclusive - fromInclusive, new ForParametersRef<TParameter>(fromInclusive, in parameter, action));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void For(int fromInclusive, int toExclusive, Action<int> action)
    {
        var parameters = (action, fromInclusive);
        ForBatched(toExclusive - fromInclusive, parameters, &ForWrapped);

        static void ForWrapped(ref readonly (Action<int> action, int start) parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.action(parameters.start + i);
            }
        }
    }

    private readonly unsafe struct ForParameters<TLocal, TParameter>(int start, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, int, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TParameter : class
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<TParameter, int, TLocal, void> Action = action;
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
                    Action(Parameter, Start + i, local);
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
    /// Executes an action on multiple threads for each item in a specified range, using a local value initialized and finalized for each batch.
    /// </summary>
    /// <typeparam name="TLocal">The local value type used while processing a batch.</typeparam>
    /// <typeparam name="TParameter">The reference type shared with the initialization, action, and finalization callbacks.</typeparam>
    /// <param name="fromInclusive">The index of the first item to process.</param>
    /// <param name="toExclusive">The index immediately after the last item to process.</param>
    /// <param name="parameter">The shared parameter instance provided to each callback invocation.</param>
    /// <param name="initializeLocal">A function pointer that initializes the local value for a batch, or null to use the default value.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter, the item's index, and the batch's local value.</param>
    /// <param name="finalizeLocal">An optional function pointer invoked to finalize the local value after processing a batch.</param>
    public static unsafe void For<TLocal, TParameter>(int fromInclusive, int toExclusive, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, int, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal = null)
        where TParameter : class
    {
        ForBatched(toExclusive - fromInclusive, new ForParameters<TLocal, TParameter>(fromInclusive, parameter, initializeLocal, action, finalizeLocal));
    }

    private readonly unsafe struct ForParametersRef<TLocal, TParameter>(int start, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, int, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal) : IBatchJob
        where TParameter : struct
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<ref readonly TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<ref readonly TParameter, int, TLocal, void> Action = action;
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
                    Action(in Parameter, Start + i, local);
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
    /// Executes an action on multiple threads for each item in a specified range, using a local value initialized and finalized for each batch.
    /// </summary>
    /// <typeparam name="TLocal">The local value type used while processing a batch.</typeparam>
    /// <typeparam name="TParameter">The value type shared by readonly reference with the initialization, action, and finalization callbacks.</typeparam>
    /// <param name="fromInclusive">The index of the first item to process.</param>
    /// <param name="toExclusive">The index immediately after the last item to process.</param>
    /// <param name="parameter">The shared parameter value provided by readonly reference to each callback invocation. It is copied twice.</param>
    /// <param name="initializeLocal">A function pointer that initializes the local value for a batch, or null to use the default value.</param>
    /// <param name="action">A function pointer invoked for each item, receiving the shared parameter by readonly reference, the item's index, and the batch's local value.</param>
    /// <param name="finalizeLocal">An optional function pointer invoked to finalize the local value after processing a batch.</param>
    public static unsafe void For<TLocal, TParameter>(int fromInclusive, int toExclusive, in TParameter parameter, delegate*<ref readonly TParameter, TLocal> initializeLocal, delegate*<ref readonly TParameter, int, TLocal, void> action, delegate*<ref readonly TParameter, TLocal, void> finalizeLocal = null)
        where TParameter : struct
    {
        ForBatched(toExclusive - fromInclusive, new ForParametersRef<TLocal, TParameter>(fromInclusive, in parameter, initializeLocal, action, finalizeLocal));
    }

    [Obsolete("Delegate pooling is not longer supported. Use a more performant overload.")]
    public static unsafe void For<TLocal>(int fromInclusive, int toExclusive, Func<TLocal> initializeLocal, Action<int, TLocal> action, Action<TLocal>? finalizeLocal = null)
    {
        var parameters = (initializeLocal, action, finalizeLocal, fromInclusive);
        ForBatched(toExclusive - fromInclusive, parameters, &ForWrapped);

        static void ForWrapped(ref readonly (Func<TLocal> initializeLocal, Action<int, TLocal> action, Action<TLocal>? finalizeLocal, int start) parameters, int from, int toExclusive)
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
                    parameters.action(parameters.start + i, local);
                }
            }
            finally
            {
                parameters.finalizeLocal?.Invoke(local);
            }
        }
    }
}
