// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// The colorspace used for textures, materials, lighting...
    /// </summary>
    [DataContract("ColorSpace")]
    public enum ColorSpace
    {
        /// <summary>
        /// Use a linear colorspace.
        /// </summary>
        Linear,

        /// <summary>
        /// Use a gamma colorspace.
        /// </summary>
        Gamma,
    }
}
