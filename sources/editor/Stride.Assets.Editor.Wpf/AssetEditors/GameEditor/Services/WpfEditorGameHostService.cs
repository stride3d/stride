// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Controls;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.Engine;
using Stride.Assets.Editor.AssetEditors.GameEditor.Services;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Services;

/// <summary>
/// What the WPF hosting of an editor game gives, for the shared controllers: the game renders into a WinForms form
/// (<see cref="WpfEditorGameViewport"/>), shown in the view by <see cref="EditorHost"/>, with its cursor and the drags out
/// of it. The viewport gives the pointer position over it to the shared <c>EditorGameMousePositionService</c>.
/// </summary>
[EditorGameService(typeof(IEditorGameController), Order = 10)]
public sealed class WpfEditorGameHostService : EditorGameServiceBase, IEditorGameCursorViewModelService
{
    private readonly WpfEditorGameViewport viewport;

    /// <summary>
    /// Initializes a new instance of the <see cref="WpfEditorGameHostService"/> class.
    /// </summary>
    /// <param name="controller">The controller of the game, whose viewport must be a WPF one.</param>
    public WpfEditorGameHostService(IEditorGameController controller)
    {
        ArgumentNullException.ThrowIfNull(controller);
        viewport = controller.Viewport as WpfEditorGameViewport
            ?? throw new ArgumentException("The game of a WPF editor renders into a WPF viewport.", nameof(controller));
    }

    /// <summary>
    /// Gets the game, once this service is initialized (game thread, before the game runs).
    /// </summary>
    public EditorServiceGame? EditorGame { get; private set; }

    /// <summary>
    /// Gets the WPF element that shows the game (UI thread, once the game started).
    /// </summary>
    public GameEngineHost EditorHost => viewport.Host;

    /// <inheritdoc/>
    /// <remarks>The minimum drag distance of the system.</remarks>
    public Vector2 MinimumDragDistance => new((float)System.Windows.SystemParameters.MinimumHorizontalDragDistance, (float)System.Windows.SystemParameters.MinimumVerticalDragDistance);

    private EmbeddedGameForm? GameForm => viewport.Form;

    /// <inheritdoc/>
    public void ChangeCursor(EditorGameCursor cursor)
    {
        if (viewport.IsReleased || GameForm is not { } form)
            return;

        form.Cursor = cursor switch
        {
            EditorGameCursor.No => Cursors.No,
            EditorGameCursor.SizeAll => Cursors.SizeAll,
            EditorGameCursor.SizeNS => Cursors.SizeNS,
            EditorGameCursor.SizeWE => Cursors.SizeWE,
            EditorGameCursor.SizeNWSE => Cursors.SizeNWSE,
            EditorGameCursor.SizeNESW => Cursors.SizeNESW,
            _ => null,
        };
    }

    /// <summary>
    /// Drags <paramref name="items"/> out of the editor game (game thread). The WPF editor only: the UI editor's view
    /// starts the drag of an element held with Alt through it.
    /// </summary>
    /// <returns>Whether the items were dropped somewhere.</returns>
    public bool StartDragDrop(params object[] items)
    {
        if (viewport.IsReleased || GameForm is not { } form)
            return false;

        var data = new DragContainer(items);
        try
        {
            return form.DoDragDrop(new DataObject(DragContainer.Format, data), DragDropEffects.All) != DragDropEffects.None;
        }
        catch (COMException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        EditorGame = editorGame;
        return Task.FromResult(true);
    }
}
