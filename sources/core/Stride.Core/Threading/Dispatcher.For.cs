// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Threading;

public static partial class Dispatcher
{
    private readonly unsafe struct ForParameters<TParameter>(int start, TParameter parameter, delegate*<TParameter, int, void> action)
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, int, void> Action = action;
    }

    public static unsafe void For<TParameter>(int fromInclusive, int toExclusive, TParameter parameter, delegate*<TParameter, int, void> action)
    {
        var parameters = new ForParameters<TParameter>(fromInclusive, parameter, action);
        ForBatched(toExclusive - fromInclusive, ref parameters, &ForWrapped);
        static void ForWrapped(ref ForParameters<TParameter> parameters, int from, int toExclusive)
        {
            for (int i = from; i < toExclusive; i++)
            {
                parameters.Action(parameters.Parameter, parameters.Start + i);
            }
        }
    }

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

    private readonly unsafe struct ForParameters<TLocal, TParameter>(int start, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, int, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal)
    {
        public readonly int Start = start;
        public readonly TParameter Parameter = parameter;
        public readonly delegate*<TParameter, TLocal> InitializeLocal = initializeLocal;
        public readonly delegate*<TParameter, int, TLocal, void> Action = action;
        public readonly delegate*<TParameter, TLocal, void> FinalizeLocal = finalizeLocal;
    }

    public static unsafe void For<TLocal, TParameter>(int fromInclusive, int toExclusive, TParameter parameter, delegate*<TParameter, TLocal> initializeLocal, delegate*<TParameter, int, TLocal, void> action, delegate*<TParameter, TLocal, void> finalizeLocal = null)
    {
        var parameters = new ForParameters<TLocal, TParameter>(fromInclusive, parameter, initializeLocal, action, finalizeLocal);
        ForBatched(toExclusive - fromInclusive, ref parameters, &ForWrapped);
        static void ForWrapped(ref ForParameters<TLocal, TParameter> parameters, int from, int toExclusive)
        {
            TLocal local = default!;
            try
            {
                if (parameters.InitializeLocal != null)
                {
                    local = parameters.InitializeLocal(parameters.Parameter);
                }
                for (int i = from; i < toExclusive; i++)
                {
                    parameters.Action(parameters.Parameter, parameters.Start + i, local);
                }
            }
            finally
            {
                if (parameters.FinalizeLocal != null)
                {
                    parameters.FinalizeLocal(parameters.Parameter, local);
                }
            }
        }
    }

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
