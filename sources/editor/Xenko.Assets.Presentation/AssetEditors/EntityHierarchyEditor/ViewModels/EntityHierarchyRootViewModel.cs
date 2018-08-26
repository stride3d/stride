// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.ViewModel;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public abstract class EntityHierarchyRootViewModel : EntityHierarchyElementViewModel
    {
        private readonly string name;
        private readonly IObjectNode rootEntitiesNode;
        private readonly IObjectNode entitiesNode;

        protected EntityHierarchyRootViewModel([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityHierarchyViewModel asset, [NotNull] string name)
            : base(editor, asset, asset.Asset.Hierarchy.EnumerateRootPartDesigns(), null)
        {
            this.name = name ?? throw new ArgumentNullException(nameof(name));
            rootEntitiesNode = Editor.Session.AssetNodeContainer.GetNode(EntityHierarchy.Hierarchy)[nameof(AssetCompositeHierarchyData<EntityDesign, Entity>.RootParts)].Target;
            rootEntitiesNode.ItemChanging += RootEntitiesChanging;
            rootEntitiesNode.ItemChanged += RootEntitiesChanged;
            entitiesNode = Editor.Session.AssetNodeContainer.GetNode(EntityHierarchy.Hierarchy)[nameof(AssetCompositeHierarchyData<EntityDesign, Entity>.Parts)].Target;
            entitiesNode.ItemChanged += EntitiesChanged;
        }

        /// <inheritdoc/>
        public override IEnumerable<EntityViewModel> InnerSubEntities => Children.SelectMany(x => x.InnerSubEntities);

        /// <inheritdoc />
        public override bool IsEditable => false;

        /// <inheritdoc/>
        [NotNull]
        public override string Name { get => name; set => throw new NotSupportedException($"Cannot change the name of a {nameof(EntityHierarchyRootViewModel)} object."); }

        /// <inheritdoc/>
        internal override int EntityCount => EntityHierarchy.Hierarchy.RootParts.Count;

        /// <inheritdoc/>
        public override void Destroy()
        {
            EnsureNotDestroyed(nameof(EntityHierarchyRootViewModel));
            rootEntitiesNode.ItemChanging -= RootEntitiesChanging;
            rootEntitiesNode.ItemChanged -= RootEntitiesChanged;
            entitiesNode.ItemChanged -= EntitiesChanged;

            base.Destroy();
        }

        /// <inheritdoc/>
        public override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.NodeContainer.GetNode(Asset.Asset));
            path.PushMember(nameof(EntityHierarchy.Hierarchy));
            path.PushTarget();
            return path;
        }

        /// <inheritdoc/>
        public override int IndexOfEntity(EntityViewModel entity)
        {
            return EntityHierarchy.Hierarchy.RootParts.IndexOf(x => x.Id == entity.Id.ObjectId);
        }

        private void EntitiesChanged(object sender, ItemChangeEventArgs e)
        {
            EntityDesign entityDesign;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    entityDesign = (EntityDesign)e.NewValue;
                    Editor.Logger.Verbose($"Add {entityDesign.Entity.Name} ({entityDesign.Entity.Id}) to the Entities collection");
                    break;
                case ContentChangeType.CollectionRemove:
                    entityDesign = (EntityDesign)e.OldValue;
                    Editor.Logger.Verbose($"Remove {entityDesign.Entity.Name} ({entityDesign.Entity.Id}) from the Entities collection");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // TODO: disable this sanity check for now. It is frequently failing in the middle of an operation, because we are modifiying both collections. Would be good to restore it later (as a CheckConsistency like in SessionViewModel?)
            //// This is just a sanity check when an entity is removed.
            //if (e.ChangeType == ContentChangeType.CollectionRemove && !Editor.UndoRedoService.UndoRedoInProgress)
            //{
            //    var allEntities = Content.SelectDeep(x => x.Content).OfType<EntityViewModel>().ToList();
            //    int count = 0;
            //    foreach (var entity in allEntities)
            //    {
            //        if (!EntityHierarchy.Hierarchy.Entities.Contains(entity.EntityDesign))
            //        {
            //            throw new InvalidOperationException($"The entity {entity.Name} ({entity.Id}) is not present in the Entity collection.");
            //        }
            //        count++;
            //    }

            //    if (count != EntityHierarchy.Hierarchy.Entities.Count)
            //    {
            //        foreach (var entity in EntityHierarchy.Hierarchy.Entities)
            //        {
            //            if (allEntities.All(x => x.EntityDesign != entity))
            //            {
            //                throw new InvalidOperationException($"The entity {entity.Entity.Name} ({entity.Entity.Id}) is present in the Entity collection but does not have a view model anymore.");
            //            }
            //        }
            //    }
            //}
        }

        private void RootEntitiesChanging(object sender, ItemChangeEventArgs e)
        {
            // Ensure the folder is created before actually doing the change
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                var entity = (Entity)e.NewValue;
                FindOrCreateFolder(EntityHierarchy.Hierarchy.Parts[entity.Id].Folder);
            }
        }

        private async void RootEntitiesChanged(object sender, ItemChangeEventArgs e)
        {
            Entity rootEntity;
            switch (e.ChangeType)
            {
                case ContentChangeType.CollectionAdd:
                    rootEntity = (Entity)e.NewValue;
                    Editor.Logger.Verbose($"Add {rootEntity.Id} to the RootEntities collection");
                    break;
                case ContentChangeType.CollectionRemove:
                    rootEntity = (Entity)e.OldValue;
                    Editor.Logger.Verbose($"Remove {rootEntity.Id} from the RootEntities collection");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Update view model and replicate changes to the game-side objects
            if (e.ChangeType == ContentChangeType.CollectionAdd)
            {
                // TODO: can be factorized with what EntityViewModel is doing, through a base method. There are a few differences though.
                var entity = (Entity)e.NewValue;
                var entityDesign = EntityHierarchy.Hierarchy.Parts[entity.Id];
                var element = (EntityViewModel)Editor.CreatePartViewModel(Asset, entityDesign);
                var index = e.Index.Int;
                var parent = FindOrCreateFolder(entityDesign.Folder);
                parent.InsertEntityViewModel(element, parent.FindIndexInParent(index, EntityHierarchy.Hierarchy.EnumerateRootPartDesigns()));

                // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested. Then just call RequestLoading(true) once.
                if (IsLoaded)
                {
                    // Set the new entity and its children as 'loaded'
                    var children = element.TransformChildren.BreadthFirst(x => x.TransformChildren).ToList();
                    element.RequestLoading(true).Forget();
                    foreach (var child in children)
                    {
                        child.RequestLoading(true).Forget();
                    }

                    // Add the element to the game
                    await Editor.Controller.AddPart(this, element.AssetSideEntity);

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
                var entity = (Entity)e.OldValue;
                var partId = new AbsoluteId(Asset.Id, entity.Id);
                var element = (EntityViewModel)Editor.FindPartViewModel(partId);
                if (element == null) throw new InvalidOperationException($"{nameof(element)} cannot be null");
                RemoveEntityViewModel(element);
                // FIXME: when we allow loading/unloading single entities, this should be implemented in OnLoadingRequested. Then just call RequestLoading(false) once.
                if (element.IsLoaded)
                {
                    // FIXME: set all children as unloaded?
                    element.RequestLoading(false).Forget();
                    Editor.Controller.RemovePart(this, element.AssetSideEntity).Forget();
                }
            }
        }
    }
}
