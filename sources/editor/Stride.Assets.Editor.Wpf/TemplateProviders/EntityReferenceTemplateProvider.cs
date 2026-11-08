// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Engine;

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
