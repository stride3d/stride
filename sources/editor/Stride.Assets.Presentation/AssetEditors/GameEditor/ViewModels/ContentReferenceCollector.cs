// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Serialization;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.ViewModels
{
    public class ContentReferenceCollector : AssetGraphVisitorBase
    {
        public struct ContentReferenceAccessor
        {
            public readonly AssetId ContentId;
            public readonly IGraphNode Node;
            public readonly NodeIndex Index;

            public ContentReferenceAccessor(AssetId contentId, IGraphNode node, NodeIndex index)
            {
                ContentId = contentId;
                Node = node;
                Index = index;
            }
        }

        public ContentReferenceCollector([NotNull] AssetPropertyGraphDefinition propertyGraphDefinition)
            : base(propertyGraphDefinition)
        {
        }

        public bool Registering { get; set; } = true;

        public List<ContentReferenceAccessor> ContentReferences { get; } = new List<ContentReferenceAccessor>();

        public void Reset()
        {
            ContentReferences.Clear();
        }

        public void Visit([NotNull] IAssetNode node, NodeIndex index)
        {
            if (index == NodeIndex.Empty)
            {
                // Normal case, no index
                Visit(node);
            }
            else if (node.IsReference)
            {
                // We have an index, and our collection is a collection of reference types, then start visit from the target item
                var target = ((IObjectNode)node).IndexedTarget(index);
                if (target != null)
                {
                    Visit(target);
                }
            }
            else if (AssetRegistry.IsContentType(node.Descriptor.GetInnerCollectionType()))
            {
                // We have an index, and our collection is directly a collection of content type. Let's just collect the corresponding item.
                var gameContent = node.GetContent("Game");
                var id = AttachedReferenceManager.GetAttachedReference(node.Retrieve(index))?.Id ?? AssetId.Empty;
                CollectContentReference(id, gameContent, index);
            }
        }

        protected override void VisitNode(IGraphNode node)
        {
            var assetNode = (IAssetNode)node;
            // TODO: share the proper const
            var gameContent = assetNode.GetContent("Game");
            if (gameContent != null)
            {
                var memberContent = node as IMemberNode;
                if (memberContent != null)
                {
                    if (AssetRegistry.IsContentType(memberContent.Type))
                    {
                        var id = AttachedReferenceManager.GetAttachedReference(memberContent.Retrieve())?.Id ?? AssetId.Empty;
                        CollectContentReference(id, gameContent, NodeIndex.Empty);
                    }
                }
                var objectNode = node as IObjectNode;
                if (objectNode != null && objectNode.Indices != null)
                {
                    if (AssetRegistry.IsContentType(objectNode.Descriptor.GetInnerCollectionType()))
                    {
                        foreach (var index in objectNode.Indices)
                        {
                            var id = AttachedReferenceManager.GetAttachedReference(objectNode.Retrieve(index))?.Id ?? AssetId.Empty;
                            CollectContentReference(id, gameContent, index);
                        }
                    }
                }
            }
            base.VisitNode(node);
        }

        protected virtual void CollectContentReference(AssetId contentId, IGraphNode content, NodeIndex index)
        {
            if (contentId == AssetId.Empty)
                return;

            ContentReferences.Add(new ContentReferenceAccessor(contentId, content, index));
        }
    }
}
