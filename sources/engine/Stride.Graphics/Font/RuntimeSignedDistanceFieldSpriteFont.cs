using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Graphics.Font.RuntimeMsdf;

namespace Stride.Graphics.Font
{    /// <summary>
     /// A dynamic font that asynchronously generates multi-channel signed distance mapping for glyphs as needed, enabling sharp, smooth edges and resizability.
     /// </summary>
    [ReferenceSerializer, DataSerializerGlobal(typeof(ReferenceSerializer<RuntimeSignedDistanceFieldSpriteFont>), Profile = "Content")]
    [ContentSerializer(typeof(RuntimeSignedDistanceFieldSpriteFontContentSerializer))]
    [DataSerializer(typeof(RuntimeSignedDistanceFieldSpriteFontSerializer))]
    internal sealed partial class RuntimeSignedDistanceFieldSpriteFont : SpriteFont
    {
        internal string FontName;
        internal FontStyle Style;

        internal int PixelRange = 8;
        internal int Padding = 2;

        internal bool UseKerning;

        // Runtime SDF glyph cache key (future-proof for multiple ranges/modes)
        private readonly ConcurrentDictionary<GlyphKey, CharacterSpecification> characters = [];
        private readonly ConcurrentDictionary<GlyphKey, FontCacheManagerMsdf.MsdfCachedGlyph> cacheRecords = [];

        [DataMemberIgnore]
        private FontManager FontManager => FontSystem?.FontManager;

        [DataMemberIgnore]
        private FontCacheManagerMsdf FontCacheManagerMsdf => FontSystem?.FontCacheManagerMsdf;

        // Async wiring
        // 1) Dedup scheduling
        private readonly System.Collections.Concurrent.ConcurrentDictionary<GlyphKey, byte> inFlight = new();

        // 2) Generated SDF results waiting for GPU upload
        private readonly System.Collections.Concurrent.ConcurrentQueue<(GlyphKey key, CharacterBitmapRgba sdf)> readyForUpload = new();

        // --- Bounded work queue + fixed worker pool ---

        private const int WorkQueueCapacity = 1024;   // backpressure / memory safety
        private const int WorkerCount = 2;

        private Channel<WorkItem> workChannel;
        private CancellationTokenSource workCts;
        private Task[] workers;

