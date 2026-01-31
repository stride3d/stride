using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Channels;
using System.Threading.Tasks;
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

        internal int BakeSize = 32;
        internal int PixelRange = 8;
        internal int Padding = 2;

        internal bool UseKerning;

        // Single-size runtime SDF: key is just char
        private readonly Dictionary<char, CharacterSpecification> characters = new Dictionary<char, CharacterSpecification>();
        private readonly Dictionary<char, FontCacheManagerMsdf.MsdfCachedGlyph> cacheRecords
    = new Dictionary<char, FontCacheManagerMsdf.MsdfCachedGlyph>();
        private readonly Channel<SdfWorkItem> workQueue = Channel.CreateUnbounded<SdfWorkItem>(
    new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private readonly HashSet<char> offsetAdjusted = new HashSet<char>();
        
        [DataMemberIgnore]
        internal FontManager FontManager => FontSystem != null ? FontSystem.FontManager : null;

        [DataMemberIgnore]
        internal FontCacheManagerMsdf FontCacheManagerMsdf => FontSystem != null ? FontSystem.FontCacheManagerMsdf : null;

        // 1) Dedup scheduling
        private readonly System.Collections.Concurrent.ConcurrentDictionary<char, byte> inFlight
            = new();

        // 2) Generated SDF results waiting for GPU upload
        private readonly System.Collections.Concurrent.ConcurrentQueue<(char c, CharacterBitmapRgba sdf, int pad)> readyForUpload
            = new();

        // 3) Limit CPU concurrency (otherwise “paragraph appears” spawns 500 tasks)
        private readonly System.Threading.SemaphoreSlim genBudget = new(initialCount: 2, maxCount: 2);

        private struct SdfWorkItem
        {
            public char Character;
            public byte[] SourceBuffer;
            public int Width;
            public int Rows;
            public int Pitch;
            public int Pad;
        }



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

            // IMPORTANT:
            // SDF fonts are scaled by Stride using requestedFontSize vs SpriteFont.Size.
            // fixScaling should only compensate baked glyph pixel size (BakeSize) -> logical font size (Size).
            var logicalSizeVec = new Vector2(Size, Size);
            fixScaling = logicalSizeVec / bakedSizeVec;

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

            // 2) Schedule async SDF generation (only once per char)
            EnsureSdfScheduled(character, spec);
            if (commandList != null)
                DrainUploads(commandList);

            // 3) Upload

            if (spec.IsBitmapUploaded && cacheRecords.TryGetValue(character, out var handle))
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

            // Async pregen glyphs
            var bakedSizeVec = new Vector2(BakeSize, BakeSize);

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var spec = GetOrCreateCharacterData(c, bakedSizeVec);

                if (spec.Bitmap == null)
                    FontManager.GenerateBitmap(spec, true);
                
                EnsureSdfScheduled(c, spec);

            }
        }

        private CharacterSpecification GetOrCreateCharacterData(char character, Vector2 bakedSize)
        {
            if (!characters.TryGetValue(character, out var spec))
            {
                // AntiAlias: use AntiAliased so coverage bitmap is smooth
                spec = new CharacterSpecification(character, FontName, bakedSize, Style, FontAntiAliasMode.Grayscale);
                spec.Glyph.Subrect = Rectangle.Empty;
                spec.Glyph.BitmapIndex = 0;
                spec.IsBitmapUploaded = false;
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

        private static unsafe CharacterBitmapRgba BuildSdfRgbFromCoverage(byte[] src, int srcW, int srcH, int srcPitch, int pad, int pixelRange)
        {
            int w = srcW + pad * 2;
            int h = srcH + pad * 2;

            var inside = new bool[w * h];

            for (int y = 0; y < srcH; y++)
            {
                int dstRow = (y + pad) * w + pad;
                int srcRow = y * srcPitch;

                for (int x = 0; x < srcW; x++)
                {
                    inside[dstRow + x] = src[srcRow + x] >= 128;
                }
            }

            var distToOutsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: false, distToOutsideSq);

            var distToInsideSq = new float[w * h];
            ComputeEdtSquared(w, h, inside, featureIsInside: true, distToInsideSq);

            var bmp = new CharacterBitmapRgba(w, h);
            byte* dst = (byte*)bmp.Buffer;

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
            int maxDim = Math.Max(w, h);

            // Rent buffers from the shared pool instead of 'new'
            float[] tmp = ArrayPool<float>.Shared.Rent(w * h);
            float[] f = ArrayPool<float>.Shared.Rent(maxDim);
            float[] d = ArrayPool<float>.Shared.Rent(maxDim);
            int[] v = ArrayPool<int>.Shared.Rent(maxDim);
            float[] z = ArrayPool<float>.Shared.Rent(maxDim + 1);

            try
            {
                // Stage 1: vertical transform
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
            finally
            {
                // ALWAYS return the arrays so they can be reused
                ArrayPool<float>.Shared.Return(tmp);
                ArrayPool<float>.Shared.Return(f);
                ArrayPool<float>.Shared.Return(d);
                ArrayPool<int>.Shared.Return(v);
                ArrayPool<float>.Shared.Return(z);
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

        private void StartWorker()
        {
            Task.Run(async () =>
            {
                // One persistent loop instead of 1000s of discarded tasks
                await foreach (var work in workQueue.Reader.ReadAllAsync())
                {
                    try
                    {
                        var sdf = BuildSdfRgbFromCoverage(
                            work.SourceBuffer, work.Width, work.Rows, work.Pitch, work.Pad, Math.Max(1, PixelRange));

                        readyForUpload.Enqueue((work.Character, sdf, work.Pad));
                    }
                    finally
                    {
                        inFlight.TryRemove(work.Character, out _);
                    }
                }
            });
        }
        private void EnsureSdfScheduled(char c, CharacterSpecification spec)
        {
            if (spec.IsBitmapUploaded) return;

            var bmp = spec.Bitmap;
            if (bmp == null || bmp.Width == 0 || bmp.Rows == 0) return;

            // Fast atomic check
            if (!inFlight.TryAdd(c, 0)) return;

            // Defensive copy
            var srcCopy = new byte[bmp.Pitch * bmp.Rows];
            unsafe { System.Runtime.InteropServices.Marshal.Copy((IntPtr)bmp.Buffer, srcCopy, 0, srcCopy.Length); }

            var work = new SdfWorkItem
            {
                Character = c,
                SourceBuffer = srcCopy,
                Width = bmp.Width,
                Rows = bmp.Rows,
                Pitch = bmp.Pitch,
                Pad = ComputeTotalPad()
            };

            // TryWrite is non-blocking and allocation-free for the queue itself
            if (!workQueue.Writer.TryWrite(work))
            {
                inFlight.TryRemove(c, out _);
            }
        }

        private void DrainUploads(CommandList commandList, int maxUploadsPerFrame = 8)
        {
            var cache = FontCacheManagerMsdf;
            if (cache == null) return;

            for (int i = 0; i < maxUploadsPerFrame; i++)
            {
                if (!readyForUpload.TryDequeue(out var item))
                    break;

                var (c, sdfBitmap, pad) = item;

                if (!characters.TryGetValue(c, out var spec) || spec == null)
                {
                    sdfBitmap.Dispose();
                    continue;
                }

                // Might have been uploaded already while task was running
                if (spec.IsBitmapUploaded)
                {
                    sdfBitmap.Dispose();
                    continue;
                }

                var subrect = new Rectangle();
                var handle = cache.UploadGlyphBitmap(commandList, spec, sdfBitmap, ref subrect, out var bitmapIndex);

                spec.Glyph.Subrect = handle.InnerSubrect;
                spec.Glyph.BitmapIndex = bitmapIndex;
                spec.IsBitmapUploaded = true;

                cacheRecords[c] = handle;
                cache.NotifyGlyphUtilization(handle);

                if (offsetAdjusted.Add(c))
                    spec.Glyph.Offset -= new Vector2(pad, pad);

                sdfBitmap.Dispose();
            }
        }

    }
}
