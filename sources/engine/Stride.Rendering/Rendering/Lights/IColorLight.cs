// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Colors;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Base interface for a light with a color
    /// </summary>
    public interface IColorLight : ILight
    {
        /// <summary>
        /// Gets or sets the light color.
        /// </summary>
        /// <value>The color.</value>
        IColorProvider Color { get; set; }

        /// <summary>
        /// Computes the color to the proper <see cref="ColorSpace"/> with the specified intensity.
        /// </summary>
        /// <param name="colorSpace"></param>
        /// <param name="intensity">The intensity.</param>
        /// <returns>Color3.</returns>
        Color3 ComputeColor(ColorSpace colorSpace, float intensity);
    }
}
