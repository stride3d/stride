// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
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
