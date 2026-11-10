// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.ViewModels;
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
        return new PrefabRootViewModel(Asset, this);
    }

    /// <inheritdoc />
    protected override EntityHierarchyEditorController CreateController(IEditorGameViewportService viewportService)
    {
        return new PrefabEditorController(this, viewportService);
    }

    /// <inheritdoc />
    protected override async Task LoadHierarchyAsync()
    {
        await base.LoadHierarchyAsync();

        // The whole prefab is always loaded, as in the WPF prefab editor
        await HierarchyRoot.RequestLoading(true);
    }

    /// <inheritdoc />
    protected override Task LoadRootsIntoGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        var controller = (PrefabEditorController)Controller!;
        return Task.WhenAll(roots.Cast<PrefabRootViewModel>().Select(controller.LoadEntities));
    }

    /// <inheritdoc />
    protected override Task UnloadRootsFromGameAsync(IReadOnlyCollection<EntityHierarchyRootViewModel> roots)
    {
        var controller = (PrefabEditorController)Controller!;
        return Task.WhenAll(roots.Cast<PrefabRootViewModel>().Select(controller.UnloadEntities));
    }
}
