// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Stride.Graphics.Font;

/// <summary>
/// Provides runtime registration and loading of fonts from the file system.
/// </summary>
public class RuntimeFontProvider
{
    private readonly FontSystem fontSystem;
    private readonly Dictionary<string, RuntimeFontInfo> registeredFonts = [];

    internal RuntimeFontProvider(FontSystem fontSystem)
    {
        this.fontSystem = fontSystem;
    }

    /// <summary>
    /// Registers a font file for runtime loading.
    /// </summary>
    /// <param name="fontName">The name to use when loading the font (e.g., "MyFont").</param>
    /// <param name="filePath">The absolute or relative path to the .ttf file.</param>
    /// <param name="style">The font style.</param>
    public void RegisterFont(string fontName, string filePath, FontStyle style = FontStyle.Regular)
    {
        if (string.IsNullOrEmpty(fontName))
            throw new ArgumentNullException(nameof(fontName));

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: {filePath}", filePath);

        var key = GetFontKey(fontName, style);
        registeredFonts[key] = new RuntimeFontInfo(fontName, filePath, style);

        // Preload into FontManager's cache
        fontSystem.FontManager.LoadFontFromFileSystem(fontName, filePath, style);
    }

    /// <summary>
    /// Checks if a font is registered for runtime loading.
    /// </summary>
    /// <param name="fontName">The font name.</param>
    /// <param name="style">The font style.</param>
    /// <returns>True if registered, false otherwise.</returns>
    public bool IsRegistered(string fontName, FontStyle style = FontStyle.Regular)
    {
        return registeredFonts.ContainsKey(GetFontKey(fontName, style));
    }

    private static string GetFontKey(string fontName, FontStyle style)
    {
        return $"{fontName}_{style}";
    }

    private record RuntimeFontInfo(string FontName, string FilePath, FontStyle Style);
}
