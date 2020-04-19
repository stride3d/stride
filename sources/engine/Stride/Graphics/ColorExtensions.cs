// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Extension class for <see cref="Color"/>
    /// </summary>
    public static class ColorExtensions
    {
        /// <summary>
        /// Create a copy of the color with the provided alpha value.
        /// </summary>
        /// <param name="color">The color to take as reference</param>
        /// <param name="alpha">The alpha value of the new color</param>
        /// <returns>The color with the provided alpha value</returns>
        public static Color WithAlpha(this Color color, byte alpha)
        {
            return new Color(color.R, color.G, color.B, alpha);
        }

        /// <summary>
        /// Converts the color in gamma space to the specified <see cref="ColorSpace"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="colorSpace">The color space.</param>
        /// <returns>The color converted to the specified color space.</returns>
        public static Color4 ToColorSpace(this Color4 color, ColorSpace colorSpace)
        {
            return colorSpace == ColorSpace.Linear ? color.ToLinear() : color;
        }

        /// <summary>
        /// Converts the color in gamma space to the specified <see cref="ColorSpace"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="colorSpace">The color space.</param>
        /// <returns>The color converted to the specified color space.</returns>
        public static Color3 ToColorSpace(this Color3 color, ColorSpace colorSpace)
        {
            return colorSpace == ColorSpace.Linear ? color.ToLinear() : color;
        }

        /// <summary>
        /// Converts the color from a particualr color space to the specified <see cref="ColorSpace"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="sourceColorSpace">The color space of this instance.</param>
        /// <param name="colorSpace">The color space.</param>
        /// <returns>The color converted to the specified color space.</returns>
        public static Color4 ToColorSpace(this Color4 color, ColorSpace sourceColorSpace, ColorSpace colorSpace)
        {
            // Nothing to do?
            if (sourceColorSpace == colorSpace)
            {
                return color;
            }

            return sourceColorSpace == ColorSpace.Gamma ? color.ToLinear() : color.ToSRgb();
        }

        /// <summary>
        /// Converts the color from a particualr color space to the specified <see cref="ColorSpace"/>.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="sourceColorSpace">The color space of this instance.</param>
        /// <param name="colorSpace">The color space.</param>
        /// <returns>The color converted to the specified color space.</returns>
        public static Color3 ToColorSpace(this Color3 color, ColorSpace sourceColorSpace, ColorSpace colorSpace)
        {
            // Nothing to do?
            if (sourceColorSpace == colorSpace)
            {
                return color;
            }

            return sourceColorSpace == ColorSpace.Gamma ? color.ToLinear() : color.ToSRgb();
        }
    }
}
