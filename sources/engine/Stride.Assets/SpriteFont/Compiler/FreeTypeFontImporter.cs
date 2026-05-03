// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

using Stride.Core;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    // Rasterizes TrueType/OpenType glyphs using FreeType (pinned native binary in deps/).
    // This replaces the previous DirectWrite-based implementation so that static font
    // compilation is OS-independent and produces identical output across machines.
    internal unsafe class FreeTypeFontImporter : IFontImporter
    {
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }

        public float BaseLine { get; private set; }

        public void Import(SpriteFontAsset options, List<char> characters)
        {
            NativeLibraryHelper.PreloadLibrary("freetype", typeof(FreeTypeFontImporter));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
                throw new InvalidOperationException($"Failed to initialize FreeType library (error {err})");

            try
            {
                var fontPath = options.FontSource.GetFontPath();
                if (string.IsNullOrEmpty(fontPath) || !File.Exists(fontPath))
                    throw new FileNotFoundException($"Font file not found: {fontPath}");

                var fontData = File.ReadAllBytes(fontPath);
                var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

                try
                {
                    FT_FaceRec* face;
                    fixed (byte* ptr = fontData)
                    {
                        err = FreeTypeNative.FT_New_Memory_Face(library, ptr, new CLong(fontData.Length), new CLong(0), out face);
                        if (err != 0)
                            throw new InvalidOperationException($"Failed to load font '{fontPath}' (FreeType error {err})");
                    }

                    try
                    {
                        var fontSize = options.FontType.Size;

                        // Set font size: 26.6 fixed-point, 72 dpi so size is in pixels
                        var charSize = new CLong((int)(fontSize * 64));
                        FreeTypeNative.FT_Set_Char_Size(face, charSize, charSize, 72, 72);

                        // Compute line spacing and baseline using FreeType metrics
                        // These are in font units — convert to pixels
                        float unitsToPixels = fontSize / face->units_per_EM;
                        var lineGap = (face->height - face->ascender + face->descender) * options.LineGapFactor;
                        LineSpacing = (lineGap + face->ascender - face->descender) * unitsToPixels;
                        BaseLine = (lineGap * options.LineGapBaseLineFactor + face->ascender) * unitsToPixels;

                        var glyphList = new List<Glyph>();
                        foreach (var character in characters)
                            glyphList.Add(ImportGlyph(face, character, fontSize, options.FontType.AntiAlias));

                        Glyphs = glyphList;
                    }
                    finally
                    {
                        FreeTypeNative.FT_Done_Face(face);
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            finally
            {
                FreeTypeNative.FT_Done_FreeType(library);
            }
        }

        private Glyph ImportGlyph(FT_FaceRec* face, char character, float fontSize, FontAntiAliasMode antiAliasMode)
        {
            var glyphIndex = FreeTypeNative.FT_Get_Char_Index(face, character);

            // Whitespace or missing glyph
            if (glyphIndex == 0 || char.IsWhiteSpace(character))
            {
                // Still need advance width for whitespace
                if (glyphIndex != 0)
                {
                    var loadTarget = antiAliasMode == FontAntiAliasMode.Aliased
                        ? FreeTypeLoadTarget.Mono
                        : FreeTypeLoadTarget.Normal;
                    FreeTypeNative.FT_Load_Glyph(face, glyphIndex, (int)FreeTypeLoadFlags.Default | (int)loadTarget);
                }

                var emptyBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
                var glyph = new Glyph(character, emptyBitmap)
                {
                    XOffset = 0,
                    YOffset = 0,
                    XAdvance = glyphIndex != 0 ? face->glyph->advance.x.Value / 64.0f : 0,
                };
                return glyph;
            }

            // Load and render the glyph
            {
                var loadTarget = antiAliasMode == FontAntiAliasMode.Aliased
                    ? FreeTypeLoadTarget.Mono
                    : FreeTypeLoadTarget.Normal;
                int err = FreeTypeNative.FT_Load_Glyph(face, glyphIndex, (int)FreeTypeLoadFlags.Default | (int)loadTarget);
                if (err != 0)
                    throw new InvalidOperationException($"Failed to load glyph for character '{character}' (FreeType error {err})");

                var renderMode = antiAliasMode == FontAntiAliasMode.Aliased
                    ? FreeTypeRenderMode.Mono
                    : FreeTypeRenderMode.Normal;
                err = FreeTypeNative.FT_Render_Glyph(face->glyph, renderMode);
                if (err != 0)
                    throw new InvalidOperationException($"Failed to render glyph for character '{character}' (FreeType error {err})");
            }

            ref FT_Bitmap ftBitmap = ref face->glyph->bitmap;
            int width = (int)ftBitmap.width;
            int height = (int)ftBitmap.rows;

            if (width == 0 || height == 0)
            {
                var emptyBitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
                return new Glyph(character, emptyBitmap)
                {
                    XOffset = face->glyph->bitmap_left,
                    YOffset = -face->glyph->bitmap_top,
                    XAdvance = face->glyph->advance.x.Value / 64.0f,
                };
            }

            var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var pixelMode = (FreeTypePixelMode)ftBitmap.pixel_mode;

            // Copy FreeType bitmap to GDI+ bitmap
            // Output format: grey value in R/G/B channels (ConvertGreyToAlpha handles the rest)
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    byte grey;
                    if (pixelMode == FreeTypePixelMode.Mono)
                    {
                        // 1-bit monochrome: each byte contains 8 pixels
                        int byteIndex = x / 8;
                        int bitIndex = 7 - (x % 8);
                        byte b = ftBitmap.buffer[y * ftBitmap.pitch + byteIndex];
                        grey = ((b >> bitIndex) & 1) != 0 ? (byte)255 : (byte)0;
                    }
                    else
                    {
                        // 8-bit grayscale
                        grey = ftBitmap.buffer[y * ftBitmap.pitch + x];
                    }

                    bitmap.SetPixel(x, y, Color.FromArgb(grey, grey, grey));
                }
            }

            return new Glyph(character, bitmap)
            {
                XOffset = face->glyph->bitmap_left,
                YOffset = -face->glyph->bitmap_top,
                XAdvance = face->glyph->advance.x.Value / 64.0f,
            };
        }
    }
}
