// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Assets.Textures
{
    /// <summary>
    /// Represents the different formats of alpha channel possibly desired.
    /// </summary>
    [DataContract]
    public enum AlphaFormat
    {
        /// <summary>
        /// Alpha channel should be ignored.
        /// </summary>
        /// <userdoc>No alpha channel</userdoc>
        None,

        /// <summary>
        /// Alpha channel should be stored as 1-bit mask if possible.
        /// </summary>
        /// <userdoc>Ensures an alpha channel composed of only absolute opaque or absolute transparent values.</userdoc>
        Mask,

        /// <summary>
        /// Alpha channel should be stored with explicit compression. Well suited to sharp alpha transitions between translucent and opaque areas.
        /// </summary>
        /// <userdoc>Ensures an alpha channel well suited for sharp alpha transitions between translucent and opaque areas.</userdoc>
        Explicit,

        /// <summary>
        /// Alpha channel should be stored using interpolation. Well suited for alpha gradient.
        /// </summary>
        /// <userdoc>Ensure an alpha channel well suited for alpha gradient.</userdoc>
        Interpolated,

        /// <summary>
        /// Automatic alpha detection.
        /// </summary>
        /// <userdoc>Automatic alpha detection</userdoc>
        Auto,
    }
}
