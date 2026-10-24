// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

/// <summary>
/// A compute shader that reads mip-level N of a texture and writes mip-level N + 1 of the same texture, through
/// two views (how mip chains are built). Run twice in a row, so the second dispatch starts with the first one's
/// views still bound.
/// </summary>
public class TestComputeReadMipWriteNextMip : GraphicTestGameBase
{
    private const int TextureSize = 4;

    private ComputeEffectShader computeEffect;
    private Texture texture;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ComputeShaderWritesNextMip);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        texture = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, mipCount: 2, PixelFormat.R32G32B32A32_Float, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess).DisposeBy(this);

        computeEffect = new ComputeEffectShader(RenderContext.GetShared(Services))
        {
            ShaderSourceName = "MipDownsampleTestShader",
            ThreadNumbers = new Int3(TextureSize / 2, TextureSize / 2, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        computeEffect.DisposeBy(this);
    }

    private void ComputeShaderWritesNextMip()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        // Mip-level 0: texel (x, y) holds (x, y, x + y, 1)
        var mip0 = new Vector4[TextureSize * TextureSize];
        for (int y = 0; y < TextureSize; y++)
            for (int x = 0; x < TextureSize; x++)
                mip0[y * TextureSize + x] = new Vector4(x, y, x + y, 1);
        texture.SetData(commandList, mip0, arrayIndex: 0, mipLevel: 0);

        computeEffect.Parameters.Set(MipDownsampleTestShaderKeys.Input, texture.ToTextureView(ViewType.Single, 0, 0));
        computeEffect.Parameters.Set(MipDownsampleTestShaderKeys.Output, texture.ToTextureView(ViewType.Single, 0, 1));
        ((RendererBase)computeEffect).Draw(renderDrawContext);
        ((RendererBase)computeEffect).Draw(renderDrawContext);

        // Mip-level 1: texel (x, y) is the average of the 2x2 block at (2x, 2y)
        var mip1 = texture.GetData<Vector4>(commandList, arrayIndex: 0, mipLevel: 1);
        for (int y = 0; y < TextureSize / 2; y++)
            for (int x = 0; x < TextureSize / 2; x++)
                Assert.Equal(new Vector4(2 * x + 0.5f, 2 * y + 0.5f, 2 * x + 2 * y + 1, 1), mip1[y * (TextureSize / 2) + x]);
    }

    [Fact]
    public void ComputeShaderReadsMipAndWritesNextMip()
    {
        RunGameTest(new TestComputeReadMipWriteNextMip());
    }
}
