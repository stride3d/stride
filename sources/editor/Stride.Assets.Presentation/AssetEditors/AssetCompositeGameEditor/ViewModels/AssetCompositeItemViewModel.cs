// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModel;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.AssetCompositeGameEditor.ViewModels
{
    /// <summary>
    /// A view model representing an item, real or virtual, of a hierarchical composite asset.
    /// </summary>
    public abstract class AssetCompositeItemViewModel : DispatcherViewModel, IChildViewModel
    {
        private AssetCompositeItemViewModel parent;
        private bool isVisible = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeItemViewModel"/> class.
        /// </summary>
        /// <param name="editor">The related editor.</param>
        /// <param name="asset">The related asset.</param>
        protected AssetCompositeItemViewModel([NotNull] AssetCompositeEditorViewModel editor, [NotNull] AssetViewModel asset)
            : base(editor.SafeArgument(nameof(editor)).ServiceProvider)
        {
            Asset = asset;
            Editor = editor;
        }

        /// <summary>
        /// The related asset.
        /// </summary>
        [NotNull]
        public AssetViewModel Asset { get; }

        /// <summary>
        /// The related editor.
        /// </summary>
        [NotNull]
        public AssetCompositeEditorViewModel Editor { get; }

        /// <summary>
        /// Gets or sets whether this item should be displayed.
        /// </summary>
        public bool IsVisible { get { return isVisible; } internal set { SetValue(ref isVisible, value); } }

        /// <summary>
        /// Gets or sets the name of this item.
        /// </summary>
        public abstract string Name { get; set; }

        /// <summary>
        /// The parent of this item.
        /// </summary>
        [CanBeNull]
        public AssetCompositeItemViewModel Parent
        {
            get { return parent; }
            protected set
            {
                if (value == parent)
                {
                    Debug.WriteLine("Ineffective change to the Parent.");
                }
                SetValue(ref parent, value);
            }
        }

        /// <inheritdoc/>
        public override void Destroy()
        {
            foreach (var child in EnumerateChildren())
            {
                child.Destroy();
            }
            base.Destroy();
        }

        /// <summary>
        /// Enumerates all child items of this <see cref="AssetCompositeItemViewModel"/>.
        /// </summary>
        /// <returns>A sequence containing all child items of this instance.</returns>
        [ItemNotNull, NotNull]
        public abstract IEnumerable<AssetCompositeItemViewModel> EnumerateChildren();

        /// <summary>
        /// Gets the path to this item in the asset.
        /// </summary>
        /// <remarks>In case of a virtual node, this method should return an equivalent path if possible; otherwise the path the the closest non-virtual ancestor item.</remarks>
        /// <seealso cref="IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode"/>>
        [NotNull]
        public abstract GraphNodePath GetNodePath();

        /// <summary>
        /// Notifies the view model that the game-side part of the object it represents has been added to the game scene.
        /// </summary>
        /// <returns>A task that completes when the actions to undertake from this notification are done.</returns>
        [NotNull]
        public virtual Task NotifyGameSidePartAdded()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        IChildViewModel IChildViewModel.GetParent()
        {
            return Parent;
        }

        /// <inheritdoc/>
        string IChildViewModel.GetName()
        {
            return (this as IEditorGamePartViewModel)?.Id.ToString() ?? Name;
        }
    }

    /// <summary>
    /// A view model representing an item, real or virtual, of a hierarchical composite asset.
    /// </summary>
    /// <typeparam name="TAssetViewModel">The type of the related asset.</typeparam>
    /// <typeparam name="TParentViewModel">The type of the parent item.</typeparam>
    /// <typeparam name="TChildViewModel">The type of the child items.</typeparam>
    public abstract class AssetCompositeItemViewModel<TAssetViewModel, TParentViewModel, TChildViewModel> : AssetCompositeItemViewModel
        where TAssetViewModel : AssetViewModel
        where TParentViewModel : AssetCompositeItemViewModel<TAssetViewModel, TParentViewModel, TChildViewModel>
        where TChildViewModel : AssetCompositeItemViewModel<TAssetViewModel, TParentViewModel, TChildViewModel>
    {
        private readonly ObservableList<TChildViewModel> children = new ObservableList<TChildViewModel>();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeItemViewModel{TAssetViewModel,TParentViewModel,TChildViewModel}"/> class.
        /// </summary>
        /// <param name="editor">The related editor.</param>
        /// <param name="asset">The related asset.</param>
        protected AssetCompositeItemViewModel([NotNull] AssetCompositeEditorViewModel editor, [NotNull] TAssetViewModel asset)
            : base(editor, asset)
        {
        }

        /// <summary>
        /// The related asset.
        /// </summary>
        [NotNull]
        public new TAssetViewModel Asset => (TAssetViewModel)base.Asset;

        /// <summary>
        /// The children collection of this item.
        /// </summary>
        [ItemNotNull, NotNull]
        public IReadOnlyObservableList<TChildViewModel> Children => children;

        /// <inheritdoc/>
        public override IEnumerable<AssetCompositeItemViewModel> EnumerateChildren() => Children;

        /// <summary>
        /// The parent of this item.
        /// </summary>
        [CanBeNull]
        protected new TParentViewModel Parent { get { return (TParentViewModel)base.Parent; } set { base.Parent = value; } }

        /// <inheritdoc/>
        public override void Destroy()
        {
            base.Destroy();
            children.Clear();
        }

        /// <summary>
        /// Adds an <paramref name="item"/> to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="item">The item to add to the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
        protected void AddItem([NotNull] TChildViewModel item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            children.Add(item);
            item.Parent = (TParentViewModel)this;
        }

        /// <summary>
        /// Adds <paramref name="items"/> to the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="items">An enumeration of items to add to the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
        protected void AddItems([NotNull] IEnumerable<TChildViewModel> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            foreach (var item in items)
            {
                children.Add(item);
                item.Parent = (TParentViewModel)this;
            }
        }

        /// <summary>
        /// Inserts an <paramref name="item"/> to the <see cref="Children"/> collection at the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
        /// <param name="item">The item to insert into the collection.</param>
        /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the collection.</exception>
        protected void InsertItem(int index, [NotNull] TChildViewModel item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            children.Insert(index, item);
            item.Parent = (TParentViewModel)this;
        }

        /// <summary>
        /// Removes the first occurence of the specified <paramref name="item"/> from the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="item">The item to remove from the collection.</param>
        /// <returns><c>true</c> if item was successfully removed from the collection; otherwise, <c>false</c>.</returns>
        protected bool RemoveItem([NotNull] TChildViewModel item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            if (!children.Remove(item))
                return false;
            item.Parent = null;
            return true;
        }

        /// <summary>
        /// Removes the item at the specified <paramref name="index"/> from the <see cref="Children"/> collection.
        /// </summary>
        /// <param name="index">The zero-based index of the item to remove.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the collection.</exception>
        protected void RemoveItemAt(int index)
        {
            var item = children[index];
            children.RemoveAt(index);
            item.Parent = null;
        }
    }

    /// <summary>
    /// A view model representing an item, real or virtual, of a hierarchical composite asset.
    /// </summary>
    /// <typeparam name="TAssetViewModel">The type of the related asset.</typeparam>
    /// <typeparam name="TItemViewModel">The type of the parent and child items.</typeparam>
    public abstract class AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel> : AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel, TItemViewModel>
        where TAssetViewModel : AssetViewModel
        where TItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TItemViewModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetCompositeItemViewModel{TAssetViewModel,TItemViewModel}"/> class.
        /// </summary>
        /// <param name="editor">The related editor.</param>
        /// <param name="asset">The related asset.</param>
        protected AssetCompositeItemViewModel([NotNull] AssetCompositeEditorViewModel editor, [NotNull] TAssetViewModel asset)
            : base(editor, asset)
        {
        }
    }

}
