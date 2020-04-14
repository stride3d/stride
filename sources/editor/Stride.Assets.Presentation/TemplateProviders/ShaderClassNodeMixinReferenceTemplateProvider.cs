// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class ShaderClassNodeMixinReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => $"ShaderClassNodeMixinReference";

        public override bool MatchNode(NodeViewModel node)
        {
            if (node.Parent == null)
                return false;

            return (node.Parent.NodeValue is ComputeShaderClassColor ||
                    node.Parent.NodeValue is ComputeShaderClassScalar) && node.Type == typeof(string);
        }
    }
}
