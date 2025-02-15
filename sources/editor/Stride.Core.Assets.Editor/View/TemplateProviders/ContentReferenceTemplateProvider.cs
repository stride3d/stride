// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class ContentReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public bool DynamicThumbnail { get; set; }

        public override string Name => (DynamicThumbnail ? "Thumbnail" : "Simple") + "reference";

        public override bool MatchNode(NodeViewModel node)
        {
            if (AssetRegistry.CanBeAssignedToContentTypes(node.Type, checkIsUrlType: true))
            {
                object hasDynamic;
                node.AssociatedData.TryGetValue("DynamicThumbnail", out hasDynamic);
                return (bool)(hasDynamic ?? false) == DynamicThumbnail;
            }

            return false;
        }
    }
}
