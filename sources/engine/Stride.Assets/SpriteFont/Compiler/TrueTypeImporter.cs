// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;

using SharpDX.Direct2D1;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using SharpDX.DirectWrite;
    using FreeImageAPI;
    using SharpDX.Mathematics.Interop;
    using SharpFont;
    using Stride.Core;
    using Factory = SharpDX.DirectWrite.Factory;

    // This code was originally taken from DirectXTk but rewritten with DirectWrite
    // for more accuracy in font rendering
    internal class TrueTypeImporter : IFontImporter
    {
        // Properties hold the imported font data.
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }

        public float BaseLine { get; private set; }

        public void Import(SpriteFontAsset options, List<char> characters)
        {
            NativeLibraryHelper.PreloadLibrary("freeimage", typeof(TrueTypeImporter));
            
            var face = options.FontSource.GetFont();

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

            // Store the font height.
            LineSpacing = (lineGap + face.Ascender + Math.Abs(face.Descender)) / face.UnitsPerEM * fontSize;

            // And then the baseline is also changed in order to allow the linegap to be distributed between the top and the 
            // bottom of the font:
            //     BaseLine = NewLineGap * LineGapBaseLineFactor
            BaseLine = (lineGap * options.LineGapBaseLineFactor + face.Ascender) / face.UnitsPerEM * fontSize;

            // Rasterize each character in turn.

            foreach (var character in characters)
                glyphList.Add(ImportGlyph(face, character, fontSize, options.FontType.AntiAlias));

            Glyphs = glyphList;
        }
        
        private Glyph ImportGlyph(Face face, char character, float fontSize, FontAntiAliasMode antiAliasMode)
        {
            var index = face.GetCharIndex(character);
            face.SetPixelSizes(0, (uint)fontSize);
            face.LoadGlyph(index, LoadFlags.NoScale, LoadTarget.Normal);

            var width = (float)face.Glyph.Metrics.Width.Value / face.UnitsPerEM * fontSize;
            var height = (float)face.Glyph.Metrics.Height.Value / face.UnitsPerEM * fontSize;
            
            var xOffset = (float)face.Glyph.Metrics.HorizontalBearingX.Value / face.UnitsPerEM * fontSize;
            var yOffset = (float)(-1)*face.Glyph.Metrics.HorizontalBearingY.Value / face.UnitsPerEM * fontSize;
            
            var advanceWidth = (float)face.Glyph.Metrics.HorizontalAdvance.Value / face.UnitsPerEM * fontSize;

            //var advanceHeight = (float)metric.AdvanceHeight / face.UnitsPerEM * fontSize;

            var pixelWidth = (int)Math.Ceiling(width + 4);
            var pixelHeight = (int)Math.Ceiling(height + 4);

            var matrix = new RawMatrix3x2
            {
                M11 = 1,
                M22 = 1,
                M31 = -MathF.Floor(xOffset) + 1,
                M32 = -MathF.Floor(yOffset) + 1
            };

            FreeImageBitmap bitmap;
            if (char.IsWhiteSpace(character))
            {
                bitmap = new FreeImageBitmap(1, 1, FreeImageAPI.PixelFormat.Format32bppArgb);
            }
            else
            {
                bitmap = new FreeImageBitmap(pixelWidth, pixelHeight, FreeImageAPI.PixelFormat.Format32bppArgb);

                face.LoadGlyph(index, LoadFlags.Render, LoadTarget.Normal);
                for (var y = 0; y < face.Glyph.Bitmap.Rows; y++)
                {
                    for (int x = 0; x < face.Glyph.Bitmap.Width; x++)
                    {
                        var pixel = face.Glyph.Bitmap.BufferData[y * face.Glyph.Bitmap.Width + x];
                        bitmap.SetPixel(x, y, Color.FromArgb(pixel, pixel, pixel));
                    }
                }
            }

            var glyph = new Glyph(character, bitmap)
            {
                XOffset = -matrix.M31,
                XAdvance = advanceWidth,
                YOffset = -matrix.M32,
            };
            return glyph;
        }

        private static byte LinearToGamma(byte color)
        {
            return (byte)(Math.Pow(color / 255.0f, 1 / 2.2f) * 255.0f);
        }
    }
}
