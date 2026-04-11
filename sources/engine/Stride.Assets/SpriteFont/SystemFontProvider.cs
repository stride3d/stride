// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.SpriteFont.Compiler;
using Stride.Core;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Graphics.Font;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Stride.Assets.SpriteFont
{
    [DataContract("SystemFontProvider")]
    [Display("System Font")]
    public class SystemFontProvider : FontProviderBase
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("SystemFontProvider");

        public SystemFontProvider()
        {
            FontName = GetDefaultFontName();
        }

        public SystemFontProvider(string fontName)
        {
            FontName = fontName;
        }

        /// <summary>
        /// Gets or sets the name of the font family to use when the <see cref="Source"/> is not specified.
        /// </summary>
        /// <userdoc>
        /// The name of the font family to use. Only the fonts installed on the system can be used here.
        /// </userdoc>
        [DataMember(20)]
        [Display("Font Name")]
        public string FontName { get; set; }

        /// <summary>
        /// Gets or sets the style of the font. A combination of 'regular', 'bold', 'italic'. Default is 'regular'.
        /// </summary>
        /// <userdoc>
        /// The style of the font (regular / bold / italic). Note that this property is ignored is the desired style is not available in the font's source file.
        /// </userdoc>
        [DataMember(40)]
        [Display("Style")]
        public override Stride.Graphics.Font.FontStyle Style { get; set; } = Graphics.Font.FontStyle.Regular;

        public override string GetFontPath(AssetCompilerResult result = null)
        {
            string fontPath = null;

            if (OperatingSystem.IsWindows())
                fontPath = FindFontInDirectory(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), result);
            else if (OperatingSystem.IsLinux())
                fontPath = FindFontInDirectory("/usr/share/fonts", result);
            else if (OperatingSystem.IsMacOS())
                fontPath = FindFontInDirectory("/Library/Fonts", result);

            if (fontPath != null)
                return fontPath;

            // Fallback to default font
            var defaultFont = GetDefaultFontName();
            if (FontName != defaultFont)
            {
                result?.Warning($"Cannot find font family '{FontName}'. Loading default font '{defaultFont}' instead");
                FontName = defaultFont;
                return GetFontPath(result);
            }

            result?.Error($"Cannot find system font '{FontName}'. Make sure it is installed on this machine.");
            return null;
        }

        private unsafe string FindFontInDirectory(string directory, AssetCompilerResult result)
        {
            if (!Directory.Exists(directory))
                return null;

            bool wantBold = Style.IsBold();
            bool wantItalic = Style.IsItalic();

            NativeLibraryHelper.PreloadLibrary("freetype", typeof(SystemFontProvider));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
            {
                result?.Warning("Failed to initialize FreeType library for font discovery.");
                return null;
            }

            try
            {
                foreach (string file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".ttf" && ext != ".otf" && ext != ".ttc")
                        continue;

                    var fontData = File.ReadAllBytes(file);
                    fixed (byte* ptr = fontData)
                    {
                        err = FreeTypeNative.FT_New_Memory_Face(library, ptr, new CLong(fontData.Length), new CLong(0), out FT_FaceRec* face);
                        if (err != 0)
                            continue;

                        var familyName = Marshal.PtrToStringAnsi((nint)face->family_name) ?? "";
                        long styleFlags = (long)face->style_flags.Value;
                        bool isBold = (styleFlags & 0x2) != 0;  // FT_STYLE_FLAG_BOLD
                        bool isItalic = (styleFlags & 0x1) != 0; // FT_STYLE_FLAG_ITALIC

                        FreeTypeNative.FT_Done_Face(face);

                        if (string.Equals(familyName, FontName, StringComparison.OrdinalIgnoreCase)
                            && isBold == wantBold && isItalic == wantItalic)
                            return file;
                    }
                }

                // Second pass: partial match (e.g. "Liberation" matches "Liberation Sans")
                foreach (string file in Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories))
                {
                    var ext = Path.GetExtension(file).ToLowerInvariant();
                    if (ext != ".ttf" && ext != ".otf" && ext != ".ttc")
                        continue;

                    var fontData = File.ReadAllBytes(file);
                    fixed (byte* ptr = fontData)
                    {
                        err = FreeTypeNative.FT_New_Memory_Face(library, ptr, new CLong(fontData.Length), new CLong(0), out FT_FaceRec* face);
                        if (err != 0)
                            continue;

                        var familyName = Marshal.PtrToStringAnsi((nint)face->family_name) ?? "";
                        long styleFlags = (long)face->style_flags.Value;
                        bool isBold = (styleFlags & 0x2) != 0;
                        bool isItalic = (styleFlags & 0x1) != 0;

                        FreeTypeNative.FT_Done_Face(face);

                        if (familyName.Contains(FontName, StringComparison.OrdinalIgnoreCase)
                            && isBold == wantBold && isItalic == wantItalic)
                            return file;
                    }
                }
            }
            finally
            {
                FreeTypeNative.FT_Done_FreeType(library);
            }

            return null;
        }

        /// <inheritdoc/>
        public override string GetFontName()
        {
            return FontName;
        }

        private static string GetDefaultFontName()
        {
            //Note : Both macOS and Windows contains Arial
            return OperatingSystem.IsLinux() ? "Liberation" : "Arial";
        }
    }
}
