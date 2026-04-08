// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Flags that specify what parts of a Depth-Stencil Buffer to clear when calling
///   <see cref="CommandList.Clear(Texture, DepthStencilClearOptions, float, byte)"/>.
/// </summary>
[Flags]
public enum DepthStencilClearOptions
{
    /// <summary>
    ///   Neither the Depth Buffer nor the Stencil Buffer will be cleared.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Selects the Depth Buffer.
    /// </summary>
    DepthBuffer = 1,

    /// <summary>
    ///   Selects the Stencil Buffer.
    /// </summary>
    Stencil = 2
}
