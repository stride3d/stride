// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    using System.Drawing;
    using System.Drawing.Imaging;

    internal unsafe class SignedDistanceFieldFontImporter : IFontImporter
    {
        // Properties hold the imported font data.
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }

        public float BaseLine { get; private set; }

        private string fontSource;
        private string msdfgenExe;
#if DEBUG
        private string tempDir;
#endif

        /// <summary>
        /// Generates and load a SDF font glyph using the msdfgen.exe
        /// </summary>
        private Bitmap LoadSDFBitmap(char c, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
        {
            try
            {
                var characterCodeArg = "0x" + Convert.ToUInt32(c).ToString("x4");
#if DEBUG
                var outputFilePath = $"{tempDir}{characterCodeArg}_{Guid.NewGuid()}.bmp";
#else
                var outputFilePath = Path.GetTempFileName();
#endif
                var exportSizeArg = $"-size {width} {height}";
                var translateArg = $"-translate {offsetX} {offsetY}";
                var scaleArg = $"-ascale {scaleX} {scaleY}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = msdfgenExe,
                    Arguments = $"msdf -font \"{fontSource}\" {characterCodeArg} -o \"{outputFilePath}\" -format bmp {exportSizeArg} {translateArg} {scaleArg}",
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false
                };
                var msdfgenProcess = Process.Start(startInfo);

                if (msdfgenProcess == null)
                    return null;

                msdfgenProcess.WaitForExit();

                if (File.Exists(outputFilePath))
                {
                    var bitmap = (Bitmap)Image.FromFile(outputFilePath);

                    Normalize(bitmap);

                    return bitmap;
                }
            }
            catch
            {
                // ignore exception
            }

            // If font generation failed for any reason, ignore it and return an empty glyph
            return new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Inverts the color channels if the signed distance appears to be negative.
        /// Msdfgen will produce an inverted picture on occasion.
        /// Because we use offset we can easily detect if the corner pixel has negative (correct) or positive distance (incorrect)
        /// </summary>
        private void Normalize(Bitmap bitmap)
        {
            // Case 1 - corner pixel is negative (outside), do not invert
            var firstPixel = bitmap.GetPixel(0, 0);
            var colorChannels = 0;
            if (firstPixel.R > 0) colorChannels++;
            if (firstPixel.G > 0) colorChannels++;
            if (firstPixel.B > 0) colorChannels++;
            if (colorChannels <= 1)
                return;

            // Case 2 - corner pixel is positive (inside), invert the image
            for (var i = 0; i < bitmap.Width; i++)
                for (var j = 0; j < bitmap.Height; j++)
                {
                    var pixel = bitmap.GetPixel(i, j);

                    int invertR = ((int)255 - pixel.R);
                    int invertG = ((int)255 - pixel.G);
                    int invertB = ((int)255 - pixel.B);
                    var invertedPixel = Color.FromArgb((invertR << 16) + (invertG << 8) + (invertB));

                    bitmap.SetPixel(i, j, invertedPixel);
                }
        }

        /// <inheritdoc/>
        public void Import(SpriteFontAsset options, List<char> characters)
        {
            fontSource = options.FontSource.GetFontPath();
            if (string.IsNullOrEmpty(fontSource))
                return;

            // Get the msdfgen.exe location
            var msdfgen = ToolLocator.LocateTool("msdfgen") ?? throw new AssetException("Failed to compile a font asset, msdfgen was not found.");

            msdfgenExe = msdfgen.FullPath;
#if DEBUG
            tempDir = $"{Environment.GetEnvironmentVariable("TEMP")}\\StrideGlyphs\\";
            Directory.CreateDirectory(tempDir);
#endif

            NativeLibraryHelper.PreloadLibrary("freetype", typeof(SignedDistanceFieldFontImporter));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
                throw new InvalidOperationException($"Failed to initialize FreeType library (error {err})");

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
                return new Glyph(character, new Bitmap(1, 1, PixelFormat.Format32bppArgb))
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

            Bitmap bitmap;
            if (char.IsWhiteSpace(character))
            {
                bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
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
}
