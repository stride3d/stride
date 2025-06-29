// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Identifies which components of each pixel of a Render Target can be written to during blending.
/// </summary>
/// <remarks>
///   These flags can be combined with a bitwise OR and set in <see cref="BlendStateDescription"/> and <see cref="BlendStateRenderTargetDescription"/>.
/// </remarks>
[Flags]
[DataContract]
public enum ColorWriteChannels
{
    /// <summary>
    ///   None of the data is stored.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Allow data to be stored in the red component (R).
    /// </summary>
    Red = 1,

    /// <summary>
    ///   Allow data to be stored in the green component (G).
    /// </summary>
    Green = 2,

    /// <summary>
    ///   Allow data to be stored in the blue component (B).
    /// </summary>
    Blue = 4,

    /// <summary>
    ///   Allow data to be stored in the alpha component (A).
    /// </summary>
    Alpha = 8,

    /// <summary>
    ///   Allow data to be stored in all components.
    /// </summary>
    All = Alpha | Blue | Green | Red
}
