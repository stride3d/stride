// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics.Regression;
using StbImageSharp;

// Tolerance image comparison for the headless promote/dedup flow. Mirrors the runtime's
// ImageTester: per-pixel max channel difference (R/G/B/A) → 256-bucket histogram → ImageThreshold
// (thresholds.jsonc, linked from the engine) decides pass/fail. Same algorithm + rules the tests
// assert and the UI shows, so a CI promote reaches the same verdict as a local CompareGold review.
//
// PNG decode uses StbImageSharp (managed, cross-platform) rather than the engine's native FreeImage
// path — decode is lossless so pixel values are identical regardless of decoder; this just avoids a
// dependency on Stride.Graphics.
internal static class ImageDiff
{
    // True when <render> is within tolerance of <gold> for the given thresholds. `comparable` is
    // false when either PNG can't be decoded — callers treat that conservatively (promote / keep).
    public static bool Matches(string renderPath, string goldPath, AllowBucket[] thresholds, out bool comparable)
    {
        comparable = true;
        ImageResult a, b;
        try
        {
            a = ImageResult.FromMemory(File.ReadAllBytes(renderPath), ColorComponents.RedGreenBlueAlpha);
            b = ImageResult.FromMemory(File.ReadAllBytes(goldPath), ColorComponents.RedGreenBlueAlpha);
        }
        catch
        {
            comparable = false;
            return false;
        }

        if (a.Width != b.Width || a.Height != b.Height || a.Data.Length != b.Data.Length)
            return false;

        var da = a.Data;
        var db = b.Data;
        var pixelDiffs = new int[256];
        for (int i = 0; i < da.Length; i += 4)
        {
            int dr = Math.Abs(da[i] - db[i]);
            int dg = Math.Abs(da[i + 1] - db[i + 1]);
            int dbl = Math.Abs(da[i + 2] - db[i + 2]);
            int dal = Math.Abs(da[i + 3] - db[i + 3]);
            pixelDiffs[Math.Max(Math.Max(dr, dg), Math.Max(dbl, dal))]++;
        }
        return ImageThreshold.Check(pixelDiffs, thresholds);
    }
}
