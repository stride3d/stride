// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Identifies a technique for resolving Texture coordinates that are outside
///   of the boundaries of a Texture (outside the [0, 1] range).
/// </summary>
[DataContract("TextureAddressMode")]
public enum TextureAddressMode
{
    /// <summary>
    ///   Tile the Texture at every (u,v) integer junction.
    ///   For example, for u values between 0 and 3, the Texture is repeated three times.
    /// </summary>
    Wrap = 1,

    /// <summary>
    ///   Flip the Texture at every (u,v) integer junction.
    ///   For u values between 0 and 1, for example, the Texture is addressed normally;
    ///   between 1 and 2, the Texture is flipped (mirrored);
    ///   between 2 and 3, the Texture is normal again; and so on.
    /// </summary>
    Mirror = 2,

    /// <summary>
    ///   Texture coordinates outside the range [0, 1] are set to the Texture color at 0 or 1, respectively.
    /// </summary>
    Clamp = 3,

    /// <summary>
    ///   Texture coordinates outside the range [0, 1] are set to the border color specified in
    ///   <see cref="SamplerState"/> or HLSL code.
    /// </summary>
    Border = 4,

    /// <summary>
    ///   Similar to <see cref="Mirror"/> and <see cref="Clamp"/>.
    ///   Takes the absolute value of the Texture coordinate (thus, mirroring around 0), and then
    ///   clamps to the maximum value.
    /// </summary>
    MirrorOnce = 5
}
