// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Quantum;
using Stride.Core.Translation;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Game;
using Stride.Assets.Presentation.AssetEditors.UIEditor.Services;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.UI;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.UI;
using Stride.UI.Panels;

namespace Stride.Assets.Presentation.AssetEditors.UIEditor.ViewModels
{
    /// <summary>
    /// Base class for the view model of an <see cref="UIBaseViewModel"/> editor.
    /// </summary>
    public abstract class UIEditorBaseViewModel : AssetCompositeHierarchyEditorViewModel<UIElementDesign, UIElement, UIElementViewModel>, IAddChildViewModel
    {
        public static readonly ILogger EditorLogger = GlobalLogger.GetLogger("UI");

        private UIElementViewModel activeRoot;
        private readonly ObservableSet<IUIElementFactory> factories = new ObservableSet<IUIElementFactory>();
        private IReadOnlyCollection<IUIElementFactory> panelFactories;
        private UIElementViewModel lastSelectedElement;
        private readonly ObservableSet<UIElementViewModel> selectableElements = new ObservableSet<UIElementViewModel>();
        private Color guidelineColor = Color.Red;
        private float guidelineThickness = 2.0f;
        private Color highlightColor = Color.Pink;
        private float highlightThickness = 2.0f;
        private Color selectionColor = Color.LimeGreen;
        private float selectionThickness = 2.0f;
        private Color sizingColor = Color.Cyan;
        private float sizingThickness = 2.0f;
        private float snapValue = 1.0f;

        /// <summary>
        /// Initializes a new instance of the <see cref="UIBaseViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        protected UIEditorBaseViewModel([NotNull] UIBaseViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
            Camera = new EditorCameraViewModel(ServiceProvider, Controller);

            BreakLinkToLibraryCommand = new AnonymousCommand(ServiceProvider, BreakLinkToLibrary);
            CreateLibraryCommand = new AnonymousCommand(ServiceProvider, CreateLibraryFromSelection);
            CreatePageCommand = new AnonymousCommand(ServiceProvider, CreatePageFromSelection);
            GroupIntoCommand = new AnonymousCommand<IUIElementFactory>(ServiceProvider, GroupInto);
            OpenLibraryEditorCommand = new AnonymousCommand(ServiceProvider, OpenLibraryEditor);
            RefreshSelectableElementsCommand = new AnonymousCommand(ServiceProvider, RefreshSelectableElements);
            ResetZoomCommand = new AnonymousCommand(ServiceProvider, ResetZoom);
            SelectLibraryCommand = new AnonymousCommand(ServiceProvider, SelectLibrary);
            SetCurrentSelectionCommand = new AnonymousCommand<AbsoluteId>(ServiceProvider, SetCurrentSelection);
            ShowPropertiesCommand = new AnonymousCommand(ServiceProvider, ShowAssetProperties);
            ZoomInCommand = new AnonymousCommand(ServiceProvider, () => Zoom(-1));
            ZoomOutCommand = new AnonymousCommand(ServiceProvider, () => Zoom(+1));
            UpdateCommands();
        }

        public UIElementViewModel ActiveRoot
        {
            get { return activeRoot; }
            protected internal set
            {
                var oldValue = activeRoot;
                SetValue(ref activeRoot, value, () => ActiveRootChanged(oldValue, activeRoot));
            }
        }

        public EditorCameraViewModel Camera { get; }

        public UIElementViewModel LastSelectedElement { get { return lastSelectedElement; } private set { SetValue(ref lastSelectedElement, value); } }

        public IReadOnlyObservableCollection<IUIElementFactory> Factories => factories;

        public ILogger Logger => EditorLogger;

        public IReadOnlyCollection<IUIElementFactory> PanelFactories { get { return panelFactories; } private set { SetValue(ref panelFactories, value); } }

        /// <summary>
        /// The list of elements that can be set as current selection.
        /// </summary>
        /// <seealso cref="SetCurrentSelectionCommand"/>
        public IReadOnlyObservableCollection<UIElementViewModel> SelectableElements => selectableElements;

        [NotNull]
        public UIAssetBase UIAsset => (UIAssetBase)Asset.Asset;

