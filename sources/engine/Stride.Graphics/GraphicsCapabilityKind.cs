// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   The kinds of capability a renderer can declare it needs.
/// </summary>
/// <remarks>
///   A backend implements a kind or it does not. The device answers per capability instance, because
///   multisampling support depends on the pixel format and the sample count.
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
    ///   Multisampled rendering.
    /// </summary>
    Multisampling,

    /// <summary>
    ///   Index buffers holding 32-bit indices.
    /// </summary>
    Index32Bits
}
