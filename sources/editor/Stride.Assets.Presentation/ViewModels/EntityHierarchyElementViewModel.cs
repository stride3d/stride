// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Assets.Entities;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// An element of the hierarchy of a scene or a prefab that has entities as children of its transform: the root of the
/// hierarchy or an entity.
/// </summary>
public abstract class EntityHierarchyElementViewModel : EntityHierarchyItemViewModel, IAssetPropertyProviderViewModel
{
    private readonly object? assetSideInstance;
    private bool isLoaded;
    private bool isLoading;
    private bool isLocked;

    protected EntityHierarchyElementViewModel(EntityHierarchyViewModel asset, IEnumerable<EntityDesign> childEntities, object? assetSideInstance)
        : base(asset, childEntities)
    {
        this.assetSideInstance = assetSideInstance;
        LoadCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLoading(!IsLoaded, recursive));
        LockCommand = new AnonymousTaskCommand<bool>(ServiceProvider, recursive => RequestLocking(!IsLocked, recursive));
        UpdateCommands();

        DependentProperties.Add(nameof(IsLoaded), [nameof(IsSelectable)]);
        DependentProperties.Add(nameof(IsLocked), [nameof(IsSelectable)]);
    }

    /// <summary>
    /// Indicates whether this element is currently loaded in the game of the editor.
    /// </summary>
    /// <remarks>
    /// When an element is loaded in the game, it increases the reference count of the content it references. Unloading an
    /// element decreases this count and might also unload content when the corresponding counter reaches zero.
    /// </remarks>
    public bool IsLoaded
    {
        get => isLoaded;
        protected set
        {
            SetValue(ref isLoaded, value, () =>
            {
                IsLoading = false;
                UpdateCommands();
                NotifySelectableChanged();
            });
        }
    }

    /// <summary>
    /// Indicates whether this element is currently being loaded in (or unloaded from) the game of the editor.
    /// </summary>
    public bool IsLoading { get => isLoading; protected set => SetValue(ref isLoading, value, UpdateCommands); }

    /// <summary>
    /// Gets whether this element is locked in the view of the editor: a locked entity cannot be selected there, hence it
    /// cannot be moved with the gizmos. It can still be selected in the hierarchy.
    /// </summary>
    public bool IsLocked { get => isLocked; protected set => SetValue(ref isLocked, value, NotifySelectableChanged); }

    /// <summary>
    /// Indicates whether this element can be selected in the view of the editor: loaded and not locked.
    /// </summary>
    public bool IsSelectable => IsLoaded && !IsLocked;

    /// <summary>
    /// Gets the command that loads or unloads this element. Its parameter says whether the children are loaded or
    /// unloaded too, recursively.
    /// </summary>
    public ICommandBase LoadCommand { get; }

    /// <summary>
    /// Gets the command that locks or unlocks this element. Its parameter says whether the children are locked or unlocked
    /// too, recursively (Ctrl in the editor).
    /// </summary>
    public ICommandBase LockCommand { get; }

    /// <inheritdoc/>
    public override EntityHierarchyElementViewModel Owner => this;

    /// <summary>
    /// Gets the number of entities whose transform is a child of the transform of this element, folders ignored.
    /// </summary>
    public abstract int EntityCount { get; }

    bool IPropertyProviderViewModel.CanProvidePropertiesViewModel => true;

    AssetViewModel IAssetPropertyProviderViewModel.RelatedAsset => Asset;

    /// <inheritdoc/>
    public override int FindIndexInParent(int indexInEntity, IEnumerable<EntityDesign> entityCollection)
    {
        // The entities without a folder, before the given one
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
    /// Gets the index of the given entity in the children of the transform of this element, folders ignored.
    /// </summary>
    /// <returns>The index, or -1 when the entity is not a child of this element.</returns>
    public abstract int IndexOfEntity(EntityViewModel entity);

    /// <summary>
    /// Requests to change the <see cref="IsLoaded"/> property.
    /// </summary>
    /// <param name="load"><c>true</c> to load, <c>false</c> to unload.</param>
    /// <param name="recursive"><c>true</c> to also load the children recursively; otherwise, <c>false</c>.</param>
    public Task RequestLoading(bool load, bool recursive = false)
    {
        if (IsLoading || IsLoaded == load)
            return Task.CompletedTask;

        return OnLoadingRequested(load, recursive);
    }

    /// <summary>
    /// Requests to change the <see cref="IsLocked"/> property. Only a loaded element can be locked.
    /// </summary>
    /// <param name="lock"><c>true</c> to lock, <c>false</c> to unlock.</param>
    /// <param name="recursive"><c>true</c> to also lock the children recursively; otherwise, <c>false</c>. They always are
    /// for a root.</param>
    public Task RequestLocking(bool @lock, bool recursive = false)
    {
        if (!IsLoaded || IsLocked == @lock)
            return Task.CompletedTask;

        return OnLockingRequested(@lock, recursive);
    }

    /// <summary>
    /// Sets <see cref="IsLoaded"/> for a change that the hierarchy itself makes: an entity added to a loaded root, or the
    /// entities of a root that loads.
    /// </summary>
    internal void SetIsLoaded(bool value) => IsLoaded = value;

    /// <summary>
    /// Sets <see cref="IsLocked"/> for a change that the hierarchy itself makes: the entities of a root that locks.
    /// </summary>
    internal void SetIsLocked(bool value) => IsLocked = value;

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

    private void NotifySelectableChanged()
    {
        GetTopRoot()?.NotifySelectableChanged(this);
    }

    /// <inheritdoc/>
    IObjectNode IPropertyProviderViewModel.GetRootNode()
    {
        return GetPropertiesRootNode();
    }

    /// <summary>
    /// Gets the node whose properties the property grid shows for this element.
    /// </summary>
    protected virtual IObjectNode GetPropertiesRootNode()
    {
        return Asset.Session.AssetNodeContainer.GetOrCreateNode(assetSideInstance)!;
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
