// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Annotations;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Quantum;
using Stride.Assets.Rendering;
using Stride.Rendering;

namespace Stride.Assets.Presentation.AssetEditors.GraphicsCompositorEditor.ViewModels
{
    /// <summary>
    /// View model for <see cref="RenderStage"/>.
    /// </summary>
    public class RenderStageViewModel : GraphicsCompositorItemViewModel
    {
        private readonly IObjectNode renderStageNode;
        private readonly MemberGraphNodeBinding<string> nameNodeBinding;
        private readonly MemberGraphNodeBinding<string> effectSlotNodeBinding;

        public RenderStageViewModel([NotNull] GraphicsCompositorEditorViewModel editor, RenderStage renderStage) : base(editor)
        {
            RenderStage = renderStage;
            renderStageNode = editor.Session.AssetNodeContainer.GetOrCreateNode(renderStage);

            nameNodeBinding = new MemberGraphNodeBinding<string>(renderStageNode[nameof(RenderStage.Name)], nameof(Name), OnPropertyChanging, OnPropertyChanged, editor.UndoRedoService);
            effectSlotNodeBinding = new MemberGraphNodeBinding<string>(renderStageNode[nameof(RenderStage.EffectSlotName)], nameof(EffectSlotName), OnPropertyChanging, OnPropertyChanged, editor.UndoRedoService);
        }

        public RenderStage RenderStage { get; }

        public string Name { get { return nameNodeBinding.Value; } set { nameNodeBinding.Value = value; } }

        public string EffectSlotName { get { return effectSlotNodeBinding.Value; } set { effectSlotNodeBinding.Value = value; } }

        /// <inheritdoc/>
        public override IObjectNode GetRootNode() => renderStageNode;

        /// <inheritdoc />
        protected override GraphNodePath GetNodePath()
        {
            var path = new GraphNodePath(Editor.Session.AssetNodeContainer.GetNode(Editor.Asset.Asset));
            path.PushMember(nameof(GraphicsCompositorAsset.RenderStages));
            path.PushTarget();
            path.PushIndex(new NodeIndex(Editor.Asset.Asset.RenderStages.IndexOf(RenderStage)));
            return path;
        }
    }
}
