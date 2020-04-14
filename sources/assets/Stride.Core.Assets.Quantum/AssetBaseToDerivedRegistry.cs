// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum
{
    internal class AssetBaseToDerivedRegistry : IBaseToDerivedRegistry
    {
        private readonly AssetPropertyGraph propertyGraph;
        private readonly Dictionary<IAssetNode, IAssetNode> baseToDerived = new Dictionary<IAssetNode, IAssetNode>();

        public AssetBaseToDerivedRegistry(AssetPropertyGraph propertyGraph)
        {
            this.propertyGraph = propertyGraph;
        }

        public void RegisterBaseToDerived(IAssetNode baseNode, IAssetNode derivedNode)
        {
            var baseValue = baseNode?.Retrieve();
            if (baseValue == null)
                return;

            if (baseValue is IIdentifiable)
            {
                baseToDerived[baseNode] = derivedNode;
                var baseMemberNode = baseNode as IAssetMemberNode;
                if (baseMemberNode?.Target != null && !propertyGraph.Definition.IsMemberTargetObjectReference(baseMemberNode, baseValue))
                {
                    baseToDerived[baseMemberNode.Target] = ((IAssetMemberNode)derivedNode).Target;
                }
            }

            var derivedObjectNode = derivedNode as IObjectNode;
            var baseObjectNode = baseNode as IObjectNode;
            if (derivedObjectNode?.ItemReferences != null && baseObjectNode?.ItemReferences != null)
            {
                foreach (var reference in derivedObjectNode.ItemReferences)
                {
                    var target = propertyGraph.baseLinker.FindTargetReference(derivedNode, baseNode, reference);
                    if (target == null)
                        continue;

                    baseValue = target.TargetNode?.Retrieve();
                    if (!propertyGraph.Definition.IsTargetItemObjectReference(baseObjectNode, target.Index, baseNode.Retrieve(target.Index)))
                    {
                        if (baseValue is IIdentifiable)
                        {
                            baseToDerived[(IAssetNode)target.TargetNode] = (IAssetNode)derivedObjectNode.IndexedTarget(reference.Index);
                        }
                    }
                }
            }
        }

        public IIdentifiable ResolveFromBase(object baseObjectReference, IAssetNode derivedReferencerNode)
        {
            if (derivedReferencerNode == null) throw new ArgumentNullException(nameof(derivedReferencerNode));
            if (baseObjectReference == null)
                return null;

            var baseNode = (IAssetNode)propertyGraph.Container.NodeContainer.GetNode(baseObjectReference);
            IAssetNode derivedNode;
            baseToDerived.TryGetValue(baseNode, out derivedNode);
            return derivedNode?.Retrieve() as IIdentifiable;
        }
    }
}
