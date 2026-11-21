// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering.Voxels;
using Stride.Rendering.Voxels.VoxelGI;
using Stride.Shaders;
using Stride.Shaders.Compiler;

namespace Stride.Engine.Tests;

/// <summary>
/// One test per rendering feature no sample uses: MSAA, temporal anti-aliasing, ambient occlusion, depth of field, local
/// reflections, cubemap backgrounds, a spot light projecting a texture and thin glass. Each test turns on its feature only.
/// </summary>
/// <remarks>
/// The tests without screenshot draw features only to compile their shaders, so the shader corpus captures their
/// permutations: clear coat, instancing, skinned tangents, mesh velocity, voxel global illumination and the scene editor's
/// render modes.
/// </remarks>
public class RenderFeatureTests : RenderFeatureTestBase
{
    public enum BackgroundMode { None, Cubemap3D, Cubemap2D }

    public enum VoxelMode { None, Default, Variant }

    // Temporal anti-aliasing and depth of field autofocus build up over the previous frames
    private const int ScreenshotFrame = 10;

    public MultisampleCount Msaa { get; init; } = MultisampleCount.None;

    /// <summary>
    /// Turns on the post effects of the test, the optional ones start off. Without it, no post effects.
    /// </summary>
    public Action<PostProcessingEffects> PostEffects { get; init; }

    public BackgroundMode Background { get; init; }

    /// <summary>
    /// Adds a spot light projecting a texture: C# mixes the projection shaders into the light group's composition node.
    /// </summary>
    public bool ProjectedTexture { get; init; }

    /// <summary>
    /// Adds a glass sphere (thin glass specular model): C# mixes the transmittance stream into the material's shading node.
    /// </summary>
    public bool ThinGlass { get; init; }

    /// <summary>
    /// Adds a clear coat sphere: its first pass mixes the metal flakes surface shaders.
    /// </summary>
    public bool ClearCoat { get; init; }

    /// <summary>
    /// Draws the teapot and the skinned knight with instancing: the instanced normal mapping and skinning transformations.
    /// </summary>
    public bool Instancing { get; init; }

    /// <summary>
    /// Adds the skinned knight with its tangents skinned too. No importer asks for it, so it is set on the meshes by code.
    /// </summary>
    public bool SkinningTangent { get; init; }

    /// <summary>
    /// Adds the per-mesh velocity of temporal anti-aliasing. No compositor registers it, so it is added by code.
    /// </summary>
    public bool MeshVelocity { get; init; }

    /// <summary>
    /// Draws three teapots with the scene editor's single stream render modes: diffuse, tangent normal and world normal.
    /// </summary>
    public bool MaterialStreamFilters { get; init; }

    /// <summary>
    /// Voxel global illumination. The variant uses the voxelization, march set and layout the defaults don't.
    /// </summary>
    public VoxelMode Voxels { get; init; }

    /// <summary>
    /// Compiles the scene editor's effect for a mesh whose effect failed to compile.
    /// </summary>
    public bool EditorErrorEffect { get; init; }

    /// <summary>
    /// False for a render that isn't the same on every run (e.g. it depends on GPU readback timing): it then only
    /// checks that the frames render, without exception or graphics validation error.
    /// </summary>
    public bool Screenshot { get; init; } = true;

    // The teapot, about 4 units from the camera, in focus: the floor in front and behind it blurred
    private static readonly Vector4 DepthOfFieldAreas = new(0.5f, 3.5f, 5f, 10f);

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var forwardRenderer = (ForwardRenderer)SceneSystem.GraphicsCompositor.SingleView;
        forwardRenderer.MSAALevel = Msaa;
        if (PostEffects != null)
        {
            var postEffects = new PostProcessingEffects();
            postEffects.AmbientOcclusion.Enabled = false;
            postEffects.LocalReflections.Enabled = false;
            postEffects.DepthOfField.Enabled = false;
            postEffects.Bloom.Enabled = false;
            postEffects.LightStreak.Enabled = false;
            postEffects.LensFlare.Enabled = false;
            postEffects.Antialiasing.Enabled = false;
            PostEffects(postEffects);
            forwardRenderer.PostEffects = postEffects;
        }

        CreateScene();

