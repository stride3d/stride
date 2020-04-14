// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Identify which components of each pixel of a render target are writable during blending.
    /// </summary>
    /// <remarks>
    /// These flags can be combined with a bitwise OR and is used in <see cref="BlendState"/>.
    /// </remarks>
    [Flags]
    [DataContract]
    public enum ColorWriteChannels
    {
        /// <summary>
        /// None of the data are stored.
        /// </summary>
        None = 0,

        /// <summary>
        /// Allow data to be stored in the red component. 
        /// </summary>
        Red = 1,

        /// <summary>
        /// Allow data to be stored in the green component. 
        /// </summary>
        Green = 2,

        /// <summary>
        /// Allow data to be stored in the blue component. 
        /// </summary>
        Blue = 4,

        /// <summary>
        /// Allow data to be stored in the alpha component. 
        /// </summary>
        Alpha = 8,

        /// <summary>
        /// Allow data to be stored in all components. 
        /// </summary>
        All = Alpha | Blue | Green | Red,
    }
}
