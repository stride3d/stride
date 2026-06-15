// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.InteropServices;

namespace Stride.GameStudio.AutoTesting;

/// <summary>Helpers shared between the AutoTesting runner and the xunit orchestrator.</summary>
public static class DpiUtil
{
    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);
    [DllImport("Shcore.dll")]
    private static extern int GetDpiForMonitor(IntPtr hmonitor, int dpiType, out uint dpiX, out uint dpiY);
    [DllImport("user32.dll")]
    private static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 = (IntPtr)(-4);

    /// <summary>
    /// Returns the primary monitor's effective DPI scale as an integer percentage (96 → 100,
    /// 144 → 150, 192 → 200, …). Switches the calling thread to PerMonitorAwareV2 first because
    /// <c>GetDpiForMonitor(MDT_EFFECTIVE_DPI)</c> returns 96 in DPI-unaware processes regardless
    /// of actual scaling.
    /// </summary>
    public static int DetectDpiPercent()
    {
        var prevContext = IntPtr.Zero;
        try
        {
            prevContext = SetThreadDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
            var hMon = MonitorFromPoint(default, 1 /* MONITOR_DEFAULTTOPRIMARY */);
            if (GetDpiForMonitor(hMon, 0 /* MDT_EFFECTIVE_DPI */, out var dpi, out _) == 0)
                return (int)Math.Round(dpi / 96.0 * 100);
        }
        catch { /* fall back to 100 */ }
        finally
        {
            if (prevContext != IntPtr.Zero) SetThreadDpiAwarenessContext(prevContext);
        }
        return 100;
    }
}
