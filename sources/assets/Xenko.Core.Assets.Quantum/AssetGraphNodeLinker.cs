// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Quantum
{
    public class AssetGraphNodeLinker : GraphNodeLinker
    {
        private readonly AssetPropertyGraphDefinition propertyGraphDefinition;

        public AssetGraphNodeLinker(AssetPropertyGraphDefinition propertyGraphDefinition)
        {
            this.propertyGraphDefinition = propertyGraphDefinition;
        }

        protected override bool ShouldVisitMemberTarget([NotNull] IMemberNode member)
        {
            return !propertyGraphDefinition.IsMemberTargetObjectReference(member, member.Retrieve()) && base.ShouldVisitMemberTarget(member);
        }

        protected override bool ShouldVisitTargetItem([NotNull] IObjectNode collectionNode, NodeIndex index)
        {
            return !propertyGraphDefinition.IsTargetItemObjectReference(collectionNode, index, collectionNode.Retrieve(index)) && base.ShouldVisitTargetItem(collectionNode, index);
        }
    }
}
