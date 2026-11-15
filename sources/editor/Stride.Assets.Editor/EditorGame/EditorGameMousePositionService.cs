// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Assets.Editor.AssetEditors.GameEditor.Services;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Gives the editors the point of the scene under the pointer of the view of their game, in both editors: an asset dropped
/// on the view of the scene editor lands there, an entity created from the menu of the view lands where the menu opened.
/// </summary>
/// <remarks>
/// Each editor's viewport fills the pointer position (<see cref="IEditorGameViewport.PointerPosition"/>,
/// <see cref="IEditorGameViewport.LastRightClickPosition"/>) from its view.
/// </remarks>
[EditorGameService(typeof(IEditorGameController), Order = 10)]
public sealed class EditorGameMousePositionService : EditorGameServiceBase, IEditorGameMousePositionViewModelService
{
    private readonly IEditorGameViewport viewport;
    private volatile EditorServiceGame? game;

    /// <summary>
    /// Initializes a new instance of the <see cref="EditorGameMousePositionService"/> class.
    /// </summary>
    /// <param name="controller">The controller of the game.</param>
    public EditorGameMousePositionService(IEditorGameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        viewport = controller.Viewport;
    }

    /// <inheritdoc/>
    public Vector3 GetMousePositionInScene(bool lastRightClick)
    {
        if (game is not { } editorGame)
            return Vector3.Zero;

        var position = lastRightClick ? viewport.LastRightClickPosition : viewport.PointerPosition;
        return position is { } fraction ? editorGame.GetPositionInScene(fraction) : Vector3.Zero;
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = editorGame;
        return Task.FromResult(true);
    }
}
