// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Annotations;
using Stride.Core.Diagnostics;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum
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
