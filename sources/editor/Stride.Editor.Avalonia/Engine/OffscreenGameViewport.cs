// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Games;
using Stride.Graphics;
using Avalonia.Input;
using AvaloniaPoint = Avalonia.Point;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// The viewport of a game that renders offscreen into an <see cref="OffscreenGameView"/>: the back buffer follows the
/// size of the view, each drawn frame is handed to the view (through the compositor's GPU interop when it has one, by
/// readback otherwise), and the input of the view reaches the game.
/// </summary>
/// <remarks>
/// The viewport can show other views of the same game (<see cref="CreateView"/>, prototype of several views of one scene
/// editor, see <see cref="IEditorGameMultiViewport"/>): the active view gets the back buffer and the pointer input, the
/// others get the frames the game draws for them.
/// </remarks>
public sealed class OffscreenGameViewport : IEditorGameViewport, IEditorGameMultiViewport
{
    private readonly IDispatcherService dispatcher;
    private readonly Logger logger;
    private readonly ViewSlot primary;
    private readonly Lock slotsLock = new();
    // Copied on each change: the game thread reads it without a lock
    private ViewSlot[] slots;
    private readonly List<ViewSlot> removed = [];
    private SimulatedInputDevices? devices;
    private volatile int requestedActiveView = IEditorGameMultiViewport.PrimaryView;
    private volatile int activeView = IEditorGameMultiViewport.PrimaryView;

    /// <summary>
    /// Creates the viewport and its view, on the UI thread of <paramref name="dispatcher"/>.
    /// </summary>
    /// <param name="dispatcher">The dispatcher of the UI thread.</param>
    /// <param name="logger">The logger of the game.</param>
    public OffscreenGameViewport(IDispatcherService dispatcher, Logger logger)
    {
        this.dispatcher = dispatcher;
        this.logger = logger;
        primary = dispatcher.Invoke(() => CreateSlot(IEditorGameMultiViewport.PrimaryView));
        View = primary.View;
        slots = [primary];
    }

    /// <summary>
    /// Whether the viewports read their frames back through the CPU even when the compositor offers a GPU interop
    /// (<c>--viewport-readback</c> on the command line).
    /// </summary>
    public static bool ForceReadback { get; set; }

    /// <summary>
    /// Gets the control that shows the game: the primary view.
    /// </summary>
    public OffscreenGameView View { get; }

    /// <summary>
    /// Gets the views of the game, the primary view first (UI thread).
    /// </summary>
    public IReadOnlyList<OffscreenGameView> Views => Volatile.Read(ref slots).Select(x => x.View).ToList();

    /// <summary>
    /// Raised on the UI thread when a view is created or removed.
    /// </summary>
    public event EventHandler? ViewsChanged;

    /// <inheritdoc />
    object IEditorGameViewport.View => View;

    /// <inheritdoc />
    /// <remarks>The last move, press or drag of the pointer over the active view (<see cref="OffscreenGameView.LastPointerPosition"/>).</remarks>
    public Vector2? PointerPosition => ToFraction(ActiveSlot.View, ActiveSlot.View.LastPointerPosition);

    /// <inheritdoc />
    public Vector2? LastRightClickPosition => ToFraction(ActiveSlot.View, ActiveSlot.View.LastRightClickPosition);

    /// <inheritdoc />
    public int RequestedActiveView => requestedActiveView;

    /// <inheritdoc />
    public int ActiveView { get => activeView; set => activeView = value; }

    private ViewSlot ActiveSlot => FindSlot(activeView) ?? primary;

    /// <summary>
    /// Gets the view <paramref name="viewId"/>, created on the first call (UI thread).
    /// </summary>
    /// <param name="viewId">The identifier the user interface gives the view; <see cref="IEditorGameMultiViewport.PrimaryView"/> is <see cref="View"/>.</param>
    public OffscreenGameView CreateView(int viewId)
    {
        dispatcher.EnsureAccess();
        lock (slotsLock)
        {
            if (FindSlot(viewId) is { } existing)
                return existing.View;
            var slot = CreateSlot(viewId);
            slot.Exporter = new OffscreenFrameExporter(slot.View)
            {
                // The device of the game already enables the export extensions, if it has them
                GpuPresenter = ForceReadback ? null : GpuFramePresenter.Create(slot.View, logger),
            };
            slots = [.. slots, slot];
        }
        ViewsChanged?.Invoke(this, EventArgs.Empty);
        return FindSlot(viewId)!.View;
    }

