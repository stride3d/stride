// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering;
using Stride.Rendering.ComputeEffect;

namespace Stride.Graphics.Tests;

public class TestUnorderedAccessOnlyTexture : GraphicTestGameBase
{
    private const int TextureSize = 4;

    private ComputeEffectShader computeEffect;
    private Texture outputTexture;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(ComputeShaderWritesToUnorderedAccessOnlyTexture);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        // Deliberately no ShaderResource flag: this is the case where the Vulkan backend used to
        // produce no image view and silently bind the empty texture instead.
        outputTexture = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, PixelFormat.R32G32B32A32_Float, TextureFlags.UnorderedAccess).DisposeBy(this);

        computeEffect = new ComputeEffectShader(RenderContext.GetShared(Services))
        {
            ShaderSourceName = "UnorderedAccessOnlyTextureShader",
            ThreadNumbers = new Int3(TextureSize, TextureSize, 1),
            ThreadGroupCounts = new Int3(1, 1, 1),
        };
        computeEffect.DisposeBy(this);
    }

    private void ComputeShaderWritesToUnorderedAccessOnlyTexture()
    {
        var commandList = GraphicsContext.CommandList;
        var renderDrawContext = new RenderDrawContext(Services, RenderContext.GetShared(Services), GraphicsContext);

        commandList.ResourceBarrierTransition(outputTexture, BarrierLayout.UnorderedAccess);

        computeEffect.Parameters.Set(UnorderedAccessOnlyTextureShaderKeys.Output, outputTexture);
        ((RendererBase)computeEffect).Draw(renderDrawContext);

        var pixels = outputTexture.GetData<Vector4>(commandList);

        Assert.Equal(TextureSize * TextureSize, pixels.Length);
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                Assert.Equal(new Vector4(x, y, 0, 1), pixels[(y * TextureSize) + x]);
            }
        }
    }

    [Fact]
    public void ComputeShaderCanWriteToUnorderedAccessOnlyTexture()
    {
        RunGameTest(new TestUnorderedAccessOnlyTexture());
    }
}
