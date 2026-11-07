// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.GeometricPrimitives;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

public class DeformationModeTests : GameTestBase
{
    /// <summary>
    /// A volumetric body squashed on the floor and a cloth bent in the middle of its swing, frozen, then drawn with each deformation mode:
    /// both images must match, shadows and normal mapped lighting included.
    /// </summary>
    [Fact]
    public static void GpuDeformationLooksLikeCpuDeformation()
    {
        var folder = Path.Combine(Path.GetTempPath(), "Stride.BepuPhysics.Soft.Tests");
        Directory.CreateDirectory(folder);
        var cpuPath = Path.Combine(folder, "deformation-cpu.png");
        var gpuPath = Path.Combine(folder, "deformation-gpu.png");

        var game = new RenderGame();
        Exception? failure = null;
        ImageTester.ComparisonStats stats = default;
        game.Script.AddTask(async () =>
        {
            try
            {
                var bodies = game.SetUpScene();
                var simulation = bodies[0].Simulation!;
                for (int step = 0; step < 50; step++)
                    await simulation.AfterUpdate();
                simulation.Enabled = false;

                using (var cpu = await game.Capture(bodies, SoftBodyDeformationMode.Cpu))
                using (var stream = File.Create(cpuPath))
                    cpu.Save(stream, ImageFileType.Png);
                var models = bodies.Select(b => b.Entity.Get<ModelComponent>()!.Model).ToArray();
                using var gpu = await game.Capture(bodies, SoftBodyDeformationMode.Gpu);
                // The mode switches in place: what follows the model (node links, skeleton bodies) is left alone
                Assert.Equal(models, bodies.Select(b => b.Entity.Get<ModelComponent>()!.Model));
                using (var stream = File.Create(gpuPath))
                    gpu.Save(stream, ImageFileType.Png);

                ImageTester.CompareImage(gpu, cpuPath, out stats);
            }
            catch (Exception e)
            {
                failure = e;
            }
            game.Exit();
        });
        RunGameTest(game);
        if (failure is not null)
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Throw(failure);

        // Both modes run the same math, in another order: rounding can move an edge or a shadow texel by one pixel, nothing more
        var message = $"{stats}, see {cpuPath} and {gpuPath}";
        Assert.True(stats.TotalPixels > 0, message);
        Assert.True(stats.PixelsAtDiff3Plus <= stats.TotalPixels / 1000, message);
        Assert.True(stats.PSNR > 45.0, message);
    }

    private sealed class RenderGame : GameTestBase
    {
        private Image? _capture;
        private bool _captureRequested;

        public RenderGame()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
            GraphicsDeviceManager.PreferredBackBufferWidth = 1280;
            GraphicsDeviceManager.PreferredBackBufferHeight = 720;
            IsFixedTimeStep = true;
            ForceOneUpdatePerDraw = true;
            IsDrawDesynchronized = false;
            TargetElapsedTime = TimeSpan.FromTicks(TimeSpan.TicksPerSecond / 60);
        }

        public SoftBodyComponent[] SetUpScene()
        {
            ScreenShotAutomationEnabled = false;
            var compositor = GraphicsCompositorHelper.CreateDefault(true, clearColor: new Color4(0.6f, 0.7f, 0.85f, 1f), graphicsProfile: GraphicsProfile.Level_11_0);
            SceneSystem.GraphicsCompositor = compositor;
            var root = SceneSystem.SceneInstance.RootScene.Entities;
            root.Clear();

            var sun = new Entity { new LightComponent { Type = new LightDirectional { Shadow = { Enabled = true, Size = LightShadowMapSize.Large } }, Intensity = 3f } };
            sun.Transform.Rotation = Quaternion.RotationYawPitchRoll(0.6f, -0.9f, 0f);
            root.Add(sun);
            root.Add(new Entity { new LightComponent { Type = new LightAmbient(), Intensity = 0.3f } });
            var camera = new Entity { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId(), NearClipPlane = 0.05f, FarClipPlane = 100f } };
            camera.Transform.Position = new Vector3(0f, 1.2f, 2.6f);
            camera.Transform.Rotation = Quaternion.RotationX(-0.3f);
            root.Add(camera);

