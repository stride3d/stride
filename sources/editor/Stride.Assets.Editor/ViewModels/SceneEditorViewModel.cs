// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Editor.Engine;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Assets.Editor.AssetEditors.GameEditor;

namespace Stride.Assets.Editor.ViewModels;

/// <summary>
/// The editor of a scene, with its child scenes.
/// </summary>
[AssetEditorViewModel<SceneViewModel>]
public sealed class SceneEditorViewModel : EntityHierarchyEditorViewModel, IAssetEditorViewModel<SceneViewModel>, IMultipleAssetEditorViewModel
{
    private readonly ObservableSet<SceneViewModel> allScenes = [];

    public SceneEditorViewModel(SceneViewModel asset)
        : base(asset)
    {
        // Every scene of the hierarchy is open in this editor
        foreach (var sceneRoot in SceneRoots(HierarchyRoot))
        {
            allScenes.Add(sceneRoot.SceneAsset);
            sceneRoot.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
        }
    }

    public override SceneViewModel Asset => (SceneViewModel)base.Asset;

    /// <summary>
    /// Gets the scenes open in this editor: the edited scene and its child scenes, in no particular order.
    /// </summary>
    public IReadOnlyObservableList<SceneViewModel> AllScenes => allScenes;

    /// <inheritdoc/>
    IReadOnlyObservableList<AssetViewModel> IMultipleAssetEditorViewModel.OpenedAssets => allScenes;

    /// <inheritdoc/>
    public override void Destroy()
    {
        EnsureNotDestroyed(nameof(SceneEditorViewModel));

        // The settings of the edited scene are saved by the base editor
        var root = (SceneRootViewModel)HierarchyRoot;
        foreach (var scene in root.ChildScenes.BreadthFirst(x => x.ChildScenes))
        {
            scene.SaveSettings();
            scene.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
        }
        root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
        base.Destroy();
    }

    protected override SceneRootViewModel CreateRootPartViewModel()
    {
        return new SceneRootViewModel(Asset, this);
    }

    /// <inheritdoc/>
    protected override Task Delete()
    {
        // Selected child scenes only: they are removed from their parent scene
        var sceneRoots = SelectedContent.OfType<SceneRootViewModel>().ToList();
        if (sceneRoots.Count == 0 || sceneRoots.Count != SelectedContent.Count)
            return base.Delete();

        var undoRedo = Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        ClearSelection();
        foreach (var sceneRoot in GetCommonRoots(sceneRoots))
        {
            sceneRoot.Delete();
        }
        undoRedo.SetName(transaction, "Remove selected child scenes");
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    protected override async Task RefreshEditorProperties()
    {
        // A selected scene shows its own properties
        if (SelectedContent.Count == 1 && SelectedContent[0] is SceneRootViewModel sceneRoot)
        {
            EditorProperties.Name = sceneRoot.Name;
            EditorProperties.TypeDescription = "Scene";
            await EditorProperties.GenerateSelectionPropertiesAsync(sceneRoot.Yield());
            return;
        }
        await base.RefreshEditorProperties();
    }

    /// <inheritdoc/>
    /// <remarks>A child scene moved in the game gets a new offset.</remarks>
    protected override void UpdateTransformations(EntityHierarchyElementViewModel element, TransformationTRS transformation)
    {
        if (element is SceneRootViewModel root)
        {
            // Update the offset only when it actually changed
            if (root.Offset != transformation.Position)
                root.Offset = transformation.Position;
            return;
        }
        base.UpdateTransformations(element, transformation);
    }

    /// <inheritdoc />
    protected override EntityHierarchyEditorController CreateController(IEditorGameViewportService viewportService)
    {
        return new SceneEditorController(this, viewportService);
    }

    /// <inheritdoc />
    protected override async Task LoadHierarchyAsync()
    {
        await base.LoadHierarchyAsync();

        // The edited scene is always loaded; its child scenes load as they were when the editor closed
        await HierarchyRoot.RequestLoading(true);
    }

    /// <inheritdoc />
    protected override Task LoadRootsIntoGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        return ((SceneEditorController)Controller!).LoadScenes(roots.Cast<SceneRootViewModel>().ToList());
    }

    /// <inheritdoc />
    protected override Task UnloadRootsFromGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        return ((SceneEditorController)Controller!).UnloadScenes(roots.Cast<SceneRootViewModel>());
    }

    /// <inheritdoc />
    public override Task UpdateSceneOffsetAsync(SceneRootViewModel root)
    {
        return Controller is SceneEditorController controller ? controller.UpdateSceneAnchorPosition(root) : Task.CompletedTask;
    }

    private static IEnumerable<SceneRootViewModel> SceneRoots(EntityHierarchyRootViewModel root)
    {
        return new[] { (SceneRootViewModel)root }.BreadthFirst(x => x.ChildScenes);
    }

    private void ChildScenesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // A scene that leaves the hierarchy, with its child scenes, is no longer open in this editor
        foreach (var root in e.OldItems?.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes) ?? [])
        {
            root.ChildScenes.CollectionChanged -= ChildScenesCollectionChanged;
            allScenes.Remove(root.SceneAsset);
        }
        foreach (var root in e.NewItems?.Cast<SceneRootViewModel>().BreadthFirst(x => x.ChildScenes) ?? [])
        {
            allScenes.Add(root.SceneAsset);
            root.ChildScenes.CollectionChanged += ChildScenesCollectionChanged;
        }
    }
}
