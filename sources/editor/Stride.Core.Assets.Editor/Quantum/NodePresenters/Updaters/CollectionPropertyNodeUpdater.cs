// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Quantum.NodePresenters.Updaters
{
    internal sealed class CollectionPropertyNodeUpdater : AssetNodePresenterUpdaterBase
    {
        protected override void UpdateNode(IAssetNodePresenter node)
        {
            MemberCollectionAttribute memberCollection;
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
                if (memberCollection.ReadOnly)
                {
                    node.AttachedProperties.Add(CollectionData.ReadOnlyCollectionKey, true);
                }
            }

            // Check if this is an item within a reorderable collection
            var parentNode = node.Parent;
            if (parentNode != null && node is ItemNodePresenter)
            {
                MemberCollectionAttribute parentCollection;
                if (parentNode is MemberNodePresenter parentMemberNode && parentMemberNode.IsEnumerable)
                {
                    parentCollection = parentMemberNode.MemberAttributes.OfType<MemberCollectionAttribute>().FirstOrDefault();
                }
                else
                {
                    parentCollection = parentNode.Descriptor.Attributes.OfType<MemberCollectionAttribute>().FirstOrDefault()
                                       ?? TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<MemberCollectionAttribute>(parentNode.Type);
                }

                if (parentCollection?.CanReorderItems == true)
                {
                    node.AttachedProperties.Add(CollectionData.ReorderCollectionItemKey, new ReorderCollectionItemViewModel());
                }
            }
        }
    }
}
