// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// A simulated device reset in the middle of the run: the content texture is reloaded, the render target and
/// the sprite batch are recreated, and the frame after the reset renders the same pixels as the frame before.
/// </summary>
public class TestDeviceReset : GraphicTestGameBase
{
    private const int ResetFrame = 3;

    private Texture texture;
    private Texture renderTarget;
    private SpriteBatch spriteBatch;
    private Color[] pixelsBeforeReset;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ResetFrame - 1, () => pixelsBeforeReset = DrawAndReadBack());
        FrameGameSystem.Update(ResetFrame, GraphicsDevice.SimulateReset);
        FrameGameSystem.Draw(ResetFrame + 3, () =>
        {
            var pixelsAfterReset = DrawAndReadBack();
            Assert.Equal(pixelsBeforeReset, pixelsAfterReset);
        });
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        // A static texture is not recreated by itself: like content-loaded ones, it needs a Reload callback
        var image = Image.New2D(64, 64, 1, PixelFormat.R8G8B8A8_UNorm).DisposeBy(this);
        var pixels = image.PixelBuffer[0].GetPixels<Color>();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color((byte)(i % 64 * 4), (byte)(i / 64 * 4), 128, 255);
        image.PixelBuffer[0].SetPixels(pixels);
        texture = Texture.New(GraphicsDevice, image).DisposeBy(this);
        texture.Reload = (resource, services) => ((Texture)resource).Recreate(image.ToDataBox());
        renderTarget = Texture.New2D(GraphicsDevice, 64, 64, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource).DisposeBy(this);
        spriteBatch = new SpriteBatch(GraphicsDevice).DisposeBy(this);
    }

    private Color[] DrawAndReadBack()
    {
        var commandList = GraphicsContext.CommandList;
        commandList.Clear(renderTarget, Color.Black);
        commandList.SetRenderTargetAndViewport(null, renderTarget);
        spriteBatch.Begin(GraphicsContext);
        spriteBatch.Draw(texture, new RectangleF(0, 0, 64, 64), Color.White);
        spriteBatch.End();
        var pixels = renderTarget.GetData<Color>(commandList);
        commandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
        return pixels;
    }

    [SkippableFact]
    public void FrameAfterResetMatchesFrameBefore()
    {
        RunGameTest(new TestDeviceReset());
    }
}
