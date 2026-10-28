// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Storage;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Engine;
using Stride.Games;
using Stride.Graphics;
using AvaloniaPixelFormat = Avalonia.Platform.PixelFormat;

namespace Stride.Editor.Avalonia.Tests.Engine;

public sealed class OffscreenGameViewportTests
{
    [AvaloniaFact]
    public void AttachMakesTheViewportTheHostOfAWindowLessGame()
    {
        var viewport = new OffscreenGameViewport(new DispatcherService(Dispatcher.UIThread), new LoggerResult());
        using var game = new EmbeddedGame();

        var context = AttachOnAnotherThread(viewport, game);

        Assert.Same(viewport, game.Host);
        Assert.IsType<GameContextHeadless>(context);
        // The game is never focused: it must not render like a minimized one
        Assert.False(game.TreatNotFocusedLikeMinimized);
        viewport.Dispose();
    }

    [AvaloniaFact]
    public void AGameRunningOnTheViewportShowsItsFramesInTheViewAtItsSize()
    {
        SkipWithoutGraphicsDevice();
        var viewport = new OffscreenGameViewport(new DispatcherService(Dispatcher.UIThread), new LoggerResult());
        var game = new ClearingGame();
        var context = AttachOnAnotherThread(viewport, game);
        var window = new Window { Width = 40, Height = 24, Content = viewport.View };
        window.Show();
        Exception? runError = null;
        var gameThread = new Thread(() =>
        {
            try
            {
                game.Run(context);
            }
            catch (Exception e)
            {
                runError = e;
            }
        }) { IsBackground = true, Name = "Viewport test game" };
        gameThread.Start();

        uint pixel = 0;
        var viewSize = Size2.Zero;
        try
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(30);
            while (DateTime.UtcNow < deadline && runError is null && (pixel != 0xFFFF0000 || game.BackBufferSize != viewSize))
            {
                // Here the headless platform stops calling the animation frames of the view, which pick the published
                // frames up (OffscreenGameViewTests covers them): the test redraws the view itself
                viewport.View.InvalidateVisual();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                Dispatcher.UIThread.RunJobs();
                viewSize = new Size2(viewport.View.PixelSize.Width, viewport.View.PixelSize.Height);
                pixel = ReadPixel(window, 20, 12);
                Thread.Sleep(10);
            }
        }
        finally
        {
            game.Exit();
            gameThread.Join(TimeSpan.FromSeconds(30));
            game.Dispose();
            viewport.Dispose();
        }

        Assert.Null(runError);
        // The back buffer follows the size of the view, and the view shows what the game drew
        Assert.True(viewSize is { Width: > 0, Height: > 0 });
        Assert.Equal(viewSize, game.BackBufferSize);
        Assert.Equal(0xFFFF0000u, pixel);
        Assert.True(game.Frames > 0);
    }

    private static GameContext AttachOnAnotherThread(OffscreenGameViewport viewport, EmbeddedGame game)
    {
        // The game thread attaches the game; it asks the UI thread about the compositor meanwhile
        var attach = Task.Run(() => viewport.Attach(game));
        while (!attach.IsCompleted)
            Dispatcher.UIThread.RunJobs();
        return attach.GetAwaiter().GetResult();
    }

    private static void SkipWithoutGraphicsDevice()
    {
        try
        {
            using var device = GraphicsDevice.New(DeviceCreationFlags.None, GraphicsProfile.Level_11_0);
        }
        catch (Exception e)
        {
            Assert.Skip($"No graphics device on this machine: {e.GetType().Name}: {e.Message}");
        }
    }

    /// <summary>
    /// Renders the window and reads a pixel of the frame, as 0xAARRGGBB.
    /// </summary>
    private static unsafe uint ReadPixel(Window window, int x, int y)
    {
        var frame = window.CaptureRenderedFrame();
        if (frame == null)
            return 0;
        using var buffer = frame.Lock();
        var pixel = *(uint*)(buffer.Address + y * buffer.RowBytes + x * 4);
        // The headless platform captures in either byte order
        return frame.Format == AvaloniaPixelFormat.Rgba8888 ? (pixel & 0xFF00FF00) | ((pixel & 0x00FF0000) >> 16) | ((pixel & 0x000000FF) << 16) : pixel;
    }

    /// <summary>
    /// A game that clears its back buffer in red.
    /// </summary>
    private sealed class ClearingGame : EmbeddedGame
    {
        private volatile int frames;

        public ClearingGame()
        {
            // The editor games share the database of the session; this one has an empty one of its own, which its effect
            // system requires
            var database = new ObjectDatabase($"/tmp/{nameof(OffscreenGameViewportTests)}/{Guid.NewGuid():N}", "index", loadDefaultBundle: false);
            var databaseService = (DatabaseFileProviderService)Services.GetSafeServiceAs<IDatabaseFileProviderService>();
            databaseService.FileProvider = new DatabaseFileProvider(database);
        }

        public int Frames => frames;

        public Size2 BackBufferSize { get; private set; }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            GraphicsContext.CommandList.Clear(backBuffer, Color.Red);
            BackBufferSize = new Size2(backBuffer.Width, backBuffer.Height);
            frames++;
        }
    }
}
