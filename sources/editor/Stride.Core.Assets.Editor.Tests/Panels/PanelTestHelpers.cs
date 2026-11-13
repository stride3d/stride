// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Panels;

namespace Stride.Core.Assets.Editor.Tests.Panels;

/// <summary>
/// Panels for the tests: the 8 panels of the editor, without view models (the tests that need one make their own).
/// </summary>
internal static class PanelTestHelpers
{
    public static readonly string[] BuiltInIds =
    [
        BuiltInPanels.SolutionExplorer, BuiltInPanels.AssetView, BuiltInPanels.References, BuiltInPanels.AssetPreview,
        BuiltInPanels.PropertyGrid, BuiltInPanels.ActionHistory, BuiltInPanels.AssetLog, BuiltInPanels.BuildLog,
    ];

    public static PanelRegistry CreateBuiltInRegistry()
    {
        var registry = new PanelRegistry();
        foreach (var descriptor in BuiltInPanels.CreateSessionPanels())
            registry.Register(descriptor);
        registry.Register(BuiltInPanels.CreateOutputPanel(new Components.Build.BuildAndRunViewModel(Stride.Core.Presentation.ViewModels.ViewModelServiceProvider.NullServiceProvider)));
        registry.Register(BuiltInPanels.CreatePreviewPanel(_ => throw new NotSupportedException()));
        return registry;
    }

    public static PanelDescriptor Panel(string id, PanelPlacement placement = PanelPlacement.Right, string? group = null, string? menuGroup = null, int order = 1000, bool multiple = false, bool visible = true)
    {
        return new PanelDescriptor
        {
            Id = id,
            Title = () => id,
            DefaultPlacement = placement,
            DefaultGroup = group,
            MenuGroup = menuGroup,
            Order = order,
            AllowsMultipleInstances = multiple,
            VisibleByDefault = visible,
            CreateViewModel = _ => throw new NotSupportedException(),
        };
    }

    public static List<string> Ids(PanelGroup? group) => group?.Panels.Select(x => x.ToString()).ToList() ?? [];
}
