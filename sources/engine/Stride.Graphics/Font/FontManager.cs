// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using SharpFont;

using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;

namespace Xenko.Graphics.Font
{
    /// <summary>
    /// A font manager is in charge of loading in memory the ttf files, looking for font informations, rendering and then caching the <see cref="CharacterBitmap"/>s on the CPU . 
    /// </summary>
    internal class FontManager : IDisposable
    {
        /// <summary>
        /// Lock both <see cref="generatedBitmaps"/> and <see cref="bitmapsToGenerate"/>.
        /// </summary>
        private readonly object dataStructuresLock = new object();

        /// <summary>
        /// The font data that are currently cached in the registry
        /// </summary>
        private readonly Dictionary<string, Face> cachedFontFaces = new Dictionary<string, Face>();

        /// <summary>
        /// The list of the bitmaps that have already been generated.
        /// </summary>
        private readonly List<CharacterSpecification> generatedBitmaps = new List<CharacterSpecification>();

        /// <summary>
        /// The list of the bitmaps that are in generation or to generate
        /// </summary>
        private readonly Queue<CharacterSpecification> bitmapsToGenerate = new Queue<CharacterSpecification>();

        /// <summary>
        /// The <see cref="AutoResetEvent"/> used to signal the bitmap build thread that a build operation is requested.
        /// </summary>
        private readonly AutoResetEvent bitmapBuildSignal = new AutoResetEvent(false);

        /// <summary>
        /// The thread in charge of building the characters bitmaps
        /// </summary>
        private readonly Thread bitmapBuilderThread;

        /// <summary>
        /// Boolean specifying if we need to quit the bitmap building thread.
        /// </summary>
        private bool bitmapShouldEndThread;

        /// <summary>
        /// A reference pointer to the freetype library.
        /// </summary>
        private Library freetypeLibrary;

        /// <summary>
        /// The asset manager used to load the ttf fonts.
        /// </summary>
        private readonly ContentManager contentManager;

        /// <summary>
        /// The size of the transparent border to add around the character bitmap.
        /// <remarks>
        /// If we don't do so artifacts appears around the character when scaling fonts
        /// Note that we cannot just increase space taken in the bin packer because artifacts with old/previous characters may happen.
        /// </remarks>
        /// </summary>
        private Int2 borderSize = Int2.One;

        /// <summary>
        /// Create an empty register.
        /// </summary>
        public FontManager(IDatabaseFileProviderService fileProviderService)
        {
            contentManager = new ContentManager(fileProviderService);

            // Preload proper freetype native library (depending on CPU type)
            Core.NativeLibrary.PreloadLibrary("freetype.dll", typeof(FontManager));

            // create a freetype library used to generate the bitmaps
            freetypeLibrary = new Library();

            // launch the thumbnail builder thread
            bitmapBuilderThread = new Thread(SafeAction.Wrap(BuildBitmapThread)) { IsBackground = true, Name = "Bitmap Builder thread" };
            bitmapBuilderThread.Start();
        }

        /// <summary>
        /// Start the generation of the specified character's bitmap. 
        /// </summary>
        /// <remarks>Does nothing if the bitmap already exist or if the generation is currently running.</remarks>
        /// <param name="characterSpecification">The character we want the bitmap of</param>
        /// <param name="synchronously">Indicate if the generation of the bitmap must by done synchronously or asynchronously</param>
        public void GenerateBitmap(CharacterSpecification characterSpecification, bool synchronously)
        {
            // generate the glyph info (and the bitmap if required) synchronously
            GenerateCharacterGlyph(characterSpecification, synchronously);

            // add the bitmap rendering job to a request queue if rendering is asynchronous
            if (!synchronously)
            {
                lock (dataStructuresLock)
                {
                    if (characterSpecification.Bitmap == null && !bitmapsToGenerate.Contains(characterSpecification))
                    {
                        bitmapsToGenerate.Enqueue(characterSpecification);
                        bitmapBuildSignal.Set();
                    }
                }
            }
        }

