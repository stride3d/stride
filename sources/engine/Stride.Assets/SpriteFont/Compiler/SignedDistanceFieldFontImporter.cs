// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

using Stride.Core;
using Stride.Core.Assets;
using Stride.Graphics.Font;

namespace Stride.Assets.SpriteFont.Compiler
{
    internal unsafe class SignedDistanceFieldFontImporter : IFontImporter
    {
        // Width of the signed-distance range, in pixels.
        private const double MsdfgenPxRange = 4.0;

        // Transparent border kept around each glyph in the SDF bitmap, in pixels.
        private const int MarginPx = 2;

        // Properties hold the imported font data.
        public IEnumerable<Glyph> Glyphs { get; private set; }

        public float LineSpacing { get; private set; }

        public float BaseLine { get; private set; }

        private string fontSource;
        private IntPtr msdfgenContext;
        private IntPtr msdfgenFont;

        /// <inheritdoc/>
        public void Import(SpriteFontAsset options, List<char> characters)
        {
            fontSource = options.FontSource.GetFontPath();
            if (string.IsNullOrEmpty(fontSource))
                return;

            NativeLibraryHelper.PreloadLibrary("freetype", typeof(SignedDistanceFieldFontImporter));
            NativeLibraryHelper.PreloadLibrary("stride_msdfgen", typeof(SignedDistanceFieldFontImporter));

            int err = FreeTypeNative.FT_Init_FreeType(out var library);
            if (err != 0)
                throw new InvalidOperationException($"Failed to initialize FreeType library (error {err})");

            msdfgenContext = MsdfgenNative.msdfgenContextCreate();
            if (msdfgenContext == IntPtr.Zero)
            {
                FreeTypeNative.FT_Done_FreeType(library);
                throw new InvalidOperationException("Failed to initialize msdfgen context");
            }

            msdfgenFont = MsdfgenNative.msdfgenLoadFont(msdfgenContext, fontSource);
            if (msdfgenFont == IntPtr.Zero)
            {
                MsdfgenNative.msdfgenContextDestroy(msdfgenContext);
                msdfgenContext = IntPtr.Zero;
                FreeTypeNative.FT_Done_FreeType(library);
                throw new AssetException($"Failed to load font '{fontSource}' into msdfgen.");
            }

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

                        var glyphList = new List<Glyph>();
                        foreach (var character in characters)
                            glyphList.Add(ImportGlyph(character, fontSize));

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
                MsdfgenNative.msdfgenUnloadFont(msdfgenFont);
                msdfgenFont = IntPtr.Zero;
                MsdfgenNative.msdfgenContextDestroy(msdfgenContext);
                msdfgenContext = IntPtr.Zero;
                FreeTypeNative.FT_Done_FreeType(library);
            }
        }

        /// <summary>
        /// Imports a single glyph, letting msdfgen frame the outline and report its placement.
        /// </summary>
        private Glyph ImportGlyph(char character, float fontSize)
        {
            int rc = MsdfgenNative.msdfgenGenerateGlyph(msdfgenFont, character, fontSize, MsdfgenPxRange, MarginPx, out var info, out var rgba);

            // Missing glyph: empty bitmap and no advance.
            if (rc != 0)
            {
                return new Glyph(character, new Image<Rgba32>(1, 1)) { XOffset = 0, YOffset = 0, XAdvance = 0 };
            }

            // Whitespace / outline-less glyph: empty bitmap but a valid advance.
            if (rgba == IntPtr.Zero || info.Width <= 0 || info.Height <= 0)
            {
                return new Glyph(character, new Image<Rgba32>(1, 1)) { XOffset = 0, YOffset = 0, XAdvance = (float)info.Advance };
            }

            Image<Rgba32> bitmap;
            try
            {
                var pixels = new byte[info.Width * info.Height * 4];
                Marshal.Copy(rgba, pixels, 0, pixels.Length);
                bitmap = Image.LoadPixelData<Rgba32>(pixels, info.Width, info.Height);
            }
            finally
            {
                MsdfgenNative.msdfgenFreeBitmap(rgba);
            }

            return new Glyph(character, bitmap)
            {
                XOffset = (float)info.OffsetX,
                YOffset = (float)info.OffsetY,
                XAdvance = (float)info.Advance,
            };
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MsdfgenGlyphInfo
    {
        public int Width;
        public int Height;
        public double OffsetX;
        public double OffsetY;
        public double Advance;
    }

    internal static unsafe class MsdfgenNative
    {
        private const string Lib = "stride_msdfgen";

        [DllImport(Lib)]
        public static extern IntPtr msdfgenContextCreate();

        [DllImport(Lib)]
        public static extern void msdfgenContextDestroy(IntPtr ctx);

        [DllImport(Lib)]
        public static extern IntPtr msdfgenLoadFont(IntPtr ctx, [MarshalAs(UnmanagedType.LPUTF8Str)] string utf8Path);

        [DllImport(Lib)]
        public static extern void msdfgenUnloadFont(IntPtr font);

        [DllImport(Lib)]
        public static extern int msdfgenGenerateGlyph(
            IntPtr font,
            uint unicode,
            double emSize,
            double pxRange,
            int margin,
            out MsdfgenGlyphInfo info,
            out IntPtr outRgba);

        [DllImport(Lib)]
        public static extern void msdfgenFreeBitmap(IntPtr rgba);
    }
}
