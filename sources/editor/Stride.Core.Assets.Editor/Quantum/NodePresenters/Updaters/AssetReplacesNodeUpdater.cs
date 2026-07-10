// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class AssetReplacesNodeUpdater : AssetNodePresenterUpdaterBase
    {
        public const string ReplacesNodeName = "ReplacesVirtual";

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.PropertyProvider is AssetViewModel) || node.Asset == null)
                return;

            // Add a link to the replaced asset in the root node, if this asset replaces one.
            if (typeof(Asset).IsAssignableFrom(node.Type) && node.Asset.Asset.Replaces is { } replaces)
            {
                var session = node.Asset.Session;
                var target = session.GetAssetByUrl(replaces.FullPath);
                // An unresolved target renders as a broken reference, which is the diagnostic we want
                var assetReference = target != null ? ContentReferenceHelper.CreateReference<AssetReference>(target) : new AssetReference(AssetId.Empty, replaces);
                var replacesNode = node.Factory.CreateVirtualNodePresenter(node, ReplacesNodeName, typeof(AssetReference), int.MinValue + 1, () => assetReference);
                replacesNode.DisplayName = nameof(Asset.Replaces);
                replacesNode.IsReadOnly = true;
            }
        }
    }
}
