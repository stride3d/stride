// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Extensions;
using Stride.Core.Quantum;
using Stride.Core.Serialization;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Collects the references to content (other assets) of an asset-side part whose nodes are linked to the game side:
/// each one with the game-side node the loaded content goes into.
/// </summary>
internal sealed class ContentReferenceCollector : AssetGraphVisitorBase
{
    /// <summary>
    /// A reference to content: the referenced asset, and the game-side node and index that receive the loaded content.
    /// </summary>
    public readonly record struct ContentReferenceAccessor(AssetId ContentId, IGraphNode Node, NodeIndex Index);

    public ContentReferenceCollector(AssetPropertyGraphDefinition propertyGraphDefinition)
        : base(propertyGraphDefinition)
    {
    }

    /// <summary>
    /// Gets the references collected so far.
    /// </summary>
    public List<ContentReferenceAccessor> ContentReferences { get; } = [];

    /// <inheritdoc />
    protected override void VisitNode(IGraphNode node)
    {
        var assetNode = (IAssetNode)node;
        var gameContent = assetNode.GetContent(GameEditorChangePropagator.GameSideContentKey);
        if (gameContent != null)
        {
            if (node is IMemberNode memberContent)
            {
                if (AssetRegistry.CanBeAssignedToContentTypes(memberContent.Type, checkIsUrlType: false))
                {
                    var id = AttachedReferenceManager.GetAttachedReference(memberContent.Retrieve())?.Id ?? AssetId.Empty;
                    CollectContentReference(id, gameContent, NodeIndex.Empty);
                }
            }

            if (node is IObjectNode objectNode && objectNode.Indices != null)
            {
                if (AssetRegistry.CanBeAssignedToContentTypes(objectNode.Descriptor.GetInnerCollectionType(), checkIsUrlType: false))
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

    private void CollectContentReference(AssetId contentId, IGraphNode content, NodeIndex index)
    {
        if (contentId == AssetId.Empty)
            return;

        ContentReferences.Add(new ContentReferenceAccessor(contentId, content, index));
    }
}
