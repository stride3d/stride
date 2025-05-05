// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Presentation.ViewModels;

/// <summary>
/// A view model representing an item, real or virtual, of a hierarchical composite asset.
/// </summary>
public abstract class AssetCompositeItemViewModel : DispatcherViewModel
{
    private AssetCompositeItemViewModel? parent;

    protected AssetCompositeItemViewModel(AssetViewModel asset)
        : base(asset.ServiceProvider)
    {
        Asset = asset;
    }

    /// <summary>
    /// The related asset.
    /// </summary>
    public virtual AssetViewModel Asset { get; }

    /// <summary>
    /// Gets or sets the name of this item.
    /// </summary>
    public abstract string? Name { get; set; }

    /// <summary>
    /// The parent of this item.
    /// </summary>
    public AssetCompositeItemViewModel? Parent
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

    /// <summary>
    /// Enumerates all child items of this <see cref="AssetCompositeItemViewModel"/>.
    /// </summary>
    /// <returns>A sequence containing all child items of this instance.</returns>
    public abstract IEnumerable<AssetCompositeItemViewModel> EnumerateChildren();

    /// <summary>
    /// Gets the path to this item in the asset.
    /// </summary>
    /// <remarks>In case of a virtual node, this method should return an equivalent path if possible; otherwise the path the the closest non-virtual ancestor item.</remarks>
    /// <seealso cref="IAssetPropertyProviderViewModel.GetAbsolutePathToRootNode"/>>
    public abstract GraphNodePath GetNodePath();

    protected IObjectNode GetNode() => Asset.Session.AssetNodeContainer.GetNode(Asset.Asset);
}

/// <summary>
/// A view model representing an item, real or virtual, of a hierarchical composite asset.
/// </summary>
/// <typeparam name="TAssetViewModel">The type of the related asset.</typeparam>
/// <typeparam name="TParentItemViewModel">The type of the parent item.</typeparam>
/// <typeparam name="TChildItemViewModel">The type of the child items.</typeparam>
public abstract class AssetCompositeItemViewModel<TAssetViewModel, TParentItemViewModel, TChildItemViewModel> : AssetCompositeItemViewModel
    where TAssetViewModel : AssetViewModel
    where TParentItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TParentItemViewModel, TChildItemViewModel>
    where TChildItemViewModel : AssetCompositeItemViewModel<TAssetViewModel, TParentItemViewModel, TChildItemViewModel>
{
    private readonly ObservableList<TChildItemViewModel> children = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetCompositeItemViewModel{TAssetViewModel,TParentViewModel,TChildViewModel}"/> class.
    /// </summary>
    /// <param name="asset">The related asset.</param>
    protected AssetCompositeItemViewModel(TAssetViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc/>
    public override TAssetViewModel Asset => (TAssetViewModel)base.Asset;

    public IReadOnlyObservableList<TChildItemViewModel> Children => children;

    /// <summary>
    /// Adds an <paramref name="item"/> to the <see cref="Children"/> collection.
    /// </summary>
    /// <param name="item">The item to add to the collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    protected void AddItem(TChildItemViewModel item)
    {
        children.Add(item);
        item.Parent = (TParentItemViewModel)this;
    }

    /// <summary>
    /// Adds <paramref name="items"/> to the <see cref="Children"/> collection.
    /// </summary>
    /// <param name="items">An enumeration of items to add to the collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="items"/> is <c>null</c>.</exception>
    protected void AddItems(IEnumerable<TChildItemViewModel> items)
    {
        foreach (var item in items)
        {
            children.Add(item);
            item.Parent = (TParentItemViewModel)this;
        }
    }

    /// <inheritdoc/>
    public override void Destroy()
    {
        base.Destroy();
        children.Clear();
    }

    /// <summary>
    /// Enumerates all child items of this <see cref="AssetCompositeItemViewModel"/>.
    /// </summary>
    /// <returns>A sequence containing all child items of this instance.</returns>
    public override IEnumerable<AssetCompositeItemViewModel> EnumerateChildren() => Children;

    /// <summary>
    /// Inserts an <paramref name="item"/> to the <see cref="Children"/> collection at the specified <paramref name="index"/>.
    /// </summary>
    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param>
    /// <param name="item">The item to insert into the collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the collection.</exception>
    protected void InsertItem(int index, TChildItemViewModel item)
    {
        children.Insert(index, item);
        item.Parent = (TParentItemViewModel)this;
    }

    /// <summary>
    /// Removes the first occurence of the specified <paramref name="item"/> from the <see cref="Children"/> collection.
    /// </summary>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns><c>true</c> if item was successfully removed from the collection; otherwise, <c>false</c>.</returns>
    protected bool RemoveItem(TChildItemViewModel item)
    {
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
    protected AssetCompositeItemViewModel(TAssetViewModel asset)
        : base(asset)
    {
    }
}
