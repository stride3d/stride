// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Physics
{
    /// <summary>
    /// Defines the different possible orientations of a shape.
    /// </summary>
    public enum ShapeOrientation
    {
        /// <summary>
        /// The shape is aligned with the Ox axis.
        /// </summary>
        /// <userdoc>The top of shape is aligned with the Ox axis.</userdoc>
        UpX,

        /// <summary>
        /// The shape is aligned with the Oy axis.
        /// </summary>
        /// <userdoc>The top shape is aligned with the Oy axis.</userdoc>
        UpY,

        /// <summary>
        /// The shape is aligned with the Oz axis.
        /// </summary>
        /// <userdoc>The top shape is aligned with the Oz axis.</userdoc>
        UpZ,
    }
}
