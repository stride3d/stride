// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    /// <summary>
    /// Specifies the buffer to use when calling Clear.
    /// </summary>
    [Flags]
    public enum DepthStencilClearOptions
    {
        /// <summary>
        /// A depth buffer.
        /// </summary>
        DepthBuffer = 1,
        /// <summary>
        /// A stencil buffer.
        /// </summary>
        Stencil = 2,
    }
}
