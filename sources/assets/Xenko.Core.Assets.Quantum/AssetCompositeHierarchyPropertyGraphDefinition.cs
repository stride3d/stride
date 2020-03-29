using System;
using Xenko.Core;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    [AssetPropertyGraphDefinition(typeof(AssetCompositeHierarchy<,>))]
    public class AssetCompositeHierarchyPropertyGraphDefinition<TAssetPartDesign, TAssetPart> : AssetPropertyGraphDefinition
        where TAssetPart : class, IIdentifiable
        where TAssetPartDesign : class, IAssetPartDesign<TAssetPart>
    {
        public override bool IsMemberTargetObjectReference(IMemberNode member, object value)
        {
            if (value is TAssetPart)
            {
                // Check if we're the part referenced by a part design - other cases are references
                return member.Parent.Type != typeof(TAssetPartDesign);
            }
            return base.IsMemberTargetObjectReference(member, value);
        }

        public override bool IsTargetItemObjectReference(IObjectNode collection, NodeIndex itemIndex, object value)
        {
            return value is TAssetPart || base.IsTargetItemObjectReference(collection, itemIndex, value);
        }
    }
}