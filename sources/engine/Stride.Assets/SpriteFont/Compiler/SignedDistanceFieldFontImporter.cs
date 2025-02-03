// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Stride.Core.Assets;

namespace Stride.Assets.SpriteFont.Compiler
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using FreeImageAPI;
    using SharpDX.DirectWrite;
    using SharpFont;
    using Factory = SharpDX.DirectWrite.Factory;

    // This code was originally taken from DirectXTk but rewritten with DirectWrite
    // for more accuracy in font rendering
    internal class SignedDistanceFieldFontImporter : IFontImporter
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
        /// <param name="c">Character code</param>
        /// <param name="width">Width of the output glyph</param>
        /// <param name="height">Height of the output glyph</param>
        /// <param name="offsetX">Left side offset of the glyph from the image border in design unit</param>
        /// <param name="offsetY">Bottom side offset of the glyph from the image border in design unit</param>
        /// <param name="scaleX">Scale factor to convert from 'shape unit' to 'pixel unit' on x-axis</param>
        /// <param name="scaleY">Scale factor to convert from 'shape unit' to 'pixel unit' on y-axis</param>
        /// <returns></returns>
        private FreeImageBitmap LoadSDFBitmap(char c, int width, int height, float offsetX, float offsetY, float scaleX, float scaleY)
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
                    var bitmap = FreeImageBitmap.FromFile(outputFilePath);

                    Normalize(bitmap);

                    return bitmap;
                }
            }
            catch
            {
                // ignore exception
            }

            // If font generation failed for any reason, ignore it and return an empty glyph
            return new FreeImageBitmap(1, 1, FreeImageAPI.PixelFormat.Format32bppArgb);
        }

        /// <summary>
        /// Inverts the color channels if the signed distance appears to be negative.
        /// Msdfgen will produce an inverted picture on occasion.
        /// Because we use offset we can easily detect if the corner pixel has negative (correct) or positive distance (incorrect)
        /// </summary>
        /// <param name="bitmap"></param>
        private void Normalize(FreeImageBitmap bitmap)
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
            tempDir = Path.Combine(Path.GetTempPath(), "StrideGlyphs");
            Directory.CreateDirectory(tempDir);
