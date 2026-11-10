// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Core.Serialization;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Assets.Presentation.ViewModels;

/// <summary>
/// The root of a scene in the hierarchy of the scene editor: the edited scene, or one of its child scenes, which come
/// after its folders and entities.
/// </summary>
[DataSerializerGlobal(null, typeof(List<Entity>))]
[DebuggerDisplay("SceneRoot = {" + nameof(Name) + "}")]
public sealed class SceneRootViewModel : EntityHierarchyRootViewModel
{
    private readonly ObservableSet<SceneRootViewModel> childScenes = [];
    private readonly AsyncLock loadMutex;
    private readonly MemberGraphNodeBinding<Vector3> offsetNodeBinding;

    /// <summary>
    /// Identifier of the game-side scene.
    /// </summary>
    private readonly Guid sceneId;

    /// <param name="scene">The edited scene.</param>
    /// <param name="editor">The editor of the scene, or <c>null</c> when the hierarchy has no editor.</param>
    public SceneRootViewModel(SceneViewModel scene, IEntityHierarchyEditorViewModel? editor)
        : this(scene, editor, new AsyncLock(), null)
    {
    }

    private SceneRootViewModel(SceneViewModel scene, IEntityHierarchyEditorViewModel? editor, AsyncLock loadMutex, SceneRootViewModel? parent)
        : base(scene, editor)
    {
        this.loadMutex = loadMutex;
        sceneId = Guid.NewGuid();
        // The parent is known before the child scenes, which compute their offset from it
        Parent = parent;

        foreach (var childScene in scene.Children)
        {
            childScenes.Add(new SceneRootViewModel(childScene, editor, loadMutex, this));
        }
        AddItems(ChildScenes);
        scene.Children.CollectionChanged += SceneChildrenCollectionChanged;

        var offsetNode = scene.Session.AssetNodeContainer.GetNode(scene.Asset)![nameof(scene.Asset.Offset)];
        offsetNodeBinding = new MemberGraphNodeBinding<Vector3>(offsetNode, nameof(Offset), OnPropertyChanging, OnPropertyChanged, UndoRedoService);

        LoadSettings().Forget();
    }

    /// <summary>
    /// Gets the child scenes of this scene.
    /// </summary>
    public IReadOnlyObservableList<SceneRootViewModel> ChildScenes => childScenes;

    /// <inheritdoc/>
    public override AbsoluteId Id => new(Asset.Id, sceneId);

    /// <inheritdoc/>
    public override IEnumerable<EntityViewModel> InnerSubEntities => ChildrenExceptScenes.SelectMany(x => x.InnerSubEntities);

    /// <inheritdoc/>
    /// <remarks>The name of the scene.</remarks>
    public override string? Name { get => Asset.Name; set => throw new NotSupportedException($"Cannot change the name of a {nameof(SceneRootViewModel)} object."); }

    /// <summary>
    /// Gets or sets the offset of this scene from its parent scene.
    /// </summary>
    public Vector3 Offset { get => offsetNodeBinding.Value; set => offsetNodeBinding.Value = value; }

    /// <summary>
    /// Gets the parent scene of this scene, or <c>null</c> for the edited scene.
    /// </summary>
    public SceneRootViewModel? ParentScene => (SceneRootViewModel?)Parent;

    /// <summary>
    /// Gets the scene asset.
    /// </summary>
    public SceneViewModel SceneAsset => (SceneViewModel)Asset;

    /// <summary>
    /// Index of the first scene in the <see cref="AssetCompositeItemViewModel{TAssetViewModel,TParentViewModel,TChildViewModel}.Children"/> property.
    /// </summary>
    /// <remarks>
    /// Scene are inserted after all entities and folders.
    /// </remarks>
    private int ChildScenesStartingIndex => Children.Count - ChildScenes.Count;

    private IEnumerable<EntityHierarchyItemViewModel> ChildrenExceptScenes => Children.Where(x => x is not SceneRootViewModel);

    /// <summary>
    /// Removes this scene from the children of its parent scene.
    /// </summary>
    public void Delete()
    {
        // Reset parenting link
        ParentScene?.SceneAsset.Children.Remove(SceneAsset);
    }

