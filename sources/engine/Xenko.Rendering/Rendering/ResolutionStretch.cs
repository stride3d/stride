// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;

namespace Xenko.Rendering
{
    /// <summary>
    /// Enumerates the different ways to interpret a visual resolution value.
    /// </summary>
    [DataContract]
    public enum ResolutionStretch
    {
        /// <summary>
        /// The resolution is determined by the width, height and depth of the field.
        /// </summary>
        FixedWidthFixedHeight,

        /// <summary>
        /// The resolution is determined by the width, the ratio of the target, and the depth.
        /// </summary>
        FixedWidthAdaptableHeight,

        /// <summary>
        /// The resolution is determined by the height, the ratio of the target, and the depth.
        /// </summary>
        FixedHeightAdaptableWidth,
    }
}
