// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Reflection;
using Stride.Core.Quantum;
using Stride.Core.Quantum.References;

namespace Stride.Core.Assets.Quantum.Visitors
{
    /// <summary>
    /// A visitor that collects all assets from an object containing multiple assets
    /// </summary>
    public class AssetCollector : GraphVisitorBase
    {
        private readonly Dictionary<GraphNodePath, Asset> assets = new Dictionary<GraphNodePath, Asset>();

        private AssetCollector()
        {
            
        }

        /// <summary>
        /// Collects all assets referenced by the object in the given node.
        /// </summary>
        /// <param name="root">The root node to visit to collect assets.</param>
        /// <returns>A collection containing all assets found by visiting the given root.</returns>
        public static IReadOnlyDictionary<GraphNodePath, Asset> Collect([NotNull] IObjectNode root)
        {
            var visitor = new AssetCollector();
            visitor.Visit(root);
            return visitor.assets;
        }

        /// <inheritdoc/>
        protected override void VisitReference(IGraphNode referencer, ObjectReference reference)
        {
            var asset = reference.TargetNode.Retrieve() as Asset;
            if (asset != null)
            {
                assets.Add(CurrentPath.Clone(), asset);
            }
            // Don't continue the visit once we found an asset, we cannot have nested assets.
            else
            {
                base.VisitReference(referencer, reference);
            }
        }
    }
}
