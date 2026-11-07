// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Gizmos;
using Stride.Assets.Presentation.AssetEditors.EntityHierarchyEditor.Game;
using Stride.Assets.Presentation.AssetEditors.GameEditor.Game;
using Stride.Assets.Presentation.SceneEditor;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Editor.EditorGame.Game;
using Stride.Editor.EditorGame.ViewModels;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Assets.Editor.EditorGame;

/// <summary>
/// Draws the light probe gizmos (<see cref="LightProbeGizmo"/>), and the volumes between the light probes of the scene as
/// a yellow wireframe ("Light probe volumes" in the gizmo options).
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>EditorGameLightProbeGizmoService</c>, without the light probe computation of the WPF
/// lighting toolbar (<c>IEditorGameLightProbeService</c>), which the Avalonia scene editor does not have yet.
/// </remarks>
[EditorGameService(typeof(EntityHierarchyEditorController), Order = 150)]
public sealed class EditorGameLightProbeGizmoService : EditorGameServiceBase, IEditorGameViewModelService
{
    // Wireframe lightprobes mesh
    public const RenderGroupMask LightProbeWireGroupMask = RenderGroupMask.Group29;
    public const RenderGroup LightProbeWireGroup = RenderGroup.Group29;

    private readonly List<IReferencable> wireframeResources = [];
    private EntityHierarchyEditorGame? game;
    // Root debug entity, which will have child entities attached to it for every debug element
    private Entity? debugEntity;
    private ModelComponent? wireframeModelComponent;
    private Material? wireframeMaterial;
    private bool isWireframeVisible = true;
    private LightProbeRuntimeData? currentLightProbeRuntimeData;

    /// <inheritdoc/>
    public override IEnumerable<Type> Dependencies { get { yield return typeof(IEditorGameComponentGizmoService); } }

    /// <summary>
    /// Gets or sets whether the volumes between the light probes show.
    /// </summary>
    public bool IsLightProbeVolumesVisible
    {
        get => isWireframeVisible;
        set
        {
            isWireframeVisible = value;

            // Making a local copy of reference since we change it from UI thread (should be OK from different thread since Enabled is atomic)
            var localWireframeModelComponent = wireframeModelComponent;
            if (localWireframeModelComponent != null)
                localWireframeModelComponent.Enabled = value;
        }
    }

    private EntityHierarchyEditorGame Game => game ?? throw new InvalidOperationException("The service is not initialized.");

    /// <inheritdoc/>
    protected override Task<bool> Initialize(EditorServiceGame editorGame)
    {
        ArgumentNullException.ThrowIfNull(editorGame);
        game = (EntityHierarchyEditorGame)editorGame;

        var pickingRenderStage = game.EditorSceneSystem.GraphicsCompositor.RenderStages.First(x => x.Name == "Picking");
        var selectionRenderStage = new RenderStage("SelectionGizmo", "Wireframe")
        {
            Filter = new WireframeFilter(),
        };

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
        editorCompositor.AddOverlayGizmoCompositor(new SceneRendererCollection
        {
            new SingleStageRenderer { RenderStage = lightProbeGizmoRenderStage, Name = "LightProbe Gizmos" },
            new SingleStageRenderer { RenderStage = selectionRenderStage, Name = "LightProbe Selection Gizmo" },
        });

        // Add debug entity
        debugEntity = new Entity("LightProbe debug entity");
        game.EditorScene.Entities.Add(debugEntity);

        var color = Color.Yellow;
        color.A = 0x9F;
        wireframeMaterial = CreateDebugMaterial(color);

        game.Script.AddTask(Update);

        return Task.FromResult(true);
    }

    /// <inheritdoc/>
    public override ValueTask DisposeAsync()
    {
        if (game is not null && debugEntity is not null)
        {
            Cleanup();
            game.EditorScene.Entities.Remove(debugEntity);
        }

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
        debugEntity?.Transform.Children.Clear();
    }

    private async Task Update()
    {
        // Check every frame if light probes need rebuild
        while (!IsDisposed)
        {
            await Game.Script.NextFrame();

            if (!IsActive)
                continue;

            // Rebuild light probes (if necessary)
            UpdateLightProbeWireframe();
        }
    }

