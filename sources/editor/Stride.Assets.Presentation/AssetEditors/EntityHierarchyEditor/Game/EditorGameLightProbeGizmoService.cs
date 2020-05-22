// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Core.Storage;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Services;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.ViewModels;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.AssetEditors.Gizmos;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Editor.EditorGame.Game;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game
{
    /// <summary>
    /// Handles rendering of navigation meshes associated with the current scene
    /// </summary>
    public class EditorGameLightProbeGizmoService : EditorGameServiceBase, IEditorGameLightProbeService
    {
        // Wireframe lightprobes mesh
        public const RenderGroupMask LightProbeWireGroupMask = RenderGroupMask.Group29;
        public const RenderGroup LightProbeWireGroup = RenderGroup.Group29;

        private readonly EntityHierarchyEditorViewModel editor;

        private EntityHierarchyEditorGame game;

        // Root debug entity, which will have child entities attached to it for every debug element
        private Entity debugEntity;
        private ModelComponent wireframeModelComponent;
        private Material wireframeMaterial;
        private bool isWireframeVisible = true;
        private LightProbeRuntimeData currentLightProbeRuntimeData;
        private List<IReferencable> wireframeResources = new List<IReferencable>();

        public EditorGameLightProbeGizmoService(EntityHierarchyEditorViewModel editor)
        {
            this.editor = editor;
        }

        public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameComponentGizmoService); } }

        /// <inheritdoc/>
        public bool IsLightProbeVolumesVisible
        {
            get { return isWireframeVisible; }
            set
            {
                isWireframeVisible = value;

                // Making a local copy of reference since we change it from UI thread (should be OK from different thread since Enabled is atomic)
                var localWireframeModelComponent = wireframeModelComponent;
                if (localWireframeModelComponent != null)
                    localWireframeModelComponent.Enabled = value;
            }
        }

        /// <inheritdoc/>
        public Task UpdateLightProbeCoefficients()
        {
            return editor.Controller.InvokeAsync(() =>
            {
                game.SceneSystem.SceneInstance.GetProcessor<LightProbeProcessor>()?.UpdateLightProbeCoefficients();
            });
        }

        /// <inheritdoc/>
        public Task<Dictionary<Guid, FastList<Color3>>> RequestLightProbesStep()
        {
            return editor.Controller.InvokeAsync(() =>
            {
                editor.ServiceProvider.TryGet<RenderDocManager>()?.StartFrameCapture(game.GraphicsDevice, IntPtr.Zero);

                // Reset lightprobes temporarily (if requested)
                // Note: we only process first LightProbeProcessor
                var runtimeData = game.SceneSystem.SceneInstance.GetProcessor<LightProbeProcessor>()?.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
                if (runtimeData == null)
                    return new Dictionary<Guid, FastList<Color3>>();

                var editorCompositor = game.EditorSceneSystem.GraphicsCompositor.Game;
                try
                {
                    // Disable Gizmo
                    game.EditorSceneSystem.GraphicsCompositor.Game = null;

                    // Regenerate lightprobe coefficients (rendering)
                    var lightProbes = LightProbeGenerator.GenerateCoefficients(game);

                    // TODO: Use LightProbe Id instead of entity id once copy/paste and duplicate properly remap them
                    return lightProbes.ToDictionary(x => x.Key.Entity.Id, x => x.Value);
                }
                finally
                {
                    game.EditorSceneSystem.GraphicsCompositor.Game = editorCompositor;

                    editor.ServiceProvider.TryGet<RenderDocManager>()?.EndFrameCapture(game.GraphicsDevice, IntPtr.Zero);
                }
            });
        }

        protected override Task<bool> Initialize(EditorServiceGame editorGame)
        {
            if (editorGame == null) throw new ArgumentNullException(nameof(editorGame));
            game = (EntityHierarchyEditorGame)editorGame;

            var pickingRenderStage = game.EditorSceneSystem.GraphicsCompositor.RenderStages.First(x => x.Name == "Picking");
            // TODO: Move selection/wireframe render stage in EditorGameComponentGizmoService (as last render step?)
            var selectionRenderStage = new RenderStage("SelectionGizmo", "Wireframe");
            selectionRenderStage.Filter = new WireframeFilter();

            var lightProbeGizmoRenderStage = new RenderStage("LightProbeGizmo", "Main");
            var lightProbeWireframeRenderStage = new RenderStage("LightProbeWireframe", "Main");

            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(selectionRenderStage);
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(lightProbeGizmoRenderStage);
            game.EditorSceneSystem.GraphicsCompositor.RenderStages.Add(lightProbeWireframeRenderStage);

            // Meshes
            var meshRenderFeature = game.EditorSceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = LightProbeGizmo.LightProbeGroupMask,
                RenderStage = lightProbeGizmoRenderStage
            });
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Picking",
                RenderGroup = LightProbeGizmo.LightProbeGroupMask,
                RenderStage = pickingRenderStage,
            });
            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect + ".Wireframe",
                RenderGroup = LightProbeGizmo.LightProbeGroupMask,
                RenderStage = selectionRenderStage,
            });
            meshRenderFeature.RenderFeatures.Add(new WireframeRenderFeature());

            meshRenderFeature.RenderStageSelectors.Add(new SimpleGroupToRenderStageSelector
            {
                EffectName = EditorGraphicsCompositorHelper.EditorForwardShadingEffect,
                RenderGroup = LightProbeWireGroupMask,
                RenderStage = lightProbeWireframeRenderStage,
            });
            meshRenderFeature.PipelineProcessors.Add(new AntiAliasLinePipelineProcessor { RenderStage = lightProbeWireframeRenderStage });
            var editorCompositor = (EditorTopLevelCompositor)game.EditorSceneSystem.GraphicsCompositor.Game;
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = lightProbeWireframeRenderStage, Name = "LightProbe Wireframe Gizmos" });
            editorCompositor.PostGizmoCompositors.Add(new ClearRenderer { ClearFlags = ClearRendererFlags.DepthOnly });
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = lightProbeGizmoRenderStage, Name = "LightProbe Gizmos" });
            editorCompositor.PostGizmoCompositors.Add(new SingleStageRenderer { RenderStage = selectionRenderStage, Name = "LightProbe Selection Gizmo" });

            // Add debug entity
            debugEntity = new Entity("Navigation debug entity");
            game.EditorScene.Entities.Add(debugEntity);

            var color = Color.Yellow;
            color.A = 0x9F;
            wireframeMaterial = CreateDebugMaterial(color);

            game.Script.AddTask(Update);

            return Task.FromResult(true);
        }

        public override Task DisposeAsync()
        {
            Cleanup();
            game.EditorScene.Entities.Remove(debugEntity);

            return base.DisposeAsync();
        }

        private void Cleanup()
        {
            // Clean GPU buffers used by previous wireframe
            foreach (var resource in wireframeResources)
            {
                resource.Release();
            }
            wireframeResources.Clear();

            wireframeModelComponent = null;
            debugEntity.Transform.Children.Clear();
        }

        private async Task Update()
        {
            // Check every frame if light probes need rebuild
            while (!IsDisposed)
            {
                await game.Script.NextFrame();

                if (!IsActive)
                    continue;

                // Rebuild light probes (if necessary)
                UpdateLightProbeWireframe();
            }
        }

        private void UpdateLightProbeWireframe()
        {
            var lightProbeProcessor = game.SceneSystem.SceneInstance.GetProcessor<LightProbeProcessor>();

            var lightProbeRuntimeData = lightProbeProcessor?.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
            if (lightProbeRuntimeData == null)
            {
                // Nothing, just remove existing wireframe and exit
                Cleanup();
                return;
            }

            var needWireframeRefresh = false;

            if (lightProbeRuntimeData != currentLightProbeRuntimeData)
            {
                // LightProbe runtime data changed (light probe added or removed) => force a wireframe refresh
                currentLightProbeRuntimeData = lightProbeRuntimeData;
                needWireframeRefresh = true;
            }
            else
            {
                // check if we need to trigger a manual refresh (the LightProbeProcessor only reacts to LightProbe added/removed at runtime)
                var needPositionRefresh = false;
                var needCoefficientsRefresh = false;
                for (var lightProbeIndex = 0; lightProbeIndex < lightProbeRuntimeData.LightProbes.Length; lightProbeIndex++)
                {
                    // check if lightprobe moved
                    var lightProbe = lightProbeRuntimeData.LightProbes[lightProbeIndex] as LightProbeComponent;
                    if (lightProbe == null)
                        continue;

                    if (lightProbe.Entity.Transform.WorldMatrix.TranslationVector != lightProbeRuntimeData.Vertices[lightProbeIndex])
                    {
                        needPositionRefresh = true;
                        needWireframeRefresh = true;
                    }

                    // check if lightprobe coefficients changed
                    var coefficientIndex = lightProbeIndex * LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder;
                    for (int i = 0; i < LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder; ++i)
                    {
                        var expectedCoefficient = lightProbe.Coefficients != null ? lightProbe.Coefficients[i] : default(Color3);
                        if (expectedCoefficient != lightProbeRuntimeData.Coefficients[coefficientIndex + i])
                        {
                            needCoefficientsRefresh = true;
                        }
                    }
                }

                if (needPositionRefresh)
                {
                    lightProbeProcessor.UpdateLightProbePositions();
                    lightProbeRuntimeData = lightProbeProcessor.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
                    if (lightProbeRuntimeData == null)
                    {
                        Cleanup();
                        return;
                    }
                }
                else if (needCoefficientsRefresh)
                {
                    lightProbeProcessor.UpdateLightProbeCoefficients();
                }
            }

            // Do we need to regenerate the wireframe?
            if (!needWireframeRefresh)
                return;

            Cleanup();

            // Need at least a tetrahedron to not have empty buffers
            if (lightProbeRuntimeData.Tetrahedra.Count > 0)
            {
                var mesh = ConvertToMesh(game.GraphicsDevice, PrimitiveType.LineList, lightProbeRuntimeData);
                var model = new Model
                {
                    mesh,
                    wireframeMaterial,
                };

                wireframeModelComponent = new ModelComponent(model) { RenderGroup = LightProbeWireGroup, Enabled = IsLightProbeVolumesVisible };
                var lightProbeWireframe = new Entity("LightProbe Wireframe") { wireframeModelComponent };
                debugEntity.AddChild(lightProbeWireframe);
            }
        }

        private Material CreateDebugMaterial(Color4 color)
        {
            Material lightProbeMaterial = Material.New(game.GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor()) { UseAlpha = true },
                }
            });

            Color4 deviceSpaceColor = color.ToColorSpace(game.GraphicsDevice.ColorSpace);

            // set the color to the material
            lightProbeMaterial.Passes[0].Parameters.Set(MaterialKeys.DiffuseValue, deviceSpaceColor);
            lightProbeMaterial.Passes[0].Parameters.Set(MaterialKeys.EmissiveIntensity, 1.0f);
            lightProbeMaterial.Passes[0].Parameters.Set(MaterialKeys.EmissiveValue, deviceSpaceColor);
            lightProbeMaterial.Passes[0].HasTransparency = true;

            return lightProbeMaterial;
        }

        private unsafe Mesh ConvertToMesh(GraphicsDevice graphicsDevice, PrimitiveType primitiveType, LightProbeRuntimeData lightProbeRuntimeData)
        {
            // Generate data for vertex buffer
            var vertices = new VertexPositionNormalColor[lightProbeRuntimeData.LightProbes.Length];
            for (var i = 0; i < lightProbeRuntimeData.LightProbes.Length; i++)
            {
                vertices[i] = new VertexPositionNormalColor(lightProbeRuntimeData.Vertices[i], Vector3.Zero, Color.White);
            }

            // Generate data for index buffer
            var indices = new int[lightProbeRuntimeData.Faces.Count * 6];
            for (var i = 0; i < lightProbeRuntimeData.Faces.Count; ++i)
            {
                var currentFace = lightProbeRuntimeData.Faces[i];

                // Skip infinite edges to not clutter display
                // Maybe we could reenable it when we have better infinite nodes
                if (currentFace.Vertices[0] >= lightProbeRuntimeData.UserVertexCount
                    || currentFace.Vertices[1] >= lightProbeRuntimeData.UserVertexCount
                    || currentFace.Vertices[2] >= lightProbeRuntimeData.UserVertexCount)
                    continue;

                indices[i * 6 + 0] = currentFace.Vertices[0];
                indices[i * 6 + 1] = currentFace.Vertices[1];
                indices[i * 6 + 2] = currentFace.Vertices[1];
                indices[i * 6 + 3] = currentFace.Vertices[2];
                indices[i * 6 + 4] = currentFace.Vertices[2];
                indices[i * 6 + 5] = currentFace.Vertices[0];
            }

            var boundingBox = BoundingBox.Empty;
            for (int i = 0; i < vertices.Length; i++)
                BoundingBox.Merge(ref boundingBox, ref vertices[i].Position, out boundingBox);

            // Compute bounding sphere
            BoundingSphere boundingSphere;
            fixed (void* verticesPtr = vertices)
                BoundingSphere.FromPoints((IntPtr)verticesPtr, 0, vertices.Length, VertexPositionNormalTexture.Size, out boundingSphere);

            var layout = vertices[0].GetLayout();

            var meshDraw = new MeshDraw
            {
                IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices).RecreateWith(indices), true, indices.Length),
                VertexBuffers = new[] { new VertexBufferBinding(Buffer.New(graphicsDevice, vertices, BufferFlags.VertexBuffer).RecreateWith(vertices), layout, vertices.Length) },
                DrawCount = indices.Length,
                PrimitiveType = primitiveType,
            };

            wireframeResources.Add(meshDraw.VertexBuffers[0].Buffer);
            wireframeResources.Add(meshDraw.IndexBuffer.Buffer);

            return new Mesh { Draw = meshDraw, BoundingBox = boundingBox, BoundingSphere = boundingSphere };
        }

        class WireframeFilter : RenderStageFilter
        {
            public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
            {
                // TODO: More general implementation (esp. if moved from this class to EditorGameComponentGizmoService)
                var renderMesh = renderObject as RenderMesh;
                if (renderMesh != null)
                {
                    // TODO: Avoid having to go through entity
                    return (renderMesh.Source as ModelComponent)?.Entity?.Tags.Get(EditorGameComponentGizmoService.SelectedKey) ?? false;
                }

                return false;
            }
        }
    }
}
