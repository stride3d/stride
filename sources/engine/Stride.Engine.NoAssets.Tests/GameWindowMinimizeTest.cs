// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;

namespace Stride.Engine.Tests;

/// <summary>
/// Tests the <see cref="GameWindow"/> base logic around <see cref="GameWindow.ClientBounds"/>:
/// an empty client area (e.g. minimized window) must not be treated as a valid size.
/// </summary>
public class GameWindowClientBoundsTest
{
    private class TestGameWindow : GameWindow
    {
        public Rectangle Bounds = new Rectangle(0, 0, 640, 480);

        public override bool AllowUserResizing { get; set; }
        public override Rectangle ClientBounds => Bounds;
        public override DisplayOrientation CurrentOrientation => DisplayOrientation.Default;
        public override bool IsMinimized => false;
        public override bool Focused => true;
        public override bool IsMouseVisible { get; set; }
        public override WindowHandle NativeWindow => null;
        public override bool Visible { get; set; }
        public override double Opacity { get; set; }
        public override bool IsBorderLess { get; set; }

        public override void BeginScreenDeviceChange(bool willBeFullScreen) { }
        public override void EndScreenDeviceChange(int clientWidth, int clientHeight) { }
        protected internal override void Initialize(GameContext gameContext) { }
        internal override void Run() { }
        internal override void Resize(int width, int height) { }
        protected internal override void SetSupportedOrientations(DisplayOrientation orientations) { }
        protected override void SetTitle(string title) { }

        public void RaiseClientSizeChanged() => OnClientSizeChanged(this, EventArgs.Empty);
    }

    [Fact]
    public void EmptyClientBoundsSkipsClientSizeChanged()
    {
        var window = new TestGameWindow
        {
            Bounds = Rectangle.Empty,
            PreferredWindowedSize = new Int2(640, 480),
        };
        var clientSizeChanged = false;
        window.ClientSizeChanged += (sender, e) => clientSizeChanged = true;

        window.RaiseClientSizeChanged();

        Assert.False(clientSizeChanged);
        Assert.Equal(new Int2(640, 480), window.PreferredWindowedSize);
    }

    [Fact]
    public void ValidClientBoundsUpdatesPreferredWindowedSize()
    {
        var window = new TestGameWindow
        {
            Bounds = new Rectangle(0, 0, 800, 600),
            PreferredWindowedSize = new Int2(640, 480),
        };
        var clientSizeChanged = false;
        window.ClientSizeChanged += (sender, e) => clientSizeChanged = true;

        window.RaiseClientSizeChanged();

        Assert.True(clientSizeChanged);
        Assert.Equal(new Int2(800, 600), window.PreferredWindowedSize);
    }

    [Fact]
    public void FullscreenKeepsPreferredWindowedSize()
    {
        var window = new TestGameWindow
        {
            Bounds = new Rectangle(0, 0, 1920, 1080),
            PreferredWindowedSize = new Int2(640, 480),
            IsFullscreen = true,
        };
        var clientSizeChanged = false;
        window.ClientSizeChanged += (sender, e) => clientSizeChanged = true;

        window.RaiseClientSizeChanged();

        Assert.True(clientSizeChanged);
        Assert.Equal(new Int2(640, 480), window.PreferredWindowedSize);
    }
}

/// <summary>
/// Minimizes and restores a real window, checking that the minimized state never produces
/// a valid-looking render size and that the window comes back at its original size.
/// </summary>
public class GameWindowMinimizeTest : GameTestBase
{
    [SkippableTheory]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void MinimizeRestore(AppContextType contextType)
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

            Assert.Equal(new Int2(640, 480), ClientSize(window));
            var preferredWindowedSize = window.PreferredWindowedSize;
            var hwnd = window.NativeWindow.Handle;

            ShowWindow(hwnd, SW_MINIMIZE);
            PumpUntil(messageLoop, () => window.IsMinimized);

            Assert.True(window.IsMinimized);
            Assert.True(window.ClientBounds.IsEmpty);
            Assert.Equal(preferredWindowedSize, window.PreferredWindowedSize);

            // With the backbuffer size driven by the window, drawing must be skipped while minimized
            windowRenderer.PreferredBackBufferWidth = 0;
            windowRenderer.PreferredBackBufferHeight = 0;
            Assert.False(windowRenderer.BeginDraw());

            ShowWindow(hwnd, SW_RESTORE);
            PumpUntil(messageLoop, () => !window.IsMinimized && window.ClientBounds.Width > 0);

            Assert.False(window.IsMinimized);
            Assert.Equal(new Int2(640, 480), ClientSize(window));
            Assert.Equal(preferredWindowedSize, window.PreferredWindowedSize);

            Assert.True(windowRenderer.BeginDraw());
            game.GraphicsContext.CommandList.Clear(windowRenderer.Presenter.BackBuffer, Color.Green);
            windowRenderer.EndDraw();

            windowRenderer.Dispose();
        });
    }

    /// <summary>
    /// Same scenario on the main game window, exercising the <see cref="GraphicsDeviceManager"/>
    /// resize path: minimizing must not corrupt the preferred windowed size or shrink the
    /// backbuffer, and the window must come back at its original size.
    /// </summary>
    [SkippableFact]
    public void MainWindowMinimizeRestore()
    {
        Skip.If(Platform.Type != PlatformType.Windows, reason: "Programmatic minimize/restore needs a real window manager; this test drives it through Win32 ShowWindow");

        Exception failure = null;
        var game = new GameWindowMinimizeTest();
        game.Script.AddTask(async () =>
        {
            try
            {
                var window = game.Window;

                // Let the window and device reach a steady state
                for (int i = 0; i < 5; i++)
                    await game.Script.NextFrame();

                var initialSize = ClientSize(window);
                Assert.True(initialSize.X > 1 && initialSize.Y > 1);
                var preferredWindowedSize = window.PreferredWindowedSize;
                var backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
                var initialBackBufferSize = new Int2(backBuffer.Width, backBuffer.Height);
                var hwnd = window.NativeWindow.Handle;

                ShowWindow(hwnd, SW_MINIMIZE);
                await WaitUntil(game, () => window.IsMinimized);

                Assert.True(window.IsMinimized);
                Assert.True(window.ClientBounds.IsEmpty);
                Assert.Equal(preferredWindowedSize, window.PreferredWindowedSize);

                ShowWindow(hwnd, SW_RESTORE);
                await WaitUntil(game, () => !window.IsMinimized && window.ClientBounds.Width > 0);

                // Let any pending device changes apply
                for (int i = 0; i < 5; i++)
                    await game.Script.NextFrame();

                Assert.False(window.IsMinimized);
                Assert.Equal(initialSize, ClientSize(window));
                Assert.Equal(preferredWindowedSize, window.PreferredWindowedSize);
                backBuffer = game.GraphicsDevice.Presenter.BackBuffer;
                Assert.Equal(initialBackBufferSize, new Int2(backBuffer.Width, backBuffer.Height));
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

        // Run directly through GameTester: with screenshot automation off it creates a real window
        GameTester.RunGameTest(game);

        if (failure != null)
            ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private static Int2 ClientSize(GameWindow window)
    {
        var bounds = window.ClientBounds;
        return new Int2(bounds.Width, bounds.Height);
    }

    private static async Task WaitUntil(GameTestBase game, Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(10))
            await game.Script.NextFrame();
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
