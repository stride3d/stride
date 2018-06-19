// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
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
