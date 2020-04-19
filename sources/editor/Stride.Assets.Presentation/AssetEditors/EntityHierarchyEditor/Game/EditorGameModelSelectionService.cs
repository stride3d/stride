// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Extensions;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameModelSelectionService : EditorGameServiceBase
    {
        private readonly EntityHierarchyEditorViewModel editor;
        private IEditorGameEntitySelectionService selectionService;
        private readonly HashSet<Entity> selectedEntities = new HashSet<Entity>();

        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameEntitySelectionService); } }

        public EditorGameModelSelectionService(EntityHierarchyEditorViewModel editor)
        {
            this.editor = editor;
        }

        /// <inheritdoc />
        public override Task DisposeAsync()
        {
            EnsureNotDestroyed(nameof(EditorGameModelSelectionService));

            if (selectionService != null)
                selectionService.SelectionUpdated -= SelectionUpdated;

            return base.DisposeAsync();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorGameModelSelectionService"/> class.
        /// </summary>
        /// <param name="editorGame">The <see cref="EntityHierarchyEditorViewModel"/> related to the current instance of the scene editor.</param>
        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            selectionService = editorGame.EditorServices.Get<IEditorGameEntitySelectionService>();
            selectionService.SelectionUpdated += SelectionUpdated;

            return Task.FromResult(true);
        }

        public override void UpdateGraphicsCompositor(EditorServiceGame game)
        {
            base.UpdateGraphicsCompositor(game);

            var wireframeRenderStage = new RenderStage("SelectionGizmo", "Wireframe");
            game.SceneSystem.GraphicsCompositor.RenderStages.Add(wireframeRenderStage);

            wireframeRenderStage.Filter = new WireframeFilter(selectedEntities);

            // Meshes
            var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            // TODO: Complain (log) if there is no MeshRenderFeature
            if (meshRenderFeature != null)
            {
                meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
                {
                    EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Wireframe",
                    RenderStage = wireframeRenderStage,
                    RenderGroup = RenderGroupMask.All
                });
                var wireframeRenderFeature = new WireframeRenderFeature();
                meshRenderFeature.RenderFeatures.Add(wireframeRenderFeature);
                wireframeRenderFeature.RegisterSelectionService(selectionService);

                // Enable editor shaders for meshes (will use StrideEditorForwardShadingEffect instead of StrideForwardShadingEffect)
				// TODO: Avoid hardcoding those shader names (maybe by letting the user mix himself editor mixin in his own effect?)
                meshRenderFeature.RenderStageSelectors.Add(new MeshEditorRenderStageSelector());
            }

            var editorCompositor = (EditorTopLevelCompositor)game.SceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = wireframeRenderStage, Name = "Render selection gizmo" });
        }

        private void SelectionUpdated(object sender, [NotNull] EntitySelectionEventArgs e)
        {
            var recursiveSelection = new HashSet<Entity>(e.NewSelection);
            foreach (var childEntity in e.NewSelection.SelectDeep(x => x.Transform.Children.Select(y => y.Entity)))
            {
                recursiveSelection.Add(childEntity);
            }

            editor.Controller.InvokeAsync(() =>
            {
                // update the selection on the gizmo entities.
                selectedEntities.Clear();
                selectedEntities.AddRange(recursiveSelection);
            });
        }

        class WireframeFilter : RenderStageFilter
        {
            private readonly HashSet<Entity> selectedEntities;

            public WireframeFilter(HashSet<Entity> selectedEntities)
            {
                this.selectedEntities = selectedEntities;
            }

            public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
            {
                var entity = ((renderObject as RenderMesh)?.Source as ModelComponent)?.Entity;
                return entity != null && selectedEntities.Contains(entity);
            }
        }

        /// <summary>
        /// Replaces all StrideForwardShadingEffect into StrideEditorForwardShadingEffect
        /// </summary>
        private class MeshEditorRenderStageSelector : RenderStageSelector
        {
            public override void Process(RenderObject renderObject)
            {
                for (int index = 0; index < renderObject.ActiveRenderStages.Length; index++)
                {
                    if (renderObject.ActiveRenderStages[index].Active)
                    {
                        var effectName = renderObject.ActiveRenderStages[index].EffectSelector.EffectName;
                        if (effectName == "StrideForwardShadingEffect"
                            || effectName.StartsWith("StrideForwardShadingEffect."))
                        {
                            effectName = effectName.Replace("StrideForwardShadingEffect", "StrideEditorForwardShadingEffect");
                            renderObject.ActiveRenderStages[index].EffectSelector = new EffectSelector(effectName);
                        }
                    }
                }
            }
        }
    }
}
