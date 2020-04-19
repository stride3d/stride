// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Quantum.Visitors;
using Stride.Core.Annotations;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum
{
    /// <summary>
    /// A node visitor that will link nodes of a part of an <see cref="AssetComposite"/> to the root node of the part itself.
    /// </summary>
    public class NodesToOwnerPartVisitor : AssetGraphVisitorBase
    {
        /// <summary>
        /// The identifier of the link in each node.
        /// </summary>
        public const string OwnerPartContentName = "OwnerPart";

        private readonly IAssetObjectNode ownerPartNode;

        public NodesToOwnerPartVisitor([NotNull] AssetPropertyGraphDefinition propertyGraphDefinition, [NotNull] INodeContainer nodeContainer, object ownerPart)
            : base(propertyGraphDefinition)
        {
            ownerPartNode = (IAssetObjectNode)nodeContainer.GetOrCreateNode(ownerPart);
        }

        protected override void VisitNode(IGraphNode node)
        {
            var assetNode = node as IAssetNode;
            assetNode?.SetContent(OwnerPartContentName, ownerPartNode);

            base.VisitNode(node);
        }
    }
}
