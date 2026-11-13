// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The default layouts, with the panels of the editor in the places the WPF editor gives them
/// (<c>GameStudioInternalSettings.DefaultLayout</c> and <c>DefaultEditorLayout</c>). The groups have identifiers that the
/// panels of plugins can name as their <see cref="PanelDescriptor.DefaultGroup"/>.
/// </summary>
public static class DefaultPanelLayouts
{
    /// <summary>
    /// The group on the left, in the layout without editors: the solution explorer.
    /// </summary>
    public const string LeftGroup = "Stride.Left";

    /// <summary>
    /// The group in the centre, in the layout without editors: the asset view.
    /// </summary>
    public const string CenterGroup = "Stride.Center";

    /// <summary>
    /// The group at the bottom, in the layout without editors: the references and the logs.
    /// </summary>
    public const string BottomGroup = "Stride.Bottom";

    /// <summary>
    /// The group on the left of the bottom, in the layout with editors: the solution explorer.
    /// </summary>
    public const string BottomLeftGroup = "Stride.BottomLeft";

    /// <summary>
    /// The group on the right of the bottom, in the layout with editors: the asset view and the logs.
    /// </summary>
    public const string BottomRightGroup = "Stride.BottomRight";

    /// <summary>
    /// The group at the top of the right column: the property grid.
    /// </summary>
    public const string RightTopGroup = "Stride.RightTop";

    /// <summary>
    /// The group at the bottom of the right column: the asset preview, the edit history (and the references, in the layout
    /// with editors).
    /// </summary>
    public const string RightBottomGroup = "Stride.RightBottom";

    /// <summary>
    /// The default layout of the given kind, with the panels of the editor only. <see cref="PanelLayoutResolver.Resolve"/>
    /// adds the other registered panels.
    /// </summary>
    public static PanelLayout Create(PanelLayoutKind kind)
    {
        return new PanelLayout { Root = kind == PanelLayoutKind.Normal ? CreateNormalRoot() : CreateEditorsRoot() };
    }

    // WPF: the solution explorer 300 px wide on the left of the asset view, the references and logs 200 px high below them,
    // and a column 400 px wide on the right with the property grid (2*) above the preview and the edit history (1*)
    private static PanelLayoutNode CreateNormalRoot()
    {
        return Split(PanelOrientation.Horizontal, [3, 1],
            Split(PanelOrientation.Vertical, [3, 1],
                Split(PanelOrientation.Horizontal, [1, 3],
                    Group(LeftGroup, null, BuiltInPanels.SolutionExplorer),
                    Group(CenterGroup, null, BuiltInPanels.AssetView)),
                Group(BottomGroup, BuiltInPanels.AssetLog, BuiltInPanels.References, BuiltInPanels.AssetLog, BuiltInPanels.BuildLog)),
            Split(PanelOrientation.Vertical, [2, 1],
                Group(RightTopGroup, null, BuiltInPanels.PropertyGrid),
                Group(RightBottomGroup, null, BuiltInPanels.AssetPreview, BuiltInPanels.ActionHistory)));
    }

    // WPF: the editors above a band 300 px high with the solution explorer (1*) and the asset view and logs (3*), and the
    // column on the right with the property grid (4*) above the preview, the edit history and the references (2*)
    private static PanelLayoutNode CreateEditorsRoot()
    {
        return Split(PanelOrientation.Horizontal, [3, 1],
            Split(PanelOrientation.Vertical, [2, 1],
                new PanelDocuments(),
                Split(PanelOrientation.Horizontal, [1, 3],
                    Group(BottomLeftGroup, null, BuiltInPanels.SolutionExplorer),
                    Group(BottomRightGroup, null, BuiltInPanels.AssetView, BuiltInPanels.AssetLog, BuiltInPanels.BuildLog))),
            Split(PanelOrientation.Vertical, [2, 1],
                Group(RightTopGroup, null, BuiltInPanels.PropertyGrid),
                Group(RightBottomGroup, BuiltInPanels.References, BuiltInPanels.AssetPreview, BuiltInPanels.ActionHistory, BuiltInPanels.References)));
    }

    private static PanelSplit Split(PanelOrientation orientation, double[] proportions, params PanelLayoutNode[] children)
    {
        return new PanelSplit { Orientation = orientation, Children = [.. children], Proportions = [.. proportions] };
    }

    private static PanelGroup Group(string id, string? active, params string[] panelIds)
    {
        return new PanelGroup { Id = id, Panels = panelIds.Select(x => new PanelRef(x)).ToList(), Active = active is null ? null : new PanelRef(active) };
    }
}
