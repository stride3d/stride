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

        static void ForWrapped((Action<int> action, int start) parameters, int from, int toExclusive)
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

        static void ForWrapped((Func<TLocal> initializeLocal, Action<int, TLocal> action, Action<TLocal>? finalizeLocal, int start) parameters, int from, int toExclusive)
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
