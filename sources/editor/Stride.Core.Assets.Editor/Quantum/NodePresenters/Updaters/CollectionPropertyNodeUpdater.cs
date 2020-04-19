// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class CollectionPropertyNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            var memberNode = node as MemberNodePresenter;
            MemberCollectionAttribute memberCollection;
            if (memberNode != null && memberNode.IsEnumerable)
            {
                memberCollection = memberNode.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
            }
            else
            {
                memberCollection = node.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                   ?? TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<MemberCollectionAttribute>(node.Type);
            }
            if (memberCollection != null)
            {
                if (memberCollection.CanReorderItems)
                    node.AttachedProperties.Add(CollectionData.ReorderCollectionItemKey, new ReorderCollectionItemViewModel());
                if (memberCollection.ReadOnly)
                    node.AttachedProperties.Add(CollectionData.ReadOnlyCollectionKey, true);
            }
        }
    }
}
