// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11
using Silk.NET.Direct3D11;
using Stride.Shaders;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestUnorderedAccessViewUnbinding : GraphicTestGameBase
{
    /// <summary>
    /// Binding a texture as a shader resource unbinds a compute UAV of one of its views,
    /// otherwise Direct3D 11 nulls the shader resource view.
    /// </summary>
    [Fact]
    public void ComputeUavOfAViewIsUnboundWhenItsTextureIsReadAsSrv()
    {
        PerformTest(game =>
        {
            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 2, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            using var mipView = texture.ToTextureView(new TextureViewDescription
            {
                Type = ViewType.Single,
                MipLevel = 1,
                Format = texture.Format,
                Flags = TextureFlags.UnorderedAccess,
            });

            commandList.SetUnorderedAccessView(ShaderStage.Compute, 0, mipView, -1);

            // Same order as ResourceBinder before it binds a shader resource view
            commandList.UnsetUnorderedAccessView(texture);
            commandList.SetShaderResourceView(ShaderStage.Pixel, 0, texture);

            var boundUav = GetBoundComputeUav(commandList, 0);
            commandList.SetShaderResourceView(ShaderStage.Pixel, 0, null);
            commandList.SetUnorderedAccessView(ShaderStage.Compute, 0, null, -1);

            Assert.True(boundUav == 0, "The compute UAV of the texture's view is still bound.");
        });
    }

    /// <summary>
    /// Reading mip-level 0 of a texture keeps bound a compute UAV of its mip-level 1: they are different sub-resources.
    /// </summary>
    [Fact]
    public void ComputeUavOfAnotherMipStaysBoundWhenAMipIsReadAsSrv()
    {
        PerformTest(game =>
        {
            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 2, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            using var mip0View = texture.ToTextureView(ViewType.Single, 0, 0);
            using var mip1View = texture.ToTextureView(new TextureViewDescription
            {
                Type = ViewType.Single,
                MipLevel = 1,
                Format = texture.Format,
                Flags = TextureFlags.UnorderedAccess,
            });

            commandList.SetUnorderedAccessView(ShaderStage.Compute, 0, mip1View, -1);

            commandList.UnsetUnorderedAccessView(mip0View);
            commandList.SetShaderResourceView(ShaderStage.Compute, 0, mip0View);

            var boundUav = GetBoundComputeUav(commandList, 0);
            commandList.SetShaderResourceView(ShaderStage.Compute, 0, null);
            commandList.SetUnorderedAccessView(ShaderStage.Compute, 0, null, -1);

            Assert.True(boundUav != 0, "The compute UAV of another mip-level was unbound.");
        });
    }

    private static unsafe nint GetBoundComputeUav(CommandList commandList, uint slot)
    {
        ID3D11UnorderedAccessView* uav = null;
        commandList.NativeDeviceContext.CSGetUnorderedAccessViews(slot, 1, &uav);
        if (uav != null)
            uav->Release();
        return (nint) uav;
    }
}
#endif
