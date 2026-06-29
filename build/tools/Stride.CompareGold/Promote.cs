// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using System.Text.RegularExpressions;
using Stride.Graphics.Regression;

// Headless gold tooling for the test-gold-gen CI flow (no web server). Subcommands: promote, dedup.
// Run `Stride.CompareGold promote --help` for usage. Both compare with the same per-pixel max-channel
// diff + thresholds.jsonc rules the tests assert and the UI shows (see ImageDiff), so a CI promote
// reaches the same verdict as a local CompareGold review — it does NOT rely on byte-identity.
internal static class HeadlessPromote
{
    private const string Usage = """
        Stride.CompareGold gold tooling (headless):

          promote [--source <dir|run>] [--tests <dir>] [--dry-run] [--out <json>]
              Promote generated images to gold. <source> defaults to tests/local (the renders from
              your last local run, same as the UI's "Local" source). It can be a gold-images tree
              (<Suite>/<Platform.API>/<Device>/<name>.png), or a CI run to download via gh — given as
              a run id (123), "owner:123" / "owner/repo:123", or a full Actions run URL. In priority order:
              if its bucket's gold already matches (within thresholds) it's left alone; if a
              higher-priority bucket already matches, the runtime fallback covers it (no new gold);
              otherwise it's written as gold.

          dedup [--source <dir|run>] [--tests <dir>] [--dry-run] [--out <json>]
              Remove redundant existing gold: per image keep the highest-priority bucket and delete
              any lower-priority bucket that matches it (within that bucket's thresholds — the
              fallback covers it). Repo-wide unless --source (dir or CI run, as for promote) scopes
              it to those suites.

          Priority: Windows.Direct3D12 > Windows.Vulkan > Linux.Vulkan > macOS.Vulkan > Android.Vulkan > iOS.Vulkan > Windows.Direct3D11.
          --tests defaults to the tests/ dir of the enclosing Stride checkout.
        """;

    // Promotion priority: a render matching one already accepted from an earlier entry is skipped,
    // so the first API/platform to render an image owns the canonical gold. An unknown/future
    // Platform.API not listed here ranks last (see Rank) and is ordered deterministically by name,
    // so it still promotes/dedups correctly — it just never outranks a known API.
    private static readonly string[] PlatformApiPriority =
    [
        "Windows.Direct3D12", "Windows.Vulkan",
        "Linux.Vulkan", "macOS.Vulkan", "Android.Vulkan", "iOS.Vulkan",
        "Windows.Direct3D11",   // slated for removal — kept lowest so nothing falls back to it
    ];

    private sealed record Render(string Suite, string PlatformApi, string Device, string Name, string SrcPath);

    public static int Run(string[] args, Func<string, string?> findStrideRoot)
    {
        var mode = args.Length > 0 ? args[0] : "";       // "promote" | "dedup"
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(Usage);
            return 0;
        }
        string? source = GetArg(args, "--source");
        string? testsArg = GetArg(args, "--tests");
        bool dryRun = args.Contains("--dry-run");
        string? outPath = GetArg(args, "--out");

        var testsDir = testsArg
            ?? (findStrideRoot(AppContext.BaseDirectory) is { } r1 ? Path.Combine(r1, "tests")
                : findStrideRoot(Directory.GetCurrentDirectory()) is { } r2 ? Path.Combine(r2, "tests")
                : null);
        if (testsDir is null)
        {
            Console.Error.WriteLine($"{mode}: could not locate the tests/ directory; pass --tests <dir>");
            return 2;
        }

        // --source as a CI run reference (a real directory always wins): a bare run id, "repo:id"
        // (repo = owner → owner/stride, or owner/name), or a full Actions run URL. Downloads that
        // run's gold-images artifact via gh and uses it as the source tree.
        if (!string.IsNullOrEmpty(source) && !Directory.Exists(source) && TryParseCiSource(source!, out var runId, out var ciRepo))
        {
            var resolved = DownloadCiArtifact(runId, ciRepo, out var dlErr);
            if (resolved is null)
            {
                Console.Error.WriteLine($"{mode}: {dlErr}");
                return 2;
            }
            source = resolved;
        }

        // promote defaults --source to tests/local (the just-generated renders) — same as the UI's
        // "Local" source. dedup with no --source means repo-wide.
        if (mode == "promote" && string.IsNullOrEmpty(source))
            source = Path.Combine(testsDir, "local");

