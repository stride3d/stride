// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Quantum;

namespace Stride.Core.Assets.Quantum;

public class AssetGraphNodeLinker : GraphNodeLinker
{
    private readonly AssetPropertyGraphDefinition propertyGraphDefinition;

    public AssetGraphNodeLinker(AssetPropertyGraphDefinition propertyGraphDefinition)
    {
        this.propertyGraphDefinition = propertyGraphDefinition;
    }

    protected override bool ShouldVisitMemberTarget(IMemberNode member)
    {
        return !propertyGraphDefinition.IsMemberTargetObjectReference(member, member.Retrieve()) && base.ShouldVisitMemberTarget(member);
    }

    protected override bool ShouldVisitTargetItem(IObjectNode collectionNode, NodeIndex index)
    {
        return !propertyGraphDefinition.IsTargetItemObjectReference(collectionNode, index, collectionNode.Retrieve(index)) && base.ShouldVisitTargetItem(collectionNode, index);
    }
}
