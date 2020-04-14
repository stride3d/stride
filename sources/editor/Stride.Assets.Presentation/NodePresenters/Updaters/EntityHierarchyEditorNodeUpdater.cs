// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Engine;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class EntityHierarchyEditorNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var entity = node.Root.Value as Entity;
            if (node.Asset?.Editor == null || entity == null)
                return;

            var partId = new AbsoluteId(node.Asset.Id, entity.Id);
            var viewModel = (EntityHierarchyElementViewModel)((EntityHierarchyEditorViewModel)node.Asset.Editor).FindPartViewModel(partId);
            viewModel?.UpdateNodePresenter(node);
        }

        protected override void FinalizeTree(IAssetNodePresenter root)
        {
            var entity = root.Value as Entity;
            if (root.Asset?.Editor == null || entity == null)
                return;

            var partId = new AbsoluteId(root.Asset.Id, entity.Id);
            var viewModel = (EntityHierarchyElementViewModel)((EntityHierarchyEditorViewModel)root.Asset.Editor).FindPartViewModel(partId);
            viewModel?.FinalizeNodePresenterTree(root);
            base.FinalizeTree(root);
        }
    }
}
