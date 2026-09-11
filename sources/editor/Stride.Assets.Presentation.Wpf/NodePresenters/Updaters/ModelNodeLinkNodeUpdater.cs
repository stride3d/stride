// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Assets.Models;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    public sealed class ModelNodeLinkNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var entity = node.Root.Value as Entity;
            var asset = node.Asset;
            if (asset == null || entity == null)
                return;

            if (node.Name == nameof(ModelNodeLinkComponent.Target) && node.Parent?.Value is ModelNodeLinkComponent)
            {
                var parent = (IAssetNodePresenter)node.Parent;
                parent.AttachedProperties.Set(ModelNodeLinkData.Key, GetAvailableNodesForLink(asset, (ModelNodeLinkComponent)parent?.Value));
            }
        }

        private static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, ModelNodeLinkComponent modelNodeLinkComponent)
        {
            return GetAvailableNodesForLink(viewModel, modelNodeLinkComponent?.Target?.Model ?? modelNodeLinkComponent?.Entity?.Transform.Parent?.Entity?.Get<ModelComponent>()?.Model);
        }

        /// <summary>
        /// The skeleton nodes a component on <paramref name="viewModel"/>'s asset can link to through <paramref name="model"/>,
        /// for a plugin's own node-name picker (set <see cref="ModelNodeLinkData.Key"/> with it).
        /// </summary>
        public static IEnumerable<NodeInformation> GetAvailableNodesForLink(AssetViewModel viewModel, Model model)
        {
            var parentModelAsset = viewModel?.AssetItem.Package.Session.FindAssetFromProxyObject(model);
            var modelAsset = parentModelAsset?.Asset as ModelAsset;
            if (modelAsset != null)
            {
                var skeletonAsset = parentModelAsset.Package.FindAssetFromProxyObject(modelAsset.Skeleton);
                if (skeletonAsset != null)
                {
                    return ((SkeletonAsset)skeletonAsset.Asset).Nodes;
                }
            }
            return Enumerable.Empty<NodeInformation>();
        }
    }
}
