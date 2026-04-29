// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Stride.Graphics.Regression;

/// <summary>
/// A single threshold rule from thresholds.jsonc.
/// </summary>
internal class ThresholdRule
{
    public string? Image { get; set; }
    public string? Platform { get; set; }
    public string? Api { get; set; }
    public string? Device { get; set; }
    public Dictionary<string, int>? Allow { get; set; }
}

/// <summary>
/// A parsed histogram bucket (e.g. "3-5" → Min=3, Max=5, or "16+" → Min=16, Max=int.MaxValue).
/// </summary>
internal readonly struct AllowBucket
{
    public readonly int Min;
    public readonly int Max;
    public readonly int Limit;

    public AllowBucket(int min, int max, int limit)
    {
        Min = min;
        Max = max;
        Limit = limit;
    }

    public static AllowBucket Parse(string key, int limit)
    {
        if (key.EndsWith('+'))
        {
            var min = int.Parse(key[..^1]);
            return new AllowBucket(min, int.MaxValue, limit);
        }
        var parts = key.Split('-');
        if (parts.Length == 2)
            return new AllowBucket(int.Parse(parts[0]), int.Parse(parts[1]), limit);
        // Single value like "1"
        var val = int.Parse(key);
        return new AllowBucket(val, val, limit);
    }
}

/// <summary>
/// Loads and resolves image comparison thresholds from thresholds.jsonc files.
/// </summary>
internal static class ImageThreshold
{
    /// <summary>
    /// Default threshold: any pixel with diff >= 3 fails the test.
    /// </summary>
    internal static readonly AllowBucket[] DefaultBuckets = [new AllowBucket(3, int.MaxValue, 0)];

    private static readonly Dictionary<string, ThresholdRule[]> cache = new();

    /// <summary>
    /// Strip // comments from JSONC content.
    /// </summary>
    private static string StripComments(string jsonc)
    {
        return Regex.Replace(jsonc, @"//.*?$", "", RegexOptions.Multiline);
    }

    /// <summary>
    /// Load threshold rules from a thresholds.jsonc file in the given suite directory.
    /// Results are cached per directory.
    /// </summary>
    public static ThresholdRule[] LoadRules(string suiteDir)
    {
        lock (cache)
        {
            if (cache.TryGetValue(suiteDir, out var cached))
                return cached;
        }

        var path = Path.Combine(suiteDir, "thresholds.jsonc");
        ThresholdRule[] rules;
        if (File.Exists(path))
        {
            var jsonc = File.ReadAllText(path);
            var json = StripComments(jsonc);
            rules = JsonSerializer.Deserialize<ThresholdRule[]>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? [];
        }
        else
        {
            rules = [];
        }

        lock (cache)
        {
            cache[suiteDir] = rules;
        }
        return rules;
    }

    /// <summary>
    /// Resolve the histogram buckets for a specific image, platform, API, and device.
    /// Most specific matching rule wins (most non-null filter fields matched).
    /// </summary>
    public static AllowBucket[] Resolve(ThresholdRule[] rules, string imageName,
        string? platform = null, string? api = null, string? device = null)
    {
        ThresholdRule? bestRule = null;
        int bestScore = -1;

        foreach (var rule in rules)
        {
            // Check filters — null/missing means "match any"
            if (rule.Image != null && rule.Image != "default" &&
                !string.Equals(rule.Image, imageName, StringComparison.OrdinalIgnoreCase))
                continue;
            if (rule.Platform != null && !string.Equals(rule.Platform, platform, StringComparison.OrdinalIgnoreCase))
                continue;
            if (rule.Api != null && !string.Equals(rule.Api, api, StringComparison.OrdinalIgnoreCase))
                continue;
            if (rule.Device != null && !string.Equals(rule.Device, device, StringComparison.OrdinalIgnoreCase))
                continue;

            // Score = number of non-null filter fields that matched
            int score = 0;
            if (rule.Image != null && rule.Image != "default") score += 1;
            if (rule.Platform != null) score += 2;
            if (rule.Api != null) score += 4;
            if (rule.Device != null) score += 8;

            // "default" rule gets score 0 (lowest priority)
            if (rule.Image == "default") score = 0;

            if (score > bestScore || (score == bestScore && rule.Allow != null))
            {
                bestScore = score;
                bestRule = rule;
            }
        }

        if (bestRule?.Allow == null)
            return DefaultBuckets;

        return bestRule.Allow
            .Select(kv => AllowBucket.Parse(kv.Key, kv.Value))
            .ToArray();
    }

    /// <summary>
    /// Check if the per-pixel diff histogram passes the given threshold buckets.
    /// </summary>
    /// <param name="pixelDiffs">For each diff value d (0-255), the count of pixels with that max channel diff.</param>
    /// <param name="buckets">The threshold buckets to check against.</param>
    /// <returns>True if all buckets are within limits.</returns>
    public static bool Check(int[] pixelDiffs, AllowBucket[] buckets)
    {
        foreach (var bucket in buckets)
        {
            int count = 0;
            int min = Math.Max(bucket.Min, 0);
            int max = Math.Min(bucket.Max, pixelDiffs.Length - 1);
            for (int d = min; d <= max; d++)
                count += pixelDiffs[d];
            if (count > bucket.Limit)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Format the threshold buckets and actual counts for display in test failure messages.
    /// </summary>
    public static string FormatResult(int[] pixelDiffs, AllowBucket[] buckets)
    {
        var parts = new List<string>();
        foreach (var bucket in buckets)
        {
            int count = 0;
            int min = Math.Max(bucket.Min, 0);
            int max = Math.Min(bucket.Max, pixelDiffs.Length - 1);
            for (int d = min; d <= max; d++)
                count += pixelDiffs[d];
            var rangeStr = bucket.Max == int.MaxValue ? $"{bucket.Min}+" : bucket.Min == bucket.Max ? $"{bucket.Min}" : $"{bucket.Min}-{bucket.Max}";
            var status = count > bucket.Limit ? "FAIL" : "ok";
            parts.Add($"[{rangeStr}]: {count}/{bucket.Limit} ({status})");
        }
        return string.Join(", ", parts);
    }
}
