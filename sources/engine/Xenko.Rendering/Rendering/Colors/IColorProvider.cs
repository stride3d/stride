// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;

namespace Xenko.Rendering.Colors
{
    /// <summary>
    /// Defines the interface for describing the color of a light.
    /// </summary>
    public interface IColorProvider
    {
        /// <summary>
        /// Computes the color of the light (sRgb space).
        /// </summary>
        /// <returns>Color3.</returns>
        Color3 ComputeColor();
    }
}
