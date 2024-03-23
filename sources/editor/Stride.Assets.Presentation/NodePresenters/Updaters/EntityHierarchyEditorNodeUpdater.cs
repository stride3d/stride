// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class EntityHierarchyEditorNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Root.Value is not Entity entity || node.Asset is null || !TryGetEditor(node.Asset, out var editor))
                return;

            var partId = new AbsoluteId(node.Asset.Id, entity.Id);
            var viewModel = editor.FindPartViewModel(partId) as EntityHierarchyElementViewModel;
            viewModel?.UpdateNodePresenter(node);
        }

        protected override void FinalizeTree(IAssetNodePresenter root)
        {
            if (root.Value is not Entity entity || root.Asset is null || !TryGetEditor(root.Asset, out var editor))
                return;

            var partId = new AbsoluteId(root.Asset.Id, entity.Id);
            var viewModel = editor.FindPartViewModel(partId) as EntityHierarchyElementViewModel;
            viewModel?.FinalizeNodePresenterTree(root);
            base.FinalizeTree(root);
        }

        private static bool TryGetEditor(AssetViewModel asset, out EntityHierarchyEditorViewModel editor)
        {
            return asset.ServiceProvider.Get<IAssetEditorsManager>().TryGetAssetEditor(asset, out editor);
        }
    }
}
