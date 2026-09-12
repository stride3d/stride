// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;

namespace Stride.Core.Assets;

/// <summary>
/// Advisory "an instance runs from this directory" marker for the launcher's uninstall check: an editor
/// re-executed through <c>dotnet exec</c> is a dotnet process, so its main module no longer points into the install.
/// </summary>
public static class HostInstanceMutex
{
    public static string NameFor(string appDirectory) => @"Local\Stride.Host." + DotNetHostSelector.PathHash(appDirectory);

    /// <summary>Holds the marker; keep the mutex alive for the process lifetime.</summary>
    public static Mutex Hold(string appDirectory) => new(false, NameFor(appDirectory));

    public static bool IsHeld(string appDirectory)
    {
        try
        {
            if (!Mutex.TryOpenExisting(NameFor(appDirectory), out var mutex))
                return false;
            mutex.Dispose();
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            // Exists but owned by a process we cannot open (an elevated editor): held.
            return true;
        }
    }
}