        private void GenerateCharacterGlyph(CharacterSpecification character, bool renderBitmap)
        {
            // first the possible current glyph info
            ResetGlyph(character);

            // let the glyph info null if the size is not valid
            if (character.Size.X < 1 || character.Size.Y < 1)
                return;

            // get the face of the font 
            var fontFace = GetOrCreateFontFace(character.FontName, character.Style);
            lock (freetypeLibrary)
            {
                // set the font size
                SetFontFaceSize(fontFace, character.Size);

                // get the glyph and render the bitmap
                var glyphIndex = fontFace.GetCharIndex(character.Character);

                // the character does not exit => let the glyph info null
                if (glyphIndex == 0)
                    return;

                // load the character glyph
                fontFace.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

                // set glyph information
                character.Glyph.XAdvance = fontFace.Glyph.Advance.X.ToSingle();

                // render the bitmap
                if (renderBitmap)
                    RenderBitmap(character, fontFace);
            }
        }

        private void RenderBitmap(CharacterSpecification character, Face fontFace)
        {
            // choose the rendering type and render the glyph
            var renderingMode = character.AntiAlias == FontAntiAliasMode.Aliased ? RenderMode.Mono : RenderMode.Normal;
            fontFace.Glyph.RenderGlyph(renderingMode);

            // create the bitmap
            var bitmap = fontFace.Glyph.Bitmap;
            if (bitmap.Width != 0 && bitmap.Rows != 0)
                character.Bitmap = new CharacterBitmap(bitmap.Buffer, ref borderSize, bitmap.Width, bitmap.Rows, bitmap.Pitch, bitmap.GrayLevels, bitmap.PixelMode);
            else
                character.Bitmap = new CharacterBitmap();

            // set the glyph offsets
            character.Glyph.Offset = new Vector2(fontFace.Glyph.BitmapLeft - borderSize.X, -fontFace.Glyph.BitmapTop - borderSize.Y);
        }

        private static void ResetGlyph(CharacterSpecification character)
        {
            character.Glyph.Offset = Vector2.Zero;
            character.Glyph.XAdvance = 0;
            character.Glyph.BitmapIndex = 0;
            character.Glyph.Subrect.X = 0;
            character.Glyph.Subrect.Y = 0;
            character.Glyph.Subrect.Width = 0;
            character.Glyph.Subrect.Height = 0;
        }

        private void SetFontFaceSize(Face fontFace, Vector2 size)
        {
            // calculate and set the size of the font
            // size is in 26.6 factional points (that is in 1/64th of points)
            // 72 => the sizes are in "points" (=1/72 inch), setting resolution to 72 dpi let us specify the size in pixels directly
            fontFace.SetCharSize(size.X, size.Y, 72, 72);
        }

        /// <summary>
        /// Get various information about a font of a given family, type, and size.
        /// </summary>
        /// <param name="fontFamily">The name of the family of the font</param>
        /// <param name="fontStyle">The style of the font</param>
        /// <param name="lineSpacing">The space between two lines for a font size of 1 pixel</param>
        /// <param name="baseLine">The default base line for a font size of 1 pixel</param>
        /// <param name="maxWidth">The width of the largest character for a font size of 1 pixel</param>
        /// <param name="maxHeight">The height of the largest character for a font size of 1 pixel</param>
        public void GetFontInfo(string fontFamily, FontStyle fontStyle, out float lineSpacing, out float baseLine, out float maxWidth, out float maxHeight)
        {
            // get the data of the font data
            var fontData = GetOrCreateFontFace(fontFamily, fontStyle);

            lineSpacing = fontData.Height / (float)fontData.UnitsPerEM;
            baseLine = (fontData.Height + fontData.Descender) / (float)fontData.UnitsPerEM;
            maxWidth = (fontData.BBox.Right - fontData.BBox.Left) / (float)fontData.UnitsPerEM;
            maxHeight = (fontData.BBox.Top - fontData.BBox.Bottom) / (float)fontData.UnitsPerEM;
        }

