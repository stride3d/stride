// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;
using Xenko.Assets.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    public class SharedRendererBlockViewModel : SharedRendererBlockBaseViewModel
    {
        private readonly Dictionary<SharedRendererReferenceKey, IGraphicsCompositorSlotViewModel> outputSlotMap = new Dictionary<SharedRendererReferenceKey, IGraphicsCompositorSlotViewModel>();
        private readonly IObjectNode sharedRendererNode;
        private readonly ISharedRenderer sharedRenderer;

        public SharedRendererBlockViewModel([NotNull] GraphicsCompositorEditorViewModel editor, ISharedRenderer sharedRenderer) : base(editor)
        {
            this.sharedRenderer = sharedRenderer;
            sharedRendererNode = editor.Session.AssetNodeContainer.GetOrCreateNode(sharedRenderer);
            InputSlots.Add(new SharedRendererInputSlotViewModel(this));
        }

        /// <inheritdoc/>
        public override string Title => DisplayAttribute.GetDisplayName(sharedRenderer?.GetType());

        public ISharedRenderer GetSharedRenderer() => sharedRenderer;

        /// <inheritdoc/>
        protected override IEnumerable<IGraphNode> GetNodesContainingReferences()
        {
            yield return sharedRendererNode;
        }

        /// <inheritdoc/>
        public override IObjectNode GetRootNode() => sharedRendererNode;

        /// <inheritdoc />
        protected override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
            path.PushMember(nameof(GraphicsCompositorAsset.SharedRenderers));
            path.PushTarget();
            path.PushIndex(new NodeIndex(Editor.Asset.Asset.SharedRenderers.IndexOf(sharedRenderer)));
            return path;
        }
    }
}
