// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.TextureConverter.TexLibraries;

internal static class TextureResampler
{
    /// <summary>
    /// Resize a BGRA32 image using the specified rescaling filter.
    /// </summary>
    /// <param name="source">The source image pixel data in BGRA32 format.</param>
    /// <param name="sourceWidth">The width of the source image.</param>
    /// <param name="sourceHeight">The height of the source image.</param>
    /// <param name="width">The width of the resized image.</param>
    /// <param name="height">The height of the resized image.</param>
    /// <param name="filter">The rescaling filter to use.</param>
    /// <returns>The resized image pixel data in BGRA32 format.</returns>
    /// <exception cref="ArgumentException">If the image dimensions or pixel data are invalid.</exception>
    public static byte[] Resize(byte[] source, int sourceWidth, int sourceHeight, int width, int height, Filter.Rescaling filter)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (sourceWidth <= 0 || sourceHeight <= 0 || width <= 0 || height <= 0 || source.Length != checked(sourceWidth * sourceHeight * 4))
            throw new ArgumentException("Invalid image dimensions or pixel data.");

        if (sourceWidth == width && sourceHeight == height)
            return (byte[])source.Clone();

        if (filter == Filter.Rescaling.Nearest)
        {
            var nearest = new byte[checked(width * height * 4)];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int sx = int.Min(sourceWidth - 1, (int)((x + 0.5) * sourceWidth / width));
                int sy = int.Min(sourceHeight - 1, (int)((y + 0.5) * sourceHeight / height));
                Array.Copy(source, (sy * sourceWidth + sx) * 4, nearest, (y * width + x) * 4, 4);
            }
            return nearest;
        }

        var horizontal = CreateWeights(sourceWidth, width, filter);
        var vertical = width == height && sourceWidth == sourceHeight
            ? horizontal
            : CreateWeights(sourceHeight, height, filter);
        const int cachedRows = 8;
        var rows = new double[cachedRows][];
        var rowIndices = new int[cachedRows];
        Array.Fill(rowIndices, -1);
        var accumulated = new double[checked(width * 4)];
        var result = new byte[checked(width * height * 4)];
        for (int y = 0; y < height; y++)
        {
            var (start, weights) = vertical[y];
            Array.Clear(accumulated, 0, accumulated.Length);
            for (int i = 0; i < weights.Length; i++)
            {
                int sourceY = start + i;
                int slot = sourceY % cachedRows;
                var row = rows[slot] ??= new double[accumulated.Length];
                if (rowIndices[slot] != sourceY)
                {
                    Array.Clear(row, 0, row.Length);
                    for (int x = 0; x < width; x++)
                    {
                        var (horizontalStart, horizontalWeights) = horizontal[x];
                        int dest = x * 4;
                        for (int j = 0; j < horizontalWeights.Length; j++)
                        {
                            int src = (sourceY * sourceWidth + horizontalStart + j) * 4;
                            double alpha = source[src + 3] / 255.0;
                            for (int c = 0; c < 3; c++)
                                row[dest + c] += source[src + c] * alpha * horizontalWeights[j];
                            row[dest + 3] += source[src + 3] * horizontalWeights[j];
                        }
                    }
                    rowIndices[slot] = sourceY;
                }
                for (int x = 0; x < accumulated.Length; x++)
                    accumulated[x] += row[x] * weights[i];
            }
            for (int x = 0; x < width; x++)
            {
                int offset = x * 4;
                int dest = (y * width + x) * 4;
                double alpha = accumulated[offset + 3];
                if (alpha > 0)
                {
                    result[dest] = ToByte(accumulated[offset] * 255 / alpha);
                    result[dest + 1] = ToByte(accumulated[offset + 1] * 255 / alpha);
                    result[dest + 2] = ToByte(accumulated[offset + 2] * 255 / alpha);
                }
                result[dest + 3] = ToByte(alpha);
            }
        }
        return result;
    }

    private static byte ToByte(double value) => (byte)double.Clamp(double.Round(value), 0, 255);

    private static (int Start, double[] Weights)[] CreateWeights(int sourceSize, int targetSize, Filter.Rescaling filter)
    {
        double radius = filter switch
        {
            Filter.Rescaling.Box => 0.5,
            Filter.Rescaling.Bilinear => 1,
            Filter.Rescaling.Bicubic or Filter.Rescaling.BSpline or Filter.Rescaling.CatmullRom => 2,
            Filter.Rescaling.Lanczos3 => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(filter)),
        };
        double ratio = (double)sourceSize / targetSize;
        double scale = double.Max(1, ratio);
        var result = new (int, double[])[targetSize];
        for (int x = 0; x < targetSize; x++)
        {
            double center = (x + 0.5) * ratio - 0.5;
            int start = int.Max(0, (int)double.Ceiling(center - radius * scale));
            int end = int.Min(sourceSize - 1, (int)double.Floor(center + radius * scale));
            var weights = new double[end - start + 1];
            double total = 0;
            for (int i = start; i <= end; i++)
            {
                double weight = Kernel((float)((i - center) / scale), filter);
                weights[i - start] = weight;
                total += weight;
            }
            for (int i = 0; i < weights.Length; i++)
                weights[i] /= total;
            result[x] = (start, weights);
        }
        return result;
    }

    private static double Kernel(double x, Filter.Rescaling filter)
    {
        if (filter == Filter.Rescaling.Box)
            return x > -0.5 && x <= 0.5 ? 1 : 0;
        x = double.Abs(x);
        return filter switch
        {
            Filter.Rescaling.Bilinear => double.Max(0, 1 - x),
            Filter.Rescaling.Bicubic or Filter.Rescaling.CatmullRom => x < 1 ? (1.5 * x - 2.5) * x * x + 1 : x < 2 ? ((-0.5 * x + 2.5) * x - 4) * x + 2 : 0,
            Filter.Rescaling.BSpline => x < 1 ? (4 - 6 * x * x + 3 * x * x * x) / 6 : x < 2 ? double.Pow(2 - x, 3) / 6 : 0,
            Filter.Rescaling.Lanczos3 => x == 0 ? 1 : x < 3 ? 3 * double.Sin(double.Pi * x) * double.Sin(double.Pi * x / 3) / (double.Pi * double.Pi * x * x) : 0,
            _ => throw new ArgumentOutOfRangeException(nameof(filter)),
        };
    }
}
