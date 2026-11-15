// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Graphics.Regression;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestHeadlessPresenter : GraphicTestGameBase
{
    /// <summary>
    /// A presenter without a window (the headless context of the tests and of the editor viewports) resizes its back
    /// buffer and its depth buffer in place: each depth buffer is released once, the old one at the resize and the
    /// last one when the presenter is destroyed at the end of the game.
    /// </summary>
    [SkippableFact]
    public void ResizingTwiceThenDisposingReleasesEachDepthBufferOnce()
    {
        var game = new TestHeadlessPresenter();
        game.FrameGameSystem.IsUnitTestFeeding = true;
        Texture initialDepthBuffer = null;
        Texture firstDepthBuffer = null;
        game.FrameGameSystem.Update(0, () =>
        {
            initialDepthBuffer = game.GraphicsDevice.Presenter.DepthStencilBuffer;
            game.pendingSizes.Enqueue((320, 200));
        });
        game.FrameGameSystem.Update(1, () =>
        {
            var presenter = game.GraphicsDevice.Presenter;
            firstDepthBuffer = presenter.DepthStencilBuffer;
            Assert.NotSame(initialDepthBuffer, firstDepthBuffer);
            Assert.True(initialDepthBuffer.IsDisposed);
            Assert.Equal(320, presenter.BackBuffer.Width);
            Assert.Equal(200, firstDepthBuffer.Height);
            game.pendingSizes.Enqueue((640, 400));
        });
        game.FrameGameSystem.Update(2, () =>
        {
            var presenter = game.GraphicsDevice.Presenter;
            Assert.NotSame(firstDepthBuffer, presenter.DepthStencilBuffer);
            Assert.True(firstDepthBuffer.IsDisposed);
            Assert.Equal(640, presenter.BackBuffer.Width);
            Assert.Equal(400, presenter.DepthStencilBuffer.Height);
        });
        game.FrameGameSystem.Draw(2, () => { });

        // The game disposes its device, hence the presenter, when it ends: a depth buffer released twice throws there
        RunGameTest(game);
    }

    private readonly Queue<(int Width, int Height)> pendingSizes = new();

    protected override bool BeginDraw()
    {
        // Between two frames, before the frame records anything, as the editor viewports resize: a frame that started
        // records its render targets in a command list that isn't submitted yet, and Direct3D 12 can't release them
        // under it
        if (pendingSizes.TryDequeue(out var size))
            Resize(this, size.Width, size.Height);
        return base.BeginDraw();
    }

    private static void Resize(GameTestBase game, int width, int height)
    {
        game.GraphicsDeviceManager.PreferredBackBufferWidth = width;
        game.GraphicsDeviceManager.PreferredBackBufferHeight = height;
        game.GraphicsDeviceManager.ApplyChanges();
    }
}