            var floor = new Entity
            {
                new ModelComponent(Model(new PlaneProceduralModel { Size = new Vector2(8f) }, Material(new Color(150, 146, 140), normalMapped: false))),
                new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(8f, 1f, 8f), PositionLocal = new Vector3(0f, -0.5f, 0f) } } } },
            };
            root.Add(floor);

            // Volumetric: dropped onto the floor, it squashes and wobbles
            var jelly = new VolumetricSoftBodyComponent { Resolution = 4 };
            var jellyEntity = new Entity { new ModelComponent(Model(new SphereProceduralModel { Radius = 0.35f, Tessellation = 24 }, Material(new Color(255, 111, 89), normalMapped: true))), jelly };
            jellyEntity.Transform.Position = new Vector3(-0.55f, 0.8f, 0f);
            root.Add(jellyEntity);

            // Cloth: pinned along one edge, it swings down and bends
            var edge = new BoundingBox(new Vector3(-0.51f, -1f, -1f), new Vector3(-0.49f, 1f, 1f));
            var cloth = new ClothComponent { Pins = { new SoftBodyPin { Region = edge } } };
            var plane = new PlaneProceduralModel { Size = new Vector2(1f), Tessellation = new Int2(16), Normal = NormalDirection.UpY, GenerateBackFace = true };
            var clothEntity = new Entity { new ModelComponent(Model(plane, Material(new Color(61, 125, 216), normalMapped: true))), cloth };
            clothEntity.Transform.Position = new Vector3(0.75f, 1.3f, -0.3f);
            root.Add(clothEntity);

            return [jelly, cloth];
        }

        /// <summary> Draws the frozen bodies with <paramref name="mode"/> once every effect is compiled, and reads the back buffer </summary>
        public async Task<Image> Capture(IReadOnlyList<SoftBodyComponent> bodies, SoftBodyDeformationMode mode)
        {
            foreach (var body in bodies)
                body.DeformationMode = mode;
            for (int frame = 0; frame < 600 && (frame < 4 || IsCompiling()); frame++)
                await Script.NextFrame();
            Assert.False(IsCompiling(), "Effects still compiling");
            Assert.All(bodies, body => Assert.Equal(mode, body.RenderModel?.Mode));

            _captureRequested = true;
            while (_capture is null)
                await Script.NextFrame();
            var image = _capture;
            _capture = null;
            return image;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (_captureRequested)
            {
                _captureRequested = false;
                _capture = GraphicsDevice.Presenter.BackBuffer.GetDataAsImage(GraphicsContext.CommandList);
            }
        }

        private bool IsCompiling()
        {
            foreach (var feature in SceneSystem.GraphicsCompositor.RenderFeatures.OfType<MeshRenderFeature>())
            {
                var renderEffects = feature.RenderData.GetData(feature.RenderEffectKey);
                var slotCount = feature.EffectPermutationSlotCount;
                foreach (var renderObject in feature.RenderObjects)
                {
                    for (int slot = 0; slot < slotCount; slot++)
                    {
                        var renderEffect = renderEffects[renderObject.StaticObjectNode * slotCount + slot];
                        if (renderEffect is { PendingEffect: not null } || renderEffect?.State == RenderEffectState.Compiling)
                            return true;
                    }
                }
            }
            return false;
        }

        private Model Model(PrimitiveProceduralModelBase shape, Material material)
        {
            var model = new ProceduralModelDescriptor(shape).GenerateModel(Services);
            model.Materials.Clear();
            model.Materials.Add(new MaterialInstance(material));
            return model;
        }

        /// <summary> A glossy material, with a normal map of bumps for the tangents to matter </summary>
        private Material Material(Color color, bool normalMapped)
        {
            var descriptor = new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(color.ToColor4())),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    MicroSurface = new MaterialGlossinessMapFeature(new ComputeFloat(0.6f)),
                    Specular = new MaterialMetalnessMapFeature(new ComputeFloat(0f)),
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature { Environment = new MaterialSpecularMicrofacetEnvironmentGGXPolynomial() },
                },
            };
            if (normalMapped)
                descriptor.Attributes.Surface = new MaterialNormalMapFeature(new ComputeTextureColor(Bumps()));
            return Stride.Rendering.Material.New(GraphicsDevice, descriptor);
        }

        private Texture Bumps()
        {
            const int size = 64;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Slopes of sin(u) sin(v) bumps, four per side
                    float u = x * MathUtil.TwoPi * 4f / size, v = y * MathUtil.TwoPi * 4f / size;
                    var normal = Vector3.Normalize(new Vector3(-0.5f * MathF.Cos(u) * MathF.Sin(v), -0.5f * MathF.Sin(u) * MathF.Cos(v), 1f));
                    pixels[y * size + x] = new Color(normal * 0.5f + new Vector3(0.5f));
                }
            }
            return Texture.New2D(GraphicsDevice, size, size, PixelFormat.R8G8B8A8_UNorm, pixels);
        }
    }
}
