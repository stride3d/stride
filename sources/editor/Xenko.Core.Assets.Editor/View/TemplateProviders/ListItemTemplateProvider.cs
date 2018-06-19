// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class ListItemTemplateProvider : TypeMatchTemplateProvider
    {
        public override string Name => "ListItem";

        public override bool MatchNode(NodeViewModel node)
        {
            return base.MatchNode(node) && node.Parent != null && (node.Parent.HasCollection || node.Parent.HasDictionary);
        }
    }
}
