// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;

namespace Stride.Engine.Tests;

/// <summary>
/// Switches the main game window to borderless full screen and back, through the <see cref="GraphicsDeviceManager"/>,
/// checking that the window covers its display at the desktop resolution with a matching windowed back buffer,
/// and that leaving full screen brings back the windowed size.
/// </summary>
/// <remarks>
/// Exclusive full screen changes the display mode of the user's monitor, so it is left to manual testing.
/// </remarks>
[Collection("Windowing")] // window tests pump global SDL/Win32 events, they cannot run in parallel
public class GameWindowFullscreenTest : GameTestBase
{
    public GameWindowFullscreenTest()
    {
        // The default test profile (Level_9_1) limits textures to 2048 pixels, smaller than most displays
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
    }

    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void BorderlessFullscreenCoversDisplay(AppContextType contextType)
    {
        Skip.If(Platform.Type != PlatformType.Windows && contextType == AppContextType.DesktopWinForms, reason: "WinForms is only available on Windows");
        Skip.If(Platform.Type == PlatformType.macOS && contextType == AppContextType.DesktopSDL, reason: "SDL2 + AppKit requires main-thread NSMenu setup; xunit test threads can't do this");
        Skip.If(Platform.Type == PlatformType.Android || Platform.Type == PlatformType.iOS, reason: "Desktop window contexts are not available on mobile platforms");
        Skip.If(Platform.Type == PlatformType.Linux, reason: "SDL takes full screen through the window manager on X11, and a bare display server (such as CI's Xvfb) has none");

        Exception failure = null;
        string skipReason = null;
        var game = new GameWindowFullscreenTest { ContextType = contextType };
        game.Script.AddTask(async () =>
        {
            try
            {
                var window = game.Window;
                // Without WinForms support in this build (net10.0, no STRIDE_UI_WINFORMS) a WinForms context falls back to SDL
                if (contextType == AppContextType.DesktopWinForms && window.GetType().Name != "GameWindowWinforms")
                {
                    skipReason = $"This build has no WinForms window, the game created a {window.GetType().Name}";
                    return;
                }

                window.FullscreenIsBorderlessWindow = true;
                // The test window is created hidden, and SDL only takes full screen once the window is shown
                window.Visible = true;

                // Let the window and device reach a steady state
                await NextFrames(game, 5);

                var windowedSize = ClientSize(window);
                Assert.Equal(window.PreferredWindowedSize, windowedSize);
                var displayBounds = contextType == AppContextType.DesktopSDL
                    ? DisplayBoundsFromSdl(window.ClientBounds)
                    : DisplayBoundsFromWin32(window.NativeWindow.Handle);

                // A borderless full-screen window has the desktop resolution, whatever size was requested
                window.PreferredFullscreenSize = new Int2(800, 600);
                window.IsFullscreen = true;
                await WaitUntil(game, () => window.ClientBounds == displayBounds);
                await NextFrames(game, 5);

                Assert.Equal(displayBounds, window.ClientBounds);
                Assert.Equal(new Int2(displayBounds.Width, displayBounds.Height), BackBufferSize(game));
                Assert.False(game.GraphicsDevice.Presenter.IsFullScreen, "A borderless full-screen window presents windowed");

                window.IsFullscreen = false;
                await WaitUntil(game, () => ClientSize(window) == windowedSize);
                await NextFrames(game, 5);

                Assert.Equal(windowedSize, ClientSize(window));
                Assert.Equal(windowedSize, BackBufferSize(game));
                Assert.False(game.GraphicsDevice.Presenter.IsFullScreen);
            }
            catch (Exception e)
            {
                failure = e;
            }
            finally
            {
                game.Exit();
            }
        });

        // Run directly through GameTester: with a context type set it creates a real window of that type
        GameTester.RunGameTest(game);

        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
        Skip.If(skipReason != null, skipReason);
    }

    private static Int2 ClientSize(GameWindow window)
    {
        var bounds = window.ClientBounds;
        return new Int2(bounds.Width, bounds.Height);
    }

    private static Int2 BackBufferSize(GameTestBase game)
    {
        var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
        return new Int2(backBuffer.Width, backBuffer.Height);
    }

    private static async Task NextFrames(GameTestBase game, int frameCount)
    {
        for (int frame = 0; frame < frameCount; frame++)
            await game.Script.NextFrame();
    }

    private static async Task WaitUntil(GameTestBase game, Func<bool> condition)
    {
        var waitDuration = Stopwatch.StartNew();
        while (!condition() && waitDuration.Elapsed < TimeSpan.FromSeconds(10))
            await game.Script.NextFrame();
    }

    /// <summary>
    /// Gets the desktop bounds of the display holding the centre of the window, as captured when SDL video started.
    /// </summary>
    private static Rectangle DisplayBoundsFromSdl(Rectangle windowBounds)
    {
        var displays = SdlDisplayProvider.Displays;
        Assert.NotNull(displays);
        Assert.NotEmpty(displays);

        var windowCentre = new Point(windowBounds.X + windowBounds.Width / 2, windowBounds.Y + windowBounds.Height / 2);
        foreach (var display in displays)
        {
            if (display.DesktopBounds.Contains(windowCentre))
                return display.DesktopBounds;
        }
        return displays[0].DesktopBounds;
    }

    private static Rectangle DisplayBoundsFromWin32(IntPtr windowHandle)
    {
        var monitor = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
        var monitorInfo = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        Assert.True(GetMonitorInfoW(monitor, ref monitorInfo));

        var monitorBounds = monitorInfo.MonitorBounds;
        return new Rectangle(monitorBounds.Left, monitorBounds.Top, monitorBounds.Right - monitorBounds.Left, monitorBounds.Bottom - monitorBounds.Top);
    }

    private const uint MonitorDefaultToNearest = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRectangle
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRectangle MonitorBounds;
        public NativeRectangle WorkAreaBounds;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr windowHandle, uint flags);

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfo monitorInfo);
}
