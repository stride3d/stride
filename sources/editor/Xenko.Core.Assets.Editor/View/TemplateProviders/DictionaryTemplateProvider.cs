// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class DictionaryTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "Dictionary";

        public override bool MatchNode(NodeViewModel node)
        {
            return CheckNode(node);
        }

        public static bool CheckNode(NodeViewModel node)
        {
            return node.HasDictionary && node.NodeValue != null;
        }
    }
}
