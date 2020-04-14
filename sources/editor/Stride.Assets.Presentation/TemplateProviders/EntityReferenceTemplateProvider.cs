// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Engine;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Presentation.TemplateProviders
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
