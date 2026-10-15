// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering.Voxels;
using Stride.Rendering.Voxels.VoxelGI;

namespace Stride.Graphics.Tests;

/// <summary>
/// Renders lit quads with a transparent material and reads the colors back, to check that the material's
/// alpha scales the lit color exactly once, whatever the light.
/// </summary>
/// <remarks>
/// <para>
///   Transparent materials blend premultiplied (One, InverseSourceAlpha) and the clear coat ones additively,
///   and the background is black, so a pixel holds the lit color multiplied by the alpha the shader applied.
///   Each material is drawn twice, once with alpha 1 and once with alpha 0.5, and the ratio of the two
///   pixels is that alpha.
/// </para>
/// <para>
///   The camera is orthographic, far away and looks along the quad normals, and the directional light
///   shines the same way, so view, light and normal are the same on every pixel: the lighting is flat over
///   a quad and the same for both quads of a pair.
/// </para>
/// </remarks>
public class MaterialTransparencyTests : GameTestBase
{
    public enum LightKind
    {
        Directional,
        Ambient,
        Skybox,
        LightProbes,
        VoxelGI,
    }

    private const int Width = 320;
    private const int Height = 96;
    private const float ViewHeight = 4.0f;
    private const float QuadSize = 1.5f;

    /// <summary>
    /// The quad pairs, one per column: the material at alpha 1 (top row) and with the alpha under test (bottom
    /// row), which keeps half of the lit color.
    /// </summary>
    private static readonly (string Name, Func<GraphicsDevice, Material> Reference, Func<GraphicsDevice, Material> Test)[] Pairs =
    [
        ("diffuse", d => DiffuseMaterial(d, Blend(1.0f)), d => DiffuseMaterial(d, Blend(0.5f))),
        ("specular", d => SpecularMaterial(d, Blend(1.0f)), d => SpecularMaterial(d, Blend(0.5f))),
        // Additive alpha 0.25 gives a specular factor of 0.5 (and a diffuse factor of 0)
        ("additive specular", d => SpecularMaterial(d, Blend(1.0f)), d => SpecularMaterial(d, new MaterialTransparencyAdditiveFeature { Alpha = new ComputeFloat(0.25f) })),
        ("clear coat", d => ClearCoatMaterial(d, Blend(1.0f)), d => ClearCoatMaterial(d, Blend(0.5f))),
    ];

    /// <summary>
    /// The pairs each light has to light. Light probes only give diffuse lighting.
    /// </summary>
    private static readonly Dictionary<LightKind, string[]> LitPairs = new()
    {
        [LightKind.Directional] = ["diffuse", "specular", "additive specular", "clear coat"],
        [LightKind.Ambient] = ["diffuse", "specular", "additive specular", "clear coat"],
        [LightKind.Skybox] = ["diffuse", "specular", "additive specular", "clear coat"],
        [LightKind.LightProbes] = ["diffuse", "clear coat"],
        [LightKind.VoxelGI] = ["diffuse", "specular", "additive specular", "clear coat"],
    };

    private readonly LightKind lightKind;
    private Texture renderTarget;
    private Half4[] pixels;

    public MaterialTransparencyTests() : this(LightKind.Directional)
    {
    }

