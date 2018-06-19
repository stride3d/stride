// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Assets.Presentation.NodePresenters.Keys;
using Xenko.Engine;

namespace Xenko.Assets.Presentation.TemplateProviders
{
    public class ModelNodeNameTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => nameof(ModelNodeNameTemplateProvider);

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Name == nameof(ModelNodeLinkComponent.NodeName) && (node.Parent?.AssociatedData.ContainsKey(ModelNodeLinkData.AvailableNodes) ?? false);
        }
    }
}
