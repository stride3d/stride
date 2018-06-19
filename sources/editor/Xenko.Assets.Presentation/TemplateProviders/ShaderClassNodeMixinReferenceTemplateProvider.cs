// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Rendering.Materials.ComputeColors;

namespace Xenko.Assets.Presentation.TemplateProviders
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
