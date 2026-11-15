// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Silk.NET.SDL;

namespace Stride.Graphics;

/// <summary>
/// Captures SDL display data at video initialization for later use without native SDL calls.
/// </summary>
internal static class SdlDisplayProvider
{
    // Published once by SDL window initialization, on its initialization thread.
    // Reading adapter outputs never calls SDL, including from render threads.
    internal static volatile DisplayInfo[] Displays;

    internal static void CaptureDisplays() => Displays = Enumerate();

    internal static bool IsVideoInitialized => SDL.Window.SDL != null &&
        (SDL.Window.SDL.WasInit(Sdl.InitVideo) & Sdl.InitVideo) != 0;

    internal static unsafe DisplayInfo[] Enumerate()
    {
        var outputs = new List<DisplayInfo>();
        var sdl = SDL.Window.SDL;
        // Never initialize SDL just to enumerate GPUs (headless tools/servers).
        // Window creation refreshes this snapshot once video is available.
        if (!IsVideoInitialized)
            return [];

        int count = sdl.GetNumVideoDisplays();
        for (int display = 0; display < count; display++)
        {
            Silk.NET.Maths.Rectangle<int> bounds;
            if (sdl.GetDisplayBounds(display, &bounds) != 0 || bounds.Size.X <= 0 || bounds.Size.Y <= 0)
                continue;

            DisplayMode? current = null;
            Silk.NET.SDL.DisplayMode native;
            if (sdl.GetCurrentDisplayMode(display, &native) == 0)
                current = ConvertMode(native);

            var modes = new List<DisplayMode>();
            var seen = new HashSet<DisplayMode>();
            int modeCount = sdl.GetNumDisplayModes(display);
            for (int index = 0; index < modeCount; index++)
            {
                if (sdl.GetDisplayMode(display, index, &native) == 0 && ConvertMode(native) is { } mode && seen.Add(mode))
                    modes.Add(mode);
            }
            outputs.Add(new DisplayInfo(sdl.GetDisplayNameS(display),
                new Rectangle(bounds.Origin.X, bounds.Origin.Y, bounds.Size.X, bounds.Size.Y), current, modes.ToArray()));
        }
        return outputs.ToArray();
    }

    private static DisplayMode? ConvertMode(Silk.NET.SDL.DisplayMode mode)
    {
        if (mode.W <= 0 || mode.H <= 0)
            return null;

        // SDL describes the desktop pixel layout, not a rendering surface format.
        // Map only layouts represented by Stride; the presenter independently
        // chooses a supported swapchain format for the actual surface.
        var format = mode.Format switch
        {
            var value when value == Sdl.PixelformatAbgr8888 => PixelFormat.R8G8B8A8_UNorm,
            var value when value == Sdl.PixelformatArgb8888 || value == Sdl.PixelformatRgb888 => PixelFormat.B8G8R8A8_UNorm,
            var value when value == Sdl.PixelformatBgr888 => PixelFormat.R8G8B8A8_UNorm,
            var value when value == Sdl.PixelformatRgb565 => PixelFormat.B5G6R5_UNorm,
            var value when value == Sdl.PixelformatArgb1555 || value == Sdl.PixelformatRgb555 => PixelFormat.B5G5R5A1_UNorm,
            _ => PixelFormat.None,
        };
        return format == PixelFormat.None ? null : new DisplayMode(format, mode.W, mode.H,
            new Rational(mode.RefreshRate > 0 ? mode.RefreshRate : 0, 1));
    }
}
#endif
