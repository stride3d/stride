// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using Stride.Editor.Preview;

namespace Stride.Assets.Editor.Preview;

/// <summary>
/// A preview of a font (<see cref="SpriteFontPreview"/>, <see cref="PrecompiledSpriteFontPreview"/>) that renders a string
/// with the font.
/// </summary>
public interface IFontPreview : IAssetPreview
{
    /// <summary>
    /// Sets a string to preview using the font. If the given string is null or empty, the preview displays the font texture
    /// instead.
    /// </summary>
    /// <param name="str">The string to preview.</param>
    void SetPreviewString(string? str);
}
