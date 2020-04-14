// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Annotations;
using Stride.Core.Quantum;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// Represent a single property of an <see cref="IObjectNode"/>.
    /// </summary>
    public class EntryPointBlockViewModel : SharedRendererBlockBaseViewModel
    {
        private readonly IObjectNode graphicsCompositorNode;
        private readonly string[] entryPoints;

        public EntryPointBlockViewModel([NotNull] GraphicsCompositorEditorViewModel editor, IObjectNode graphicsCompositorNode, string[] entryPoints)
            : base(editor)
        {
            this.graphicsCompositorNode = graphicsCompositorNode;
            this.entryPoints = entryPoints;
        }

        /// <inheritdoc/>
        public override string Title => "Entry points";

        /// <inheritdoc/>
        public override IObjectNode GetRootNode() => graphicsCompositorNode;

        /// <inheritdoc />
        protected override GraphNodePath GetNodePath()
        {
            return new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
        }

        /// <inheritdoc/>
        protected override IEnumerable<IGraphNode> GetNodesContainingReferences()
        {
            foreach (var entryPoint in entryPoints)
                yield return graphicsCompositorNode[entryPoint];
        }

        public override bool ShouldConstructMember(IMemberNode member)
        {
            if (!base.ShouldConstructMember(member))
                return false;

            // Only allow entry point at the top level
            if (member.Parent == graphicsCompositorNode && !entryPoints.Contains(member.MemberDescriptor.Name))
                return false;

            return true;
        }
    }
}
