// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Engine;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Assets.Presentation.TemplateProviders
{
    public class EntityReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "EntityReference";

        public override bool MatchNode(NodeViewModel node)
        {
            return typeof(Entity).IsAssignableFrom(node.Type);
        }
    }
}
