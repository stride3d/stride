// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Services;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Games;
using Stride.Graphics;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// The viewport of a game that renders offscreen into an <see cref="OffscreenGameView"/>: the back buffer follows the
/// size of the view, each drawn frame is handed to the view (through the compositor's GPU interop when it has one, by
/// readback otherwise), and the input of the view reaches the game.
/// </summary>
public sealed class OffscreenGameViewport : IEditorGameViewport
{
    private readonly Logger logger;
    private readonly AvaloniaInputBridge inputBridge;
    private OffscreenFrameExporter? exporter;
    private bool frameExported;

    /// <summary>
    /// Creates the viewport and its view, on the UI thread of <paramref name="dispatcher"/>.
    /// </summary>
    /// <param name="dispatcher">The dispatcher of the UI thread.</param>
    /// <param name="logger">The logger of the game.</param>
    public OffscreenGameViewport(IDispatcherService dispatcher, Logger logger)
    {
        this.logger = logger;
        (View, inputBridge) = dispatcher.Invoke(() =>
        {
            var view = new OffscreenGameView();
            return (view, new AvaloniaInputBridge(view));
        });
    }

    /// <summary>
    /// Whether the viewports read their frames back through the CPU even when the compositor offers a GPU interop
    /// (<c>--viewport-readback</c> on the command line).
    /// </summary>
    public static bool ForceReadback { get; set; }

    /// <summary>
    /// Gets the control that shows the game.
    /// </summary>
    public OffscreenGameView View { get; }

    /// <inheritdoc />
    object IEditorGameViewport.View => View;

    /// <inheritdoc />
    public GameContext Attach(EmbeddedGame game)
    {
        // The presenter is created before the device, so that a Vulkan device enables the export extensions; it falls
        // back to readback by itself
        exporter = new OffscreenFrameExporter(View)
        {
            GpuPresenter = ForceReadback ? null : GpuFramePresenter.Create(View, logger),
        };
        if (ForceReadback)
            logger.Info("Viewport: the frames are read back through the CPU, as the command line asks (--viewport-readback).");

        game.Host = this;
        // A game renders an unfocused window like a minimized one, 15 frames a second, and the window-less game is never
        // focused: a shown game renders at most 60 frames a second; a hidden editor game is still throttled
        game.TreatNotFocusedLikeMinimized = false;
        game.WindowMinimumUpdateRate.SetMaxFrequency(60);
        UseCompositorAdapter(game);

        // No window: the game renders into an offscreen back buffer that the view shows
        return new GameContextHeadless(64, 64) { InitializeDatabase = false };
    }

    /// <inheritdoc />
    public void Initialize(EmbeddedGame game)
    {
        // A window-less game counts as hidden, and a hidden game renders 15 frames a second: the view shows it
        game.Window.Visible = true;
    }

    /// <inheritdoc />
    public void BeginDraw(EmbeddedGame game)
    {
        // The input the view received since the last frame, before the input manager updates
        inputBridge.Apply(game.Input);
        // Between two frames: the back buffer follows the view, while it shows
        if (game is not EditorServiceGame { IsEditorHidden: true })
            exporter?.ApplyRequestedSize(game.GraphicsDeviceManager);
    }

    /// <inheritdoc />
    public void EndDraw(EmbeddedGame game, bool present)
    {
        // Only a frame that was drawn is shown (present is false when the game skipped the draw)
        frameExported = present && exporter != null && game.GraphicsDevice?.Presenter?.BackBuffer != null;
        if (frameExported)
            exporter!.EndFrame(game.GraphicsContext.CommandList, game.GraphicsDevice!.Presenter.BackBuffer);
    }

    /// <inheritdoc />
    public void AfterEndDraw(EmbeddedGame game, bool present)
    {
        if (frameExported)
            exporter?.AfterSubmit();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        exporter?.Dispose();
        exporter = null;
    }

    /// <summary>
    /// Makes the game render on the GPU of the editor's compositor, so that it can share its frames.
    /// </summary>
    private void UseCompositorAdapter(EmbeddedGame game)
    {
        var uid = GpuFramePresenter.TryGetCompositorAdapterUid(logger);
        if (uid is { } adapterUid && GraphicsAdapterFactory.Adapters.ToArray().Any(x => x.AdapterUid == adapterUid))
            game.GraphicsDeviceManager.RequiredAdapterUid = adapterUid.ToString();
    }
}
