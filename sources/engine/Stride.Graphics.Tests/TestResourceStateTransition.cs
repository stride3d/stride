// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Shaders;

namespace Stride.Graphics.Tests;

#pragma warning disable CS0618 // GraphicsResourceState is obsolete: these tests check the overload games still call

/// <summary>
/// The obsolete <see cref="GraphicsResourceState"/> overload of <see cref="CommandList.ResourceBarrierTransition(GraphicsResource, GraphicsResourceState)"/>
/// transitions to the matching <see cref="BarrierLayout"/>, on every graphics API.
/// </summary>
public class TestResourceStateTransition : GraphicTestGameBase
{
    [SkippableFact]
    public void AStateMovesTheTextureToItsLayout()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Direct3D12 or GraphicsPlatform.Vulkan, "Layouts are tracked on Direct3D 12 and Vulkan.");

            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource | TextureFlags.RenderTarget | TextureFlags.UnorderedAccess);

            (GraphicsResourceState State, BarrierLayout Layout)[] cases =
            [
                (GraphicsResourceState.RenderTarget, BarrierLayout.RenderTarget),
                (GraphicsResourceState.PixelShaderResource, BarrierLayout.ShaderResource),
                (GraphicsResourceState.UnorderedAccess, BarrierLayout.UnorderedAccess),
                (GraphicsResourceState.NonPixelShaderResource, BarrierLayout.ShaderResource),
                (GraphicsResourceState.CopyDestination, BarrierLayout.CopyDest),
                (GraphicsResourceState.AllShaderResource, BarrierLayout.ShaderResource),
                (GraphicsResourceState.CopySource, BarrierLayout.CopySource),
                // A combination of read states has no layout of its own
                (GraphicsResourceState.GenericRead, BarrierLayout.Common),
            ];
            foreach (var (state, layout) in cases)
            {
                commandList.ResourceBarrierTransition(texture, state);
                Assert.True(layout == texture.LayoutTracker.Get(0), $"{state} moved the texture to {texture.LayoutTracker.Get(0)}, not {layout}");
            }
        });
    }

    [SkippableFact]
    public void TheUnorderedAccessStateUnbindsTheShaderResourceViewsAndAReadStateTheUnorderedAccessViews()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D11, "Unbinding on transitions is Direct3D 11 only.");

#if STRIDE_GRAPHICS_API_DIRECT3D11
            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess);

            commandList.SetShaderResourceView(ShaderStage.Compute, 0, texture);
            commandList.ResourceBarrierTransition(texture, GraphicsResourceState.UnorderedAccess);
            Assert.False(commandList.IsShaderResourceViewBound(ShaderStage.Compute, 0));

            commandList.SetUnorderedAccessView(ShaderStage.Compute, 0, texture, -1);
            commandList.ResourceBarrierTransition(texture, GraphicsResourceState.PixelShaderResource);
            Assert.False(commandList.IsUnorderedAccessViewBound(ShaderStage.Compute, 0));
#endif
        });
    }
}
