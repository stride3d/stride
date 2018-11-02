// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Xenko.Core.Assets;
using Xenko.Core.IO;

namespace Xenko.Assets.SpriteFont.Compiler
{
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Reflection;
    using SharpDX.DirectWrite;
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
        private string tempDir;

        /// <summary>
        /// Generates and load a SDF font glyph using the msdfgen.exe
        /// </summary>
        /// <param name="c">Character code</param>
        /// <param name="width">Width of the output glyph</param>
        /// <param name="height">Height of the output glyph</param>
        /// <param name="offsetx">Left side offset of the glyph from the image border</param>
        /// <param name="offsety">Bottom side offset of the glyph from the image border</param>
        /// <returns></returns>
        private Bitmap LoadSDFBitmap(char c, int width, int height, int offsetx, int offsety)
        {
            try
            {

                var characterCode = "0x" + Convert.ToUInt32(c).ToString("x4");
#if DEBUG
                var outputFile = $"{tempDir}{characterCode}_{Guid.NewGuid()}.bmp";
#else
                var outputFile = Path.GetTempFileName();
#endif
                var exportSize = $" -size {width} {height} ";
                var translate = $" -translate {offsetx} {offsety} ";

                var startInfo = new ProcessStartInfo
                {
                    FileName = msdfgenExe,
                    Arguments = $"msdf -font \"{fontSource}\" {characterCode} -o \"{outputFile}\" -format bmp {exportSize} {translate} -autoframe",
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

                if (File.Exists(outputFile))
                {
                    var bitmap = (Bitmap)Image.FromFile(outputFile);

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
        /// <param name="bitmap"></param>
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
            var msdfgen = ToolLocator.LocateTool("msdfgen.exe") ?? throw new AssetException("Failed to compile a font asset, msdfgen was not found.");

            msdfgenExe = msdfgen.FullPath;
            tempDir = $"{Environment.GetEnvironmentVariable("TEMP")}\\";

            var factory = new Factory();

            FontFace fontFace = options.FontSource.GetFontFace();

            var fontMetrics = fontFace.Metrics;

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
            var lineGap = fontMetrics.LineGap * options.LineGapFactor;

            // Store the font height.
            LineSpacing = (float)(lineGap + fontMetrics.Ascent + fontMetrics.Descent) / fontMetrics.DesignUnitsPerEm * fontSize;

            // And then the baseline is also changed in order to allow the linegap to be distributed between the top and the 
            // bottom of the font:
            //     BaseLine = NewLineGap * LineGapBaseLineFactor
            BaseLine = (float)(lineGap * options.LineGapBaseLineFactor + fontMetrics.Ascent) / fontMetrics.DesignUnitsPerEm * fontSize;

            // Generate SDF bitmaps for each character in turn.
            foreach (var character in characters)
                glyphList.Add(ImportGlyph(fontFace, character, fontMetrics, fontSize));

            Glyphs = glyphList;

            factory.Dispose();
        }

        /// <summary>
        /// Imports a single glyph as a bitmap using the msdfgen to convert it to a signed distance field image
        /// </summary>
        /// <param name="fontFace">FontFace, use to obtain the metrics for the glyph</param>
        /// <param name="character">The glyph's character code</param>
        /// <param name="fontMetrics">Font metrics, used to obtain design units scale</param>
        /// <param name="fontSize">Requested font size. The bigger, the more precise the SDF image is going to be</param>
        /// <returns></returns>
        private Glyph ImportGlyph(FontFace fontFace, char character, FontMetrics fontMetrics, float fontSize)
        {
            var indices = fontFace.GetGlyphIndices(new int[] { character });

            var metrics = fontFace.GetDesignGlyphMetrics(indices, false);
            var metric = metrics[0];

            var width = (float)(metric.AdvanceWidth - metric.LeftSideBearing - metric.RightSideBearing) / fontMetrics.DesignUnitsPerEm * fontSize;
            var height = (float)(metric.AdvanceHeight - metric.TopSideBearing - metric.BottomSideBearing) / fontMetrics.DesignUnitsPerEm * fontSize;

            var xOffset = (float)metric.LeftSideBearing / fontMetrics.DesignUnitsPerEm * fontSize;
            var yOffset = (float)(metric.TopSideBearing - metric.VerticalOriginY) / fontMetrics.DesignUnitsPerEm * fontSize;

            var advanceWidth = (float)metric.AdvanceWidth / fontMetrics.DesignUnitsPerEm * fontSize;
            //var advanceHeight = (float)metric.AdvanceHeight / fontMetrics.DesignUnitsPerEm * fontSize;

            var pixelWidth = (int)Math.Ceiling(width + 4);
            var pixelHeight = (int)Math.Ceiling(height + 4);


            var matrixM31 = -(float)Math.Floor(xOffset) + 1;
            var matrixM32 = -(float)Math.Floor(yOffset) + 1;

            Bitmap bitmap;
            if (char.IsWhiteSpace(character))
            {
                bitmap = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
            }
            else
            {
                bitmap = LoadSDFBitmap(character, pixelWidth, pixelHeight, 1, 1);
            }

            var glyph = new Glyph(character, bitmap)
            {
                XOffset = -matrixM31,
                XAdvance = advanceWidth,
                YOffset = -matrixM32,
            };

            return glyph;
        }

        private static byte LinearToGamma(byte color)
        {
            return (byte)(Math.Pow(color / 255.0f, 1 / 2.2f) * 255.0f);
        }
    }
}
