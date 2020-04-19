// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Stride.Core.Annotations;

namespace Stride.Core.Diagnostics
{
    public static class SafeAction
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("SafeAction");

        [NotNull]
        public static ThreadStart Wrap(ThreadStart action, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return () =>
            {
                try
                {
                    action();
                }
                catch (ThreadAbortException)
                {
                    // Ignore this exception
                }
                catch (Exception e)
                {
                    Log.Fatal("Unexpected exception", e, CallerInfo.Get(sourceFilePath, memberName, sourceLineNumber));
                    throw;
                }
            };
        }

        [NotNull]
        public static ParameterizedThreadStart Wrap(ParameterizedThreadStart action, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            return obj =>
            {
                try
                {
                    action(obj);
                }
                catch (ThreadAbortException)
                {
                    // Ignore this exception
                }
                catch (Exception e)
                {
                    Log.Fatal("Unexpected exception", e, CallerInfo.Get(sourceFilePath, memberName, sourceLineNumber));
                    throw;
                }
            };
        }
    }
}
