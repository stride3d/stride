// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   The kinds of capability a renderer can declare it needs.
/// </summary>
/// <remarks>
///   A backend declares the kinds it implements, and that declaration holds for every device it runs on.
///   Whether the user's device provides one of them is a separate question, and it is asked of a
///   <see cref="GraphicsCapability"/> rather than of a kind, because an instance can carry the detail
///   the device needs to answer.
/// </remarks>
public enum GraphicsCapabilityKind
{
    /// <summary>
    ///   Compute shaders, and unordered access on structured and raw buffers.
    /// </summary>
    ComputeShaders,

    /// <summary>
    ///   Double precision operations in shaders.
    /// </summary>
    DoublePrecision,

    /// <summary>
    ///   Reading the depth buffer as a shader resource.
    /// </summary>
    DepthAsShaderResource,

    /// <summary>
    ///   Reading a multisampled depth buffer as a shader resource.
    /// </summary>
    MultisampleDepthAsShaderResource,

    /// <summary>
    ///   Renaming a resource on map or full update, rather than waiting for the GPU.
    /// </summary>
    ResourceRenaming,

    /// <summary>
    ///   sRGB textures and render targets.
    /// </summary>
    SRgb,

    /// <summary>
    ///   Multisampled rendering. The device answers per pixel format and sample count, so this is the
    ///   one kind whose instances differ from one another.
    /// </summary>
    Multisampling,

    /// <summary>
    ///   Index buffers holding 32-bit indices.
    /// </summary>
    Index32Bits
}
