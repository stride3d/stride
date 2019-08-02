// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Assets.Editor.Components.FixReferences;
using Xenko.Core.Assets.Editor.Services;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Quantum;

namespace Xenko.Core.Assets.Editor.Components.FixAssetReferences
{
    public class AssetReferenceReplacementViewModel : ReferenceReplacementViewModel<AssetViewModel>
    {
        private readonly IGraphNode node;
        private readonly NodeIndex index;

        public AssetReferenceReplacementViewModel(FixAssetReferencesViewModel fixReferences, AssetViewModel objectToFix, AssetViewModel referencer, object referencedMember, IGraphNode node, NodeIndex index)
            : base(fixReferences, objectToFix, referencer, referencedMember)
        {
            this.node = node;
            this.index = index;
        }

        public override void ClearReference()
        {
            if (node is IMemberNode)
                ((IMemberNode)node).Update(null);
            else
                ((IObjectNode)node).Update(null, index);
        }

        protected override void ReplaceReference()
        {
            var referenceType = node.Descriptor.GetInnerCollectionType();
            var newReference = ContentReferenceHelper.CreateReference(ReplacementObject, referenceType);
            if (node is IMemberNode)
                ((IMemberNode)node).Update(newReference);
            else
                ((IObjectNode)node).Update(newReference, index);
        }
    }
}
