// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

namespace Stride.CrashReport;

/// <summary>
/// One naming for managed frames across the live capture, the dump walk and the signature.
/// </summary>
public static class FrameNames
{
    // The compiler's async state machine: "Ns.Type+<Method>d__30" running "MoveNext". The number is the compiler's
    // and moves between builds, so a title or a signature that kept it would split one bug per build.
    private static readonly Regex AsyncStateMachine = new(@"^(?<type>.+)\+<(?<method>[^>]+)>d__\d+$", RegexOptions.Compiled);

    /// <summary>"Ns.Type.Method", with an async state machine folded back to the method it implements.</summary>
    public static string Qualified(string typeFullName, string methodName)
    {
        if (string.IsNullOrEmpty(typeFullName))
            return methodName;
        if (methodName == "MoveNext" && AsyncStateMachine.Match(typeFullName) is { Success: true } async)
            return async.Groups["type"].Value + "." + async.Groups["method"].Value;
        return typeFullName + "." + methodName;
    }
}
