// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Assets.Editor.Quantum.NodePresenters;
using Stride.Core.Quantum;
using Stride.Assets.Models;
using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.NodePresenters.Updaters
{
    internal sealed class SkeletonAssetNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.Asset is SkeletonViewModel))
                return;

            if (typeof(NodeInformation).IsAssignableFrom(node.Type) && node.IsVisible)
            {
                // Hide all children
                foreach (var child in node.Children)
                {
                    child.IsVisible = false;
                }
                var name = (string)node[nameof(NodeInformation.Name)].Value;
                var depth = (int)node[nameof(NodeInformation.Depth)].Value;
                // Set the display name to be the name of the node, indented using space.
                node.DisplayName = $"{"".PadLeft(2 * depth)}{name}";
                // Set the order to be the index, we don't want to sort alphabetically
                var index = (node as AssetItemNodePresenter)?.Index ?? NodeIndex.Empty;
                if (index.IsInt)
                {
                    node.Order = index.Int;
                }
            }
        }
    }
}
