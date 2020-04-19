// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Graphics
{
    /// <summary>
    /// Defines the possible rotations to apply on image regions.
    /// </summary>
    public enum ImageOrientation
    {
        /// <summary>
        /// The image region is taken as is.
        /// </summary>
        AsIs = 0,

        /// <summary>
        /// The image is rotated of the 90 degrees (clockwise) in the source texture.
        /// </summary>
        Rotated90 = 1,
    }
}
