// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Stride.Graphics.Regression;

/// <summary>
/// A gold bucket (<c>tests/&lt;Suite&gt;/&lt;Platform.API&gt;/&lt;Device&gt;/</c>) holds one gold per image,
/// <c>&lt;name&gt;.png</c>, and optional variants <c>&lt;name&gt;.variant2.png</c>, <c>&lt;name&gt;.variant3.png</c>…
/// A render passes when it matches any of them.
/// </summary>
/// <remarks>
/// Variants are for the CPU rasterizers (WARP, Lavapipe, SwiftShader): their output depends on the CPU's
/// code paths, and CI runners come with different CPUs.
/// </remarks>
internal static class GoldVariant
{
    private static readonly Regex VariantSuffix = new(@"\.variant(\d+)\.png$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>The image name without its variant suffix: <c>X.f5.variant2.png</c> → <c>X.f5.png</c>.</summary>
    public static string BaseName(string fileName)
    {
        var match = VariantSuffix.Match(fileName);
        return match.Success ? fileName[..match.Index] + ".png" : fileName;
    }

    /// <summary>The variant number: 1 for <c>&lt;name&gt;.png</c>, N for <c>&lt;name&gt;.variantN.png</c>.</summary>
    public static int Index(string fileName)
    {
        var match = VariantSuffix.Match(fileName);
        return match.Success ? int.Parse(match.Groups[1].Value) : 1;
    }

    /// <summary>The file name of variant <paramref name="index"/> of <paramref name="baseName"/> (<c>X.png</c>).</summary>
    public static string FileName(string baseName, int index) =>
        index == 1 ? baseName : Path.ChangeExtension(baseName, null) + $".variant{index}.png";

    /// <summary>Regex fragment that matches the variant suffix, to put before <c>\.png$</c>.</summary>
    public const string SuffixPattern = @"(\.variant\d+)?";

    /// <summary>The golds of <paramref name="baseName"/> in a bucket directory, <c>&lt;name&gt;.png</c> first.</summary>
    public static List<string> InBucket(string bucketDir, string baseName)
    {
        if (!Directory.Exists(bucketDir))
            return [];
        var stem = Path.ChangeExtension(baseName, null);
        return Directory.EnumerateFiles(bucketDir, stem + ".*png")
            .Where(f => string.Equals(BaseName(Path.GetFileName(f)), baseName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => Index(Path.GetFileName(f)))
            .ToList();
    }

    /// <summary>Devices that rasterize on the CPU, the only ones that get variants.</summary>
    public static bool IsCpuRasterizer(string device) =>
        device.Equals("WARP", StringComparison.OrdinalIgnoreCase)
        || device.StartsWith("Lavapipe", StringComparison.OrdinalIgnoreCase)
        || device.StartsWith("SwiftShader", StringComparison.OrdinalIgnoreCase);
}
