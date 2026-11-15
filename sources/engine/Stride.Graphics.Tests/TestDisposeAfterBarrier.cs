// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Graphics.Tests;

public class TestDisposeAfterBarrier : GraphicTestGameBase
{
    /// <summary>
    /// A resource can be disposed right after a transition was requested on it: the command list
    /// keeps recording, and the next command that flushes the barriers does not use the destroyed resource.
    /// </summary>
    [Fact]
    public void DisposeWithPendingTransitionKeepsRecording()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            var texture = Texture.New2D(device, 16, 16, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            var buffer = Buffer.New(device, 256, BufferFlags.ShaderResource | BufferFlags.UnorderedAccess, PixelFormat.R32_UInt);

            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);
            commandList.ResourceBarrierTransition(buffer, BarrierLayout.UnorderedAccess);
            texture.Dispose();
            buffer.Dispose();

            // Any command that flushes the pending barriers
            commandList.Clear(device.Presenter.BackBuffer, Color.Black);
        });
    }
}
