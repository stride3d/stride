// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Defines flags for GPU pipeline synchronization points used in barrier transitions.
///   This is a cross-platform abstraction over D3D12 Enhanced Barrier sync flags and Vulkan pipeline stage flags.
/// </summary>
[Flags]
public enum BarrierSync
{
    /// <summary>
    ///   No synchronization.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Synchronize with all GPU work.
    /// </summary>
    All = 1 << 0,

    /// <summary>
    ///   Synchronize with draw operations.
    /// </summary>
    Draw = 1 << 1,

    /// <summary>
    ///   Synchronize with vertex input assembly.
    /// </summary>
    VertexInput = 1 << 2,

    /// <summary>
    ///   Synchronize with pixel shader execution.
    /// </summary>
    PixelShader = 1 << 3,

    /// <summary>
    ///   Synchronize with non-pixel shader execution (vertex, geometry, hull, domain).
    /// </summary>
    NonPixelShader = 1 << 4,

    /// <summary>
    ///   Synchronize with compute shader execution.
    /// </summary>
    ComputeShader = 1 << 5,

    /// <summary>
    ///   Synchronize with depth-stencil operations.
    /// </summary>
    DepthStencil = 1 << 6,

    /// <summary>
    ///   Synchronize with render target operations.
    /// </summary>
    RenderTarget = 1 << 7,

    /// <summary>
    ///   Synchronize with copy operations.
    /// </summary>
    Copy = 1 << 8,

    /// <summary>
    ///   Synchronize with multisample resolve operations.
    /// </summary>
    Resolve = 1 << 9,
}
