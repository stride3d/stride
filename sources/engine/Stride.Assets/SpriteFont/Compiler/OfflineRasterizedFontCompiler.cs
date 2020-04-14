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
using System.IO;
using System.Text;

using Stride.Graphics.Font;

using System.Linq;
using Stride.Graphics;

namespace Stride.Assets.SpriteFont.Compiler
{
    /// <summary>
    /// Main class used to compile a Font file XML file.
    /// </summary>
    public class OfflineRasterizedFontCompiler
    {
        /// <summary>
        /// Compiles the specified font description into a <see cref="OfflineRasterizedSpriteFont" /> object.
        /// </summary>
        /// <param name="fontFactory">The font factory used to create the fonts</param>
        /// <param name="fontAsset">The font description.</param>
        /// <param name="srgb"></param>
        /// <returns>A SpriteFontData object.</returns>
        public static Graphics.SpriteFont Compile(IFontFactory fontFactory, SpriteFontAsset fontAsset, bool srgb)
        {
            var fontTypeStatic = fontAsset.FontType as OfflineRasterizedSpriteFontType;
            if (fontTypeStatic == null)
                throw new ArgumentException("Tried to compile a dynamic sprite font with compiler for static fonts");

            float lineSpacing;
            float baseLine;

            var glyphs = ImportFont(fontAsset, out lineSpacing, out baseLine);

            // Optimize.
            foreach (Glyph glyph in glyphs)
                GlyphCropper.Crop(glyph);

            Bitmap bitmap = GlyphPacker.ArrangeGlyphs(glyphs);

            // Automatically detect whether this is a monochromatic or color font?
            //if (fontAsset.Format == FontTextureFormat.Auto)
            //{
            //    bool isMono = BitmapUtils.IsRgbEntirely(Color.White, bitmap);
            //
            //    fontAsset.Format = isMono ? FontTextureFormat.CompressedMono :
            //                                     FontTextureFormat.Rgba32;
            //}

            // Convert to pre-multiplied alpha format.
            if (fontAsset.FontType.IsPremultiplied)
            {
                if (fontAsset.FontType.AntiAlias == FontAntiAliasMode.ClearType)
                {
                    BitmapUtils.PremultiplyAlphaClearType(bitmap, srgb);
                }
                else
                {
                    BitmapUtils.PremultiplyAlpha(bitmap, srgb);
                }
            }

            return OfflineRasterizedSpriteFontWriter.CreateSpriteFontData(fontFactory, fontAsset, glyphs, lineSpacing, baseLine, bitmap, srgb);
        }

        static Glyph[] ImportFont(SpriteFontAsset options, out float lineSpacing, out float baseLine)
        {
            // Which importer knows how to read this source font?
            IFontImporter importer;

            var sourceExtension = (Path.GetExtension(options.FontSource.GetFontPath()) ?? "").ToLowerInvariant();
            var bitmapFileExtensions = new List<string> { ".bmp", ".png", ".gif" };
            var importFromBitmap = bitmapFileExtensions.Contains(sourceExtension);

            importer = importFromBitmap ? (IFontImporter) new BitmapImporter() : new TrueTypeImporter();

            // create the list of character to import
            var characters = GetCharactersToImport(options); 

            // Import the source font data.
            importer.Import(options, characters);

            lineSpacing = importer.LineSpacing;
            baseLine = importer.BaseLine;

            // Get all glyphs
            var glyphs = new List<Glyph>(importer.Glyphs);

            // Validate.
            if (glyphs.Count == 0)
            {
                throw new Exception("Font does not contain any glyphs.");
            }
            if (!importFromBitmap && options.FontType.AntiAlias != FontAntiAliasMode.ClearType)
            {
                foreach (var glyph in importer.Glyphs)
                    BitmapUtils.ConvertGreyToAlpha(glyph.Bitmap, glyph.Subrect);
            }

            // Sort the glyphs
            glyphs.Sort((left, right) => left.Character.CompareTo(right.Character));


            // Check that the default character is part of the glyphs
            if (options.DefaultCharacter != 0)
            {
                bool defaultCharacterFound = false;
                foreach (var glyph in glyphs)
                {
                    if (glyph.Character == options.DefaultCharacter)
                    {
                        defaultCharacterFound = true;
                        break;
                    }
                }
                if (!defaultCharacterFound)
                {
                    throw new InvalidOperationException("The specified DefaultCharacter is not part of this font.");
                }
            }

            return glyphs.ToArray();
        }

        public static List<char> GetCharactersToImport(SpriteFontAsset asset)
        {
            var characters = new List<char>();

            var fontTypeStatic = asset.FontType as OfflineRasterizedSpriteFontType;
            if (fontTypeStatic == null)
                throw new ArgumentException("Tried to compile a dynamic sprite font with compiler for signed distance field fonts");

            // extract the list from the provided file if it exits
            if (File.Exists(fontTypeStatic.CharacterSet))
            {
                string text;
                using (var streamReader = new StreamReader(fontTypeStatic.CharacterSet, Encoding.UTF8))
                    text = streamReader.ReadToEnd();
                characters.AddRange(text);
            }

            // add character coming from character ranges
            characters.AddRange(CharacterRegion.Flatten(fontTypeStatic.CharacterRegions));

            // remove duplicated characters
            characters = characters.Distinct().ToList();

            return characters;
        }
    }
}
