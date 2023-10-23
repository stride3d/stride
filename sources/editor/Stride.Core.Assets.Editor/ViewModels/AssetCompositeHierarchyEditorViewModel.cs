// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Collections;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Base class for the view model of an <see cref="AssetCompositeHierarchyViewModel{TAssetPartDesign,TAssetPart}"/> editor.
/// </summary>
/// <typeparam name="TAssetPartDesign">The type of a part design.</typeparam>
/// <typeparam name="TAssetPart">The type of a part.</typeparam>
/// <typeparam name="TAssetViewModel"></typeparam>
/// <typeparam name="TItemViewModel">The type of a real <see cref="AssetCompositeItemViewModel"/> that can be copied/cut/pasted.</typeparam>
public abstract class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>
    : AssetCompositeEditorViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
    where TAssetViewModel : AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>
    where TItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel>
{
    private TItemViewModel rootPart;
    private bool updateSelectionGuard;

    protected AssetCompositeHierarchyEditorViewModel(TAssetViewModel asset)
        : base(asset)
    {
        rootPart = CreateRootPartViewModel();

        SelectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedItems.CollectionChanged += SelectedItemsCollectionChanged;
    }

    public TItemViewModel RootPart { get => rootPart; private set => SetValue(ref rootPart, value); }

    public ObservableSet<TItemViewModel> SelectedItems { get; } = new ObservableSet<TItemViewModel>();

    protected abstract TItemViewModel CreateRootPartViewModel();

    protected abstract Task RefreshEditorProperties();

    /// <summary>
    /// Called when the content of <see cref="AssetCompositeEditorViewModel.SelectedContent"/> changed.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <remarks>
    /// Default implementation populates <see cref="SelectedItems"/> by filtering elements of type <typeparamref name="TItemViewModel"/>.
    /// </remarks>
    protected virtual void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedItems.Clear();
        SelectedItems.AddRange(SelectedContent.OfType<TItemViewModel>());
    }

    /// <summary>
    /// Called when the content of <see cref="SelectedItems"/> changed.
    /// </summary>
    /// <param name="action">The action that caused the event.</param>
    /// <remarks>
    /// Default implementation populates <see cref="AssetCompositeEditorViewModel{,}.SelectedContent"/> with the same elements.
    /// </remarks>
    protected virtual void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedContent.Clear();
        SelectedContent.AddRange(SelectedItems);
    }

    private async void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedContentCollectionChanged(args.Action);
            // Refresh the property grid
            await RefreshEditorProperties();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }

    private async void SelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedItemsCollectionChanged(args.Action);
            // Refresh the property grid
            await RefreshEditorProperties();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }
}
