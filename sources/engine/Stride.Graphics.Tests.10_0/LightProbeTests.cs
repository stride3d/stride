// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace Stride.Graphics.Tests;

/// <summary>
/// Renders a scene lit by light probes next to a shadow casting directional light. That combination
/// makes several shaders declare the PerView.Lighting and PerView.LightProbes logical groups, which is
/// what broke the lighting constants and descriptors in #3323.
/// </summary>
public class LightProbeTests : GameTestBase
{
    public LightProbeTests()
    {
        // Light probes read their data from buffers, which needs more than the 9_3 the other tests use
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_10_0;

        // Fixed time step, so the two captured frames only differ if rendering is unstable
        IsFixedTimeStep = true;
        ForceOneUpdatePerDraw = true;
        IsDrawDesynchronized = false;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        // The default compositor: its mixin order is what put PerView.LightProbes between the two halves
        // of PerView.Lighting, which is the ordering this test guards
        var compositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");
        SceneSystem.GraphicsCompositor = compositor;

        var scene = new Scene();

        var camera = new Entity("Camera") { new CameraComponent { Slot = compositor.Cameras[0].ToSlotId() } };
        camera.Transform.Position = new Vector3(0.0f, 3.5f, 9.0f);
        camera.Transform.Rotation = Quaternion.RotationYawPitchRoll(0.0f, MathUtil.DegreesToRadians(-15.0f), 0.0f);
        scene.Entities.Add(camera);

        var material = Material.New(GraphicsDevice, new MaterialDescriptor
        {
            Attributes =
            {
                Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                DiffuseModel = new MaterialDiffuseLambertModelFeature(),
            },
        });

        // A sphere over a ground plane: the ground shows both the light probe gradient and the sphere shadow
        var sphereModel = new ProceduralModelDescriptor(
            new SphereProceduralModel { Radius = 1.0f, MaterialInstance = { Material = material } }).GenerateModel(Services);
        var sphere = new Entity("Sphere") { new ModelComponent { Model = sphereModel } };
        sphere.Transform.Position = new Vector3(0.0f, 1.0f, 0.0f);
        scene.Entities.Add(sphere);

        var groundModel = new ProceduralModelDescriptor(
            new PlaneProceduralModel { Size = new Vector2(10.0f), MaterialInstance = { Material = material } }).GenerateModel(Services);
        scene.Entities.Add(new Entity("Ground") { new ModelComponent { Model = groundModel } });

        var light = new Entity("Light")
        {
            new LightComponent
            {
                // Dim, so that the light probes drive most of the color and a wrong probe stands out
                Intensity = 0.35f,
                Type = new LightDirectional
                {
                    Color = new ColorRgbProvider(Color.White),
                    Shadow = { Enabled = true },
                },
            },
        };
        light.Transform.Rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(30.0f), MathUtil.DegreesToRadians(-60.0f), 0.0f);
        scene.Entities.Add(light);

        // Four probes forming a tetrahedron around the sphere, each a different color so that a wrong
        // interpolation is visible
        AddLightProbe(scene, new Vector3(0.0f, 6.0f, 0.0f), new Color3(1.0f, 0.2f, 0.2f));
        AddLightProbe(scene, new Vector3(-5.0f, -1.0f, 3.0f), new Color3(0.2f, 1.0f, 0.2f));
        AddLightProbe(scene, new Vector3(5.0f, -1.0f, 3.0f), new Color3(0.2f, 0.2f, 1.0f));
        AddLightProbe(scene, new Vector3(0.0f, -1.0f, -5.0f), new Color3(1.0f, 1.0f, 0.2f));

        SceneSystem.SceneInstance = new SceneInstance(Services, scene);
    }

    private static void AddLightProbe(Scene scene, Vector3 position, Color3 color)
    {
        // Only the constant band, so the probe lights its surroundings with a flat color. Kept dim
        // enough that the ground does not clip to white, where a wrong probe color would go unseen
        var coefficients = new List<Color3>(
            new Color3[LightProbeGenerator.LambertHamonicOrder * LightProbeGenerator.LambertHamonicOrder]);
        coefficients[0] = color * 0.6f;

        var entity = new Entity("LightProbe") { new LightProbeComponent { Coefficients = coefficients } };
        entity.Transform.Position = position;
        scene.Entities.Add(entity);
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        // Two frames: the constants were left with recycled buffer content, which made the scene
        // flicker a different color every frame
        FrameGameSystem.Draw(() => { }).TakeScreenshot();
        FrameGameSystem.Draw(() => { }).TakeScreenshot();
    }

    [Fact]
    public void RunTestGame()
    {
        RunGameTest(new LightProbeTests());
    }
}
