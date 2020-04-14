// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.View.TemplateProviders;
using Stride.Engine;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Rendering;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class ModelComponentMaterialTemplateProvider : ContentReferenceTemplateProvider
    {
        public ModelComponentMaterialTemplateProvider()
        {
            DynamicThumbnail = true;
        }

        public override string Name => "ModelComponentMaterial";

        public override bool MatchNode(NodeViewModel node)
        {
            return base.MatchNode(node)
                && node.NodeValue is Material
                && node.Parent?.Parent?.NodeValue is ModelComponent;
        }
    }
}
