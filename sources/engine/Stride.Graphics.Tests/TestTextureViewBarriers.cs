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

    /// <summary>
    /// A barrier on a mip band view (one mip-level of the array slices from its first) or an array band view (one array
    /// slice, the mip-levels from its first) moves only the sub-resources the view covers.
    /// </summary>
    [SkippableFact]
    public void BandViewsTransitionOnlyTheirRange()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D12, "Per-subresource view barriers are implemented on Direct3D 12.");

            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 3, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, arraySize: 2);
            using var mipBand = texture.ToTextureView(ViewType.MipBand, arraySlice: 0, mipLevel: 1);
            using var arrayBand = texture.ToTextureView(ViewType.ArrayBand, arraySlice: 1, mipLevel: 1);

            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(mipBand, BarrierLayout.CopyDest);
            AssertLayouts(texture, BarrierLayout.CopyDest, (0, 1), (1, 1));

            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(arrayBand, BarrierLayout.CopyDest);
            AssertLayouts(texture, BarrierLayout.CopyDest, (1, 1), (1, 2));
        });

        // The listed (array slice, mip-level) sub-resources are in `layout`, the others in ShaderResource
        static void AssertLayouts(Texture texture, BarrierLayout layout, params (int ArraySlice, int MipLevel)[] moved)
        {
            for (int arraySlice = 0; arraySlice < texture.ArraySize; arraySlice++)
            {
                for (int mipLevel = 0; mipLevel < texture.MipLevelCount; mipLevel++)
                {
                    var expected = System.Array.IndexOf(moved, (arraySlice, mipLevel)) >= 0 ? layout : BarrierLayout.ShaderResource;
                    Assert.Equal(expected, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(arraySlice, mipLevel)));
                }
            }
        }
    }

    /// <summary>
    /// A barrier on a view that covers the whole texture is a single whole-texture barrier, and a whole-texture barrier
    /// after a mip chain moved every mip-level back to one layout is a single barrier too.
    /// </summary>
    [SkippableFact]
    public void WholeTextureViewAndChainEndAreOneBarrier()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D12, "Counts Direct3D 12 barriers.");

#if STRIDE_GRAPHICS_API_DIRECT3D12
            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 3, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var full = texture.ToTextureView(ViewType.Full, 0, 0);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);

            int count = commandList.PendingBarrierCount;
            commandList.ResourceBarrierTransition(full, BarrierLayout.CopySource);
            Assert.Equal(1, commandList.PendingBarrierCount - count);
            Assert.False(texture.LayoutTracker.HasPerSubresourceTracking);

            // A mip chain: each mip-level written, then read
            for (int mipLevel = 0; mipLevel < texture.MipLevelCount; mipLevel++)
            {
                using var mip = texture.ToTextureView(ViewType.Single, 0, mipLevel);
                commandList.ResourceBarrierTransition(mip, BarrierLayout.RenderTarget);
                commandList.ResourceBarrierTransition(mip, BarrierLayout.ShaderResource);
            }
            Assert.False(texture.LayoutTracker.HasPerSubresourceTracking);

            count = commandList.PendingBarrierCount;
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopySource);
            Assert.Equal(1, commandList.PendingBarrierCount - count);
#endif
        });
    }

    /// <summary>
    /// A barrier on a single-slice view of a depth-stencil texture moves the whole texture, depth and stencil planes.
    /// </summary>
    [SkippableFact]
    public void SingleSliceDepthStencilViewTransitionsTheWholeTexture()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D12, "Per-subresource view barriers are implemented on Direct3D 12.");

            var commandList = game.GraphicsContext.CommandList;
            var texture = Texture.New2D(game.GraphicsDevice, 16, 16, PixelFormat.D24_UNorm_S8_UInt, TextureFlags.DepthStencil, arraySize: 2);
            var slice1 = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, ArraySlice = 1, Format = texture.Format, Flags = TextureFlags.DepthStencil });

            commandList.ResourceBarrierTransition(texture, BarrierLayout.DepthStencilRead);
            commandList.ResourceBarrierTransition(slice1, BarrierLayout.DepthStencilWrite);

            Assert.Equal(BarrierLayout.DepthStencilWrite, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(0, 0)));
            Assert.Equal(BarrierLayout.DepthStencilWrite, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(1, 0)));
        });
    }
}
