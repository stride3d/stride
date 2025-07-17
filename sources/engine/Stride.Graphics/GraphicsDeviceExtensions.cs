// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;
using Stride.Rendering;

namespace Stride.Graphics;

public static class GraphicsDeviceExtensions
{
    #region DrawQuad / DrawTexture Helpers

    // TODO: Should this be named DrawFullScreen or DrawFullScreenTriangle instead?

    /// <summary>
    /// Extensions for the <see cref="GraphicsDevice"/>
    /// </summary>
    public static void DrawQuad(this GraphicsContext graphicsContext, EffectInstance effectInstance)
    {
        /// <summary>
        /// Draws a fullscreen quad with the specified effect and parameters.
        /// </summary>
        /// <param name="graphicsContext">The graphics context used for drawing.</param>
        /// <param name="effectInstance">The effect instance to apply when drawing the quad.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="effectInstance"/> is <c>null</c>.</exception>
        ArgumentNullException.ThrowIfNull(effectInstance);

        // Draw a full screen triangle using the provided effect instance.
        graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, effectInstance);
    }

        /// <summary>
        /// Draws a full screen quad. An <see cref="Effect"/> must be applied before calling this method.
        /// </summary>
    public static void DrawQuad(this CommandList commandList)
    {
        commandList.GraphicsDevice.PrimitiveQuad.Draw(commandList);
    }

        /// <summary>
        /// Draws a fullscreen texture using a <see cref="SamplerStateFactory.LinearClamp"/> sampler. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>

        /// <summary>
        /// Draws a fullscreen texture using the specified sampler. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="sampler">The sampler.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>

        /// <summary>
        /// Draws a fullscreen texture using a <see cref="SamplerStateFactory.LinearClamp"/> sampler
        /// and the texture color multiplied by a custom color. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="color">The color.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>

        /// <summary>
        /// Draws a fullscreen texture using the specified sampler
        /// and the texture color multiplied by a custom color. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="sampler">The sampler.</param>
        /// <param name="color">The color.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>

    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState: null, color: Color4.White, blendState);
    }

    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState samplerState, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState, Color4.White, blendState);
    }

    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, Color4 color, BlendStateDescription? blendState = null)
    {
        graphicsContext.DrawTexture(texture, samplerState: null, color, blendState);
    }

    public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState samplerState, Color4 color, BlendStateDescription? blendState = null)
    {
        graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, texture, samplerState, color, blendState);
    }

    #endregion

    public static Texture GetSharedWhiteTexture(this GraphicsDevice device)
    {
        return device.GetOrCreateSharedData("WhiteTexture", CreateWhiteTexture);

        static Texture CreateWhiteTexture(GraphicsDevice device)
        {
            const int Size = 2;

            Span<Color> textureData = stackalloc Color[Size * Size];
            textureData.Fill(Color.White);

            var texture = Texture.New2D(device, Size, Size, PixelFormat.R8G8B8A8_UNorm, textureData.ToArray());  // TODO: Use the Span<T> overload when available
            texture.Name = "WhiteTexture";
            return texture;
        }
    }
}
