// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

using Stride.Core.Mathematics;

using Stride.Rendering;

namespace Stride.Graphics;

/// <summary>
///   Defines a set of extension methods for the <see cref="GraphicsDevice"/> class.
/// </summary>
public static class GraphicsDeviceExtensions
{
    #region DrawQuad / DrawTexture Helpers

    // TODO: Should this be named DrawFullScreen or DrawFullScreenTriangle instead?

    /// <summary>
    ///   Draws a full-screen triangle with the specified effect.
    /// </summary>
    /// <param name="graphicsContext">The graphics context used for drawing.</param>
    /// <param name="effectInstance">The Effect instance to apply when drawing the triangle.</param>
    /// <exception cref="ArgumentNullException"><paramref name="effectInstance"/> is <see langword="null"/>.</exception>
    public static void DrawQuad(this GraphicsContext graphicsContext, EffectInstance effectInstance)
    {
        ArgumentNullException.ThrowIfNull(effectInstance);

        // Draw a full screen triangle using the provided effect instance.
        graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, effectInstance);
    }

    /// <summary>
    ///   Draws a full-screen triangle with the currently applied <see cref="Effect"/>.
    /// </summary>
    /// <param name="commandList">The Command List to use to draw the full-screen triangle.</param>
    public static void DrawQuad(this CommandList commandList)
    {
        commandList.GraphicsDevice.PrimitiveQuad.Draw(commandList);
    }

    /// <summary>
    ///   Draws a full-screen Texture using a <see cref="SamplerStateFactory.LinearClamp"/> Sampler.
    /// </summary>
    /// <param name="graphicsContext">The graphics context used for drawing.</param>
    /// <param name="texture">The Texture to draw.</param>
    /// <param name="blendState">
    ///   An optional Blend State to use when drawing the Texture.
    ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
    /// </param>
    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState: null, color: Color4.White, blendState);
    }

    /// <summary>
    ///   Draws a full-screen Texture using the specified sampler.
    /// </summary>
    /// <param name="graphicsContext">The graphics context used for drawing.</param>
    /// <param name="texture">The Texture to draw.</param>
    /// <param name="samplerState">
    ///   The Sampler State to use for sampling the texture.
    ///   Specify <see langword="null"/> to use the default Sampler State <see cref="SamplerStateFactory.LinearClamp"/>.
    /// </param>
    /// <param name="blendState">
    ///   An optional Blend State to use when drawing the Texture.
    ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
    /// </param>
    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState samplerState, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState, Color4.White, blendState);
    }

    /// <summary>
    ///   Draws a full-screen tinted Texture using a <see cref="SamplerStateFactory.LinearClamp"/> Sampler.
    /// </summary>
    /// <param name="graphicsContext">The graphics context used for drawing.</param>
    /// <param name="texture">The Texture to draw.</param>
    /// <param name="color">
    ///   The color to tint the Texture with. The final color will be the texture color multiplied by the specified color.
    /// </param>
    /// <param name="blendState">
    ///   An optional Blend State to use when drawing the Texture.
    ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
    /// </param>
    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, Color4 color, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState: null, color, blendState);
    }

    /// <summary>
    ///   Draws a full-screen tinted Texture using the specified Sampler.
    /// </summary>
    /// <param name="graphicsContext">The graphics context used for drawing.</param>
    /// <param name="texture">The Texture to draw.</param>
    /// <param name="samplerState">
    ///   The Sampler State to use for sampling the texture.
    ///   Specify <see langword="null"/> to use the default Sampler State <see cref="SamplerStateFactory.LinearClamp"/>.
    /// </param>
    /// <param name="color">
    ///   The color to tint the Texture with. The final color will be the texture color multiplied by the specified color.
    /// </param>
    /// <param name="blendState">
    ///   An optional Blend State to use when drawing the Texture.
    ///   Specify <see langword="null"/> to use <see cref="BlendStates.Default"/>.
    /// </param>
    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState samplerState, Color4 color, BlendStateDescription? blendState = null)
    {
        graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, texture, samplerState, color, blendState);
    }

    #endregion

    /// <summary>
    ///   Gets or creates a shared 2x2 white Texture for the specified Graphics Device.
    /// </summary>
    /// <param name="device">The Graphics Device for which to retrieve the shared white Texture.</param>
    /// <returns>A <see cref="Texture"/> consisting of 2x2 white pixels.</returns>
    public static Texture GetSharedWhiteTexture(this GraphicsDevice device)
    {
        return device.GetOrCreateSharedData("WhiteTexture", CreateWhiteTexture);

        //
        // Creates a 2x2 white Texture that can be shared across multiple graphics contexts.
        //
        static unsafe Texture CreateWhiteTexture(GraphicsDevice device)
        {
            const int Size = 2;

            Span<Color> textureData = stackalloc Color[Size * Size];
            textureData.Fill(Color.White);

            Unsafe.SkipInit(out DataBox dataBox);
            fixed (Color* textureDataPtr = textureData)
            {
                var rowPitch = Size * sizeof(Color);
                var slicePitch = rowPitch * Size;
                dataBox = new DataBox((nint) textureDataPtr, rowPitch, slicePitch);
            }

            var texture = device.IsDebugMode
                ? new Texture(device, "WhiteTexture")
                : new Texture(device);

            var textureDescription = TextureDescription.New2D(Size, Size, PixelFormat.R8G8B8A8_UNorm);
            texture.InitializeFrom(textureDescription, [ dataBox ]); // TODO: Use the Span<T> overload when available

            return texture;
        }
    }
}
