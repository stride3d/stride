// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Stride.Graphics.Regression;

// Headless `stats`: reads the comparison sidecars (<name>.results.json) that every screenshot test writes,
// pass or fail, across several runs, and shows per thresholds.jsonc rule how close the runs came to its
// limits, per lane and CPU model. Tells whether a rule can be tightened, or whether only one CPU model
// needs it.
internal static class GoldStats
{
    private const string Usage = """
        Stride.CompareGold stats [--source <dir|run>]... [--last N] [--branch <name>] [--workflow <file>]
                                 [--repo <owner/name>] [--image <pattern>] [--all] [--tests <dir>] [--out <json>]

          Shows, per thresholds.jsonc rule, the worst pixel counts seen against each of its limits, per
          lane and CPU model. Input is the <name>.results.json sidecars that tests write next to their
          output for every screenshot, pass or fail, and the <name>.metadata.json with the CPU.

          --source    A test output tree (tests/local, or downloaded test-artifacts-*), or a CI run whose
                      test-artifacts-* get downloaded: run id, "owner:id", "owner/repo:id" or a run URL.
                      Repeat it for several runs.
          --last N    Add the last N completed runs of --workflow (default main.yml) in --repo (default
                      stride3d/stride), only on --branch if given.
          --image     Only images matching this pattern ('*' and '?' wildcards).
          --all       Also list lanes without a rule whose pixels all stayed under 3. By default, lanes
                      without a rule show only when they had pixels at 3+.
          --tests     Gold tests/ dir (default: the enclosing Stride checkout). Its thresholds.jsonc files
                      give the current rules. Samples compared against a gold that has changed since are
                      skipped.
          --out       Also write the rows as JSON.

          Completed CI runs are cached under %TEMP%/stride-compare-gold/stats/.
        """;

    private const string SidecarSuffix = ".results.json";

    private sealed record Sample(string Run, string Suite, string Lane, string Name, bool Passed,
        string GoldLane, int MaxDiff, Dictionary<string, int> Histogram, string Cpu);

    // Gold: "own" when every sample compared against the lane's own gold, else the lanes whose gold it fell back to.
    private sealed record Row(string Suite, int RuleIndex, string Lane, string Cpu, string Gold, int Samples, int Fails,
        int MaxDiff, Dictionary<string, int> Worst, List<string> Images);

    public static int Run(string[] args, Func<string, string?> findStrideRoot)
    {
        if (args.Contains("--help") || args.Contains("-h"))
        {
            Console.WriteLine(Usage);
            return 0;
        }

        var sources = GetArgs(args, "--source");
        var testsDir = GetArg(args, "--tests")
            ?? (findStrideRoot(AppContext.BaseDirectory) is { } r1 ? Path.Combine(r1, "tests")
                : findStrideRoot(Directory.GetCurrentDirectory()) is { } r2 ? Path.Combine(r2, "tests")
                : null);
        if (testsDir is null)
        {
            Console.Error.WriteLine("stats: could not locate the tests/ directory; pass --tests <dir>");
            return 2;
        }
        var imagePattern = GetArg(args, "--image");
        var showAll = args.Contains("--all");
        var outPath = GetArg(args, "--out");

        if (GetArg(args, "--last") is { } lastArg)
        {
            if (!int.TryParse(lastArg, out var last) || last <= 0)
            {
                Console.Error.WriteLine($"stats: --last needs a positive number, got '{lastArg}'");
                return 2;
            }
            var repo = GetArg(args, "--repo") ?? CiArtifacts.UpstreamRepo;
            var runs = CiArtifacts.ListCompletedRuns(repo, GetArg(args, "--workflow") ?? "main.yml", GetArg(args, "--branch"), last, out var listError);
            if (runs is null)
            {
                Console.Error.WriteLine($"stats: {listError}");
                return 2;
            }
            sources.AddRange(runs.Select(id => $"{repo}:{id}"));
        }

        if (sources.Count == 0)
            sources.Add(Path.Combine(testsDir, "local"));

        // Each source becomes a directory tree to scan; CI runs are downloaded first, a few at a time.
        var roots = new (string label, string? dir)[sources.Count];
        Parallel.For(0, sources.Count, new ParallelOptions { MaxDegreeOfParallelism = 4 }, i =>
        {
            var source = sources[i];
            if (Directory.Exists(source))
                roots[i] = (source, source);
            else if (HeadlessPromote.TryParseCiSource(source, out var runId, out var repo))
                roots[i] = ($"run {runId}", DownloadRun(runId, repo));
            else
            {
                Console.Error.WriteLine($"stats: '{source}' is neither a directory nor a CI run, skipped");
                roots[i] = (source, null);
            }
        });

        var samples = new List<Sample>();
        int stale = 0;
        var goldHashes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (label, dir) in roots)
        {
            if (dir is null) continue;
            foreach (var file in Directory.EnumerateFiles(dir, "*" + SidecarSuffix, SearchOption.AllDirectories))
            {
                var sample = ReadSample(label, dir, file, testsDir, goldHashes, out var isStale);
                if (isStale) stale++;
                if (sample is not null && (imagePattern is null || WildcardMatches(imagePattern, sample.Name)))
                    samples.Add(sample);
            }
        }

