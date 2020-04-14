// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Engine;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class EntityComponentCollectionTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "EntityComponentCollection";

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Type == typeof(EntityComponentCollection) && node.Parent?.Type == typeof(Entity);
        }
    }
}
