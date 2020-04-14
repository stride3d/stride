// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core.Annotations;
using Xenko.Core.Quantum;
using Xenko.Assets.Rendering;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
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
