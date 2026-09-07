// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using System.Threading.Tasks;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.EditorGame.Game;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;

namespace Stride.Assets.Presentation.AssetEditors.GameEditor.Game
{
    [EditorGameService(typeof(EntityHierarchyEditorController), Order = 140)]
    public class WireframeGizmoStageService : EditorGameServiceBase
    {
        private EntityHierarchyEditorGame game;

        public override bool IsActive { get; set; } = true;

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (editorGame == null) throw new ArgumentNullException(nameof(editorGame));
            game = (EntityHierarchyEditorGame)editorGame;

            // Create render stage
            var wireframeRenderStage = new RenderStage("WireframeGizmo", "Main");
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(wireframeRenderStage);

            // Setup stage selector
            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = GizmoBase.PhysicsShapesGroupMask,
                RenderStage = wireframeRenderStage,
            });

            // Apply wireframe
            meshRenderFeature.PipelineProcessors.Add(new WireframePipelineProcessor { RenderStage = wireframeRenderStage });

            // Setup renderer
            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer
            {
                Name = "Render wireframe gizmos",
                RenderStage = wireframeRenderStage,
            });

            return Task.FromResult(true);
        }
    }
}