    /// <summary>
    /// Removes the view <paramref name="viewId"/> (UI thread). The primary view stays.
    /// </summary>
    public void RemoveView(int viewId)
    {
        dispatcher.EnsureAccess();
        if (viewId == IEditorGameMultiViewport.PrimaryView)
            return;
        lock (slotsLock)
        {
            if (FindSlot(viewId) is not { } slot)
                return;
            slots = [.. slots.Where(x => x != slot)];
            // Its exporter goes on the game thread, between two frames
            removed.Add(slot);
        }
        if (requestedActiveView == viewId)
            requestedActiveView = IEditorGameMultiViewport.PrimaryView;
        ViewsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public GameContext Attach(EmbeddedGame game)
    {
        // The presenter is created before the device, so that a Vulkan device enables the export extensions; it falls
        // back to readback by itself
        primary.Exporter = new OffscreenFrameExporter(View)
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
        DisposeRemovedSlots();
        var current = Volatile.Read(ref slots);
        var active = ActiveSlot;
        // The input the views received since the last frame, before the input manager updates: the pointer of the active
        // view only
        devices ??= SimulatedInputDevices.Create(game.Input);
        foreach (var slot in current)
            slot.InputBridge.Apply(devices, acceptPointer: slot == active);
        // Between two frames: the back buffer follows the active view, while it shows
        if (game is not EditorServiceGame { IsEditorHidden: true })
            active.Exporter?.ApplyRequestedSize(game.GraphicsDeviceManager);
    }

    /// <inheritdoc />
    public void EndDraw(EmbeddedGame game, bool present)
    {
        // Only a frame that was drawn is shown (present is false when the game skipped the draw)
        var active = ActiveSlot;
        active.FrameExported = present && active.Exporter != null && game.GraphicsDevice?.Presenter?.BackBuffer != null;
        if (active.FrameExported)
            active.Exporter!.EndFrame(game.GraphicsContext.CommandList, game.GraphicsDevice!.Presenter.BackBuffer);
    }

    /// <inheritdoc />
    public void AfterEndDraw(EmbeddedGame game, bool present)
    {
        foreach (var slot in Volatile.Read(ref slots))
        {
            if (slot.FrameExported)
                slot.Exporter?.AfterSubmit();
            slot.FrameExported = false;
        }
    }

    /// <inheritdoc />
    public void GetViews(List<EditorGameViewInfo> views)
    {
        views.Clear();
        foreach (var slot in Volatile.Read(ref slots))
        {
            var size = slot.IsShown ? slot.Exporter?.RequestedSize ?? default : default;
            views.Add(new EditorGameViewInfo(slot.Id, size.Width, size.Height));
        }
    }

    /// <inheritdoc />
    public void PresentView(int viewId, CommandList commandList, Texture frame)
    {
        if (FindSlot(viewId) is not { Exporter: { } exporter } slot)
            return;
        exporter.EndFrame(commandList, frame);
        slot.FrameExported = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeRemovedSlots();
        foreach (var slot in Volatile.Read(ref slots))
        {
            slot.Exporter?.Dispose();
            slot.Exporter = null;
        }
    }

    private ViewSlot CreateSlot(int viewId)
    {
        var view = new OffscreenGameView();
        var slot = new ViewSlot(viewId, view, new AvaloniaInputBridge(view));
        // The view the pointer comes over becomes the active view; the game switches between two frames, once no button
        // is down
        view.PointerEntered += (_, e) => RequestActive(slot, e);
        view.PointerMoved += (_, e) => RequestActive(slot, e);
        view.PointerPressed += (_, _) => requestedActiveView = slot.Id;
        view.AddHandler(DragDrop.DragOverEvent, (_, _) => requestedActiveView = slot.Id, handledEventsToo: true);
        // A view out of the visual tree (a pane hidden by a maximized one) is not drawn
        view.AttachedToVisualTree += (_, _) => slot.IsShown = true;
        view.DetachedFromVisualTree += (_, _) => slot.IsShown = false;
        return slot;
    }

    private void RequestActive(ViewSlot slot, PointerEventArgs e)
    {
        var properties = e.GetCurrentPoint(slot.View).Properties;
        if (!properties.IsLeftButtonPressed && !properties.IsRightButtonPressed && !properties.IsMiddleButtonPressed)
            requestedActiveView = slot.Id;
    }

    private ViewSlot? FindSlot(int viewId)
    {
        foreach (var slot in Volatile.Read(ref slots))
        {
            if (slot.Id == viewId)
                return slot;
        }
        return null;
    }

    private void DisposeRemovedSlots()
    {
        ViewSlot[] toDispose;
        lock (slotsLock)
        {
            if (removed.Count == 0)
                return;
            toDispose = [.. removed];
            removed.Clear();
        }
        foreach (var slot in toDispose)
        {
            slot.Exporter?.Dispose();
            slot.Exporter = null;
        }
    }

    private static Vector2? ToFraction(OffscreenGameView view, AvaloniaPoint? position)
    {
        var size = view.Bounds.Size;
        if (position is not { } point || size.Width <= 0 || size.Height <= 0)
            return null;

        return new Vector2((float)(point.X / size.Width), (float)(point.Y / size.Height));
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

    /// <summary>
    /// One view of the game: its control, its input and its exporter.
    /// </summary>
    private sealed class ViewSlot(int id, OffscreenGameView view, AvaloniaInputBridge inputBridge)
    {
        public int Id { get; } = id;

        public OffscreenGameView View { get; } = view;

        public AvaloniaInputBridge InputBridge { get; } = inputBridge;

        public OffscreenFrameExporter? Exporter { get; set; }

        public bool FrameExported { get; set; }

        public volatile bool IsShown;
    }
}
