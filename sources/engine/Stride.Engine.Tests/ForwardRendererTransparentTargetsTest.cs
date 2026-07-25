// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Engine.Tests
{
    public class ForwardRendererTransparentTargetsTest : GameTestBase
    {
        /// <summary>
        /// Verifies the ForwardRenderer binds only as many render targets as the transparent stage declares it
        /// outputs, dropping surplus opaque MRT targets (e.g. those Local Reflections adds). See #3251.
        /// </summary>
        [Fact]
        public void BindsOnlyTransparentStageDeclaredTargets()
        {
            PerformDrawTest((game, context) =>
            {
                var device = game.GraphicsDevice;
                var commandList = context.CommandList;

                var transparentStage = new RenderStage("Transparent", "Main")
                {
                    Output = new RenderOutputDescription(PixelFormat.R8G8B8A8_UNorm, PixelFormat.D24_UNorm_S8_UInt),
                };
                var forwardRenderer = new ForwardRenderer { TransparentRenderStage = transparentStage };

                // Formats are irrelevant here (only the target count matters); use a widely supported one.
                var color = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
                var normal = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
                var specular = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
                var depth = Texture.New2D(device, 16, 16, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil);

                // Opaque stage left 3 color targets + depth bound, as SSLR would.
                commandList.SetRenderTargets(depth, color, normal, specular);
                Assert.Equal(3, commandList.RenderTargetCount);

                forwardRenderer.SetTransparentStageRenderTargets(context);

                Assert.Equal(1, commandList.RenderTargetCount);
                Assert.Equal(color, commandList.RenderTargets[0]);
                Assert.Equal(depth, commandList.DepthStencilBuffer);

                // Already matching the declared count: no change.
                forwardRenderer.SetTransparentStageRenderTargets(context);
                Assert.Equal(1, commandList.RenderTargetCount);

                color.Dispose();
                normal.Dispose();
                specular.Dispose();
                depth.Dispose();
            }, takeSnapshot: false);
        }
    }
}
