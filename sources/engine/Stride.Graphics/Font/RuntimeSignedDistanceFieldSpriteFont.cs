using System;
using System.Collections.Generic;
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
        internal string FontName;
        internal FontStyle Style;

        internal int BakeSize = 64;
        internal int PixelRange = 8;
        internal int Padding = 2;

        internal bool UseKerning;

        // Single-size runtime SDF: key is just char
        private readonly Dictionary<char, CharacterSpecification> characters = new Dictionary<char, CharacterSpecification>();
        private readonly Dictionary<char, CharacterBitmapRgba> pendingSdfBitmaps = new Dictionary<char, CharacterBitmapRgba>();
        private readonly Dictionary<char, FontCacheManagerMsdf.MsdfCachedGlyph> cacheRecords
    = new Dictionary<char, FontCacheManagerMsdf.MsdfCachedGlyph>();

        private readonly HashSet<char> offsetAdjusted = new HashSet<char>();
        
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

                // Metrics from font
                float relativeLineSpacing;
                float relativeBaseOffsetY;
                float relativeMaxWidth;
                float relativeMaxHeight;
                FontManager.GetFontInfo(FontName, Style, out relativeLineSpacing, out relativeBaseOffsetY, out relativeMaxWidth, out relativeMaxHeight);

                DefaultLineSpacing = relativeLineSpacing * Size;
                BaseOffsetY = relativeBaseOffsetY * Size;

                // Use RGBA MSDF cache textures
                Textures = FontCacheManagerMsdf.Textures;

                // Keep channels as-is (RGB median used by shader)
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

            // All glyphs baked at BakeSize
            var bakedSizeVec = new Vector2(BakeSize, BakeSize);
            fixScaling = fontSize / bakedSizeVec;

            var spec = GetOrCreateCharacterData(character, bakedSizeVec);

            // 1) Ensure we have the coverage bitmap + correct metrics (sync)
            if (spec.Bitmap == null)
            {
                FontManager.GenerateBitmap(spec, true);

                // Missing glyph (glyphIndex == 0 => XAdvance==0 and Bitmap null/empty)
                if (spec.Bitmap == null || spec.Bitmap.Width == 0 || spec.Bitmap.Rows == 0 || spec.Glyph.XAdvance == 0)
                {
                    if (character != DefaultCharacter && DefaultCharacter.HasValue)
                        return GetGlyph(commandList, DefaultCharacter.Value, in fontSize, uploadGpuResources, out fixScaling);

                    return null;
                }
            }

            // 2) Build SDF bitmap once (CPU)
            if (!pendingSdfBitmaps.TryGetValue(character, out var sdfBitmap))
            {
                var pad = ComputeTotalPad();
                sdfBitmap = BuildSdfRgbFromCoverage(spec.Bitmap, pad, Math.Max(1, PixelRange));

                // IMPORTANT: our bitmap now has extra pixels on left/top => shift offset
                if (offsetAdjusted.Add(character))
                {
                    spec.Glyph.Offset -= new Vector2(pad, pad);
                }

                // Give subrect sane dimensions even before upload (not critical for MeasureString but harmless)
                spec.Glyph.Subrect = new Rectangle(0, 0, sdfBitmap.Width, sdfBitmap.Rows);
                spec.IsBitmapUploaded = false;

                pendingSdfBitmaps[character] = sdfBitmap;
            }

            // 3) Upload only when drawing (MeasureString passes uploadGpuResources=false and commandList=null)
            if (commandList != null && !spec.IsBitmapUploaded)
            {
                var subrect = new Rectangle();
                var handle = cache.UploadGlyphBitmap(commandList, spec, sdfBitmap, ref subrect, out var bitmapIndex);

                spec.Glyph.Subrect = handle.InnerSubrect;
                spec.Glyph.BitmapIndex = bitmapIndex;
                spec.IsBitmapUploaded = true;

                cacheRecords[character] = handle;
                cache.NotifyGlyphUtilization(handle);

                sdfBitmap.Dispose();
                pendingSdfBitmaps.Remove(character);
            }
            else if (spec.IsBitmapUploaded && cacheRecords.TryGetValue(character, out var handle))
            {
                // If evicted/cleared, this will flip false and we’ll reupload next draw
                if (!handle.IsUploaded)
                {
                    spec.IsBitmapUploaded = false;
                    cacheRecords.Remove(character);
                }
                else
                {
                    cache.NotifyGlyphUtilization(handle);
                }
            }

            return spec.Glyph;
        }

        internal override void PreGenerateGlyphs(ref StringProxy text, ref Vector2 size)
        {
            // Sync pregen for M4.1 proof-of-work (matches your preference)
            var bakedSizeVec = new Vector2(BakeSize, BakeSize);

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var spec = GetOrCreateCharacterData(c, bakedSizeVec);

                if (spec.Bitmap == null)
                    FontManager.GenerateBitmap(spec, true);

                if (spec.Bitmap != null && spec.Bitmap.Width != 0 && spec.Bitmap.Rows != 0 && !pendingSdfBitmaps.ContainsKey(c))
                {
                    var pad = ComputeTotalPad();
                    var sdf = BuildSdfRgbFromCoverage(spec.Bitmap, pad, Math.Max(1, PixelRange));
                    if (offsetAdjusted.Add(c))
                        spec.Glyph.Offset -= new Vector2(pad, pad);
                    spec.Glyph.Subrect = new Rectangle(0, 0, sdf.Width, sdf.Rows);
                    pendingSdfBitmaps[c] = sdf;
                }
            }
        }

        private CharacterSpecification GetOrCreateCharacterData(char character, Vector2 bakedSize)
        {
            if (!characters.TryGetValue(character, out var spec))
            {
                // AntiAlias: use AntiAliased so coverage bitmap is smooth
                spec = new CharacterSpecification(character, FontName, bakedSize, Style, FontAntiAliasMode.Aliased);
                characters[character] = spec;
            }

            return spec;
        }

        private int ComputeTotalPad()
        {
            // You generally want enough room to represent distance out to PixelRange
            // plus your own explicit Padding.
            var pad = Padding + PixelRange;
            return Math.Max(1, pad);
        }

        // --- SDF generation (CPU), packed into RGB so median(R,G,B)=SDF value ---

        private static unsafe CharacterBitmapRgba BuildSdfRgbFromCoverage(CharacterBitmap coverage, int pad, int pixelRange)
        {
            int srcW = coverage.Width;
            int srcH = coverage.Rows;

            int w = srcW + pad * 2;
            int h = srcH + pad * 2;

            // Build inside mask from coverage into padded image
            var inside = new bool[w * h];

            byte* src = (byte*)coverage.Buffer;
            int srcPitch = coverage.Pitch; // in CharacterBitmap, pitch == width

            for (int y = 0; y < srcH; y++)
            {
                int dstRow = (y + pad) * w + pad;
                byte* srcRow = src + y * srcPitch;

                for (int x = 0; x < srcW; x++)
                {
                    inside[dstRow + x] = srcRow[x] >= 128;
                }
            }

            // EDT to nearest OUTSIDE (feature = outside)
            var distToOutsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: false, distToOutsideSq);

            // EDT to nearest INSIDE (feature = inside)
            var distToInsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: true, distToInsideSq);

            // Pack signed distance into RGB
            var bmp = new CharacterBitmapRgba(w, h);
            byte* dst = (byte*)bmp.Buffer;

            // Stride shader uses an edge around ~0.4 (see SignedDistanceFieldFont.sdsl)
            // We encode: 0.4 at boundary, +/- pixelRange maps to +/-0.5 range.
            float scale = 0.5f / Math.Max(1, pixelRange);

            for (int y = 0; y < h; y++)
            {
                byte* row = dst + y * bmp.Pitch;
                int baseIdx = y * w;

                for (int x = 0; x < w; x++)
                {
                    int i = baseIdx + x;

                    float dOut = MathF.Sqrt(distToOutsideSq[i]);
                    float dIn = MathF.Sqrt(distToInsideSq[i]);

                    // signed: + inside, - outside
                    float signed = dOut - dIn;

                    float encoded = Math.Clamp(0.4f + signed * scale, 0f, 1f);
                    byte b = (byte)(encoded * 255f + 0.5f);

                    int o = x * 4;
                    row[o + 0] = b;
                    row[o + 1] = b;
                    row[o + 2] = b;
                    row[o + 3] = 255;
                }
            }

            return bmp;
        }

        // Compute squared distances to the nearest feature pixels using Felzenszwalb/Huttenlocher EDT.
        // If featureIsInside == true, features are where inside==true; else features are where inside==false.
        private static void ComputeEdtSquared(int w, int h, bool[] inside, bool featureIsInside, float[] outDistSq)
        {
            const float INF = 1e20f;

            // Stage 1: vertical transform
            var tmp = new float[w * h];
            var f = new float[Math.Max(w, h)];
            var d = new float[Math.Max(w, h)];
            var v = new int[Math.Max(w, h)];
            var z = new float[Math.Max(w, h) + 1];

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    bool isFeature = (inside[y * w + x] == featureIsInside);
                    f[y] = isFeature ? 0f : INF;
                }

                DistanceTransform1D(f, h, d, v, z);

                for (int y = 0; y < h; y++)
                    tmp[y * w + x] = d[y];
            }

            // Stage 2: horizontal transform
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                for (int x = 0; x < w; x++)
                    f[x] = tmp[row + x];

                DistanceTransform1D(f, w, d, v, z);

                for (int x = 0; x < w; x++)
                    outDistSq[row + x] = d[x];
            }
        }

        // 1D squared distance transform for f[] using lower envelope of parabolas.
        // Produces d[i] = min_j ( (i-j)^2 + f[j] )
        private static void DistanceTransform1D(float[] f, int n, float[] d, int[] v, float[] z)
        {
            int k = 0;
            v[0] = 0;
            z[0] = float.NegativeInfinity;
            z[1] = float.PositiveInfinity;

            for (int q = 1; q < n; q++)
            {
                float s;
                while (true)
                {
                    int p = v[k];
                    // intersection of parabolas from p and q
                    s = ((f[q] + q * q) - (f[p] + p * p)) / (2f * (q - p));

                    if (s > z[k]) break;
                    k--;
                }

                k++;
                v[k] = q;
                z[k] = s;
                z[k + 1] = float.PositiveInfinity;
            }

            k = 0;
            for (int q = 0; q < n; q++)
            {
                while (z[k + 1] < q) k++;
                int p = v[k];
                float dx = q - p;
                d[q] = dx * dx + f[p];
            }
        }
    }
}
