// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Extensions;
using Stride.Editor.Engine;

namespace Stride.Assets.Editor.ViewModels;

[AssetEditorViewModel<PrefabViewModel>]
public sealed class PrefabEditorViewModel : EntityHierarchyEditorViewModel, IAssetEditorViewModel<PrefabViewModel>
{
    public PrefabEditorViewModel(PrefabViewModel asset)
        : base(asset)
    {
    }

    /// <inheritdoc />
    public override PrefabViewModel Asset => (PrefabViewModel)base.Asset;

    /// <inheritdoc />
    protected override PrefabRootViewModel CreateRootPartViewModel()
    {
        return new PrefabRootViewModel(Asset);
    }

    /// <inheritdoc />
    protected override EntityHierarchyEditorController CreateController(IEditorGameViewportService viewportService)
    {
        return new PrefabEditorController(this, viewportService);
    }

    /// <inheritdoc />
    protected override async Task OnGameContentLoaded()
    {
        await base.OnGameContentLoaded();

        // The whole prefab is always loaded, as in the WPF prefab editor
        var root = (PrefabRootViewModel)HierarchyRoot;
        await ((PrefabEditorController)Controller!).LoadEntities(root);
        await LinkToGameSide(root.Children.BreadthFirst(x => x.Children).OfType<EntityViewModel>());
    }
}
