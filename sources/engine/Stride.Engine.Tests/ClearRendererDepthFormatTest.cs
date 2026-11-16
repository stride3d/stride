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
/// Checks that the <see cref="ClearRenderer"/> clears a depth buffer that has no stencil.
/// </summary>
public class ClearRendererDepthFormatTest : EngineTestBase
{
    private const float ClearDepth = 0.5f;

    private float[] depth;

    public ClearRendererDepthFormatTest()
    {
        GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D32_Float;
        GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
        GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        var forwardRenderer = (ForwardRenderer)SceneSystem.GraphicsCompositor.SingleView;
        forwardRenderer.Clear.Depth = ClearDepth;
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

    [Fact]
    public void ClearsDepthBufferWithoutStencil()
    {
        RunGameTest(this);

        Assert.NotNull(depth);
        Assert.NotEmpty(depth);
        Assert.Equal(ClearDepth, depth.Min(), 1e-4f);
        Assert.Equal(ClearDepth, depth.Max(), 1e-4f);
    }
}
