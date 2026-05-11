// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Font.RuntimeMsdf;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// A font manager is in charge of loading in memory the ttf files, looking for font informations, rendering and then caching the <see cref="CharacterBitmap"/>s on the CPU .
    /// </summary>
    internal unsafe class FontManager : IDisposable
    {
        /// <summary>
        /// Lock both <see cref="generatedBitmaps"/> and <see cref="bitmapsToGenerate"/>.
        /// </summary>
        private readonly Lock dataStructuresLock = new();

        /// <summary>
        /// The font data that are currently cached in the registry.
        /// Keys are font paths, values are (face pointer, pinned font data handle) pairs.
        /// The GCHandle keeps font data alive because FreeType references it from the Face.
        /// </summary>
        private readonly Dictionary<string, (nint face, GCHandle dataHandle)> cachedFontFaces = new();

        /// <summary>
        /// The list of the bitmaps that have already been generated.
        /// </summary>
        private readonly List<CharacterSpecification> generatedBitmaps = [];

        /// <summary>
        /// The list of the bitmaps that are in generation or to generate
        /// </summary>
        private readonly Queue<CharacterSpecification> bitmapsToGenerate = new();

        /// <summary>
        /// The <see cref="AutoResetEvent"/> used to signal the bitmap build thread that a build operation is requested.
        /// </summary>
        private readonly AutoResetEvent bitmapBuildSignal = new(false);

        /// <summary>
        /// The thread in charge of building the characters bitmaps
        /// </summary>
        private readonly Thread bitmapBuilderThread;

        /// <summary>
        /// Boolean specifying if we need to quit the bitmap building thread.
        /// </summary>
        private bool bitmapShouldEndThread;

        /// <summary>
        /// A handle to the freetype library.
        /// </summary>
        private nint freetypeLibrary;

        /// <summary>
        /// Lock for all FreeType calls (FreeType is not thread-safe per library instance).
        /// </summary>
        private readonly object freetypeLock = new object();

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

            // Preload proper freetype native library (depending on CPU type).
            NativeLibraryHelper.PreloadLibrary("freetype", typeof(FontManager));

            // Initialize FreeType library
            int err = FreeTypeNative.FT_Init_FreeType(out freetypeLibrary);
            if (err != 0)
                throw new InvalidOperationException($"Failed to initialize FreeType library (error {err})");

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

        /// <summary>
        /// Loads a font from the specified file on the file system and adds it to the internal font cache.
        /// </summary>
        public void LoadFontFromFileSystem(string fontName, string filePath, FontStyle style)
        {
            var cacheKey = FontHelper.GetFontPath(fontName, style);

            lock (freetypeLock)
            {
                if (cachedFontFaces.ContainsKey(cacheKey))
                    return;

                var fontData = File.ReadAllBytes(filePath);
                var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);

                fixed (byte* ptr = fontData)
                {
                    int err = FreeTypeNative.FT_New_Memory_Face(freetypeLibrary, ptr, new CLong(fontData.Length), new CLong(0), out FT_FaceRec* face);
                    if (err != 0)
                    {
                        handle.Free();
                        throw new InvalidOperationException($"Failed to load font from '{filePath}' (FreeType error {err})");
                    }
                    cachedFontFaces[cacheKey] = ((nint)face, handle);
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
            lock (freetypeLock)
            {
                // set the font size
                SetFontFaceSize(fontFace, character.Size);

                // get the glyph and render the bitmap
                var glyphIndex = FreeTypeNative.FT_Get_Char_Index(fontFace, character.Character);

                // the character does not exit => let the glyph info null
                if (glyphIndex == 0)
                    return;

                // load the character glyph — use Mono target when rendering aliased fonts
                // so the auto-hinter optimizes for 1-bit output (fixes junction gaps)
                var loadTarget = character.AntiAlias == FontAntiAliasMode.Aliased
                    ? FreeTypeLoadTarget.Mono
                    : FreeTypeLoadTarget.Normal;
                int err = FreeTypeNative.FT_Load_Glyph(fontFace, glyphIndex, (int)FreeTypeLoadFlags.Default | (int)loadTarget);
                if (err != 0)
                    return;

                // set glyph information (advance is in 26.6 fixed-point)
                character.Glyph.XAdvance = (int)fontFace->glyph->advance.x.Value / 64.0f;

                // render the bitmap
                if (renderBitmap)
                    RenderBitmap(character, fontFace);
            }
        }

        private void RenderBitmap(CharacterSpecification character, FT_FaceRec* fontFace)
        {
            // choose the rendering type and render the glyph
            var renderingMode = character.AntiAlias == FontAntiAliasMode.Aliased ? FreeTypeRenderMode.Mono : FreeTypeRenderMode.Normal;
            int err = FreeTypeNative.FT_Render_Glyph(fontFace->glyph, renderingMode);
            if (err != 0)
                return;

            // create the bitmap
            ref FT_Bitmap bitmap = ref fontFace->glyph->bitmap;
            if (bitmap.width != 0 && bitmap.rows != 0)
                character.Bitmap = new CharacterBitmap((nint)bitmap.buffer, ref borderSize, (int)bitmap.width, (int)bitmap.rows, bitmap.pitch, bitmap.num_grays, (FreeTypePixelMode)bitmap.pixel_mode);
            else
                character.Bitmap = new CharacterBitmap();

            // set the glyph offsets
            character.Glyph.Offset = new Vector2(fontFace->glyph->bitmap_left - borderSize.X, -fontFace->glyph->bitmap_top - borderSize.Y);
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

        private void SetFontFaceSize(FT_FaceRec* fontFace, Vector2 size)
        {
            // size is in 26.6 fractional points (that is in 1/64th of points)
            // 72 => the sizes are in "points" (=1/72 inch), setting resolution to 72 dpi let us specify the size in pixels directly
            var charWidth = new CLong((int)(size.X * 64));
            var charHeight = new CLong((int)(size.Y * 64));
            FreeTypeNative.FT_Set_Char_Size(fontFace, charWidth, charHeight, 72, 72);
        }

        /// <summary>
        /// Get various information about a font of a given family, type, and size.
        /// </summary>
        public void GetFontInfo(string fontFamily, FontStyle fontStyle, out float lineSpacing, out float baseLine, out float maxWidth, out float maxHeight)
        {
            var fontFace = GetOrCreateFontFace(fontFamily, fontStyle);

            lineSpacing = fontFace->height / (float)fontFace->units_per_EM;
            baseLine = (fontFace->height + fontFace->descender) / (float)fontFace->units_per_EM;
            maxWidth = ((int)fontFace->bbox.xMax.Value - (int)fontFace->bbox.xMin.Value) / (float)fontFace->units_per_EM;
            maxHeight = ((int)fontFace->bbox.yMax.Value - (int)fontFace->bbox.yMin.Value) / (float)fontFace->units_per_EM;
        }

        /// <summary>
        /// Returns a boolean indicating if the specified font contains the provided character.
        /// </summary>
        public bool DoesFontContains(string fontFamily, FontStyle fontStyle, char character)
        {
            var fontFace = GetOrCreateFontFace(fontFamily, fontStyle);

            // FT_Get_Char_Index only reads charmap data, safe to call without the FreeType lock
            uint glyphIndex = FreeTypeNative.FT_Get_Char_Index(fontFace, character);
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
                character.Bitmap?.Dispose();
                character.Bitmap = null;
            }
            generatedBitmaps.Clear();

            // free font faces and their pinned data
            foreach (var (face, dataHandle) in cachedFontFaces.Values)
            {
                FreeTypeNative.FT_Done_Face((FT_FaceRec*)face);
                dataHandle.Free();
            }
            cachedFontFaces.Clear();

            // free freetype library
            if (freetypeLibrary != 0)
                FreeTypeNative.FT_Done_FreeType(freetypeLibrary);
            freetypeLibrary = 0;
        }

        private FT_FaceRec* GetOrCreateFontFace(string fontFamily, FontStyle fontStyle)
        {
            var fontPath = FontHelper.GetFontPath(fontFamily, fontStyle);

            // ensure that the font is loaded in memory
            LoadFontInMemory(fontPath);

            return (FT_FaceRec*)cachedFontFaces[fontPath].face;
        }

        private void LoadFontInMemory(string fontPath)
        {
            // return if the font is already cached
            if (cachedFontFaces.ContainsKey(fontPath))
                return;

            // Load the font from the database
            using (var fontStream = contentManager.OpenAsStream(fontPath))
            {
                var fontData = new byte[fontStream.Length];
                fontStream.ReadExactly(fontData);

                var handle = GCHandle.Alloc(fontData, GCHandleType.Pinned);
                lock (freetypeLock)
                {
                    // Double-check inside lock
                    if (cachedFontFaces.ContainsKey(fontPath))
                    {
                        handle.Free();
                        return;
                    }

                    fixed (byte* ptr = fontData)
                    {
                        int err = FreeTypeNative.FT_New_Memory_Face(freetypeLibrary, ptr, new CLong(fontData.Length), new CLong(0), out FT_FaceRec* face);
                        if (err != 0)
                        {
                            handle.Free();
                            throw new InvalidOperationException($"Failed to load font '{fontPath}' (FreeType error {err})");
                        }
                        cachedFontFaces[fontPath] = ((nint)face, handle);
                    }
                }
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

                    lock (freetypeLock)
                    {
                        // set the font to the correct size
                        SetFontFaceSize(fontFace, character.Size);

                        // get the glyph and render the bitmap
                        var glyphIndex = FreeTypeNative.FT_Get_Char_Index(fontFace, character.Character);

                        // if the character does not exist in the face => continue
                        if (glyphIndex == 0)
                            goto DequeueRequest;

                        // load the character glyph
                        var loadTarget = character.AntiAlias == FontAntiAliasMode.Aliased
                            ? FreeTypeLoadTarget.Mono
                            : FreeTypeLoadTarget.Normal;
                        int err = FreeTypeNative.FT_Load_Glyph(fontFace, glyphIndex, (int)FreeTypeLoadFlags.Default | (int)loadTarget);
                        if (err != 0)
                            goto DequeueRequest;

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

        /// <summary>
        /// Extracts a glyph outline (vector shape) for MSDF generation.
        /// This is intentionally synchronous and protected by the same FreeType lock as bitmap generation.
        /// If serialization/perf control is needed later, route this request through the existing
        /// bitmap builder thread and return a copied <see cref="GlyphOutline"/>.
        /// </summary>
        public bool TryGetGlyphOutline(
            string fontFamily,
            FontStyle fontStyle,
            Vector2 size, // Use Vector2 as the primary input
            char character,
            out GlyphOutline outline,
            out GlyphOutlineMetrics metrics,
            FreeTypeLoadFlags loadFlags = FreeTypeLoadFlags.NoBitmap | FreeTypeLoadFlags.NoHinting)
        {
            outline = null;
            metrics = default;

            var fontFace = GetOrCreateFontFace(fontFamily, fontStyle);

            lock (freetypeLock)
            {
                SetFontFaceSize(fontFace, size);

                return SharpFontOutlineExtractor.TryExtractGlyphOutline(
                    fontFace,
                    (uint)character,
                    out outline,
                    out metrics,
                    loadFlags);
            }
        }
    }
}
