// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Shaders;

namespace Stride.Graphics.Tests;

/// <summary>
/// On Direct3D 11, a transition to unordered access unbinds the shader resource views that read what is about to
/// be written, and only those: reading mip-level N while writing mip-level N + 1 stays possible.
/// </summary>
public class TestUnorderedAccessTransitionUnbind : GraphicTestGameBase
{
    [Fact]
    public void ViewSubResourceRanges()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var array = Texture.New2D(device, 8, 8, mipCount: 3, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource, arraySize: 2);
            var volume = Texture.New3D(device, 8, 8, 8, mipCount: 3, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);

            Assert.Equal(new TextureSubresourceRange(0, 3, 0, 2), array.GetViewSubResourceRange());
            Assert.Equal(new TextureSubresourceRange(1, 1, 1, 1), array.ToTextureView(ViewType.Single, 1, 1).GetViewSubResourceRange());
            Assert.Equal(new TextureSubresourceRange(1, 1, 1, 1), array.ToTextureView(ViewType.MipBand, 1, 1).GetViewSubResourceRange());
            Assert.Equal(new TextureSubresourceRange(1, 2, 1, 1), array.ToTextureView(ViewType.ArrayBand, 1, 1).GetViewSubResourceRange());

            // Depth slices are not sub-resources
            Assert.Equal(new TextureSubresourceRange(0, 3, 0, 1), volume.GetViewSubResourceRange());
            Assert.Equal(new TextureSubresourceRange(2, 1, 0, 1), volume.ToTextureView(ViewType.Single, 0, 2).GetViewSubResourceRange());
        });
    }

    [SkippableFact]
    public void TransitionUnbindsOnlyTheViewsOfTheWrittenSubResource()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D11, "Shader resource unbinding on transitions is Direct3D 11 only.");

#if STRIDE_GRAPHICS_API_DIRECT3D11
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            var texture = Texture.New2D(device, 4, 4, mipCount: 2, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);
            var mip0 = texture.ToTextureView(ViewType.Single, 0, 0);
            var mip1 = texture.ToTextureView(ViewType.Single, 0, 1);

            // Read mip-level 0 (compute and pixel stages), and the whole texture
            commandList.SetShaderResourceView(ShaderStage.Compute, 0, mip0);
            commandList.SetShaderResourceView(ShaderStage.Pixel, 0, mip0);
            commandList.SetShaderResourceView(ShaderStage.Compute, 1, texture);

            // Write mip-level 1: the views of mip-level 0 stay bound, the whole texture view (which reads it too) does not
            commandList.ResourceBarrierTransition(texture, BarrierLayout.UnorderedAccess, (uint) texture.GetSubResourceIndex(0, 1));
            Assert.True(commandList.IsShaderResourceViewBound(ShaderStage.Compute, 0));
            Assert.True(commandList.IsShaderResourceViewBound(ShaderStage.Pixel, 0));
            Assert.False(commandList.IsShaderResourceViewBound(ShaderStage.Compute, 1));

            // Write mip-level 1 through its view: a view stands for the sub-resources it covers, as in Direct3D 12 and Vulkan
            commandList.ResourceBarrierTransition(mip1, BarrierLayout.UnorderedAccess);
            Assert.True(commandList.IsShaderResourceViewBound(ShaderStage.Compute, 0));
            Assert.True(commandList.IsShaderResourceViewBound(ShaderStage.Pixel, 0));

            // Write the whole texture
            commandList.ResourceBarrierTransition(texture, BarrierLayout.UnorderedAccess);
            Assert.False(commandList.IsShaderResourceViewBound(ShaderStage.Compute, 0));
            Assert.False(commandList.IsShaderResourceViewBound(ShaderStage.Pixel, 0));
#endif
        });
    }
}
