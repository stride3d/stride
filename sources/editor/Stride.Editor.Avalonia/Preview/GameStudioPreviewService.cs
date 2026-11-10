// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.Preview;
using Stride.Games;

namespace Stride.Editor.Avalonia.Preview;

/// <summary>
/// The preview service of a session in the Avalonia editor: it runs the preview game offscreen, shown by an
/// <see cref="OffscreenGameView"/> (through the compositor's GPU interop when it has one, by readback otherwise).
/// </summary>
public sealed class GameStudioPreviewService : GameStudioPreviewServiceBase, IAvaloniaPreviewBuilder
{
    // The view the game renders in, created on the UI thread before the game starts
    private readonly OffscreenGameViewport viewport;

    public GameStudioPreviewService(ISessionViewModel session)
        : base(session)
    {
        viewport = new OffscreenGameViewport(Dispatcher, Logger);
        StartPreviewGame();
    }

    /// <inheritdoc />
    public Control? GetStrideView()
    {
        return !IsDisposed ? viewport.View : null;
    }

    /// <inheritdoc />
    protected override GameContext CreateGameContext(PreviewGame game)
    {
        // No window: the game renders into an offscreen back buffer that the viewport shows
        return viewport.Attach(game);
    }

    /// <inheritdoc />
    protected override void DisposeView()
    {
        viewport.Dispose();
    }
}
