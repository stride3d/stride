// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Interop;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.SceneEditor.ViewModels;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.View;
using Stride.Assets.Presentation.ViewModel;
using Stride.Assets.Presentation.ViewModel.CopyPasteProcessors;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    /// <summary>
    /// Base class for the view model of an <see cref="EntityHierarchyViewModel"/> editor.
    /// </summary>
    public abstract class EntityHierarchyEditorViewModel : AssetCompositeHierarchyEditorViewModel<EntityDesign, Entity, EntityViewModel>, IAddChildViewModel
    {
        private static readonly ILogger EditorLogger = GlobalLogger.GetLogger("Scene");
        private EntityHierarchyRootViewModel activeRoot;
        private readonly IDebugPage debugPage;
        private string filterPattern;
        private List<string> filterTokens;
        private bool compilingAssets;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityHierarchyEditorViewModel"/> class.
        /// </summary>
        /// <param name="asset">The asset related to this editor.</param>
        /// <param name="controllerFactory">A factory to create the associated <see cref="IEditorGameController"/>.</param>
        protected EntityHierarchyEditorViewModel([NotNull] EntityHierarchyViewModel asset, [NotNull] Func<GameEditorViewModel, IEditorGameController> controllerFactory)
            : base(asset, controllerFactory)
        {
            Controller.Loader.AssetLoading += (s, e) => asset.Dispatcher.InvokeAsync(() => CompilingAssets = e.ContentLoadingCount > 0);
            Controller.Loader.AssetLoaded += (s, e) => asset.Dispatcher.InvokeAsync(() => CompilingAssets = e.ContentLoadingCount > 0);
            Camera = new EditorCameraViewModel(ServiceProvider, Controller);
            Transform = new EntityTransformationViewModel(ServiceProvider, Controller);
            Grid = new EditorGridViewModel(ServiceProvider, Controller);
            Navigation = new EditorNavigationViewModel(ServiceProvider, Controller, this);
            Lighting = new EditorLightingViewModel(ServiceProvider, Controller, this);
            Rendering = new EditorRenderingViewModel(ServiceProvider, Controller);
            EntityGizmos = new EntityGizmosViewModel(ServiceProvider, Controller);
            CreateEntityCommand = new AnonymousTaskCommand<IEntityFactory>(ServiceProvider, x => CreateEntity(true, x, ActiveRoot ?? HierarchyRoot));
            CreateEntityInRootCommand = new AnonymousTaskCommand<IEntityFactory>(ServiceProvider, x => CreateEntity(false, x, ActiveRoot ?? HierarchyRoot));
            CreateFolderInRootCommand = new AnonymousCommand<IEntityFactory>(ServiceProvider, x => CreateFolder(HierarchyRoot.Asset, ActiveRoot ?? HierarchyRoot, true));
            CreateEntityInSelectionCommand = new AnonymousTaskCommand<IEntityFactory>(ServiceProvider, x => CreateEntity(false, x, (EntityHierarchyItemViewModel)SelectedContent.FirstOrDefault() ?? ActiveRoot ?? HierarchyRoot));
            CreateFolderInSelectionCommand = new AnonymousCommand<IEntityFactory>(ServiceProvider, x =>
            {
                var element = (EntityHierarchyItemViewModel)SelectedContent.FirstOrDefault() ?? ActiveRoot ?? HierarchyRoot;
                CreateFolder(element.Asset, element, true);
            });
            OpenPrefabEditorCommand = new AnonymousCommand(ServiceProvider, OpenPrefabEditor);
            SelectPrefabCommand = new AnonymousCommand(ServiceProvider, SelectPrefab);
            SetActiveRootCommand = new AnonymousCommand(ServiceProvider, SetActiveRoot);
            BreakLinkToPrefabCommand = new AnonymousCommand(ServiceProvider, BreakLinkToPrefab);
            CreatePrefabFromSelectionCommand = new AnonymousCommand(ServiceProvider, CreatePrefabFromSelection);
            UpdateCommands();
            debugPage = new DebugEntityHierarchyEditorUserControl(this);
            EditorDebugTools.RegisterDebugPage(debugPage);
        }

        public EntityHierarchyRootViewModel ActiveRoot { get => activeRoot; protected internal set => SetValue(ref activeRoot, value); }

        public ILogger Logger => EditorLogger;

        public EntityHierarchyRootViewModel HierarchyRoot => (EntityHierarchyRootViewModel)RootPart;

        [CanBeNull]
        public EntityViewModel EntityWithGizmo => SelectedItems.LastOrDefault();

        public bool DisplaySelectionMask { get => Selection.DisplaySelectionMask; set { SetValue(Selection.DisplaySelectionMask != value, () => Selection.DisplaySelectionMask = value); } }

        public string FilterPattern { get => filterPattern; set => SetValue(ref filterPattern, value, UpdateVisibilities); }

        public bool CompilingAssets { get => compilingAssets; set => SetValue(ref compilingAssets, value); }

        [NotNull]
        public EditorCameraViewModel Camera { get; }

        [NotNull]
        public EntityTransformationViewModel Transform { get; }

        [NotNull]
        public EditorGridViewModel Grid { get; }

        [NotNull]
        public EditorNavigationViewModel Navigation { get; }

        [NotNull]
        public EditorRenderingViewModel Rendering { get; }

        [NotNull]
        public EditorLightingViewModel Lighting { get; }

        [NotNull]
        public EntityGizmosViewModel EntityGizmos { get; }

        public bool MaterialSelectionMode { get => MaterialHighlight.IsActive; set { SetValue(MaterialHighlight.IsActive != value, () => MaterialHighlight.IsActive = value); } }

        [NotNull]
        public ICommandBase CreateEntityCommand { get; }

        [NotNull]
        public ICommandBase CreateEntityInRootCommand { get; }

        [NotNull]
        public ICommandBase CreateFolderInRootCommand { get; }

        [NotNull]
        public ICommandBase CreateEntityInSelectionCommand { get; }

        [NotNull]
        public ICommandBase CreateFolderInSelectionCommand { get; }

        [NotNull]
        public ICommandBase OpenPrefabEditorCommand { get; }

        [NotNull]
        public ICommandBase SelectPrefabCommand { get; }

        [NotNull]
        public ICommandBase SetActiveRootCommand { get; }

        [NotNull]
        public ICommandBase BreakLinkToPrefabCommand { get; }

        [NotNull]
        public ICommandBase CreatePrefabFromSelectionCommand { get; }

        [NotNull]
        protected new EntityHierarchyViewModel Asset => (EntityHierarchyViewModel)base.Asset;

        // TODO: turn private, create a service getter that accepts only IEditorGameViewModelService
        [NotNull]
        protected internal new EntityHierarchyEditorController Controller => (EntityHierarchyEditorController)base.Controller;

        private IEditorGameMaterialHighlightViewModelService MaterialHighlight => Controller.GetService<IEditorGameMaterialHighlightViewModelService>();

        private IEditorGameSelectionViewModelService Selection => Controller.GetService<IEditorGameSelectionViewModelService>();

        /// <inheritdoc />
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(EntityHierarchyEditorViewModel));

            // We should save settings first, before starting to dispose everything
            SaveSettings();

            EditorDebugTools.UnregisterDebugPage(debugPage);

            Session.ActiveAssetView.SelectedAssets.CollectionChanged -= SelectedAssetsChanged;

            // Unregister editor view models
            Navigation.Destroy();

            base.Destroy();
        }

        /// <inheritdoc />
        public override AssetCompositeItemViewModel CreatePartViewModel(AssetCompositeHierarchyViewModel<EntityDesign, Entity> asset, EntityDesign partDesign)
        {
            return new EntityViewModel(this, (EntityHierarchyViewModel)asset, partDesign);
        }

        [NotNull]
        public ISet<EntityViewModel> DuplicateSelectedEntities()
        {
            // save elements to copy and remove them from current selection.
            var entitiesToDuplicate = GetCommonRoots(SelectedItems);
            if (entitiesToDuplicate.Count == 0)
                return entitiesToDuplicate;

            SelectedItems.Clear();

            // duplicate the elements
            HashSet<EntityViewModel> duplicatedAssets;
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                duplicatedAssets = new HashSet<EntityViewModel>(entitiesToDuplicate.Select(x => x.Duplicate()));
                UndoRedoService.SetName(transaction, "Duplicate entities");
            }

            // set selection to new copied elements.
            SelectedItems.AddRange(duplicatedAssets);

            return duplicatedAssets;
        }

        [NotNull]
        public static EntityHierarchyRootViewModel GetRoot([NotNull] EntityViewModel entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            var rootEntity = entity;
            EntityViewModel parentEntity;
            while ((parentEntity = rootEntity.TransformParent as EntityViewModel) != null)
            {
                rootEntity = parentEntity;
            }
            if (rootEntity.TransformParent == null)
                throw new InvalidOperationException($"{entity} is not contained in a hierarchy.");
            return (EntityHierarchyRootViewModel)rootEntity.TransformParent;
        }

        public void UpdateTransformations([NotNull] IReadOnlyDictionary<AbsoluteId, TransformationTRS> transformations)
        {
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                foreach (var transformation in transformations)
                {
                    var element = (EntityHierarchyElementViewModel)FindPartViewModel(transformation.Key);
                    UpdateTransformations(element, transformation.Value);
                }

                UndoRedoService.SetName(transaction, "Update transformation");
            }
        }

        protected virtual void UpdateTransformations(EntityHierarchyElementViewModel element, TransformationTRS transformation)
        {
            var entity = element as EntityViewModel;
            if (entity == null)
                return;

            var node = NodeContainer.GetNode(entity.AssetSideEntity.Transform);

            // Update properties only when they actually changed
            var oldPosition = (Vector3)node[nameof(TransformComponent.Position)].Retrieve();
            if (oldPosition != transformation.Position)
                node[nameof(TransformComponent.Position)].Update(transformation.Position);

            var oldRotation = (Quaternion)node[nameof(TransformComponent.Rotation)].Retrieve();
            if (oldRotation != transformation.Rotation)
                node[nameof(TransformComponent.Rotation)].Update(transformation.Rotation);

            var oldScale = (Vector3)node[nameof(TransformComponent.Scale)].Retrieve();
            if (oldScale != transformation.Scale)
                node[nameof(TransformComponent.Scale)].Update(transformation.Scale);
        }

        /// <inheritdoc />
        protected override object GetObjectToSelect(AbsoluteId id)
        {
            if (id == HierarchyRoot.Id)
                return HierarchyRoot;

            foreach (var item in HierarchyRoot.Children.BreadthFirst(x => x.Children))
            {
                var element = item as EntityHierarchyElementViewModel;
                if (id == element?.Id)
                    return element;

                var folder = item as EntityFolderViewModel;
                if (id == folder?.Id)
                {
                    return folder;
                }
            }
            return null;
        }

        /// <inheritdoc />
        protected override AbsoluteId? GetSelectedObjectId(object obj)
        {
            return (obj as EntityHierarchyElementViewModel)?.Id ?? (obj as EntityFolderViewModel)?.Id;
        }

        /// <inheritdoc />
        protected override async Task<bool> InitializeEditor()
        {
            Session.ActiveAssetView.SelectedAssets.CollectionChanged += SelectedAssetsChanged;
            if (!await base.InitializeEditor())
                return false;

            return true;
        }

        /// <inheritdoc />
        protected override Task OnGameContentLoaded()
        {
            LoadSettings();
            Rendering.RenderMode = EditorRenderMode.DefaultEditor;

            return base.OnGameContentLoaded();
        }

        protected virtual void LoadSettings([NotNull] SceneSettingsData settings)
        {
            Camera.LoadSettings(settings);
            Transform.LoadSettings(settings);
            EntityGizmos.LoadSettings(settings);
            DisplaySelectionMask = settings.SelectionMaskVisible;
            Grid.IsVisible = settings.GridVisible;
            Grid.Color = settings.GridColor;
            Lighting.LightProbeWireframeVisible = settings.LightProbeWireframe;
            Lighting.LightProbeBounces = settings.LightProbeBounces;
            MaterialSelectionMode = false;
            Navigation.LoadSettings(settings);
            Rendering.LoadSettings(settings);
        }

        protected virtual void SaveSettings([NotNull] SceneSettingsData settings)
        {
            Camera.SaveSettings(settings);
            Transform.SaveSettings(settings);
            settings.SelectionMaskVisible = DisplaySelectionMask;
            settings.GridVisible = Grid.IsVisible;
            settings.GridColor = Grid.Color;
            settings.LightProbeWireframe = Lighting.LightProbeWireframeVisible;
            settings.LightProbeBounces = Lighting.LightProbeBounces;
            Navigation.SaveSettings(settings);
            Rendering.SaveSettings(settings);
            EntityGizmos.SaveSettings(settings);
        }

        /// <inheritdoc />
        protected override async Task RefreshEditorProperties()
        {
            EditorProperties.UpdateTypeAndName(SelectedItems, x => "Entity", x => x.Name, "entities");
            await EditorProperties.GenerateSelectionPropertiesAsync(SelectedItems);
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
                }
                LoadSettings(settings);
            }
            catch (Exception e)
            {
                e.Ignore();
            }

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

        private async Task CreateEntity(bool atMousePosition, [CanBeNull] IEntityFactory factory, [NotNull] EntityHierarchyItemViewModel parent)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var entity = factory != null ? await factory.CreateEntity(parent) : new Entity { Name = EntityFactory.ComputeNewName(parent, "Entity") };

            if (entity != null)
            {
                // Create item ids collections for new entity before actually adding them to the asset.
                AssetCollectionItemIdHelper.GenerateMissingItemIds(entity);

                if (atMousePosition)
                {
                    var position = Controller.GetMousePositionInScene(true);
                    entity.Transform.Position = position;
                }

                // TODO: this can be easily supported by calling the other override of InsertAssetEntity. IEntityFactory and all implementations must be modified to return a hierarchy
                if (entity.Transform.Children.Count > 0)
                    throw new InvalidOperationException("Entity factories should create single entity. Creating a hierarchy is not supported.");

                var collection = new AssetPartCollection<EntityDesign, Entity> { new EntityDesign(entity, (parent as EntityFolderViewModel)?.Path ?? "") };

                using (var transaction = UndoRedoService.CreateTransaction())
                {
                    parent.Asset.AssetHierarchyPropertyGraph.AddPartToAsset(collection, collection.Single().Value, (parent.Owner as EntityViewModel)?.AssetSideEntity, parent.Owner.EntityCount);
                    UndoRedoService.SetName(transaction, $"Create entity '{entity.Name}'");
                }
                // Select the newly created entity
                var partId = new AbsoluteId(parent.Asset.Id, entity.Id);
                var newEntity = (EntityViewModel)FindPartViewModel(partId);
                ClearSelection();
                SelectedContent.Add(newEntity);
                newEntity.IsEditing = true;
            }
        }

        [NotNull]
        internal EntityFolderViewModel CreateFolder([NotNull] EntityHierarchyViewModel asset, [NotNull] EntityHierarchyItemViewModel parent, bool editName)
        {
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            EntityFolderViewModel folder;
            var transaction = UndoRedoService.UndoRedoInProgress ? null : UndoRedoService.CreateTransaction();
            try
            {
                folder = new EntityFolderViewModel(parent.Editor, parent.Asset, NamingHelper.ComputeNewName("Folder", parent.Folders, x => x.Name), Enumerable.Empty<EntityDesign>());
                parent.Folders.Add(folder);
                parent.IsExpanded = true;

                if (transaction != null)
                {
                    var operation = new EntityFolderOperation(asset, EntityFolderOperation.Action.FolderCreated, folder.Path, parent.Owner.Id);
                    UndoRedoService.PushOperation(operation);
                    UndoRedoService.SetName(transaction, $"Create folder '{folder.Name}'");
                }
            }
            finally
            {
                transaction?.Dispose();
            }

            // Select the newly created entity
            ClearSelection();
            SelectedContent.Add(folder);

            folder.IsEditing = editName;

            return folder;
        }

        /// <inheritdoc />
        protected override bool CanDelete()
        {
            return SelectedContent.Count > 0 && !SelectedContent.Contains(HierarchyRoot);
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
                    copyPasteService.CanPaste(SafeClipboard.GetText(), Asset.AssetType, typeof(AssetCompositeHierarchyData<EntityDesign, Entity>),
                        typeof(AssetCompositeHierarchyData<EntityDesign, Entity>), typeof(EntityComponent)));
            }
            return PasteMonitor.Get(() =>
                copyPasteService.CanPaste(SafeClipboard.GetText(), Asset.AssetType, typeof(Entity),
                    typeof(AssetCompositeHierarchyData<EntityDesign, Entity>), typeof(EntityComponent)));
        }

        /// <inheritdoc />
        protected override async Task Delete()
        {
            var entitiesToDelete = GetCommonRoots(SelectedItems);
            var ask = SceneEditorSettings.AskBeforeDeletingEntities.GetValue();
            if (ask)
            {
                var confirmMessage = Tr._p("Message", "Are you sure you want to delete this entity?");
                // TODO: we should compute the actual total number of entities to be deleted here (children recursively, etc.)
                if (entitiesToDelete.Count > 1)
                    confirmMessage = string.Format(Tr._p("Message", "Are you sure you want to delete these {0} entities?"), entitiesToDelete.Count);
                var checkedMessage = string.Format(Stride.Core.Assets.Editor.Settings.EditorSettings.AlwaysDeleteWithoutAsking, "entities");
                var buttons = DialogHelper.CreateButtons(new[] { Tr._p("Button", "Delete"), Tr._p("Button", "Cancel") }, 1, 2);
                var result = await ServiceProvider.Get<IDialogService>().CheckedMessageBox(confirmMessage, false, checkedMessage, buttons, MessageBoxImage.Question);
                if (result.Result != 1)
                    return;
                if (result.IsChecked == true)
                {
                    SceneEditorSettings.AskBeforeDeletingEntities.SetValue(false);
                    SceneEditorSettings.Save();
                }
            }

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var foldersToDelete = SelectedContent.OfType<EntityFolderViewModel>().ToList();
                ClearSelection();

                // Delete entities first
                var entitiesPerScene = entitiesToDelete.GroupBy(x => x.Asset);
                foreach (var entities in entitiesPerScene)
                {
                    HashSet<Tuple<Guid, Guid>> mapping;
                    entities.Key.AssetHierarchyPropertyGraph.DeleteParts(entities.Select(x => x.EntityDesign), out mapping);
                    var operation = new DeletedPartsTrackingOperation<EntityDesign, Entity>(entities.Key, mapping);
                    UndoRedoService.PushOperation(operation);
                }

                // Then folders
                foreach (var folder in foldersToDelete)
                {
                    folder.Delete();
                }

                UndoRedoService.SetName(transaction, "Delete selected entities");
            }
        }

        /// <inheritdoc />
        protected override ISet<EntityViewModel> DuplicateSelection() => DuplicateSelectedEntities();

        /// <inheritdoc />
        protected override void PrepareToCopy(AssetCompositeHierarchyData<EntityDesign, Entity> clonedHierarchy, ICollection<AssetCompositeItemViewModel> commonRoots, ICollection<EntityViewModel> commonParts)
        {
            // First clear all folder information
            foreach (var entity in clonedHierarchy.Parts.Values)
            {
                entity.Folder = null;
            }

            // Then add folder information only if we are copying a folder
            foreach (var item in commonRoots.OfType<EntityFolderViewModel>())
            {
                var parentFolderPathLength = 1 + ((item.Parent as EntityFolderViewModel)?.Path.Length ?? -1);
                foreach (var entity in item.InnerSubEntities)
                {
                    clonedHierarchy.Parts[entity.Id.ObjectId].Folder = entity.Asset.Asset.Hierarchy.Parts[entity.Id.ObjectId].Folder.Substring(parentFolderPathLength);
                }
            }
            base.PrepareToCopy(clonedHierarchy, commonRoots, commonParts);
        }

        /// <inheritdoc />
        protected override void AttachPropertiesForPaste(ref PropertyContainer propertyContainer, AssetCompositeItemViewModel pasteTarget)
        {
            var folder = (pasteTarget as EntityFolderViewModel)?.Path;
            if (!string.IsNullOrEmpty(folder))
            {
                propertyContainer.Add(EntityHierarchyPasteProcessor.TargetFolderKey, folder);
            }
            base.AttachPropertiesForPaste(ref propertyContainer, pasteTarget);
        }

        /// <inheritdoc />
        protected override async Task Paste(bool asRoot)
        {
            if (!asRoot)
            {
                await base.Paste(false);
                return;
            }

            using (var transaction = UndoRedoService.CreateTransaction())
            {
                // Attempt to paste at the active root level
                var root = ActiveRoot ?? HierarchyRoot;
                await PasteIntoItems(root.Yield());
                var actionName = $"Paste into {root.Asset.Name}";

                UndoRedoService.SetName(transaction, actionName);
            }
        }

        /// <inheritdoc />
        protected override void SelectedContentCollectionChanged(NotifyCollectionChangedAction action)
        {
            SelectedItems.Clear();
            SelectedItems.AddRange(SelectedContent.Cast<EntityHierarchyItemViewModel>().SelectMany(x => x.InnerSubEntities));
            OnPropertyChanging(nameof(EntityWithGizmo));
            OnPropertyChanged(nameof(EntityWithGizmo));
        }

        /// <inheritdoc />
        protected override void SelectedItemsCollectionChanged(NotifyCollectionChangedAction action)
        {
            base.SelectedItemsCollectionChanged(action);

            OnPropertyChanging(nameof(EntityWithGizmo));
            OnPropertyChanged(nameof(EntityWithGizmo));
        }

        /// <inheritdoc />
        protected override void UpdateCommands()
        {
            base.UpdateCommands();

            var atLeastOne = SelectedItems.Count >= 1;
            var exactlyOne = SelectedItems.Count == 1;

            CreatePrefabFromSelectionCommand.IsEnabled = atLeastOne;
            var newSelection = SelectedContent.Cast<EntityHierarchyItemViewModel>().ToList();
            DuplicateSelectionCommand.IsEnabled = GetCommonRoots(newSelection).Count > 0;

            var prefabInstanceSelected = SelectedItems.Any(x => x.SourcePrefab != null);
            BreakLinkToPrefabCommand.IsEnabled = prefabInstanceSelected;
            OpenPrefabEditorCommand.IsEnabled = prefabInstanceSelected;
            SelectPrefabCommand.IsEnabled = prefabInstanceSelected && exactlyOne;
        }

        private void CreatePrefabFromSelection()
        {
            Dictionary<AssetCompositeHierarchyViewModel<EntityDesign, Entity>, Dictionary<Guid, Guid>> idRemappings;
            CreateAssetFromSelectedParts(() => new PrefabAsset(), e => e?.Name ?? "Prefab", true, out idRemappings);
        }

        private void SelectedAssetsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            Controller.GetService<IEditorGameAssetHighlighterViewModelService>()?.HighlightAssets(Session.ActiveAssetView.SelectedAssets);
        }

        // TODO: this is code for re-selection after undo/redo, it is here for reference and can be removed once the feature is re-implemented
        //private void UndoRedoExecuted(object sender, ActionItemsEventArgs<IActionItem> e)
        //{
        //    if (Session.ActiveProperties == EditorProperties)
        //    {
        //        var entitiesToSelect = new HashSet<EntityViewModel>();

        //        var aggregate = e.ActionItems.First() as IAggregateActionItem;
        //        var viewModelActionItem = e.ActionItems.First() as DirtiableActionItem;
        //        if (aggregate != null)
        //        {
        //            var actionItems = aggregate.GetInnerActionItems();
        //            // TODO: find correct action items in a different way!
        //            foreach (var entity in actionItems.OfType<DirtiableActionItem>().SelectMany(x => x.Dirtiables).OfType<EntityViewModel>().Where(x => !x.IsDeleted))
        //                entitiesToSelect.Add(entity);
        //        }
        //        else if (viewModelActionItem != null)
        //        {
        //            foreach (var entity in viewModelActionItem.Dirtiables.OfType<EntityViewModel>().Where(x => !x.IsDeleted))
        //                entitiesToSelect.Add(entity);
        //        }

        //        // Change selection if the affected assets are not all already selected.
        //        if (entitiesToSelect.Any(x => !SelectedEntities.Contains(x)))
        //        {
        //            SelectedEntities.Clear();
        //            SelectedEntities.AddRange(entitiesToSelect);
        //        }
        //    }
        //}

        private void UpdateVisibilities()
        {
            filterTokens = filterPattern.Split(" \t\n\r".ToCharArray(), StringSplitOptions.RemoveEmptyEntries).Select(x => x.ToLowerInvariant()).ToList();
            foreach (var content in HierarchyRoot.Children)
            {
                UpdateVisibilities(content);
            }
        }

        private void UpdateVisibilities([NotNull] EntityHierarchyItemViewModel entity)
        {
            var newValue = false;
            foreach (var subEntity in entity.Children)
            {
                UpdateVisibilities(subEntity);
                if (subEntity.IsVisible)
                    newValue = true;
            }
            if (!newValue)
                newValue = filterTokens.Count == 0 || filterTokens.All(x => entity.Name.ToLowerInvariant().Contains(x));

            entity.IsVisible = newValue;
        }

        private void OpenPrefabEditor()
        {
            var prefabs = new HashSet<PrefabViewModel>(SelectedItems.Select(x => x.SourcePrefab).NotNull());
            foreach (var prefab in prefabs)
            {
                ServiceProvider.Get<IEditorDialogService>().AssetEditorsManager.OpenAssetEditorWindow(prefab);
            }
        }

        private void SelectPrefab()
        {
            var prefab = EntityWithGizmo?.SourcePrefab;
            if (prefab != null)
                Session.ActiveAssetView.SelectAssets(prefab.Yield());
        }

        private void SetActiveRoot()
        {
            if (SelectedContent.LastOrDefault() is EntityHierarchyRootViewModel root)
                ActiveRoot = root;
        }

        private void BreakLinkToPrefab()
        {
            BreakLinkToBase("prefab");
        }

        bool IAddChildViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            // Here we only allow dropping assets
            if (children.Any(x => !(x is AssetViewModel)))
            {
                message = DragDropBehavior.InvalidDropAreaMessage;
                return false;
            }
            return ((IAddChildViewModel)ActiveRoot ?? HierarchyRoot).CanAddChildren(children, modifiers, out message);
        }

        void IAddChildViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            var position = Controller.GetMousePositionInScene(false);
            if (Transform.TranslationSnap.IsActive)
            {
                position = MathUtil.Snap(position, Transform.TranslationSnap.Value);
            }

            IReadOnlyCollection<EntityViewModel> newEntities;
            using (var transaction = UndoRedoService.CreateTransaction())
            {
                var root = ActiveRoot ?? HierarchyRoot;
                // Transform the position to the root local space
                TransformToLocalRoot(root as SceneRootViewModel, ref position);
                newEntities = root.AddEntitiesFromAssets(children, root.Asset.Asset.Hierarchy.RootParts.Count, modifiers, position);
                UndoRedoService.SetName(transaction, "Create entities");
            }

            // Select the newly created entities
            ClearSelection();
            SelectedContent.AddRange(newEntities);
        }

        private static void TransformToLocalRoot(SceneRootViewModel root, ref Vector3 position)
        {
            while (root != null)
            {
                position -= root.Offset;
                root = root.ParentScene;
            }
        }
    }
}
