// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;

/// <summary>
/// What the active tool adds to the context menu of its view (<see cref="IEditorViewTool.GetContextMenu"/>), which a right
/// click without camera drag opens.
/// </summary>
public sealed class EditorViewContextMenu
{
    /// <summary>
    /// Gets the entries of the tool, before the commands of the plugins.
    /// </summary>
    public IReadOnlyList<EditorViewMenuEntry> Entries { get; init; } = [];

    /// <summary>
    /// Gets whether the entries of the tool replace the commands of the plugins (<c>SceneLocations.SceneViewContext</c>):
    /// with no entry, the menu does not open.
    /// </summary>
    public bool ReplacesOthers { get; init; }
}

/// <summary>
/// An entry of the context menu of a view, which the active tool gives (<see cref="EditorViewContextMenu"/>).
/// </summary>
public sealed class EditorViewMenuEntry
{
    /// <summary>
    /// Gets the title of the entry.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Gets the icon of the entry as path data in a 16 by 16 box, as <see cref="IEditorViewTool.IconPath"/>, or null.
    /// </summary>
    public string? IconPath { get; init; }

    /// <summary>
    /// Gets whether the entry can be chosen: a disabled entry shows greyed.
    /// </summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets what the entry does, on the game thread like the other members of the tool. When it throws, the tool is turned
    /// off.
    /// </summary>
    public required Action Execute { get; init; }
}

/// <summary>
/// A right click without camera drag in a view, which opens its context menu (<see cref="IEditorGameToolViewModelService.ContextMenuRequested"/>).
/// </summary>
public sealed class EditorViewContextMenuRequest
{
    /// <summary>
    /// Gets the click, with the entity under the cursor (a game-side entity: read it on the game thread only).
    /// </summary>
    public required EditorViewClick Click { get; init; }

    /// <summary>
    /// Gets the identifier of the entity under the cursor in the asset, to find its view model, or null.
    /// </summary>
    public AbsoluteId? EntityId { get; init; }

    /// <summary>
    /// Gets the active tool of the view.
    /// </summary>
    public required IEditorViewTool Tool { get; init; }

    /// <summary>
    /// Gets what the active tool adds to the menu, or null.
    /// </summary>
    public EditorViewContextMenu? ToolMenu { get; init; }
}
