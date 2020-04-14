// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    class ObjectTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "Object";

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Type != typeof(string) && (node.NodeValue == null || node.NodeValue.GetType().IsStruct() || node.NodeValue.GetType().IsClass);
        }
    }
}
