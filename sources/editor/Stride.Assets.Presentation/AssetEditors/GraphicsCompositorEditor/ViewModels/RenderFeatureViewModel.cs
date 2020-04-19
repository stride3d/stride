// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Annotations;
using Stride.Core.Quantum;
using Stride.Assets.Rendering;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// View model for <see cref="RenderFeature"/>.
    /// </summary>
    public class RenderFeatureViewModel : GraphicsCompositorItemViewModel
    {
        private readonly IObjectNode renderFeatureNode;

        public RenderFeatureViewModel([NotNull] GraphicsCompositorEditorViewModel editor, RenderFeature renderFeature) : base(editor)
        {
            RenderFeature = renderFeature;
            renderFeatureNode = editor.Session.AssetNodeContainer.GetOrCreateNode(renderFeature);
        }

        public RenderFeature RenderFeature { get; }

        public string Title => RenderFeature?.GetType().Name;

        /// <inheritdoc/>
        public override IObjectNode GetRootNode() => renderFeatureNode;

        /// <inheritdoc />
        protected override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
            path.PushMember(nameof(GraphicsCompositorAsset.RenderFeatures));
            path.PushTarget();
            path.PushIndex(new NodeIndex(Editor.Asset.Asset.RenderFeatures.IndexOf((RootRenderFeature)RenderFeature)));
            return path;
        }
    }
}
