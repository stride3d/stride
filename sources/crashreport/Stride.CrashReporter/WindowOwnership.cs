// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace Stride.CrashReporter;

/// <summary>
/// Makes the reporter window an <em>owned</em> window of another process's top-level window (Win32 ownership,
/// set after creation via <c>GWLP_HWNDPARENT</c>, which is allowed across processes). An owned window stays above
/// its owner but not above other applications, is minimized with it, and takes no input away from it -- the right
/// shape for a build crash surfaced next to a healthy GameStudio. Windows only; elsewhere the window stays plain.
/// </summary>
internal static class WindowOwnership
{
    private const int GWLP_HWNDPARENT = -8;

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int index, IntPtr newLong);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hWnd);

    /// <summary>True when <paramref name="owner"/> names a live window on Windows.</summary>
    public static bool CanOwn(IntPtr owner)
        => owner != IntPtr.Zero && OperatingSystem.IsWindows() && IsWindow(owner);

    /// <summary>Sets <paramref name="owner"/> as the owner of <paramref name="window"/>. Best effort: a failure
    /// leaves a plain top-level window.</summary>
    public static void TrySetOwner(Window window, IntPtr owner)
    {
        if (!CanOwn(owner))
            return;
        try
        {
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle != IntPtr.Zero)
                SetWindowLongPtr(handle, GWLP_HWNDPARENT, owner);
        }
        catch
        {
            // Ownership is a nicety; the window works fine without it.
        }
    }
}
