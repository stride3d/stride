// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Analysis;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Assets.Quantum;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Extensions;
using Xenko.Core.IO;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Quantum;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Interop;
using Xenko.Core.Presentation.Services;
using Xenko.Core.Quantum;

namespace Xenko.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels
{
    /// <summary>
    /// Base class for the view model of an <see cref="AssetCompositeHierarchyViewModel{TAssetPartDesign,TAssetPart}"/> editor.
    /// </summary>
    /// <typeparam name="TAssetPartDesign">The type of a part design.</typeparam>
    /// <typeparam name="TAssetPart">The type of a part.</typeparam>
    /// <typeparam name="TItemViewModel">The type of a real <see cref="AssetCompositeItemViewModel"/> that can be copied/cut/pasted.</typeparam>
    public abstract class AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TItemViewModel> : AssetCompositeEditorViewModel
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
        where TAssetPart : class, IIdentifiable
        where TItemViewModel : AssetCompositeItemViewModel, IEditorDesignPartViewModel<TAssetPartDesign, TAssetPart>, IEditorGamePartViewModel
    {
        private AssetCompositeItemViewModel rootPart;
        private bool updateSelectionCollection;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeHierarchyEditorViewModel{TAssetPartDesign,TAssetPart,TItemViewModel}"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        protected AssetCompositeHierarchyEditorViewModel([NotNull] AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
            CopyCommand = new AnonymousCommand(ServiceProvider, Copy, CanCopy);
            CutCommand = new AnonymousCommand(ServiceProvider, Cut, CanCut);
            DeleteCommand = new AnonymousTaskCommand(ServiceProvider, Delete, CanDelete);
            DuplicateSelectionCommand = new AnonymousCommand(ServiceProvider, () => DuplicateSelection());
            PasteCommand = new AnonymousTaskCommand<bool>(ServiceProvider, Paste, CanPaste);

            SelectedContent.CollectionChanged += SelectedContentCollectionChanged;
            SelectedItems.CollectionChanged += SelectedItemsCollectionChanged;
        }

        public AssetCompositeItemViewModel RootPart { get => rootPart; private set => SetValue(ref rootPart, value); }

        [ItemNotNull, NotNull]
        public ObservableSet<TItemViewModel> SelectedItems { get; } = new ObservableSet<TItemViewModel>();

        [NotNull]
        public ICommandBase CopyCommand { get; }

        [NotNull]
        public ICommandBase CutCommand { get; }

        [NotNull]
        public ICommandBase DeleteCommand { get; }

        [NotNull]
        public ICommandBase DuplicateSelectionCommand { get; }

        [NotNull]
        public ICommandBase PasteCommand { get; }

