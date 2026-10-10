// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

public class TestMultisampledRenderTarget : GraphicTestGameBase
{
    /// <summary>
    /// Resolving a cleared 4x multisampled render target copies its color into a single-sampled texture.
    /// </summary>
    [SkippableFact]
    public void CopyMultisampleResolvesIntoDestination()
    {
        PerformTest(game =>
        {
            Skip.If(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "CopyMultisample is not implemented on Vulkan.");

            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            var multisampled = Texture.New2D(device, 64, 64, MipMapCount.One, PixelFormat.R8G8B8A8_UNorm, textureData: null,
                TextureFlags.RenderTarget | TextureFlags.ShaderResource, multisampleCount: MultisampleCount.X4);
            var resolved = Texture.New2D(device, 64, 64, PixelFormat.R8G8B8A8_UNorm);

            commandList.Clear(multisampled, Color.Red);
            commandList.CopyMultisample(multisampled, 0, resolved, 0);

            var pixels = resolved.GetData<Color>(commandList);
            Assert.Equal(Color.Red, pixels[32 * 64 + 32]);
        });
    }
}
