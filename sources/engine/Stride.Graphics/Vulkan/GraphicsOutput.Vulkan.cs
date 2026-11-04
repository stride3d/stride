// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Stride.Core.Mathematics;

namespace Stride.Graphics;

public partial class GraphicsOutput
{
    // Managed snapshots: querying these properties never calls SDL from a render thread.
    internal GraphicsOutput(GraphicsAdapter adapter, DisplayInfo info)
    {
        ArgumentNullException.ThrowIfNull(adapter);
        Adapter = adapter;
        Name = info.Name;
        DesktopBounds = info.DesktopBounds;
        MonitorHandle = info.MonitorHandle;
        currentDisplayMode = info.CurrentDisplayMode;
        supportedDisplayModes = (DisplayMode[])info.SupportedDisplayModes.Clone();
    }

    /// <summary>Gets the native Windows monitor handle, or zero when unavailable.</summary>
    public IntPtr MonitorHandle { get; }

    private void InitializeSupportedDisplayModes() => supportedDisplayModes ??= [];
    private void InitializeCurrentDisplayMode() { }

    /// <summary>
    /// Finds an enumerated mode nearest to the requested dimensions and refresh rate.
    /// Zero dimensions and refresh rate default to the current display mode when known.
    /// A specified format is kept as requested.
    /// </summary>
    public DisplayMode FindClosestMatchingDisplayMode(ReadOnlySpan<GraphicsProfile> targetProfiles, DisplayMode mode)
    {
        // The swapchain picks its own surface format: keep the requested one.
        var match = SelectDisplayMode(SupportedDisplayModes, CurrentDisplayMode, mode);
        return mode.Format != PixelFormat.None ? match with { Format = mode.Format } : match;
    }
}

#endif
