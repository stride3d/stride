// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Tools;
using Stride.Assets.Editor.ViewModels;
using Stride.Core;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;

/// <summary>
/// The edge of the view of a scene or prefab editor that a widget docks on, over the game.
/// </summary>
public enum SceneViewEdge
{
    /// <summary>
    /// The top edge, the widgets from left to right.
    /// </summary>
    Top,

    /// <summary>
    /// The bottom edge, the widgets from left to right.
    /// </summary>
    Bottom,

    /// <summary>
    /// The left edge, the widgets from top to bottom.
    /// </summary>
    Left,

    /// <summary>
    /// The right edge, the widgets from top to bottom.
    /// </summary>
    Right,
}

/// <summary>
/// Describes a widget of the view of the scene and prefab editors: a view model that the plugin creates for each editor,
/// shown on an edge of the view, over the game, with the view that its toolkit companion registers for it
/// (<see cref="Core.Assets.Editor.Services.IEditorPluginContext.ViewTypes"/>). A plugin registers it with
/// <see cref="ScenePluginContextExtensions.SceneViewWidgets"/>; it goes when the plugin leaves, and its view models are destroyed.
/// </summary>
/// <remarks>
/// The user shows and hides each widget from the Widgets list of the view, and the editor remembers it per scene. The
/// editor creates the view model and the view through its contribution guard: when one of them throws, the widget is
/// turned off for the session and the error is logged with the plugin's name. The cross-platform Game Studio shows the
/// widgets; the WPF one accepts them and shows nothing.
/// </remarks>
public sealed class SceneViewWidgetDescriptor
{
    /// <summary>
    /// The identifier of the widget, "&lt;plugin&gt;.&lt;name&gt;". It must never change once released: the visibility of
    /// the widget is saved under it.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The title of the widget in the Widgets list.
    /// </summary>
    public required Func<string> Title { get; init; }

    /// <summary>
    /// The edge of the view the widget docks on.
    /// </summary>
    public SceneViewEdge Edge { get; init; } = SceneViewEdge.Top;

    /// <summary>
    /// The order of the widget among the widgets of its edge.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// Whether the widget shows in a scene where the user has not shown or hidden it yet.
    /// </summary>
    public bool VisibleByDefault { get; init; } = true;

    /// <summary>
    /// Creates the view model of the widget for an editor, the first time it shows; the editor destroys it when it closes or
    /// the plugin leaves.
    /// </summary>
    public required Func<SceneViewWidgetContext, IDestroyable> CreateViewModel { get; init; }

    /// <inheritdoc />
    public override string ToString() => $"{Id} ({Edge}, order {Order})";
}

/// <summary>
/// What a widget of the view of a scene or prefab editor works with.
/// </summary>
public sealed class SceneViewWidgetContext
{
    /// <summary>
    /// The descriptor of the widget.
    /// </summary>
    public required SceneViewWidgetDescriptor Descriptor { get; init; }

    /// <summary>
    /// The editor of the view: its scene or prefab, its entities and their selection (<c>SelectedItems</c>), its game.
    /// </summary>
    public required EntityHierarchyEditorViewModel Editor { get; init; }

    /// <summary>
    /// The open session.
    /// </summary>
    public SessionViewModel Session => Editor.Session;

    /// <summary>
    /// The tools of the view, once its game has started (null before, or without a game).
    /// </summary>
    public IEditorGameToolViewModelService? Tools => Editor.Controller?.GetService<IEditorGameToolViewModelService>();
}
