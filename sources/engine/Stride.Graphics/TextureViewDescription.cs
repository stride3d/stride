// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Describes a View for a <see cref="Texture"/>.
/// </summary>
public struct TextureViewDescription
{
    /// <summary>
    ///   A combination of flags determining what kind of <see cref="Texture"/> the View is attached to and
    ///   how it should behave (i.e. how it is bound, how can it be read / written, etc.).
    ///   <list type="bullet">
    ///     <item>If this field is <see cref="TextureFlags.None"/>, the View is reusing the same flags as its parent Texture.</item>
    ///     <item>For any other value, this field overrides the flags of the parent Texture.</item>
    ///   </list>
    /// </summary>
    public TextureFlags Flags;

    /// <summary>
    ///   The pixel format of the View (used for the Shader Resource View or Unordered Access View).
    /// </summary>
    public PixelFormat Format;

    /// <summary>
    ///   A value of <see cref="ViewType"/> indicating which sub-resources the View can see (single mip, band, or full).
    /// </summary>
    public ViewType Type;

    /// <summary>
    ///   The index of the array slice.
    /// </summary>
    /// <remarks>
    ///   If the Texture is not a Texture Array, only a single slice is assumed, so this should be zero (i.e. the first index).
    /// </remarks>
    public int ArraySlice;

    /// <summary>
    ///   The index of the mip-level.
    /// </summary>
    /// <remarks>
    ///   If the Texture has a single mipmap, this should be zero (i.e. the first index).
    /// </remarks>
    public int MipLevel;


    /// <summary>
    ///   Returns a copy of this description modified to describe a Texture View that will be used for staging.
    /// </summary>
    /// <returns>A staging-compatible copy of the View description.</returns>
    public readonly TextureViewDescription ToStagingDescription()
    {
        return this with { Flags = TextureFlags.None };
    }
}
