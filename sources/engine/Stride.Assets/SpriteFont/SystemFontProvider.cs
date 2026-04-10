// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using SharpDX.DirectWrite;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Core;
using Stride.Core.Assets.Compiler;
using Stride.Core.Diagnostics;
using Stride.Graphics.Font;
using System;
using System.Linq;

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

        /// <inheritdoc/>
        public override FontFace GetFontFace()
        {
            using var factory = new Factory();

            Font font;
            using (var fontCollection = factory.GetSystemFontCollection(false))
            {
                if (!fontCollection.FindFamilyName(FontName, out var index))
                {
                    // Lets try to import System.Drawing for old system bitmap fonts (like MS Sans Serif)
                    throw new FontNotFoundException(FontName);
                }

                using var fontFamily = fontCollection.GetFontFamily(index);
                var weight = Style.IsBold() ? FontWeight.Bold : FontWeight.Regular;
                var style = Style.IsItalic() ? SharpDX.DirectWrite.FontStyle.Italic : SharpDX.DirectWrite.FontStyle.Normal;
                font = fontFamily.GetFirstMatchingFont(weight, FontStretch.Normal, style);
            }

            return new FontFace(font);
        }

        public override string GetFontPath(AssetCompilerResult result = null)
        {
            if (OperatingSystem.IsWindows())
                return GetFontPathWindows(result);
            if (OperatingSystem.IsLinux())
            {
                var fontPath = GetFontPathLinux(result);
                var defaultFont = GetDefaultFontName();
                if (fontPath == null && FontName != defaultFont)
                {
                    result?.Warning($"Cannot find font family '{FontName}'. Loading default font '{defaultFont}' instead");
                    FontName = defaultFont;
                    fontPath = GetFontPathLinux(result);
                }
                return fontPath;
            }
            return null;
        }

        private unsafe string GetFontPathLinux(AssetCompilerResult result)
        {
            bool wantBold = Style.IsBold();
            bool wantItalic = Style.IsItalic();

            string systemFontDirectory = "/usr/share/fonts";
            var files = System.IO.Directory.EnumerateFiles(systemFontDirectory, "*.ttf", System.IO.SearchOption.AllDirectories);

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
            {
                result?.Warning("Failed to initialize FreeType library for font discovery.");
                return null;
            }

            try
            {
                foreach (string file in files)
                {
                    var fontData = System.IO.File.ReadAllBytes(file);
                    fixed (byte* ptr = fontData)
                    {
                        err = FreeTypeNative.FT_New_Memory_Face(library, ptr, new System.Runtime.InteropServices.CLong(fontData.Length), new System.Runtime.InteropServices.CLong(0), out FT_FaceRec* face);
                        if (err != 0)
                            continue;

                        var familyName = System.Runtime.InteropServices.Marshal.PtrToStringAnsi((nint)face->family_name) ?? "";
                        long styleFlags = (long)face->style_flags.Value;
                        bool isBold = (styleFlags & 0x2) != 0;  // FT_STYLE_FLAG_BOLD
                        bool isItalic = (styleFlags & 0x1) != 0; // FT_STYLE_FLAG_ITALIC

                        FreeTypeNative.FT_Done_Face(face);

                        if (familyName.Contains(FontName) && isBold == wantBold && isItalic == wantItalic)
                            return file;
                    }
                }
            }
            finally
            {
                FreeTypeNative.FT_Done_FreeType(library);
            }

            result?.Warning($"Cannot find style '{Style}' for font family '{FontName}'. Make sure it is installed on this machine.");
            return null;
        }

        private string GetFontPathWindows(AssetCompilerResult result)
        {
            using var factory = new Factory();
            Font font;

            using (var fontCollection = factory.GetSystemFontCollection(false))
            {
                if (!fontCollection.FindFamilyName(FontName, out var index))
                {
                    result?.Error($"Cannot find system font '{FontName}'. Make sure it is installed on this machine.");
                    return null;
                }

                using var fontFamily = fontCollection.GetFontFamily(index);
                var weight = Style.IsBold() ? FontWeight.Bold : FontWeight.Regular;
                var style = Style.IsItalic() ? SharpDX.DirectWrite.FontStyle.Italic : SharpDX.DirectWrite.FontStyle.Normal;
                font = fontFamily.GetFirstMatchingFont(weight, FontStretch.Normal, style);
                if (font == null)
                {
                    result?.Error($"Cannot find style '{Style}' for font family {FontName}. Make sure it is installed on this machine.");
                    return null;
                }
            }

            var fontFace = new FontFace(font);

            // get the font path on the hard drive
            var file = fontFace.GetFiles().First();
            var referenceKey = file.GetReferenceKey();
            var originalLoader = (FontFileLoaderNative)file.Loader;
            var loader = originalLoader.QueryInterface<LocalFontFileLoader>();
            return loader.GetFilePath(referenceKey);
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
