// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class AssetReplacesNodeUpdater : AssetNodePresenterUpdaterBase
    {
        public const string ReplacesNodeName = "ReplacesVirtual";
        public const string ReplacedByNodeName = "ReplacedByVirtual";

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.PropertyProvider is AssetViewModel) || node.Asset == null)
                return;

            if (!typeof(Asset).IsAssignableFrom(node.Type))
                return;

            var session = node.Asset.Session;

            // Forward: if this asset replaces another, add a link to the replaced asset.
            if (node.Asset.Asset.Replaces is { } replaces)
            {
                var target = session.GetAssetById(replaces.Id);
                // An unresolved target renders as a broken reference, which is the diagnostic we want
                var assetReference = target != null ? ContentReferenceHelper.CreateReference<AssetReference>(target) : replaces;
                var replacesNode = node.Factory.CreateVirtualNodePresenter(node, ReplacesNodeName, typeof(AssetReference), int.MinValue + 1, () => assetReference);
                replacesNode.DisplayName = nameof(Asset.Replaces);
                replacesNode.IsReadOnly = true;
            }

            // Reverse: if another asset replaces this one, add a link to that replacing asset.
            if (FindReplacer(session, node.Asset.Id) is { } replacer)
            {
                var replacerReference = ContentReferenceHelper.CreateReference<AssetReference>(replacer);
                var replacedByNode = node.Factory.CreateVirtualNodePresenter(node, ReplacedByNodeName, typeof(AssetReference), int.MinValue + 2, () => replacerReference);
                replacedByNode.DisplayName = "Replaced by";
                replacedByNode.IsReadOnly = true;
            }
        }

        /// <summary>
        /// Finds the asset that replaces <paramref name="targetId"/> via the incoming Replace links.
        /// At most one replacement per asset is valid.
        /// </summary>
        private static AssetViewModel FindReplacer(SessionViewModel session, AssetId targetId)
        {
            var dependencies = session.DependencyManager.ComputeDependencies(targetId, AssetDependencySearchOptions.In, ContentLinkType.Replace);
            if (dependencies == null)
                return null;
            foreach (var link in dependencies.LinksIn)
            {
                if (session.GetAssetById(link.Item.Id) is { } replacer)
                    return replacer;
            }
            return null;
        }
    }
}