        bool hasSource = !string.IsNullOrEmpty(source);
        if (hasSource && !Directory.Exists(source))
        {
            Console.Error.WriteLine(mode == "promote"
                ? $"promote: no images to promote ('{source}' not found) — run tests first, or pass --source <dir>"
                : $"{mode}: --source '{source}' does not exist");
            return 2;
        }

        var summary = new Summary();

        if (mode == "promote")
            RunPromote(testsDir, source!, dryRun, summary);

        if (mode == "dedup")
        {
            // Scope to the suites present in --source if given, else every suite under tests/.
            var suites = hasSource
                ? Directory.GetDirectories(source!).Select(d => Path.GetFileName(d)!)
                : EnumerateSuites(testsDir);
            DedupExistingGold(testsDir, suites, dryRun, summary);
        }

        Print(summary, dryRun);
        WriteSummary(outPath, summary);
        return 0;
    }

    private static void RunPromote(string testsDir, string source, bool dryRun, Summary summary)
    {
        // Collect generated renders: <source>/<Suite>/<Platform.API>/<Device>/<name>.png
        var renders = new List<Render>();
        foreach (var png in Directory.EnumerateFiles(source, "*.png", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, png).Replace('\\', '/').Split('/');
            if (rel.Length != 4)
            {
                Console.WriteLine($"  skip (unexpected depth {rel.Length}): {Path.GetRelativePath(source, png)}");
                continue;
            }
            renders.Add(new Render(rel[0], rel[1], rel[2], rel[3], png));
        }

        // Highest-priority platform/API first so it owns the canonical gold; ties ordered
        // deterministically so a dry-run and the real run agree.
        renders.Sort((a, b) =>
        {
            int ra = Rank(a.PlatformApi), rb = Rank(b.PlatformApi);
            if (ra != rb) return ra.CompareTo(rb);
            return string.CompareOrdinal($"{a.Suite}/{a.PlatformApi}/{a.Device}/{a.Name}",
                                         $"{b.Suite}/{b.PlatformApi}/{b.Device}/{b.Name}");
        });

        // golds[suite|name][<Platform.API>/<Device>] = (priority rank, pixel source) for the gold that
        // currently covers that bucket — seeded from existing gold, updated as we promote. A render is
        // "covered" (skip) ONLY by a strictly higher-priority bucket: those are processed earlier and so
        // are final for the rest of the pass, whereas a lower/equal bucket could still be re-promoted to
        // something else and invalidate the skip. Redundancy against a lower bucket is the dedup pass's
        // job, not promote's. Source paths (not dest) are stored so a --dry-run matches the real run.
        var golds = new Dictionary<string, Dictionary<string, (int rank, string src)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var suite in renders.Select(r => r.Suite).Distinct())
            SeedExistingGold(testsDir, suite, golds);

        foreach (var r in renders)
        {
            var key = $"{r.Suite}|{r.Name}";
            var buckets = golds.TryGetValue(key, out var b) ? b : golds[key] = new(StringComparer.OrdinalIgnoreCase);
            var bucketKey = $"{r.PlatformApi}/{r.Device}";
            var rank = Rank(r.PlatformApi);
            var thresholds = ResolveThresholds(testsDir, r.Suite, r.PlatformApi, r.Device, r.Name);
            var destDir = Path.Combine(testsDir, r.Suite, r.PlatformApi, r.Device);
            var destFile = Path.Combine(destDir, r.Name);
            var label = $"{r.Suite}/{r.PlatformApi}/{r.Device}/{r.Name}";

            if (File.Exists(destFile))
            {
                // Exact bucket has gold: keep it if it still matches, else update it.
                if (ImageDiff.Matches(r.SrcPath, destFile, thresholds, out _))
                {
                    summary.Unchanged.Add(label);
                }
                else
                {
                    Promote(r, destDir, destFile, dryRun);
                    buckets[bucketKey] = (rank, r.SrcPath);
                    summary.Changed.Add(label);
                }
            }
            else if (buckets.Values.Any(c => c.rank < rank && ImageDiff.Matches(r.SrcPath, c.src, thresholds, out var comp) && comp))
            {
                // A strictly higher-priority bucket already matches — the runtime fallback covers it.
                summary.CoveredByFallback.Add(label);
            }
            else
            {
                Promote(r, destDir, destFile, dryRun);
                buckets[bucketKey] = (rank, r.SrcPath);
                summary.Promoted.Add(label);
            }
        }
    }

    // Per image, keep the highest-priority bucket and remove any lower-priority bucket that matches a
    // kept one (within the removed bucket's thresholds — the runtime fallback then covers it).
    private static void DedupExistingGold(string testsDir, IEnumerable<string> suites, bool dryRun, Summary summary)
    {
        foreach (var suite in suites.Distinct())
        {
            var byName = new Dictionary<string, List<(string platApi, string device, string path)>>(StringComparer.OrdinalIgnoreCase);
            CollectGold(testsDir, suite, byName);
            foreach (var (name, golds) in byName)
            {
                var kept = new List<(string platApi, string device, string path)>();
                foreach (var g in golds.OrderBy(g => Rank(g.platApi)).ThenBy(g => g.platApi, StringComparer.Ordinal).ThenBy(g => g.device, StringComparer.Ordinal))
                {
                    var thresholds = ResolveThresholds(testsDir, suite, g.platApi, g.device, name);
                    if (kept.Any(k => ImageDiff.Matches(g.path, k.path, thresholds, out var comp) && comp))
                    {
                        if (!dryRun)
                        {
                            File.Delete(g.path);
                            var meta = Path.ChangeExtension(g.path, ".metadata.json");
                            if (File.Exists(meta)) File.Delete(meta);
                        }
                        summary.RemovedRedundant.Add($"{suite}/{g.platApi}/{g.device}/{name}");
                    }
                    else kept.Add(g);
                }
            }
        }
    }

    private static AllowBucket[] ResolveThresholds(string testsDir, string suite, string platformApi, string device, string name)
    {
        var rules = ImageThreshold.LoadRules(Path.Combine(testsDir, suite));
        int dot = platformApi.IndexOf('.');
        var platform = dot >= 0 ? platformApi[..dot] : platformApi;
        var api = dot >= 0 ? platformApi[(dot + 1)..] : null;
        return ImageThreshold.Resolve(rules, name, platform, api, device);
    }

    private static void Promote(Render r, string destDir, string destFile, bool dryRun)
    {
        if (dryRun) return;
        Directory.CreateDirectory(destDir);
        File.Copy(r.SrcPath, destFile, overwrite: true);
        var srcMeta = Path.ChangeExtension(r.SrcPath, ".metadata.json");
        if (File.Exists(srcMeta))
            File.Copy(srcMeta, Path.ChangeExtension(destFile, ".metadata.json"), overwrite: true);
    }

    private static void SeedExistingGold(string testsDir, string suite, Dictionary<string, Dictionary<string, (int rank, string src)>> golds)
    {
        var byName = new Dictionary<string, List<(string platApi, string device, string path)>>(StringComparer.OrdinalIgnoreCase);
        CollectGold(testsDir, suite, byName);
        foreach (var (name, existing) in byName)
        {
            var key = $"{suite}|{name}";
            if (!golds.TryGetValue(key, out var buckets)) golds[key] = buckets = new(StringComparer.OrdinalIgnoreCase);
            foreach (var g in existing) buckets[$"{g.platApi}/{g.device}"] = (Rank(g.platApi), g.path);
        }
    }

    // Existing gold layout: tests/<Suite>/<Platform.API>/<Device>/<name>.png (skip the "local" dir).
    private static void CollectGold(string testsDir, string suite, Dictionary<string, List<(string platApi, string device, string path)>> byName)
    {
        var suiteDir = Path.Combine(testsDir, suite);
        if (!Directory.Exists(suiteDir)) return;
        foreach (var platDir in Directory.GetDirectories(suiteDir))
        {
            var platApi = Path.GetFileName(platDir);
            if (platApi.Equals("local", StringComparison.OrdinalIgnoreCase)) continue;
            foreach (var devDir in Directory.GetDirectories(platDir))
            {
                var device = Path.GetFileName(devDir);
                foreach (var png in Directory.GetFiles(devDir, "*.png"))
                {
                    var name = Path.GetFileName(png);
                    if (!byName.TryGetValue(name, out var list)) byName[name] = list = [];
                    list.Add((platApi, device, png));
                }
            }
        }
    }

    // Suite == the top-level assembly-named dir under tests/ (excluding the local/ output dir).
    private static IEnumerable<string> EnumerateSuites(string testsDir)
    {
        if (!Directory.Exists(testsDir)) yield break;
        foreach (var dir in Directory.GetDirectories(testsDir))
        {
            var name = Path.GetFileName(dir);
            if (!name.Equals("local", StringComparison.OrdinalIgnoreCase))
                yield return name;
        }
    }

    private static int Rank(string platformApi)
    {
        int i = Array.IndexOf(PlatformApiPriority, platformApi);
        return i < 0 ? PlatformApiPriority.Length : i;
    }

    private static string? GetArg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    // Recognise a --source that names a CI run rather than a directory: a full Actions URL, "repo:id"
    // (repo = owner or owner/name), or a bare run id. Returns false for plain paths.
    private static bool TryParseCiSource(string source, out string runId, out string? repo)
    {
        runId = "";
        repo = null;
        var url = Regex.Match(source, @"github\.com/([^/]+/[^/]+)/actions/runs/(\d+)", RegexOptions.IgnoreCase);
        if (url.Success) { repo = url.Groups[1].Value; runId = url.Groups[2].Value; return true; }
        var repoId = Regex.Match(source, @"^([^:]+):(\d+)$");
        if (repoId.Success) { repo = repoId.Groups[1].Value; runId = repoId.Groups[2].Value; return true; }
        if (source.Length > 0 && source.All(char.IsDigit)) { runId = source; return true; }
        return false;
    }

    // Download a CI run's gold-images artifact (via the shared CiArtifacts helper) and return the
    // tree root (<Suite>/<Platform.API>/<Device>/<name>.png). A bare repo ("owner") expands to
    // "owner/stride"; with no repo, resolve which of the checkout's github remotes owns the run.
    private static string? DownloadCiArtifact(string runId, string? repo, out string error)
    {
        error = "";
        if (!string.IsNullOrEmpty(repo) && !repo.Contains('/'))
            repo = $"{repo}/stride";
        if (string.IsNullOrEmpty(repo))
            // bare id: find the owning remote in this checkout, else fall back to upstream.
            repo = CiArtifacts.ResolveRepoFromRemotes(runId) ?? CiArtifacts.UpstreamRepo;

        var dir = Path.Combine(Path.GetTempPath(), "stride-compare-gold", "ci", runId);
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best-effort clean */ }
        Directory.CreateDirectory(dir);

        Console.WriteLine($"Downloading gold-images from run {runId}{(string.IsNullOrEmpty(repo) ? "" : $" ({repo})")} ...");
        var dlError = CiArtifacts.Download(runId, repo, "gold-images", dir);
        if (dlError is not null)
        {
            error = $"{dlError} — is run {runId} a test-gold-gen run with a gold-images artifact?";
            return null;
        }

        // gh may nest the contents under <dir>/gold-images/; find where a png sits 4 levels deep and
        // treat that as the tree root.
        var png = Directory.EnumerateFiles(dir, "*.png", SearchOption.AllDirectories).FirstOrDefault();
        if (png is null) { error = $"run {runId} gold-images artifact contained no PNGs"; return null; }
        var rel = Path.GetRelativePath(dir, png).Replace('\\', '/').Split('/');
        var root = dir;
        for (int i = 0; i < rel.Length - 4; i++) root = Path.Combine(root, rel[i]);
        return root;
    }

    private static void Print(Summary s, bool dryRun)
    {
        var prefix = dryRun ? "[dry-run] " : "";
        void Section(string title, List<string> items)
        {
            if (items.Count == 0) return;
            Console.WriteLine($"{prefix}{title} ({items.Count}):");
            foreach (var i in items) Console.WriteLine($"  {i}");
        }
        Section("Promoted (new gold)", s.Promoted);
        Section("Updated (changed gold)", s.Changed);
        Section("Skipped — covered by fallback", s.CoveredByFallback);
        Section("Removed — redundant existing gold", s.RemovedRedundant);
        Console.WriteLine($"{prefix}Summary: {s.Promoted.Count} new, {s.Changed.Count} changed, " +
            $"{s.CoveredByFallback.Count} covered, {s.RemovedRedundant.Count} removed, {s.Unchanged.Count} unchanged.");
    }

    private static void WriteSummary(string? outPath, Summary s)
    {
        if (string.IsNullOrEmpty(outPath)) return;
        File.WriteAllText(outPath, JsonSerializer.Serialize(s, new JsonSerializerOptions { WriteIndented = true }));
    }

    private sealed class Summary
    {
        public List<string> Promoted { get; init; } = [];
        public List<string> Changed { get; init; } = [];
        public List<string> CoveredByFallback { get; init; } = [];
        public List<string> RemovedRedundant { get; init; } = [];
        public List<string> Unchanged { get; init; } = [];
    }
}