        if (Background != BackgroundMode.None)
        {
            // One color per face
            const int size = 16;
            var faces = new[] { Color.Red, Color.Green, Color.Blue, Color.Yellow, Color.Cyan, Color.Magenta }
                .Select(color => Enumerable.Repeat(color, size * size).ToArray())
                .ToArray();
            Scene.Entities.Add(new Entity
            {
                new BackgroundComponent
                {
                    Texture = Texture.NewCube(GraphicsDevice, size, PixelFormat.R8G8B8A8_UNorm, faces),
                    Is2D = Background == BackgroundMode.Cubemap2D,
                },
            });
        }

        if (ProjectedTexture)
        {
            // A 4x4 checker, above the teapot, pointing down
            const int size = 64;
            var checker = Enumerable.Range(0, size * size).Select(i => (i % size / (size / 4) + i / size / (size / 4)) % 2 == 0 ? Color.White : Color.Orange).ToArray();
            var spot = new Entity
            {
                new LightComponent
                {
                    Type = new LightSpot
                    {
                        Color = new ColorRgbProvider(Color.White),
                        Range = 8,
                        AngleInner = 30,
                        AngleOuter = 35,
                        ProjectiveTexture = Texture.New2D(GraphicsDevice, size, size, PixelFormat.R8G8B8A8_UNorm, checker),
                    },
                    Intensity = 20,
                },
            };
            spot.Transform.Position = new Vector3(0, 4, 0);
            spot.Transform.Rotation = Quaternion.RotationX(-MathUtil.PiOverTwo);
            Scene.Entities.Add(spot);
        }

