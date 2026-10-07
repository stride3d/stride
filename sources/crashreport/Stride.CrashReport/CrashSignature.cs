// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;

namespace Stride.CrashReport;

/// <summary>
/// Computes the dedup key that decides when two crashes count as "the same": exception type + the frame it
/// was thrown from + the build-step kind. It deliberately excludes the asset name, so one bug hitting many
/// assets collapses into a single group instead of looking like many distinct crashes.
/// </summary>
public static class CrashSignature
{
    public static string Compute(Exception exception, string stepKind)
    {
        var type = exception?.GetType().FullName ?? "UnknownException";
        return $"{type}|{TopFrame(exception)}|{(string.IsNullOrEmpty(stepKind) ? "?" : stepKind)}";
    }

    // "Namespace.Type.Method" of the frame the exception was thrown from — the crash location, stable across
    // the different assets that trigger the same bug. No file/line, which would over-split the same bug.
    private static string TopFrame(Exception exception)
    {
        if (exception == null)
            return "?";
        var frames = new StackTrace(exception, fNeedFileInfo: false).GetFrames();
        if (frames.Length == 0)
            return "?";
        var method = frames[0].GetMethod();
        if (method == null)
            return "?";
        return FrameNames.Qualified(method.DeclaringType?.FullName ?? "?", method.Name);
    }
}
