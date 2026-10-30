// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Editor.EditorGame.Game;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Draws the meshes of the wireframe gizmo group (<see cref="GizmoBase.PhysicsShapesGroup"/>) in wireframe, after the scene:
/// the collider shapes of the Physics package, the shapes of the particle systems and the light shaft volumes.
/// </summary>
/// <remarks>
/// The cross-platform copy of the WPF <c>WireframeGizmoStageService</c>: without it these gizmos are created, but never drawn.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 140)]
public sealed class WireframeGizmoStageService : EditorGameServiceBase
{
    /// <inheritdoc/>
    public override bool IsActive { get; set; } = true;

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        ArgumentNullException.ThrowIfNull(editorGame);
        var game = (EntityHierarchyEditorGame)editorGame;

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
