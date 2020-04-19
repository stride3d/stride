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
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace Stride.Assets.SpriteFont.Compiler
{
    // Helper for arranging many small bitmaps onto a single larger surface.
    internal static class GlyphPacker
    {
        public static Bitmap ArrangeGlyphs(Glyph[] sourceGlyphs)
        {
            // Build up a list of all the glyphs needing to be arranged.
            List<ArrangedGlyph> glyphs = new List<ArrangedGlyph>();

            for (int i = 0; i < sourceGlyphs.Length; i++)
            {
                ArrangedGlyph glyph = new ArrangedGlyph();

                glyph.Source = sourceGlyphs[i];

                // Leave a one pixel border around every glyph in the output bitmap.
                glyph.Width = sourceGlyphs[i].Subrect.Width + 2;
                glyph.Height = sourceGlyphs[i].Subrect.Height + 2;

                glyphs.Add(glyph);
            }

            // Sort so the largest glyphs get arranged first.
            glyphs.Sort(CompareGlyphSizes);

            // Work out how big the output bitmap should be.
            int outputWidth = GuessOutputWidth(sourceGlyphs);
            int outputHeight = 0;

            // Choose positions for each glyph, one at a time.
            for (int i = 0; i < glyphs.Count; i++)
            {
                PositionGlyph(glyphs, i, outputWidth);

                outputHeight = Math.Max(outputHeight, glyphs[i].Y + glyphs[i].Height);
            }

            // Create the merged output bitmap.
            outputHeight = MakeValidTextureSize(outputHeight, false);

            return CopyGlyphsToOutput(glyphs, outputWidth, outputHeight);
        }


        // Once arranging is complete, copies each glyph to its chosen position in the single larger output bitmap.
        static Bitmap CopyGlyphsToOutput(List<ArrangedGlyph> glyphs, int width, int height)
        {
            Bitmap output = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            foreach (ArrangedGlyph glyph in glyphs)
            {
                Glyph sourceGlyph = glyph.Source;
                Rectangle sourceRegion = sourceGlyph.Subrect;
                Rectangle destinationRegion = new Rectangle(glyph.X + 1, glyph.Y + 1, sourceRegion.Width, sourceRegion.Height);

                BitmapUtils.CopyRect(sourceGlyph.Bitmap, sourceRegion, output, destinationRegion);

                BitmapUtils.PadBorderPixels(output, destinationRegion);

                sourceGlyph.Bitmap = output;
                sourceGlyph.Subrect = destinationRegion;
            }

            return output;
        }


        // Internal helper class keeps track of a glyph while it is being arranged.
        class ArrangedGlyph
        {
            public Glyph Source;

            public int X;
            public int Y;

            public int Width;
            public int Height;
        }


        // Works out where to position a single glyph.
        static void PositionGlyph(List<ArrangedGlyph> glyphs, int index, int outputWidth)
        {
            int x = 0;
            int y = 0;

            while (true)
            {
                // Is this position free for us to use?
                int intersects = FindIntersectingGlyph(glyphs, index, x, y);

                if (intersects < 0)
                {
                    glyphs[index].X = x;
                    glyphs[index].Y = y;

                    return;
                }

                // Skip past the existing glyph that we collided with.
                x = glyphs[intersects].X + glyphs[intersects].Width;

                // If we ran out of room to move to the right, try the next line down instead.
                if (x + glyphs[index].Width > outputWidth)
                {
                    x = 0;
                    y++;
                }
            }
        }


        // Checks if a proposed glyph position collides with anything that we already arranged.
        static int FindIntersectingGlyph(List<ArrangedGlyph> glyphs, int index, int x, int y)
        {
            int w = glyphs[index].Width;
            int h = glyphs[index].Height;

            for (int i = 0; i < index; i++)
            {
                if (glyphs[i].X >= x + w)
                    continue;

                if (glyphs[i].X + glyphs[i].Width <= x)
                    continue;

                if (glyphs[i].Y >= y + h)
                    continue;

                if (glyphs[i].Y + glyphs[i].Height <= y)
                    continue;

                return i;
            }

            return -1;
        }


        // Comparison function for sorting glyphs by size.
        static int CompareGlyphSizes(ArrangedGlyph a, ArrangedGlyph b)
        {
            const int heightWeight = 1024;

            int aSize = a.Height * heightWeight + a.Width;
            int bSize = b.Height * heightWeight + b.Width;

            if (aSize != bSize)
                return bSize.CompareTo(aSize);
            else
                return a.Source.Character.CompareTo(b.Source.Character);
        }


        // Heuristic guesses what might be a good output width for a list of glyphs.
        static int GuessOutputWidth(Glyph[] sourceGlyphs)
        {
            int maxWidth = 0;
            int totalSize = 0;

            foreach (Glyph glyph in sourceGlyphs)
            {
                maxWidth = Math.Max(maxWidth, glyph.Subrect.Width);
                totalSize += glyph.Subrect.Width * glyph.Subrect.Height;
            }

            int width = Math.Max((int)Math.Sqrt(totalSize), maxWidth);

            return MakeValidTextureSize(width, true);
        }


        // Rounds a value up to the next larger valid texture size.
        static int MakeValidTextureSize(int value, bool requirePowerOfTwo)
        {
            // In case we want to DXT compress, make sure the size is a multiple of 4.
            const int blockSize = 4;

            if (requirePowerOfTwo)
            {
                // Round up to a power of two.
                int powerOfTwo = blockSize;

                while (powerOfTwo < value)
                    powerOfTwo <<= 1;

                return powerOfTwo;
            }
            else
            {
                // Round up to the specified block size.
                return (value + blockSize - 1) & ~(blockSize - 1);
            }
        }
    }
}
