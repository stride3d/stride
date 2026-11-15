// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Editor.Avalonia.EditorGame;

/// <summary>
/// Shows the cursors the editor games ask for over the Avalonia view of their game, and gives them the drag distance of
/// the platform.
/// </summary>
/// <remarks>
/// The games ask from their own thread: the cursor changes on the UI thread, and only the last one asked shows.
/// </remarks>
[EditorGameService(typeof(IEditorGameController), Order = 10)]
public sealed class AvaloniaEditorGameCursorService : EditorGameServiceBase, IEditorGameCursorViewModelService
{
    /// <summary>
    /// The drag distance until the view knows the platform's: that of the drags of the other Avalonia views of the editor.
    /// </summary>
    internal static readonly Vector2 DefaultDragDistance = new(4, 4);

    private readonly OffscreenGameView? view;
    private readonly OffscreenGameViewport? viewport;
    private readonly Lock dragDistanceLock = new();
    private Vector2 minimumDragDistance = DefaultDragDistance;
    private int requestedCursor;
    private int pendingUpdate;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaEditorGameCursorService"/> class.
    /// </summary>
    /// <param name="controller">The controller of the game. A game that renders elsewhere than in an Avalonia view shows no cursor.</param>
    public AvaloniaEditorGameCursorService(IEditorGameController controller)
        : this((controller ?? throw new ArgumentNullException(nameof(controller))).Viewport as OffscreenGameViewport)
    {
    }

    internal AvaloniaEditorGameCursorService(OffscreenGameViewport? viewport)
    {
        this.viewport = viewport;
        view = viewport?.View;
        if (view is not null)
            Dispatcher.UIThread.Post(WatchPlatformSettings);
    }

    /// <inheritdoc/>
    /// <remarks>The tap size of the mouse in the platform settings, once the view is shown.</remarks>
    public Vector2 MinimumDragDistance
    {
        get
        {
            lock (dragDistanceLock)
                return minimumDragDistance;
        }
    }

    /// <inheritdoc/>
    public void ChangeCursor(EditorGameCursor cursor)
    {
        if (view is null)
            return;

        Volatile.Write(ref requestedCursor, (int)cursor);
        // One update at a time: a burst of requests between two UI frames shows the last one
        if (Interlocked.Exchange(ref pendingUpdate, 1) == 0)
            Dispatcher.UIThread.Post(UpdateCursor);
    }

    /// <summary>
    /// Gets the Avalonia cursor shown for <paramref name="cursor"/>, or <c>null</c> for the default cursor of the view.
    /// </summary>
    internal static StandardCursorType? ToStandardCursor(EditorGameCursor cursor) => cursor switch
    {
        EditorGameCursor.No => StandardCursorType.No,
        EditorGameCursor.SizeAll => StandardCursorType.SizeAll,
        EditorGameCursor.SizeNS => StandardCursorType.SizeNorthSouth,
        EditorGameCursor.SizeWE => StandardCursorType.SizeWestEast,
        // The diagonal resize cursors of Avalonia are the corner ones
        EditorGameCursor.SizeNWSE => StandardCursorType.TopLeftCorner,
        EditorGameCursor.SizeNESW => StandardCursorType.TopRightCorner,
        _ => null,
    };

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame) => Task.FromResult(true);

    private void UpdateCursor()
    {
        Volatile.Write(ref pendingUpdate, 0);
        var type = ToStandardCursor((EditorGameCursor)Volatile.Read(ref requestedCursor));
        var cursor = type is { } standard ? new Cursor(standard) : null;
        // Every view of the game: the game works in the one the pointer is over
        foreach (var gameView in viewport?.Views ?? [view!])
            gameView.Cursor = cursor;
    }

    private void WatchPlatformSettings()
    {
        ReadDragDistance();
        view!.AttachedToVisualTree += (_, _) => ReadDragDistance();
    }

    private void ReadDragDistance()
    {
        if (view!.GetPlatformSettings() is not { } settings)
            return;

        var size = settings.GetTapSize(PointerType.Mouse);
        lock (dragDistanceLock)
            minimumDragDistance = new Vector2((float)size.Width, (float)size.Height);
    }
}
