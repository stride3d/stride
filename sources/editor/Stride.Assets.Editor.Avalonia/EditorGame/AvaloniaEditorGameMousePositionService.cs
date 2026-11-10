// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Engine;
using Stride.Editor.EditorGame.Game;

namespace Stride.Assets.Editor.Avalonia.EditorGame;

/// <summary>
/// Gives the editors the point of the scene under the pointer of the Avalonia view of their game, as the WPF
/// <c>WpfEditorGameHostService</c> does with the cursor of the WPF view: an asset dropped on the view of the scene editor
/// lands there.
/// </summary>
/// <remarks>
/// The view knows the pointer while it moves over it and while something is dragged over it
/// (<see cref="OffscreenGameView.LastPointerPosition"/>); the game itself gets no pointer move during a drag.
/// </remarks>
[EditorGameService(typeof(IEditorGameController), Order = 10)]
public sealed class AvaloniaEditorGameMousePositionService : EditorGameServiceBase, IEditorGameMousePositionViewModelService
{
    private readonly OffscreenGameView? view;
    private volatile EditorServiceGame? game;

    /// <summary>
    /// Initializes a new instance of the <see cref="AvaloniaEditorGameMousePositionService"/> class.
    /// </summary>
    /// <param name="controller">The controller of the game. A game that renders elsewhere than in an Avalonia view always gives the origin.</param>
    public AvaloniaEditorGameMousePositionService(IEditorGameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        view = (controller.Viewport as OffscreenGameViewport)?.View;
    }

    /// <inheritdoc/>
    public Vector3 GetMousePositionInScene(bool lastRightClick)
    {
        if (game is not { } editorGame || view is null)
            return Vector3.Zero;

        var position = lastRightClick ? view.LastRightClickPosition : view.LastPointerPosition;
        var size = view.Bounds.Size;
        if (position is not { } pointer || size.Width <= 0 || size.Height <= 0)
            return Vector3.Zero;

        return editorGame.GetPositionInScene(new Vector2((float)(pointer.X / size.Width), (float)(pointer.Y / size.Height)));
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = editorGame;
        return Task.FromResult(true);
    }
}
