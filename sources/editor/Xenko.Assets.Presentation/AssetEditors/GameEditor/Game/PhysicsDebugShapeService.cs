// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Xenko.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using System.Threading.Tasks;
using Xenko.Assets.Presentation.AssetEditors.Gizmos;
using Xenko.Editor.EditorGame.Game;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Assets.Presentation.AssetEditors.GameEditor.Game
{
    public class PhysicsDebugShapeService : EditorGameServiceBase
    {
        private EntityHierarchyEditorGame game;

        public override bool IsActive { get; set; } = true;

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (editorGame == null) throw new ArgumentNullException(nameof(editorGame));
            game = (EntityHierarchyEditorGame)editorGame;

            // Create render stage
            var physicsDebugShapeRenderStage = new RenderStage("PhysicsDebugShape", "Main");
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(physicsDebugShapeRenderStage);

            // Setup stage selector
            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = GizmoBase.PhysicsShapesGroupMask,
                RenderStage = physicsDebugShapeRenderStage,
            });

            // Apply wireframe
            meshRenderFeature.PipelineProcessors.Add(new WireframePipelineProcessor { RenderStage = physicsDebugShapeRenderStage });

            // Setup renderer
            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer
            {
                Name = "Render Physics Gizmo",
                RenderStage = physicsDebugShapeRenderStage,
            });

            return Task.FromResult(true);
        }
    }
}
