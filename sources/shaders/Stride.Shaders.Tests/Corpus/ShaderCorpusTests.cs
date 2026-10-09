// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests.Corpus;

/// <summary>
/// Compiles every corpus permutation and compares its normalized SPIR-V, reflection and explain dump: with the hashes in
/// <c>Corpus/snapshot.txt</c> (<see cref="CompilerOutputUnchanged"/>, the CI check), or with a full baseline run saved locally
/// (<see cref="SaveBaseline"/> then <see cref="MatchesBaseline"/>, which gives a readable diff). See <c>Corpus/README.md</c>.
/// </summary>
public class ShaderCorpusTests
{
    static string SnapshotFile => Path.Combine(ShaderCorpus.CorpusDirectory, "snapshot.txt");

    // Next to the test output, so it survives rebuilds and branch switches
    static string OutputDirectory => Path.Combine(AppContext.BaseDirectory, "ShaderCorpus");
    static string BaselineDirectory => Path.Combine(AppContext.BaseDirectory, "ShaderCorpus.baseline");

    // The parts of a result, in snapshot order: file suffix and text used for the hash (names stripped where it matters)
    static IEnumerable<(string Part, string Suffix, string Hashed, string Written)> Parts(CorpusResult result) => result.Error != null
        ? [("error", ".error.txt", result.Error, result.ErrorDetail!)]
        : [
            ("strict", ".spvasm", result.Strict!, result.StrictNamed!),
            ("legal", ".legal.spvasm", result.Legalized!, result.LegalizedNamed!),
            ("reflection", ".reflection.txt", result.Reflection!, result.Reflection!),
            ("explain", ".explain.txt", result.Explain!, result.Explain!),
        ];

    [Fact]
    public void CompilerOutputUnchanged()
    {
        var current = Run(OutputDirectory, out var filter);
        Check(Compare(ReadSnapshot(SnapshotFile), current, filter, OutputDirectory, baseDirectory: null));
    }

    // The corpus format keeps everything the hash covers (what capture writes, the test reads back)
    [Fact]
    public void CorpusEntriesRoundTrip()
    {
        var entries = ShaderCorpus.ReadEntries();
        Assert.NotEmpty(entries);
        foreach (var entry in entries)
            Assert.Equal(entry.Hash, ShaderCorpusEntry.FromJson(ShaderCorpusEntry.ToJson(entry)).Hash);
    }

    /// <summary>
    /// Saves a full run (every text, not only hashes) as the baseline of <see cref="MatchesBaseline"/>: run it before
    /// changing the compiler.
    /// </summary>
    [Fact(Explicit = true)]
    public void SaveBaseline()
    {
        Run(BaselineDirectory, out _);
        Console.WriteLine($"Baseline saved: {BaselineDirectory}");
    }

    /// <summary>
    /// Compares with the run saved by <see cref="SaveBaseline"/> and writes the diff of every changed file, including
    /// changes the hashes ignore (names).
    /// </summary>
    [Fact(Explicit = true)]
    public void MatchesBaseline()
    {
        if (!File.Exists(Path.Combine(BaselineDirectory, "snapshot.txt")))
            Assert.Fail($"No baseline in {BaselineDirectory}: run {nameof(SaveBaseline)} first (before the compiler change)");
        var current = Run(OutputDirectory, out var filter);
        Check(Compare(ReadSnapshot(Path.Combine(BaselineDirectory, "snapshot.txt")), current, filter, OutputDirectory, BaselineDirectory));
    }

    /// <summary>
    /// Rewrites <c>Corpus/snapshot.txt</c> from the current compiler, after a change was checked to be expected.
    /// </summary>
    [Fact(Explicit = true)]
    public void UpdateSnapshot()
    {
        var current = Run(OutputDirectory, out var filter);
        if (!string.IsNullOrEmpty(filter) || ShaderCorpus.SelectedPlatforms.Length != ShaderCorpus.Platforms.Length)
            Assert.Fail("Updating the snapshot needs the whole corpus on every platform, unset STRIDE_SHADER_CORPUS_FILTER and STRIDE_SHADER_CORPUS_PLATFORMS");
        File.WriteAllText(SnapshotFile, Manifest(current));
        Console.WriteLine($"Snapshot updated: {SnapshotFile}");
    }

    static void Check((bool Failed, string Text) report)
    {
        File.WriteAllText(Path.Combine(OutputDirectory, "report.txt"), report.Text);
        Console.WriteLine(report.Text);
        if (report.Failed)
            Assert.Fail(report.Text);
    }

