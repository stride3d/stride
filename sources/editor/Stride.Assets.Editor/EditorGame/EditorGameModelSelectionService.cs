// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core.Extensions;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Draws the wireframe of the models of the selected entities and of their children, which fades out after the selection
/// changes, or stays while the selection mask shows.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameModelSelectionService</c>.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 220)]
public sealed class EditorGameModelSelectionService : EditorGameServiceBase
{
    // Read by the render thread, which is the game thread
    private readonly HashSet<Entity> selectedEntities = [];
    private IEditorGameEntitySelectionService? selectionService;

    /// <inheritdoc />
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameEntitySelectionService); } }

    /// <inheritdoc />
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameModelSelectionService));

        if (selectionService != null)
            selectionService.SelectionUpdated -= SelectionUpdated;

        return base.DisposeAsync();
    }

    /// <inheritdoc />
    public override void UpdateGraphicsCompositor(EditorServiceGame game)
    {
        base.UpdateGraphicsCompositor(game);

        var wireframeRenderStage = new RenderStage("SelectionGizmo", "Wireframe") { Filter = new WireframeFilter(selectedEntities) };
        game.SceneSystem.GraphicsCompositor.RenderStages.Add(wireframeRenderStage);

        // Meshes
        if (game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault() is { } meshRenderFeature)
        {
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Wireframe",
                RenderStage = wireframeRenderStage,
                RenderGroup = RenderGroupMask.All,
            });
            var wireframeRenderFeature = new WireframeRenderFeature();
            meshRenderFeature.RenderFeatures.Add(wireframeRenderFeature);
            wireframeRenderFeature.RegisterSelectionService(game.EditorServices.Get<IEditorGameEntitySelectionService>());

            // The meshes of the compositor of the game use the editor shaders (StrideEditorForwardShadingEffect instead of StrideForwardShadingEffect)
            meshRenderFeature.RenderStageSelectors.Add(new MeshEditorRenderStageSelector());
        }

        var editorCompositor = (EditorTopLevelCompositor)game.SceneSystem.GraphicsCompositor.Game;
        editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = wireframeRenderStage, Name = "Render selection gizmo" });
    }

    /// <inheritdoc />
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        selectionService = editorGame.EditorServices.Get<IEditorGameEntitySelectionService>();
        selectionService.SelectionUpdated += SelectionUpdated;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Follows the selection (game thread).
    /// </summary>
    private void SelectionUpdated(object? sender, EntitySelectionEventArgs e)
    {
        selectedEntities.Clear();
        selectedEntities.AddRange(e.NewSelection.BreadthFirst(x => x.Transform.Children.Select(y => y.Entity)));
    }

    private sealed class WireframeFilter(HashSet<Entity> selectedEntities) : RenderStageFilter
    {
        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            return ((renderObject as RenderMesh)?.Source as ModelComponent)?.Entity is { } entity && selectedEntities.Contains(entity);
        }
    }

    /// <summary>
    /// Replaces StrideForwardShadingEffect with StrideEditorForwardShadingEffect.
    /// </summary>
    private sealed class MeshEditorRenderStageSelector : RenderStageSelector
    {
        public override void Process(RenderObject renderObject)
        {
            for (var index = 0; index < renderObject.ActiveRenderStages.Length; index++)
            {
                if (!renderObject.ActiveRenderStages[index].Active)
                    continue;

                var effectName = renderObject.ActiveRenderStages[index].EffectSelector.EffectName;
                if (effectName == "StrideForwardShadingEffect" || effectName.StartsWith("StrideForwardShadingEffect.", StringComparison.Ordinal))
                {
                    effectName = effectName.Replace("StrideForwardShadingEffect", "StrideEditorForwardShadingEffect");
                    renderObject.ActiveRenderStages[index].EffectSelector = new EffectSelector(effectName);
                }
            }
        }
    }
}
