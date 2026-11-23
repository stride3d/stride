// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.ProceduralModels;

namespace Stride.Engine.Tests;

/// <summary>
/// Checks that the <see cref="ForwardRenderer"/> builds its pipelines for the format of the depth buffer it draws into.
/// </summary>
public class ForwardRendererDepthFormatTest : EngineTestBase
{
    private PixelFormat presenterDepthFormat;
    private PixelFormat opaqueStageDepthFormat;

    public ForwardRendererDepthFormatTest()
    {
        GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
        GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D32_Float_S8X24_UInt;
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var material = Content.Load<Material>("NoTessellation");
        var sphere = new SphereProceduralModel { Radius = 50, Tessellation = 5, MaterialInstance = { Material = material } };
        var entity = new Entity("Sphere") { new ModelComponent(new ProceduralModelDescriptor(sphere).GenerateModel(Services)) };
        Scene.Entities.Add(entity);

        var camera = new TestCamera(SceneSystem.GraphicsCompositor);
        CameraComponent = camera.Camera;
        Script.Add(camera);
        camera.Position = new Vector3(50, 90, 160);
        camera.SetTarget(entity, true);
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(3, () =>
        {
            var forwardRenderer = (ForwardRenderer)SceneSystem.GraphicsCompositor.SingleView;
            presenterDepthFormat = GraphicsDevice.Presenter.DepthStencilBuffer.ViewFormat;
            opaqueStageDepthFormat = forwardRenderer.OpaqueRenderStage.Output.DepthStencilFormat;
        });
    }

    [Fact]
    public void PipelinesUseTheBoundDepthFormat()
    {
        RunGameTest(this);

        Assert.Equal(PixelFormat.D32_Float_S8X24_UInt, presenterDepthFormat);
        Assert.Equal(presenterDepthFormat, opaqueStageDepthFormat);
    }
}
