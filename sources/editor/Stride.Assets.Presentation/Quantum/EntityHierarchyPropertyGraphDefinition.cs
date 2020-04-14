using Xenko.Core.Assets.Quantum;
using Xenko.Core.Quantum;
using Xenko.Assets.Entities;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.Quantum
{
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
}