        [NotNull]
        protected new AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> Asset => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)base.Asset;

        [NotNull]
        protected FuncClipboardMonitor<bool> PasteAsRootMonitor { get; } = new FuncClipboardMonitor<bool>();

        [NotNull]
        protected FuncClipboardMonitor<bool> PasteMonitor { get; } = new FuncClipboardMonitor<bool>();

        /// <summary>
        /// Clears the selection.
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ClearSelection()
        {
            SelectedContent.Clear();
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(AssetCompositeHierarchyEditorViewModel<TAssetPartDesign, TAssetPart, TItemViewModel>));

            PasteAsRootMonitor.Destroy();
            PasteMonitor.Destroy();

            // Unregister collection
            ServiceProvider.Get<SelectionService>().UnregisterSelectionScope(SelectedContent);
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

        /// <summary>
        /// Creates a view model for the given part of the specified <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset"></param>
        /// <param name="partDesign">A part design with the part for which to create a view model.</param>
        /// <returns>The view model of the given part.</returns>
        [NotNull]
        public abstract AssetCompositeItemViewModel CreatePartViewModel([NotNull] AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> asset, [NotNull] TAssetPartDesign partDesign);

        /// <inheritdoc/>
        [CanBeNull]
        public override IEditorGamePartViewModel FindPartViewModel(AbsoluteId id)
        {
            var item = RootPart as IEditorGamePartViewModel;
            if (item != null && id == item.Id)
                return item;

            return RootPart?.EnumerateChildren().BreadthFirst(x => x.EnumerateChildren()).OfType<IEditorGamePartViewModel>().FirstOrDefault(part => part.Id == id);
        }

        /// <summary>
        /// Gathers all base assets used in the composition of the given hierarchy, recursively.
        /// </summary>
        /// <param name="hierarchy"></param>
        /// <returns></returns>
        [NotNull]
        public ISet<AssetId> GatherAllBasePartAssets([NotNull] AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> hierarchy)
        {
            if (hierarchy == null) throw new ArgumentNullException(nameof(hierarchy));
            var baseAssets = new HashSet<AssetId>();
            GatherAllBasePartAssetsRecursively(hierarchy.Parts.Values, Session, baseAssets);
            return baseAssets;
        }

        /// <summary>
        /// Gathers all base assets used in the composition of the given item, recursively.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="includeChildren"><c>true</c> if the children (recursively) of the item should be included; otherwise, <c>false</c>.</param>
        /// <returns></returns>
        [NotNull]
        public ISet<AssetId> GatherAllBasePartAssets([NotNull] TItemViewModel item, bool includeChildren)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            var assetParts = item.PartDesign.Yield();
            if (includeChildren)
            {
                var assetHierarchy = (AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>)item.Asset.Asset;
                assetParts = assetParts.Concat(assetHierarchy.EnumerateChildPartDesigns(item.PartDesign, assetHierarchy.Hierarchy, true));
            }
            var baseAssets = new HashSet<AssetId>();
            GatherAllBasePartAssetsRecursively(assetParts, Session, baseAssets);
            return baseAssets;
        }

        [NotNull]
        public static ISet<TViewModel> GetCommonRoots<TViewModel>([NotNull] IReadOnlyCollection<TViewModel> items)
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

        protected void BreakLinkToBase(string baseTypeName = "base")
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // Break links to the base for selected parts of their respective asset.
                foreach (var grp in SelectedItems.GroupBy(e => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)e.Asset))
                {
                    grp.Key.AssetHierarchyPropertyGraph.BreakBasePartLinks(grp.Select(x => x.PartDesign));
                }
                UndoRedoService.SetName(transaction, $"Break link to {baseTypeName}");
            }
        }

        /// <summary>
        /// Indicates whether <see cref="CopyCommand"/> can be executed.
        /// </summary>
        /// <returns><c>true</c> is <see cref="CopyCommand"/> can be executed; otherwise, <c>false</c>.</returns>
        protected bool CanCopy()
        {
            return SelectedItems.Count > 0 && ServiceProvider.TryGet<ICopyPasteService>() != null;
        }

        /// <summary>
        /// Indicates whether <see cref="CutCommand"/> can be executed.
        /// </summary>
        /// <returns><c>true</c> is <see cref="CutCommand"/> can be executed; otherwise, <c>false</c>.</returns>
        protected bool CanCut()
        {
            return CanCopy() && CanDelete();
        }

        /// <summary>
        /// Indicates whether <see cref="DeleteCommand"/> can be executed.
        /// </summary>
        /// <returns><c>true</c> is <see cref="DeleteCommand"/> can be executed; otherwise, <c>false</c>.</returns>
        protected virtual bool CanDelete()
        {
            return SelectedItems.Count > 0;
        }

        /// <summary>
        /// Indicates whether <see cref="PasteCommand"/> can be executed.
        /// </summary>
        /// <param name="asRoot"></param>
        /// <returns><c>true</c> is <see cref="PasteCommand"/> can be executed; otherwise, <c>false</c>.</returns>
        protected abstract bool CanPaste(bool asRoot);

        /// <summary>
        /// Creates a view model to contain the root parts of this asset.
        /// </summary>
        /// <returns>An item that contains the root parts of this asset.</returns>
        [NotNull]
        protected abstract AssetCompositeItemViewModel CreateRootPartViewModel();

        /// <summary>
        /// Implements <see cref="DeleteCommand"/>.
        /// </summary>
        /// <returns></returns>
        protected abstract Task Delete();

        /// <summary>
        /// Implements <see cref="DuplicateSelectionCommand"/>.
        /// </summary>
        /// <returns></returns>
        [NotNull]
        protected abstract ISet<TItemViewModel> DuplicateSelection();

        /// <summary>
        /// Checks whether the given paste data can be pasted into the given item.
        /// </summary>
        /// <param name="pasteResult"></param>
        /// <param name="item"></param>
        /// <param name="error"></param>
        /// <returns></returns>
        protected virtual bool CanPasteIntoItem([NotNull] IPasteResult pasteResult, AssetCompositeItemViewModel item, [CanBeNull] out string error)
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

        [CanBeNull]
        protected AssetItem CreateAssetFromSelectedParts([NotNull] Func<AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>> assetFactory, [NotNull] Func<TAssetPart, string> baseNameFunc, bool linkToBase, out Dictionary<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>, Dictionary<Guid, Guid>> idRemappings)
        {
            idRemappings = new Dictionary<AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>, Dictionary<Guid, Guid>>();
            AssetItem assetItem;
            if (SelectedItems.Count == 0)
                return null;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var clonedHierarchy = new AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>();
                foreach (var grp in SelectedItems.GroupBy(e => (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)e.Asset))
                {
                    var currentAsset = grp.Key;

                    // Gather overrides information, in case one of the part is already an instance of a base
                    var overrides = new Dictionary<Guid, YamlAssetMetadata<OverrideType>>();
                    foreach (var part in currentAsset.Asset.Hierarchy.Parts.Values)
                    {
                        var node = NodeContainer.GetNode(part.Part);
                        var partOverrides = AssetPropertyGraph.GenerateOverridesForSerialization(node);
                        overrides.Add(part.Part.Id, partOverrides);
                    }

                    // Clone the hierarchy starting from the parts that are root according to the selection
                    var selectedRoots = GetCommonRoots(grp.ToList());
                    var flags = SubHierarchyCloneFlags.CleanExternalReferences | SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
                    var subHierarchy = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(Session.AssetNodeContainer, currentAsset.Asset, selectedRoots.Select(x => x.Id.ObjectId), flags, out Dictionary<Guid, Guid> idRemapping);
                    AssetPartsAnalysis.GenerateNewBaseInstanceIds(subHierarchy);
                    clonedHierarchy.MergeInto(subHierarchy);

                    // For each new part, apply the same override that was existing before.
                    // NOTE: we need to do this before creating the AssetNodeGraph (done via CreateAsset) to be sure the reconciliation won't occur before we transfer the overrides.
                    foreach (var id in idRemapping)
                    {
                        // Process only ids that correspond to parts
                        if (!clonedHierarchy.Parts.TryGetValue(id.Value, out var clonedPart))
                            continue;

                        var node = (IAssetNode)NodeContainer.GetOrCreateNode(clonedPart.Part);
                        AssetPropertyGraph.ApplyOverrides(node, overrides[id.Key]);
                    }

                    idRemappings.Add(currentAsset, idRemapping);
                }

                // Build an AssetItem for the new base asset
                var firstRootName = baseNameFunc.Invoke(clonedHierarchy.RootParts.FirstOrDefault()) ?? Asset.TypeDisplayName;
                var sanitizedAssetName = Utilities.BuildValidFileName(firstRootName);
                var newAssetName = NamingHelper.ComputeNewName(sanitizedAssetName, Asset.Directory.Assets, x => x.Name);
                var defaultLocation = UFile.Combine(Asset.Directory.Path, newAssetName);

                var asset = assetFactory.Invoke();
                asset.Hierarchy = clonedHierarchy;
                assetItem = new AssetItem(defaultLocation, asset);

                // Create the view model (and the AssetPropertyGraph)
                var assetViewModel = Asset.Directory.Package.CreateAsset(Asset.Directory, assetItem, true, null);
                Session.ActiveAssetView.SelectAssets(assetViewModel.Yield());

                if (linkToBase)
                {
                    // Clear overrides and set the base of the selected parts to target the newly created base asset
                    foreach (var kv in idRemappings)
                    {
                        var currentAsset = kv.Key;
                        var idRemapping = kv.Value;
                        var instanceId = Guid.NewGuid();

                        foreach (var id in idRemapping)
                        {
                            TAssetPartDesign partDesign;
                            // Process only ids that correspond to parts
                            if (!currentAsset.Asset.Hierarchy.Parts.TryGetValue(id.Key, out partDesign))
                                continue;
                            // Update the base
                            NodeContainer.GetNode(partDesign)[nameof(IAssetPartDesign.Base)].Update(new BasePart(new AssetReference(assetItem.Id, assetItem.Location), id.Value, instanceId));
                        }
                        // Re-link the parts to their (new) bases
                        currentAsset.PropertyGraph.RefreshBase();

                        foreach (var id in idRemapping)
                        {
                            TAssetPartDesign partDesign;
                            // Process only ids that correspond to parts
                            if (!currentAsset.Asset.Hierarchy.Parts.TryGetValue(id.Key, out partDesign))
                                continue;

                            var partNode = (IAssetObjectNode)NodeContainer.GetNode(partDesign.Part);
                            // Now that we have the proper base for each part, we can reset all overrides
                            partNode.ResetOverrideRecursively();
                        }
                    }
                }

                UndoRedoService.SetName(transaction, $"Create {newAssetName} from selection");
            }

            return assetItem;
        }

        /// <inheritdoc/>
        protected override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            RootPart = CreateRootPartViewModel();

            return true;
        }

        /// <summary>
        /// Implements <see cref="PasteCommand"/>.
        /// </summary>
        /// <param name="asRoot"><c>true</c> if the content should be pasted as root; otherwise, <c>false</c>.</param>
        /// <returns>A <see cref="Task"/> that can be awaited until the operation completes.</returns>
        protected virtual async Task Paste(bool asRoot)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                string actionName;
                if (asRoot)
                {
                    // Attempt to paste at the root level
                    await PasteIntoItems(RootPart.Yield());
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

                UndoRedoService.SetName(transaction, actionName);
            }
        }

        /// <summary>
        /// Attempts to paste the current clipboard's content into the specified <paramref name="items"/>.
        /// </summary>
        /// <param name="items"></param>
        /// <returns>A <see cref="Task"/> that can be awaited until the operation completes.</returns>
        protected async Task PasteIntoItems([NotNull] [ItemNotNull]  IEnumerable<AssetCompositeItemViewModel> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            // Retrieve data from the clipboard
            var text = SafeClipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return;

            var pasteResults = new Dictionary<AssetCompositeItemViewModel, IPasteResult>();
            foreach (var item in items)
            {
                var pasteResult = ServiceProvider.TryGet<ICopyPasteService>()?.DeserializeCopiedData(text, item.Asset.Asset, typeof(TAssetPart));
                if (pasteResult == null || pasteResult.Items.Count == 0)
                    return;

                if (!CanPasteIntoItem(pasteResult, item, out string error))
                {
                    await ServiceProvider.Get<IEditorDialogService>().MessageBox(error, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                pasteResults.Add(item, pasteResult);
            }
            foreach (var kv in pasteResults)
            {
                var item = kv.Key;
                var pasteResult = kv.Value;
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
        /// Default implementation populates <see cref="AssetCompositeEditorViewModel.SelectedContent"/> with the same elements.
        /// </remarks>
        protected virtual void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
        {
            ClearSelection();
            SelectedContent.AddRange(SelectedItems);
        }

        protected virtual void UpdateCommands()
        {
            // We need to it on the cut/copy/paste/delete commands too, otherwise it is not correct in the game view
            CutCommand.IsEnabled = CanCut();
            CopyCommand.IsEnabled = CanCopy();
            DeleteCommand.IsEnabled = CanDelete();
        }

        /// <summary>
        /// Attaches additional properties into the given <see cref="PropertyContainer"/>, to be consumed by the paste processor.
        /// </summary>
        /// <param name="propertyContainer">The container into which to attach the properties</param>
        /// <param name="pasteTarget">The view model of the item into which the paste will occur.</param>
        protected virtual void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, AssetCompositeItemViewModel pasteTarget)
        {
            // Do nothing by default.
        }

        /// <summary>
        /// Implements <see cref="CopyCommand"/>.
        /// </summary>
        private void Copy()
        {
            if (SelectedItems.Count == 0)
                return;

            // Group by asset
            var items = SelectedContent.Cast<AssetCompositeItemViewModel>().GroupBy(x => x.Asset).Select(grp =>
            {
                var commonRoots = (ICollection<AssetCompositeItemViewModel>)GetCommonRoots(grp.ToList());
                var commonParts = (ICollection<TItemViewModel>)GetCommonRoots(SelectedItems.Where(x => x.Asset == grp.Key).ToList());
                return (commonRoots: commonRoots, commonParts: commonParts, asset: (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)grp.Key);
            });
            CopyToClipboard(items);
        }

        private void CopyToClipboard(IEnumerable<(ICollection<AssetCompositeItemViewModel> commonRoots, ICollection<TItemViewModel> commonParts, AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart> asset)> items)
        {
            var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
            if (copyPasteService == null)
                return;

            try
            {
                var text = copyPasteService.CopyFromAssets(items.Select(x =>
                {
                    var hierarchy = AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>.CloneSubHierarchies(Session.AssetNodeContainer, x.asset.Asset, x.commonParts.Select(r => r.Id.ObjectId), SubHierarchyCloneFlags.None, out _);
                    PrepareToCopy(hierarchy, x.commonRoots, x.commonParts);
                    return ((AssetPropertyGraph)x.asset.AssetHierarchyPropertyGraph, (AssetId?)x.asset.Id, (object)hierarchy, false);
                }).ToList(), typeof(AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart>));
                if (string.IsNullOrEmpty(text))
                    return;

                // save to the clipboard
                SafeClipboard.SetText(text);
            }
            catch (SystemException e)
            {
                // We don't provide feedback when copying fails.
                e.Ignore();
            }
        }

        /// <summary>
        /// Prepares the given hierarchy to be copied into the clipboard.
        /// </summary>
        /// <param name="clonedHierarchy">The hierarchy to prepare, that has been cloned out of the actual parts.</param>
        /// <param name="commonRoots">The view models of the actual items that are being copied (including parts and virtual items).</param>
        /// <param name="commonParts">The view models of the actual parts that are being copied.</param>
        protected virtual void PrepareToCopy([NotNull] AssetCompositeHierarchyData<TAssetPartDesign, TAssetPart> clonedHierarchy, [NotNull] ICollection<AssetCompositeItemViewModel> commonRoots, ICollection<TItemViewModel> commonParts)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Implements <see cref="CutCommand"/>.
        /// </summary>
        private void Cut()
        {
            if (SelectedItems.Count == 0)
                return;

            // Group by asset
            var items = SelectedContent.Cast<AssetCompositeItemViewModel>().GroupBy(x => x.Asset).Select(grp =>
            {
                var commonRoots = (ICollection<AssetCompositeItemViewModel>)GetCommonRoots(grp.ToList());
                var commonParts = (ICollection<TItemViewModel>)GetCommonRoots(SelectedItems.Where(x => x.Asset == grp.Key).ToList());
                return (commonRoots: commonRoots, commonParts: commonParts, asset: (AssetCompositeHierarchyViewModel<TAssetPartDesign, TAssetPart>)grp.Key);
            }).ToList();
            // Clear the selection
            ClearSelection();
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                CopyToClipboard(items);

                // We don't use DeletePart but rather RemovePartFromAsset so references to the cut element won't be cleared.
                // Then, if we paste into the same asset, they will be automagically restored.
                foreach (var item in items.SelectMany(x => x.commonParts).DepthFirst(x => x.EnumerateChildren().OfType<TItemViewModel>()).Reverse())
                {
                    ((AssetCompositeHierarchyPropertyGraph<TAssetPartDesign, TAssetPart>)item.Asset.PropertyGraph).RemovePartFromAsset(item.PartDesign);
                }
                UndoRedoService.SetName(transaction, "Cut selection");
            }
        }

        /// <summary>
        /// Gathers all base assets used in the composition of the given asset parts, recursively.
        /// </summary>
        /// <returns></returns>
        private static void GatherAllBasePartAssetsRecursively([ItemNotNull, NotNull] IEnumerable<TAssetPartDesign> assetParts, [NotNull] IAssetFinder assetFinder, [NotNull] ISet<AssetId> baseAssets)
        {
            foreach (var part in assetParts)
            {
                if (part.Base == null || !baseAssets.Add(part.Base.BasePartAsset.Id))
                    continue;

                var baseAsset = assetFinder.FindAsset(part.Base.BasePartAsset.Id)?.Asset as AssetCompositeHierarchy<TAssetPartDesign, TAssetPart>;
                if (baseAsset != null)
                {
                    GatherAllBasePartAssetsRecursively(baseAsset.Hierarchy.Parts.Values, assetFinder, baseAssets);
                }
            }
        }

        private void SelectedContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (updateSelectionCollection)
                return;

            try
            {
                updateSelectionCollection = true;
                SelectedContentCollectionChanged(args.Action);
                // Refresh the property grid
                RefreshEditorProperties().Forget();
                // Update the commands
                UpdateCommands();
                // Invalidate CanPaste
                PasteMonitor.Invalidate();
            }
            finally
            {
                updateSelectionCollection = false;
            }
        }

        private void SelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (updateSelectionCollection)
                return;

            try
            {
                updateSelectionCollection = true;
                SelectedItemsCollectionChanged(args.Action);
                // Refresh the property grid
                RefreshEditorProperties().Forget();
                // Update the commands
                UpdateCommands();
                // Invalidate CanPaste
                PasteMonitor.Invalidate();
            }
            finally
            {
                updateSelectionCollection = false;
            }
        }
    }
}
