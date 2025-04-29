// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// Base class for the view model of an <see cref="AssetCompositeHierarchyViewModel{TAssetPartDesign,TAssetPart}"/> editor.
/// </summary>
/// <typeparam name="TAssetPartDesign">The type of a part design.</typeparam>
/// <typeparam name="TAssetPart">The type of a part.</typeparam>
/// <typeparam name="TAssetViewModel"></typeparam>
/// <typeparam name="TItemViewModel">The type of a real <see cref="AssetCompositeItemViewModel"/> that can be copied/cut/pasted.</typeparam>
public abstract partial class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>
    : AssetCompositeEditorViewModel<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>, TAssetViewModel>
    where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    where TAssetPart : class, IIdentifiable
    where TAssetViewModel : AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>
    where TItemViewModel : AssetCompositeItemViewModel, IPartDesignViewModel<TAssetPartDesign, TAssetPart>, IAssetPartViewModel
{
    private bool updateSelectionGuard;

    protected AssetCompositeHierarchyEditorViewModel(TAssetViewModel asset)
        : base(asset)
    {
        CopyCommand = new AnonymousTaskCommand(ServiceProvider, Copy, CanCopy);
        CutCommand = new AnonymousTaskCommand(ServiceProvider, Cut, CanCut);
        DeleteCommand = new AnonymousTaskCommand(ServiceProvider, Delete, CanDelete);
        PasteCommand = new AnonymousTaskCommand<bool>(ServiceProvider, Paste, CanPaste);

        SelectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedItems.CollectionChanged += SelectedItemsCollectionChanged;
    }

    public required AssetCompositeItemViewModel RootPart { get; init; }

    public ObservableSet<TItemViewModel> SelectedItems { get; } = [];

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>));

        // FIXME xplat-editor
        //PasteAsRootMonitor.Destroy();
        //PasteMonitor.Destroy();

        // Unregister collection
        SelectedItems.CollectionChanged -= SelectedItemsCollectionChanged;
        SelectedContent.CollectionChanged -= SelectedContentCollectionChanged;

        // Clear the property grid if any of our items was selected.
        // TODO: this should be factorized with UI editor (at least) and with Sprite editor (ideally)
        //if (Session.ActiveProperties.Selection.OfType< AssetCompositeItemViewModel>().Any(x => x == RootPart))
        {
            // TODO: reimplement this!
            Session.ActiveProperties.GenerateSelectionPropertiesAsync(Enumerable.Empty<IPropertyProviderViewModel>()).Forget();
        }
        // Destroy all parts recursively
        RootPart?.Destroy();
        base.Destroy();
    }

    public static IReadOnlySet<TViewModel> GetCommonRoots<TViewModel>(IReadOnlyCollection<TViewModel> items)
        where TViewModel : AssetCompositeItemViewModel
    {
        var hashSet = new HashSet<TViewModel>(items);
        foreach (var item in items)
        {
            var parent = item.Parent;
            while (parent != null)
            {
                if (hashSet.Contains(parent))
                {
                    hashSet.Remove(item);
                    break;
                }
                parent = parent.Parent;
            }
        }
        return hashSet;
    }

    /// <inheritdoc/>
    public override IAssetPartViewModel? FindPartViewModel(AbsoluteId id)
    {
        if (RootPart is IAssetPartViewModel item && id == item.Id)
            return item;

        return RootPart.EnumerateChildren().BreadthFirst(x => x.EnumerateChildren()).FirstOrDefault(part => part is IAssetPartViewModel viewModel && viewModel.Id == id) as IAssetPartViewModel;
    }

    /// <summary>
    /// Gathers all base assets used in the composition of the given hierarchy, recursively.
    /// </summary>
    /// <param name="hierarchy"></param>
    /// <returns></returns>
    public IReadOnlySet<AssetId> GatherAllBasePartAssets(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> hierarchy)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        var baseAssets = new HashSet<AssetId>();
        GatherAllBasePartAssetsRecursively(hierarchy.Parts.Values, Session.PackageSession, baseAssets);
        return baseAssets;
    }

    protected abstract Task RefreshEditorProperties();

    /// <summary>
    /// Called when the content of <see cref="AssetCompositeEditorViewModel{TAsset,TAssetViewModel}.SelectedContent"/> changed.
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
    /// Default implementation populates <see cref="AssetCompositeEditorViewModel{TAsset,TAssetViewModel}.SelectedContent"/> with the same elements.
    /// </remarks>
    protected virtual void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
    {
        SelectedContent.Clear();
        SelectedContent.AddRange(SelectedItems);
    }

    /// <summary>
    /// Gathers all base assets used in the composition of the given asset parts, recursively.
    /// </summary>
    /// <returns></returns>
    private static void GatherAllBasePartAssetsRecursively(IEnumerable<TAssetPartDesign> assetParts, IAssetFinder assetFinder, ISet<AssetId> baseAssets)
    {
        foreach (var part in assetParts)
        {
            if (part.Base == null || !baseAssets.Add(part.Base.BasePartAsset.Id))
                continue;

            if (assetFinder.FindAsset(part.Base.BasePartAsset.Id)?.Asset is AssetCompositeHierarchy<TAssetPartDesign, TAssetPart> baseAsset)
            {
                GatherAllBasePartAssetsRecursively(baseAsset.Hierarchy.Parts.Values, assetFinder, baseAssets);
            }
        }
    }

    private void SelectedContentCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedContentCollectionChanged(args.Action);
            // Refresh the property grid asynchronously
            RefreshEditorProperties().Forget();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }

    private void SelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (updateSelectionGuard)
            return;

        try
        {
            updateSelectionGuard = true;
            SelectedItemsCollectionChanged(args.Action);
            // Refresh the property grid asynchronously
            RefreshEditorProperties().Forget();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }
}
