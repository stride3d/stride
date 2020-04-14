// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    public sealed class ArchetypeNodeUpdater : AssetNodePresenterUpdaterBase
    {
        public const string ArchetypeNodeName = "ArchetypeVirtual";

        protected override void UpdateNode(IAssetNodePresenter node)
        {
            if (!(node.PropertyProvider is AssetViewModel) || node.Asset == null)
                return;

            // Add a link to the archetype in the root node, if there is an archetype for this asset.
            if (typeof(Asset).IsAssignableFrom(node.Type) && node.Asset.Asset.Archetype != null)
            {
                var session = node.Asset.Session;
                var archetype = session.GetAssetById(node.Asset.Asset.Archetype.Id);
                var assetReference = ContentReferenceHelper.CreateReference<AssetReference>(archetype);
                var archetypeNode = node.Factory.CreateVirtualNodePresenter(node, ArchetypeNodeName, typeof(AssetReference), int.MinValue, () => assetReference);
                archetypeNode.DisplayName = nameof(Asset.Archetype);
                archetypeNode.IsReadOnly = true;
            }
        }
    }
}
