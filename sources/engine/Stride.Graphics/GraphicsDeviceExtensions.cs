// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Xenko.Core.Mathematics;
using Xenko.Rendering;

namespace Xenko.Graphics
{
    /// <summary>
    /// Extensions for the <see cref="GraphicsDevice"/>
    /// </summary>
    public static class GraphicsDeviceExtensions
    {
        /// <summary>
        /// Draws a fullscreen quad with the specified effect and parameters.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="effectInstance">The effect instance.</param>
        /// <exception cref="System.ArgumentNullException">effect</exception>
        public static void DrawQuad(this GraphicsContext graphicsContext, EffectInstance effectInstance)
        {
            if (effectInstance == null) throw new ArgumentNullException("effectInstance");

            // Draw a full screen quad
            graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, effectInstance);
        }

        #region DrawQuad/DrawTexture Helpers
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
        public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, BlendStateDescription? blendState = null)
        {
            graphicsContext.DrawTexture(texture, null, Color4.White, blendState);
        }

        /// <summary>
        /// Draws a fullscreen texture using the specified sampler. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="sampler">The sampler.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>
        public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState sampler, BlendStateDescription? blendState = null)
        {
            graphicsContext.DrawTexture(texture, sampler, Color4.White, blendState);
        }

        /// <summary>
        /// Draws a fullscreen texture using a <see cref="SamplerStateFactory.LinearClamp"/> sampler
        /// and the texture color multiplied by a custom color. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="color">The color.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>
        public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, Color4 color, BlendStateDescription? blendState = null)
        {
            graphicsContext.DrawTexture(texture, null, color, blendState);
        }

        /// <summary>
        /// Draws a fullscreen texture using the specified sampler
        /// and the texture color multiplied by a custom color. See <see cref="Draw+a+texture"/> to learn how to use it.
        /// </summary>
        /// <param name="texture">The texture. Expecting an instance of <see cref="Texture"/>.</param>
        /// <param name="sampler">The sampler.</param>
        /// <param name="color">The color.</param>
        /// <param name="applyEffectStates">The flag to apply effect states.</param>
        public static void DrawTexture(this GraphicsContext graphicsContext, Texture texture, SamplerState sampler, Color4 color, BlendStateDescription? blendState = null)
        {
            graphicsContext.CommandList.GraphicsDevice.PrimitiveQuad.Draw(graphicsContext, texture, sampler, color, blendState);
        }
        #endregion

        public static Texture GetSharedWhiteTexture(this GraphicsDevice device)
        {
            return device.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, "WhiteTexture", CreateWhiteTexture);
        }

        private static Texture CreateWhiteTexture(GraphicsDevice device)
        {
            const int Size = 2;
            var whiteData = new Color[Size * Size];
            for (int i = 0; i < Size * Size; i++)
                whiteData[i] = Color.White;

            return Texture.New2D(device, Size, Size, PixelFormat.R8G8B8A8_UNorm, whiteData);
        }
    }
}
