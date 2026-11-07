// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class SolutionExplorerView : UserControl
{
    private SessionViewModel? session;

    public SolutionExplorerView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (session is not null)
            session.AssetCollection.SelectedLocations.CollectionChanged -= SelectedLocationsChanged;
        session = DataContext as SessionViewModel;
        if (session is not null)
        {
            session.AssetCollection.SelectedLocations.CollectionChanged += SelectedLocationsChanged;
            Dispatcher.UIThread.Post(ExpandToSelection, DispatcherPriority.Background);
        }
    }

    private void SelectedLocationsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Once the tree has the new selection
        Dispatcher.UIThread.Post(ExpandToSelection, DispatcherPriority.Background);
    }

    /// <summary>
    /// Expands the folders, packages and categories that contain the selected locations, as the WPF solution explorer does when
    /// the asset view selects an asset (e.g. the editors that open with the session).
    /// </summary>
    private void ExpandToSelection()
    {
        if (session is null)
            return;

        foreach (var location in session.AssetCollection.SelectedLocations.ToList())
        {
            var ancestors = new List<IChildViewModel>();
            for (var parent = (location as IChildViewModel)?.GetParent(); parent is not null; parent = parent.GetParent())
                ancestors.Insert(0, parent);

            ItemsControl owner = SolutionTree;
            foreach (var ancestor in ancestors)
            {
                if (owner.ContainerFromItem(ancestor) is not TreeViewItem container)
                    continue;

                if (!container.IsExpanded)
                {
                    container.IsExpanded = true;
                    // The children get their containers
                    container.UpdateLayout();
                }
                owner = container;
            }
            (owner.ContainerFromItem(location) as Control)?.BringIntoView();
        }
    }
}