        #region Editor Settings
        public Color GuidelineColor
        {
            get { return guidelineColor; }
            set { SetValue(ref guidelineColor, value, () => Controller.AdornerService.Refresh()); }
        }

        public float GuidelineThickness
        {
            get { return guidelineThickness; }
            set { SetValue(ref guidelineThickness, value, () => Controller.AdornerService.Refresh()); }
        }

        public Color HighlightColor
        {
            get { return highlightColor; }
            set { SetValue(ref highlightColor, value, () => Controller.AdornerService.Refresh()); }
        }

        public float HighlightThickness
        {
            get { return highlightThickness; }
            set { SetValue(ref highlightThickness, value, () => Controller.AdornerService.Refresh()); }
        }

        public Color SelectionColor
        {
            get { return selectionColor; }
            set { SetValue(ref selectionColor, value, () => Controller.AdornerService.Refresh()); }
        }

        public float SelectionThickness
        {
            get { return selectionThickness; }
            set { SetValue(ref selectionThickness, value, () => Controller.AdornerService.Refresh()); }
        }

        public Color SizingColor
        {
            get { return sizingColor; }
            set { SetValue(ref sizingColor, value, () => Controller.AdornerService.Refresh()); }
        }

        public float SizingThickness
        {
            get { return sizingThickness; }
            set { SetValue(ref sizingThickness, value, () => Controller.AdornerService.Refresh()); }
        }

        public float SnapValue
        {
            get { return snapValue; }
            set { SetValue(ref snapValue, value); }
        }
        #endregion // Editor Settings

        [NotNull]
        internal new UIBaseViewModel Asset => (UIBaseViewModel)base.Asset;

        [NotNull]
        internal new UIEditorController Controller => (UIEditorController)base.Controller;

        [NotNull]
        public ICommandBase BreakLinkToLibraryCommand { get; }

        [NotNull]
        public ICommandBase CreateLibraryCommand { get; }

        [NotNull]
        public ICommandBase CreatePageCommand { get; }

        /// <summary>
        /// Gets the command to layout the current selection inside a new panel.
        /// </summary>
        [NotNull]
        public ICommandBase GroupIntoCommand { get; }

        [NotNull]
        public ICommandBase OpenLibraryEditorCommand { get; }

        /// <summary>
        /// Gets the command to refresh the list of selectable elements.
        /// </summary>
        /// <seealso cref="SelectableElements"/>
        [NotNull]
        public ICommandBase RefreshSelectableElementsCommand { get; }

        [NotNull]
        public ICommandBase ResetZoomCommand { get; }

        [NotNull]
        public ICommandBase SelectLibraryCommand { get; }

        /// <summary>
        /// Gets the command to set the current selection from the list of <see cref="SelectableElements"/>.
        /// </summary>
        /// <seealso cref="SelectableElements"/>
        [NotNull]
        public ICommandBase SetCurrentSelectionCommand { get; }

        /// <summary>
        /// Gets the command to show the UI properties in the property grid.
        /// </summary>
        [NotNull]
        public ICommandBase ShowPropertiesCommand { get; }

        [NotNull]
        public ICommandBase ZoomInCommand { get; }

        [NotNull]
        public ICommandBase ZoomOutCommand { get; }

        [NotNull]
        public static string GetDisplayName(UIElement element)
        {
            return string.IsNullOrWhiteSpace(element.Name) ? $"[{element.GetType().Name}]" : element.Name;
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(UIEditorBaseViewModel));

            // We should save settings first, before starting to destroy everything
            SaveSettings();

            Session.ActiveAssetView.Assets.CollectionChanged -= AssetsCollectionChanged;