        private readonly record struct WorkItem(
            GlyphKey Key,
            GlyphInput Input,
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

            var p = GetDistanceFieldParams();

            // IMPORTANT:
            // SDF fonts are scaled by Stride using requestedFontSize vs SpriteFont.Size.
            // Glyphs are baked at Size, so no compensating scaling is required.
            fixScaling = Vector2.One;

            var key = MakeKey(character, p);
            var spec = GetOrCreateCharacterData(key, sizeVec);

            // 1) Ensure we have the coverage bitmap + correct metrics (sync)
            if (spec.Bitmap == null)
            {
                FontManager.GenerateBitmap(spec, true);

                //Apply padding offset once metrics are loaded
                if (spec.Bitmap != null && spec.Glyph.XAdvance != 0)
                {
                    spec.Glyph.Offset -= new Vector2(p.Pad, p.Pad);
                }

                // Missing glyph (glyphIndex == 0 => XAdvance==0 and Bitmap null/empty)
                if (spec.Bitmap == null || spec.Bitmap.Width == 0 || spec.Bitmap.Rows == 0 || spec.Glyph.XAdvance == 0)
                {
                    if (character != DefaultCharacter && DefaultCharacter.HasValue)
                        return GetGlyph(commandList, DefaultCharacter.Value, in fontSize, uploadGpuResources, out fixScaling);

                    return null;
                }
            }

            // 2) Schedule async SDF generation (only once per char)
            EnsureSdfScheduled(key, spec);
            
            // 3) Upload
            if (commandList != null)
                DrainUploads(commandList);
                        
            if (spec.IsBitmapUploaded && cacheRecords.TryGetValue(key, out var handle))
            {
                // If evicted/cleared, this will flip false and we’ll reupload next draw
                if (!handle.IsUploaded)
                {
                    spec.IsBitmapUploaded = false;
                    cacheRecords.TryRemove(key, out _);
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
            var p = GetDistanceFieldParams();

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var key = MakeKey(c, p);
                var spec = GetOrCreateCharacterData(key, sizeVec);

                if (spec.Bitmap == null)
                {
                    FontManager.GenerateBitmap(spec, true);

                    // Apply padding offset once, when glyph metrics are first materialized.
                    if (spec.Bitmap != null && spec.Glyph.XAdvance != 0)
                    {
                        spec.Glyph.Offset -= new Vector2(p.Pad, p.Pad);
                    }
                }

                EnsureSdfScheduled(key, spec);
            }
        }

        private CharacterSpecification GetOrCreateCharacterData(GlyphKey key, Vector2 size)
        {
            if (!characters.TryGetValue(key, out var spec))
            {
                // AntiAlias: use AntiAliased so coverage bitmap is smooth
                spec = new CharacterSpecification(key.C, FontName, size, Style, FontAntiAliasMode.Grayscale);
                spec.Glyph.Subrect = Rectangle.Empty;
                spec.Glyph.BitmapIndex = 0;
                spec.IsBitmapUploaded = false;
                characters[key] = spec;
            }

            return spec;
        }

        private int ComputeTotalPad()
        {
            // Generally, want enough room to represent distance out to PixelRange, 
            // plus explicit Padding.
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
                            var sdf = generator.Generate(item.Input, item.Params);

                            // hand off to render thread for GPU upload
                            readyForUpload.Enqueue((item.Key, sdf));
                        }
                        catch
                        {
                            // If generation fails, we just allow rescheduling later.
                        }
                        finally
                        {
                            if (item.Input is CoverageInput c)
                                ArrayPool<byte>.Shared.Return(c.Buffer);

                            inFlight.TryRemove(item.Key, out _);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        private void EnsureSdfScheduled(GlyphKey key, CharacterSpecification spec)
        {
            // Already uploaded? nothing to do.
            if (spec.IsBitmapUploaded) return;

            // Ensure worker infrastructure is alive (safe even if already started)
            EnsureWorkersStarted();

            // Already scheduled? bail.
            if (!inFlight.TryAdd(key, 0)) return;

            var p = new DistanceFieldParams(key.PixelRange, key.Pad, DefaultEncode);


            // Try Outline-based MSDF first
            // Uses the merged TryGetGlyphOutline signature
            if (FontManager != null &&
                FontManager.TryGetGlyphOutline(FontName, Style, new Vector2(Size, Size), key.C, out var outline, out _))
            {
                // Resolve dimensions: Prefer existing bitmap metrics, fallback to outline bounds
                int w = (spec.Bitmap != null && spec.Bitmap.Width > 0) ? spec.Bitmap.Width : (outline != null ? (int)MathF.Ceiling(outline.Bounds.Width) : 0);
                int h = (spec.Bitmap != null && spec.Bitmap.Rows > 0) ? spec.Bitmap.Rows : (outline != null ? (int)MathF.Ceiling(outline.Bounds.Height) : 0);

                // Handle zero-dimension glyphs (like spaces) AND oversized glyphs immediately
                var cache = FontCacheManagerMsdf;
                if (w <= 0 || h <= 0 ||
                    w + cache.AtlasPaddingPixels * 2 > cache.Textures[0].ViewWidth ||
                    h + cache.AtlasPaddingPixels * 2 > cache.Textures[0].ViewHeight)
                {
                    inFlight.TryRemove(key, out _);
                    return;
                }

                // If the queue is full, exit now. 
                // Do NOT fall through to the coverage logic if the channel is already saturated.
                if (workChannel.Writer.TryWrite(new WorkItem(key, new OutlineInput(outline, w, h), p)))
                    return;

                inFlight.TryRemove(key, out _);
                return;
            }

            // Fallback: bitmap/coverage-based SDF.
            var bmp = spec.Bitmap;
            if (bmp == null || bmp.Width == 0 || bmp.Rows == 0)
            {
                inFlight.TryRemove(key, out _);
                return;
            }

            // Copy coverage bitmap to a pooled array so background thread is safe (avoid per-glyph allocations).
            int len = bmp.Pitch * bmp.Rows;
            var srcCopy = ArrayPool<byte>.Shared.Rent(len);
            try
            {
                System.Runtime.InteropServices.Marshal.Copy(bmp.Buffer, srcCopy, 0, len);
                var input = new CoverageInput(srcCopy, len, bmp.Width, bmp.Rows, bmp.Pitch);

                // Render thread must NEVER block: TryWrite only.
                if (!workChannel.Writer.TryWrite(new WorkItem(key, input, p)))
                {
                    // Queue full; allow retry next frame
                    ArrayPool<byte>.Shared.Return(srcCopy);
                    inFlight.TryRemove(key, out _);
                }
            }
            catch
            {
                ArrayPool<byte>.Shared.Return(srcCopy);
                inFlight.TryRemove(key, out _);
                throw;
            }
        }

        private void ApplyUploadedGlyph(FontCacheManagerMsdf cache, GlyphKey key, CharacterSpecification spec, FontCacheManagerMsdf.MsdfCachedGlyph handle, int bitmapIndex)
        {
            spec.Glyph.Subrect = handle.InnerSubrect;
            spec.Glyph.BitmapIndex = bitmapIndex;
            spec.IsBitmapUploaded = true;

            cacheRecords[key] = handle;
            cache.NotifyGlyphUtilization(handle);

        }

        internal void PrepareGlyphsForThumbnail(string text, Vector2 requestedSize, CommandList commandList, int maxWaitMilliseconds = 50)
        {
            if (string.IsNullOrEmpty(text) || commandList == null)
                return;

            var sizeVec = new Vector2(Size, Size);
            var p = GetDistanceFieldParams();

            var requestedKeys = new HashSet<GlyphKey>();

            for (int i = 0; i < text.Length; i++)
            {
                var c = text[i];
                var key = MakeKey(c, p);
                requestedKeys.Add(key);

                var spec = GetOrCreateCharacterData(key, sizeVec);

                if (spec.Bitmap == null)
                {
                    FontManager.GenerateBitmap(spec, true);

                    if (spec.Bitmap != null && spec.Glyph.XAdvance != 0)
                    {
                        spec.Glyph.Offset -= new Vector2(p.Pad, p.Pad);
                    }

                    if (spec.Bitmap == null || spec.Bitmap.Width == 0 || spec.Bitmap.Rows == 0 || spec.Glyph.XAdvance == 0)
                    {
                        if (c != DefaultCharacter && DefaultCharacter.HasValue)
                            requestedKeys.Add(MakeKey(DefaultCharacter.Value, p));

                        continue;
                    }
                }

                EnsureSdfScheduled(key, spec);
            }

            if (requestedKeys.Count == 0)
                return;

            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.ElapsedMilliseconds < maxWaitMilliseconds)
            {
                DrainUploads(commandList, maxUploadsPerFrame: requestedKeys.Count);

                if (AreAllGlyphsUploaded(requestedKeys))
                    break;

                Thread.Sleep(1);
            }

            // One last drain pass in case workers completed right at the timeout boundary.
            DrainUploads(commandList, maxUploadsPerFrame: requestedKeys.Count);
        }

        private bool AreAllGlyphsUploaded(HashSet<GlyphKey> requestedKeys)
        {
            foreach (var key in requestedKeys)
            {
                if (!characters.TryGetValue(key, out var spec) || spec == null)
                    return false;

                if (!spec.IsBitmapUploaded)
                    return false;

                if (cacheRecords.TryGetValue(key, out var handle) && !handle.IsUploaded)
                {
                    spec.IsBitmapUploaded = false;
                    cacheRecords.TryRemove(key, out _);
                    return false;
                }
            }

            return true;
        }

        private void DrainUploads(CommandList commandList, int maxUploadsPerFrame = 8)
        {
            var cache = FontCacheManagerMsdf;
            if (cache == null) return;

            for (int i = 0; i < maxUploadsPerFrame; i++)
            {
                if (!readyForUpload.TryDequeue(out var item))
                    break;

                var (key, sdfBitmap) = item;

                if (!characters.TryGetValue(key, out var spec) || spec == null)
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

                ApplyUploadedGlyph(cache, key, spec, handle, bitmapIndex);

                sdfBitmap.Dispose();
            }
        }
    }
}
