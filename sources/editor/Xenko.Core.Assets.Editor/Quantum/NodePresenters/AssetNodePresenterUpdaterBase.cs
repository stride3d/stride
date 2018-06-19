// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;

namespace Xenko.Core.Assets.Editor.Quantum.NodePresenters
{
    public abstract class AssetNodePresenterUpdaterBase : NodePresenterUpdaterBase
    {
        public sealed override void UpdateNode(INodePresenter node)
        {
            var assetNode = node as IAssetNodePresenter;
            if (assetNode != null)
            {
                UpdateNode(assetNode);
            }
        }

        public sealed override void FinalizeTree(INodePresenter root)
        {
            var assetNode = root as IAssetNodePresenter;
            if (assetNode != null)
            {
                FinalizeTree(assetNode);
            }
        }

        protected virtual void UpdateNode([NotNull] IAssetNodePresenter node)
        {

        }

        protected virtual void FinalizeTree([NotNull] IAssetNodePresenter root)
        {

        }
    }
}
