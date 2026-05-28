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

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Stride.Core.Mathematics;

using Rectangle = SixLabors.ImageSharp.Rectangle;

namespace Stride.Assets.SpriteFont.Compiler
{
    // Assorted helpers for doing useful things with bitmaps.
    internal static class BitmapUtils
    {
        // Copies a rectangular area from one bitmap to another.
        public static void CopyRect(Image<Rgba32> source, Rectangle sourceRegion, Image<Rgba32> output, Rectangle outputRegion)
        {
            if (sourceRegion.Width != outputRegion.Width ||
                sourceRegion.Height != outputRegion.Height)
            {
                throw new ArgumentException();
            }

            for (int y = 0; y < sourceRegion.Height; y++)
            {
                for (int x = 0; x < sourceRegion.Width; x++)
                {
                    output[outputRegion.X + x, outputRegion.Y + y] = source[sourceRegion.X + x, sourceRegion.Y + y];
                }
            }
        }


        // Checks whether an area of a bitmap contains entirely the specified alpha value.
        public static bool IsAlphaEntirely(byte expectedAlpha, Image<Rgba32> bitmap, Rectangle? region = null)
        {
            var area = region.GetValueOrDefault(new Rectangle(0, 0, bitmap.Width, bitmap.Height));

            for (int y = 0; y < area.Height; y++)
            {
                for (int x = 0; x < area.Width; x++)
                {
                    if (bitmap[area.X + x, area.Y + y].A != expectedAlpha)
                        return false;
                }
            }

            return true;
        }


        // Checks whether a bitmap contains entirely the specified RGB value.
        public static bool IsRgbEntirely(Rgba32 expectedRgb, Image<Rgba32> bitmap)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap[x, y];

                    if (color.A == 0)
                        continue;

                    if ((color.R != expectedRgb.R) ||
                        (color.G != expectedRgb.G) ||
                        (color.B != expectedRgb.B))
                    {
                        return false;
                    }
                }
            }

            return true;
        }


        // Converts greyscale luminosity to alpha data.
        public static void ConvertGreyToAlpha(Image<Rgba32> bitmap, Rectangle region)
        {
            for (int y = 0; y < region.Height; y++)
            {
                for (int x = 0; x < region.Width; x++)
                {
                    var color = bitmap[region.X + x, region.Y + y];

                    // Average the red, green and blue values to compute brightness.
                    var alpha = (byte)((color.R + color.G + color.B) / 3);

                    bitmap[region.X + x, region.Y + y] = new Rgba32(255, 255, 255, alpha);
                }
            }
        }

        // Converts a bitmap to premultiplied alpha format.
        public static void PremultiplyAlphaClearType(Image<Rgba32> bitmap, bool srgb)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap[x, y];

                    int a;
                    if (srgb)
                    {
                        var colorLinear = new Color4(new Stride.Core.Mathematics.Color(color.R, color.G, color.B)).ToLinear();
                        var alphaLinear = (colorLinear.R + colorLinear.G + colorLinear.B) / 3.0f;
                        a = MathUtil.Clamp((int)Math.Round(alphaLinear * 255), 0, 255);
                    }
                    else
                    {
                        a = (color.R + color.G + color.B) / 3;
                    }

                    bitmap[x, y] = new Rgba32(color.R, color.G, color.B, (byte)a);
                }
            }
        }

        // Converts a bitmap to premultiplied alpha format.
        public static void PremultiplyAlpha(Image<Rgba32> bitmap, bool srgb)
        {
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    var color = bitmap[x, y];
                    int a = color.A;
                    byte r;
                    byte g;
                    byte b;
                    if (srgb)
                    {
                        var colorLinear = new Color4(new Stride.Core.Mathematics.Color(color.R, color.G, color.B)).ToLinear();
                        colorLinear *= color.A / 255.0f;
                        var colorSRgb = (Stride.Core.Mathematics.Color)colorLinear.ToSRgb();
                        r = colorSRgb.R;
                        g = colorSRgb.G;
                        b = colorSRgb.B;
                    }
                    else
                    {
                        r = (byte)(color.R * a / 255);
                        g = (byte)(color.G * a / 255);
                        b = (byte)(color.B * a / 255);
                    }

                    bitmap[x, y] = new Rgba32(r, g, b, (byte)a);
                }
            }
        }


        // To avoid filtering artifacts when scaling or rotating fonts that do not use premultiplied alpha,
        // make sure the one pixel border around each glyph contains the same RGB values as the edge of the
        // glyph itself, but with zero alpha. This processing is an elaborate no-op when using premultiplied
        // alpha, because the premultiply conversion will change the RGB of all such zero alpha pixels to black.
        public static void PadBorderPixels(Image<Rgba32> bitmap, Rectangle region)
        {
            // Pad the top and bottom.
            for (int x = region.Left; x < region.Right; x++)
            {
                CopyBorderPixel(bitmap, x, region.Top, x, region.Top - 1);
                CopyBorderPixel(bitmap, x, region.Bottom - 1, x, region.Bottom);
            }

            // Pad the left and right.
            for (int y = region.Top; y < region.Bottom; y++)
            {
                CopyBorderPixel(bitmap, region.Left, y, region.Left - 1, y);
                CopyBorderPixel(bitmap, region.Right - 1, y, region.Right, y);
            }

            // Pad the four corners.
            CopyBorderPixel(bitmap, region.Left, region.Top, region.Left - 1, region.Top - 1);
            CopyBorderPixel(bitmap, region.Right - 1, region.Top, region.Right, region.Top - 1);
            CopyBorderPixel(bitmap, region.Left, region.Bottom - 1, region.Left - 1, region.Bottom);
            CopyBorderPixel(bitmap, region.Right - 1, region.Bottom - 1, region.Right, region.Bottom);
        }


        // Copies a single pixel within a bitmap, preserving RGB but forcing alpha to zero.
        static void CopyBorderPixel(Image<Rgba32> bitmap, int sourceX, int sourceY, int destX, int destY)
        {
            var color = bitmap[sourceX, sourceY];

            bitmap[destX, destY] = new Rgba32(color.R, color.G, color.B, (byte)0);
        }
    }
}
