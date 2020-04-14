// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Quantum;
using Xenko.Assets.Rendering;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// View model for <see cref="SceneCameraSlot"/>.
    /// </summary>
    public class GraphicsCompositorCameraSlotsViewModel : GraphicsCompositorItemViewModel
    {
        private readonly IObjectNode cameraSlotNode;

        private readonly MemberGraphNodeBinding<string> nameNodeBinding;

        public GraphicsCompositorCameraSlotsViewModel([NotNull] GraphicsCompositorEditorViewModel editor, SceneCameraSlot cameraSlot) : base(editor)
        {
            CameraSlot = cameraSlot;
            cameraSlotNode = editor.Session.AssetNodeContainer.GetOrCreateNode(cameraSlot);

            nameNodeBinding = new MemberGraphNodeBinding<string>(cameraSlotNode[nameof(CameraSlot.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, editor.UndoRedoService);
        }

        public void Dispose()
        {
            nameNodeBinding?.Dispose();
        }

        public SceneCameraSlot CameraSlot { get; }

        public string Title => CameraSlot?.GetType().Name;

        public string Name { get { return nameNodeBinding.Value; } set { nameNodeBinding.Value = value; } }

        /// <inheritdoc/>
        public override IObjectNode GetRootNode() => cameraSlotNode;

        /// <inheritdoc />
        protected override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
            path.PushMember(nameof(GraphicsCompositorAsset.Cameras));
            path.PushTarget();
            path.PushIndex(new NodeIndex(Editor.Asset.Asset.Cameras.IndexOf(CameraSlot)));
            return path;
        }
    }
}
