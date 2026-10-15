// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace Stride.Graphics.Tests;

/// <summary>
/// Renders a cel shaded quad and reads its color back, to check that the cel shading ramp keeps the color of
/// what it ramps: the environment light's color for diffuse, the specular color for specular.
/// </summary>
/// <remarks>
///   The ramp takes a single intensity. It used to get a color and keep its red channel only, which turned a
///   colored light or a colored specular into a gray one, or into black when the red channel was dark.
/// </remarks>
public class CelShadingTests : GameTestBase
{
    public enum Case
    {
        /// <summary>A white cel diffuse quad under a blue ambient light.</summary>
        EnvironmentDiffuse,
        /// <summary>A cel specular metal with an orange specular color under a white directional light.</summary>
        DirectSpecular,
    }

    private const int Size = 32;

    private static readonly Color3 AmbientColor = new(0.2f, 0.4f, 1.0f);
    private static readonly Color3 MetalColor = new(1.0f, 0.5f, 0.2f);

    private readonly Case testCase;
    private Texture renderTarget;
    private Half4[] pixels;

    public CelShadingTests() : this(Case.EnvironmentDiffuse)
    {
    }

    private CelShadingTests(Case testCase)
    {
        this.testCase = testCase;
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        // A float target, so values are linear and nothing clips
        renderTarget = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R16G16B16A16_Float, TextureFlags.RenderTarget | TextureFlags.ShaderResource);

        var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, clearColor: Color.Black);
        compositor.Game = new RenderTextureSceneRenderer { RenderTexture = renderTarget, Child = compositor.Game };
        SceneSystem.GraphicsCompositor = compositor;

        var scene = new Scene();

        // Orthographic and far away, looking along the quad normal: view, light and normal are the same on
        // every pixel, so the quad is flat
        var camera = new Entity("Camera")
        {
            new CameraComponent
            {
                Projection = CameraProjectionMode.Orthographic,
                OrthographicSize = 1.0f,
                UseCustomAspectRatio = true,
                AspectRatio = 1.0f,
                FarClipPlane = 2000.0f,
                Slot = compositor.Cameras[0].ToSlotId(),
            },
        };
        camera.Transform.Position = new Vector3(0, 0, 1000);
        scene.Entities.Add(camera);

        var material = testCase switch
        {
            Case.EnvironmentDiffuse => Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                    DiffuseModel = new MaterialDiffuseCelShadingModelFeature(),
                },
            }),
            // With the content manager, the specular model's lookup table is loaded, not left an empty texture
            Case.DirectSpecular => Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(new Color4(MetalColor, 1.0f))),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(1.0f)),
                    SpecularModel = new MaterialSpecularCelShadingModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.5f)),
                },
            }, Content),
            _ => throw new ArgumentOutOfRangeException(),
        };

        var model = new ProceduralModelDescriptor(
            new PlaneProceduralModel { Size = new Vector2(2.0f), MaterialInstance = { Material = material } }).GenerateModel(Services);
        var quad = new Entity("Quad") { new ModelComponent { Model = model } };
        // The plane is authored facing +Y, turn it to face the camera
        quad.Transform.Rotation = Quaternion.RotationX(MathUtil.PiOverTwo);
        scene.Entities.Add(quad);

        scene.Entities.Add(testCase == Case.EnvironmentDiffuse
            // Bright enough that its luminance is past the default ramp's first step (0.2), its red channel is not
            ? new Entity("Light") { new LightComponent { Type = new LightAmbient { Color = new ColorRgbProvider(AmbientColor) }, Intensity = 2.0f } }
            // No rotation: the light shines along -Z, towards the quad
            : new Entity("Light") { new LightComponent { Type = new LightDirectional { Color = new ColorRgbProvider(Color.White) } } });

        SceneSystem.SceneInstance = new SceneInstance(Services, scene);
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        // Gives the game one frame to render the quad, read back in Draw
        FrameGameSystem.Draw(() => { });
    }

    protected override void Draw(GameTime gameTime)
    {
        base.Draw(gameTime);

        pixels = renderTarget.GetData<Half4>(GraphicsContext.CommandList);
    }

    [Theory]
    [InlineData(Case.EnvironmentDiffuse)]
    [InlineData(Case.DirectSpecular)]
    public void RampKeepsTheColor(Case testCase)
    {
        var game = new CelShadingTests(testCase);
        RunGameTest(game);

        Assert.NotNull(game.pixels);

        var pixel = game.pixels[(Size / 2) * Size + Size / 2];
        var color = new Color3((float)pixel.X, (float)pixel.Y, (float)pixel.Z);

        // Authored colors are sRGB, lighting is linear. White albedo and a white light leave only the other
        // color's hue
        var expected = (testCase == Case.EnvironmentDiffuse ? AmbientColor : MetalColor).ToLinear();

        var sum = color.R + color.G + color.B;
        Assert.True(sum > 1e-3f, $"{testCase}: the quad is black ({color})");

        var hue = color * (1.0f / sum);
        var expectedHue = expected * (1.0f / (expected.R + expected.G + expected.B));
        Assert.True(new[] { hue.R - expectedHue.R, hue.G - expectedHue.G, hue.B - expectedHue.B }.All(d => Math.Abs(d) < 0.01f),
            $"{testCase}: got {color} (hue {hue}), expected the hue of {expected} ({expectedHue})");
    }
}
