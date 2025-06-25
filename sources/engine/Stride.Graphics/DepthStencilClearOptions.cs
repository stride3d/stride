// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

[Flags]
public enum DepthStencilClearOptions
{
    /// <summary>
    /// Specifies the buffer to use when calling Clear.
    /// </summary>
        /// <summary>
        /// A depth buffer.
        /// </summary>
        /// <summary>
        /// A stencil buffer.
        /// </summary>
    DepthBuffer = 1,

    Stencil = 2
}
