// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Yaml;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class UnloadableObjectTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "UnloadableObject";

        public override bool MatchNode(NodeViewModel node)
        {
            return node.Name == DisplayData.UnloadableObjectInfo && node.NodeValue is IUnloadable;
        }
    }
}
