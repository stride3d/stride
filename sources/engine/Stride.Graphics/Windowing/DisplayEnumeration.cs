// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Graphics;

/// <summary>
/// Selects platform display data and associates it with graphics adapters.
/// </summary>
internal static class DisplayEnumeration
{
    // null means SDL video has not initialized yet; do not cache that as no displays.
    internal static DisplayInfo[] Capture()
    {
#if STRIDE_PLATFORM_DESKTOP
        if (OperatingSystem.IsWindows())
            return WindowsDisplayProvider.Enumerate();
#endif
        return SdlDisplays;
    }

    internal static DisplayInfo[] SdlDisplays
    {
        get
        {
#if STRIDE_UI_SDL
            return SdlDisplayProvider.Displays;
#else
            return [];
#endif
        }
    }

    internal static DisplayInfo[] ForAdapter(DisplayInfo[] displays, long? adapterLuid)
    {
        if (displays == null)
            return null;

        var result = new List<DisplayInfo>();
        foreach (var display in displays)
        {
            // SDL2 has no GPU identity: expose its desktop displays as presentation
            // targets on each adapter, without inferring physical ownership.
            if (!display.AdapterLuid.HasValue || display.AdapterLuid == adapterLuid)
                result.Add(display);
        }
        return result.ToArray();
    }
}

/// <summary>
/// Carries display geometry, modes and optional native identity for creating graphics outputs.
/// </summary>
internal sealed record DisplayInfo(string Name, Rectangle DesktopBounds, DisplayMode? CurrentDisplayMode,
    DisplayMode[] SupportedDisplayModes, long? AdapterLuid = null, IntPtr MonitorHandle = default);
