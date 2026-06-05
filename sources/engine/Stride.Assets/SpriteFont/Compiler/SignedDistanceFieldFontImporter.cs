// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Stride.Core;
using Stride.Core.Assets;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    internal unsafe class SignedDistanceFieldFontImporter : IFontImporter
    {
        // Matches msdfgen CLI default range when no -range / -pxrange argument is passed.
        private const double MsdfgenRange = 4.0;

        // Properties hold the imported font data.
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }

        public float BaseLine { get; private set; }

        private string fontSource;
        private IntPtr msdfgenContext;
        private IntPtr msdfgenFont;

        private Image<Rgba32> LoadSDFBitmap(char c, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
        {
            try
            {
                var rgba = new byte[width * height * 4];
                int rc;
                fixed (byte* outRgba = rgba)
                {
                    rc = MsdfgenNative.msdfgenGenerateMsdf(
                        msdfgenFont,
                        unicode: c,
                        width, height,
                        offsetX, offsetY,
                        scaleX, scaleY,
                        MsdfgenRange,
                        outRgba);
                }

                if (rc != 0)
                    return new Image<Rgba32>(1, 1);

                var bitmap = Image.LoadPixelData<Rgba32>(rgba, width, height);
                Normalize(bitmap);
                return bitmap;
            }
            catch
            {
                // ignore exception
            }

            // If font generation failed for any reason, ignore it and return an empty glyph
            return new Image<Rgba32>(1, 1);
        }

        /// <summary>
        /// Inverts the color channels if the signed distance appears to be negative.
        /// Msdfgen will produce an inverted picture on occasion.
        /// Because we use offset we can easily detect if the corner pixel has negative (correct) or positive distance (incorrect)
        /// </summary>
        private void Normalize(Image<Rgba32> bitmap)
        {
            // Case 1 - corner pixel is negative (outside), do not invert
            var firstPixel = bitmap[0, 0];
            var colorChannels = 0;
            if (firstPixel.R > 0) colorChannels++;
            if (firstPixel.G > 0) colorChannels++;
            if (firstPixel.B > 0) colorChannels++;
            if (colorChannels <= 1)
                return;

            // Case 2 - corner pixel is positive (inside), invert the image.
            // Note: alpha is forced to 0 here to match the previous GDI+ behavior
            // (Color.FromArgb(int) with no alpha bits zeros the alpha channel).
            for (var i = 0; i < bitmap.Width; i++)
                for (var j = 0; j < bitmap.Height; j++)
                {
                    var pixel = bitmap[i, j];

                    bitmap[i, j] = new Rgba32((byte)(255 - pixel.R), (byte)(255 - pixel.G), (byte)(255 - pixel.B), (byte)0);
                }
        }

        /// <inheritdoc/>
        public void Import(SpriteFontAsset options, List<char> characters)
        {
            fontSource = options.FontSource.GetFontPath();
            if (string.IsNullOrEmpty(fontSource))
                return;

            NativeLibraryHelper.PreloadLibrary("freetype", typeof(SignedDistanceFieldFontImporter));
            NativeLibraryHelper.PreloadLibrary("stride_msdfgen", typeof(SignedDistanceFieldFontImporter));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
                throw new InvalidOperationException($"Failed to initialize FreeType library (error {err})");

            msdfgenContext = MsdfgenNative.msdfgenContextCreate();
            if (msdfgenContext == IntPtr.Zero)
            {
                FreeTypeNative.FT_Done_FreeType(library);
                throw new InvalidOperationException("Failed to initialize msdfgen context");
            }

            msdfgenFont = MsdfgenNative.msdfgenLoadFont(msdfgenContext, fontSource);
            if (msdfgenFont == IntPtr.Zero)
            {
                MsdfgenNative.msdfgenContextDestroy(msdfgenContext);
                msdfgenContext = IntPtr.Zero;
                FreeTypeNative.FT_Done_FreeType(library);
                throw new AssetException($"Failed to load font '{fontSource}' into msdfgen.");
            }

            try
            {
                var fontData = File.ReadAllBytes(fontSource);
                var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

                try
                {
                    FT_FaceRec* face;
                    fixed (byte* ptr = fontData)
                    {
                        err = FreeTypeNative.FT_New_Memory_Face(library, ptr, new CLong(fontData.Length), new CLong(0), out face);
                        if (err != 0)
                            throw new InvalidOperationException($"Failed to load font '{fontSource}' (FreeType error {err})");
                    }

                    try
                    {
                        var fontSize = options.FontType.Size;

                        // FreeType metrics are in font units; convert to pixels
                        float unitsToPixels = fontSize / face->units_per_EM;

                        var lineGap = (face->height - face->ascender + face->descender) * options.LineGapFactor;
                        LineSpacing = (lineGap + face->ascender - face->descender) * unitsToPixels;
                        BaseLine = (lineGap * options.LineGapBaseLineFactor + face->ascender) * unitsToPixels;

                        // Set font size for glyph metric queries (26.6 fixed-point, 72 dpi)
                        var charSize = new CLong((int)(fontSize * 64));
                        FreeTypeNative.FT_Set_Char_Size(face, charSize, charSize, 72, 72);

                        var glyphList = new List<Glyph>();
                        foreach (var character in characters)
                            glyphList.Add(ImportGlyph(face, character, fontSize));

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
                MsdfgenNative.msdfgenUnloadFont(msdfgenFont);
                msdfgenFont = IntPtr.Zero;
                MsdfgenNative.msdfgenContextDestroy(msdfgenContext);
                msdfgenContext = IntPtr.Zero;
                FreeTypeNative.FT_Done_FreeType(library);
            }
        }

        /// <summary>
        /// Imports a single glyph as a bitmap using the msdfgen to convert it to a signed distance field image
        /// </summary>
        private Glyph ImportGlyph(FT_FaceRec* face, char character, float fontSize)
        {
            var glyphIndex = FreeTypeNative.FT_Get_Char_Index(face, character);

            // Load glyph to get metrics (no rendering needed — msdfgen does that)
            if (glyphIndex == 0 || FreeTypeNative.FT_Load_Glyph(face, glyphIndex, (int)FreeTypeLoadFlags.Default) != 0)
            {
                return new Glyph(character, new Image<Rgba32>(1, 1))
                {
                    XOffset = 0, YOffset = 0, XAdvance = 0,
                };
            }

            ref FT_Glyph_Metrics metrics = ref face->glyph->metrics;

            // FreeType glyph metrics are in 26.6 fixed-point
            float horiBearingX = metrics.horiBearingX.Value / 64.0f;
            float horiBearingY = metrics.horiBearingY.Value / 64.0f;
            float glyphWidth = metrics.width.Value / 64.0f;
            float glyphHeight = metrics.height.Value / 64.0f;
            float advanceWidth = metrics.horiAdvance.Value / 64.0f;

            float fontOffsetXPx = horiBearingX;
            float fontOffsetYPx = -horiBearingY; // FreeType Y-up to screen Y-down

            const int MarginPx = 2;     // Buffer zone for the sdf image to avoid clipping
            int bitmapWidthPx = (int)Math.Ceiling(glyphWidth) + (2 * MarginPx);
            int bitmapHeightPx = (int)Math.Ceiling(glyphHeight) + (2 * MarginPx);

            float bitmapOffsetXPx = fontOffsetXPx - MarginPx;
            float bitmapOffsetYPx = fontOffsetYPx - MarginPx;

            Image<Rgba32> bitmap;
            if (char.IsWhiteSpace(character))
            {
                bitmap = new Image<Rgba32>(1, 1);
            }
            else
            {
                // msdfgen uses its own coordinate system (1/64 design units by default)
                // https://github.com/stride3d/msdfgen/blob/1af188c77822e447fe8e412420fe0fe05b782b38/ext/import-font.cpp#L126-L150
                const float sdfPixelPerDesignUnit = 1 / 64f;

                // Convert FreeType 26.6 metrics back to font design units for msdfgen
                float designHoriBearingX = metrics.horiBearingX.Value / 64.0f / (fontSize / face->units_per_EM);
                float designVertOriginY = horiBearingY / (fontSize / face->units_per_EM); // approximate
                float designAdvanceHeight = (glyphHeight + (horiBearingY - fontOffsetYPx - glyphHeight)) / (fontSize / face->units_per_EM);
                float designBottomBearing = 0; // approximate — not critical for SDF

                float boundLeft = designHoriBearingX * sdfPixelPerDesignUnit;
                float boundBottom = (designVertOriginY - designAdvanceHeight + designBottomBearing) * sdfPixelPerDesignUnit;

                float sdfGlyphWidth = glyphWidth / (fontSize / face->units_per_EM) * sdfPixelPerDesignUnit;
                float sdfGlyphHeight = glyphHeight / (fontSize / face->units_per_EM) * sdfPixelPerDesignUnit;

                // Need to scale from msdfgen's 'shape unit' into the final bitmap's space
                float scaleX = sdfGlyphWidth != 0 ? glyphWidth / sdfGlyphWidth : 1;
                float scaleY = sdfGlyphHeight != 0 ? glyphHeight / sdfGlyphHeight : 1;

                // Note: msdfgen uses coordinates from bottom-left corner
                float offsetX = (MarginPx / scaleX) - boundLeft;
                float offsetY = ((bitmapHeightPx - MarginPx) / scaleY) - sdfGlyphHeight - boundBottom;

                bitmap = LoadSDFBitmap(character, bitmapWidthPx, bitmapHeightPx, offsetX, offsetY, scaleX, scaleY);
            }

            return new Glyph(character, bitmap)
            {
                XOffset = bitmapOffsetXPx,
                XAdvance = advanceWidth,
                YOffset = bitmapOffsetYPx,
            };
        }
    }

    internal static unsafe class MsdfgenNative
    {
        private const string Lib = "stride_msdfgen";

        [DllImport(Lib)]
        public static extern IntPtr msdfgenContextCreate();

        [DllImport(Lib)]
        public static extern void msdfgenContextDestroy(IntPtr ctx);

        [DllImport(Lib)]
        public static extern IntPtr msdfgenLoadFont(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path);

        [DllImport(Lib)]
        public static extern void msdfgenUnloadFont(IntPtr font);

        [DllImport(Lib)]
        public static extern int msdfgenGenerateMsdf(
            IntPtr font,
            uint unicode,
            int width, int height,
            double translateX, double translateY,
            double scaleX, double scaleY,
            double range,
            byte* outRgba);
    }
}
