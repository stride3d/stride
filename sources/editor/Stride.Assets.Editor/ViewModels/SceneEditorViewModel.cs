// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<SceneViewModel>]
public sealed class SceneEditorViewModel : EntityHierarchyEditorViewModel, IAssetEditorViewModel<SceneViewModel>
{
    public SceneEditorViewModel(SceneViewModel asset)
        : base(asset)
    {
    }

    public override SceneViewModel Asset => (SceneViewModel)base.Asset;

    protected override SceneRootViewModel CreateRootPartViewModel()
    {
        return new SceneRootViewModel(Asset, this);
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
}
