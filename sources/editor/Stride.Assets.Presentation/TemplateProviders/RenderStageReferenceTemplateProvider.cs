// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Rendering;
using Stride.Rendering;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class RenderStageReferenceTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => nameof(RenderStageReferenceTemplateProvider);

        public override bool MatchNode(NodeViewModel node)
        {
            // Everything that is not in GraphicsCompositorAsset.RenderStages should become a reference
            return node.Type == typeof(RenderStage)
                && !(node.Parent?.MemberInfo != null && node.Parent.MemberInfo.DeclaringType == typeof(GraphicsCompositorAsset) && node.Parent.MemberInfo.Name == nameof(GraphicsCompositorAsset.RenderStages));
        }
    }
}
