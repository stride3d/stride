// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Components.FixReferences;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Quantum;

namespace Stride.Core.Assets.Editor.Components.FixAssetReferences
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
