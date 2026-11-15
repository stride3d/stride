// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Xunit;

using Stride.Core.Mathematics;

namespace Stride.Graphics.Tests;

/// <summary>
/// A copy or an upload through a texture view starts from the layout its texture is in, not the one the texture
/// was in when the view was created (Vulkan validates it).
/// </summary>
public class TestTextureViewCopy : GraphicTestGameBase
{
    [Fact]
    public void CopyFromViewAfterTheTextureChangedLayout()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var texture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var view = texture.ToTextureView(ViewType.Full, 0, 0);
            using var staging = texture.ToStaging();

            // The texture leaves the layout it had when the view was created
            commandList.Clear(texture, Color.Red);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);

            commandList.Copy(view, staging);

            Assert.Equal(Color.Red, staging.GetData<Color>(commandList)[0]);
        });
    }

    [Fact]
    public void UploadThroughViewAfterTheTextureChangedLayout()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var texture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
            using var view = texture.ToTextureView(ViewType.Full, 0, 0);

            // The texture leaves the layout it had when the view was created
            commandList.ResourceBarrierTransition(texture, BarrierLayout.CopySource);

            var pixels = new Color[4 * 4];
            pixels.AsSpan().Fill(Color.Blue);
            view.SetData(commandList, pixels);

            Assert.Equal(Color.Blue, texture.GetData<Color>(commandList)[0]);
        });
    }

    [Fact]
    public void CreatingAViewKeepsTheTextureLayout()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            var commandList = game.GraphicsContext.CommandList;

            using var texture = Texture.New2D(device, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);
            using var staging = texture.ToStaging();
            commandList.Clear(texture, Color.Green);
            commandList.ResourceBarrierTransition(texture, BarrierLayout.ShaderResource);

            // A view shares the state of its texture: creating one does not reset it to the initial layout
            using var view = texture.ToTextureView(ViewType.Single, 0, 0);
            commandList.Copy(texture, staging);

            Assert.Equal(Color.Green, staging.GetData<Color>(commandList)[0]);
        });
    }
}
