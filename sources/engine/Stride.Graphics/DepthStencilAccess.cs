// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines how draws access the bound Depth-Stencil Buffer.
/// </summary>
internal enum DepthStencilAccess
{
    /// <summary>
    ///   Depth and stencil writes are allowed.
    /// </summary>
    Write,

    /// <summary>
    ///   Read-only: the buffer can be depth-tested and sampled at the same time.
    ///   Depth and stencil writes are dropped.
    /// </summary>
    Read,
}
