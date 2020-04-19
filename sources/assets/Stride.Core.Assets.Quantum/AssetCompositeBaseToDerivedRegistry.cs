// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Assets.Quantum
{
    internal class AssetCompositeBaseToDerivedRegistry : IBaseToDerivedRegistry
    {
        private readonly Dictionary<Guid, AssetBaseToDerivedRegistry> baseToInstances = new Dictionary<Guid, AssetBaseToDerivedRegistry>();
        private readonly AssetPropertyGraph propertyGraph;

        public AssetCompositeBaseToDerivedRegistry(AssetPropertyGraph propertyGraph)
        {
            this.propertyGraph = propertyGraph;
        }

        public void RegisterBaseToDerived(IAssetNode baseNode, IAssetNode derivedNode)
        {
            var ownerPart = derivedNode.GetContent(NodesToOwnerPartVisitor.OwnerPartContentName);
            var instanceId = (ownerPart?.Retrieve() as IAssetPartDesign)?.Base?.InstanceId ?? Guid.Empty;
            AssetBaseToDerivedRegistry derivedRegistry;
            if (!baseToInstances.TryGetValue(instanceId, out derivedRegistry))
                baseToInstances[instanceId] = derivedRegistry = new AssetBaseToDerivedRegistry(propertyGraph);

            derivedRegistry.RegisterBaseToDerived(baseNode, derivedNode);
        }

        public IIdentifiable ResolveFromBase(object baseObjectReference, IAssetNode derivedReferencerNode)
        {
            if (derivedReferencerNode == null) throw new ArgumentNullException(nameof(derivedReferencerNode));
            var ownerPart = derivedReferencerNode.GetContent(NodesToOwnerPartVisitor.OwnerPartContentName);
            var instanceId = (ownerPart?.Retrieve() as IAssetPartDesign)?.Base?.InstanceId ?? Guid.Empty;
            AssetBaseToDerivedRegistry derivedRegistry;
            if (!baseToInstances.TryGetValue(instanceId, out derivedRegistry))
                return null;

            return derivedRegistry.ResolveFromBase(baseObjectReference, derivedReferencerNode);
        }
    }
}
