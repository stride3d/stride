// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests;

public class TestTextureViewBarriers : GraphicTestGameBase
{
    /// <summary>
    /// A barrier on a single-mip view moves that mip only, so a mip chain can read one mip while it writes the next.
    /// </summary>
    [SkippableFact]
    public void SingleMipViewTransitionsOnlyItsMip()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D12, "Per-subresource view barriers are implemented on Direct3D 12.");

            var commandList = game.GraphicsContext.CommandList;
            var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 2, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            var mip0 = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, MipLevel = 0, Format = texture.Format, Flags = TextureFlags.ShaderResource });
            var mip1 = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, MipLevel = 1, Format = texture.Format, Flags = TextureFlags.RenderTarget });

            commandList.ResourceBarrierTransition(mip0, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(mip1, BarrierLayout.RenderTarget);

            Assert.Equal(BarrierLayout.ShaderResource, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(0, 0)));
            Assert.Equal(BarrierLayout.RenderTarget, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(0, 1)));
        });
    }
}
