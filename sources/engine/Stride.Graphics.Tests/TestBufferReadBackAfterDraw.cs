// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests
{
    public class TestBufferReadBackAfterDraw : GameTestBase
    {
        /// <summary>
        ///   Reading a Buffer back right after a draw copies it while the draw's render pass is still active.
        ///   The copy has to end the render pass first, as Vulkan forbids copies inside one.
        /// </summary>
        [SkippableFact]
        public void BufferReadsBackAfterDraw()
        {
            PerformTest(
                game =>
                {
                    var device = game.GraphicsDevice;
                    var commandList = game.GraphicsContext.CommandList;

                    using var buffer = Buffer.Vertex.New(device, [0xC0FFEEu]);
                    using var source = Texture.New2D(device, 8, 8, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
                    using var target = Texture.New2D(device, 8, 8, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);

                    commandList.SetRenderTargetAndViewport(null, target);
                    device.PrimitiveQuad.Draw(game.GraphicsContext, source);

                    Assert.Equal(0xC0FFEEu, buffer.GetData<uint>(commandList)[0]);
                });
        }
    }
}
