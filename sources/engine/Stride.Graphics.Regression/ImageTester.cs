// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Regression
{
    /// <summary>
    ///   Payload for <see cref="ImageTester.ImageComparisonCompleted"/>. Carries paths and the
    ///   resulting stats; lets in-process consumers (e.g. the interactive runner) drive a
    ///   diff / promote UI without parsing log strings.
    /// </summary>
    public sealed class ImageComparisonEventArgs : EventArgs
    {
        /// <summary>Path under <c>tests/local/...</c> where the rendered output was (or
        /// would be) written. May not exist on disk for a passing test unless
        /// <c>ForceSaveImageOnSuccess</c> is set.</summary>
        public required string CurrentPath { get; init; }
        /// <summary>Path under <c>tests/...</c> of the gold image that was used for the
        /// comparison. For "missing reference" failures this is the expected gold path
        /// even though no file exists there.</summary>
        public required string ReferencePath { get; init; }
        /// <summary>True if the comparison succeeded (within thresholds). False on
        /// mismatch or missing reference.</summary>
        public required bool Passed { get; init; }
        /// <summary>Stats from the last comparison attempt — populated on success and on
        /// the failing exact-match attempt; default when no reference existed.</summary>
        public ImageTester.ComparisonStats Stats { get; init; }
    }

    public static class ImageTester
    {
        /// <summary>Fired after every screenshot/gold comparison in <see cref="GameTestBase"/>.
        /// Process-global, intended for in-process subscribers (e.g. the interactive runner UI).</summary>
        public static event EventHandler<ImageComparisonEventArgs>? ImageComparisonCompleted;

        internal static void RaiseImageComparison(ImageComparisonEventArgs args)
            => ImageComparisonCompleted?.Invoke(null, args);

        public enum ComparisonMode
        {
            /// <summary>
            /// Comparison will fails if image doesn't exist or is different.
            /// </summary>
            CompareOnly,
            /// <summary>
            /// Comparison will fails if image is different. If no version of it exist yet, it will be created.
            /// </summary>
            CompareOrCreate,
            CompareOrCreateAlternative,
        }

        public enum ComparisonResult
        {
            ReferenceCreated,
            Success,
            Failed,
        }

        /// <summary>
        /// Statistics from an image comparison.
        /// </summary>
        public struct ComparisonStats
        {
            public int TotalPixels;
            public int DifferentPixels;
            public int MaxDiff;
            public double MeanSquaredError;
            public double PSNR;
            public bool Passed;
            /// <summary>SHA-256 hex of the gold file's bytes — lets CompareGold tell
            /// if the gold the sidecar was judged against has since changed.</summary>
            public string? GoldHash;

            /// <summary>
            /// Histogram of per-pixel max channel difference.
            /// Buckets: [0]=0, [1]=1-2, [2]=3-5, [3]=6-15, [4]=16+
            /// </summary>
            public DiffHistogramBuffer DiffHistogram;

            [System.Runtime.CompilerServices.InlineArray(5)]
            public struct DiffHistogramBuffer
            {
                private int _element0;
            }

            public override readonly string ToString()
            {
                var hist = $"[1-2]:{DiffHistogram[1]} [3-5]:{DiffHistogram[2]} [6-15]:{DiffHistogram[3]} [16+]:{DiffHistogram[4]}";
                var label = Passed ? "PASS" : "FAIL";
                return $"{label} (max diff={MaxDiff}, PSNR={PSNR:F1}dB, {hist})";
            }
        }

        /// <summary>
        /// One record per gold the runtime compared the current output against. Serialized
        /// next to the output PNG so CompareGold can render pass and fail with the same data.
        /// </summary>
        public sealed class SidecarAttempt
        {
            public required string Gold { get; init; }
            /// <summary><c>"reference"</c> for the exact-match gold, <c>"alternate"</c> for
            /// fallback golds tried when the reference was missing.</summary>
            public required string Kind { get; init; }
            public required bool Passed { get; init; }
            public required int MaxDiff { get; init; }
            public required double PsnrDb { get; init; }
            /// <summary>Pixel-diff histogram keyed by range. Canonical keys, in order:
            /// <c>"0"</c>, <c>"1-2"</c>, <c>"3-5"</c>, <c>"6-15"</c>, <c>"16+"</c>.</summary>
            [JsonConverter(typeof(InlineStringIntDictConverter))]
            public required Dictionary<string, int> Buckets { get; init; }
            /// <summary>Allow rule that was applied for this comparison (e.g. <c>{"3+": 0}</c>),
            /// so CompareGold can format the brief without re-resolving thresholds.jsonc.</summary>
            [JsonConverter(typeof(InlineStringIntDictConverter))]
            public Dictionary<string, int>? Thresholds { get; init; }
            /// <summary>SHA-256 of the gold file's bytes at compare time. CompareGold
            /// compares this against the gold's current hash to detect promotions / edits
            /// without depending on filesystem mtime precision.</summary>
            public string? GoldHash { get; init; }
        }

        /// <summary>
        /// Host / GPU / driver facts attached to the gold or current run. Stable across all
        /// tests in a single process invocation. Persisted alongside gold as <c>*.metadata.json</c>.
        /// </summary>
        public sealed class SidecarMetadata
        {
            public string? Os { get; init; }
            public string? Cpu { get; init; }
            public string? Gpu { get; init; }
            /// <summary>PCI vendor ID, 0x-prefixed hex.</summary>
            public string? GpuVendorId { get; init; }
            /// <summary>PCI device ID, 0x-prefixed hex.</summary>
            public string? GpuDeviceId { get; init; }
            public string? VendorName { get; init; }
            /// <summary>VkDriverId on Vulkan; vendor name on D3D.</summary>
            public string? DriverId { get; init; }
            public string? DriverName { get; init; }
            /// <summary>Free-form driver string (Vulkan only).</summary>
            public string? DriverInfo { get; init; }
            /// <summary>Vendor-formatted version (e.g. "1.4.1", "32.0.15.7270").</summary>
            public string? DriverVersion { get; init; }
            public string? ApiName { get; init; }
            public string? ApiVersion { get; init; }
        }

        /// <summary>
        /// Sidecar emitted next to each output PNG with the comparison outcome and stats.
        /// Filename: same as the PNG with a <c>.results.json</c> extension (companion to
        /// <c>.metadata.json</c> which carries the renderer identity).
        /// </summary>
        public sealed class Sidecar
        {
            public required string Outcome { get; init; }
            public required DateTime At { get; init; }
            /// <summary>Gold path that matched (null on fail).</summary>
            public string? Matched { get; init; }
            public required List<SidecarAttempt> Attempts { get; init; }
        }

        // WriteIndented spreads dicts/arrays across lines; the bucket and threshold dicts
        // are 5 entries max and read better on one line. WriteRawValue bypasses the
        // writer's indentation pass for the rendered chunk.
        private sealed class InlineStringIntDictConverter : JsonConverter<Dictionary<string, int>>
        {
            public override Dictionary<string, int>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                JsonSerializer.Deserialize<Dictionary<string, int>>(ref reader, options);
            public override void Write(Utf8JsonWriter writer, Dictionary<string, int> value, JsonSerializerOptions options)
            {
                var sb = new System.Text.StringBuilder("{");
                bool first = true;
                foreach (var kv in value)
                {
                    if (!first) sb.Append(", ");
                    sb.Append('"').Append(kv.Key).Append("\": ").Append(kv.Value);
                    first = false;
                }
                sb.Append('}');
                writer.WriteRawValue(sb.ToString(), skipInputValidation: true);
            }
        }

        // PSNR is +Infinity on an exact match (zero pixel diff); JSON spec doesn't allow
        // infinity, so opt into .NET's named-literal extension on both ends. camelCase
        // to match standard JSON conventions (consumer is JS). UnsafeRelaxedJsonEscaping
        // so dict keys like "3+" don't render as "3+".
        public static readonly JsonSerializerOptions SidecarJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        /// <summary>Build metadata for the current run from a live <see cref="GraphicsDevice"/>.</summary>
        public static SidecarMetadata? BuildMetadata(GraphicsDevice? device)
        {
            var info = device?.Adapter?.DriverInfo;
            return new SidecarMetadata
            {
                Os = HostEnvironment.OsDescription,
                Cpu = HostEnvironment.CpuName,
                Gpu = info?.GpuName,
                GpuVendorId = info != null ? $"0x{info.VendorId:X4}" : null,
                GpuDeviceId = info != null ? $"0x{info.DeviceId:X4}" : null,
                VendorName = info?.VendorName,
                DriverId = info?.DriverId,
                DriverName = info?.DriverName,
                DriverInfo = info?.DriverInfo,
                DriverVersion = info?.DriverVersion,
                ApiName = info?.ApiName,
                ApiVersion = info?.ApiVersion,
            };
        }

        public static void SaveSidecar(string outputPngPath, Sidecar sidecar, SidecarMetadata? metadata = null)
        {
            var sidecarPath = Path.ChangeExtension(outputPngPath, ".results.json");
            DiagLog($"SaveSidecar: {sidecarPath}");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(sidecarPath));
                File.WriteAllText(sidecarPath, JsonSerializer.Serialize(sidecar, SidecarJsonOptions));
            }
            catch (Exception ex)
            {
                DiagLog($"SaveSidecar FAILED: {ex.GetType().Name}: {ex.Message}");
                throw;
            }

            // Metadata-only companion. Promoted alongside the PNG so persistent gold carries renderer identity.
            SaveMetadata(outputPngPath, metadata);
        }

        /// <summary>Write the renderer-identity <c>*.metadata.json</c> next to <paramref name="outputPngPath"/>.</summary>
        public static void SaveMetadata(string outputPngPath, SidecarMetadata? metadata)
        {
            if (metadata == null)
                return;

            var metaPath = Path.ChangeExtension(outputPngPath, ".metadata.json");
            try
            {
                File.WriteAllText(metaPath, JsonSerializer.Serialize(metadata, SidecarJsonOptions));
            }
            catch (Exception ex)
            {
                DiagLog($"SaveMetadata FAILED: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Canonical histogram-bucket keys, in order. The 5 ranges align with how the
        // runtime computes the histogram; the consumer reads them by key.
        private static readonly string[] BucketKeys = ["0", "1-2", "3-5", "6-15", "16+"];

        internal static SidecarAttempt ToSidecarAttempt(string goldPath, string referencePath, ComparisonStats stats, AllowBucket[]? thresholds)
        {
            var buckets = new Dictionary<string, int>(5);
            for (int i = 0; i < 5; i++) buckets[BucketKeys[i]] = stats.DiffHistogram[i];
            Dictionary<string, int>? thresholdDict = null;
            if (thresholds is { Length: > 0 })
            {
                thresholdDict = new Dictionary<string, int>(thresholds.Length);
                foreach (var b in thresholds)
                    thresholdDict[ImageThreshold.RangeKey(b)] = b.Limit;
            }
            return new SidecarAttempt
            {
                Gold = goldPath,
                Kind = goldPath == referencePath ? "reference" : "alternate",
                Passed = stats.Passed,
                MaxDiff = stats.MaxDiff,
                PsnrDb = stats.PSNR,
                Buckets = buckets,
                Thresholds = thresholdDict,
                GoldHash = stats.GoldHash,
            };
        }

        internal static string HashHex(byte[] data) =>
            Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(data));

        public static void SaveImage(Image image, string testFilename, SidecarMetadata? metadata = null)
        {
            DiagLog($"SaveImage: {testFilename}");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(testFilename));
                using (var stream = File.Open(testFilename, FileMode.Create))
                {
                    image.Save(stream, ImageFileType.Png);
                }
                DiagLog($"SaveImage OK: exists={File.Exists(testFilename)}, size={new FileInfo(testFilename).Length}");
            }
            catch (Exception ex)
            {
                DiagLog($"SaveImage FAILED: {ex.GetType().Name}: {ex.Message}");
                throw;
            }

            // Renderer identity for an orphan image with no gold to compare against.
            SaveMetadata(testFilename, metadata);
        }

        internal static void DiagLog(string message)
        {
            try
            {
                var logPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "local", "compare-gold-diag.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));
                File.AppendAllText(logPath, $"[{DateTime.UtcNow:HH:mm:ss}] {message}\n");
            }
            catch { }
        }

        /// <summary>
        /// Compare an image against a reference file.
        /// </summary>
        public static bool CompareImage(Image image, string testFilename) => CompareImage(image, testFilename, 2);

        /// <summary>
        /// Send the data of the test to the server.
        /// </summary>
        /// <param name="image">The image to send.</param>
        /// <param name="testFilename">The expected filename.</param>
        /// <param name="allowedDiff">Maximum per-channel difference allowed before a pixel is considered different.</param>
        public static bool CompareImage(Image image, string testFilename, int allowedDiff)
        {
            return CompareImage(image, testFilename, out _);
        }

        internal static bool CompareImage(Image image, string testFilename, AllowBucket[]? thresholds)
        {
            return CompareImage(image, testFilename, out _, thresholds);
        }

        /// <summary>
        /// Compare an image against a reference file, returning detailed statistics.
        /// </summary>
        public static bool CompareImage(Image image, string testFilename, out ComparisonStats stats)
        {
            return CompareImage(image, testFilename, out stats, null);
        }

        internal static bool CompareImage(Image image, string testFilename, out ComparisonStats stats,
            AllowBucket[]? thresholds)
        {
            stats = default;
            thresholds ??= ImageThreshold.DefaultBuckets;

            // Read gold bytes once: hash for the sidecar (content fingerprint that CompareGold
            // uses to detect "gold has changed since the sidecar was written"), then decode.
            var goldBytes = File.ReadAllBytes(testFilename);
            var goldHash = HashHex(goldBytes);
            using (var stream = new MemoryStream(goldBytes))
            using (var referenceImage = Image.Load(stream))
            {
                if (image.PixelBuffer.Count != referenceImage.PixelBuffer.Count)
                    return false;

                for (int i = 0; i < image.PixelBuffer.Count; ++i)
                {
                    var buffer = image.PixelBuffer[i];
                    var referenceBuffer = referenceImage.PixelBuffer[i];

                    if (buffer.Width != referenceBuffer.Width
                        || buffer.Height != referenceBuffer.Height
                        || buffer.RowStride != referenceBuffer.RowStride)
                        return false;

                    // Both buffers must be 8-bit RGBA in one of the four supported variants
                    // (channel order × sRGB/UNorm). The sRGB flag is metadata only — at 8-bit
                    // storage the bytes are identical — and channel order is handled via swapBGR.
                    static bool Is8888(PixelFormat f)
                        => f == PixelFormat.R8G8B8A8_UNorm || f == PixelFormat.R8G8B8A8_UNorm_SRgb
                        || f == PixelFormat.B8G8R8A8_UNorm || f == PixelFormat.B8G8R8A8_UNorm_SRgb;
                    if (!Is8888(buffer.Format) || !Is8888(referenceBuffer.Format))
                        return false;

                    var swapBGR = buffer.Format.IsBgraOrder != referenceBuffer.Format.IsBgraOrder;

                    bool checkAlpha = buffer.Format.AlphaSizeInBits > 0;

                    int maxDiff = 0;
                    long sumSquaredError = 0;
                    int totalPixels = buffer.Width * buffer.Height;
                    // Per-value diff counts: pixelDiffs[d] = number of pixels with max channel diff == d
                    var pixelDiffs = new int[256];

                    unsafe
                    {
                        for (int y = 0; y < buffer.Height; ++y)
                        {
                            var pSrc = (Color*)(buffer.DataPointer + y * buffer.RowStride);
                            var pDst = (Color*)(referenceBuffer.DataPointer + y * referenceBuffer.RowStride);
                            for (int x = 0; x < buffer.Width; ++x, ++pSrc, ++pDst)
                            {
                                var src = *pSrc;
                                if (swapBGR)
                                    (src.R, src.B) = (src.B, src.R);

                                var r = Math.Abs((int)src.R - (int)pDst->R);
                                var g = Math.Abs((int)src.G - (int)pDst->G);
                                var b = Math.Abs((int)src.B - (int)pDst->B);
                                var a = Math.Abs((int)src.A - (int)pDst->A);

                                var pixelMaxDiff = Math.Max(r, Math.Max(g, Math.Max(b, checkAlpha ? a : 0)));
                                if (pixelMaxDiff > maxDiff)
                                    maxDiff = pixelMaxDiff;

                                pixelDiffs[pixelMaxDiff]++;

                                sumSquaredError += (long)(r * r + g * g + b * b);
                                if (checkAlpha)
                                    sumSquaredError += (long)(a * a);
                            }
                        }
                    }

                    // Build legacy display histogram
                    int hist0 = pixelDiffs[0], hist1 = 0, hist2 = 0, hist3 = 0, hist4 = 0;
                    for (int d = 1; d <= 2; d++) hist1 += pixelDiffs[d];
                    for (int d = 3; d <= 5; d++) hist2 += pixelDiffs[d];
                    for (int d = 6; d <= 15; d++) hist3 += pixelDiffs[d];
                    for (int d = 16; d <= 255; d++) hist4 += pixelDiffs[d];

                    int differentPixels = totalPixels - pixelDiffs[0];
                    int channels = checkAlpha ? 4 : 3;
                    double mse = totalPixels > 0 ? (double)sumSquaredError / (totalPixels * channels) : 0;
                    double psnr = mse > 0 ? 10.0 * Math.Log10(255.0 * 255.0 / mse) : double.PositiveInfinity;

                    bool passed = ImageThreshold.Check(pixelDiffs, thresholds);

                    stats = new ComparisonStats
                    {
                        TotalPixels = totalPixels,
                        DifferentPixels = differentPixels,
                        MaxDiff = maxDiff,
                        MeanSquaredError = mse,
                        PSNR = psnr,
                        Passed = passed,
                        GoldHash = goldHash,
                    };
                    stats.DiffHistogram[0] = hist0;
                    stats.DiffHistogram[1] = hist1;
                    stats.DiffHistogram[2] = hist2;
                    stats.DiffHistogram[3] = hist3;
                    stats.DiffHistogram[4] = hist4;

                    return passed;
                }

                return true;
            }
        }
    }
}