    /// <summary>
    /// Compiles the corpus (STRIDE_SHADER_CORPUS_FILTER keeps the ids containing it) into <paramref name="outputDirectory"/>:
    /// one readable file per part and entry, plus <c>snapshot.txt</c> with their hashes.
    /// </summary>
    static SortedDictionary<string, string> Run(string outputDirectory, out string? filter)
    {
        if (Directory.Exists(outputDirectory))
            Directory.Delete(outputDirectory, recursive: true);
        Directory.CreateDirectory(outputDirectory);

        var items = ShaderCorpus.LoadItems();
        filter = Environment.GetEnvironmentVariable("STRIDE_SHADER_CORPUS_FILTER");
        if (!string.IsNullOrEmpty(filter))
        {
            var filterValue = filter;
            items = items.Where(x => x.Id.Contains(filterValue, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        // In parallel: the loader is shared and thread-safe, like in the asset compiler
        var platforms = ShaderCorpus.SelectedPlatforms;
        foreach (var platform in platforms)
            Directory.CreateDirectory(Path.Combine(outputDirectory, platform.ToString()));
        var loader = ShaderCorpus.CreateLoader();
        var total = Stopwatch.StartNew();
        var results = new (CorpusResult Result, string Line, double Seconds)[items.Count * platforms.Length];
        Parallel.For(0, results.Length, index =>
        {
            var stopwatch = Stopwatch.StartNew();
            var result = ShaderCorpus.Compile(items[index / platforms.Length], platforms[index % platforms.Length], loader);
            var line = new StringBuilder();
            foreach (var (part, suffix, hashed, written) in Parts(result))
            {
                File.WriteAllText(Path.Combine(outputDirectory, result.Key + suffix), written);
                line.Append($" {part}={ShaderCorpus.ShortHash(hashed)}");
            }
            results[index] = (result, line.ToString().TrimStart(), stopwatch.Elapsed.TotalSeconds);
        });
        total.Stop();

        var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var (result, line, _) in results)
            current[result.Key] = line;
        File.WriteAllText(Path.Combine(outputDirectory, "snapshot.txt"), Manifest(current));

        var mixTime = TimeSpan.FromTicks(results.Sum(x => x.Result.MixTime.Ticks));
        var legalizeTime = TimeSpan.FromTicks(results.Sum(x => x.Result.LegalizeTime.Ticks));
        var normalizeTime = TimeSpan.FromTicks(results.Sum(x => x.Result.NormalizeTime.Ticks));
        var errorCount = current.Values.Count(x => x.StartsWith("error=", StringComparison.Ordinal));
        Console.WriteLine($"Shader corpus: {items.Count} permutations ({items.Count(x => x.SourceFile == null)} captured, {items.Count(x => x.SourceFile != null)} RenderTests) x {string.Join(" + ", platforms)} = {current.Count} compiles, {errorCount} don't compile, {total.Elapsed.TotalSeconds:F1} s");
        Console.WriteLine($"Time ({Environment.ProcessorCount} threads, summed): mix {mixTime.TotalSeconds:F1} s, legalize {legalizeTime.TotalSeconds:F1} s, normalize {normalizeTime.TotalSeconds:F1} s");
        Console.WriteLine($"Output: {outputDirectory}");
        foreach (var (result, _, seconds) in results.OrderByDescending(x => x.Seconds).Take(5))
            Console.WriteLine($"  slowest: {result.Item.Id} {seconds:F2} s");
        return current;
    }

    static string Manifest(SortedDictionary<string, string> entries) => string.Concat(entries.Select(x => $"{x.Key} {x.Value}\n"));

    // Only the lines of the platforms this run compiles: the others are neither checked nor "removed"
    static SortedDictionary<string, string> ReadSnapshot(string path)
    {
        var platformPrefixes = ShaderCorpus.SelectedPlatforms.Select(x => $"{x}/").ToArray();
        var snapshot = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (File.Exists(path))
        {
            foreach (var line in File.ReadAllLines(path))
            {
                var separator = line.IndexOf(' ');
                if (separator > 0 && platformPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)))
                    snapshot[line[..separator]] = line[(separator + 1)..];
            }
        }
        return snapshot;
    }

