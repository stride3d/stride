// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Xunit;

namespace Stride.Graphics.Tests;

/// <summary>
/// <see cref="CommandList.SetBlendFactor"/> sets the color a blend state using <see cref="Blend.BlendFactor"/> blends with.
/// </summary>
public class TestBlendFactor : GraphicTestGameBase
{
    [SkippableFact]
    public void ADrawBlendsWithTheBlendFactor()
    {
        PerformTest(game =>
        {
            var context = game.GraphicsContext;
            var commandList = context.CommandList;

            using var white = Texture.New2D(game.GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, [Color.White]);
            using var target = Texture.New2D(game.GraphicsDevice, 4, 4, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget);

            commandList.Clear(target, Color.Black);
            commandList.SetRenderTargetAndViewport(null, target);
            commandList.SetBlendFactor(new Color4(0.25f, 0.5f, 0.75f, 1.0f));
            // White times the blend factor, plus none of the target
            context.DrawTexture(white, new BlendStateDescription(Blend.BlendFactor, Blend.Zero));

            var pixel = target.GetData<Color>(commandList)[0];
            Assert.InRange(pixel.R, 63, 65);
            Assert.InRange(pixel.G, 127, 128);
            Assert.InRange(pixel.B, 190, 192);
            Assert.Equal(255, pixel.A);
        });
    }
}
