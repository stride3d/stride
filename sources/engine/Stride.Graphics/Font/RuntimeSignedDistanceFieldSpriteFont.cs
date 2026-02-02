using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
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

        internal int PixelRange = 8;
        internal int Padding = 2;

        internal bool UseKerning;

        // --- Distance field configuration & generator seam (MSDF-ready) ---

        private readonly record struct DistanceEncodeParams(float Bias, float Scale);
        private readonly record struct DistanceFieldParams(int PixelRange, int Pad, DistanceEncodeParams Encode);

        // Keep today’s encoding behavior explicit and centralized.
        private static readonly DistanceEncodeParams DefaultEncode = new(Bias: 0.4f, Scale: 0.5f);

        private DistanceFieldParams GetDfParams()
        {
            int pixelRange = Math.Max(1, PixelRange);
            int pad = ComputeTotalPad();
            return new DistanceFieldParams(pixelRange, pad, DefaultEncode);
        }

        private interface IDistanceFieldGenerator
        {
            // Current path: coverage bitmap → packed RGBA bitmap
            CharacterBitmapRgba GenerateFromCoverage(byte[] coverage, int width, int rows, int pitch, DistanceFieldParams p);

            // Future path: outline → MSDF (placeholder for msdfgen integration)
            // CharacterBitmapRgba GenerateFromOutline(GlyphOutline outline, DistanceFieldParams p);
        }

        private sealed class SdfCoverageGenerator : IDistanceFieldGenerator
        {
            public CharacterBitmapRgba GenerateFromCoverage(byte[] coverage, int width, int rows, int pitch, DistanceFieldParams p)
                => BuildSdfRgbFromCoverage(coverage, width, rows, pitch, p.Pad, p.PixelRange, p.Encode);
        }

        private readonly IDistanceFieldGenerator generator = new SdfCoverageGenerator();

        // Single-size runtime SDF: key is just char
        private readonly Dictionary<char, CharacterSpecification> characters = [];
        private readonly Dictionary<char, FontCacheManagerMsdf.MsdfCachedGlyph> cacheRecords = [];

        private readonly HashSet<char> offsetAdjusted = [];

        [DataMemberIgnore]
        internal FontManager FontManager => FontSystem?.FontManager;

        [DataMemberIgnore]
        internal FontCacheManagerMsdf FontCacheManagerMsdf => FontSystem?.FontCacheManagerMsdf;

        // Async wiring
        // 1) Dedup scheduling
        private readonly System.Collections.Concurrent.ConcurrentDictionary<char, byte> inFlight = new();

        // 2) Generated SDF results waiting for GPU upload
        private readonly System.Collections.Concurrent.ConcurrentQueue<(char c, CharacterBitmapRgba sdf, int pad)> readyForUpload = new();

        // --- Bounded work queue + fixed worker pool ---

        private const int WorkQueueCapacity = 1024;   // backpressure / memory safety
        private const int WorkerCount = 2;

        private Channel<WorkItem> workChannel;
        private CancellationTokenSource workCts;
        private Task[] workers;

        private readonly record struct WorkItem(
            char C,
            byte[] Src,
            int Width,
            int Rows,
            int Pitch,
            DistanceFieldParams Params);

        internal override FontSystem FontSystem
        {
            set
            {
                if (FontSystem == value)
                    return;

                // if we're detaching, shut down background workers
                if (FontSystem != null && value == null)
                {
                    ShutdownWorkers();
                }

                base.FontSystem = value;

                if (FontSystem == null)
                    return;

                EnsureWorkersStarted();

                // Metrics from font
                FontManager.GetFontInfo(FontName, Style, out var relativeLineSpacing, out var relativeBaseOffsetY, out var relativeMaxWidth, out var relativeMaxHeight);

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
            var cache = FontCacheManagerMsdf ?? throw new InvalidOperationException("RuntimeSignedDistanceFieldSpriteFont requires FontSystem.FontCacheManagerMsdf to be initialized.");

            // All glyphs are generated at Size
            var sizeVec = new Vector2(Size, Size);

            // IMPORTANT:
            // SDF fonts are scaled by Stride using requestedFontSize vs SpriteFont.Size.
            // Glyphs are baked at Size, so no compensating scaling is required.
            fixScaling = Vector2.One;

            var spec = GetOrCreateCharacterData(character, sizeVec);

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
            var sizeVec = new Vector2(Size, Size);

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var spec = GetOrCreateCharacterData(c, sizeVec);

                if (spec.Bitmap == null)
                    FontManager.GenerateBitmap(spec, true);

                EnsureSdfScheduled(c, spec);

            }
        }

        private CharacterSpecification GetOrCreateCharacterData(char character, Vector2 size)
        {
            if (!characters.TryGetValue(character, out var spec))
            {
                // AntiAlias: use AntiAliased so coverage bitmap is smooth
                spec = new CharacterSpecification(character, FontName, size, Style, FontAntiAliasMode.Grayscale);
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

        private void EnsureWorkersStarted()
        {
            if (workChannel != null)
                return;

            workCts = new CancellationTokenSource();

            workChannel = Channel.CreateBounded<WorkItem>(new BoundedChannelOptions(WorkQueueCapacity)
            {
                SingleWriter = false,
                SingleReader = false,
                // Writers that await will wait; render thread uses TryWrite so it never blocks.
                FullMode = BoundedChannelFullMode.Wait
            });

            workers = new Task[WorkerCount];
            for (int i = 0; i < workers.Length; i++)
                workers[i] = Task.Run(() => WorkerLoop(workCts.Token));
        }

        private void ShutdownWorkers()
        {
            if (workChannel == null)
                return;

            try
            {
                workCts.Cancel();
                workChannel.Writer.TryComplete();
                try { Task.WaitAll(workers); } catch { /* ignore shutdown exceptions */ }
            }
            finally
            {
                workCts.Dispose();
                workCts = null;
                workChannel = null;
                workers = null;
            }
        }

        private async Task WorkerLoop(CancellationToken token)
        {
            try
            {
                var reader = workChannel.Reader;

                while (await reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var item))
                    {
                        try
                        {
                            // CPU SDF build
                            var sdf = generator.GenerateFromCoverage(item.Src, item.Width, item.Rows, item.Pitch, item.Params);

                            // hand off to render thread for GPU upload
                            readyForUpload.Enqueue((item.C, sdf, item.Params.Pad));
                        }
                        catch
                        {
                            // If generation fails, we just allow rescheduling later.
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(item.Src);
                            inFlight.TryRemove(item.C, out _);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        private void EnsureSdfScheduled(char c, CharacterSpecification spec)
        {
            // Already uploaded? nothing to do.
            if (spec.IsBitmapUploaded) return;

            // Already have bitmap? If not, we can’t generate.
            var bmp = spec.Bitmap;
            if (bmp == null || bmp.Width == 0 || bmp.Rows == 0) return;

            // Ensure worker infrastructure is alive (safe even if already started)
            EnsureWorkersStarted();

            // Already scheduled? bail.
            if (!inFlight.TryAdd(c, 0)) return;

            var p = GetDfParams();

            // Copy coverage bitmap to a pooled array so background thread is safe (avoid per-glyph allocations).
            var width = bmp.Width;
            var rows = bmp.Rows;
            var pitch = bmp.Pitch;

            int len = pitch * rows;
            var srcCopy = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                unsafe
                {
                    System.Runtime.InteropServices.Marshal.Copy((IntPtr)bmp.Buffer, srcCopy, 0, len);
                }

                // Render thread must NEVER block: TryWrite only.
                if (!workChannel.Writer.TryWrite(new WorkItem(c, srcCopy, width, rows, pitch, p)))
                {
                    // Queue full; allow retry next frame
                    ArrayPool<byte>.Shared.Return(srcCopy);
                    inFlight.TryRemove(c, out _);
                }
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(srcCopy);
                inFlight.TryRemove(c, out _);
                throw;
            }
        }

        private void ApplyUploadedGlyph(FontCacheManagerMsdf cache, char c, CharacterSpecification spec, FontCacheManagerMsdf.MsdfCachedGlyph handle, int bitmapIndex, int pad)
        {
            spec.Glyph.Subrect = handle.InnerSubrect;
            spec.Glyph.BitmapIndex = bitmapIndex;
            spec.IsBitmapUploaded = true;

            cacheRecords[c] = handle;
            cache.NotifyGlyphUtilization(handle);

            if (offsetAdjusted.Add(c))
                spec.Glyph.Offset -= new Vector2(pad, pad);
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

                ApplyUploadedGlyph(cache, c, spec, handle, bitmapIndex, pad);

                sdfBitmap.Dispose();
            }
        }


        // --- SDF generation (CPU), packed into RGB so median(R,G,B)=SDF value ---

        private static unsafe CharacterBitmapRgba BuildSdfRgbFromCoverage(byte[] src, int srcW, int srcH, int srcPitch, int pad, int pixelRange, DistanceEncodeParams enc)
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

            float scale = enc.Scale / Math.Max(1, pixelRange);

            float bias = enc.Bias;
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

                    float encoded = Math.Clamp(bias + signed * scale, 0f, 1f);
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
    }
}
