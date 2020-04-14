// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core.Assets.Editor.Components.Properties;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Commands;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels;
using Xenko.Assets.Presentation.AssetEditors.GameEditor.Services;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels
{
    public abstract class EntityHierarchyElementViewModel : EntityHierarchyItemViewModel, IAssetPropertyProviderViewModel, IEditorGamePartViewModel
    {
        private readonly object assetSideInstance;
        private bool isLoaded;
        private bool isLoading;
        private bool isLocked;

        protected EntityHierarchyElementViewModel([NotNull] EntityHierarchyEditorViewModel editor, [NotNull] EntityHierarchyViewModel asset, [NotNull] IEnumerable<EntityDesign> childEntities, object assetSideInstance)
            : base(editor, asset, childEntities)
        {
            this.assetSideInstance = assetSideInstance;
            LoadCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLoading(!IsLoaded, recursive));
            LockCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLocking(!IsLocked, recursive));

            DependentProperties.Add(nameof(IsLoaded), new[] { nameof(IsSelectable) });
            DependentProperties.Add(nameof(IsLocked), new[] { nameof(IsSelectable) });
        }

        public abstract AbsoluteId Id { get; }

        /// <summary>
        /// Indicates whether this element is currently loaded in the scene game.
        /// </summary>
        /// <remarks>
        /// When an element is loaded in the scene game, it increase the reference count of content it references.
        /// Unloading an element will decrease this count and might also unload content when the corresponding counter reaches zero.
        /// </remarks>
        public bool IsLoaded
        {
            get { return isLoaded; }
            protected internal set
            {
                SetValue(ref isLoaded, value, () =>
                {
                    IsLoading = false;
                    UpdateSelectables();
                });
            }
        }

        /// <summary>
        /// Indicates whether this element is currently being loaded in (or unloaded from) the scene game.
        /// </summary>
        public bool IsLoading { get { return isLoading; } protected set { SetValue(ref isLoading, value, UpdateCommands); } }

        /// <summary>
        /// Indicates whether this element is locked in the scene game.
        /// </summary>
        /// <remarks>
        /// When an element is locked, it cannot be manipulated with gizmos.
        /// </remarks>
        public bool IsLocked { get { return isLocked; } protected internal set { SetValue(ref isLocked, value, UpdateSelectables); } }

        /// <summary>
        /// Indicates whether this element can be selected in the scene game.
        /// </summary>
        public bool IsSelectable => IsLoaded && !IsLocked;

        [NotNull]
        public ICommandBase LoadCommand { get; }

        [NotNull]
        public ICommandBase LockCommand { get; }

        /// <inheritdoc/>
        public override EntityHierarchyElementViewModel Owner => this;

        /// <summary>
        /// Gets the number of entity in this object.
        /// </summary>
        /// <remarks>
        /// This property ignores the concept of folder.
        /// </remarks>
        internal abstract int EntityCount { get; }

        [CanBeNull]
        private IEditorGameSelectionViewModelService SelectionService => Editor.Controller.GetService<IEditorGameSelectionViewModelService>();

        bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

        AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

        /// <inheritdoc/>
        public override int FindIndexInParent(int indexInEntity, [NotNull] IEnumerable<EntityDesign> entityCollection)
        {
            var indexInParent = 0;
            var i = 0;
            foreach (var entity in entityCollection)
            {
                if (i++ == indexInEntity)
                    break;

                if (string.IsNullOrEmpty(entity.Folder))
                    ++indexInParent;
            }
            return indexInParent;
        }

        /// <summary>
        /// Gets the index of the given entity in this object. This method ignores the concept of folder.
        /// </summary>
        /// <param name="entity">The entity for which to find the index.</param>
        /// <returns>The index of the given entity in this <see cref="EntityHierarchyElementViewModel"/>, or -1 if not found.</returns>
        public abstract int IndexOfEntity([NotNull] EntityViewModel entity);

        /// <summary>
        /// Requests to change the <see cref="IsLoaded"/> property.
        /// </summary>
        /// <param name="load"></param>
        /// <param name="recursive"><c>true</c> to also load Children recursively; otherwise, <c>false</c>.</param>
        /// <returns></returns>
        public Task RequestLoading(bool load, bool recursive = false)
        {
            if (IsLoading || IsLoaded == load)
                return Task.CompletedTask;

            return OnLoadingRequested(load, recursive);
        }

        /// <summary>
        /// Requests to change the <see cref="IsLocked"/> property.
        /// </summary>
        /// <param name="lock"></param>
        /// <param name="recursive"><c>true</c> to also lock Children recursively; otherwise, <c>false</c>.</param>
        /// <returns></returns>
        public Task RequestLocking(bool @lock, bool recursive = false)
        {
            if (!IsLoaded || IsLocked == @lock)
                return Task.CompletedTask;

            return OnLockingRequested(@lock, recursive);
        }

        public virtual void UpdateNodePresenter(INodePresenter node)
        {
            // Do nothing by default
        }

        public virtual void FinalizeNodePresenterTree(IAssetNodePresenter root)
        {
            // Do nothing by default
        }

        /// <summary>
        /// Called whenever the value of <see cref="IsLoaded"/> property is requested to change.
        /// </summary>
        /// <seealso cref="RequestLoading"/>
        protected abstract Task OnLoadingRequested(bool load, bool recursive);

        /// <summary>
        /// Called whenever the value of <see cref="IsLocked"/> property is requested to change.
        /// </summary>
        /// <seealso cref="RequestLocking"/>
        protected abstract Task OnLockingRequested(bool @lock, bool recursive);

        private void UpdateCommands()
        {
            LoadCommand.IsEnabled = !IsLoading;
            LockCommand.IsEnabled = IsLoaded;
        }

        private void UpdateSelectables()
        {
            if (IsSelectable)
                SelectionService?.AddSelectable(Id);
            else
                SelectionService?.RemoveSelectable(Id);
        }

        /// <inheritdoc/>
        IObjectNode IPropertyProviderViewModel.GetRootNode()
        {
            return Editor.NodeContainer.GetOrCreateNode(assetSideInstance);
        }

        /// <inheritdoc/>
        GraphNodePath IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode()
        {
            return GetNodePath();
        }

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.ShouldConstructMember(IMemberNode member) => ((IPropertyProviderViewModel)Asset).ShouldConstructMember(member);

        /// <inheritdoc/>
        bool IPropertyProviderViewModel.ShouldConstructItem(IObjectNode collection, NodeIndex index) => ((IPropertyProviderViewModel)Asset).ShouldConstructItem(collection, index);
    }
}
