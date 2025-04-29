// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.ViewModels;

partial class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TAssetViewModel, TItemViewModel>
{
    protected IClipboardService? ClipboardService => ServiceProvider.TryGet<IClipboardService>();
    protected ICopyPasteService? CopyPasteService => ServiceProvider.TryGet<ICopyPasteService>();
    private IDialogService DialogService => ServiceProvider.Get<IDialogService>();

    public ICommandBase CopyCommand { get; }

    public ICommandBase CutCommand { get; }

    public ICommandBase DeleteCommand { get; }

    public ICommandBase PasteCommand { get; }

    /// <summary>
    /// Attaches additional properties into the given <see cref="PropertyContainer"/>, to be consumed by the paste processor.
    /// </summary>
    /// <param name="propertyContainer">The container into which to attach the properties</param>
    /// <param name="pasteTarget">The view model of the item into which the paste will occur.</param>
    protected virtual void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, AssetCompositeItemViewModel pasteTarget)
    {
        // Do nothing by default.
    }

    protected virtual bool CanCopy()
    {
        return CopyPasteService is not null && ClipboardService is not null;
    }

    protected virtual bool CanCut() => CanCopy() && CanDelete();

    protected virtual bool CanDelete()
    {
        return SelectedItems.Count > 0;
    }

    protected virtual bool CanPaste(bool asRoot) => CopyPasteService is not null && ClipboardService is not null;

    /// <summary>
    /// Checks whether the given paste data can be pasted into the given item.
    /// </summary>
    /// <param name="pasteResult"></param>
    /// <param name="item"></param>
    /// <param name="error"></param>
    /// <returns></returns>
    protected virtual bool CanPasteIntoItem(IPasteResult pasteResult, AssetCompositeItemViewModel item, [NotNullWhen(false)] out string? error)
    {
        if (pasteResult == null) throw new ArgumentNullException(nameof(pasteResult));

        if (pasteResult.Items
            .Select(r => r.Data as AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>).NotNull()
            .Any(h => GatherAllBasePartAssets(h).Contains(item.Asset.Id)))
        {
            error = "The copied elements depend on this asset and cannot be pasted.";
            return false;
        }

        error = null;
        return true;
    }

    protected abstract Task Delete();

    protected virtual async Task Paste(bool asRoot)
    {
        using var transaction = Session.ActionService?.CreateTransaction();
        string actionName;
        if (asRoot)
        {
            // Attempt to paste at the root level
            await PasteIntoItems(RootPart.Yield()!);
            actionName = $"Paste into {Asset.Name}";
        }
        else
        {
            var selectedItems = SelectedContent.OfType<AssetCompositeItemViewModel>().ToList();
            if (selectedItems.Count == 0)
                return;

            // Attempt to paste into the selected items
            await PasteIntoItems(selectedItems);
            actionName = "Paste into selection";
        }

        Session.ActionService?.SetName(transaction!, actionName);
    }

    /// <summary>
    /// Attempts to paste the current clipboard's content into the specified <paramref name="items"/>.
    /// </summary>
    /// <param name="items"></param>
    /// <returns>A <see cref="Task"/> that can be awaited until the operation completes.</returns>
    protected async Task PasteIntoItems(IEnumerable<AssetCompositeItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        // Retrieve data from the clipboard
        var text = await ClipboardService!.GetTextAsync();
        if (string.IsNullOrEmpty(text))
            return;

        var pasteResults = new Dictionary<AssetCompositeItemViewModel, IPasteResult>();
        foreach (var item in items)
        {
            var pasteResult = CopyPasteService!.DeserializeCopiedData(text, item.Asset.Asset, typeof(TAssetPart));
            if (pasteResult.Items.Count == 0)
                return;

            if (!CanPasteIntoItem(pasteResult, item, out var error))
            {
                await DialogService.MessageBoxAsync(error, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            pasteResults.Add(item, pasteResult);
        }
        foreach (var (item, pasteResult) in pasteResults)
        {
            var targetContent = item.GetNodePath().GetNode();
            var propertyContainer = new PropertyContainer();
            AttachPropertiesForPaste(ref propertyContainer, item);
            var nodeAccessor = new NodeAccessor(targetContent, NodeIndex.Empty);
            foreach (var pasteItem in pasteResult.Items)
            {
                await (pasteItem.Processor?.Paste(pasteItem, item.Asset.PropertyGraph!, ref nodeAccessor, ref propertyContainer) ?? Task.CompletedTask);
            }
        }
    }

    /// <summary>
    /// Prepares the given hierarchy to be copied into the clipboard.
    /// </summary>
    /// <param name="clonedHierarchy">The hierarchy to prepare, that has been cloned out of the actual parts.</param>
    /// <param name="commonRoots">The view models of the actual items that are being copied (including parts and virtual items).</param>
    /// <param name="commonParts">The view models of the actual parts that are being copied.</param>
    protected virtual void PrepareToCopy(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> clonedHierarchy, IReadOnlyCollection<AssetCompositeItemViewModel> commonRoots, IReadOnlyCollection<TItemViewModel> commonParts)
    {
        // Do nothing by default
    }

    protected virtual void UpdateCommands()
    {
        // We need to do it on the cut/copy/paste/delete commands too, otherwise it is not correct in the game view
        CopyCommand.IsEnabled = CanCopy();
        DeleteCommand.IsEnabled = CanDelete();
    }

    private async Task Copy()
    {
        // Group by asset
        var items = SelectedContent.Cast<AssetCompositeItemViewModel>().GroupBy(x => x.Asset).Select(grp =>
        {
            IReadOnlyCollection<AssetCompositeItemViewModel> commonRoots = GetCommonRoots(grp.ToList());
            IReadOnlyCollection<TItemViewModel> commonParts = GetCommonRoots(SelectedItems.Where(x => x.Asset == grp.Key).ToList());
            return (commonRoots, commonParts, asset: (TAssetViewModel)grp.Key);
        });
        await WriteToClipboardAsync(items);
    }

    private async Task Cut()
    {
        if (SelectedItems.Count == 0)
            return;

        // Group by asset
        var items = SelectedContent.Cast<AssetCompositeItemViewModel>().GroupBy(x => x.Asset).Select(grp =>
        {
            IReadOnlyCollection<AssetCompositeItemViewModel> commonRoots = GetCommonRoots(grp.ToList());
            IReadOnlyCollection<TItemViewModel> commonParts = GetCommonRoots(SelectedItems.Where(x => x.Asset == grp.Key).ToList());
            return (commonRoots, commonParts, asset: (TAssetViewModel)grp.Key);
        }).ToList();

        // Clear the selection
        ClearSelection();

        using var transaction = Session.ActionService?.CreateTransaction();
        await WriteToClipboardAsync(items);

        // We don't use DeletePart but rather RemovePartFromAsset so references to the cut element won't be cleared.
        // Then, if we paste into the same asset, they will be automagically restored.
        foreach (var item in items.SelectMany(x => x.commonParts).DepthFirst(x => x.EnumerateChildren().OfType<TItemViewModel>()).Reverse())
        {
            ((AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>?)item.Asset.PropertyGraph)?.RemovePartFromAsset(item.PartDesign);
        }
        Session.ActionService?.SetName(transaction!, "Cut selection");
    }

    private async Task WriteToClipboardAsync(
        IEnumerable<(IReadOnlyCollection<AssetCompositeItemViewModel> commonRoots, IReadOnlyCollection<TItemViewModel> commonParts, TAssetViewModel asset)> items)
    {
        try
        {
            var text = CopyPasteService!.CopyFromAssets(items.Select(x =>
            {
                var hierarchy = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(Session.AssetNodeContainer, x.asset.Asset, x.commonParts.Select(r => r.Id.ObjectId), SubHierarchyCloneFlags.None, out _);
                PrepareToCopy(hierarchy, x.commonRoots, x.commonParts);
                return ((AssetPropertyGraph)x.asset.AssetHierarchyPropertyGraph, (AssetId?)x.asset.Id, (object)hierarchy, false);
            }).ToList(), typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>));
            if (string.IsNullOrEmpty(text))
                return;

            await ClipboardService!.SetTextAsync(text);
        }
        catch (SystemException)
        {
            // We don't provide feedback when copying fails.
        }
    }
}