    private MaterialTransparencyTests(LightKind lightKind)
    {
        this.lightKind = lightKind;

        // Light probes read their data from buffers, voxel GI writes 3D textures from compute shaders
        var profile = lightKind == LightKind.VoxelGI ? GraphicsProfile.Level_11_0 : GraphicsProfile.Level_10_0;
        GraphicsDeviceManager.PreferredGraphicsProfile = [profile];
        GraphicsDeviceManager.ShaderProfile = profile;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        // A float target, so values are linear and nothing clips
        renderTarget = Texture.New2D(GraphicsDevice, Width, Height, PixelFormat.R16G16B16A16_Float, TextureFlags.RenderTarget | TextureFlags.ShaderResource);

        // The test compositor: light probes on, no post effects
        var compositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");
        var cameraRenderer = (SceneCameraRenderer)compositor.Game;
        foreach (var forwardRenderer in ((SceneRendererCollection)cameraRenderer.Child).Children.OfType<ForwardRenderer>())
            forwardRenderer.Clear.Color = Color.Black;
        compositor.Game = new RenderTextureSceneRenderer { RenderTexture = renderTarget, Child = cameraRenderer };
        SceneSystem.GraphicsCompositor = compositor;

        var scene = new Scene();

        var camera = new Entity("Camera")
        {
            new CameraComponent
            {
                Projection = CameraProjectionMode.Orthographic,
                OrthographicSize = ViewHeight,
                UseCustomAspectRatio = true,
                AspectRatio = (float)Width / Height,
                FarClipPlane = 2000.0f,
                Slot = compositor.Cameras[0].ToSlotId(),
            },
        };
        // Far away: the view vector goes to the camera position even for an orthographic camera, so this
        // keeps it the same over all the quads (it decides where reflections sample the skybox)
        camera.Transform.Position = new Vector3(0, 0, 1000);
        scene.Entities.Add(camera);

        for (var i = 0; i < Pairs.Length; i++)
        {
            AddQuad(scene, Pairs[i].Reference(GraphicsDevice), QuadPosition(i, test: false));
            AddQuad(scene, Pairs[i].Test(GraphicsDevice), QuadPosition(i, test: true));
        }

        switch (lightKind)
        {
            case LightKind.Directional:
                // No rotation: the light shines along -Z, towards the quads
                scene.Entities.Add(new Entity("Light") { new LightComponent { Type = new LightDirectional { Color = new ColorRgbProvider(Color.White) }, Intensity = 0.5f } });
                break;
            case LightKind.Ambient:
                scene.Entities.Add(new Entity("Light") { new LightComponent { Type = new LightAmbient { Color = new ColorRgbProvider(Color.White) }, Intensity = 0.5f } });
                break;
            case LightKind.Skybox:
                scene.Entities.Add(TakeSkyboxLight());
                break;
            case LightKind.LightProbes:
                // A tetrahedron around all the quads, with the same flat color everywhere
                AddLightProbe(scene, new Vector3(0.0f, 20.0f, 0.0f));
                AddLightProbe(scene, new Vector3(-25.0f, -10.0f, 15.0f));
                AddLightProbe(scene, new Vector3(25.0f, -10.0f, 15.0f));
                AddLightProbe(scene, new Vector3(0.0f, -10.0f, -20.0f));
                break;
            case LightKind.VoxelGI:
                AddVoxelGlobalIllumination(compositor, cameraRenderer, scene);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        SceneSystem.SceneInstance = new SceneInstance(Services, scene);
    }

    private static Vector3 QuadPosition(int pair, bool test)
        => new((pair - (Pairs.Length - 1) * 0.5f) * 2.0f * QuadSize, test ? -1.0f : 1.0f, 0.0f);

    private void AddQuad(Scene scene, Material material, Vector3 position)
    {
        var model = new ProceduralModelDescriptor(
            new PlaneProceduralModel
            {
                Size = new Vector2(QuadSize),
                MaterialInstance = { Material = material },
            }).GenerateModel(Services);

        var entity = new Entity { new ModelComponent { Model = model } };
        entity.Transform.Position = position;
        // The plane is authored facing +Y, turn it to face the camera
        entity.Transform.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo);
        scene.Entities.Add(entity);
    }

    /// <summary>
    /// Takes the skybox light out of the lighting test scene.
    /// </summary>
    private Entity TakeSkyboxLight()
    {
        var lightingScene = Content.Load<Scene>("LightingTests/LightingScene");
        var skybox = lightingScene.Entities.First(x => x.Name == "Skybox");
        lightingScene.Entities.Remove(skybox);
        skybox.Transform.Rotation = Quaternion.Identity;
        skybox.Get<LightComponent>().Enabled = true;

        return skybox;
    }

    private static void AddLightProbe(Scene scene, Vector3 position)
    {
        // Only the constant band, so the probe lights its surroundings with a flat color
        var coefficients = new List<Color3>(
            new Color3[LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder]);
        coefficients[0] = new Color3(0.5f);

        var entity = new Entity("LightProbe") { new LightProbeComponent { Coefficients = coefficients } };
        entity.Transform.Position = position;
        scene.Entities.Add(entity);
    }