        var runCount = roots.Count(r => r.dir is not null);
        Console.WriteLine($"{runCount} source(s), {samples.Count} samples"
            + (stale > 0 ? $", {stale} skipped (compared against a gold that has changed since)" : "") + ".");
        Console.WriteLine();

        var rows = BuildRows(samples, testsDir);
        Print(rows, testsDir, showAll);

        if (!string.IsNullOrEmpty(outPath))
            File.WriteAllText(outPath, JsonSerializer.Serialize(rows, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    // Downloads the run's test-artifacts-* into the stats cache, or reuses the cached copy of a
    // completed run. Null when the run has none (expired, or no screenshot lane ran).
    private static string? DownloadRun(string runId, string? repo)
    {
        repo = CiArtifacts.ResolveRepo(runId, repo);
        var dir = Path.Combine(Path.GetTempPath(), "stride-compare-gold", "stats", repo.Replace('/', '_'), runId);
        var completeMarker = Path.Combine(dir, ".complete");
        if (File.Exists(completeMarker))
            return dir;

        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best-effort clean */ }
        Directory.CreateDirectory(dir);
        Console.WriteLine($"Downloading test-artifacts-* from run {runId} ({repo}) ...");
        var completed = CiArtifacts.IsCompleted(runId, repo);
        if (CiArtifacts.DownloadMatching(runId, repo, "test-artifacts-*", dir) is { } error)
        {
            Console.Error.WriteLine($"stats: run {runId}: {error}");
            return null;
        }
        if (completed)
            File.WriteAllText(completeMarker, "");
        return dir;
    }

    // One sidecar → one sample, laid out as .../<Suite>/<Platform.API>/<Device>/<name>.results.json.
    // The attempt that counts is the gold that matched, else the lane's own gold, else the closest one.
    private static Sample? ReadSample(string run, string root, string file, string testsDir,
        Dictionary<string, string?> goldHashes, out bool stale)
    {
        stale = false;
        var parts = Path.GetRelativePath(root, file).Replace('\\', '/').Split('/');
        if (parts.Length < 4) return null;
        var suite = parts[^4];
        var lane = $"{parts[^3]}/{parts[^2]}";
        var baseName = parts[^1][..^SidecarSuffix.Length];

        JsonDocument doc;
        try { doc = JsonDocument.Parse(File.ReadAllText(file)); }
        catch (JsonException) { return null; }
        using (doc)
        {
            var sidecar = doc.RootElement;
            var matched = sidecar.TryGetProperty("matched", out var m) ? m.GetString() : null;
            if (!sidecar.TryGetProperty("attempts", out var attemptsElement)) return null;
            var attempts = attemptsElement.EnumerateArray().ToList();
            if (attempts.Count == 0) return null;

            var attempt = attempts.FirstOrDefault(a => matched is not null && a.GetProperty("gold").GetString() == matched);
            if (attempt.ValueKind == JsonValueKind.Undefined)
                attempt = attempts.FirstOrDefault(a => a.GetProperty("kind").GetString() == "reference");
            if (attempt.ValueKind == JsonValueKind.Undefined)
                attempt = attempts.MinBy(a => CountIn(ReadHistogram(a), new AllowBucket(3, int.MaxValue, 0)));

            var gold = attempt.GetProperty("gold").GetString()!.Replace('\\', '/').Split('/');
            var goldLane = gold.Length >= 3 ? $"{gold[^3]}/{gold[^2]}" : lane;
            if (attempt.TryGetProperty("goldHash", out var hashElement) && hashElement.GetString() is { } goldHash)
            {
                var goldPath = Path.Combine(testsDir, suite, goldLane, gold[^1]);
                if (!goldHashes.TryGetValue(goldPath, out var current))
                    goldHashes[goldPath] = current = GoldHash(goldPath);
                if (!string.Equals(current, goldHash, StringComparison.OrdinalIgnoreCase))
                {
                    stale = true;
                    return null;
                }
            }

            return new Sample(run, suite, lane, baseName + ".png",
                Passed: sidecar.GetProperty("outcome").GetString() == "Pass",
                GoldLane: goldLane,
                MaxDiff: attempt.GetProperty("maxDiff").GetInt32(),
                Histogram: ReadHistogram(attempt),
                Cpu: ReadCpu(Path.Combine(Path.GetDirectoryName(file)!, baseName + ".metadata.json")));
        }
    }

    private static Dictionary<string, int> ReadHistogram(JsonElement attempt) =>
        attempt.GetProperty("buckets").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());