    /// <inheritdoc />
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SceneRootViewModel));

        offsetNodeBinding.Dispose();
        SceneAsset.Children.CollectionChanged -= SceneChildrenCollectionChanged;
        base.Destroy();
    }

    /// <summary>
    /// Saves whether this scene is loaded, for the next time its editor opens.
    /// </summary>
    public void SaveSettings()
    {
        Editor?.SaveSceneLoaded(this);
    }

    /// <inheritdoc />
    protected override IObjectNode GetPropertiesRootNode()
    {
        // Display the asset properties
        return Asset.Session.AssetNodeContainer.GetNode(Asset.Asset)!;
    }

    /// <inheritdoc />
    protected override int GetAddIndex(IReadOnlyCollection<object> children)
    {
        var sceneCount = children.OfType<SceneViewModel>().Count();
        var rootCount = children.OfType<SceneRootViewModel>().Count();
        if (sceneCount == children.Count || rootCount == children.Count)
        {
            // Scenes are added at the end
            return Owner.EntityCount + ChildScenes.Count;
        }
        // Cannot add scene/root along other types
        if (sceneCount != 0 || rootCount != 0)
            return -1;

        return base.GetAddIndex(children);
    }

    /// <inheritdoc />
    protected override int GetInsertionIndex(InsertPosition position)
    {
        var index = 0;
        var parentRoot = ParentScene;
        if (parentRoot != null)
        {
            // Note: scene are inserted after all entities
            index = parentRoot.EntityCount + parentRoot.ChildScenes.IndexOf(this);
        }
        if (position == InsertPosition.After)
            ++index;
        return index;
    }

    /// <inheritdoc />
    protected override async Task OnLoadingRequested(bool load, bool recursive)
    {
        if (load)
        {
            using (await loadMutex.LockAsync())
            {
                // Compute the list of scenes that need loading
                var stack = new Stack<SceneRootViewModel>();
                var root = this;
                while (root != null)
                {
                    if (!root.IsLoaded)
                        stack.Push(root);
                    root = root.ParentScene;
                }
                var scenesToLoad = stack.ToList();
                if (recursive)
                {
                    // Also take child scenes, recursively
                    scenesToLoad.AddRange(ChildScenes.BreadthFirst(x => x.ChildScenes).Where(s => !s.IsLoaded));
                }
                if (scenesToLoad.Count == 0)
                    return;
                // Set the IsLoading property immediately, so users have a clear feedback
                foreach (var scene in scenesToLoad)
                    scene.IsLoading = true;
                // Load the scenes (the editor waits for its game first)
                if (Editor is { } editor)
                    await editor.LoadRootsAsync(scenesToLoad);
                foreach (var scene in scenesToLoad)
                {
                    // Loading/unloading a scene root always applies to all its content
                    scene.SetEntityIsLoadedRecursively(true);
                    scene.IsLoaded = true;
                }
            }
        }
        else
        {
            using (await loadMutex.LockAsync())
            {
                if (!IsLoaded)
                    return;
                // Compute the list of scenes that need unloading
                var scenesToUnload = new[] { this }.Concat(ChildScenes.BreadthFirst(r => r.ChildScenes).Where(r => r.IsLoaded)).Reverse().ToList();
                // Set the IsLoading property immediately, so users have a clear feedback
                foreach (var scene in scenesToUnload)
                    scene.IsLoading = true;
                foreach (var scene in scenesToUnload)
                {
                    scene.IsLoaded = false;
                    // Loading/unloading a scene root always applies to all its content
                    scene.SetEntityIsLoadedRecursively(false);
                }
                // Unload the scenes (the editor waits for its game first)
                if (Editor is { } editor)
                    await editor.UnloadRootsAsync(scenesToUnload);
            }
        }
    }

    /// <inheritdoc />
    protected override Task OnLockingRequested(bool @lock, bool recursive)
    {
        IsLocked = @lock;
        // Locking/unlocking a scene always applies to all its content
        SetEntityIsLockedRecursively(@lock);
        if (!recursive)
            return Task.CompletedTask;

        // Update IsLocked property recursively
        foreach (var scene in ChildScenes.BreadthFirst(x => x.ChildScenes))
        {
            scene.IsLocked = @lock;
            scene.SetEntityIsLockedRecursively(@lock);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(params string[] propertyNames)
    {
        base.OnPropertyChanged(propertyNames);

        if (propertyNames.Any(p => string.Equals(p, nameof(Offset))))
        {
            // Unloaded scenes don't exist on the editor game side
            if ((IsLoaded || IsLoading) && Editor is { } editor)
                editor.UpdateSceneOffsetAsync(this).Forget();
        }
    }

    /// <inheritdoc />
    protected override DropAcceptance CanAddOrInsertChildren(IReadOnlyCollection<object> children, bool checkSameParent, AddChildModifiers modifiers, int index, out string message)
    {
        message = "Selection is empty";
        var roots = children.OfType<SceneRootViewModel>().ToList();
        if (roots.Count != children.Count)
            return base.CanAddOrInsertChildren(children, checkSameParent, modifiers, index, out message);

        // Note: scene are inserted after all entities
        if (index < EntityCount)
        {
            message = "Can only add a scene to another scene";
            return DropAcceptance.Rejected;
        }
        var parentName = !string.IsNullOrWhiteSpace(Name) ? Name : "this location";
        foreach (var root in roots)
        {
            if (!SceneAsset.CanBeParentOf(root.SceneAsset, out message, checkSameParent))
            {
                return DropAcceptance.Rejected;
            }
            // Accepting child scenes
            message = $"Add as a child to {parentName}";
        }
        return DropAcceptance.Accepted;
    }

    /// <inheritdoc />
    protected override void MoveChildren(IReadOnlyCollection<object> children, AddChildModifiers modifiers, int index)
    {
        var roots = children.OfType<SceneRootViewModel>().ToList();
        if (roots.Count != children.Count)
        {
            base.MoveChildren(children, modifiers, index);
            return;
        }
        var undoRedo = UndoRedoService ?? throw new InvalidOperationException("Moving scenes needs the undo/redo service.");
        // Note: scene are inserted after all entities
        index -= EntityCount;
        using var transaction = undoRedo.CreateTransaction();
        // Move the roots in two steps: first remove all, then insert all
        foreach (var root in roots)
        {
            // Some of the roots we're moving might already be children of this object, let's count for their removal in the insertion index.
            var rootIndex = ChildScenes.IndexOf(root);
            if (rootIndex >= 0 && rootIndex < index)
                --index;

            root.ParentScene?.SceneAsset.Children.Remove(root.SceneAsset);
        }
        foreach (var root in roots)
        {
            SceneAsset.Children.Insert(index++, root.SceneAsset);
        }
        undoRedo.SetName(transaction, $"Move child scene{(roots.Count > 1 ? "s" : "")}");
    }

    /// <summary>
    /// Loads the scene when it was loaded the last time its editor closed.
    /// </summary>
    private async Task LoadSettings()
    {
        if (Editor is not { } editor)
            return;

        // The editor waits for its game first
        if (await editor.IsSceneLoadedOnOpenAsync(this) && !IsDestroyed)
            RequestLoading(true).Forget();
    }

    private void SceneChildrenCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            case NotifyCollectionChangedAction.Remove:
                if (e.OldItems?.Count > 0)
                {
                    foreach (var sceneRoot in e.OldItems.Cast<SceneViewModel>().Join(childScenes, s => s.Id, r => r.Asset.Id, (s, r) => r).ToList())
                    {
                        // Save settings first
                        sceneRoot.SaveSettings();
                        sceneRoot.ChildScenes.DepthFirst(s => s.ChildScenes).ForEach(s => s.SaveSettings());
                        // Unload the scene recursively
                        var unloadingTask = sceneRoot.RequestLoading(false);
                        // Remove the scene
                        RemoveItem(sceneRoot);
                        childScenes.Remove(sceneRoot);
                        // Update the active root
                        if (Editor is { } editor && editor.ActiveRoot == sceneRoot)
                            editor.ActiveRoot = this;
                        // Wait for the unloading operation to finish, then destroy the scene and its content, recursively.
                        unloadingTask.ContinueWith(t =>
                        {
                            Dispatcher.InvokeAsync(() => sceneRoot.Destroy()).Forget();
                        }, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted).Forget();
                    }
                }
                if (e.NewItems?.Count > 0)
                {
                    var sceneIndex = e.NewStartingIndex;
                    foreach (var sceneRoot in e.NewItems.Cast<SceneViewModel>().Select(s => new SceneRootViewModel(s, Editor, loadMutex, this)))
                    {
                        InsertItem(sceneIndex + ChildScenesStartingIndex, sceneRoot);
                        childScenes.Insert(sceneIndex++, sceneRoot);
                    }
                }
                break;
            case NotifyCollectionChangedAction.Replace:
            case NotifyCollectionChangedAction.Move:
            case NotifyCollectionChangedAction.Reset:
                throw new NotSupportedException();
            default:
                throw new ArgumentOutOfRangeException(nameof(e));
        }
    }
}
