// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestVulkanImageLayouts : GraphicTestGameBase
{
    /// <summary>
    /// A barrier on a single-mip view moves that mip only, so a mip chain can read one mip while it writes the next.
    /// </summary>
    [SkippableFact]
    public void SingleMipViewTransitionsOnlyItsMip()
    {
        PerformTest(game =>
        {
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Vulkan layout tracking.");

            var commandList = game.GraphicsContext.CommandList;
            var texture = Texture.New2D(game.GraphicsDevice, 16, 16, mipCount: 2, PixelFormat.R8G8B8A8_UNorm,
                TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            var mip0 = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, MipLevel = 0, Format = texture.Format, Flags = TextureFlags.ShaderResource });
            var mip1 = texture.ToTextureView(new TextureViewDescription { Type = ViewType.Single, MipLevel = 1, Format = texture.Format, Flags = TextureFlags.RenderTarget });

            commandList.ResourceBarrierTransition(mip0, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(mip1, BarrierLayout.RenderTarget);

            Assert.Equal(BarrierLayout.ShaderResource, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(0, 0)));
            Assert.Equal(BarrierLayout.RenderTarget, texture.LayoutTracker.Get((uint) texture.GetSubResourceIndex(0, 1)));
        });
    }

    /// <summary>
    /// Command lists recorded in one order and submitted in another find their images in the layouts they assumed,
    /// as the validation layer checks during vkQueueSubmit2.
    /// </summary>
    [SkippableFact]
    public void CommandListsSubmittedOutOfRecordingOrderFindTheirLayouts()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Vulkan layout tracking.");
            Skip.IfNot(device.IsProfilingSupported, "Needs the Vulkan validation layer.");

            var errors = new List<string>();
            void OnMessage(ILogMessage message)
            {
                if (message.Type >= LogMessageType.Error && message.Text.Contains("[Vulkan]"))
                    lock (errors) errors.Add(message.Text);
            }

            var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            var setup = CommandList.New(device);
            var first = CommandList.New(device);
            var second = CommandList.New(device);

            GlobalLogger.GlobalMessageLogged += OnMessage;
            try
            {
                setup.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
                device.ExecuteCommandList(setup.Close());

                // Recorded first, submitted last: starts from ShaderResource
                first.ResourceBarrierTransition(texture, BarrierLayout.RenderTarget);
                // Recorded second, submitted first: starts from the RenderTarget the first one leaves
                second.ResourceBarrierTransition(texture, BarrierLayout.CopySource);

                device.ExecuteCommandList(second.Close());
                device.ExecuteCommandList(first.Close());
            }
            finally
            {
                GlobalLogger.GlobalMessageLogged -= OnMessage;
            }

            Assert.True(errors.Count == 0, string.Join('\n', errors));
        });
    }
}
