// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Threading;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests;

/// <summary>
/// Presents while the window is minimized with a fixed backbuffer size: the presenter must keep its
/// backbuffer (Vulkan cannot create a 0x0 swapchain) and draw at the original size after restore.
/// </summary>
public class TestPresentWhileMinimized : GameTestBase
{
    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void KeepsBackBufferSize(AppContextType contextType)
    {
        Skip.If(Platform.Type != PlatformType.Windows, reason: "Programmatic minimize/restore needs a real window manager; this test drives it through Win32 ShowWindow");

        PerformTest(game =>
        {
            var context = GameContextFactory.NewGameContext(contextType, isUserManagingRun: true);
            var windowRenderer = new GameWindowRenderer(game.Services, context)
            {
                PreferredBackBufferWidth = 640,
                PreferredBackBufferHeight = 480,
            };
            windowRenderer.Initialize();
            ((IContentable)windowRenderer).LoadContent();

            var window = windowRenderer.Window;
            var messageLoop = window.CreateUserManagedMessageLoop();
            messageLoop.NextFrame();

            Assert.True(windowRenderer.BeginDraw());
            game.GraphicsContext.CommandList.Clear(windowRenderer.Presenter.BackBuffer, Color.Blue);
            windowRenderer.EndDraw();

            var hwnd = window.NativeWindow.Handle;
            ShowWindow(hwnd, SW_MINIMIZE);
            PumpUntil(messageLoop, () => window.IsMinimized);
            Assert.True(window.IsMinimized);

            // The backbuffer size is fixed, so the frame is drawn and presented unless the presenter skips it
            if (windowRenderer.BeginDraw())
            {
                game.GraphicsContext.CommandList.Clear(windowRenderer.Presenter.BackBuffer, Color.Red);
                windowRenderer.EndDraw();
            }
            Assert.Equal(new Int2(640, 480), BackBufferSize(windowRenderer.Presenter));

            ShowWindow(hwnd, SW_RESTORE);
            PumpUntil(messageLoop, () => !window.IsMinimized && window.ClientBounds.Width > 0);
            Assert.False(window.IsMinimized);

            Assert.True(windowRenderer.BeginDraw());
            Assert.Equal(new Int2(640, 480), BackBufferSize(windowRenderer.Presenter));
            game.GraphicsContext.CommandList.Clear(windowRenderer.Presenter.BackBuffer, Color.Green);
            windowRenderer.EndDraw();

            windowRenderer.Dispose();
        });
    }

    private static Int2 BackBufferSize(GraphicsPresenter presenter)
    {
        var backBuffer = presenter.BackBuffer;
        return new Int2(backBuffer.Width, backBuffer.Height);
    }

    private static void PumpUntil(IMessageLoop messageLoop, Func<bool> condition)
    {
        for (int i = 0; i < 200 && !condition(); i++)
        {
            messageLoop.NextFrame();
            Thread.Sleep(10);
        }
    }

    private const int SW_MINIMIZE = 6;
    private const int SW_RESTORE = 9;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
}
