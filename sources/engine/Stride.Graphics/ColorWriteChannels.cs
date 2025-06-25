// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Graphics;

[Flags]
[DataContract]
public enum ColorWriteChannels
{
    /// <summary>
    /// Identify which components of each pixel of a render target are writable during blending.
    /// </summary>
    /// <remarks>
    /// These flags can be combined with a bitwise OR and is used in <see cref="BlendStateDescription"/> and <see cref="BlendStateRenderTargetDescription"/>.
    /// </remarks>
        /// <summary>
        /// None of the data are stored.
        /// </summary>
        /// <summary>
        /// Allow data to be stored in the red component. 
        /// </summary>
        /// <summary>
        /// Allow data to be stored in the green component. 
        /// </summary>
        /// <summary>
        /// Allow data to be stored in the blue component. 
        /// </summary>
        /// <summary>
        /// Allow data to be stored in the alpha component. 
        /// </summary>
        /// <summary>
        /// Allow data to be stored in all components. 
        /// </summary>
    None = 0,

    Red = 1,

    Green = 2,

    Blue = 4,

    Alpha = 8,

    All = Alpha | Blue | Green | Red
}
