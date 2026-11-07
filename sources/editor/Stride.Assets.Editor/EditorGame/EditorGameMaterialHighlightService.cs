// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Models;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Services;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Input;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// The material selection mode of the scene view: the material of a selected entity under the mouse is highlighted (in
/// the view, and in the materials of the property grid), and a click on it selects the material asset in the asset view
/// instead of the entity.
/// </summary>
/// <remarks>
/// The cross-platform counterpart of the WPF <c>EditorGameMaterialHighlightService</c>. Off by default, as in the WPF
/// scene editor: the toolbar of the view turns it on.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 230)]
public sealed class EditorGameMaterialHighlightService : EditorGameMouseServiceBase, IEditorGameViewModelService
{
    // The direct reference color of the WPF asset highlighters
    private static readonly Color4 HighlightColor = Color4.PremultiplyAlpha(new Color4(1.0f, 0.35f, 0.25f, 0.8f));

    private readonly EntityHierarchyEditorController controller;
    private readonly EntityHierarchyEditorViewModel editor;
    // Read by the render thread, which is the game thread
    private readonly HashSet<ModelComponent> highlightedModelComponents = [];
    private readonly HashSet<Material> highlightedMaterials = [];
    private Entity? highlightedEntity;
    private int highlightedMaterialIndex = -1;
    private int highlightedMeshIndex = -1;
    private EntityHierarchyEditorGame? game;

    public EditorGameMaterialHighlightService(EntityHierarchyEditorController controller, EntityHierarchyEditorViewModel editor)
    {
        this.controller = controller;
        this.editor = editor;
    }

    /// <summary>
    /// Gets or sets whether the material selection mode is on (any thread).
    /// </summary>
    public override bool IsActive { get; set; }

    /// <inheritdoc/>
    public override bool IsControllingMouse { get; protected set; }

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies
    {
        get
        {
            yield return typeof(EditorGameEntitySelectionService);
            yield return typeof(IEditorGameComponentGizmoService);
        }
    }

    private EditorGameEntitySelectionService Selection => Services.Get<EditorGameEntitySelectionService>();

    private IEditorGameComponentGizmoService Gizmos => Services.Get<IEditorGameComponentGizmoService>();

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    /// <summary>
    /// Gets the entity and the mesh of the highlighted material, or <c>null</c> when the mode is off or no material is
    /// highlighted (game thread). Framing the selection frames this mesh instead, as in the WPF scene editor.
    /// </summary>
    internal (Entity Entity, int MeshIndex)? HighlightedMesh => IsActive && highlightedEntity is { } entity ? (entity, highlightedMeshIndex) : null;

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        EnsureNotDestroyed(nameof(EditorGameMaterialHighlightService));

