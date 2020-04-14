// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Assets.Editor.Services;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.AssetHighlighters;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    public class EditorGameMaterialHighlightService : EditorGameMouseServiceBase, IEditorGameMaterialHighlightService, IEditorGameMaterialHighlightViewModelService
    {
        private readonly EntityHierarchyEditorViewModel editor;
        private readonly object lockObject = new object();

        /// <summary>
        /// The list of model components that are currently highlighted while the scene editor is in material selection mode.
        /// </summary>
        private readonly HashSet<ModelComponent> highlightedModelComponents = new HashSet<ModelComponent>();
        /// <summary>
        /// The list of materials that are currently highlighted while the scene editor is in material selection mode.
        /// </summary>
        private readonly HashSet<Material> highlightedMaterials = new HashSet<Material>();

        private EditorServiceGame game;

        public EditorGameMaterialHighlightService(EntityHierarchyEditorViewModel editor)
        {
            this.editor = editor;
        }

        /// <summary>
        /// The last entity which has one if its material highlighted (null when nothing is highlighted) 
        /// </summary>/// 
        public Entity HighlightedEntity { get; private set; }

        /// <summary>
        /// The index of the material in the last entity which has one if its material highlighted (-1 when nothing is highlighted) 
        /// </summary>
        public int HighlightedMaterialIndex { get; private set; } = -1;

        /// <summary>
        /// The index of the mesh node in the last entity which has one if its material highlighted (-1 when nothing is highlighted) 
        /// </summary>
        public int HighlightedMeshNodeIndex { get; private set; } = -1;

        /// <inheritdoc/>
        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameEntitySelectionService); yield return typeof(IEditorGameComponentGizmoService); } }

        /// <inheritdoc/>
        public override bool IsControllingMouse { get; protected set; }

        private IEditorGameEntitySelectionService Selection => game.EditorServices.Get<IEditorGameEntitySelectionService>();

        private IEditorGameComponentGizmoService Gizmos => game.EditorServices.Get<IEditorGameComponentGizmoService>();

        /// <summary>
        /// Clears any material previously highlighted with <see cref="HighlightMaterial"/>.
        /// </summary>
        public void ClearHighlight()
        {
            lock (lockObject)
            {
                editor.Controller.InvokeAsync(() =>
                {
                    foreach (var modelComponent in highlightedModelComponents)
                    {
                        HighlightRenderFeature.ModelHighlightColors.Remove(modelComponent);
                    }

                    foreach (var material in highlightedMaterials)
                    {
                        HighlightRenderFeature.MaterialsHighlightedForModel.Remove(material);
                        HighlightRenderFeature.MaterialHighlightColors.Remove(material);
                    }

                    highlightedModelComponents.Clear();
                    highlightedMaterials.Clear();
                });
            }
        }

        /// <summary>
        /// Highlights the given material, but only within the given entity.
        /// </summary>
        /// <param name="entity">The entity in which to highlight the given material. This method does nothing if it's null.</param>
        /// <param name="materialIndex">The index of the material to highlight in the given entity.</param>
        internal void HighlightMaterial(Entity entity, int materialIndex)
        {
            editor.Controller.EnsureGameAccess();
            lock (lockObject)
            {
                if (entity == null || materialIndex < 0)
                    return;

                var modelComponent = entity.Get<ModelComponent>();
                if (modelComponent?.Model == null)
                    return;

                var material = modelComponent.GetMaterial(materialIndex);
                if (material == null)
                    return;

                editor.Controller.InvokeAsync(() =>
                {
                    HighlightRenderFeature.MaterialsHighlightedForModel.Add(material);
                    highlightedMaterials.Add(material);

                    HighlightRenderFeature.ModelHighlightColors[modelComponent] = Color4.PremultiplyAlpha(AssetHighlighter.DirectReferenceColor);
                    highlightedModelComponents.Add(modelComponent);

                    foreach (var selectedEntity in Selection.GetSelectedIds().Select(x => editor.Controller.FindGameSidePart(x)).Cast<Entity>())
                    {
                        modelComponent = selectedEntity.Get<ModelComponent>();
                        if (modelComponent?.Model == null)
                            continue;

                        HighlightRenderFeature.ModelHighlightColors[modelComponent] = Color4.PremultiplyAlpha(AssetHighlighter.DirectReferenceColor);
                        highlightedModelComponents.Add(modelComponent);
                    }
                });
            }
        }

        /// <summary>
        /// Notifies view models of the currently highlighted material.
        /// </summary>
        /// <param name="entity">An entity that is using the material to highlight, or null if no material is highlighted</param>
        /// <param name="materialIndex">The index of the material to highlight in the given entity.</param>
        /// <param name="meshNodeIndex">The index of the mesh node in given entity.</param>
        internal void NotifyMaterialHighlighted(Entity entity, int materialIndex, int meshNodeIndex)
        {
            HighlightedMeshNodeIndex = meshNodeIndex;

            if (entity == HighlightedEntity && HighlightedMaterialIndex == materialIndex)
                return;

            HighlightedEntity = entity;
            HighlightedMaterialIndex = materialIndex;

            var entityId = entity != null ? editor.Controller.GetAbsoluteId(entity) : (AbsoluteId?)null;

            editor.Dispatcher.InvokeAsync(() =>
            {
                var entityGroup = entityId.HasValue ? (editor.FindPartViewModel(entityId.Value) as EntityViewModel)?.EntityHierarchy : null;
                EntityDesign assetEntity;
                // We retrieve the asset-side entity using the same guid that the given game-side entity.
                if (entityGroup != null && entityGroup.Hierarchy.Parts.TryGetValue(entityId.Value.ObjectId, out assetEntity))
                {
                    var modelComponent = assetEntity.Entity.Get<ModelComponent>();
                    if (modelComponent == null)
                    {
                        HighlightMaterialInPropertyGrid(null);
                        return;
                    }

                    var model = modelComponent.Model;
                    if (model == null)
                    {
                        HighlightMaterialInPropertyGrid(null);
                        return;
                    }

                    var reference = AttachedReferenceManager.GetOrCreateAttachedReference(model);
                    var modelViewModel = editor.Session.GetAssetById(reference.Id);
                    var modelAsset = modelViewModel?.AssetItem.Asset as IModelAsset;
                    if (modelAsset != null)
                    {
                        var materialInstance = modelAsset.Materials.Skip(materialIndex).FirstOrDefault();
                        if (string.IsNullOrEmpty(materialInstance?.Name))
                        {
                            HighlightMaterialInPropertyGrid(null);
                            return;
                        }

                        HighlightMaterialInPropertyGrid(materialInstance.Name);
                    }
                }
                else
                {
                    HighlightMaterialInPropertyGrid(null);
                }
            }).Forget();
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            game = editorGame;

            // Execute before others: we want this script to take over mouse control before others have a chance to do so
            game.Script.AddTask(Update, -1);
            return Task.FromResult(true);
        }

        public override void UpdateGraphicsCompositor(EditorServiceGame game)
        {
            base.UpdateGraphicsCompositor(game);

            var highlightRenderStage = new RenderStage("Highlight", "Highlight");
            game.SceneSystem.GraphicsCompositor.RenderStages.Add(highlightRenderStage);

            highlightRenderStage.Filter = new HighlightFilter();

            // Meshes
            var meshRenderFeature = game.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            // TODO: Complain (log) if there is no MeshRenderFeature
            if (meshRenderFeature != null)
            {
                meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
                {
                    EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Highlight",
                    RenderStage = highlightRenderStage,
                    RenderGroup = RenderGroupMask.All
                });
                meshRenderFeature.RenderFeatures.Add(new HighlightRenderFeature());
            }

            var editorCompositor = (EditorTopLevelCompositor)game.SceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = highlightRenderStage, Name = "Material Highlighting" });
        }

        private async Task Update()
        {
            while (!IsDisposed)
            {
                await game.Script.NextFrame();

                if (!IsActive)
                    continue;

                var entityUnderMouse = Gizmos.GetContentEntityUnderMouse();
                int materialSelected = -1;
                int meshSelected = -1;

                var previouslySelected = Selection.GetSelectedIds();
                bool entityUnderMouseInSelection = false;
                ClearHighlight();
                AbsoluteId? entityId = null;
                if (entityUnderMouse == null)
                {
                    var entityPicked = Selection.Pick();
                    materialSelected = entityPicked.MaterialIndex;
                    meshSelected = entityPicked.MeshNodeIndex;
                    entityUnderMouse = entityPicked.Entity;
                    if (entityUnderMouse != null)
                    {
                        entityId = editor.Controller.GetAbsoluteId(entityUnderMouse);
                        entityUnderMouseInSelection = previouslySelected.Contains(entityId.Value);
                    }
                    if (!game.Input.IsMousePositionLocked && entityUnderMouse != null && materialSelected >= 0 && entityUnderMouseInSelection)
                    {
                        HighlightMaterial(entityUnderMouse, materialSelected);
                    }
                }
                else
                {
                    entityId = editor.Controller.GetAbsoluteId(entityUnderMouse);
                }

                NotifyMaterialHighlighted(entityUnderMouseInSelection ? entityUnderMouse : null, materialSelected, meshSelected);

                if (IsMouseAvailable && entityUnderMouse != null && previouslySelected.Contains(entityId.Value) && game.Input.IsMouseButtonPressed(MouseButton.Left))
                {
                    IsControllingMouse = true;
                }
                
                if (IsControllingMouse && !game.Input.IsMouseButtonReleased(MouseButton.Left) && !game.Input.IsMouseButtonDown(MouseButton.Left))
                {
                    IsControllingMouse = false;
                }

                if (IsControllingMouse && game.Input.IsMouseButtonReleased(MouseButton.Left))
                {
                    // Note: we set IsControllingMouse back to false next frame so that no other EditorGameMouseServiceBase take over during the same frame
                    if (entityUnderMouse != null && materialSelected >= 0 && previouslySelected.Contains(entityId.Value))
                    {
                        editor.Dispatcher.Invoke(() => SelectMaterialInAssetView(entityUnderMouse, materialSelected));
                    }
                }
            }
        }

        /// <summary>
        /// Select the given material in the asset view.
        /// </summary>
        /// <param name="entity">The entity referencing the material, via its model component.</param>
        /// <param name="materialIndex">The index of the material to select in the model component, or in the model itself.</param>
        private void SelectMaterialInAssetView(Entity entity, int materialIndex)
        {
            var partId = editor.Controller.GetAbsoluteId(entity);
            var viewModel = (EntityViewModel)editor.FindPartViewModel(partId);
            var modelComp = viewModel.AssetSideEntity.Get<ModelComponent>();
            if (modelComp == null)
                return;

            var material = modelComp.Materials.SafeGet(materialIndex);
            if (material == null)
            {
                var modelViewModel = ContentReferenceHelper.GetReferenceTarget(viewModel.Editor.Session, modelComp.Model);
                var model = modelViewModel?.AssetItem.Asset as IModelAsset;
                if (model != null && model.Materials.Count > materialIndex)
                {
                    material = model.Materials[materialIndex].MaterialInstance.Material;
                }
            }

            if (material == null)
                return;

            var materialAsset = ContentReferenceHelper.GetReferenceTarget(viewModel.Editor.Session, material);
            editor.Session.ActiveAssetView.SelectAssetCommand.Execute(materialAsset);
        }

        private void HighlightMaterialInPropertyGrid(string materialName)
        {
            var node = editor.EditorProperties.ViewModel?.RootNode.Children.FirstOrDefault(x => x.Name == "Components");
            var modelComp = node?.Children.FirstOrDefault(x => x.NodeValue is ModelComponent);
            node = modelComp?.Children.FirstOrDefault(x => x.Name == "Materials");
            if (node != null)
            {
                foreach (var material in node.Children)
                {
                    ((NodeViewModel)material).IsHighlighted = material.DisplayName == materialName;
                }
            }
        }

        Tuple<Guid, int> IEditorGameMaterialHighlightViewModelService.GetTargetMeshIndex(EntityViewModel entity)
        {
            if (HighlightedEntity != null && IsActive)
            {
                return Tuple.Create(HighlightedEntity.Id, HighlightedMeshNodeIndex);
            }
            return null;
        }

        class HighlightFilter : RenderStageFilter
        {
            public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
            {
                var renderMesh = renderObject as RenderMesh;
                return renderMesh != null &&
                    (HighlightRenderFeature.MaterialHighlightColors.ContainsKey(renderMesh.MaterialPass.Material) ||
                    HighlightRenderFeature.MeshHighlightColors.ContainsKey(renderMesh.Mesh) ||
                    (HighlightRenderFeature.MaterialsHighlightedForModel.Contains(renderMesh.MaterialPass.Material)
                     && renderMesh.Source is ModelComponent component
                     && HighlightRenderFeature.ModelHighlightColors.ContainsKey(component)));
            }
        }
    }
}
