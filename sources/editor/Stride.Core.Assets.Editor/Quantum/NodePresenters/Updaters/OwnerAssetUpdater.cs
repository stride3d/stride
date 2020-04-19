// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class OwnerAssetUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (node.Asset != null)
            {
                node.AttachedProperties.Add(OwnerAssetData.Key, node.Asset);
            }
        }
    }
}
