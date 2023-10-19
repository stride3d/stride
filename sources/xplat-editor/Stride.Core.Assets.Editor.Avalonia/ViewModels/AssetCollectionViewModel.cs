// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.ViewModels;

public sealed class AssetCollectionViewModel : ViewModelBase
{
    private readonly ObservableSet<AssetViewModel> assets = [];
    private readonly ObservableSet<AssetViewModel> selectedAssets = [];
    private readonly ObservableSet<object> selectedContent = [];
    private object? singleSelectedContent;

    public AssetCollectionViewModel(SessionViewModel session)
    {
        Session = session;

        selectedContent.CollectionChanged += SelectedContentCollectionChanged;
    }

    public IReadOnlyObservableCollection<AssetViewModel> Assets => assets;

    /// <remarks>
    /// <see cref="SelectedAssets"/> is a sub-collection of <see cref="SelectedContent"/>.
    /// It should always be read from and never directly updated, except in <see cref="SelectedContentCollectionChanged"/>.
    /// </remarks>
    public IReadOnlyObservableCollection<AssetViewModel> SelectedAssets => selectedAssets;

    /// <summary>
    /// List of all selected items (e.g. in the asset view).
    /// </summary>
    public IReadOnlyObservableCollection<object> SelectedContent => selectedContent;

    public ObservableCollection<object> SelectedLocations { get; } = [];

    public SessionViewModel Session { get; }

    public object? SingleSelectedContent
    {
        get => singleSelectedContent;
        private set => SetValue(ref singleSelectedContent, value);
    }

    private void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateSingleSelectedContent();

        // Synchronize SelectedAssets collection
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            selectedAssets.Clear();
            selectedAssets.AddRange(SelectedContent.OfType<AssetViewModel>());
        }
        else
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems.OfType<AssetViewModel>())
                {
                    selectedAssets.Remove(item);
                }
            }

            if (e.NewItems != null)
            {
                selectedAssets.AddRange(e.NewItems.OfType<AssetViewModel>());
            }
        }
    }

    private void UpdateSingleSelectedContent()
    {
        SingleSelectedContent = SelectedContent.Count == 1 ? SelectedContent.First() : null;
    }
}
