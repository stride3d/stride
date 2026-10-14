// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

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

namespace Stride.Engine.Tests;

/// <summary>
/// One test per rendering feature no sample uses: MSAA, temporal anti-aliasing, ambient occlusion, depth of field, local
/// reflections, cubemap backgrounds, a spot light projecting a texture and thin glass. Each test turns on its feature only.
/// </summary>
public class RenderFeatureTests : RenderFeatureTestBase
{
    public enum BackgroundMode { None, Cubemap3D, Cubemap2D }

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
}
