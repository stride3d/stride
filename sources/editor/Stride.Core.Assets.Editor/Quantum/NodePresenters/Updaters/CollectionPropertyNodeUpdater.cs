// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Presentation.Quantum.NodePresenters;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters;

internal sealed class CollectionPropertyNodeUpdater : AssetNodePresenterUpdaterBase
{
    protected override void UpdateNode(IAssetNodePresenter node)
    {
        MemberCollectionAttribute? memberCollection;
        if (node is MemberNodePresenter memberNode && memberNode.IsEnumerable)
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
            //if (memberCollection.CanReorderItems)
            //    node.AttachedProperties.Add(CollectionData.ReorderCollectionItemKey, new ReorderCollectionItemViewModel());
            if (memberCollection.ReadOnly)
                node.AttachedProperties.Add(CollectionData.ReadOnlyCollectionKey, true);
        }
    }
}
