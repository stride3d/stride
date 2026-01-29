using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;

namespace Stride.Graphics.Font
{
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<RuntimeSignedDistanceFieldSpriteFont>), Profile = "Content")]
    [ContentSerializer(typeof(RuntimeSignedDistanceFieldSpriteFontContentSerializer))]
    [DataSerializer(typeof(RuntimeSignedDistanceFieldSpriteFontSerializer))]
    internal sealed class RuntimeSignedDistanceFieldSpriteFont : SpriteFont
    {
        // Data set by FontDataFactory / serialized content
        internal string FontName;
        internal FontStyle Style;

        internal int BakeSize = 64;
        internal int PixelRange = 8;
        internal int Padding = 2;

        internal bool UseKerning;

        private readonly Dictionary<char, CharacterEntry> characters = new Dictionary<char, CharacterEntry>();

        [DataMemberIgnore]
        internal FontManager FontManager => FontSystem != null ? FontSystem.FontManager : null;

        [DataMemberIgnore]
        internal FontCacheManagerMsdf FontCacheManagerMsdf => FontSystem != null ? FontSystem.FontCacheManagerMsdf : null;

        internal override FontSystem FontSystem
        {
            set
            {
                if (FontSystem == value)
                    return;

                base.FontSystem = value;

                if (FontSystem == null)
                    return;

                // Pull metrics from the font (same pattern as RuntimeRasterizedSpriteFont)
                float relativeLineSpacing;
                float relativeBaseOffsetY;
                float relativeMaxWidth;
                float relativeMaxHeight;
                FontManager.GetFontInfo(FontName, Style, out relativeLineSpacing, out relativeBaseOffsetY, out relativeMaxWidth, out relativeMaxHeight);

                DefaultLineSpacing = relativeLineSpacing * Size;
                BaseOffsetY = relativeBaseOffsetY * Size;

                // Use the parallel MSDF cache atlas list
                Textures = FontCacheManagerMsdf.Textures;

                // Identity swizzle (RGBA) — do NOT use Swizzle property; it doesn't exist here.
                swizzle = default;
            }
        }

        public RuntimeSignedDistanceFieldSpriteFont()
        {
            FontType = SpriteFontType.SDF;
        }

        public override bool IsCharPresent(char c)
        {
            return FontManager != null && FontManager.DoesFontContains(FontName, Style, c);
        }

        protected override Glyph GetGlyph(CommandList commandList, char character, in Vector2 fontSize, bool uploadGpuResources, out Vector2 fixScaling)
        {
            var cache = FontCacheManagerMsdf;
            if (cache == null)
                throw new InvalidOperationException("RuntimeSignedDistanceFieldSpriteFont requires FontSystem.FontCacheManagerMsdf to be initialized.");

            // Bake once at BakeSize, scale to requested fontSize
            var baked = new Vector2(BakeSize, BakeSize);
            fixScaling = fontSize / baked;

            if (!characters.TryGetValue(character, out var entry))
            {
                entry = new CharacterEntry();
                characters.Add(character, entry);
            }

            // 1) Ensure we have a CPU bitmap (placeholder for M3)
            if (entry.Bitmap == null && !entry.IsBitmapUploaded)
            {
                entry.Bitmap = GeneratePlaceholderMsdfBitmap(character, BakeSize, PixelRange, Padding, out var missing);

                if (missing)
                {
                    entry.Bitmap?.Dispose();
                    entry.Bitmap = null;

                    if (character != DefaultCharacter && DefaultCharacter.HasValue)
                        return GetGlyph(commandList, DefaultCharacter.Value, in fontSize, uploadGpuResources, out fixScaling);

                    return null;
                }

                // Ensure there is at least a glyph object for MeasureString/layout.
                if (entry.Glyph == null)
                {
                    entry.Glyph = new Glyph
                    {
                        Character = character,
                        Subrect = new Rectangle(0, 0, entry.Bitmap.Width, entry.Bitmap.Rows), // placeholder
                        Offset = Vector2.Zero,
                        XAdvance = BakeSize * 0.6f,
                        BitmapIndex = 0,
                    };
                }
            }

            // 2) Upload only when requested (render path). MeasureString passes uploadGpuResources=false and commandList=null.
            if (uploadGpuResources && commandList != null && entry.Bitmap != null && !entry.IsBitmapUploaded)
            {
                var subrect = new Rectangle();
                cache.UploadGlyphBitmap(commandList, entry.Bitmap, ref subrect, out var bitmapIndex);

                entry.Glyph.Subrect = subrect;
                entry.Glyph.BitmapIndex = bitmapIndex;

                entry.IsBitmapUploaded = true;

                // Free CPU bitmap after upload (same spirit as runtime raster)
                entry.Bitmap.Dispose();
                entry.Bitmap = null;
            }

            return entry.Glyph;
        }


        private static unsafe CharacterBitmapRgba GeneratePlaceholderMsdfBitmap(char character, int bakeSize, int pixelRange, int padding, out bool missing)
        {
            missing = false;

            var w = Math.Max(1, bakeSize + padding * 2);
            var h = Math.Max(1, bakeSize + padding * 2);

            var bmp = new CharacterBitmapRgba(w, h);
            var ptr = (byte*)bmp.Buffer;

            // Placeholder: radial gradient replicated across RGB so median(R,G,B)=v
            for (int y = 0; y < h; y++)
            {
                var row = ptr + y * bmp.Pitch;
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w * 0.5f) / (w * 0.5f);
                    float dy = (y - h * 0.5f) / (h * 0.5f);
                    float d = MathF.Sqrt(dx * dx + dy * dy);

                    float v = Math.Clamp(1.0f - d, 0, 1);
                    byte b = (byte)(v * 255);

                    row[x * 4 + 0] = b;
                    row[x * 4 + 1] = b;
                    row[x * 4 + 2] = b;
                    row[x * 4 + 3] = 255;
                }
            }

            return bmp;
        }

        private sealed class CharacterEntry
        {
            public CharacterBitmapRgba Bitmap;
            public bool IsBitmapUploaded;
            public Glyph Glyph;
        }
    }
}
