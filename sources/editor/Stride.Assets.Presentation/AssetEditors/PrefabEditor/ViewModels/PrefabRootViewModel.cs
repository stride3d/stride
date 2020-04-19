// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.AssetEditors.PrefabEditor.ViewModels
{
    [DebuggerDisplay("PrefabRoot = {" + nameof(Name) + "}")]
    public sealed class PrefabRootViewModel : EntityHierarchyRootViewModel
    {
        /// <inheritdoc />
        public PrefabRootViewModel([NotNull] PrefabEditorViewModel editor, [NotNull] PrefabViewModel asset)
            : base(editor, asset, "Prefab root")
        {
        }

        /// <inheritdoc/>
        public override AbsoluteId Id => new AbsoluteId(Asset.Id, Guid.Empty);

        [NotNull]
        private new PrefabEditorViewModel Editor => (PrefabEditorViewModel)base.Editor;

        /// <inheritdoc/>
        public override Task NotifyGameSidePartAdded()
        {
            // Manually notify the game-side scene
            return Task.WhenAll(Children.BreadthFirst(x => x.Children).Select(x => x.NotifyGameSidePartAdded()));
        }

        /// <inheritdoc />
        protected override async Task OnLoadingRequested(bool load, bool recursive)
        {
            var controller = Editor.Controller;
            if (load)
            {
                // Loading/unloading a prefab root always applies to all its content
                SetEntityIsLoadedRecursively(true);
                await controller.LoadEntities(this);
                await NotifyGameSidePartAdded();
            }
            else
            {
                // Loading/unloading a prefab root always applies to all its content
                SetEntityIsLoadedRecursively(false);
                await controller.UnloadEntities(this);
            }
            IsLoaded = load;
        }

        /// <inheritdoc />
        protected override Task OnLockingRequested(bool @lock, bool recursive)
        {
            IsLocked = @lock;
            // Locking/unlocking a scene always applies to all its content
            SetEntityIsLockedRecursively(@lock);
            return Task.CompletedTask;
        }

        /// <summary>
        /// Updates the <see cref="EntityHierarchyElementViewModel.IsLoaded"/> property on all entities of the prefab.
        /// </summary>
        /// <param name="isLoaded"></param>
        private void SetEntityIsLoadedRecursively(bool isLoaded)
        {
            // This method is called from OnLoadingRequested, therefore we don't need to request the entities to load.
            // We just need to update their status.
            foreach (var entity in Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                entity.IsLoaded = isLoaded;
            }
        }

        /// <summary>
        /// Updates the <see cref="EntityHierarchyElementViewModel.IsLocked"/> property on all entities of the prefab.
        /// </summary>
        /// <param name="isLocked"></param>
        private void SetEntityIsLockedRecursively(bool isLocked)
        {
            // This method is called from OnLockingRequested, therefore we don't need to request the entities to lock.
            // We just need to update their status.
            foreach (var entity in Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>())
            {
                entity.IsLocked = isLocked;
            }
        }
    }
}
