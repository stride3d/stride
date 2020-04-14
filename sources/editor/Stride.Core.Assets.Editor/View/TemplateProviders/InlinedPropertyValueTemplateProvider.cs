// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class InlinedPropertyValueTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "InlinedPropertyValue";

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Name == "InlinedProperty";
        }
    }
}