    static Dictionary<string, string> ParseParts(string value) => value.Split(' ').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);

    static readonly string[] DiffSuffixes = [".error.txt", ".explain.txt", ".reflection.txt", ".legal.spvasm", ".spvasm"];

    static (bool Failed, string Text) Compare(SortedDictionary<string, string> snapshot, SortedDictionary<string, string> current, string? filter, string outputDirectory, string? baseDirectory)
    {
        var changed = new List<string>();
        var lines = new List<string>();
        var namesOnly = new List<string>();
        var stale = 0;
        var unchanged = 0;

        foreach (var (id, value) in current)
        {
            if (!snapshot.TryGetValue(id, out var expected))
            {
                lines.Add($"added    {id}");
                continue;
            }
            if (expected == value)
            {
                if (value.StartsWith("error=", StringComparison.Ordinal))
                    stale++;
                else
                    unchanged++;
                // The hashes ignore names; with the base files at hand, still say where only names changed
                if (baseDirectory != null && DiffSuffixes.Any(suffix => FileDiffers(baseDirectory, outputDirectory, id + suffix)))
                    namesOnly.Add(id);
                continue;
            }

            var expectedParts = ParseParts(expected);
            var currentParts = ParseParts(value);
            string kind;
            if (currentParts.ContainsKey("error"))
                kind = expectedParts.ContainsKey("error") ? "error changed" : "now fails";
            else if (expectedParts.ContainsKey("error"))
                kind = "now compiles";
            else
            {
                // Strict-only = same code after legalization (e.g. function order); the rest changes what runs or how it's bound
                var differing = currentParts.Keys.Where(x => expectedParts.GetValueOrDefault(x) != currentParts[x]).ToList();
                kind = string.Join("+", differing);
                if (differing is ["strict"] or ["explain"] or ["strict", "explain"])
                    kind += " (same legalized code)";
            }
            lines.Add($"changed  {id}: {kind}");
            changed.Add(id);
        }
        foreach (var id in snapshot.Keys.Where(x => !current.ContainsKey(x)))
        {
            if (string.IsNullOrEmpty(filter))
                lines.Add($"removed  {id}");
        }

        var text = new StringBuilder();
        text.AppendLine($"Shader corpus ({(baseDirectory != null ? "against the saved baseline" : "against Corpus/snapshot.txt")}): {current.Count} compiles, {unchanged} unchanged, {stale} stale (failed to compile before too), {lines.Count} differences");
        foreach (var line in lines)
            text.AppendLine(line);
        foreach (var id in namesOnly)
            text.AppendLine($"names    {id} (not a difference: only names changed)");

        if (lines.Count > 0 || namesOnly.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Files of this run: {outputDirectory}");
            if (baseDirectory != null)
            {
                // The whole diff can be MBs: the report keeps the start, diff.txt has everything
                var diff = Diff(baseDirectory, outputDirectory, [.. changed, .. namesOnly]);
                File.WriteAllText(Path.Combine(outputDirectory, "diff.txt"), diff);
                text.AppendLine($"Full diff: {Path.Combine(outputDirectory, "diff.txt")}");
                const int maxReportLength = 20000;
                text.Append(diff.Length > maxReportLength ? diff[..maxReportLength] + $"{Environment.NewLine}... (truncated, see diff.txt)" : diff);
            }
            else if (lines.Count > 0)
            {
                text.AppendLine($"For a readable diff: {nameof(SaveBaseline)} on the base commit, then {nameof(MatchesBaseline)} here. If the change is expected: {nameof(UpdateSnapshot)}, and commit Corpus/snapshot.txt.");
            }
        }
        return (lines.Count > 0, text.ToString());
    }

    static bool FileDiffers(string baseDirectory, string outputDirectory, string fileName)
    {
        var before = Path.Combine(baseDirectory, fileName);
        var after = Path.Combine(outputDirectory, fileName);
        if (File.Exists(before) != File.Exists(after))
            return true;
        return File.Exists(before) && File.ReadAllText(before) != File.ReadAllText(after);
    }

    // Unified diff of the files that changed, through git (always there for a repository checkout)
    static string Diff(string baseDirectory, string outputDirectory, List<string> ids)
    {
        var text = new StringBuilder();
        foreach (var id in ids)
        {
            foreach (var suffix in DiffSuffixes)
            {
                if (!FileDiffers(baseDirectory, outputDirectory, id + suffix))
                    continue;
                var before = Path.Combine(baseDirectory, id + suffix);
                var after = Path.Combine(outputDirectory, id + suffix);
                text.Append(GitDiff(File.Exists(before) ? before : "/dev/null", File.Exists(after) ? after : "/dev/null"));
            }
        }

        // Short headers: "base/<file>" and "new/<file>" instead of two absolute folders (git quotes and doubles backslashes)
        foreach (var (directory, label) in new[] { (baseDirectory, "base"), (outputDirectory, "new") })
        {
            text.Replace(directory.Replace("\\", "\\\\") + "\\\\", label + "/");
            text.Replace(directory.Replace('\\', '/') + "/", label + "/");
            text.Replace(directory + Path.DirectorySeparatorChar, label + "/");
        }
        return text.ToString();
    }

    static string GitDiff(string before, string after)
    {
        var startInfo = new ProcessStartInfo("git") { RedirectStandardOutput = true, UseShellExecute = false };
        foreach (var argument in new[] { "diff", "--no-index", "--no-color", "-U3", before, after })
            startInfo.ArgumentList.Add(argument);
        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}
