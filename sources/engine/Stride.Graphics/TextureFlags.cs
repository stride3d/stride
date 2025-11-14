// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics;

/// <summary>
///   Flags describing how a <see cref="Texture"/> can be bound to the graphics pipeline.
/// </summary>
[Flags]
public enum TextureFlags
{
    None = 0,

    /// <summary>
    ///   The Texture can be used as a Shader Resource View, i.e. it can be bound to a shader stage.
    /// </summary>
    ShaderResource = 1,

    /// <summary>
    ///   The Texture can be used as a Render Target View, i.e. it can be bound as the render target of the output-merger stage.
    /// </summary>
    RenderTarget = 2,   // TODO: This naming is ambiguous. A render target can be a depth, stencil or color buffer. But we use "RenderTarget" synonymously with "ColorTarget".

    /// <summary>
    ///   The Texture can be used as an Unordered Access resource.
    /// </summary>
    UnorderedAccess = 4,

    /// <summary>
    ///   The Texture can be used as a Depth-Stencil buffer, i.e. it can be bound as a render target where the output-merger stage
    ///   writes depth or stencil information.
    /// </summary>
    DepthStencil = 8,

    /// <summary>
    ///   The Texture can be used as a read-only Depth-Stencil buffer. This allows reading from it in a shader stage
    ///   even at the same time as it is bound as a Depth-Stencil Buffer in the output-merger stage.
    /// </summary>
    DepthStencilReadOnly = DepthStencil | Texture.DepthStencilReadOnlyFlags
}
