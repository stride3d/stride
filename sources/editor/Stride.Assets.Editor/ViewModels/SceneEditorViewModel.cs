// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
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
        return new SceneRootViewModel(Asset);
    }

    /// <inheritdoc />
    protected override EntityHierarchyEditorController CreateController(IEditorGameViewportService viewportService)
    {
        return new SceneEditorController(this, viewportService);
    }

    /// <inheritdoc />
    protected override async Task OnGameContentLoaded()
    {
        await base.OnGameContentLoaded();

        // The child scenes are not supported yet: the scene is loaded alone
        var root = (SceneRootViewModel)HierarchyRoot;
        await ((SceneEditorController)Controller!).LoadScenes([root]);
        await LinkToGameSide(root.Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>());
    }
}
