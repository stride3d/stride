// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// Describes a panel of the main window that is not an asset editor: the solution explorer, the asset view, the logs, or
/// a panel of a plugin. The editor's shell creates its view model with <see cref="CreateViewModel"/> and its view from
/// the view model type, through the views of its toolkit.
/// </summary>
public sealed class PanelDescriptor
{
    /// <summary>
    /// The identifier of the panel, saved in the layouts: "Stride.SolutionExplorer" for the panels of the editor, and
    /// "&lt;plugin&gt;.&lt;name&gt;" for the panels of a plugin. It must never change once released.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The title of the panel, read when it is shown so that it follows the language of the editor. A panel view model
    /// can change its own title (<see cref="IPanelViewModel.Title"/>) to add a count, for instance.
    /// </summary>
    public required Func<string> Title { get; init; }

    /// <summary>
    /// An image of the editor's toolkit, or null.
    /// </summary>
    public object? Icon { get; init; }

    /// <summary>
    /// Where the panel goes when a layout has no place for it, and no group with the identifier <see cref="DefaultGroup"/>.
    /// </summary>
    public PanelPlacement DefaultPlacement { get; init; } = PanelPlacement.Right;

    /// <summary>
    /// The identifier of the group the panel joins when a layout has no place for it ("Stride.RightBottom", for instance:
    /// see <see cref="DefaultPanelLayouts"/>), or null to go by <see cref="DefaultPlacement"/>.
    /// </summary>
    public string? DefaultGroup { get; init; }

    /// <summary>
    /// The order of the panel in its section of the View menu, and in the group it joins.
    /// </summary>
    public int Order { get; init; }

    /// <summary>
    /// The section of the View menu the panel is listed in: panels of the same section are listed together, between
    /// separators. Null for a section of its own.
    /// </summary>
    public string? MenuGroup { get; init; }

    /// <summary>
    /// Whether the panel is visible when a layout has no place for it yet.
    /// </summary>
    public bool VisibleByDefault { get; init; } = true;

    /// <summary>
    /// Whether the panel can be opened several times: the View menu then has a "New" entry for it, and closing one of its
    /// instances closes it, where closing a single panel only hides it.
    /// </summary>
    public bool AllowsMultipleInstances { get; init; }

    /// <summary>
    /// Creates the view model of the panel, or of one of its instances.
    /// </summary>
    public required Func<PanelContext, IPanelViewModel> CreateViewModel { get; init; }

    /// <inheritdoc />
    public override string ToString() => Id;
}
