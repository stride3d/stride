// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;
using Stride.GameStudio.Avalonia.Views.Panels;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// The content of the panels and editors of the layout, in the main window and in the floating windows: the view of each
/// built-in panel, with its view model as data context; the view the plugins registered for an editor; and, for a panel the
/// shell does not know, the view its plugin registered for this editor, a data template of the application, or "no view for
/// this editor".
/// </summary>
public sealed class DockViewLocator : IDataTemplate
{
    /// <inheritdoc />
    public bool Match(object? data) => data is PanelDockable or EditorDocument;

    /// <inheritdoc />
    public Control? Build(object? param)
    {
        if (param is not IDockableView dockable)
            return null;
        dockable.View ??= param switch
        {
            PanelDockable panel => BuildPanel(panel),
            EditorDocument document => BuildEditor(document),
            _ => null,
        };
        return dockable.View is { } view ? new DockableViewHost(dockable, view) : null;
    }

    private static Control BuildPanel(PanelDockable panel)
    {
        var viewModel = panel.ViewModel;
        Control view = viewModel switch
        {
            SolutionExplorerPanelViewModel x => new SolutionExplorerView { DataContext = x.Session },
            AssetViewPanelViewModel x => new AssetExplorerView { DataContext = x.Session.AssetCollection },
            // The properties follow the selection of the active editor or of the asset view
            PropertyGridPanelViewModel x => new PropertyGridView { [!Control.DataContextProperty] = new Binding(nameof(SessionViewModel.ActiveProperties)) { Source = x.Session } },
            ReferencesPanelViewModel x => new ReferencesView { DataContext = x },
            ActionHistoryPanelViewModel x => new ActionHistoryView { DataContext = x },
            AssetLogPanelViewModel x => new AssetLogView { DataContext = x },
            OutputPanelViewModel x => new OutputView { DataContext = x },
            AssetPreviewPanelViewModel x => new AssetPreviewView { DataContext = x },
            // A panel of a plugin: the view its plugin registered for this editor (see DockPanelService.CreatePluginView)
            _ => panel.CreatePluginView() ?? new ContentControl { Content = viewModel, DataContext = viewModel },
        };
        return view;
    }

    private static Control BuildEditor(EditorDocument document)
    {
        var viewType = document.Session.ServiceProvider.TryGet<IAssetsPluginService>()?.GetEditorViewType(document.Editor.GetType());
        if (viewType is not null && Activator.CreateInstance(viewType) is Control view)
        {
            view.DataContext = document.Editor;
            return view;
        }
        return new TextBlock
        {
            Text = document.Title,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.5,
        };
    }
}
