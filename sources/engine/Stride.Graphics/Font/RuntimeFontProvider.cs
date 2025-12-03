// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Stride.Graphics.Font;

/// <summary>
/// Provides runtime registration and loading of fonts from the file system.
/// </summary>
/// <remarks>
/// <para>This provider allows loading custom TrueType fonts (.ttf) at runtime without going through
/// the content pipeline. Fonts registered through this provider are immediately available for use
/// with <see cref="FontSystem.LoadRuntimeFont"/>.</para>
/// <para><strong>Memory Management:</strong> All registered fonts remain in memory for the lifetime
/// of the application. There is no mechanism to unload individual fonts once registered. For applications
/// with dynamic font requirements or memory constraints, consider the total memory footprint of all
/// registered fonts.</para>
/// </remarks>
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
    /// <remarks>
    /// <para>Once registered, fonts are loaded into memory and cached for the lifetime of the font system.
    /// Individual fonts cannot be unregistered or unloaded - they remain in memory until the application exits
    /// or the font system is disposed.</para>
    /// <para>Attempting to register the same font name and style with a different file path will throw an exception.</para>
    /// </remarks>
    /// <param name="fontName">The name to use when loading the font (e.g., "MyFont").</param>
    /// <param name="filePath">The absolute or relative path to the .ttf file.</param>
    /// <param name="style">The font style.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fontName"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the font file does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to register the same font name and style with a different file path.</exception>
    public void RegisterFont(string fontName, string filePath, FontStyle style = FontStyle.Regular)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fontName);

        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Font file not found: {filePath}", filePath);

        var key = FontHelper.GetFontPath(fontName, style);

        if (registeredFonts.TryGetValue(key, out var existing))
        {
            if (existing.FilePath == filePath) return;

            throw new InvalidOperationException(
                $"Font '{fontName}' with style '{style}' is already registered with path '{existing.FilePath}'. " +
                $"Cannot register a different path '{filePath}' for the same font name and style.");
        }

        fontSystem.FontManager.LoadFontFromFileSystem(fontName, filePath, style);

        registeredFonts[key] = new RuntimeFontInfo(fontName, filePath, style);
    }

    /// <summary>
    /// Checks if a font is registered for runtime loading.
    /// </summary>
    /// <param name="fontName">The font name.</param>
    /// <param name="style">The font style.</param>
    /// <returns>True if registered, false otherwise.</returns>
    public bool IsRegistered(string fontName, FontStyle style = FontStyle.Regular)
    {
        return registeredFonts.ContainsKey(FontHelper.GetFontPath(fontName, style));
    }

    private record RuntimeFontInfo(string FontName, string FilePath, FontStyle Style);
}
