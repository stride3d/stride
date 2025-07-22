// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Entities;
using Stride.Engine;

namespace Stride.Assets.Presentation.Quantum;

[AssetPropertyGraphDefinition(typeof(EntityHierarchyAssetBase))]
public class EntityHierarchyPropertyGraphDefinition : AssetCompositeHierarchyPropertyGraphDefinition<EntityDesign, Entity>
{
    public override bool IsMemberTargetObjectReference(IMemberNode member, object value)
    {
        return value is EntityComponent || base.IsMemberTargetObjectReference(member, value);
    }

    public override bool IsTargetItemObjectReference(IObjectNode collection, NodeIndex itemIndex, object value)
    {
        if (value is EntityComponent)
        {
            // Check if we're in the component collection of an entity - other cases are references
            return collection.Type != typeof(EntityComponentCollection);
        }
        return base.IsTargetItemObjectReference(collection, itemIndex, value);
    }
}
