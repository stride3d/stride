// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters;
using Xenko.Core;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.NodePresenters.Updaters
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
