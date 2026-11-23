// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// Covers a render target written, moved to <see cref="BarrierLayout.Common"/>, then sampled by a draw that
/// makes no transition of its own.
/// </summary>
/// <remarks>
/// Run with STRIDE_VULKAN_SYNC_VALIDATION=1 to have Vulkan report a missing dependency between the write and
/// the sampling, or a descriptor layout that is not the one the texture is in.
/// </remarks>
public class TestTextureCommonBarrier : GraphicTestGameBase
{
    private const int TextureSize = 4;

    private Texture source;
    private Texture target;

    protected override void RegisterTests()
    {
        base.RegisterTests();

        FrameGameSystem.Draw(SampledTextureInCommonLayoutHoldsItsContent);
    }

    protected override async Task LoadContent()
    {
        await base.LoadContent();

        source = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, PixelFormat.R32G32B32A32_Float, TextureFlags.RenderTarget | TextureFlags.ShaderResource).DisposeBy(this);
        target = Texture.New2D(GraphicsDevice, TextureSize, TextureSize, PixelFormat.R32G32B32A32_Float, TextureFlags.RenderTarget).DisposeBy(this);
    }

    private void SampledTextureInCommonLayoutHoldsItsContent()
    {
        var commandList = GraphicsContext.CommandList;
        var color = new Color4(0.25f, 0.5f, 0.75f, 1f);

        commandList.SetRenderTargetAndViewport(null, source);
        commandList.Clear(source, color);

        commandList.ResourceBarrierTransition(source, BarrierLayout.Common);

        commandList.SetRenderTargetAndViewport(null, target);
        GraphicsContext.DrawTexture(source, GraphicsDevice.SamplerStates.PointClamp);

        foreach (var pixel in target.GetData<Vector4>(commandList))
        {
            Assert.Equal(new Vector4(color.R, color.G, color.B, color.A), pixel);
        }
    }

    [SkippableFact]
    public void TextureInCommonLayoutCanBeSampled()
    {
        RunGameTest(new TestTextureCommonBarrier());
    }
}