    private void UpdateLightProbeWireframe()
    {
        var lightProbeProcessor = Game.SceneSystem.SceneInstance.GetProcessor<LightProbeProcessor>();

        var lightProbeRuntimeData = lightProbeProcessor?.VisibilityGroup.Tags.Get(LightProbeRenderer.CurrentLightProbes);
        if (lightProbeProcessor is null || lightProbeRuntimeData is null)
        {
            // Nothing, just remove existing wireframe and exit
            Cleanup();
            currentLightProbeRuntimeData = null;
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
                if (lightProbeRuntimeData.LightProbes[lightProbeIndex] is not LightProbeComponent lightProbe)
                    continue;

                if (lightProbe.Entity.Transform.WorldMatrix.TranslationVector != lightProbeRuntimeData.Vertices[lightProbeIndex])
                {
                    needPositionRefresh = true;
                    needWireframeRefresh = true;
                }

                // check if lightprobe coefficients changed
                var coefficientIndex = lightProbeIndex * LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder;
                for (var i = 0; i < LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder; ++i)
                {
                    var expectedCoefficient = lightProbe.Coefficients != null ? lightProbe.Coefficients[i] : default;
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
            var mesh = ConvertToMesh(Game.GraphicsDevice, PrimitiveType.LineList, lightProbeRuntimeData);
            var model = new Model
            {
                mesh,
                wireframeMaterial,
            };

            wireframeModelComponent = new ModelComponent(model) { RenderGroup = LightProbeWireGroup, Enabled = IsLightProbeVolumesVisible };
            var lightProbeWireframe = new Entity("LightProbe Wireframe") { wireframeModelComponent };
            debugEntity?.AddChild(lightProbeWireframe);
        }
    }

    private Material CreateDebugMaterial(Color4 color)
    {
        var lightProbeMaterial = Material.New(Game.GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor()),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Emissive = new MaterialEmissiveMapFeature(new ComputeColor()) { UseAlpha = true },
            }
        });

        var deviceSpaceColor = color.ToColorSpace(Game.GraphicsDevice.ColorSpace);

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
        for (var i = 0; i < vertices.Length; i++)
            BoundingBox.Merge(ref boundingBox, ref vertices[i].Position, out boundingBox);

        // Compute bounding sphere
        BoundingSphere boundingSphere;
        fixed (void* verticesPtr = vertices)
            BoundingSphere.FromPoints((IntPtr)verticesPtr, 0, vertices.Length, VertexPositionNormalTexture.Size, out boundingSphere);

        var layout = vertices[0].GetLayout();

        var meshDraw = new MeshDraw
        {
            IndexBuffer = new IndexBufferBinding(Buffer.Index.New(graphicsDevice, indices), true, indices.Length),
            VertexBuffers = [new VertexBufferBinding(Buffer.Vertex.New(graphicsDevice, vertices), layout, vertices.Length)],
            DrawCount = indices.Length,
            PrimitiveType = primitiveType
        };

        wireframeResources.Add(meshDraw.VertexBuffers[0].Buffer);
        wireframeResources.Add(meshDraw.IndexBuffer.Buffer);

        return new Mesh { Draw = meshDraw, BoundingBox = boundingBox, BoundingSphere = boundingSphere };
    }

    /// <summary>
    /// Keeps the light probe gizmos that are selected.
    /// </summary>
    private sealed class WireframeFilter : RenderStageFilter
    {
        public override bool IsVisible(RenderObject renderObject, RenderView renderView, RenderViewStage renderViewStage)
        {
            return renderObject is RenderMesh { Source: ModelComponent { Entity: { } entity } } && entity.Tags.Get(EditorGameComponentGizmoService.SelectedKey);
        }
    }
}

/// <summary>
/// Draws the lines of a render stage with anti-aliasing and blending, without writing the depth.
/// </summary>
/// <remarks>
/// The agnostic copy of the WPF <c>AntiAliasLinePipelineProcessor</c>.
/// </remarks>
internal sealed class AntiAliasLinePipelineProcessor : PipelineProcessor
{
    public RenderStage? RenderStage { get; set; }

    public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
    {
        if (renderNode.RenderStage == RenderStage)
        {
            pipelineState.BlendState = BlendStates.NonPremultiplied;
            pipelineState.DepthStencilState = DepthStencilStates.DepthRead;
            pipelineState.RasterizerState.MultisampleAntiAliasLine = true;
        }
    }
}
