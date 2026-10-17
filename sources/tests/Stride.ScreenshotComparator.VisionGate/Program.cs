// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Stride.Tests.ScreenshotComparator;

// Vision gate runner. A keyless run (e.g. a fork PR, where the API key isn't exposed) captures
// screenshots and marks borderline frames "deferred" in <sample>/vision-deferred.json. This tool — run
// from a trusted base/master checkout, WITH the key — re-judges those frames and fails closed if any
// deferred frame can't be conclusively resolved. It reads only the PR's image artifacts (data), so no
// PR-built code runs with the key.
//
// Usage: --new <captures-dir> --baseline <fixtures-dir> [--prompt gameplay|editor]
//        [--summary <markdown-file>] [--baseline-url <url-of-fixtures-dir>] [--failures <file>]
// --summary appends a table of the frames the gate judged (failed first, with Claude's reason) for the run page;
// --failures appends one "<sample>/<frame>" line per failed frame, for the status the workflow posts.

string? newDir = null, baselineDir = null, promptKind = null, summaryFile = null, baselineUrl = null, failuresFile = null;
for (var i = 0; i < args.Length - 1; i++)
{
    if (args[i] == "--new") newDir = args[i + 1];
    else if (args[i] == "--baseline") baselineDir = args[i + 1];
    else if (args[i] == "--prompt") promptKind = args[i + 1];
    else if (args[i] == "--summary") summaryFile = args[i + 1];
    else if (args[i] == "--baseline-url") baselineUrl = args[i + 1];
    else if (args[i] == "--failures") failuresFile = args[i + 1];
}
if (newDir is null || baselineDir is null)
{
    Console.Error.WriteLine("usage: --new <captures-dir> --baseline <fixtures-dir> [--prompt gameplay|editor] [--summary <file>] [--baseline-url <url>] [--failures <file>]");
    return 2;
}

// Match the vision prompt to the capture domain so the re-judge applies the same guidance the
// capturing suite used. Null → Compare defaults to the gameplay prompt.
ComparisonPrompt? defaultPrompt = string.Equals(promptKind, "editor", StringComparison.OrdinalIgnoreCase)
    ? EditorComparisonPrompt.Default
    : null;

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
    results = ScreenshotComparator.Compare(newDir, baselineDir, defaultPrompt: defaultPrompt);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"FAIL: comparator threw: {ex.Message}");
    return 1;
}

foreach (var r in results)
    Console.WriteLine($"[gate] {r.Status,-12} {r.Sample}/{r.Frame} {(r.Lpips.HasValue ? $"lpips={r.Lpips.Value:F4}" : "")}{(r.Detail is null ? "" : "  " + r.Detail)}");

var exit = 0;
var failed = new List<(string Sample, string Frame)>();

var hardFailures = results.Where(r => r.Status is "drift" or "error" or "new").ToList();
if (hardFailures.Count > 0)
{
    Console.Error.WriteLine($"FAIL: {hardFailures.Count} frame(s) failed: {string.Join(", ", hardFailures.Select(f => $"{f.Sample}/{f.Frame}({f.Status})"))}");
    failed.AddRange(hardFailures.Select(f => (f.Sample, f.Frame)));
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
        if (!failed.Contains((sample, frame)))
            failed.Add((sample, frame));
        exit = 1;
    }
}

if (summaryFile is not null)
    File.AppendAllText(summaryFile, Summary());
if (failuresFile is not null && failed.Count > 0)
    File.AppendAllLines(failuresFile, failed.Select(f => $"{f.Sample}/{f.Frame}"));

if (exit == 0)
    Console.WriteLine($"[gate] OK — {expectedDeferred.Count} deferred frame(s) resolved.");
return exit;

// The frames the gate had to judge, failed first: where the capture is (inside the screenshot artifact the gate
// downloaded, <artifact>/<captures-dir>/...) and the baselines it was compared with.
string Summary()
{
    var captures = Path.GetFullPath(newDir).TrimEnd('/', '\\');
    var artifact = Path.GetFileName(Path.GetDirectoryName(captures));
    var capturesDir = Path.GetFileName(captures);

    var rows = results.Where(r => r.Status != "ok").Select(r => (r.Sample, r.Frame, r.Status, r.Lpips, Detail: r.VisionReason ?? r.Detail)).ToList();
    rows.AddRange(failed.Where(f => !statusByKey.ContainsKey(f)).Select(f => (f.Sample, f.Frame, "absent", (float?)null, (string?)"deferred, but not captured again")));

    var text = new System.Text.StringBuilder();
    text.AppendLine($"### {(exit == 0 ? "✅" : "❌")} `{Cell(artifact)}`: {failed.Count} failed, {expectedDeferred.Count} deferred");
    text.AppendLine();
    text.AppendLine("| Frame | Result | LPIPS | Reason | Baseline / capture (in the artifact) |");
    text.AppendLine("|---|---|---|---|---|");
    var used = new List<string>();
    foreach (var row in rows.OrderBy(r => failed.Contains((r.Sample, r.Frame)) ? 0 : 1))
    {
        var baseline = baselineUrl is null ? "" : BaselineLink(row.Sample, row.Frame);
        var (icon, _) = Legend(row.Status);
        if (!used.Contains(row.Status))
            used.Add(row.Status);
        text.AppendLine($"| `{Cell(row.Sample)}/{Cell(row.Frame)}` | {icon} | {row.Lpips?.ToString("F4") ?? ""} | {Cell(row.Detail ?? "")} | {(baseline == "" ? "" : baseline + "<br>")}`{Cell($"{capturesDir}/{row.Sample}/screenshots/{row.Frame}.png")}` |");
    }
    text.AppendLine();
    // The statuses of this table, so the short icons stay readable
    text.AppendLine(string.Join(" · ", used.Select(status => $"{Legend(status).Icon} `{Cell(status)}`: {Legend(status).Meaning}")));
    text.AppendLine();
    return text.ToString();
}

// A status the comparator gave as an icon for the Result column, and what it means
static (string Icon, string Meaning) Legend(string status) => status switch
{
    "ok-via-claude" => ("✅", "LPIPS differed, Claude accepted it"),
    "drift" => ("❌", "LPIPS differed, Claude rejected it (or no Claude for this frame)"),
    "new" => ("🆕", "no baseline yet"),
    "error" => ("💥", "the comparison threw"),
    "missing" => ("❓", "a baseline with no capture"),
    "absent" => ("❓", "deferred, but not captured again"),
    _ => ("⚠️", status),
};

// The baseline image (it opens in the browser), else the sample's folder; variants (<frame>.<variant>.png) are counted
string BaselineLink(string sample, string frame)
{
    var sampleUrl = $"{baselineUrl!.TrimEnd('/')}/{Uri.EscapeDataString(sample)}";
    var sampleDir = Path.Combine(baselineDir, sample);
    if (!File.Exists(Path.Combine(sampleDir, frame + ".png")))
        return $"[baselines]({sampleUrl})";

    var variants = Directory.EnumerateFiles(sampleDir, frame + ".*.png").Count();
    return $"[baseline]({sampleUrl}/{Uri.EscapeDataString(frame)}.png){(variants > 0 ? $" (+{variants} [variants]({sampleUrl}))" : "")}";
}

// The names come from the PR's artifacts and the reason from the model: plain text in one table cell
static string Cell(string? value)
    => (value ?? "").Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|").Replace("`", "'").Replace("<", "&lt;").Replace(">", "&gt;");
