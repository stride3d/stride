// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Core.Diagnostics;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    [AssetPropertyGraph(typeof(AssetComposite))]
    public class AssetCompositePropertyGraph : AssetPropertyGraph
    {
        public AssetCompositePropertyGraph([NotNull] AssetPropertyGraphContainer container, [NotNull] AssetItem assetItem, ILogger logger)
            : base(container, assetItem, logger)
        {
        }

        protected void LinkToOwnerPart([NotNull] IGraphNode node, object part)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var visitor = new NodesToOwnerPartVisitor(Definition, Container.NodeContainer, part);
            visitor.Visit(node);
        }

        protected sealed override IBaseToDerivedRegistry CreateBaseToDerivedRegistry()
        {
            return new AssetCompositeBaseToDerivedRegistry(this);
        }
    }
}
