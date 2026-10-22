// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// Command lists record in any order but run in submission order: on first touch, a command list assumes the layout the
/// last recorded one left, and submission corrects it when the last submitted one left another. The validation layers
/// (Vulkan, and Direct3D 12 with GPU-based validation, on a GPU only) report a command list that finds a texture in
/// another layout than the one its barriers start from.
/// </summary>
public class TestSubmittedLayouts : GraphicTestGameBase
{
    [SkippableFact]
    public void CommandListsSubmittedOutOfRecordingOrderFindTheirLayouts()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Explicit layouts.");

            using var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var setup = CommandList.New(device);
            using var first = CommandList.New(device);
            using var second = CommandList.New(device);

            setup.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            device.ExecuteCommandList(setup.Close());

            // Recorded first, submitted last: starts from ShaderResource
            first.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget);
            // Recorded second, submitted first: starts from the RenderTarget the first one leaves
            second.ResourceBarrierTransition(texture, BarrierLayout.CopySource);

            device.ExecuteCommandList(second.Close());
            device.ExecuteCommandList(first.Close());

            Assert.Equal(BarrierLayout.RenderTarget, texture.SubmittedLayouts[0]);
        });
    }

    /// <summary>
    /// The same, with both command lists in one submission: each gets its corrections right before it.
    /// </summary>
    [SkippableFact]
    public void CommandListsExecutedTogetherOutOfRecordingOrderFindTheirLayouts()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Explicit layouts.");

            using var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var setup = CommandList.New(device);
            using var first = CommandList.New(device);
            using var second = CommandList.New(device);

            setup.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            device.ExecuteCommandList(setup.Close());

            first.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget);
            second.ResourceBarrierTransition(texture, BarrierLayout.CopySource);

            device.ExecuteCommandLists(2, [second.Close(), first.Close()]);

            Assert.Equal(BarrierLayout.RenderTarget, texture.SubmittedLayouts[0]);
        });
    }

    /// <summary>
    /// A command list whose first touch needs no barrier (the texture is already in the layout it asks for) still gets
    /// corrected: the layout it assumes is recorded on first touch. The common case with parallel recording.
    /// </summary>
    [SkippableFact]
    public void FirstTouchWithoutBarrierIsCorrected()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Explicit layouts.");

            using var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var setup = CommandList.New(device);
            using var first = CommandList.New(device);
            using var second = CommandList.New(device);

            setup.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            device.ExecuteCommandList(setup.Close());

            first.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget);
            // Already RenderTarget for this command list: no barrier recorded, but submitted first it finds ShaderResource
            second.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget);

            device.ExecuteCommandList(second.Close());
            Assert.Equal(BarrierLayout.RenderTarget, texture.SubmittedLayouts[0]);

            device.ExecuteCommandList(first.Close());
            Assert.Equal(BarrierLayout.RenderTarget, texture.SubmittedLayouts[0]);
        });
    }

    /// <summary>
    /// A texture whose native resource is recreated starts from the new one's layout, not from what was submitted for the old one.
    /// </summary>
    [SkippableFact]
    public void RecreatedTextureStartsFromItsNewResource()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Explicit layouts.");

            using var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var commandList = CommandList.New(device);

            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopySource);
            device.ExecuteCommandList(commandList.Close());

            texture.OnDestroyed();
            texture.Recreate();

            // Starts from the new resource's initial layout: a correction from CopySource would move it from a layout it is not in
            commandList.Reset();
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopyDest);
            device.ExecuteCommandList(commandList.Close());

            Assert.Equal(BarrierLayout.CopyDest, texture.SubmittedLayouts[0]);
        });
    }
}
