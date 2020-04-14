// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class ContentReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public bool DynamicThumbnail { get; set; }

        public override string Name => (DynamicThumbnail ? "Thumbnail" : "Simple") + "reference";

        public override bool MatchNode(NodeViewModel node)
        {
            var isReference = typeof(AssetReference).IsAssignableFrom(node.Type);

            if (!isReference)
            {
                isReference = AssetRegistry.IsContentType(node.Type);
            }

            object hasDynamic;
            node.AssociatedData.TryGetValue("DynamicThumbnail", out hasDynamic);
            return isReference && (bool)(hasDynamic ?? false) == DynamicThumbnail;
        }
    }
}