    /// <summary>
    /// Adds to the compositor what the voxel GI compositor of Stride.Voxels has, and to the scene a voxel
    /// volume, its light and an emissive wall between the camera and the quads, which only the voxelization
    /// sees.
    /// </summary>
    private void AddVoxelGlobalIllumination(GraphicsCompositor compositor, SceneCameraRenderer cameraRenderer, Scene scene)
    {
        RenderStage[] voxelStages = [new("VoxelizationPassFirst", "Voxelizer"), new("VoxelizationPassSecond", "Voxelizer2")];

        var meshRenderFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().First();
        foreach (var stage in voxelStages)
        {
            compositor.RenderStages.Add(stage);

            // Opaque objects only: voxelized, the transparent quads would occlude the cones they start
            meshRenderFeature.RenderStageSelectors.Add(new MeshTransparentRenderStageSelector
            {
                OpaqueRenderStage = stage,
                EffectName = "StrideForwardShadingEffectVXGI.VoxelizeToFragmentsEffect",
            });
        }
        meshRenderFeature.PipelineProcessors.Add(new VoxelPipelineProcessor { VoxelRenderStage = [.. voxelStages] });
        meshRenderFeature.RenderFeatures.Add(new VoxelRenderFeature());
        meshRenderFeature.RenderFeatures.OfType<ForwardLightingRenderFeature>().First().LightRenderers.Add(new LightVoxelRenderer());

        var renderers = ((SceneRendererCollection)cameraRenderer.Child).Children;
        var forwardRenderer = renderers.OfType<ForwardRenderer>().First();
        var voxelForwardRenderer = new ForwardRendererVoxels
        {
            Clear = forwardRenderer.Clear,
            LightProbes = forwardRenderer.LightProbes,
            OpaqueRenderStage = forwardRenderer.OpaqueRenderStage,
            TransparentRenderStage = forwardRenderer.TransparentRenderStage,
            GBufferRenderStage = forwardRenderer.GBufferRenderStage,
            VoxelRenderer = new VoxelRenderer { VoxelStages = [.. voxelStages] },
        };
        voxelForwardRenderer.ShadowMapRenderStages.AddRange(forwardRenderer.ShadowMapRenderStages);
        renderers[renderers.IndexOf(forwardRenderer)] = voxelForwardRenderer;

        var volume = new VoxelVolumeComponent
        {
            VoxelVolumeSize = 20.0f,
            AproximateVoxelSize = 0.5f,
            Attributes = { new VoxelAttributeEmissionOpacity() },
        };
        scene.Entities.Add(new Entity("VoxelVolume") { volume });
        scene.Entities.Add(new Entity("VoxelLight")
        {
            new LightComponent { Type = new LightVoxel { Volume = volume, BounceIntensityScale = 1.0f, SpecularIntensityScale = 1.0f } },
        });

        // The wall lights the quads through the voxels only: it is in a render group the camera does not draw
        var wallMaterial = Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Black)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Emissive = new MaterialEmissiveMapFeature(new ComputeColor(Color.White)),
                CullMode = CullMode.None,
            },
        });
        var wallModel = new ProceduralModelDescriptor(
            new PlaneProceduralModel { Size = new Vector2(16.0f), MaterialInstance = { Material = wallMaterial } }).GenerateModel(Services);
        var wall = new Entity("Wall") { new ModelComponent { Model = wallModel, RenderGroup = RenderGroup.Group1 } };
        wall.Transform.Position = new Vector3(0.0f, 0.0f, 2.0f);
        wall.Transform.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo);
        scene.Entities.Add(wall);
        cameraRenderer.RenderMask = RenderGroupMask.All & ~RenderGroupMask.Group1;
    }

    private static MaterialTransparencyBlendFeature Blend(float alpha) => new() { Alpha = new ComputeFloat(alpha) };

    // The GGX environment function that needs no lookup texture: a runtime material does not load it
    private static MaterialSpecularMicrofacetModelFeature SpecularModel() => new() { Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial() };

    private static Material DiffuseMaterial(GraphicsDevice device, IMaterialTransparencyFeature transparency)
    {
        return Material.New(device, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color4(0.5f, 0.5f, 0.5f, 1.0f))),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Transparency = transparency,
                CullMode = CullMode.None,
            },
        });
    }

    private static Material SpecularMaterial(GraphicsDevice device, IMaterialTransparencyFeature transparency)
    {
        return Material.New(device, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Black)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Specular = new MaterialSpecularMapFeature { SpecularMap = new ComputeColor(new Color4(0.5f, 0.5f, 0.5f, 1.0f)) },
                SpecularModel = SpecularModel(),
                MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.2f)),
                Transparency = transparency,
                CullMode = CullMode.None,
            },
        });
    }

    private static Material ClearCoatMaterial(GraphicsDevice device, IMaterialTransparencyFeature transparency)
    {
        // Flat normal maps, so the clear coat's normals are the quad's
        var flatNormal = new Color4(0.5f, 0.5f, 1.0f, 1.0f);

        return Material.New(device, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.Black)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0.0f)),
                SpecularModel = SpecularModel(),
                ClearCoat = new MaterialClearCoatFeature
                {
                    BasePaintDiffuseMap = new ComputeColor(new Color4(0.5f, 0.5f, 0.5f, 1.0f)),
                    BasePaintGlossinessMap = new ComputeFloat(0.5f),
                    MetalFlakesDiffuseMap = new ComputeColor(new Color4(0.5f, 0.5f, 0.5f, 1.0f)),
                    MetalFlakesGlossinessMap = new ComputeFloat(0.5f),
                    MetalFlakesMetalnessMap = new ComputeFloat(0.0f),
                    MetalFlakesNormalMap = new ComputeColor(flatNormal),
                    ClearCoatGlossinessMap = new ComputeFloat(0.5f),
                    ClearCoatMetalnessMap = new ComputeFloat(0.0f),
                    OrangePeelNormalMap = new ComputeColor(flatNormal),
                },
                Transparency = transparency,
                CullMode = CullMode.None,
            },
        });
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        // Gives the game frames to render the quads, read back in Draw. Voxel GI gets a few, so the voxel
        // volume holds the wall's light before the last one
        FrameGameSystem.Draw(lightKind == LightKind.VoxelGI ? 3 : 0, () => { });
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        // Every frame: the last one is what the test checks
        pixels = renderTarget.GetData<Half4>(GraphicsContext.CommandList);
    }

    /// <summary>
    /// The color at the center of a quad.
    /// </summary>
    private Color3 Sample(Vector3 position)
    {
        var viewWidth = ViewHeight * Width / Height;
        var x = (int)((position.X / viewWidth + 0.5f) * Width);
        var y = (int)((0.5f - position.Y / ViewHeight) * Height);
        var pixel = pixels[y * Width + x];

        return new Color3((float)pixel.X, (float)pixel.Y, (float)pixel.Z);
    }

    [Theory]
    [InlineData(LightKind.Directional)]
    [InlineData(LightKind.Ambient)]
    [InlineData(LightKind.Skybox)]
    [InlineData(LightKind.LightProbes)]
    // Passes its checks, but the D3D11 debug layer warns on every frame that voxelization binds a texture as
    // a compute UAV while it is still bound as an input, which fails any game test
    [InlineData(LightKind.VoxelGI, Skip = "Voxel GI: CSSetUnorderedAccessViews hazard in voxelization (D3D11 debug layer warning)")]
    public void AlphaScalesLitColorOnce(LightKind light)
    {
        var game = new MaterialTransparencyTests(light);
        RunGameTest(game);

        Assert.NotNull(game.pixels);

        var failures = new List<string>();
        var report = new List<string>();

        for (var i = 0; i < Pairs.Length; i++)
        {
            var reference = game.Sample(QuadPosition(i, test: false));
            var test = game.Sample(QuadPosition(i, test: true));
            var lit = LitPairs[light].Contains(Pairs[i].Name);

            // Over the sum of the channels, so a dim channel does not decide
            var ratio = test.R + test.G + test.B > 0.0f ? (test.R + test.G + test.B) / (reference.R + reference.G + reference.B) : 0.0f;
            report.Add($"{Pairs[i].Name}: alpha 1 = {reference}, tested = {test}, ratio {ratio:F4}");

            if (!lit)
            {
                if (reference.R + reference.G + reference.B > 1e-4f)
                    failures.Add($"{Pairs[i].Name} is lit, expected no lighting from {light}");
            }
            else if (reference.R + reference.G + reference.B < 1e-3f)
            {
                failures.Add($"{Pairs[i].Name} is not lit by {light}");
            }
            else if (Math.Abs(ratio - 0.5f) > 0.02f)
            {
                failures.Add($"{Pairs[i].Name} keeps {ratio:F4} of its lit color, expected 0.5");
            }
        }

        Assert.True(failures.Count == 0,
            $"{light}: {string.Join("; ", failures)}\n  {string.Join("\n  ", report)}");
    }
}
