// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.Images;

namespace Stride.Graphics.Tests;

/// <summary>
/// An image effect that reads mip-level N - 1 of a texture and renders mip-level N of the same texture, through two
/// views, for each mip-level (how the skybox and video mip chains are built).
/// </summary>
public class TestImageScalerReadMipWriteNextMip : GraphicTestGameBase
{
    private const int TextureSize = 4;

    private ImageScaler scaler;
    private Texture texture;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ScalerRendersEachMipFromThePreviousOne);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        texture = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, mipCount: 3, PixelFormat.R16G16B16A16_Float, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
        scaler = new ImageScaler(SamplingPattern.Linear).DisposeBy(this);
    }

    private void ScalerRendersEachMipFromThePreviousOne()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        // Mip-level 0: texel (x, y) holds (x, y, x + y, 1)
        var mip0 = new Half4[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
                mip0[y * TextureSize + x] = new Half4((Half) x, (Half) y, (Half) (x + y), (Half) 1);
        texture.SetData(commandList, mip0, arrayIndex: 0, mipLevel: 0);

        // A single bilinear tap at an exact 2x downscale averages the 2x2 source texels
        for (int mipLevel = 1; mipLevel < texture.MipLevelCount; mipLevel++)
        {
            using var source = texture.ToTextureView(ViewType.Single, 0, mipLevel - 1);
            using var target = texture.ToTextureView(ViewType.Single, 0, mipLevel);
            scaler.SetInput(0, source);
            scaler.SetOutput(target);
            scaler.Draw(renderDrawContext);
        }
        commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);

        // Mip-level 1: texel (x, y) is the average of the 2x2 block at (2x, 2y); mip-level 2 is the average of all
        var mip1 = texture.GetData<Half4>(commandList, arrayIndex: 0, mipLevel: 1);
        for (int y = 0; y < TextureSize / 2; y++)
            for (int x = 0; x < TextureSize / 2; x++)
                Assert.Equal(new Vector4(2 * x + 0.5f, 2 * y + 0.5f, 2 * x + 2 * y + 1, 1), (Vector4) mip1[y * (TextureSize / 2) + x]);

        var mip2 = texture.GetData<Half4>(commandList, arrayIndex: 0, mipLevel: 2);
        Assert.Equal(new Vector4(1.5f, 1.5f, 3, 1), (Vector4) mip2[0]);
    }

    [Fact]
    public void ImageScalerReadsMipAndRendersNextMip()
    {
        RunGameTest(new TestImageScalerReadMipWriteNextMip());
    }
}
