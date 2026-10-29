// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Yaml;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Core.Reflection;

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

        CopyCommand = new AnonymousTaskCommand(ServiceProvider, Copy, CanCopy);
        CutCommand = new AnonymousTaskCommand(ServiceProvider, Cut, CanCut);
        DeleteCommand = new AnonymousTaskCommand(ServiceProvider, Delete, CanDelete);
        DuplicateSelectionCommand = new AnonymousCommand(ServiceProvider, () => DuplicateSelection());
        PasteCommand = new AnonymousTaskCommand<bool>(ServiceProvider, Paste, _ => CanPaste());

        SelectedContent.CollectionChanged += SelectedContentCollectionChanged;
        SelectedItems.CollectionChanged += SelectedItemsCollectionChanged;
    }

    public TItemViewModel RootPart { get => rootPart; private set => SetValue(ref rootPart, value); }

    public ObservableSet<TItemViewModel> SelectedItems { get; } = [];

    /// <summary>
    /// Gets the command that copies the selected parts, with their children, to the clipboard.
    /// </summary>
    public ICommandBase CopyCommand { get; }

    /// <summary>
    /// Gets the command that copies the selected parts to the clipboard and removes them from the asset, as one undoable
    /// operation.
    /// </summary>
    public ICommandBase CutCommand { get; }

    /// <summary>
    /// Gets the command that deletes the selected parts, as one undoable operation.
    /// </summary>
    public ICommandBase DeleteCommand { get; }

    /// <summary>
    /// Gets the command that adds a copy of the selected parts next to them and selects the copies, as one undoable
    /// operation (Ctrl+D).
    /// </summary>
    public ICommandBase DuplicateSelectionCommand { get; }

    /// <summary>
    /// Gets the command that pastes the parts of the clipboard into the selected items, or at the root of the asset when
    /// its parameter is <c>true</c>, as one undoable operation.
    /// </summary>
    public ICommandBase PasteCommand { get; }

    /// <summary>
    /// Gets the asset of this editor, as a hierarchy of parts.
    /// </summary>
    protected AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> HierarchyAsset => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)Asset;

    /// <summary>
    /// Clears the selection.
    /// </summary>
    public void ClearSelection()
    {
        SelectedContent.Clear();
    }

    /// <summary>
    /// Gathers the base assets of the parts of the given hierarchy, recursively.
    /// </summary>
    public ISet<AssetId> GatherAllBasePartAssets(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> hierarchy)
    {
        ArgumentNullException.ThrowIfNull(hierarchy);
        var baseAssets = new HashSet<AssetId>();
        GatherAllBasePartAssetsRecursively(hierarchy.Parts.Values, Session, baseAssets);
        return baseAssets;
    }

    /// <summary>
    /// Gathers the base assets of the given part, and of its children when <paramref name="includeChildren"/> is set,
    /// recursively.
    /// </summary>
    public ISet<AssetId> GatherAllBasePartAssets(TItemViewModel item, bool includeChildren)
    {
        ArgumentNullException.ThrowIfNull(item);

        var partDesign = GetPartDesign(item) ?? throw new ArgumentException("The item is not a part.", nameof(item));
        var assetParts = partDesign.Yield();
        if (includeChildren)
        {
            var assetHierarchy = ((AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)item.Asset).Asset;
            assetParts = assetParts.Concat(assetHierarchy.EnumerateChildPartDesigns(partDesign, assetHierarchy.Hierarchy, true));
        }
        var baseAssets = new HashSet<AssetId>();
        GatherAllBasePartAssetsRecursively(assetParts, Session, baseAssets);
        return baseAssets;
    }

    /// <summary>
    /// Gets the items of the given collection that have no ancestor in the collection.
    /// </summary>
    public static ISet<TViewModel> GetCommonRoots<TViewModel>(IReadOnlyCollection<TViewModel> items)
        where TViewModel : TItemViewModel
    {
        var hashSet = new HashSet<TViewModel>(items);
        foreach (var item in items)
        {
            var parent = item.Parent;
            while (parent != null)
            {
                if (parent is TViewModel ancestor && hashSet.Contains(ancestor))
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

    /// <inheritdoc/>
    public override IAssetPartViewModel? FindPartViewModel(AbsoluteId id)
    {
        if (RootPart is IAssetPartViewModel item && id == item.Id)
            return item;

        return RootPart?.EnumerateChildren().BreadthFirst(x => x.EnumerateChildren()).FirstOrDefault(part => part is IAssetPartViewModel viewModel && viewModel.Id == id) as IAssetPartViewModel;
    }

    protected abstract TItemViewModel CreateRootPartViewModel();

    protected abstract Task RefreshEditorProperties();

    /// <summary>
    /// Gets the design of the part an item represents, or <c>null</c> when the item is not a part (the root, a folder).
    /// </summary>
    protected abstract TAssetPartDesign? GetPartDesign(TItemViewModel item);

    /// <summary>
    /// Gets the type of the member the parts are pasted into, and the types of the data the clipboard can hold, for
    /// <see cref="PasteCommand"/>.
    /// </summary>
    /// <param name="asRoot">Whether the paste goes to the root of the asset.</param>
    protected abstract (Type TargetMemberType, Type[] ExpectedTypes) GetPasteTypes(bool asRoot);

    /// <summary>
    /// Implements <see cref="DeleteCommand"/>.
    /// </summary>
    protected abstract Task Delete();

    /// <summary>
    /// Implements <see cref="DuplicateSelectionCommand"/>.
    /// </summary>
    /// <returns>The copies, which are selected.</returns>
    protected abstract ISet<TItemViewModel> DuplicateSelection();

    /// <summary>
    /// Breaks the link of the selected parts to their base, as one undoable operation.
    /// </summary>
    /// <param name="baseTypeName">The name of the base, for the name of the operation.</param>
    protected void BreakLinkToBase(string baseTypeName = "base")
    {
        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        // The selected parts of each asset
        foreach (var group in SelectedItems.GroupBy(x => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)x.Asset))
        {
            group.Key.AssetHierarchyPropertyGraph.BreakBasePartLinks(group.Select(GetPartDesign).NotNull());
        }
        undoRedo.SetName(transaction, $"Break link to {baseTypeName}");
    }

    /// <summary>
    /// Creates an asset (a prefab of the scene editor) from the selected parts, with their children, in the folder of the
    /// edited asset, and selects it in the asset view, as one undoable operation.
    /// </summary>
    /// <param name="assetFactory">Creates the empty asset.</param>
    /// <param name="baseNameFunc">The base name of the asset, from its first root part (<c>null</c> when it has none).</param>
    /// <param name="linkToBase">Whether the selected parts become instances of the new asset.</param>
    /// <param name="idRemappings">The ids of the parts in the new asset, by id of the selected parts, by edited asset.</param>
    /// <returns>The new asset, or <c>null</c> when nothing is selected.</returns>
    protected AssetItem? CreateAssetFromSelectedParts(Func<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>> assetFactory, Func<TAssetPart?, string?> baseNameFunc, bool linkToBase, out Dictionary<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>, Dictionary<Guid, Guid>> idRemappings)
    {
        idRemappings = [];
        if (SelectedItems.Count == 0)
            return null;

        var nodeContainer = Session.AssetNodeContainer;
        var undoRedo = Session.UndoRedoService;
        AssetItem assetItem;
        using (var transaction = undoRedo.CreateTransaction())
        {
            var clonedHierarchy = new AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>();
            foreach (var group in SelectedItems.GroupBy(e => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)e.Asset))
            {
                var currentAsset = group.Key;

                // The overrides of the parts, in case one of the parts is already an instance of a base
                var overrides = new Dictionary<Guid, YamlAssetMetadata<OverrideType>>();
                foreach (var part in currentAsset.Asset.Hierarchy.Parts.Values)
                {
                    var node = nodeContainer.GetNode(part.Part);
                    overrides.Add(part.Part.Id, AssetPropertyGraph.GenerateOverridesForSerialization(node));
                }

                // The hierarchy from the parts that are roots of the selection
                var selectedRoots = GetCommonRoots(group.ToList());
                var flags = SubHierarchyCloneFlags.CleanExternalReferences | SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                var subHierarchy = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(nodeContainer, currentAsset.Asset, selectedRoots.Select(GetPartDesign).NotNull().Select(x => x.Part.Id), flags, out var idRemapping);
                AssetPartsAnalysis.GenerateNewBaseInstanceIds(subHierarchy);
                clonedHierarchy.MergeInto(subHierarchy);

                // Each new part gets the overrides of its part. Before the asset gets its graph, so that the reconciliation
                // with the base doesn't occur before.
                foreach (var id in idRemapping)
                {
                    // The ids of the parts only
                    if (!clonedHierarchy.Parts.TryGetValue(id.Value, out var clonedPart))
                        continue;

                    var node = (IAssetNode)nodeContainer.GetOrCreateNode(clonedPart.Part);
                    AssetPropertyGraph.ApplyOverrides(node, overrides[id.Key]);
                }

                idRemappings.Add(currentAsset, idRemapping);
            }

            // The item of the new asset, next to the edited asset
            var firstRootName = baseNameFunc(clonedHierarchy.RootParts.FirstOrDefault()) ?? Asset.TypeDisplayName;
            // The characters a file name cannot have, as Utilities.BuildValidFileName of the WPF editor
            var sanitizedAssetName = Regex.Replace(firstRootName, "[=;,/\\?:&!.*<>|#%\"]", "_");
            var newAssetName = NamingHelper.ComputeNewName(sanitizedAssetName, Asset.Directory.Assets, x => x.Name);
            var defaultLocation = UFile.Combine(Asset.Directory.Path, newAssetName);

            var asset = assetFactory();
            asset.Hierarchy = clonedHierarchy;
            assetItem = new AssetItem(defaultLocation, asset);

            // The view model of the asset, and its property graph
            var assetViewModel = Asset.Directory.Package.CreateAsset(Asset.Directory, assetItem, true, null);
            Session.AssetCollection.SelectAssets(assetViewModel.Yield());

            if (linkToBase)
            {
                // The selected parts become instances of the new asset, without overrides
                foreach (var (currentAsset, idRemapping) in idRemappings)
                {
                    var instanceId = Guid.NewGuid();
                    foreach (var id in idRemapping)
                    {
                        // The ids of the parts only
                        if (!currentAsset.Asset.Hierarchy.Parts.TryGetValue(id.Key, out var partDesign))
                            continue;
                        nodeContainer.GetNode(partDesign)[nameof(IAssetPartDesign.Base)].Update(new BasePart(new AssetReference(assetItem.Id, assetItem.Location), id.Value, instanceId));
                    }
                    // The parts are linked to their new base
                    currentAsset.PropertyGraph!.RefreshBase();

                    foreach (var id in idRemapping)
                    {
                        if (!currentAsset.Asset.Hierarchy.Parts.TryGetValue(id.Key, out var partDesign))
                            continue;

                        // With the proper base for each part, all the overrides can be reset
                        var partNode = (IAssetObjectNode)nodeContainer.GetNode(partDesign.Part);
                        partNode.ResetOverrideRecursively();
                    }
                }
            }

            undoRedo.SetName(transaction, $"Create {newAssetName} from selection");
        }

        return assetItem;
    }

    /// <summary>
    /// Whether <see cref="CopyCommand"/> can be executed.
    /// </summary>
    protected bool CanCopy()
    {
        return SelectedItems.Count > 0 && ServiceProvider.TryGet<ICopyPasteService>() is not null && ServiceProvider.TryGet<IClipboardService>() is not null;
    }

    /// <summary>
    /// Whether <see cref="CutCommand"/> can be executed.
    /// </summary>
    protected bool CanCut()
    {
        return CanCopy() && CanDelete();
    }

    /// <summary>
    /// Whether <see cref="DeleteCommand"/> can be executed.
    /// </summary>
    protected virtual bool CanDelete()
    {
        return SelectedItems.Count > 0;
    }

    /// <summary>
    /// Whether <see cref="PasteCommand"/> can be executed. The clipboard can only be read asynchronously: the command checks its
    /// content when it runs, and does nothing when the clipboard holds nothing to paste.
    /// </summary>
    protected bool CanPaste()
    {
        return ServiceProvider.TryGet<ICopyPasteService>() is not null && ServiceProvider.TryGet<IClipboardService>() is not null;
    }

    /// <summary>
    /// Checks whether the given paste data can be pasted into the given item.
    /// </summary>
    protected virtual bool CanPasteIntoItem(IPasteResult pasteResult, TItemViewModel item, out string? error)
    {
        ArgumentNullException.ThrowIfNull(pasteResult);

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

    /// <summary>
    /// Implements <see cref="PasteCommand"/>.
    /// </summary>
    /// <param name="asRoot"><c>true</c> to paste at the root of the asset; otherwise, into the selected items.</param>
    protected virtual async Task Paste(bool asRoot)
    {
        var text = await GetPastedTextAsync(asRoot);
        if (text is null)
            return;

        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        string actionName;
        if (asRoot)
        {
            // Paste at the root level
            await PasteIntoItems(text, RootPart.Yield());
            actionName = $"Paste into {Asset.Name}";
        }
        else
        {
            var selectedItems = SelectedContent.OfType<TItemViewModel>().ToList();
            if (selectedItems.Count == 0)
                return;
            // Paste into the selected items
            await PasteIntoItems(text, selectedItems);
            actionName = "Paste into selection";
        }

        undoRedo.SetName(transaction, actionName);
    }

    /// <summary>
    /// Reads the clipboard for <see cref="PasteCommand"/>.
    /// </summary>
    /// <returns>The text of the clipboard, or <c>null</c> when it holds nothing this editor can paste.</returns>
    protected async Task<string?> GetPastedTextAsync(bool asRoot)
    {
        if (ServiceProvider.TryGet<ICopyPasteService>() is not { } copyPasteService || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
            return null;

        var text = await clipboard.GetTextAsync();
        var (targetMemberType, expectedTypes) = GetPasteTypes(asRoot);
        if (string.IsNullOrEmpty(text) || !copyPasteService.CanPaste(text, Asset.AssetType, targetMemberType, expectedTypes))
            return null;
        return text;
    }

    /// <summary>
    /// Pastes the given copied text into the given items.
    /// </summary>
    protected async Task PasteIntoItems(string text, IEnumerable<TItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (ServiceProvider.TryGet<ICopyPasteService>() is not { } copyPasteService)
            return;

        var pasteResults = new Dictionary<TItemViewModel, IPasteResult>();
        foreach (var item in items)
        {
            var pasteResult = copyPasteService.DeserializeCopiedData(text, item.Asset.Asset, typeof(TAssetPart));
            if (pasteResult.Items.Count == 0)
                return;

            if (!CanPasteIntoItem(pasteResult, item, out var error))
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(error!, MessageBoxButton.OK, MessageBoxImage.Information);
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
                await (pasteItem.Processor?.Paste(pasteItem, item.Asset.PropertyGraph, ref nodeAccessor, ref propertyContainer) ?? Task.CompletedTask);
            }
        }
    }

    /// <summary>
    /// Attaches additional properties into the given <see cref="PropertyContainer"/>, to be consumed by the paste processor.
    /// </summary>
    /// <param name="propertyContainer">The container into which to attach the properties</param>
    /// <param name="pasteTarget">The view model of the item into which the paste will occur.</param>
    protected virtual void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, TItemViewModel pasteTarget)
    {
        // Do nothing by default.
    }

    /// <summary>
    /// Prepares the given hierarchy to be copied into the clipboard.
    /// </summary>
    /// <param name="clonedHierarchy">The hierarchy to prepare, that has been cloned out of the actual parts.</param>
    /// <param name="commonRoots">The view models of the actual items that are being copied (including parts and virtual items).</param>
    /// <param name="commonParts">The view models of the actual parts that are being copied.</param>
    protected virtual void PrepareToCopy(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> clonedHierarchy, ICollection<TItemViewModel> commonRoots, ICollection<TItemViewModel> commonParts)
    {
        // Do nothing by default
    }

    /// <summary>
    /// Updates whether the commands can execute, for the current selection.
    /// </summary>
    protected virtual void UpdateCommands()
    {
        CutCommand.IsEnabled = CanCut();
        CopyCommand.IsEnabled = CanCopy();
        DeleteCommand.IsEnabled = CanDelete();
    }

    /// <summary>
    /// Implements <see cref="CopyCommand"/>.
    /// </summary>
    private Task Copy()
    {
        return SelectedItems.Count == 0 ? Task.CompletedTask : CopyToClipboardAsync(GetCopiedItems());
    }

    /// <summary>
    /// Implements <see cref="CutCommand"/>.
    /// </summary>
    private async Task Cut()
    {
        if (SelectedItems.Count == 0)
            return;

        var items = GetCopiedItems();
        if (!await CopyToClipboardAsync(items))
            return;

        // Clear the selection
        ClearSelection();
        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        // RemovePartFromAsset, not DeletePart, keeps the references to the cut parts: a paste into the same asset restores them
        foreach (var item in items.SelectMany(x => x.CommonParts).DepthFirst(x => x.Children).Reverse())
        {
            if (GetPartDesign(item) is { } partDesign)
                ((AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>)item.Asset.PropertyGraph!).RemovePartFromAsset(partDesign);
        }
        undoRedo.SetName(transaction, "Cut selection");
    }

    /// <summary>
    /// The selected items to copy, by asset: the items without a selected ancestor, and the parts among them.
    /// </summary>
    private List<(ICollection<TItemViewModel> CommonRoots, ICollection<TItemViewModel> CommonParts, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> Asset)> GetCopiedItems()
    {
        return SelectedContent.OfType<TItemViewModel>().GroupBy(x => x.Asset).Select(group =>
        {
            var commonRoots = (ICollection<TItemViewModel>)GetCommonRoots(group.ToList());
            var commonParts = (ICollection<TItemViewModel>)GetCommonRoots(SelectedItems.Where(x => x.Asset == group.Key).ToList());
            return (commonRoots, commonParts, (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)group.Key);
        }).ToList();
    }

    private async Task<bool> CopyToClipboardAsync(IEnumerable<(ICollection<TItemViewModel> CommonRoots, ICollection<TItemViewModel> CommonParts, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> Asset)> items)
    {
        if (ServiceProvider.TryGet<ICopyPasteService>() is not { } copyPasteService || ServiceProvider.TryGet<IClipboardService>() is not { } clipboard)
            return false;

        try
        {
            var text = copyPasteService.CopyFromAssets(items.Select(x =>
            {
                var partIds = x.CommonParts.Select(GetPartDesign).NotNull().Select(r => r.Part.Id);
                var hierarchy = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(Session.AssetNodeContainer, x.Asset.Asset, partIds, SubHierarchyCloneFlags.None, out _);
                PrepareToCopy(hierarchy, x.CommonRoots, x.CommonParts);
                return ((AssetPropertyGraph)x.Asset.AssetHierarchyPropertyGraph, (AssetId?)x.Asset.Id, (object)hierarchy, false);
            }).ToList(), typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>));
            if (string.IsNullOrEmpty(text))
                return false;

            await clipboard.SetTextAsync(text);
            return true;
        }
        catch (SystemException e)
        {
            // No feedback when the copy fails, as in the WPF editor
            e.Ignore();
            return false;
        }
    }

    /// <summary>
    /// Gathers the base assets used in the composition of the given parts, recursively.
    /// </summary>
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
            UpdateCommands();
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
            UpdateCommands();
        }
        finally
        {
            updateSelectionGuard = false;
        }
    }
}
