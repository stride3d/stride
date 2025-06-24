// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public struct TextureViewDescription
{
    /// <summary>
    /// View description of a <see cref="Texture"/>.
    /// </summary>
    public TextureFlags Flags;

    public PixelFormat Format;

    public ViewType Type;

    public int ArraySlice;

    public int MipLevel;

    public readonly TextureViewDescription ToStagingDescription()
    {
        return this with { Flags = TextureFlags.None };
    }
}
