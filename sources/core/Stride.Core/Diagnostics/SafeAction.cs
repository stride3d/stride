// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Core.Diagnostics;

public static class SafeAction
{
    private static readonly Logger Log = GlobalLogger.GetLogger("SafeAction");

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
