// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// Recording on a closed <see cref="CommandList"/> throws on Direct3D 12 and Vulkan, and works again after <see cref="CommandList.Reset"/>.
/// </summary>
public class TestClosedCommandList : GraphicTestGameBase
{
    [SkippableFact]
    public void RecordingOnAClosedCommandListThrows()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(GraphicsDevice.Platform is GraphicsPlatform.Vulkan or GraphicsPlatform.Direct3D12, "Only these APIs have a closed state.");

            using var target = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget);
            using var buffer = Buffer.Typed.New(device, 16, PixelFormat.R32_UInt);
            using var commandList = CommandList.New(device);

            device.ExecuteCommandList(commandList.Close());

            Assert.Throws<InvalidOperationException>(() => commandList.Clear(target, Color.Red));
            Assert.Throws<InvalidOperationException>(() => buffer.SetData(commandList, new byte[64]));
            Assert.Throws<InvalidOperationException>(() => commandList.Close());

            commandList.Reset();
            commandList.Clear(target, Color.Red);
            buffer.SetData(commandList, new byte[64]);
            device.ExecuteCommandList(commandList.Close());
        });
    }
}
