// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Stride.Assets.Quantum;
using Stride.Core;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Quantum;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Services;
using Stride.Core.Quantum;
using Stride.Engine;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// An entity of the hierarchy of a scene or a prefab.
/// </summary>
[DebuggerDisplay("Entity = {Name}")]
public sealed class EntityViewModel : EntityHierarchyElementViewModel, IEditorDesignPartViewModel<EntityDesign, Entity>, IAddChildrenPropertiesProviderViewModel
{
    private readonly MemberGraphNodeBinding<string> nameNodeBinding;
    private readonly ObjectGraphNodeBinding<EntityComponentCollection> componentsNodeBinding;
    private readonly IObjectNode transformChildrenNode;
    private readonly IMemberNode basePrefabNode;
    private PrefabViewModel? sourcePrefab;

    public EntityViewModel(EntityHierarchyViewModel asset, EntityDesign entityDesign)
        : base(asset, GetOrCreateChildPartDesigns((EntityHierarchyAssetBase)asset.Asset, entityDesign), entityDesign.Entity)
    {
        if (entityDesign.Entity == null) throw new ArgumentException(@"entity must contain a non-null asset entity.", nameof(entityDesign));

        EntityDesign = entityDesign;

        var nodeContainer = asset.Session.AssetNodeContainer;
        var assetNode = nodeContainer.GetOrCreateNode(entityDesign.Entity);
        nameNodeBinding = new MemberGraphNodeBinding<string>(assetNode[nameof(Entity.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, UndoRedoService);
        componentsNodeBinding = new ObjectGraphNodeBinding<EntityComponentCollection>(assetNode[nameof(Entity.Components)].Target!, nameof(Components), OnPropertyChanging, OnPropertyChanged, UndoRedoService, false);
        transformChildrenNode = nodeContainer.GetOrCreateNode(entityDesign.Entity.Transform)[nameof(TransformComponent.Children)].Target!;
        transformChildrenNode.ItemChanging += TransformChildrenChanging;
        transformChildrenNode.ItemChanged += TransformChildrenChanged;
        RenameCommand = new AnonymousCommand(ServiceProvider, () => IsEditing = true);
        FocusOnEntityCommand = new AnonymousTaskCommand(ServiceProvider, FocusOnEntity);

        UpdateSourcePrefab();
        basePrefabNode = nodeContainer.GetOrCreateNode(entityDesign)[nameof(EntityDesign.Base)];
        basePrefabNode.ValueChanged += BasePrefabChanged;
    }

    public IEnumerable<EntityComponent> Components => componentsNodeBinding.GetNodeValue() ?? [];

    public Entity AssetSideEntity => EntityDesign.Entity;

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, AssetSideEntity.Id);

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities { get { yield return this; } }

    /// <inheritdoc/>
    public override string? Name
    {
        get => nameNodeBinding.Value;
        set => nameNodeBinding.Value = value;
    }

    /// <inheritdoc/>
    public override bool IsEditing { get => base.IsEditing; set { base.IsEditing = value; FocusOnEntityCommand.IsEnabled = !value; } }

    /// <inheritdoc/>
    public override int EntityCount => AssetSideEntity.Transform.Children.Count;

    /// <summary>
    /// Gets the command that begins the renaming of this entity in place (F2).
    /// </summary>
    public ICommandBase RenameCommand { get; }

    /// <summary>
    /// Gets the command that moves the camera of the editor to this entity, when it is loaded.
    /// </summary>
    public ICommandBase FocusOnEntityCommand { get; }

    /// <summary>
    /// Gets the prefab this entity is an instance of, or <c>null</c> when it is not part of a prefab instance.
    /// </summary>
    public PrefabViewModel? SourcePrefab { get => sourcePrefab; private set => SetValue(ref sourcePrefab, value); }

    /// <summary>
    /// Gets the design of this entity: the entity and its folder.
    /// </summary>
    public EntityDesign EntityDesign { get; }

    EntityDesign IEditorDesignPartViewModel<EntityDesign, Entity>.PartDesign => EntityDesign;

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(EntityViewModel));
        transformChildrenNode.ItemChanging -= TransformChildrenChanging;
        transformChildrenNode.ItemChanged -= TransformChildrenChanged;
        basePrefabNode.ValueChanged -= BasePrefabChanged;
        nameNodeBinding.Dispose();
        componentsNodeBinding.Dispose();
        base.Destroy();
    }

    /// <inheritdoc/>
    public override GraphNodePath GetNodePath()
    {
        var path = new GraphNodePath(GetNode());
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

    /// <summary>
    /// Adds a copy of this entity, with its children, right after it in the same folder (Ctrl+D in the editor).
    /// </summary>
    /// <returns>The view model of the copy.</returns>
    public EntityViewModel Duplicate()
    {
        if (Parent is null)
            throw new InvalidOperationException($"{nameof(Parent)} cannot be null");

        var clonedHierarchy = EntityHierarchyPropertyGraph.CloneSubHierarchies(Asset.Session.AssetNodeContainer, Asset.Asset, AssetSideEntity.Id.Yield(), SubHierarchyCloneFlags.GenerateNewIdsForIdentifiableObjects, out _);
        AssetPartsAnalysis.GenerateNewBaseInstanceIds(clonedHierarchy);

        var addedRoot = clonedHierarchy.Parts[clonedHierarchy.RootParts.Single().Id];
        addedRoot.Folder = (Parent as EntityFolderViewModel)?.Path;

        // rename the entity to avoid having the same names
        addedRoot.Entity.Name = EntityNames.ComputeNewName(Parent, addedRoot.Entity.Name);
        Asset.AssetHierarchyPropertyGraph.AddPartToAsset(clonedHierarchy.Parts, addedRoot, (Parent.Owner as EntityViewModel)?.AssetSideEntity, Parent.Owner.IndexOfEntity(this) + 1);

        // The view model exists at that point
        // TODO: Offset a bit (by 1 scene unit horizontally?) the cloned entity so it appears distincly from the source entity
        return Parent.Owner.TransformChildren.First(x => x.AssetSideEntity == addedRoot.Entity);
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

    // TODO: turn this non-static and put it in the base - just keep the entity-specific part here. This need to rework a bit how we initialize folders
    private static IEnumerable<EntityDesign> GetOrCreateChildPartDesigns(EntityHierarchyAssetBase asset, EntityDesign entityDesign)
    {
        foreach (var child in entityDesign.Entity.Transform.Children)
        {
            if (!asset.Hierarchy.Parts.TryGetValue(child.Entity.Id, out var childDesign))
            {
                childDesign = new EntityDesign(child.Entity);
            }
            if (child.Entity != childDesign.Entity) throw new InvalidOperationException();
            yield return childDesign;
        }
    }

    private Task FocusOnEntity()
    {
        return IsLoaded && Editor is { } editor ? editor.FocusOnEntityAsync(this) : Task.CompletedTask;
    }

    private void BasePrefabChanged(object? sender, MemberNodeChangeEventArgs e)
    {
        UpdateSourcePrefab();
    }

    private void UpdateSourcePrefab()
    {
        SourcePrefab = EntityDesign.Base != null ? Asset.Session.GetAssetById(EntityDesign.Base.BasePartAsset.Id) as PrefabViewModel : null;
    }

    private void TransformChildrenChanging(object? sender, ItemChangeEventArgs e)
    {
        // The folder exists before the entity goes in
        if (e.ChangeType == ContentChangeType.CollectionAdd)
        {
            FindOrCreateFolder(EntityHierarchy.Hierarchy.Parts[((TransformComponent)e.NewValue!).Entity.Id].Folder);
        }
    }

    private void TransformChildrenChanged(object? sender, ItemChangeEventArgs e)
    {
        switch (e.ChangeType)
        {
            case ContentChangeType.CollectionAdd:
                InsertEntity(e.Index.Int, EntityHierarchy.Hierarchy.Parts[((TransformComponent)e.NewValue!).Entity.Id],
                    AssetSideEntity.Transform.Children.Select(x => EntityHierarchy.Hierarchy.Parts[x.Entity.Id]));
                break;
            case ContentChangeType.CollectionRemove:
                RemoveEntity(((TransformComponent)e.OldValue!).Entity, e.Index.Int);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }

    /// <inheritdoc/>
    bool IAddChildrenPropertiesProviderViewModel.CanAddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, out string message)
    {
        return ((IAddChildViewModel)this).CanAddChildren(children, modifiers, out message);
    }

    /// <inheritdoc/>
    void IAddChildrenPropertiesProviderViewModel.AddChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers)
    {
        var undoRedo = UndoRedoService;
        foreach (var asset in children.OfType<AssetViewModel>())
        {
            var component = CreateComponentFromAsset(asset);
            if (component == null)
                continue;

            // 3 cases:
            // - If AllowMultipleComponents is true, we add at the end
            // - If AllowMultipleComponents is true and the component doesn't exist yet, we replace
            // - If AllowMultipleComponents is false and the component already exists, we replace

            // Retrieve list of components to check if we need to add or replace
            var assetSideEntityNode = Asset.Session.AssetNodeContainer.GetNode(AssetSideEntity)!;
            var componentsNode = assetSideEntityNode[nameof(Entity.Components)].Target!;
            var components = AssetSideEntity.Components;

            var replaceIndex = -1;

            // Only try to replace in case AllowMultipleComponents is not set
            if (!EntityComponentAttributes.Get(component.GetType()).AllowMultipleComponents)
            {
                // Find the replace index (if a component of same type already exists)
                replaceIndex = components.IndexOf(x => x.GetType() == component.GetType());
            }

            // Add or replace the component
            using var transaction = undoRedo?.CreateTransaction();
            if (replaceIndex == -1)
            {
                componentsNode.Add(component);
                if (transaction != null)
                    undoRedo!.SetName(transaction, $"Add component {component.GetType().Name}");
            }
            else
            {
                componentsNode.Update(component, new NodeIndex(replaceIndex));
                if (transaction != null)
                    undoRedo!.SetName(transaction, $"Replace component {component.GetType().Name}");
            }
        }
    }
}