#endif
            Face face = options.FontSource.GetFont();

            // Create a bunch of GDI+ objects.
            var fontSize = options.FontType.Size;

            var glyphList = new List<Glyph>();

            // Remap the LineMap coming from the font with a user defined remapping
            // Note:
            // We are remapping the lineMap to allow to shrink the LineGap and to reposition it at the top and/or bottom of the
            // font instead of using only the top
            // According to http://stackoverflow.com/questions/13939264/how-to-determine-baseline-position-using-directwrite#comment27947684_14061348
            // (The response is from a MSFT employee), the BaseLine should be = LineGap + Ascent but this is not what
            // we are experiencing when comparing with MSWord (LineGap + Ascent seems to offset too much.)
            //
            // So we are first applying a factor to the line gap:
            //     NewLineGap = LineGap * LineGapFactor
            var lineGap = (face.Height - face.Ascender + face.Descender) * options.LineGapFactor;

            float pixelPerDesignUnit = fontSize / face.UnitsPerEM;
            // Store the font height.
            LineSpacing = (lineGap + face.Ascender + Math.Abs(face.Descender)) * pixelPerDesignUnit;

            // And then the baseline is also changed in order to allow the linegap to be distributed between the top and the
            // bottom of the font:
            //     BaseLine = NewLineGap * LineGapBaseLineFactor
            BaseLine = (lineGap * options.LineGapBaseLineFactor + face.Ascender) * pixelPerDesignUnit;

            // Generate SDF bitmaps for each character in turn.
            foreach (var character in characters)
                glyphList.Add(ImportGlyph(face, character, fontSize));

            Glyphs = glyphList;
        }

        /// <summary>
        /// Imports a single glyph as a bitmap using the msdfgen to convert it to a signed distance field image
        /// </summary>
        /// <param name="fontFace">FontFace, use to obtain the metrics for the glyph</param>
        /// <param name="character">The glyph's character code</param>
        /// <param name="fontMetrics">Font metrics, used to obtain design units scale</param>
        /// <param name="fontSize">Requested font size. The bigger, the more precise the SDF image is going to be</param>
        /// <returns></returns>
        private Glyph ImportGlyph(Face face, char character, float fontSize)
        {
            var index = face.GetCharIndex(character);
            face.SetPixelSizes(0, (uint)fontSize);
            face.LoadGlyph(index, LoadFlags.NoScale, LoadTarget.Normal);
            
            //------------------------------------
            float pixelPerDesignUnit = fontSize / face.UnitsPerEM;
            
            float fontWidthPx = face.Glyph.Metrics.Width.Value * pixelPerDesignUnit;
            float fontHeightPx = face.Glyph.Metrics.Height.Value * pixelPerDesignUnit;

            float fontOffsetXPx = face.Glyph.Metrics.HorizontalBearingX.Value * pixelPerDesignUnit;
            float fontOffsetYPx = -face.Glyph.Metrics.HorizontalBearingY.Value * pixelPerDesignUnit;

            float advanceWidthPx = face.Glyph.Metrics.HorizontalAdvance.Value * pixelPerDesignUnit;
            
            //---------------------------------------------------
            const int MarginPx = 2;     // Buffer zone for the sdf image to avoid clipping
            int bitmapWidthPx = (int)Math.Ceiling(fontWidthPx) + (2 * MarginPx);
            int bitmapHeightPx = (int)Math.Ceiling(fontHeightPx) + (2 * MarginPx);

            float bitmapOffsetXPx = fontOffsetXPx - MarginPx;
            float bitmapOffsetYPx = fontOffsetYPx - MarginPx;

            FreeImageBitmap bitmap;
            if (char.IsWhiteSpace(character))
            {
                bitmap = new FreeImageBitmap(1, 1, FreeImageAPI.PixelFormat.Format32bppArgb);
            }
            else
            {
                // sdfPixelPerDesignUnit is hardcoded from the import in this code
                // https://github.com/stride3d/msdfgen/blob/1af188c77822e447fe8e412420fe0fe05b782b38/ext/import-font.cpp#L126-L150
                const float sdfPixelPerDesignUnit = 1 / 64f;      // msdf default coordinate scale
                float boundLeft = face.Glyph.Metrics.HorizontalBearingX.Value * sdfPixelPerDesignUnit;
                //float boundRight = (metric.AdvanceWidth - metric.RightSideBearing) * sdfPixelPerDesignUnit;
                //float boundTop = (metric.VerticalOriginY - metric.TopSideBearing) * sdfPixelPerDesignUnit;
                float boundBottom = (face.Glyph.Metrics.HorizontalBearingY.Value - face.Glyph.Metrics.Height.Value) * sdfPixelPerDesignUnit;

                float glyphWidthPx = face.Glyph.Metrics.Width.Value * sdfPixelPerDesignUnit;
                float glyphHeightPx = face.Glyph.Metrics.Height.Value * sdfPixelPerDesignUnit;

                // Need to scale from msdfgen's 'shape unit' into the final bitmap's space
                float scaleX = fontWidthPx / glyphWidthPx;
                float scaleY = fontHeightPx / glyphHeightPx;

                // Note: msdfgen uses coordinates from bottom-left corner
                // so offsetY needs to calculate the offset such that it snaps to the top side of the bitmap (+ margin space)
                float offsetX = (MarginPx / scaleX) - boundLeft;
                float offsetY = ((bitmapHeightPx - MarginPx) / scaleY) - glyphHeightPx - boundBottom;

                bitmap = LoadSDFBitmap(character, bitmapWidthPx, bitmapHeightPx, offsetX, offsetY, scaleX, scaleY);
            }

            var glyph = new Glyph(character, bitmap)
            {
                XOffset = bitmapOffsetXPx,
                XAdvance = advanceWidthPx,
                YOffset = bitmapOffsetYPx,
            };

            return glyph;
        }

        private static byte LinearToGamma(byte color)
        {
            return (byte)(Math.Pow(color / 255.0f, 1 / 2.2f) * 255.0f);
        }
    }
}