        /// <summary>
        /// Returns a boolean indicating if the specified font contains the provided character.
        /// </summary>
        /// <param name="fontStyle">The style in the font family</param>
        /// <param name="character">The character to look for</param>
        /// <param name="fontFamily">The family of the font</param>
        /// <returns>boolean indicating if the font contains the character</returns>
        public bool DoesFontContains(string fontFamily, FontStyle fontStyle, char character)
        {
            var fontFace = GetOrCreateFontFace(fontFamily, fontStyle);

            uint glyphIndex;
            lock (fontFace)
                glyphIndex = fontFace.GetCharIndex(character);

            // see if the index of the character is valid
            return glyphIndex != 0;
        }
        
        public void Dispose()
        {
            // terminate the build thread
            bitmapShouldEndThread = true;
            bitmapBuildSignal.Set();
            bitmapBuilderThread.Join();

            // free and clear the list of generated bitmaps
            foreach (var character in generatedBitmaps)
            {
                if (character.Bitmap != null)
                {
                    character.Bitmap.Dispose();
                    character.Bitmap = null;
                }
            }
            generatedBitmaps.Clear();

            // free font faces
            foreach (var font in cachedFontFaces.Values)
                font.Dispose();
            cachedFontFaces.Clear();

            // free freetype library
            if (freetypeLibrary != null)
                freetypeLibrary.Dispose();
            freetypeLibrary = null;
        }
        
        private Face GetOrCreateFontFace(string fontFamily, FontStyle fontStyle)
        {
            var fontPath = FontHelper.GetFontPath(fontFamily, fontStyle);

            // ensure that the font is load in memory
            LoadFontInMemory(fontPath);

            return cachedFontFaces[fontPath];
        }

        private void LoadFontInMemory(string fontPath)
        {
            // return if the font is already cached
            if (cachedFontFaces.ContainsKey(fontPath)) 
                return;

            // load the font from the data base
            using (var fontStream = contentManager.OpenAsStream(fontPath, StreamFlags.None))
            {
                // create the font data from the stream
                var newFontData = new byte[fontStream.Length];
                fontStream.Read(newFontData, 0, newFontData.Length);

                lock (freetypeLibrary)
                    cachedFontFaces[fontPath] = freetypeLibrary.NewMemoryFace(newFontData, 0);
            }
        }

        /// <summary>
        /// Thread function in charge of building the bitmap
        /// </summary>
        private void BuildBitmapThread()
        {
            while (!bitmapShouldEndThread)
            {
                bitmapBuildSignal.WaitOne();

                while (bitmapsToGenerate.Count > 0)
                {
                    var character = bitmapsToGenerate.Peek();

                    // let the glyph data null if the size is not valid
                    if (character.Size.X < 1 || character.Size.Y < 1)
                        goto DequeueRequest;

                    // get the face of the font 
                    var fontFace = GetOrCreateFontFace(character.FontName, character.Style);

                    lock (freetypeLibrary)
                    {
                        // set the font to the correct size
                        SetFontFaceSize(fontFace, character.Size);

                        // get the glyph and render the bitmap
                        var glyphIndex = fontFace.GetCharIndex(character.Character);

                        // if the character does not exist in the face => continue
                        if (glyphIndex == 0)
                            goto DequeueRequest;

                        // load the character glyph
                        fontFace.LoadGlyph(glyphIndex, LoadFlags.Default, LoadTarget.Normal);

                        // render the bitmap and set remaining info of the glyph
                        RenderBitmap(character, fontFace);
                    }

                DequeueRequest:

                    // update the generated cached data
                    lock (dataStructuresLock)
                        bitmapsToGenerate.Dequeue();
                }
            }
        }   
    }
}