        ClearHighlight();
        return base.DisposeAsync();
    }

    /// <inheritdoc/>
    public override void UpdateGraphicsCompositor(EditorServiceGame editorGame)
    {
        base.UpdateGraphicsCompositor(editorGame);

        var highlightRenderStage = new RenderStage("Highlight", "Highlight") { Filter = new HighlightFilter() };
        editorGame.SceneSystem.GraphicsCompositor.RenderStages.Add(highlightRenderStage);

        // Meshes
        if (editorGame.SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().FirstOrDefault() is { } meshRenderFeature)
        {
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Highlight",
                RenderStage = highlightRenderStage,
                RenderGroup = RenderGroupMask.All,
            });
            meshRenderFeature.RenderFeatures.Add(new HighlightRenderFeature());
        }

        var editorCompositor = (EditorTopLevelCompositor)editorGame.SceneSystem.GraphicsCompositor.Game;
        editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = highlightRenderStage, Name = "Material Highlighting" });
    }

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        game = (EntityHierarchyEditorGame)editorGame;

        // Before the other services, to take the mouse on a press before the selection and the tools see it
        game.Script.AddTask(Update, -1);
        return Task.FromResult(true);
    }

    private async Task Update()
    {
        while (!IsDisposed)
        {
            await Game.Script.NextFrame();

            ClearHighlight();
            if (!IsActive)
            {
                NotifyMaterialHighlighted(null, -1);
                IsControllingMouse = false;
                continue;
            }

            // The material of a selected entity under the mouse
            Entity? entity = null;
            var materialIndex = -1;
            var meshIndex = -1;
            if (Gizmos.GetContentEntityUnderMouse() is null)
            {
                var picking = Selection.Pick();
                if (picking.Entity is { } picked && picking.MaterialIndex >= 0 && Selection.GetSelectedIds().Contains(controller.GetAbsoluteId(picked)))
                {
                    entity = picked;
                    materialIndex = picking.MaterialIndex;
                    meshIndex = picking.MeshNodeIndex;
                }
            }

            if (entity is not null && !Game.Input.IsMousePositionLocked)
                HighlightMaterial(entity, materialIndex);
            highlightedMeshIndex = meshIndex;
            NotifyMaterialHighlighted(entity, materialIndex);

            // A click on the highlighted material selects it instead of the entity
            if (IsMouseAvailable && entity is not null && Game.Input.IsMouseButtonPressed(MouseButton.Left))
                IsControllingMouse = true;

            // The mouse is given back the frame after the release, so that no other service takes the release
            if (IsControllingMouse && !Game.Input.IsMouseButtonReleased(MouseButton.Left) && !Game.Input.IsMouseButtonDown(MouseButton.Left))
                IsControllingMouse = false;

            if (IsControllingMouse && Game.Input.IsMouseButtonReleased(MouseButton.Left) && entity is not null)
            {
                var entityId = controller.GetAbsoluteId(entity);
                _ = editor.Dispatcher.InvokeAsync(() => SelectMaterialInAssetView(entityId, materialIndex));
            }
        }
    }

    /// <summary>
    /// Highlights a material of an entity, on the entity and on the selected entities that use it.
    /// </summary>
    private void HighlightMaterial(Entity entity, int materialIndex)
    {
        if (entity.Get<ModelComponent>() is not { Model: not null } modelComponent || modelComponent.GetMaterial(materialIndex) is not { } material)
            return;

        HighlightRenderFeature.MaterialsHighlightedForModel.Add(material);
        highlightedMaterials.Add(material);

        HighlightRenderFeature.ModelHighlightColors[modelComponent] = HighlightColor;
        highlightedModelComponents.Add(modelComponent);

        foreach (var selectedEntity in Selection.GetSelectedIds().Select(controller.FindGameSidePart).OfType<Entity>())
        {
            if (selectedEntity.Get<ModelComponent>() is not { Model: not null } selectedModelComponent)
                continue;

            HighlightRenderFeature.ModelHighlightColors[selectedModelComponent] = HighlightColor;
            highlightedModelComponents.Add(selectedModelComponent);
        }
    }

    private void ClearHighlight()
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
    }

    /// <summary>
    /// Highlights the highlighted material in the materials of the property grid, when it changes.
    /// </summary>
    private void NotifyMaterialHighlighted(Entity? entity, int materialIndex)
    {
        if (entity == highlightedEntity && materialIndex == highlightedMaterialIndex)
            return;

        highlightedEntity = entity;
        highlightedMaterialIndex = materialIndex;
        var entityId = entity is not null ? controller.GetAbsoluteId(entity) : (Core.AbsoluteId?)null;
        editor.Dispatcher.InvokeAsync(() => HighlightMaterialInPropertyGrid(entityId is { } id ? GetMaterialName(id, materialIndex) : null));
    }

    /// <summary>
    /// Gets the name of a material of the model of an entity, as the property grid shows it (UI thread).
    /// </summary>
    private string? GetMaterialName(Core.AbsoluteId entityId, int materialIndex)
    {
        if (editor.FindPartViewModel(entityId) is not EntityViewModel entity || entity.AssetSideEntity.Get<ModelComponent>()?.Model is not { } model)
            return null;

        var modelAsset = ContentReferenceHelper.GetReferenceTarget(editor.Session, model)?.Asset as IModelAsset;
        return modelAsset?.Materials.Skip(materialIndex).FirstOrDefault()?.Name is { Length: > 0 } name ? name : null;
    }

    private void HighlightMaterialInPropertyGrid(string? materialName)
    {
        var components = editor.EditorProperties.ViewModel?.RootNode.Children.FirstOrDefault(x => x.Name == "Components");
        var modelComponent = components?.Children.FirstOrDefault(x => x.NodeValue is ModelComponent);
        if (modelComponent?.Children.FirstOrDefault(x => x.Name == "Materials") is not { } materials)
            return;

        foreach (var material in materials.Children.OfType<NodeViewModel>())
        {
            material.IsHighlighted = material.DisplayName == materialName;
        }
    }

    /// <summary>
    /// Selects in the asset view the material of the model of an entity (UI thread).
    /// </summary>
    private void SelectMaterialInAssetView(Core.AbsoluteId entityId, int materialIndex)
    {
        if (editor.FindPartViewModel(entityId) is not EntityViewModel entity || entity.AssetSideEntity.Get<ModelComponent>() is not { } modelComponent)
            return;

        // The material of the component overrides the one of the model
        var material = modelComponent.Materials.SafeGet(materialIndex);
        if (material is null && ContentReferenceHelper.GetReferenceTarget(editor.Session, modelComponent.Model)?.Asset is IModelAsset modelAsset && materialIndex < modelAsset.Materials.Count)
            material = modelAsset.Materials[materialIndex].MaterialInstance.Material;

        if (material is not null && ContentReferenceHelper.GetReferenceTarget(editor.Session, material) is { } materialAsset)
            editor.Session.AssetCollection.SelectAssetCommand.Execute(materialAsset);
    }

    private sealed class HighlightFilter : RenderStageFilter
    {
        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            return renderObject is RenderMesh renderMesh
                && (HighlightRenderFeature.MaterialHighlightColors.ContainsKey(renderMesh.MaterialPass.Material)
                    || HighlightRenderFeature.MeshHighlightColors.ContainsKey(renderMesh.Mesh)
                    || (HighlightRenderFeature.MaterialsHighlightedForModel.Contains(renderMesh.MaterialPass.Material)
                        && renderMesh.Source is ModelComponent component
                        && HighlightRenderFeature.ModelHighlightColors.ContainsKey(component)));
        }
    }
}
