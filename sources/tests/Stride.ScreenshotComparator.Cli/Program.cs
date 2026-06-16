// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Stride.Tests.ScreenshotComparator;

// Vision gate runner. A keyless run (e.g. a fork PR, where ANTHROPIC_API_KEY isn't exposed) captures
// screenshots and marks borderline frames "deferred" in <sample>/vision-deferred.json. This tool — run
// from a trusted base/master checkout, WITH the key — re-judges those frames and fails closed if any
// deferred frame can't be conclusively resolved. It reads only the PR's image artifacts (data), so no
// PR-built code runs with the key.
//
// Usage: --new <captures-dir> --baseline <fixtures-dir>

string? newDir = null, baselineDir = null;
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--new") newDir = args[i + 1];
    else if (args[i] == "--baseline") baselineDir = args[i + 1];
}
if (newDir is null || baselineDir is null)
{
    Console.Error.WriteLine("usage: --new <captures-dir> --baseline <fixtures-dir>");
    return 2;
}

// Collect the frames the keyless run deferred. These MUST be resolved to a passing status here.
var expectedDeferred = new HashSet<(string Sample, string Frame)>();
if (Directory.Exists(newDir))
{
    foreach (var manifest in Directory.EnumerateFiles(newDir, "vision-deferred.json", SearchOption.AllDirectories))
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(manifest));
            var sample = doc.RootElement.GetProperty("sample").GetString()!;
            foreach (var f in doc.RootElement.GetProperty("frames").EnumerateArray())
                expectedDeferred.Add((sample, f.GetProperty("frame").GetString()!));
        }
        catch (Exception ex)
        {
            // Fail closed: a manifest we can't read means we can't prove the deferred frames are fine.
            Console.Error.WriteLine($"FAIL: unreadable deferred manifest '{manifest}': {ex.Message}");
            return 1;
        }
    }
}

if (expectedDeferred.Count == 0)
{
    // Nothing was deferred (e.g. a keyed run that judged everything inline, or a clean keyless run).
    // The capturing run was authoritative; the gate has nothing to add.
    Console.WriteLine("[gate] no deferred frames — nothing to resolve.");
    return 0;
}

Console.WriteLine($"[gate] resolving {expectedDeferred.Count} deferred frame(s) with the vision tiebreak...");

List<ComparisonResult> results;
try
{
    // Key present here → ClaudeVisionFallback runs inline; deferWhenVisionUnavailable left false so a
    // missing key (shouldn't happen in the gate) fails closed to "drift" rather than re-deferring.
    results = ScreenshotComparator.Compare(newDir, baselineDir);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL: comparator threw: {ex.Message}");
    return 1;
}

foreach (var r in results)
    Console.WriteLine($"[gate] {r.Status,-12} {r.Sample}/{r.Frame} {(r.Lpips.HasValue ? $"lpips={r.Lpips.Value:F4}" : "")}{(r.Detail is null ? "" : "  " + r.Detail)}");

var exit = 0;

var hardFailures = results.Where(r => r.Status is "drift" or "error" or "new").ToList();
if (hardFailures.Count > 0)
{
    Console.Error.WriteLine($"FAIL: {hardFailures.Count} frame(s) failed: {string.Join(", ", hardFailures.Select(f => $"{f.Sample}/{f.Frame}({f.Status})"))}");
    exit = 1;
}

// Fail closed: every deferred frame must be present and resolved to a pass.
var statusByKey = results
    .GroupBy(r => (r.Sample, r.Frame))
    .ToDictionary(g => g.Key, g => g.First().Status);
foreach (var (sample, frame) in expectedDeferred)
{
    if (!statusByKey.TryGetValue((sample, frame), out var status) || status is not ("ok" or "ok-via-claude"))
    {
        Console.Error.WriteLine($"FAIL (fail-closed): deferred frame {sample}/{frame} not resolved (status: {status ?? "absent"}).");
        exit = 1;
    }
}

if (exit == 0)
    Console.WriteLine($"[gate] OK — {expectedDeferred.Count} deferred frame(s) resolved.");
return exit;
