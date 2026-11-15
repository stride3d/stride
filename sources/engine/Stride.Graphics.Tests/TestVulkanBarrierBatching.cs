// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// On Vulkan, the barriers asked for before a command go into the command buffer together, right before it, and two
/// transitions of the same sub-resources merge into one barrier.
/// </summary>
public class TestVulkanBarrierBatching : GraphicTestGameBase
{
    [SkippableFact]
    public void TransitionsBeforeACommandAreOneBarrierCommand()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Counts Vulkan barrier commands.");

#if STRIDE_GRAPHICS_API_VULKAN
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;
            using var a = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var b = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var c = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var cleared = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            commandList.Clear(cleared, Color.Black);

            int commands = commandList.NativeBarrierCommandCount;
            int barriers = commandList.NativeBarrierCount;

            // Three transitions, then a second one of `a` that merges with its first
            commandList.ResourceBarrierTransition(a, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(b, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(c, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(a, BarrierLayout.CopySource);
            Assert.Equal(commands, commandList.NativeBarrierCommandCount);

            // The clear's own transition joins them, and they all go in before the clear
            commandList.Clear(cleared, Color.Red);
            Assert.Equal(commands + 1, commandList.NativeBarrierCommandCount);
            Assert.Equal(barriers + 4, commandList.NativeBarrierCount);

            // The pending ones (back to render target after the clear) still apply: the copy reads the cleared color
            using var staging = cleared.ToStaging();
            commandList.Copy(cleared, staging);
            Assert.Equal(Color.Red, staging.GetData<Color>(commandList)[0]);
#endif
        });
    }

    /// <summary>
    /// A copy between two ranges of one buffer asks for a read barrier and a write barrier on the same buffer: merged,
    /// the barrier keeps both accesses (sync validation reports the one it would drop).
    /// </summary>
    [SkippableFact]
    public void CopyWithinOneBufferKeepsTheReadAndTheWrite()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Merges Vulkan barriers.");

            var commandList = game.GraphicsContext.CommandList;
            using var buffer = Buffer.Typed.New(game.GraphicsDevice, new uint[] { 1, 2, 3, 4, 0, 0, 0, 0 }, PixelFormat.R32_UInt);

            commandList.CopyRegion(buffer, 0, new ResourceRegion(left: 0, top: 0, front: 0, right: 16, bottom: 1, back: 1), buffer, 0, dstX: 16);

            Assert.Equal(new uint[] { 1, 2, 3, 4, 1, 2, 3, 4 }, buffer.GetData<uint>(commandList));
        });
    }
}
