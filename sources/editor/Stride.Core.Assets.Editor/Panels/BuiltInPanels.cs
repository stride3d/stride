// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Components.Build;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.Panels;

/// <summary>
/// The panels of the editor: the 8 panels of the WPF editor, with its <c>ContentId</c>s under a "Stride." prefix, and in the
/// same sections of the View menu.
/// </summary>
public static class BuiltInPanels
{
    public const string SolutionExplorer = "Stride.SolutionExplorer";
    public const string AssetView = "Stride.AssetView";
    public const string References = "Stride.References";
    public const string AssetPreview = "Stride.AssetPreview";
    public const string PropertyGrid = "Stride.PropertyGrid";
    public const string ActionHistory = "Stride.ActionHistory";
    public const string AssetLog = "Stride.AssetLog";
    public const string BuildLog = "Stride.BuildLog";

    /// <summary>
    /// The sections of the View menu, as in WPF: the explorers, the preview, the editing panels and the logs.
    /// </summary>
    public const string ExploreMenuGroup = "Stride.Explore";
    public const string PreviewMenuGroup = "Stride.Preview";
    public const string EditMenuGroup = "Stride.Edit";
    public const string LogsMenuGroup = "Stride.Logs";

    /// <summary>
    /// The panels that show the session: the solution explorer, the asset view, the references, the property grid, the edit
    /// history and the asset errors. The shell registers them with <see cref="CreateOutputPanel"/> and its asset preview.
    /// </summary>
    public static IReadOnlyList<PanelDescriptor> CreateSessionPanels()
    {
        return
        [
            new PanelDescriptor
            {
                Id = SolutionExplorer,
                Title = () => Tr._p("Panel", "Solution explorer"),
                MenuGroup = ExploreMenuGroup,
                Order = 100,
                DefaultPlacement = PanelPlacement.Left,
                CreateViewModel = context => new SolutionExplorerPanelViewModel(context),
            },
            new PanelDescriptor
            {
                Id = AssetView,
                Title = () => Tr._p("Panel", "Asset view"),
                MenuGroup = ExploreMenuGroup,
                Order = 110,
                DefaultPlacement = PanelPlacement.Document,
                CreateViewModel = context => new AssetViewPanelViewModel(context),
            },
            new PanelDescriptor
            {
                Id = References,
                Title = () => Tr._p("Panel", "References"),
                MenuGroup = ExploreMenuGroup,
                Order = 120,
                DefaultPlacement = PanelPlacement.Bottom,
                CreateViewModel = context => new ReferencesPanelViewModel(context),
            },
            new PanelDescriptor
            {
                Id = PropertyGrid,
                Title = () => Tr._p("Panel", "Property grid"),
                MenuGroup = EditMenuGroup,
                Order = 300,
                DefaultPlacement = PanelPlacement.Right,
                CreateViewModel = context => new PropertyGridPanelViewModel(context),
            },
            new PanelDescriptor
            {
                Id = ActionHistory,
                Title = () => Tr._p("Panel", "Edit history"),
                MenuGroup = EditMenuGroup,
                Order = 310,
                DefaultPlacement = PanelPlacement.Right,
                CreateViewModel = context => new ActionHistoryPanelViewModel(context),
            },
            new PanelDescriptor
            {
                Id = AssetLog,
                Title = () => Tr._p("Panel", "Asset errors"),
                MenuGroup = LogsMenuGroup,
                Order = 400,
                DefaultPlacement = PanelPlacement.Bottom,
                CreateViewModel = context => new AssetLogPanelViewModel(context),
            },
        ];
    }

    /// <summary>
    /// The Output: the log of the builds and of the games started from the editor.
    /// </summary>
    /// <param name="build">The builds of the editor's shell.</param>
    public static PanelDescriptor CreateOutputPanel(BuildAndRunViewModel build)
    {
        ArgumentNullException.ThrowIfNull(build);
        return new PanelDescriptor
        {
            Id = BuildLog,
            Title = () => Tr._p("Panel", "Output"),
            MenuGroup = LogsMenuGroup,
            Order = 410,
            DefaultPlacement = PanelPlacement.Bottom,
            CreateViewModel = context => new OutputPanelViewModel(context, build),
        };
    }

    /// <summary>
    /// The asset preview, whose view model the shell makes: the preview game is the toolkit's.
    /// </summary>
    public static PanelDescriptor CreatePreviewPanel(Func<PanelContext, IPanelViewModel> createViewModel)
    {
        ArgumentNullException.ThrowIfNull(createViewModel);
        return new PanelDescriptor
        {
            Id = AssetPreview,
            Title = () => Tr._p("Panel", "Asset preview"),
            MenuGroup = PreviewMenuGroup,
            Order = 200,
            DefaultPlacement = PanelPlacement.Right,
            CreateViewModel = createViewModel,
        };
    }
}