        if (ThinGlass)
        {
            // In front of the teapot, slightly tinted
            var glassMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color4(0.8f, 0.9f, 1.0f, 1.0f))),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.95f)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.0f)),
                    SpecularModel = new MaterialSpecularThinGlassModelFeature(),
                },
            }, Content);
            var glass = new Entity { new ModelComponent(new ProceduralModelDescriptor(new SphereProceduralModel { Radius = 0.5f, MaterialInstance = { Material = glassMaterial } }).GenerateModel(Services)) };
            glass.Transform.Position = new Vector3(0.3f, 0.6f, 1.5f);
            Scene.Entities.Add(glass);
        }

        var meshRenderFeature = SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>().First();

        if (ClearCoat)
        {
            var clearCoatMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color4(0.6f, 0.05f, 0.05f, 1.0f))),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.6f)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.5f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature(),
                    ClearCoat = new MaterialClearCoatFeature(),
                },
            }, Content);
            var sphere = new Entity { new ModelComponent(new ProceduralModelDescriptor(new SphereProceduralModel { Radius = 0.5f, MaterialInstance = { Material = clearCoatMaterial } }).GenerateModel(Services)) };
            sphere.Transform.Position = new Vector3(0.3f, 0.6f, 1.5f);
            Scene.Entities.Add(sphere);
        }

        if (Instancing)
        {
            meshRenderFeature.RenderFeatures.Add(new InstancingRenderFeature());

            // Three copies side by side of the normal mapped teapot, and of the knight
            var teapotModel = new ProceduralModelDescriptor(new TeapotProceduralModel { Size = 0.5f, MaterialInstance = { Material = Content.Load<Material>("c100_chr_ch00_Knight_KINGHT") } }).GenerateModel(Services);
            Scene.Entities.Add(CreateInstanced(teapotModel, new Vector3(-1, 0.3f, 1.5f)));
            Scene.Entities.Add(CreateInstanced(Content.Load<Model>("knight Model"), new Vector3(-1, 0, 0.5f)));
        }

        if (SkinningTangent)
        {
            var knightModel = Content.Load<Model>("knight Model");
            foreach (var mesh in knightModel.Meshes)
                mesh.Parameters.Set(MaterialKeys.HasSkinningTangent, true);
            var knight = new Entity { new ModelComponent(knightModel) };
            knight.Transform.Position = new Vector3(-0.5f, 0, 1);
            Scene.Entities.Add(knight);
        }

        if (MeshVelocity)
            meshRenderFeature.RenderFeatures.Add(new MeshVelocityRenderFeature());

        if (MaterialStreamFilters)
        {
            var normalStreams = new MaterialNormalMapFeature().GetStreams().ToList();
            var filters = new[] { MaterialDiffuseMapFeature.DiffuseStream.Filter, normalStreams[0].Filter, normalStreams[1].Filter };
            var streamFilterFeature = new MaterialStreamFilterRenderFeature();
            meshRenderFeature.RenderFeatures.Add(streamFilterFeature);

            // One normal mapped teapot per filter
            var knightMaterial = Content.Load<Material>("c100_chr_ch00_Knight_KINGHT");
            for (int i = 0; i < filters.Length; i++)
            {
                var teapot = new ModelComponent(new ProceduralModelDescriptor(new TeapotProceduralModel { Size = 0.5f, MaterialInstance = { Material = knightMaterial } }).GenerateModel(Services));
                var entity = new Entity { teapot };
                entity.Transform.Position = new Vector3(i - 1, 0.3f, 1.5f);
                Scene.Entities.Add(entity);
                streamFilterFeature.Filters.Add(teapot, filters[i]);
            }
        }

        if (Voxels != VoxelMode.None)
        {
            UseVoxelRenderer(meshRenderFeature);

            var emissionOpacity = new VoxelAttributeEmissionOpacity();
            var volume = new VoxelVolumeComponent { Attributes = { emissionOpacity }, VoxelVolumeSize = 8 };
            var voxelLight = new LightVoxel { Volume = volume };
            if (Voxels == VoxelMode.Variant)
            {
                volume.VoxelizationMethod = new VoxelizationMethodSingleAxis();
                emissionOpacity.VoxelLayout = new VoxelLayoutAnisotropic();
                voxelLight.DiffuseMarcher = new VoxelMarchSetRandomHemisphere();
            }
            var volumeEntity = new Entity { volume };
            volumeEntity.Transform.Position = new Vector3(0, 1, 0);
            Scene.Entities.Add(volumeEntity);
            Scene.Entities.Add(new Entity { new LightComponent { Type = voxelLight } });
        }

        if (EditorErrorEffect)
        {
            // What StrideEditorForwardShadingEffect mixes when SceneEditorParameters.IsEffectError is set (the editor's
            // effects are not in this package, its shaders are)
            var errorEffect = new ShaderMixinSource { Name = "StrideEditorForwardShadingEffect" };
            foreach (var shader in new[] { "ShaderBase", "ShadingBase", "TransformationBase", "TransformationWAndVP", "CompilationErrorShader" })
                errorEffect.Mixins.Add(new ShaderClassSource(shader));
            var compilerParameters = new CompilerParameters();
            compilerParameters.EffectParameters.Platform = GraphicsDevice.Platform;
            compilerParameters.EffectParameters.Profile = GraphicsDevice.ShaderProfile ?? GraphicsDevice.Features.RequestedProfile;
            var result = EffectSystem.Compiler.Compile(errorEffect, compilerParameters).Bytecode.WaitForResult();
            Assert.True(result.Bytecode != null, result.CompilationLog?.ToText());
        }
    }

    private Entity CreateInstanced(Model model, Vector3 position)
    {
        var instances = new InstancingUserArray();
        instances.UpdateWorldMatrices([.. Enumerable.Range(0, 3).Select(i => Matrix.Translation(position + new Vector3(i, 0, 0)))]);
        return new Entity { new ModelComponent(model), new InstancingComponent { Type = instances } };
    }

    /// <summary>
    /// Replaces the forward renderer with the voxel one, set up like <c>DefaultGraphicsCompositorVoxels</c>: two voxelization
    /// stages, the voxel render feature and pipeline processor, and the voxel light renderer.
    /// </summary>
    private void UseVoxelRenderer(MeshRenderFeature meshRenderFeature)
    {
        var compositor = SceneSystem.GraphicsCompositor;
        var voxelStages = new List<RenderStage> { new("VoxelizationPassFirst", "Voxelizer"), new("VoxelizationPassSecond", "Voxelizer2") };
        foreach (var stage in voxelStages)
        {
            compositor.RenderStages.Add(stage);
            meshRenderFeature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
            {
                OpaqueRenderStage = stage,
                TransparentRenderStage = stage,
                EffectName = "StrideForwardShadingEffectVXGI.VoxelizeToFragmentsEffect",
            });
        }
        meshRenderFeature.PipelineProcessors.Add(new VoxelPipelineProcessor { VoxelRenderStage = voxelStages });
        meshRenderFeature.RenderFeatures.Add(new VoxelRenderFeature());
        meshRenderFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().First().LightRenderers.Add(new LightVoxelRenderer());

        var forwardRenderer = (ForwardRenderer)compositor.SingleView;
        var voxelRenderer = new ForwardRendererVoxels
        {
            Clear = forwardRenderer.Clear,
            LightProbes = forwardRenderer.LightProbes,
            OpaqueRenderStage = forwardRenderer.OpaqueRenderStage,
            TransparentRenderStage = forwardRenderer.TransparentRenderStage,
            PostEffects = forwardRenderer.PostEffects,
            VoxelRenderer = new VoxelRenderer { VoxelStages = voxelStages },
        };
        voxelRenderer.ShadowMapRenderStages.AddRange(forwardRenderer.ShadowMapRenderStages);
        var renderers = ((SceneRendererCollection)((SceneCameraRenderer)compositor.Game).Child).Children;
        renderers[renderers.IndexOf(forwardRenderer)] = voxelRenderer;
        compositor.SingleView = voxelRenderer;
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();
        if (Screenshot)
            FrameGameSystem.TakeScreenshot(ScreenshotFrame);
        else
            FrameGameSystem.Draw(ScreenshotFrame, () => { });
    }

    [SkippableFact]
    public void Msaa4x() => RunGameTest(new RenderFeatureTests { Msaa = MultisampleCount.X4, TestName = nameof(Msaa4x) });

    // The color, normal and specular targets are all multisampled, and resolved one after another
    [SkippableFact]
    public void Msaa4xLocalReflections() => RunGameTest(new RenderFeatureTests { Msaa = MultisampleCount.X4, PostEffects = x => x.LocalReflections.Enabled = true, TestName = nameof(Msaa4xLocalReflections) });

    [SkippableFact]
    public void TemporalAntiAliasing() => RunGameTest(new RenderFeatureTests { PostEffects = x => x.Antialiasing = new TemporalAntiAliasEffect(), TestName = nameof(TemporalAntiAliasing) });

    [SkippableFact]
    public void AmbientOcclusion() => RunGameTest(new RenderFeatureTests { PostEffects = x => x.AmbientOcclusion.Enabled = true, TestName = nameof(AmbientOcclusion) });

    [SkippableFact]
    public void DepthOfFieldTripleRhombi() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = false; x.DepthOfField.DOFAreas = DepthOfFieldAreas; x.DepthOfField.Technique = BokehTechnique.HexagonalTripleRhombi; },
        TestName = nameof(DepthOfFieldTripleRhombi),
    });

    [SkippableFact]
    public void DepthOfFieldMcIntosh() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = false; x.DepthOfField.DOFAreas = DepthOfFieldAreas; x.DepthOfField.Technique = BokehTechnique.HexagonalMcIntosh; },
        TestName = nameof(DepthOfFieldMcIntosh),
    });

    // The focus follows a depth read back from the GPU, so the frame it reaches depends on timing: no screenshot
    [SkippableFact]
    public void DepthOfFieldAutoFocus() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => { x.DepthOfField.Enabled = true; x.DepthOfField.AutoFocus = true; },
        Screenshot = false,
        TestName = nameof(DepthOfFieldAutoFocus),
    });

    [SkippableFact]
    public void LocalReflections() => RunGameTest(new RenderFeatureTests { PostEffects = x => x.LocalReflections.Enabled = true, TestName = nameof(LocalReflections) });

    [SkippableFact]
    public void BackgroundCubemap3D() => RunGameTest(new RenderFeatureTests { Background = BackgroundMode.Cubemap3D, TestName = nameof(BackgroundCubemap3D) });

    [SkippableFact]
    public void BackgroundCubemap2D() => RunGameTest(new RenderFeatureTests { Background = BackgroundMode.Cubemap2D, TestName = nameof(BackgroundCubemap2D) });

    [SkippableFact]
    public void SpotLightProjectedTexture() => RunGameTest(new RenderFeatureTests { ProjectedTexture = true, Screenshot = false, TestName = nameof(SpotLightProjectedTexture) });

    [SkippableFact]
    public void ThinGlassSpecular() => RunGameTest(new RenderFeatureTests { ThinGlass = true, Screenshot = false, TestName = nameof(ThinGlassSpecular) });

    [SkippableFact]
    public void ClearCoatSpecular() => RunGameTest(new RenderFeatureTests { ClearCoat = true, Screenshot = false, TestName = nameof(ClearCoatSpecular) });

    // A skinned model adds run-to-run noise: no screenshot
    [SkippableFact]
    public void InstancingNormalMapAndSkinning() => RunGameTest(new RenderFeatureTests { Instancing = true, Screenshot = false, TestName = nameof(InstancingNormalMapAndSkinning) });

    [SkippableFact]
    public void SkinningWithTangents() => RunGameTest(new RenderFeatureTests { SkinningTangent = true, Screenshot = false, TestName = nameof(SkinningWithTangents) });

    [SkippableFact]
    public void TemporalAntiAliasingMeshVelocity() => RunGameTest(new RenderFeatureTests
    {
        PostEffects = x => x.Antialiasing = new TemporalAntiAliasEffect(),
        MeshVelocity = true,
        Screenshot = false,
        TestName = nameof(TemporalAntiAliasingMeshVelocity),
    });

    [SkippableFact]
    public void EditorMaterialStreamFilters() => RunGameTest(new RenderFeatureTests { MaterialStreamFilters = true, Screenshot = false, TestName = nameof(EditorMaterialStreamFilters) });

    [SkippableFact]
    public void EditorEffectErrorFallback() => RunGameTest(new RenderFeatureTests { EditorErrorEffect = true, Screenshot = false, TestName = nameof(EditorEffectErrorFallback) });

    private const string VoxelGlobalIlluminationUnsupported = "MoltenVK has no geometry shaders for the default voxelization, and the tri-axis voxelization pipeline fails to create there";

    [SkippableFact]
    public void VoxelGlobalIllumination()
    {
        Skip.If(Platform.Type is PlatformType.macOS or PlatformType.iOS, VoxelGlobalIlluminationUnsupported);
        RunGameTest(new RenderFeatureTests { Voxels = VoxelMode.Default, Screenshot = false, TestName = nameof(VoxelGlobalIllumination) });
    }

    [SkippableFact]
    public void VoxelGlobalIlluminationVariant()
    {
        Skip.If(Platform.Type is PlatformType.macOS or PlatformType.iOS, VoxelGlobalIlluminationUnsupported);
        RunGameTest(new RenderFeatureTests { Voxels = VoxelMode.Variant, Screenshot = false, TestName = nameof(VoxelGlobalIlluminationVariant) });
    }

    /// <summary>
    /// Sets a material stream filter per model. The scene editor's render modes set one for the whole scene.
    /// </summary>
    private class MaterialStreamFilterRenderFeature : SubRenderFeature
    {
        private StaticObjectPropertyKey<RenderEffect> renderEffectKey;

        public Dictionary<ModelComponent, ShaderSource> Filters { get; } = [];

        protected override void InitializeCore()
        {
            base.InitializeCore();

            renderEffectKey = ((RootEffectRenderFeature)RootRenderFeature).RenderEffectKey;
        }

        public override void PrepareEffectPermutations(RenderDrawContext context)
        {
            var renderEffects = RootRenderFeature.RenderData.GetData(renderEffectKey);
            int effectSlotCount = ((RootEffectRenderFeature)RootRenderFeature).EffectPermutationSlotCount;

            foreach (var renderObject in RootRenderFeature.RenderObjects)
            {
                if (renderObject.Source is not ModelComponent model || !Filters.TryGetValue(model, out var filter))
                    continue;

                for (int i = 0; i < effectSlotCount; ++i)
                {
                    var renderEffect = renderEffects[renderObject.StaticObjectNode * effectSlotCount + i];
                    if (renderEffect != null && renderEffect.IsUsedDuringThisFrame(RenderSystem))
                        renderEffect.EffectValidator.ValidateParameter(MaterialKeys.PixelStageSurfaceFilter, filter);
                }
            }
        }
    }
}
