// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Defines flags for the type of access a GPU operation performs on a graphics resource.
///   This is a cross-platform abstraction over D3D12 Enhanced Barrier access flags and Vulkan access flags.
/// </summary>
[Flags]
public enum BarrierAccess
{
    /// <summary>
    ///   No access.
    /// </summary>
    None = 0,

    /// <summary>
    ///   The resource is read as a vertex buffer.
    /// </summary>
    VertexBuffer = 1 << 0,

    /// <summary>
    ///   The resource is read as an index buffer.
    /// </summary>
    IndexBuffer = 1 << 1,

    /// <summary>
    ///   The resource is read as a constant buffer.
    /// </summary>
    ConstantBuffer = 1 << 2,

    /// <summary>
    ///   The resource is read by a shader (SRV).
    /// </summary>
    ShaderRead = 1 << 3,

    /// <summary>
    ///   The resource is written by a shader (UAV).
    /// </summary>
    ShaderWrite = 1 << 4,

    /// <summary>
    ///   The resource is written as a render target.
    /// </summary>
    RenderTarget = 1 << 5,

    /// <summary>
    ///   The resource is read as a depth-stencil buffer.
    /// </summary>
    DepthStencilRead = 1 << 6,

    /// <summary>
    ///   The resource is written as a depth-stencil buffer.
    /// </summary>
    DepthStencilWrite = 1 << 7,

    /// <summary>
    ///   The resource is read as a copy source.
    /// </summary>
    CopyRead = 1 << 8,

    /// <summary>
    ///   The resource is written as a copy destination.
    /// </summary>
    CopyWrite = 1 << 9,

    /// <summary>
    ///   The resource is read as an indirect argument buffer.
    /// </summary>
    IndirectArgument = 1 << 10,

    /// <summary>
    ///   The resource is read as a resolve source.
    /// </summary>
    ResolveRead = 1 << 11,

    /// <summary>
    ///   The resource is written as a resolve destination.
    /// </summary>
    ResolveWrite = 1 << 12,

    /// <summary>
    ///   The resource is used for presentation.
    /// </summary>
    Present = 1 << 13,
}
