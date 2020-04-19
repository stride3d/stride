// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Extensions for <see cref="ImageEffect"/>.
    /// </summary>
    public static class ImageEffectExtensions
    {
        /// <summary>
        /// Sets an input texture
        /// </summary>
        /// <param name="imageEffect">The post effect.</param>
        /// <param name="texture">The texture.</param>
        public static void SetInput(this IImageEffect imageEffect, Texture texture)
        {
            imageEffect.SetInput(0, texture);
        }

        /// <summary>
        /// Sets two input textures
        /// </summary>
        /// <param name="imageEffect">The post effect.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="texture2">The texture2.</param>
        public static void SetInput(this IImageEffect imageEffect, Texture texture, Texture texture2)
        {
            imageEffect.SetInput(0, texture);
            imageEffect.SetInput(1, texture2);
        }

        /// <summary>
        /// Sets two input textures
        /// </summary>
        /// <param name="imageEffect">The post effect.</param>
        /// <param name="texture">The texture.</param>
        /// <param name="texture2">The texture2.</param>
        /// <param name="texture3">The texture3.</param>
        public static void SetInput(this IImageEffect imageEffect, Texture texture, Texture texture2, Texture texture3)
        {
            imageEffect.SetInput(0, texture);
            imageEffect.SetInput(1, texture2);
            imageEffect.SetInput(2, texture3);
        }         
    }
}
