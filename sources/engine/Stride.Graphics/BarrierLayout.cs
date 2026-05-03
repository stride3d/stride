// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines the memory layout of a graphics resource for barrier transitions.
///   This is a cross-platform abstraction over D3D12 Enhanced Barrier layouts and Vulkan image layouts.
/// </summary>
public enum BarrierLayout
{
    /// <summary>
    ///   The resource layout is undefined. Contents may be discarded.
    /// </summary>
    Undefined,

    /// <summary>
    ///   General-purpose layout compatible with all operations.
    /// </summary>
    Common,

    /// <summary>
    ///   The resource is used as a render target.
    /// </summary>
    RenderTarget,

    /// <summary>
    ///   The resource is used as a writable depth-stencil buffer.
    /// </summary>
    DepthStencilWrite,

    /// <summary>
    ///   The resource is used as a read-only depth-stencil buffer.
    /// </summary>
    DepthStencilRead,

    /// <summary>
    ///   The resource is used as a shader resource (SRV) for reading.
    /// </summary>
    ShaderResource,

    /// <summary>
    ///   The resource is used for unordered access (UAV) for reading and writing.
    /// </summary>
    UnorderedAccess,

    /// <summary>
    ///   The resource is used as the source of a copy operation.
    /// </summary>
    CopySource,

    /// <summary>
    ///   The resource is used as the destination of a copy operation.
    /// </summary>
    CopyDest,

    /// <summary>
    ///   The resource is used for presentation to the screen.
    /// </summary>
    Present,

    /// <summary>
    ///   The resource is used as the source of a multisample resolve operation.
    /// </summary>
    ResolveSource,

    /// <summary>
    ///   The resource is used as the destination of a multisample resolve operation.
    /// </summary>
    ResolveDest,
}
