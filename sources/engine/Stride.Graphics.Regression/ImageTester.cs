// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Stride.Core.Mathematics;

namespace Stride.Graphics.Regression
{
    public static class ImageTester
    {
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
            public string? ThresholdResult;

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
                var threshold = ThresholdResult != null ? $", thresholds: {ThresholdResult}" : "";
                if (Passed)
                    return $"PASS (max diff={MaxDiff}, PSNR={PSNR:F1}dB, {hist}{threshold})";
                return $"FAIL (max diff={MaxDiff}, PSNR={PSNR:F1}dB, {hist}{threshold})";
            }
        }

        public static void SaveImage(Image image, string testFilename)
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

            using (var stream = File.OpenRead(testFilename))
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

                    var swapBGR = buffer.Format.IsBgraOrder != referenceBuffer.Format.IsBgraOrder;
                    if ((buffer.Format != PixelFormat.R8G8B8A8_UNorm_SRgb && buffer.Format != PixelFormat.B8G8R8A8_UNorm_SRgb)
                        || referenceBuffer.Format != PixelFormat.B8G8R8A8_UNorm)
                    {
                        return false;
                    }

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
                        ThresholdResult = ImageThreshold.FormatResult(pixelDiffs, thresholds),
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
