// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Tests.ScreenshotComparator;

internal static class BicubicResize
{
    /// <summary>
    /// RGB 24 → NCHW float32 in [-1, 1], matching the preprocessing the lpips Python lib does.
    /// </summary>
    /// <param name="pixels">The source image pixel data in RGB24 format.</param>
    /// <param name="width">The width of the source image.</param>
    /// <param name="height">The height of the source image.</param>
    /// <param name="targetWidth">The width of the resized image.</param>
    /// <param name="targetHeight">The height of the resized image.</param>
    /// <returns>The resized image pixel data in NCHW float32 format.</returns>
    public static float[] Rgb24ToTensorData(byte[] pixels, int width, int height, int targetWidth, int targetHeight)
    {
        var resized = new float[targetWidth * targetHeight * 3];
        if (width == targetWidth && height == targetHeight)
        {
            for (int i = 0; i < width * height; i++)
            for (int c = 0; c < 3; c++)
                resized[c * width * height + i] = pixels[3 * i + c] / 127.5f - 1f;
            return resized;
        }

        var horizontal = CreateWeights(width, targetWidth);
        var vertical = width == height && targetWidth == targetHeight
            ? horizontal
            : CreateWeights(height, targetHeight);
        var intermediate = new float[targetWidth * height * 3];
        for (int x = 0; x < targetWidth; x++)
        {
            var (start, weights) = horizontal[x];
            for (int y = 0; y < height; y++)
            for (int c = 0; c < 3; c++)
            {
                float value = 0;
                for (int i = 0; i < weights.Length; i++)
                    value += pixels[(y * width + start + i) * 3 + c] / 255f * weights[i];
                intermediate[(y * targetWidth + x) * 3 + c] = value;
            }
        }

        for (int y = 0; y < targetHeight; y++)
        {
            var (start, weights) = vertical[y];
            for (int x = 0; x < targetWidth; x++)
            for (int c = 0; c < 3; c++)
            {
                float value = 0;
                for (int i = 0; i < weights.Length; i++)
                    value += intermediate[((start + i) * targetWidth + x) * 3 + c] * weights[i];
                resized[c * targetWidth * targetHeight + y * targetWidth + x] = float.Clamp(value, 0f, 1f) * 2f - 1f;
            }
        }

        return resized;
    }

    private static (int Start, float[] Weights)[] CreateWeights(int sourceSize, int targetSize)
    {
        var result = new (int, float[])[targetSize];
        double ratio = (double)sourceSize / targetSize;
        double scale = double.Max(1, ratio);
        for (int i = 0; i < targetSize; i++)
        {
            double center = (i + 0.5) * ratio - 0.5;
            int start = int.Max(0, (int)double.Ceiling(center - 2 * scale));
            int end = int.Min(sourceSize - 1, (int)double.Floor(center + 2 * scale));
            var values = new double[end - start + 1];
            double sum = 0;
            for (int j = start; j <= end; j++)
            {
                double distance = double.Abs((j - center) / scale);
                double weight = distance <= 1 ? ((1.5d * distance - 2.5d) * distance * distance) + 1
                    : distance < 2 ? (((-0.5d * distance + 2.5d) * distance - 4) * distance) + 2 : 0;
                values[j - start] = weight;
                sum += weight;
            }
            var weights = new float[values.Length];
            for (int j = 0; j < weights.Length; j++)
                weights[j] = (float)(values[j] / sum);
            result[i] = (start, weights);
        }
        return result;
    }
}
