// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
        game.FrameGameSystem.Update(() =>
        {
            var presenter = game.GraphicsDevice.Presenter;
            var initialDepthBuffer = presenter.DepthStencilBuffer;

            Resize(game, 320, 200);
            var firstDepthBuffer = presenter.DepthStencilBuffer;
            Assert.NotSame(initialDepthBuffer, firstDepthBuffer);
            Assert.True(initialDepthBuffer.IsDisposed);
            Assert.Equal(320, presenter.BackBuffer.Width);
            Assert.Equal(200, firstDepthBuffer.Height);

            Resize(game, 640, 400);
            Assert.NotSame(firstDepthBuffer, presenter.DepthStencilBuffer);
            Assert.True(firstDepthBuffer.IsDisposed);
            Assert.Equal(640, presenter.BackBuffer.Width);
            Assert.Equal(400, presenter.DepthStencilBuffer.Height);
        });
        game.FrameGameSystem.Draw(() => { });

        // The game disposes its device, hence the presenter, when it ends: a depth buffer released twice throws there
        RunGameTest(game);
    }

    private static void Resize(GameTestBase game, int width, int height)
    {
        game.GraphicsDeviceManager.PreferredBackBufferWidth = width;
        game.GraphicsDeviceManager.PreferredBackBufferHeight = height;
        game.GraphicsDeviceManager.ApplyChanges();
    }
}
