// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Assets.SpriteFont.Compiler;
using Stride.Core;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Graphics.Font;
using System;
using System.Collections.Generic;
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
            string[] searchDirs;
            if (OperatingSystem.IsWindows())
                searchDirs = [Environment.GetFolderPath(Environment.SpecialFolder.Fonts)];
            else if (OperatingSystem.IsLinux())
                searchDirs = ["/usr/share/fonts", "/usr/local/share/fonts", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local/share/fonts")];
            else if (OperatingSystem.IsMacOS())
                // Modern macOS (Catalina+) keeps most system fonts under /System/Library/Fonts, with the
                // bundled "extras" like Arial under the Supplemental subfolder. /Library/Fonts is largely
                // empty on stock installs; ~/Library/Fonts holds user-installed fonts.
                searchDirs = [Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library/Fonts"), "/Library/Fonts", "/System/Library/Fonts"];
            else
                searchDirs = [];

            // Try the requested font, then its metric-compatible equivalents. The Liberation family
            // ships metric-identical to the Microsoft core fonts (Arial/Times New Roman/Courier New),
            // so substituting whichever member a platform actually has keeps text layout — and the
            // screenshot goldens that depend on it — consistent across Windows/Linux/macOS.
            foreach (var candidate in GetFontNameCandidates(FontName))
            {
                var fontPath = ResolveFont(candidate, searchDirs);
                if (fontPath == null)
                    continue;
                if (!string.Equals(candidate, FontName, StringComparison.OrdinalIgnoreCase))
                {
                    result?.Warning($"Cannot find font family '{FontName}'. Using metric-compatible fallback '{candidate}' instead.");
                    FontName = candidate;
                }
                return fontPath;
            }

            result?.Error($"Cannot find system font '{FontName}' or a metric-compatible fallback. Make sure it is installed on this machine.");
            return null;
        }

        // Exact-match pass across every directory before partial matches, so a partial hit in an
        // earlier directory doesn't shadow an exact hit in a later one.
        private string ResolveFont(string fontName, string[] searchDirs)
        {
            foreach (var dir in searchDirs)
            {
                var path = FindFontInDirectory(dir, fontName, exactMatch: true);
                if (path != null)
                    return path;
            }
            foreach (var dir in searchDirs)
            {
                var path = FindFontInDirectory(dir, fontName, exactMatch: false);
                if (path != null)
                    return path;
            }
            return null;
        }

        /// <summary>
        /// The requested font name followed by its metric-compatible equivalents. The three groups
        /// (sans / serif / mono) are mutually metric-identical — Liberation is built to match the MS
        /// core fonts — so the lookup is bidirectional: an asset naming <c>Arial</c> resolves to
        /// Liberation Sans on Linux, and one naming <c>Liberation Sans</c> resolves to Arial on Windows.
        /// </summary>
        internal static IEnumerable<string> GetFontNameCandidates(string requested)
        {
            yield return requested;

            var key = requested?.Trim().ToLowerInvariant();
            string[] group = key switch
            {
                "times new roman" or "times" or "liberation serif" or "tinos" or "georgia" or "dejavu serif"
                    => ["Times New Roman", "Liberation Serif", "DejaVu Serif"],
                "courier new" or "courier" or "liberation mono" or "cousine" or "consolas" or "dejavu sans mono"
                    => ["Courier New", "Liberation Mono", "DejaVu Sans Mono"],
                // sans-serif: Arial, Helvetica, Liberation Sans, Segoe UI, Tahoma, Verdana, ...
                _ => ["Arial", "Liberation Sans", "DejaVu Sans"],
            };

            foreach (var candidate in group)
                if (!string.Equals(candidate, requested, StringComparison.OrdinalIgnoreCase))
                    yield return candidate;
        }

        private unsafe string FindFontInDirectory(string directory, string fontName, bool exactMatch)
        {
            if (!Directory.Exists(directory))
                return null;

            bool wantBold = Style.IsBold();
            bool wantItalic = Style.IsItalic();

            NativeLibraryHelper.PreloadLibrary("freetype", typeof(SystemFontProvider));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
                return null;

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

                        // Partial match (e.g. "Liberation" matches "Liberation Sans") is the looser fallback.
                        bool nameMatches = exactMatch
                            ? string.Equals(familyName, fontName, StringComparison.OrdinalIgnoreCase)
                            : familyName.Contains(fontName, StringComparison.OrdinalIgnoreCase);
                        if (nameMatches && isBold == wantBold && isItalic == wantItalic)
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
            return OperatingSystem.IsLinux() ? "Liberation Sans" : "Arial";
        }
    }
}