    private static string ReadCpu(string metadataPath)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(metadataPath));
            if (doc.RootElement.TryGetProperty("cpu", out var cpu) && cpu.GetString() is { Length: > 0 } name)
                return ShortCpu(name);
        }
        catch (Exception e) when (e is IOException or JsonException) { }
        return "?";
    }

    // "AMD EPYC 7763 64-Core Processor" → "AMD EPYC 7763", "Intel(R) Xeon(R) Platinum 8370C CPU @ 2.80GHz" →
    // "Intel Xeon Platinum 8370C".
    private static string ShortCpu(string name)
    {
        name = Regex.Replace(name, @"\((R|TM)\)", "", RegexOptions.IgnoreCase);
        name = Regex.Replace(name, @"\s+CPU\s+@.*$|\s+\d+-Core Processor$|\s+Processor$", "", RegexOptions.IgnoreCase);
        return Regex.Replace(name, @"\s+", " ").Trim();
    }

    // SHA-256 of a gold, matching the sidecar's goldHash. A git-lfs pointer (LFS not pulled) gives the
    // same value through its oid. Null when the gold is gone.
    private static string? GoldHash(string path)
    {
        if (!File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 512 && Encoding.ASCII.GetString(bytes).StartsWith("version https://git-lfs", StringComparison.Ordinal))
        {
            var oid = Regex.Match(Encoding.ASCII.GetString(bytes), @"oid sha256:([0-9a-f]{64})");
            return oid.Success ? oid.Groups[1].Value : null;
        }
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    // Pixels of a histogram (keys "0", "1-2", "3-5", "6-15", "16+") inside a rule range. A histogram
    // bucket that only partly overlaps the range counts whole, so for ranges not aligned to the
    // histogram (like "3-8") this is an upper bound.
    private static int CountIn(Dictionary<string, int> histogram, AllowBucket range)
    {
        int count = 0;
        foreach (var (key, pixels) in histogram)
        {
            var bucket = AllowBucket.Parse(key, 0);
            if (bucket.Max >= range.Min && bucket.Min <= range.Max)
                count += pixels;
        }
        return count;
    }

    // Groups samples by the rule that applies to them now (RuleIndex -1 = no rule, default "3+": 0),
    // then by lane and CPU model.
    private static List<Row> BuildRows(List<Sample> samples, string testsDir)
    {
        var rows = new List<Row>();
        var groups = samples.GroupBy(s =>
        {
            var rules = ImageThreshold.LoadRules(Path.Combine(testsDir, s.Suite));
            var (platform, api, device) = SplitLane(s.Lane);
            var rule = ImageThreshold.ResolveRule(rules, s.Name, platform, api, device);
            var index = rule?.Allow is null ? -1 : Array.IndexOf(rules, rule);
            return (s.Suite, index, s.Lane, s.Cpu);
        });
        foreach (var g in groups)
        {
            var buckets = Buckets(testsDir, g.Key.Suite, g.Key.index);
            var goldLanes = g.Select(s => s.GoldLane == s.Lane ? "own" : s.GoldLane).Distinct().Order(StringComparer.Ordinal);
            rows.Add(new Row(g.Key.Suite, g.Key.index, g.Key.Lane, g.Key.Cpu, string.Join(", ", goldLanes),
                Samples: g.Count(),
                Fails: g.Count(s => !s.Passed),
                MaxDiff: g.Max(s => s.MaxDiff),
                Worst: buckets.ToDictionary(ImageThreshold.RangeKey, b => g.Max(s => CountIn(s.Histogram, b))),
                Images: g.Select(s => s.Name).Distinct().Order(StringComparer.Ordinal).ToList()));
        }
        return rows
            .OrderBy(r => r.Suite, StringComparer.Ordinal).ThenBy(r => r.RuleIndex < 0 ? int.MaxValue : r.RuleIndex)
            .ThenBy(r => r.Lane, StringComparer.Ordinal).ThenBy(r => r.Cpu, StringComparer.Ordinal)
            .ToList();
    }

    private static AllowBucket[] Buckets(string testsDir, string suite, int ruleIndex)
    {
        if (ruleIndex < 0) return ImageThreshold.DefaultBuckets;
        var rule = ImageThreshold.LoadRules(Path.Combine(testsDir, suite))[ruleIndex];
        return rule.Allow!.Select(kv => AllowBucket.Parse(kv.Key, kv.Value)).ToArray();
    }

    private static void Print(List<Row> rows, string testsDir, bool showAll)
    {
        foreach (var rule in rows.GroupBy(r => (r.Suite, r.RuleIndex)))
        {
            var buckets = Buckets(testsDir, rule.Key.Suite, rule.Key.RuleIndex);
            var shown = rule.Key.RuleIndex >= 0 || showAll
                ? rule.ToList()
                : rule.Where(r => r.Fails > 0 || r.Worst.Values.Any(v => v > 0)).ToList();
            if (rule.Key.RuleIndex < 0 && shown.Count == 0) continue;

            Console.WriteLine(rule.Key.RuleIndex >= 0
                ? $"{rule.Key.Suite} rule #{rule.Key.RuleIndex + 1}: {Describe(ImageThreshold.LoadRules(Path.Combine(testsDir, rule.Key.Suite))[rule.Key.RuleIndex])}"
                : $"{rule.Key.Suite}, no rule (default {{\"3+\": 0}}){(showAll ? "" : ", lanes with pixels at 3+")}:");
            foreach (var b in buckets)
            {
                var key = ImageThreshold.RangeKey(b);
                var worst = rule.Max(r => r.Worst[key]);
                Console.WriteLine($"  {key,-6} limit {b.Limit,5}, worst {worst,5}" + (b.Limit > 0 ? $" ({100 * worst / b.Limit}%)" : ""));
            }

            var laneWidth = Math.Max(4, shown.Max(r => r.Lane.Length));
            var cpuWidth = Math.Max(3, shown.Max(r => r.Cpu.Length));
            var goldWidth = Math.Max(4, shown.Max(r => r.Gold.Length));
            var header = new StringBuilder($"  {"lane".PadRight(laneWidth)}  {"CPU".PadRight(cpuWidth)}  {"gold".PadRight(goldWidth)}  samples  fails  max diff");
            foreach (var b in buckets) header.Append($"  {ImageThreshold.RangeKey(b),6}");
            Console.WriteLine(header);
            foreach (var r in shown)
            {
                var line = new StringBuilder($"  {r.Lane.PadRight(laneWidth)}  {r.Cpu.PadRight(cpuWidth)}  {r.Gold.PadRight(goldWidth)}  {r.Samples,7}  {r.Fails,5}  {r.MaxDiff,8}");
                foreach (var b in buckets) line.Append($"  {r.Worst[ImageThreshold.RangeKey(b)],6}");
                if (rule.Key.RuleIndex < 0) line.Append("  ").Append(string.Join(", ", r.Images));
                Console.WriteLine(line);
            }
            Console.WriteLine();
        }
    }

    private static string Describe(ThresholdRule rule)
    {
        var filters = new List<string>();
        if (rule.Image is not null) filters.Add($"image {rule.Image}");
        if (rule.Platform is not null) filters.Add($"platform {rule.Platform}");
        if (rule.Api is not null) filters.Add($"api {rule.Api}");
        if (rule.Device is not null) filters.Add($"device {rule.Device}");
        var allow = string.Join(", ", rule.Allow!.Select(kv => $"\"{kv.Key}\": {kv.Value}"));
        return $"{string.Join(", ", filters)}, allow {{{allow}}}";
    }

    private static (string platform, string? api, string device) SplitLane(string lane)
    {
        var slash = lane.IndexOf('/');
        var platformApi = lane[..slash];
        var dot = platformApi.IndexOf('.');
        return dot >= 0 ? (platformApi[..dot], platformApi[(dot + 1)..], lane[(slash + 1)..]) : (platformApi, null, lane[(slash + 1)..]);
    }

    private static bool WildcardMatches(string pattern, string name) =>
        Regex.IsMatch(name, "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);

    private static string? GetArg(string[] args, string name)
    {
        int i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static List<string> GetArgs(string[] args, string name)
    {
        var values = new List<string>();
        for (int i = 0; i + 1 < args.Length; i++)
            if (args[i] == name) values.Add(args[++i]);
        return values;
    }
}
