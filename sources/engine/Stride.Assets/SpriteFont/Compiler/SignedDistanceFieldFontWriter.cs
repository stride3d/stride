// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
// -----------------------------------------------------------------------------
// The following code is a port of MakeSpriteFont from DirectXTk
// http://go.microsoft.com/fwlink/?LinkId=248929
// -----------------------------------------------------------------------------
// Microsoft Public License (Ms-PL)
//
// This license governs use of the accompanying software. If you use the 
// software, you accept this license. If you do not accept the license, do not
// use the software.
//
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and 
// "distribution" have the same meaning here as under U.S. copyright law.
// A "contribution" is the original software, or any additions or changes to 
// the software.
// A "contributor" is any person that distributes its contribution under this 
// license.
// "Licensed patents" are a contributor's patent claims that read directly on 
// its contribution.
//
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the 
// license conditions and limitations in section 3, each contributor grants 
// you a non-exclusive, worldwide, royalty-free copyright license to reproduce
// its contribution, prepare derivative works of its contribution, and 
// distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license
// conditions and limitations in section 3, each contributor grants you a 
// non-exclusive, worldwide, royalty-free license under its licensed patents to
// make, have made, use, sell, offer for sale, import, and/or otherwise dispose
// of its contribution in the software or derivative works of the contribution 
// in the software.
//
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any 
// contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that 
// you claim are infringed by the software, your patent license from such 
// contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all 
// copyright, patent, trademark, and attribution notices that are present in the
// software.
// (D) If you distribute any portion of the software in source code form, you 
// may do so only under this license by including a complete copy of this 
// license with your distribution. If you distribute any portion of the software
// in compiled or object code form, you may only do so under a license that 
// complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The
// contributors give no express warranties, guarantees or conditions. You may
// have additional consumer rights under your local laws which this license 
// cannot change. To the extent permitted under your local laws, the 
// contributors exclude the implied warranties of merchantability, fitness for a
// particular purpose and non-infringement.
//--------------------------------------------------------------------

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Stride.Core.Mathematics;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    // Writes the output sprite font binary file.
    internal static class SignedDistanceFieldFontWriter
    {
        public static Graphics.SpriteFont CreateSpriteFontData(IFontFactory fontFactory, SpriteFontAsset options, Glyph[] glyphs, float lineSpacing, float baseLine, Bitmap bitmap)
        {
            var fontGlyphs = ConvertGlyphs(glyphs);
            var images = new[] { GetImage(options, bitmap) };
            var sizeInPixels = options.FontType.Size;

            return fontFactory.NewScalable(sizeInPixels, fontGlyphs, images, baseLine, lineSpacing, null, options.Spacing, options.LineSpacing, options.DefaultCharacter);
        }

        static Graphics.Font.Glyph[] ConvertGlyphs(Glyph[] glyphs)
        {
            var fontGlyphs = new Graphics.Font.Glyph[glyphs.Length];

            for (var i = 0; i < glyphs.Length; ++i)
            {
                var glyph = glyphs[i];
                fontGlyphs[i] = new Graphics.Font.Glyph
                {
                    Character = glyph.Character,
                    Subrect = new Core.Mathematics.Rectangle(glyph.Subrect.X, glyph.Subrect.Y, glyph.Subrect.Width, glyph.Subrect.Height),
                    Offset = new Vector2(glyph.XOffset, glyph.YOffset),
                    XAdvance = glyph.XAdvance,
                };
            }

            return fontGlyphs;
        }

        static Graphics.Image GetImage(SpriteFontAsset options, Bitmap bitmap)
        {
            // TODO Currently we only support Rgba32 as an option. Grayscale might be added later
            return GetImageRgba32(bitmap);
        }

        // Writes an uncompressed 32 bit font texture.
        // We have distance values encoded so we want uncompressed texture
        static Graphics.Image GetImageRgba32(Bitmap bitmap)
        {
            // TODO Maybe switch to 3-channel texture
            var image = Graphics.Image.New2D(bitmap.Width, bitmap.Height, 1, Graphics.PixelFormat.R8G8B8A8_UNorm);
            var pixelBuffer = image.PixelBuffer[0];
            using (var bitmapData = new BitmapUtils.PixelAccessor(bitmap, ImageLockMode.ReadOnly))
            {
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmapData[x, y];
                        pixelBuffer.SetPixel(x, y, new Core.Mathematics.Color(color.R, color.G, color.B, color.A));
                    }
                }
            }
            return image;
        }
   
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct BC2Pixel
        {
            public long AlphaBits;
            public uint EndPoint;
            public int RgbBits;
        }        
    }
}
