// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.Regression;
using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// <see cref="GraphicsDeviceManager.ApplyChanges"/> with a new back buffer size from <see cref="GameBase.BeginRun"/> or
/// from an update: both run between the BeginDraw and the EndDraw of a frame, whose command list already uses the back
/// buffer and the depth buffer that the resize replaces.
/// </summary>
public class TestResizeInFrame : GraphicTestGameBase
{
    private const int NewWidth = 640;
    private const int NewHeight = 360;

    private bool resizeInBeginRun;

    public TestResizeInFrame()
    {
    }

    private TestResizeInFrame(AppContextType contextType)
    {
        ContextType = contextType;
        // Software Vulkan (llvmpipe) fails to present to a hidden window
        MakeWindowVisibleOnRun = contextType != AppContextType.Headless;
    }

    [SkippableTheory]
    [InlineData(AppContextType.Headless)]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void ApplyChangesInBeginRun(AppContextType contextType)
    {
        SkipIfNoWindow(contextType);

        var game = new TestResizeInFrame(contextType) { resizeInBeginRun = true };
        game.FrameGameSystem.IsUnitTestFeeding = true;
        game.FrameGameSystem.Draw(1, game.DrawAndCheckSize);
        game.FrameGameSystem.Draw(2, game.DrawAndCheckSize);
        RunGameTest(game);
    }

    [SkippableTheory]
    [InlineData(AppContextType.Headless)]
    [InlineData(AppContextType.DesktopWinForms)]
    [InlineData(AppContextType.DesktopSDL)]
    public void ApplyChangesInUpdate(AppContextType contextType)
    {
        SkipIfNoWindow(contextType);

        var game = new TestResizeInFrame(contextType);
        game.FrameGameSystem.IsUnitTestFeeding = true;
        game.FrameGameSystem.Update(1, game.ResizeAndCheckSize);
        game.FrameGameSystem.Draw(1, game.DrawAndCheckSize);
        game.FrameGameSystem.Draw(2, game.DrawAndCheckSize);
        RunGameTest(game);
    }

    protected override void BeginRun()
    {
        base.BeginRun();

        if (resizeInBeginRun)
            ResizeAndCheckSize();
    }

    private void ResizeAndCheckSize()
    {
        GraphicsDeviceManager.PreferredBackBufferWidth = NewWidth;
        GraphicsDeviceManager.PreferredBackBufferHeight = NewHeight;
        GraphicsDeviceManager.ApplyChanges();

        CheckSize();
    }

    private void DrawAndCheckSize()
    {
        var presenter = GraphicsDevice.Presenter;
        GraphicsContext.CommandList.Clear(presenter.BackBuffer, Color.CornflowerBlue);
        GraphicsContext.CommandList.Clear(presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);

        CheckSize();
    }

    private void CheckSize()
    {
        // A Vulkan swap chain takes the size of its window, so it is only re-created there, at the same size
        var expectedSize = GraphicsDevice.Platform == GraphicsPlatform.Vulkan && ContextType != AppContextType.Headless
            ? new Int2(Window.ClientBounds.Width, Window.ClientBounds.Height)
            : new Int2(NewWidth, NewHeight);

        var presenter = GraphicsDevice.Presenter;
        Assert.Equal(expectedSize, new Int2(presenter.BackBuffer.Width, presenter.BackBuffer.Height));
        Assert.Equal(expectedSize, new Int2(presenter.DepthStencilBuffer.Width, presenter.DepthStencilBuffer.Height));
    }

    private static void SkipIfNoWindow(AppContextType contextType)
    {
        Skip.If(contextType == AppContextType.DesktopWinForms && Platform.Type != PlatformType.Windows, reason: "WinForms is only available on Windows");
        Skip.If(contextType == AppContextType.DesktopSDL && Platform.Type is PlatformType.macOS or PlatformType.Android or PlatformType.iOS,
            reason: "SDL windows need the main thread on macOS, and desktop windows don't exist on mobile");
    }
}
