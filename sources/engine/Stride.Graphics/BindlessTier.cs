// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Describes how shaders can index a large array of textures (bindless access).
/// </summary>
/// <seealso cref="GraphicsDeviceFeatures.BindlessTier"/>
public enum BindlessTier
{
    /// <summary>
    ///   No bindless access. Resources are bound per slot.
    /// </summary>
    None = 0,

    /// <summary>
    ///   Large, partially bound texture arrays, indexed with a dynamically uniform index (the same for the whole draw).
    /// </summary>
    Uniform = 1,

    /// <summary>
    ///   Large, partially bound texture arrays, indexed with an index that can differ per invocation
    ///   (<c>NonUniformResourceIndex</c>). Needed when one draw or dispatch reads many different resources.
    /// </summary>
    NonUniform = 2,
}
