// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Engine.Tests;

/// <summary>
/// Checks that the depth buffer keeps the value of the <see cref="ClearRenderer"/> after the frame, with and without the
/// light-probe Z prepass.
/// </summary>
public class ForwardRendererPrepassDepthTest : EngineTestBase
{
    private const float ClearDepth = 0.5f;

    private bool lightProbes = true;
    private float[] depth;

    public ForwardRendererPrepassDepthTest()
    {
        GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var compositor = SceneSystem.GraphicsCompositor;
        var forwardRenderer = (ForwardRenderer)compositor.SingleView;
        forwardRenderer.Clear.Depth = ClearDepth;
        forwardRenderer.LightProbes = lightProbes;

        var gBufferStage = new RenderStage("GBuffer", "GBuffer");
        compositor.RenderStages.Add(gBufferStage);
        forwardRenderer.GBufferRenderStage = gBufferStage;
    }

    protected override void RegisterTests()
    {
        base.RegisterTests();

        // The scene is empty: nothing draws into the depth buffer
        FrameGameSystem.Draw(2, () => depth = ReadPresenterDepth());
    }

    /// <summary>
    /// Copies the presenter depth to a float color target with a sprite and reads that target back: reading a
    /// depth-stencil texture directly is not possible on every graphics API.
    /// </summary>
    private float[] ReadPresenterDepth()
    {
        var commandList = GraphicsContext.CommandList;
        var presenter = GraphicsDevice.Presenter;
        var depthBuffer = presenter.DepthStencilBuffer;
        var backBuffer = presenter.BackBuffer;

        using var target = Texture.New2D(GraphicsDevice, depthBuffer.Width, depthBuffer.Height, PixelFormat.R32_Float, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
        using var spriteBatch = new SpriteBatch(GraphicsDevice);

        commandList.ResourceBarrierTransition(depthBuffer, BarrierLayout.ShaderResource);
        commandList.SetRenderTargetAndViewport(null, target);
        spriteBatch.Begin(GraphicsContext, SpriteSortMode.Immediate, BlendStates.Opaque, GraphicsDevice.SamplerStates.PointClamp, DepthStencilStates.None);
        spriteBatch.Draw(depthBuffer, new RectangleF(0, 0, target.Width, target.Height), Color.White);
        spriteBatch.End();

        commandList.ResourceBarrierTransition(depthBuffer, BarrierLayout.DepthStencilWrite);
        commandList.SetRenderTargetAndViewport(depthBuffer, backBuffer);

        return target.GetData<float>(commandList);
    }

    private void AssertDepthIsClearValue()
    {
        RunGameTest(this);

        Assert.NotNull(depth);
        Assert.NotEmpty(depth);
        Assert.Equal(ClearDepth, depth.Min(), 1e-4f);
        Assert.Equal(ClearDepth, depth.Max(), 1e-4f);
    }

    [Fact]
    public void LightProbesKeepClearDepth() => AssertDepthIsClearValue();

    [Fact]
    public void NoLightProbesKeepClearDepth()
    {
        lightProbes = false;
        AssertDepthIsClearValue();
    }
}
