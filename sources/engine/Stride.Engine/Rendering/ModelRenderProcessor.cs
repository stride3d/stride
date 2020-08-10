// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Threading;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Rendering
{
    public class ModelRenderProcessor : EntityProcessor<ModelComponent, RenderModel>, IEntityComponentRenderProcessor
    {
        private Material fallbackMaterial;

        public Dictionary<ModelComponent, RenderModel> RenderModels => ComponentDatas;

        public VisibilityGroup VisibilityGroup { get; set; }

        public ModelRenderProcessor() : base(typeof(TransformComponent))
        {
        }

        /// <inheritdoc />
        protected internal override void OnSystemAdd()
        {
            var graphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            fallbackMaterial = Material.New(graphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeTextureColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
        }

        /// <inheritdoc />
        protected override RenderModel GenerateComponentData(Entity entity, ModelComponent component)
        {
            return new RenderModel();
        }

        /// <inheritdoc />
        protected override bool IsAssociatedDataValid(Entity entity, ModelComponent component, RenderModel associatedData)
        {
            return true;
        }

        /// <inheritdoc />
        protected override void OnEntityComponentRemoved(Entity entity, ModelComponent component, RenderModel renderModel)
        {
            // Remove old meshes
            if (renderModel.Meshes != null)
            {
                foreach (var renderMesh in renderModel.Meshes)
                {
                    // Unregister from render system
                    VisibilityGroup.RenderObjects.Remove(renderMesh);
                }
            }
        }

        /// <inheritdoc />
        public override void Draw(RenderContext context)
        {
            // Note: we are rebuilding RenderMeshes every frame
            // TODO: check if it wouldn't be better to add/remove directly in CheckMeshes()?
            //foreach (var entity in ComponentDatas)
            Dispatcher.ForEach(ComponentDatas, entity =>
            {
                var modelComponent = entity.Key;
                var renderModel = entity.Value;

                CheckMeshes(modelComponent, renderModel);
                UpdateRenderModel(modelComponent, renderModel);
            });
        }

        private void UpdateRenderModel(ModelComponent modelComponent, RenderModel renderModel)
        {
            if (modelComponent.Model == null)
                return;

            var modelViewHierarchy = modelComponent.Skeleton;
            var nodeTransformations = modelViewHierarchy.NodeTransformations;

            for (int sourceMeshIndex = 0; sourceMeshIndex < renderModel.Materials.Length; sourceMeshIndex++)
            {
                var passes = renderModel.Materials[sourceMeshIndex].MeshCount;
                // Note: indices in RenderModel.Meshes and Model.Meshes are different (due to multipass materials)
                var meshIndex = renderModel.Materials[sourceMeshIndex].MeshStartIndex;

                for (int pass = 0; pass < passes; ++pass, ++meshIndex)
                {
                    var renderMesh = renderModel.Meshes[meshIndex];

                    renderMesh.Enabled = modelComponent.Enabled;
                    renderMesh.RenderGroup = modelComponent.RenderGroup;

                    if (modelComponent.Enabled)
                    {
                        // Copy world matrix
                        var mesh = renderModel.Model.Meshes[sourceMeshIndex];
                        var meshInfo = modelComponent.MeshInfos[sourceMeshIndex];
                        var nodeIndex = mesh.NodeIndex;
                        renderMesh.World = nodeTransformations[nodeIndex].WorldMatrix;
                        renderMesh.IsScalingNegative = nodeTransformations[nodeIndex].IsScalingNegative;
                        renderMesh.BoundingBox = new BoundingBoxExt(meshInfo.BoundingBox);
                        renderMesh.BlendMatrices = meshInfo.BlendMatrices;
                    }
                }
            }
        }

        private void UpdateMaterial(RenderMesh renderMesh, MaterialPass materialPass, MaterialInstance modelMaterialInstance, ModelComponent modelComponent)
        {
            renderMesh.MaterialPass = materialPass;

            var isShadowCaster = modelComponent.IsShadowCaster;
            if (modelMaterialInstance != null)
                isShadowCaster &= modelMaterialInstance.IsShadowCaster;

            if (isShadowCaster != renderMesh.IsShadowCaster)
            {
                renderMesh.IsShadowCaster = isShadowCaster;
                VisibilityGroup.NeedActiveRenderStageReevaluation = true;
            }
        }

        private Material FindMaterial(Material materialOverride, MaterialInstance modelMaterialInstance)
        {
            return materialOverride ?? modelMaterialInstance?.Material ?? fallbackMaterial;
        }

        private void CheckMeshes(ModelComponent modelComponent, RenderModel renderModel)
        {
            // Check if model changed
            var model = modelComponent.Model;
            if (renderModel.Model == model)
            {
                // Check if any material pass count changed
                if (model != null)
                {
                    // Number of meshes changed in the model?
                    if (model.Meshes.Count != renderModel.UniqueMeshCount)
                        goto RegenerateMeshes;

                    if (modelComponent.Enabled)
                    {
                        // Check materials
                        var modelComponentMaterials = modelComponent.Materials;
                        for (int sourceMeshIndex = 0; sourceMeshIndex < model.Meshes.Count; sourceMeshIndex++)
                        {
                            ref var material = ref renderModel.Materials[sourceMeshIndex];
                            var materialIndex = model.Meshes[sourceMeshIndex].MaterialIndex;

                            var newMaterial = FindMaterial(modelComponentMaterials.SafeGet(materialIndex), model.Materials.GetItemOrNull(materialIndex));

                            // If material changed or its number of pass changed, trigger a full regeneration of RenderMeshes (note: we could do partial later)
                            if ((newMaterial?.Passes.Count ?? 1) != material.MeshCount)
                                goto RegenerateMeshes;

                            // Update materials
                            material.Material = newMaterial;
                            int meshIndex = material.MeshStartIndex;
                            for (int pass = 0; pass < material.MeshCount; ++pass, ++meshIndex)
                            {
                                UpdateMaterial(renderModel.Meshes[meshIndex], newMaterial?.Passes[pass], model.Materials.GetItemOrNull(materialIndex), modelComponent);
                            }
                        }
                    }
                }

                return;
            }

        RegenerateMeshes:
            renderModel.Model = model;

            // Remove old meshes
            if (renderModel.Meshes != null)
            {
                lock (VisibilityGroup.RenderObjects)
                {
                    foreach (var renderMesh in renderModel.Meshes)
                    {
                        // Unregister from render system
                        VisibilityGroup.RenderObjects.Remove(renderMesh);
                    }
                }
            }

            if (model == null)
                return;

            // Count meshes
            var materialMeshCount = 0;
            renderModel.Materials = new RenderModel.MaterialInfo[model.Meshes.Count];
            for (int sourceMeshIndex = 0; sourceMeshIndex < model.Meshes.Count; sourceMeshIndex++)
            {
                var materialIndex = model.Meshes[sourceMeshIndex].MaterialIndex;
                var material = FindMaterial(modelComponent.Materials.SafeGet(materialIndex), model.Materials.GetItemOrNull(materialIndex));
                var meshCount = material?.Passes.Count ?? 1;
                renderModel.Materials[sourceMeshIndex] = new RenderModel.MaterialInfo { Material = material, MeshStartIndex = materialMeshCount, MeshCount = meshCount };
                materialMeshCount += meshCount;
            }

            // Create render meshes
            var renderMeshes = new RenderMesh[materialMeshCount];
            for (int sourceMeshIndex = 0; sourceMeshIndex < model.Meshes.Count; sourceMeshIndex++)
            {
                var mesh = model.Meshes[sourceMeshIndex];
                ref var material = ref renderModel.Materials[sourceMeshIndex];
                int meshIndex = material.MeshStartIndex;

                for (int pass = 0; pass < material.MeshCount; ++pass, ++meshIndex)
                {
                    // TODO: Somehow, if material changed we might need to remove/add object in render system again (to evaluate new render stage subscription)
                    var materialIndex = mesh.MaterialIndex;
                    renderMeshes[meshIndex] = new RenderMesh
                    {
                        Source = modelComponent,
                        RenderModel = renderModel,
                        Mesh = mesh,
                    };

                    // Update material
                    UpdateMaterial(renderMeshes[meshIndex], material.Material?.Passes[pass], model.Materials.GetItemOrNull(materialIndex), modelComponent);
                }
            }

            renderModel.Meshes = renderMeshes;
            renderModel.UniqueMeshCount = model.Meshes.Count;

            // Update before first add so that RenderGroup is properly set
            UpdateRenderModel(modelComponent, renderModel);

            // Update and register with render system
            lock (VisibilityGroup.RenderObjects)
            {
                foreach (var renderMesh in renderMeshes)
                {
                    VisibilityGroup.RenderObjects.Add(renderMesh);
                }
            }
        }
    }
}
