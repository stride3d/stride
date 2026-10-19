// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// The layout tracker goes back to a single layout after a whole-resource transition, and a whole-resource
/// transition emits one barrier when the sub-resources share a layout again.
/// </summary>
public class TestSubresourceLayoutTracker : GraphicTestGameBase
{
    [Fact]
    public void WholeTransitionAfterPerSubresourceGoesBackToOneLayout()
    {
        var tracker = new SubresourceLayoutTracker();
        tracker.Initialize(BarrierLayout.ShaderResource, subresourceCount: 4);

        tracker.Set(1, BarrierLayout.RenderTarget);
        Assert.True(tracker.HasPerSubresourceTracking);
        Assert.Equal(BarrierLayout.RenderTarget, tracker.Get(1));
        Assert.Equal(BarrierLayout.ShaderResource, tracker.Get(2));

        tracker.Set(uint.MaxValue, BarrierLayout.CopySource);
        Assert.False(tracker.HasPerSubresourceTracking);
        Assert.Equal(BarrierLayout.CopySource, tracker.Get(1));
        Assert.False(tracker.NeedsTransition(uint.MaxValue, BarrierLayout.CopySource));

        // The array kept for reuse starts again from the single layout
        tracker.Set(2, BarrierLayout.UnorderedAccess);
        Assert.Equal(BarrierLayout.CopySource, tracker.Get(1));
        Assert.Equal(BarrierLayout.UnorderedAccess, tracker.Get(2));
    }

    [Fact]
    public void CommonLayout()
    {
        var tracker = new SubresourceLayoutTracker();
        tracker.Initialize(BarrierLayout.ShaderResource, subresourceCount: 3);
        Assert.True(tracker.TryGetCommonLayout(out var layout));
        Assert.Equal(BarrierLayout.ShaderResource, layout);

        tracker.Set(1, BarrierLayout.RenderTarget);
        Assert.False(tracker.TryGetCommonLayout(out _));

        // Every sub-resource moved back one by one: per-subresource tracking, but one layout
        tracker.Set(1, BarrierLayout.ShaderResource);
        Assert.True(tracker.HasPerSubresourceTracking);
        Assert.True(tracker.TryGetCommonLayout(out layout));
        Assert.Equal(BarrierLayout.ShaderResource, layout);
    }

    [Fact]
    public void CloneDoesNotShareTheArray()
    {
        var tracker = new SubresourceLayoutTracker();
        tracker.Initialize(BarrierLayout.ShaderResource, subresourceCount: 3);
        tracker.Set(1, BarrierLayout.RenderTarget);
        tracker.Set(uint.MaxValue, BarrierLayout.ShaderResource);

        // A clone of a single-layout tracker that kept its array
        var clone = tracker.Clone();
        clone.Set(1, BarrierLayout.UnorderedAccess);
        tracker.Set(2, BarrierLayout.CopyDest);

        Assert.Equal(BarrierLayout.UnorderedAccess, clone.Get(1));
        Assert.Equal(BarrierLayout.ShaderResource, clone.Get(2));
        Assert.Equal(BarrierLayout.ShaderResource, tracker.Get(1));
        Assert.Equal(BarrierLayout.CopyDest, tracker.Get(2));
    }

    [SkippableFact]
    public void WholeTransitionEmitsOneBarrierWhenSubresourcesShareALayout()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Direct3D12, "Counts Direct3D 12 barriers.");

#if STRIDE_GRAPHICS_API_DIRECT3D12
            var commandList = game.GraphicsContext.CommandList;
            using var texture = Texture.New2D(game.GraphicsDevice, 8, 8, mipCount: 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);

            // Mip-level 1 goes to render target and back: all mip-levels share one layout again. The whole-resource
            // barrier is recorded after the pending sub-resource ones, which leaves it alone
            commandList.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget, 1);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource, 1);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopySource);
            Assert.Equal(1, commandList.PendingBarrierCount);

            // Mip-levels 1 and 2 in other layouts: one barrier for each sub-resource that differs
            commandList.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget, 1);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopyDest, 2);
            int count = commandList.PendingBarrierCount;
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopySource);
            Assert.Equal(2, commandList.PendingBarrierCount - count);
#endif
        });
    }
}