            base.Destroy();
        }

        public void Select(Guid elementId, bool isAdditive)
        {
            var partId = new AbsoluteId(Asset.Id, elementId);
            var element = FindPartViewModel(partId);
            if (element == null)
                return;

            if (SelectedContent.Contains(element))
                return;

            if (!isAdditive)
                ClearSelection();
            SelectedContent.Add(element);
        }

        public void UpdateProperties(Guid elementId, IReadOnlyDictionary<string, object> changes)
        {
            var partId = new AbsoluteId(Asset.Id, elementId);
            var element = (UIElementViewModel)FindPartViewModel(partId);
            if (element == null)
                return;

            var node = NodeContainer.GetNode(element.AssetSideUIElement);
            if (node == null)
                return;

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var kv in changes)
                {
                    var propertyName = kv.Key;
                    var propertyValue = kv.Value;

                    var member = node.TryGetChild(propertyName);
                    if (member == null)
                        continue;

                    // Update properties only when they actually changed
                    var currentValue = member.Retrieve();
                    if (currentValue != propertyValue)
                    {
                        member.Update(propertyValue);
                    }
                }
                UndoRedoService.SetName(transaction, $"Update {element.ElementType.Name}");
            }
        }

        /// <inheritdoc/>
        protected override async Task<bool> InitializeEditor()
        {
            if (!await base.InitializeEditor())
                return false;

            // Initialize the libraries
            InitializeLibraries();
            Session.ActiveAssetView.Assets.CollectionChanged += AssetsCollectionChanged;

            ShowAssetProperties();
            return true;
        }

        /// <inheritdoc/>
        public override AssetCompositeItemViewModel CreatePartViewModel(AssetCompositeHierarchyViewModel<UIElementDesign, UIElement> asset, UIElementDesign partDesign)
        {
            return UIElementViewModelFactory.Instance.ProvideViewModel(this, (UIBaseViewModel)asset, partDesign);
        }

        /// <inheritdoc/>
        protected override Task OnGameContentLoaded()
        {
            LoadSettings();
            return base.OnGameContentLoaded();
        }

        private async void ActiveRootChanged(UIElementViewModel previousRoot, UIElementViewModel newRoot)
        {
            if (previousRoot != null)
            {
                await Controller.InvokeAsync(() =>
                {
                    Controller.HideUI();
                    previousRoot.DisableAdorner().Forget();
                    previousRoot.Children.BreadthFirst(e => e.Children).ForEach(e => e.DisableAdorner().Forget());
                });

            }
            if (newRoot != null)
            {
                var rootId = newRoot.Id.ObjectId;
                await Controller.InvokeAsync(() =>
                {
                    Controller.ShowUI(rootId);
                });
                // FIXME: we would like to call it in the same controller invocation, but because EnableAdorner is awaiting the propagator we have to call it from the main thread
                newRoot.EnableAdorner().Forget();
                newRoot.Children.BreadthFirst(e => e.Children).ForEach(e => e.EnableAdorner().Forget());
            }
        }

        private void AddLibrary(UILibraryViewModel library)
        {
            // Don't add itself as a library or if already referenced
            if (library == Asset || factories.Any(l => l.Category == library.Url))
                return;

            var asset = library.Asset;
            var list = asset.PublicUIElements.Select(x => new UIElementFromLibrary(ServiceProvider, library, x.Key)).OrderBy(x => x.Name);
            factories.AddRange(list);

            if (!library.IsEditable)
                return;

            var node = NodeContainer.GetOrCreateNode(asset)[nameof(UILibraryAsset.PublicUIElements)].Target;
            node.ItemChanged += LibraryContentChanged;
        }

        private void AssetsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    e.OldItems?.OfType<UILibraryViewModel>().ForEach(RemoveLibrary);
                    e.NewItems?.OfType<UILibraryViewModel>().ForEach(AddLibrary);
                    break;

                case NotifyCollectionChangedAction.Move:
                    break;

                case NotifyCollectionChangedAction.Reset:
                    InitializeLibraries();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <inheritdoc />
        protected override bool CanPaste(bool asRoot)
        {
            var copyPasteService = ServiceProvider.TryGet<ICopyPasteService>();
            if (copyPasteService == null)
                return false;

            if (asRoot)
            {
                return PasteAsRootMonitor.Get(() =>
                        copyPasteService.CanPaste(SafeClipboard.GetText(), Asset.AssetType, typeof(AssetCompositeHierarchyData<UIElementDesign, UIElement>), typeof(AssetCompositeHierarchyData<UIElementDesign, UIElement>)));
            }

            var canAddOrInsert = SelectedItems.All(element =>
            {
                var contentControl = element as ContentControlViewModel;
                if (contentControl != null)
                {
                    return contentControl.AssetSideControl.Content == null;
                }
                return element is PanelViewModel;
            });
            if (!canAddOrInsert)
                return false;

            return PasteMonitor.Get(() => copyPasteService.CanPaste(SafeClipboard.GetText(), Asset.AssetType, typeof(UIElement), typeof(AssetCompositeHierarchyData<UIElementDesign, UIElement>)));
        }

        /// <inheritdoc />
        protected override async Task Delete()
        {
            var elementsToDelete = GetCommonRoots(SelectedItems);
            var ask = UIEditorSettings.AskBeforeDeletingUIElements.GetValue();
            if (ask)
            {
                var confirmMessage = Tr._p("Message", "Are you sure you want to delete this UI element?");
                if (elementsToDelete.Count > 1)
                    confirmMessage = string.Format(Tr._p("Message", "Are you sure you want to delete these {0} UI elements?"), elementsToDelete.Count);
                var buttons = DialogHelper.CreateButtons(new[] { Tr._p("Button", "Delete"), Tr._p("Button", "Cancel") }, 1, 2);
                var result = await ServiceProvider.Get<IDialogService>().CheckedMessageBox(confirmMessage, false, DialogHelper.DontAskAgain, buttons, MessageBoxImage.Question);
                if (result.Result != 1)
                    return;
                if (result.IsChecked == true)
                {
                    UIEditorSettings.AskBeforeDeletingUIElements.SetValue(false);
                    UIEditorSettings.Save();
                }
            }

            var hadActiveRoot = elementsToDelete.Any(x => ReferenceEquals(x, ActiveRoot));
            var asset = elementsToDelete.First().Asset; // all selected items are from the same asset
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                ClearSelection();
                HashSet<Tuple<Guid, Guid>> mapping;
                asset.AssetHierarchyPropertyGraph.DeleteParts(elementsToDelete.Select(x => x.UIElementDesign), out mapping);
                var operation = new DeletedPartsTrackingOperation<UIElementDesign, UIElement>(asset, mapping);
                UndoRedoService.PushOperation(operation);
                UndoRedoService.SetName(transaction, "Delete selected UI elements");
            }
            // Clear active root if it was deleted
            if (hadActiveRoot)
                ActiveRoot = null;
        }

        /// <inheritdoc />
        protected override ISet<UIElementViewModel> DuplicateSelection()
        {
            // save elements to copy
            var elementsToDuplicate = GetCommonRoots(SelectedItems);
            // check that the selected elements can be duplicated
            if (elementsToDuplicate.Any(e => !e.CanDuplicate()))
                return elementsToDuplicate;

            // clear selection
            ClearSelection();

            // duplicate the elements
            var duplicatedElements = new HashSet<UIElementViewModel>();
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                duplicatedElements.AddRange(elementsToDuplicate.Select(x => x.Duplicate()));
                UndoRedoService.SetName(transaction, "Duplicate elements");
            }

            // set selection to new copied elements.
            SelectedContent.AddRange(duplicatedElements);

            return duplicatedElements;
        }

        protected override bool CanPasteIntoItem(IPasteResult pasteResult, AssetCompositeItemViewModel item, out string error)
        {
            // try to paste as hierarchy
            var hierarchy = pasteResult.Items[0].Data as AssetCompositeHierarchyData<UIElementDesign, UIElement>;
            if (hierarchy != null)
            {
                var contentControl = item as ContentControlViewModel;
                if (contentControl != null && hierarchy.RootParts.Count > 1)
                {
                    error = $"({contentControl.Name}) can only have one child as content. {hierarchy.RootParts.Count} elements can't be pasted into it.";
                    return false;
                }
            }

            return base.CanPasteIntoItem(pasteResult, item, out error);
        }

        /// <inheritdoc />
        protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
        {
            base.SelectedContentCollectionChanged(action);

            if (action == NotifyCollectionChangedAction.Reset)
            {
                Controller.AdornerService.ClearSelection().Forget();
                return;
            }

            LastSelectedElement = SelectedItems.LastOrDefault();
            if (LastSelectedElement != null)
            {
                // Update active root
                ActiveRoot = LastSelectedElement.GetRootElementViewModel();
            }
            if (ActiveRoot != null)
            {
                // Select adorners (for the active root only)
                var elements = SelectedItems.Where(e => e.AssetSideUIElement.FindVisualRoot().Id == ActiveRoot.Id.ObjectId);
                elements.ForEach(e => e.SelectAdorner().Forget());
            }
        }

        /// <inheritdoc />
        protected override void UpdateCommands()
        {
            base.UpdateCommands();

            var allSiblings = SelectedItems.AllSiblings();
            var atLeastOne = SelectedItems.Count >= 1;
            var exactlyOne = SelectedItems.Count == 1;

            CreateLibraryCommand.IsEnabled = atLeastOne;
            CreatePageCommand.IsEnabled = exactlyOne;
            DuplicateSelectionCommand.IsEnabled = atLeastOne && GetCommonRoots(SelectedItems).All(e => e.CanDuplicate());
            GroupIntoCommand.IsEnabled = atLeastOne && allSiblings;

            var libraryInstanceSelected = SelectedItems.Any(x => x.SourceLibrary != null);
            BreakLinkToLibraryCommand.IsEnabled = libraryInstanceSelected;
            OpenLibraryEditorCommand.IsEnabled = libraryInstanceSelected;
            SelectLibraryCommand.IsEnabled = libraryInstanceSelected && exactlyOne;
        }

        /// <inheritdoc />
        protected override Task RefreshEditorProperties()
        {
            EditorProperties.UpdateTypeAndName(SelectedItems, e => e.ElementType.Name, e => e.AssetSideUIElement.Name, "elements");
            return EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems);
        }

        private void BreakLinkToLibrary()
        {
            BreakLinkToBase("library");
        }

        private void CreateLibraryFromSelection()
        {
            Dictionary<AssetCompositeHierarchyViewModel<UIElementDesign, UIElement>, Dictionary<Guid, Guid>> idRemappings;
            var assetItem = CreateAssetFromSelectedParts(() => new UILibraryAsset { Design = new UIAssetBase.UIDesign { Resolution = UIAsset.Design.Resolution } }, e => e?.Name ?? "UILibrary", true, out idRemappings);
            if (assetItem != null)
            {
                var idRemapping = idRemappings[Asset];
                var namedRootElements = GetCommonRoots(SelectedItems).Where(e => !string.IsNullOrWhiteSpace(e.Name));
                ((UILibraryAsset)assetItem.Asset).PublicUIElements.AddRange(namedRootElements.ToDictionary(e => idRemapping[e.Id.ObjectId], e => e.Name));
            }
        }

        private void CreatePageFromSelection()
        {
            if (SelectedItems.Count != 1)
                return;

            Dictionary<AssetCompositeHierarchyViewModel<UIElementDesign, UIElement>, Dictionary<Guid, Guid>> idRemappings;
            CreateAssetFromSelectedParts(() => new UIPageAsset { Design = new UIAssetBase.UIDesign { Resolution = UIAsset.Design.Resolution } }, e => e?.Name ?? "UIPage", false, out idRemappings);
        }

        /// <summary>
        /// Retrieves an enumeration of elements at the given UI-space position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<UIElementViewModel> GetElementsAtPosition(ref Vector3 worldPosition)
        {
            return Controller.AdornerService.GetElementIdsAtPosition(ref worldPosition).Select(id => FindPartViewModel(new AbsoluteId(Asset.Id, id))).OfType<UIElementViewModel>();
        }

        private void GroupInto(IUIElementFactory factory)
        {
            var targetPanelType = (factory as UIElementFromSystemLibrary)?.Type;
            if (targetPanelType == null)
                throw new NotSupportedException("Grouping elements into a user library type isn't supported.");
            if (!typeof(Panel).IsAssignableFrom(targetPanelType))
                throw new ArgumentException(@"The target type isn't a panel", nameof(targetPanelType));

            if (SelectedContent.Count == 0)
                return;

            // Ensure that the selected elements are sibling.
            var allParents = SelectedItems.Select(x => x.Parent).OfType<UIHierarchyItemViewModel>().ToList();
            var parent = allParents[0];
            if (allParents.Any(x => x != parent))
                throw new InvalidOperationException("This operation can only be executed on a selection of sibling elements.");

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var children = SelectedItems.ToList();
                // Create the new panel into which we'll insert the selection
                var newPanel = (Panel)Activator.CreateInstance(targetPanelType);
                var newPanelDesign = new UIElementDesign(newPanel);
                // Create a hierarchy containing all children and the panel
                var hierarchy = UIAssetPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, children.Select(c => c.Id.ObjectId), SubHierarchyCloneFlags.None, out _);
                hierarchy.RootParts.Add(newPanel);
                hierarchy.Parts.Add(newPanelDesign);
                // Remove all children from their partDesign panel.
                foreach (var child in children)
                {
                    child.Asset.AssetHierarchyPropertyGraph.RemovePartFromAsset(child.UIElementDesign);
                }
                // Add the new panel in place of the selected content.
                parent.Asset.InsertUIElement(hierarchy.Parts, newPanelDesign, (parent as UIElementViewModel)?.AssetSideUIElement);
                // Add the children into the new panel.
                foreach (var child in children)
                {
                    parent.Asset.InsertUIElement(hierarchy.Parts, hierarchy.Parts[child.Id.ObjectId], newPanel);
                }
                UndoRedoService.SetName(transaction, $"Group into {targetPanelType.Name}");
            }
        }

        private void InitializeLibraries()
        {
            factories.Clear();

            // FIXME: only retrieve from current package and its dependencies
            var allLibraries = Session.AllPackages
                .GroupBy(p => p.Package.IsSystem)
                .ToDictionary(p => p.Key, p => p.SelectMany(x => x.Assets).OfType<UILibraryViewModel>());

            // system libraries
            foreach (var sysLib in allLibraries[true])
            {
                var asset = sysLib.Asset;
                factories.AddRange(asset.PublicUIElements.Select(x => new UIElementFromSystemLibrary(ServiceProvider, sysLib, x.Key)));
            }
            PanelFactories = factories.Where(f => f.Category == "Panel").OrderBy(f => f.Name).ToList();

            // user libraries
            foreach (var userLib in allLibraries[false])
            {
                RemoveLibrary(userLib);
                AddLibrary(userLib);
            }
        }

        private void LibraryContentChanged(object sender, ItemChangeEventArgs e)
        {
            if (e.ChangeType != ContentChangeType.CollectionAdd && e.ChangeType != ContentChangeType.CollectionRemove)
                return;

            var node = (IAssetNode)e.Collection;
            var assetId = node.PropertyGraph.RootNode[nameof(Asset.Id)].Retrieve();
            if (assetId == null)
                return;

            var viewModel = Session.GetAssetById((AssetId)assetId) as UILibraryViewModel;
            if (viewModel == null)
                return;

            var index = (Guid)e.Index.Value;
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                factories.Add(new UIElementFromLibrary(ServiceProvider, viewModel, index));
            }
            else
            {
                factories.RemoveWhere(f => (f as UIElementFromLibrary)?.Id == index);
            }
        }

        private void LoadSettings()
        {
            try
            {
                var userSettings = Asset.Directory.Package.UserSettings;
                var sceneSettingsCollection = userSettings.GetValue(PackageSceneSettings.SceneSettings);
                SceneSettingsData settings;
                if (!sceneSettingsCollection.TryGetValue(Asset.Asset.Id, out settings))
                {
                    // Fall back to default settings
                    settings = SceneSettingsData.CreateDefault();
                    settings.CamProjection = CameraProjectionMode.Orthographic;
                    settings.CamPosition = UIEditorGameCameraService.DefaultPosition;
                    settings.CamPitchYaw = new Vector2(UIEditorGameCameraService.DefaultPitch, UIEditorGameCameraService.DefaultYaw);
                }
                LoadSettings(settings);
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private void LoadSettings(SceneSettingsData settings)
        {
            Camera.LoadSettings(settings);
        }

        private void OpenLibraryEditor()
        {
            var libraries = new HashSet<UILibraryViewModel>(SelectedItems.Select(x => x.SourceLibrary).NotNull());
            foreach (var library in libraries)
            {
                ServiceProvider.Get<IEditorDialogService>().AssetEditorsManager.OpenAssetEditorWindow(library);
            }
        }

        /// <summary>
        /// Refreshes the list of elements that can be set as current selection.
        /// </summary>
        private void RefreshSelectableElements()
        {
            selectableElements.Clear();

            var worldPosition = Controller.GetMousePositionInUI();
            var elements = GetElementsAtPosition(ref worldPosition);
            if (elements != null)
            {
                selectableElements.AddRange(elements);
            }
        }

        private void RemoveLibrary(UILibraryViewModel library)
        {
            factories.RemoveWhere(l => l.Category == library.Url);

            var node = NodeContainer.GetNode(library.Asset)[nameof(UILibraryAsset.PublicUIElements)].Target;
            node.ItemChanged -= LibraryContentChanged;
        }

        private void ResetZoom()
        {
            var service = Controller.GetService<IEditorGameCameraViewModelService>();
            service?.SetOrthographicSize(CameraComponent.DefaultOrthographicSize);
        }

        private void SaveSettings()
        {
            try
            {
                var userSettings = Asset.Directory.Package.UserSettings;
                var sceneSettingsCollection = userSettings.GetValue(PackageSceneSettings.SceneSettings);
                SceneSettingsData sceneSettings;
                if (!sceneSettingsCollection.TryGetValue(Asset.Asset.Id, out sceneSettings))
                {
                    // Create new settings
                    sceneSettings = SceneSettingsData.CreateDefault();
                    sceneSettingsCollection.Add(Asset.Asset.Id, sceneSettings);
                }

                SaveSettings(sceneSettings);

                // FIXME: it would be better to just set the one scene settings data instead of the whole collection.
                userSettings.SetValue(PackageSceneSettings.SceneSettings, sceneSettingsCollection);
                Asset.Directory.Package.UserSettings.Save();
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private void SaveSettings(SceneSettingsData settings)
        {
            Camera.SaveSettings(settings);
        }

        private void SelectLibrary()
        {
            var library = LastSelectedElement?.SourceLibrary;
            if (library != null)
                Session.ActiveAssetView.SelectAssets(library.Yield());
        }

        private void SetCurrentSelection(AbsoluteId elementId)
        {
            var element = FindPartViewModel(elementId);
            if (element == null)
                return;

            ClearSelection();

            SelectedContent.Add(element);
        }

        private void Zoom(float amount)
        {
            var service = Controller.GetService<IEditorGameCameraViewModelService>() as UIEditorGameCameraService;
            if (service == null)
                return;
            var newOrthographicSize = service.Component.OrthographicSize + amount;
            if (newOrthographicSize > 0)
            {
                ((IEditorGameCameraViewModelService)service).SetOrthographicSize(newOrthographicSize);
            }
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            var worldPosition = Controller.GetMousePositionInUI();
            var element = GetDropTarget(ref worldPosition, children, modifiers, out message);
            if (element != null)
            {
                Controller.AdornerService.HighlightAdorner(element.Id.ObjectId).Forget();
                return true;
            }

            Controller.AdornerService.UnlitAllAdorners().Forget();
            return false;
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var worldPosition = Controller.GetMousePositionInUI();
            var target = GetDropTarget(ref worldPosition, children, modifiers, out string _);

            ((IAddChildViewModel)target)?.AddChildren(children, modifiers);
        }

        private UIElementViewModel GetDropTarget(ref Vector3 worldPosition, IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            message = DragDropBehavior.InvalidDropAreaMessage;

            var elements = GetElementsAtPosition(ref worldPosition);
            if (elements == null)
                return null;

            foreach (var target in elements)
            {
                if (((IAddChildViewModel)target).CanAddChildren(children, modifiers, out message))
                    return target;
            }
            return null;
        }
    }
}
