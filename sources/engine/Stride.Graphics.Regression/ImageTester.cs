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
                var pct = 100.0 * DifferentPixels / Math.Max(TotalPixels, 1);
                if (Passed)
                    return $"PASS (max diff={MaxDiff}, PSNR={PSNR:F1}dB, {hist})";
                return $"FAIL ({DifferentPixels}/{TotalPixels} exceed threshold ({pct:F2}%), max diff={MaxDiff}, PSNR={PSNR:F1}dB, {hist})";
            }
        }

        public static void SaveImage(Image image, string testFilename)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(testFilename));
            using (var stream = File.Open(testFilename, FileMode.Create))
            {
                image.Save(stream, ImageFileType.Png);
            }
        }

        /// <summary>
        /// Compare an image against a reference file.
        /// </summary>
        public static bool CompareImage(Image image, string testFilename)
        {
            return CompareImage(image, testFilename, out _);
        }

        /// <summary>
        /// Compare an image against a reference file, returning detailed statistics.
        /// </summary>
        public static bool CompareImage(Image image, string testFilename, out ComparisonStats stats)
        {
            stats = default;

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

                    int allowedDiff = 2;
                    int differentPixels = 0;
                    int maxDiff = 0;
                    long sumSquaredError = 0;
                    int totalPixels = buffer.Width * buffer.Height;
                    // Histogram buckets: [0]=0, [1]=1-2, [2]=3-5, [3]=6-15, [4]=16+
                    int hist0 = 0, hist1 = 0, hist2 = 0, hist3 = 0, hist4 = 0;

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
                                {
                                    (src.R, src.B) = (src.B, src.R);
                                }

                                var r = Math.Abs((int)src.R - (int)pDst->R);
                                var g = Math.Abs((int)src.G - (int)pDst->G);
                                var b = Math.Abs((int)src.B - (int)pDst->B);
                                var a = Math.Abs((int)src.A - (int)pDst->A);

                                var pixelMaxDiff = Math.Max(r, Math.Max(g, Math.Max(b, checkAlpha ? a : 0)));
                                if (pixelMaxDiff > maxDiff)
                                    maxDiff = pixelMaxDiff;

                                if (pixelMaxDiff == 0) hist0++;
                                else if (pixelMaxDiff <= 2) hist1++;
                                else if (pixelMaxDiff <= 5) hist2++;
                                else if (pixelMaxDiff <= 15) hist3++;
                                else hist4++;

                                sumSquaredError += (long)(r * r + g * g + b * b);
                                if (checkAlpha)
                                    sumSquaredError += (long)(a * a);

                                if (r > allowedDiff || g > allowedDiff || b > allowedDiff || (a > allowedDiff && checkAlpha))
                                    differentPixels++;
                            }
                        }
                    }

                    int channels = checkAlpha ? 4 : 3;
                    double mse = totalPixels > 0 ? (double)sumSquaredError / (totalPixels * channels) : 0;

                    double psnr = mse > 0 ? 10.0 * Math.Log10(255.0 * 255.0 / mse) : double.PositiveInfinity;

                    stats = new ComparisonStats
                    {
                        TotalPixels = totalPixels,
                        DifferentPixels = differentPixels,
                        MaxDiff = maxDiff,
                        MeanSquaredError = mse,
                        PSNR = psnr,
                        Passed = differentPixels == 0,
                    };
                    stats.DiffHistogram[0] = hist0;
                    stats.DiffHistogram[1] = hist1;
                    stats.DiffHistogram[2] = hist2;
                    stats.DiffHistogram[3] = hist3;
                    stats.DiffHistogram[4] = hist4;

                    if (differentPixels > 0)
                        return false;
                }

                return true;
            }
        }
    }
}
