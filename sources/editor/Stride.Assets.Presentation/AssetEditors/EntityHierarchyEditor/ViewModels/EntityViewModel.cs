// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Quantum;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.EntityFactories;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.Quantum;
using Stride.Assets.Presentation.ViewModel;
using Stride.Engine;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    [DebuggerDisplay("Entity = {Name}")]
    public sealed class EntityViewModel : EntityHierarchyElementViewModel, IEditorDesignPartViewModel<EntityDesign, Entity>, IIsEditableViewModel, IDisposable, IAddChildrenPropertiesProviderViewModel
    {
        private EntityHierarchyElementChangePropagator propagator;

        // TODO These models should be pluggable later
        private readonly ModelComponentViewModel modelComponent;
        private readonly ParticleSystemComponentViewModel particleComponent;
        private readonly CameraComponentViewModel cameraComponent;

        private readonly MemberGraphNodeBinding<string> nameNodeBinding;
        private readonly ObjectGraphNodeBinding<EntityComponentCollection> componentsNodeBinding;
        private readonly IObjectNode transformationNode;
        private PrefabViewModel sourcePrefab;

        public EntityViewModel([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityHierarchyViewModel asset, [NotNull] EntityDesign entityDesign)
            : base(editor, asset, GetOrCreateChildPartDesigns((EntityHierarchyAssetBase)asset.Asset, entityDesign), entityDesign.Entity)
        {
            if (entityDesign.Entity == null) throw new ArgumentException(@"entity must contain a non-null asset entity.", nameof(entityDesign));

            EntityDesign = entityDesign;

            var assetNode = Editor.NodeContainer.GetOrCreateNode(entityDesign.Entity);
            nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(Entity.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService);
            componentsNodeBinding = new ObjectGraphNodeBinding<EntityComponentCollection>(assetNode[nameof(Entity.Components)].Target, nameof(Components), OnPropertyChanging, OnPropertyChanged, Editor.UndoRedoService, false);

            modelComponent = new ModelComponentViewModel(ServiceProvider, this);
            particleComponent = new ParticleSystemComponentViewModel(ServiceProvider, this);
            cameraComponent = new CameraComponentViewModel(ServiceProvider, this);
            transformationNode = Editor.NodeContainer.GetNode(AssetSideEntity.Transform)[nameof(TransformComponent.Children)].Target;
            transformationNode.ItemChanging += TransformChildrenChanging;
            transformationNode.ItemChanged += TransformChildrenChanged;
            RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
            FocusOnEntityCommand = new AnonymousCommand(ServiceProvider, FocusOnEntity);

            UpdateSourcePrefab();
            var basePrefabNode = Editor.NodeContainer.GetNode(EntityDesign)[nameof(EntityDesign.Base)];
            basePrefabNode.ValueChanged += BasePrefabChanged;
        }

        /// <inheritdoc/>
        public override string Name { get { return nameNodeBinding.Value; } set { nameNodeBinding.Value = value; } }

        /// <inheritdoc/>
        public override IEnumerable<EntityViewModel> InnerSubEntities { get { yield return this; } }

        /// <inheritdoc/>
        public override AbsoluteId Id => new AbsoluteId(Asset.Id, AssetSideEntity.Id);

        /// <inheritdoc/>
        public override bool IsEditing { get { return base.IsEditing; } set { base.IsEditing = value; FocusOnEntityCommand.IsEnabled = !value; } }

        [NotNull]
        public ICommandBase RenameCommand { get; }

        [NotNull]
        public ICommandBase FocusOnEntityCommand { get; }

        public IEnumerable<EntityComponent> Components => componentsNodeBinding.GetNodeValue();

        [NotNull]
        public Entity AssetSideEntity => EntityDesign.Entity;

        [NotNull]
        internal EntityDesign EntityDesign { get; }

        public PrefabViewModel SourcePrefab { get { return sourcePrefab; } private set { SetValue(ref sourcePrefab, value); } }

        EntityDesign IEditorDesignPartViewModel<EntityDesign, Entity>.PartDesign => EntityDesign;

        /// <inheritdoc/>
        public override async Task NotifyGameSidePartAdded()
        {
            propagator.NotifyGameSidePartAdded();
            // We need the propagator ready to initialize the model component view model, since it might modify the material list.
            await propagator.Initialized;
            modelComponent.Initialize();
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(EntityViewModel));
            Cleanup();
            base.Destroy();
        }

        /// <inheritdoc/>
        public override void UpdateNodePresenter([NotNull] INodePresenter node)
        {
            // TODO: make an interface for component view models so they present these methods
            if (node == null) throw new ArgumentNullException(nameof(node));
            base.UpdateNodePresenter(node);
            modelComponent.UpdateNodePresenter(node);
        }

        /// <inheritdoc/>
        public override void FinalizeNodePresenterTree(IAssetNodePresenter root)
        {
            base.FinalizeNodePresenterTree(root);
            cameraComponent.FinalizeNodePresenterTree(root);
            particleComponent.FinalizeNodePresenterTree(root);
        }

        /// <inheritdoc/>
        void IDisposable.Dispose()
        {
            Cleanup();
        }

        /// <inheritdoc/>
        public override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
            path.PushMember(nameof(EntityHierarchy.Hierarchy));
            path.PushTarget();
            path.PushMember(nameof(EntityHierarchy.Hierarchy.Parts));
            path.PushTarget();
            path.PushIndex(new NodeIndex(Id.ObjectId));
            path.PushMember(nameof(EntityDesign.Entity));
            path.PushTarget();
            return path;
        }

        /// <inheritdoc/>
        public override int IndexOfEntity(EntityViewModel entity)
        {
            return AssetSideEntity.Transform.Children.IndexOf(x => x.Entity.Id == entity.Id.ObjectId);
        }

        /// <inheritdoc/>
        internal override int EntityCount => AssetSideEntity.Transform.Children.Count;

        // TODO: turn this non-static and put it in the base - just keep the entity-specific part here. This need to rework a bit how we initialize folders
        private static IEnumerable<EntityDesign> GetOrCreateChildPartDesigns([NotNull] EntityHierarchyAssetBase asset, [NotNull] EntityDesign entityDesign)
        {
            foreach (var child in entityDesign.Entity.Transform.Children)
            {
                if (!asset.Hierarchy.Parts.TryGetValue(child.Entity.Id, out EntityDesign childDesign))
                {
                    childDesign = new EntityDesign(child.Entity);
                }
                if (child.Entity != childDesign.Entity) throw new InvalidOperationException();
                yield return childDesign;
            }
        }

        /// <inheritdoc />
        protected override int GetInsertionIndex(InsertPosition position)
        {
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
            var index = Parent.Owner.IndexOfEntity(this);
            if (position == InsertPosition.After)
                ++index;

            return index;
        }

        /// <inheritdoc />
        protected override Task OnLoadingRequested(bool load, bool recursive)
        {
            // TODO: we don't support explicit loading request on entities for the moment.
            // TODO: in the future to allow loading/unloading a specific entity, we will need to call AddPart/RemovePart here
            // TODO: and handle all issues releated to references (i.e. what happens when we unload an entity that is still referenced by another loaded entity)
            IsLoaded = load;
            if (!recursive)
                return Task.CompletedTask;

            // Update IsLoaded property recursively
            foreach (var entity in TransformChildren.BreadthFirst(x => x.TransformChildren))
            {
                entity.IsLoaded = load;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override Task OnLockingRequested(bool @lock, bool recursive)
        {
            IsLocked = @lock;
            if (!recursive)
                return Task.CompletedTask;

            // Update IsLocked property recursively
            foreach (var entity in TransformChildren.BreadthFirst(x => x.TransformChildren))
            {
                entity.IsLocked = @lock;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            base.OnPropertyChanged(propertyNames);

            if (propertyNames.Any(p => string.Equals(p, nameof(IsLoaded))))
            {
                OnIsLoadedChanged();
            }
        }

        private void OnIsLoadedChanged()
        {
            if (IsLoaded)
            {
                propagator = new EntityHierarchyElementChangePropagator(Editor, this, AssetSideEntity);
            }
            else
            {
                propagator?.Destroy();
                propagator = null;
            }
        }

        private void Cleanup()
        {
            transformationNode.ItemChanging -= TransformChildrenChanging;
            transformationNode.ItemChanged -= TransformChildrenChanged;
            var basePrefabNode = Editor.NodeContainer.GetNode(EntityDesign)[nameof(EntityDesign.Base)];
            basePrefabNode.ValueChanged -= BasePrefabChanged;

            nameNodeBinding.Dispose();
            componentsNodeBinding.Dispose();
            modelComponent.Destroy();
            particleComponent.Destroy();
            cameraComponent.Destroy();
            propagator?.Destroy();
        }

        private void BasePrefabChanged(object sender, MemberNodeChangeEventArgs e)
        {
            UpdateSourcePrefab();
        }

        private void UpdateSourcePrefab()
        {
            SourcePrefab = EntityDesign.Base != null ? Editor.Session.GetAssetById(EntityDesign.Base.BasePartAsset.Id) as PrefabViewModel : null;
        }

        private void TransformChildrenChanging(object sender, ItemChangeEventArgs e)
        {
            // Ensure the folder is created before actually doing the change
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                var component = (TransformComponent)e.NewValue;
                FindOrCreateFolder(EntityHierarchy.Hierarchy.Parts[component.Entity.Id].Folder);
            }
        }

        private async void TransformChildrenChanged(object sender, ItemChangeEventArgs e)
        {
            Entity childEntity;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    childEntity = ((TransformComponent)e.NewValue).Entity;
                    Editor.Logger.Verbose($"Add {childEntity.Name} ({childEntity.Id}) to the Entities collection");
                    break;
                case ContentChangeType.CollectionRemove:
                    childEntity = ((TransformComponent)e.OldValue).Entity;
                    Editor.Logger.Verbose($"Remove {childEntity.Name} ({childEntity.Id}) from the Entities collection");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update view model
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                // TODO: can be factorized with what EntityHierarchyRootViewModel is doing, through a base method. There are a few differences though.
                var component = (TransformComponent)e.NewValue;
                var entity = EntityHierarchy.Hierarchy.Parts[component.Entity.Id];
                var element = (EntityViewModel)Editor.CreatePartViewModel(Asset, entity);
                var index = e.Index.Int;
                var parent = FindOrCreateFolder(entity.Folder);
                parent.InsertEntityViewModel(element, parent.FindIndexInParent(index, EntityDesign.Entity.Transform.Children.Select(x => EntityHierarchy.Hierarchy.Parts[x.Entity.Id])));

                // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested. Then just call RequestLoading(true).
                // Check if the container root is loaded
                if (EntityHierarchyEditorViewModel.GetRoot(this).IsLoaded)
                {
                    var children = element.TransformChildren.BreadthFirst(x => x.TransformChildren).ToList();
                    // Set the new entity and its children as 'loading'
                    element.IsLoading = true;
                    foreach (var child in children)
                    {
                        child.IsLoading = true;
                    }
                    // Add the entity to the game (actually load the entity and its children)
                    await Editor.Controller.AddPart(this, element.AssetSideEntity);
                    // Set the new entity and its children as 'loaded'
                    element.IsLoaded = true;
                    foreach (var child in children)
                    {
                        child.IsLoaded = true;
                    }
                    // Manually notify the game-side scene
                    element.NotifyGameSidePartAdded().Forget();
                    foreach (var child in children)
                    {
                        child.NotifyGameSidePartAdded().Forget();
                    }
                }
            }
            else if (e.ChangeType == ContentChangeType.CollectionRemove)
            {
                var component = (TransformComponent)e.OldValue;
                var partId = new AbsoluteId(Asset.Id, component.Entity.Id);
                var element = (EntityViewModel)Editor.FindPartViewModel(partId);
                if (element == null) throw new InvalidOperationException($"{nameof(element)} cannot be null");
                RemoveEntityViewModel(element);
                // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested. Then just call RequestLoading(false).
                if (element.IsLoaded)
                {
                    element.IsLoading = true;
                    Editor.Controller.RemovePart(this, element.AssetSideEntity).Forget();
                    element.IsLoaded = false;
                }
            }
        }

        private void FocusOnEntity()
        {
            if (!IsLoaded)
                return;

            int meshIndex = -1;
            var target = this;
            var result = Editor.Controller.GetService<IEditorGameMaterialHighlightViewModelService>()?.GetTargetMeshIndex(this);
            if (result != null)
            {
                var partId = new AbsoluteId(Asset.Id, result.Item1);
                target = (EntityViewModel)Editor.FindPartViewModel(partId);
                meshIndex = result.Item2;
            }

            Editor.Controller.GetService<IEditorGameEntityCameraViewModelService>().CenterOnEntity(target, meshIndex);
        }

        public EntityViewModel Duplicate()
        {
            var flags = SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects;
            var clonedHierarchy = EntityHierarchyPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, AssetSideEntity.Id.Yield(), flags, out Dictionary<Guid, Guid> idRemapping);
            AssetPartsAnalysis.GenerateNewBaseInstanceIds(clonedHierarchy);

            var addedRoot = clonedHierarchy.Parts[clonedHierarchy.RootParts.Single().Id];
            addedRoot.Folder = (Parent as EntityFolderViewModel)?.Path;

            // rename the entity to avoid having the same names
            if (Parent == null) throw new InvalidOperationException($"{nameof(Parent)} cannot be null");
            addedRoot.Entity.Name = EntityFactory.ComputeNewName(Parent, addedRoot.Entity.Name);
            Asset.AssetHierarchyPropertyGraph.AddPartToAsset(clonedHierarchy.Parts, addedRoot, (Parent.Owner as EntityViewModel)?.AssetSideEntity, Parent.Owner.IndexOfEntity(this) + 1);
            var cloneId = addedRoot.Entity.Id;

            // The view model should already exist at that point
            var partId = new AbsoluteId(Asset.Id, cloneId);
            var viewModel = (EntityViewModel)Editor.FindPartViewModel(partId);
            // TODO: Offset a bit (by 1 scene unit horizontally?) the cloned entity so it appears distincly from the source entity
            return viewModel;
        }

        /// <inheritdoc/>
        bool IAddChildrenPropertiesProviderViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
        {
            return ((IAddChildViewModel)this).CanAddChildren(children, modifiers, out message);
        }

        /// <inheritdoc/>
        void IAddChildrenPropertiesProviderViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
        {
            foreach (var asset in children.OfType<AssetViewModel>())
            {
                var component = CreateComponentFromAsset(asset);
                if (component != null)
                {
                    // 3 cases:
                    // - If AllowMultipleComponents is true, we add at the end
                    // - If AllowMultipleComponents is true and the component doesn't exist yet, we replace
                    // - If AllowMultipleComponents is false and the component already exists, we replace

                    // Retrieve list of components to check if we need to add or replace
                    var assetSideEntityNode = Editor.NodeContainer.GetNode(AssetSideEntity);
                    var componentsNode = assetSideEntityNode[nameof(Entity.Components)].Target;
                    var components = AssetSideEntity.Components;

                    int replaceIndex = -1;

                    // Only try to replace in case AllowMultipleComponents is not set
                    if (!EntityComponentAttributes.Get(component.GetType()).AllowMultipleComponents)
                    {
                        // Find the replace index (if a component of same type already exists)
                        replaceIndex = components.IndexOf(x => x.GetType() == component.GetType());
                    }

                    // Add or replace the component
                    using (var transaction = Editor.UndoRedoService.CreateTransaction())
                    {
                        if (replaceIndex == -1)
                        {
                            componentsNode.Add(component);
                            Editor.UndoRedoService.SetName(transaction, $"Add component {component.GetType().Name}");
                        }
                        else
                        {
                            componentsNode.Update(component, new NodeIndex(replaceIndex));
                            Editor.UndoRedoService.SetName(transaction, $"Replace component {component.GetType().Name}");
                        }
                    }
                }
            }
        }
    }
}
