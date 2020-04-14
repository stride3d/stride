// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Assets.Rendering;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.TemplateProviders
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
