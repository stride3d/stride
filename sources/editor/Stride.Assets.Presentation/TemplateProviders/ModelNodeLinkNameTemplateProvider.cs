// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Presentation.NodePresenters.Keys;
using Stride.Engine;

namespace Stride.Assets.Presentation.TemplateProviders
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
