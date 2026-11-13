// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests.Corpus;

/// <summary>
/// Compiles every corpus permutation and compares its normalized SPIR-V, reflection and explain dump with a baseline run:
/// <see cref="SaveBaseline"/> on the base commit, then <see cref="MatchesBaseline"/> on the change (CI runs both, see
/// <c>Corpus/README.md</c>). The comparison also lists the engine shader classes that no permutation mixes any more.
/// </summary>
public partial class ShaderCorpusTests
{
    // Next to the test output by default, so a baseline survives rebuilds and branch switches
    static string OutputDirectory => DirectoryFromEnvironment("STRIDE_SHADER_CORPUS_OUTPUT", "ShaderCorpus");
    static string BaselineDirectory => DirectoryFromEnvironment("STRIDE_SHADER_CORPUS_BASELINE", "ShaderCorpus.baseline");

    static string DirectoryFromEnvironment(string variable, string defaultName) => Environment.GetEnvironmentVariable(variable) is { Length: > 0 } directory
        ? Path.GetFullPath(directory)
        : Path.Combine(AppContext.BaseDirectory, defaultName);

    // The files of a run besides the per-permutation ones
    const string HashesFile = "hashes.txt";
    const string UncoveredFile = "uncovered.txt";
    const string ChangesFile = "changes.txt";

    // The parts of a result, in hash order: file suffix and text used for the hash (names stripped where it matters)
    static IEnumerable<(string Part, string Suffix, string Hashed, string Written)> Parts(CorpusResult result) => result.Error != null
        ? [("error", ".error.txt", result.Error, result.ErrorDetail!)]
        : [
            ("strict", ".spvasm", result.Strict!, result.StrictNamed!),
            ("legal", ".legal.spvasm", result.Legalized!, result.LegalizedNamed!),
            ("reflection", ".reflection.txt", result.Reflection!, result.Reflection!),
            ("explain", ".explain.txt", result.Explain!, result.Explain!),
        ];

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
    /// Saves a full run as the baseline of <see cref="MatchesBaseline"/>: run it on the commit before the change.
    /// </summary>
    [Fact(Explicit = true)]
    public void SaveBaseline()
    {
        Run(BaselineDirectory, out _, out _);
        Console.WriteLine($"Baseline saved: {BaselineDirectory}");
    }

    /// <summary>
    /// Compares with the run saved by <see cref="SaveBaseline"/>, and writes <c>report.txt</c>, <c>summary.md</c> and the diff
    /// of every changed file (<c>diff.txt</c>, including changes the hashes ignore: names).
    /// </summary>
    [Fact(Explicit = true)]
    public void MatchesBaseline()
    {
        if (!File.Exists(Path.Combine(BaselineDirectory, HashesFile)))
            Assert.Fail($"No baseline in {BaselineDirectory}: run {nameof(SaveBaseline)} first, on the commit before the change");
        var current = Run(OutputDirectory, out var filter, out var uncovered);
        var report = Compare(ReadHashes(Path.Combine(BaselineDirectory, HashesFile)), current, filter, OutputDirectory, BaselineDirectory);
        if (string.IsNullOrEmpty(filter))
        {
            var baselineUncovered = ReadUncovered(BaselineDirectory);
            var newlyUncovered = uncovered.Where(x => !baselineUncovered.Contains(x.Key)).ToList();
            report.Uncovered.AddRange(newlyUncovered.Select(x => $"{x.Key} ({x.Value})"));
            report.Changes.AddRange(newlyUncovered.Select(x => $"{x.Key}\tuncovered\t-\t-"));
        }
        Check(report, current.Count);
    }

    /// <param name="Changes">One line per difference, tab-separated: the id, the kind of change, the base and the new hashes
    /// (<c>-</c> where there is none). Written to <c>changes.txt</c>, which CI compares with what a maintainer accepted.</param>
    sealed record Report(List<(string Id, string Kind)> Differences, List<KindGroup> Kinds, List<ChangedLine> ChangedLines, int NamesOnly, List<string> Uncovered, StringBuilder Text, string Diff, List<string> Changes);

    /// <summary>
    /// The changed permutations of one kind of change (<c>now fails</c>, <c>strict+legal+reflection</c>...).
    /// </summary>
    /// <param name="Diff">The diff of the first permutation, as an example.</param>
    sealed record KindGroup(string Kind, List<string> Ids, string Diff)
    {
        public (int Order, string? Meaning) Risk { get; } = RiskOf(Kind);
    }

    /// <summary>
    /// A line that a change adds (<c>+</c>) or removes (<c>-</c>), with the permutations that have it once the lines that
    /// only moved or shifted cancel out: one source change gives a few such lines, found in every permutation it reaches.
    /// </summary>
    /// <param name="Source">The source line it comes from in most of its permutations: on the new side for <c>+</c>, the base
    /// side for <c>-</c>.</param>
    sealed record ChangedLine(string Line, List<string> Ids, SourceLine? Source);

    /// <param name="File">Relative to the repository.</param>
    /// <param name="OtherPlaces">How many other source lines the changed line also comes from.</param>
    sealed record SourceLine(string File, int Line, string Code, int OtherPlaces);

    // The most likely to break a game first
    static (int Order, string? Meaning) RiskOf(string kind) => kind switch
    {
        "now fails" => (0, "no longer compiles"),
        _ when kind.Contains("reflection", StringComparison.Ordinal) => (1, "resource bindings or constant buffer layouts changed"),
        _ when kind.EndsWith("(same optimized code)", StringComparison.Ordinal) => (4, "same code once optimized: only the order of functions or declarations changed"),
        _ when kind.EndsWith("(same legalized code)", StringComparison.Ordinal) => (5, "same code once legalized"),
        _ when kind.Contains("legal", StringComparison.Ordinal) => (2, "the code that runs changed"),
        _ => (3, null),
    };

    static void Check(Report report, int compiles)
    {
        var text = report.Text;
        if (report.Uncovered.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"{report.Uncovered.Count} engine shader classes that the baseline mixed, or that are new, are mixed by no permutation:");
            foreach (var line in report.Uncovered)
                text.AppendLine($"uncovered {line}");
            text.AppendLine("Capture an effect that uses them (see sources/shaders/Stride.Shaders.Tests/Corpus/README.md), or let the weekly corpus refresh pick them up once a sample or a test compiles them.");
        }
        if (report.Diff.Length > 0)
        {
            // The whole diff can be MBs: the report keeps the start, diff.txt has everything
            const int maxReportLength = 20000;
            text.AppendLine();
            text.AppendLine($"Full diff: {Path.Combine(OutputDirectory, "diff.txt")}");
            text.Append(report.Diff.Length > maxReportLength ? report.Diff[..maxReportLength] + $"{Environment.NewLine}... (truncated, see diff.txt)" : report.Diff);
        }

        File.WriteAllText(Path.Combine(OutputDirectory, "report.txt"), text.ToString());
        File.WriteAllText(Path.Combine(OutputDirectory, "summary.md"), Summary(report, compiles));
        File.WriteAllText(Path.Combine(OutputDirectory, ChangesFile), string.Concat(report.Changes.Order(StringComparer.Ordinal).Select(x => x + "\n")));
        Console.WriteLine(text);
        if (report.Differences.Count > 0 || report.Uncovered.Count > 0)
            Assert.Fail(text.ToString());
    }

    // Markdown for a CI job summary (limited to 1 MB): the kinds of change riskiest first, the changed lines, and one
    // example diff per kind
    static string Summary(Report report, int compiles)
    {
        var text = new StringBuilder();
        var platforms = string.Join(" + ", ShaderCorpus.SelectedPlatforms);
        if (report.Differences.Count == 0 && report.Uncovered.Count == 0)
        {
            text.AppendLine($"Shader corpus ({platforms}): {compiles} compiles, no change.");
            return text.ToString();
        }

        text.AppendLine($"Shader corpus ({platforms}): {report.Differences.Count} of {compiles} compiles changed.");
        if (report.Kinds.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("| Change | Permutations | Effects |");
            text.AppendLine("|---|---|---|");
            foreach (var group in report.Kinds)
                text.AppendLine($"| {KindTitle(group)} | {group.Ids.Count} | {EffectList(group.Ids)} |");
        }
        if (report.NamesOnly > 0)
        {
            text.AppendLine();
            text.AppendLine($"{report.NamesOnly} more permutations differ only in names (in diff.txt, not a change).");
        }
        if (report.Uncovered.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Engine shader classes mixed by no permutation (new, or mixed by the baseline):");
            foreach (var line in report.Uncovered)
                text.AppendLine($"- `{line}`");
        }
        if (report.ChangedLines.Count > 0)
        {
            const int maxLines = 80;
            text.AppendLine();
            text.AppendLine("### Changed lines");
            text.AppendLine();
            text.AppendLine(ChangedLinesExplanation);
            text.AppendLine();
            text.AppendLine("````diff");
            foreach (var line in ChangedLinesText(report.ChangedLines.Take(maxLines), fullPaths: false))
                text.AppendLine(line);
            text.AppendLine("````");
            if (report.ChangedLines.Count > maxLines)
                text.AppendLine($"{report.ChangedLines.Count - maxLines} more lines, found in {report.ChangedLines[maxLines].Ids.Count} permutations or fewer: in report.txt (the job's artifact).");
        }

        // Within the 1 MB of a job summary, with room for the rest
        var diffBudget = 300_000;
        const int maxKindDiffLength = 40_000;
        foreach (var group in report.Kinds.Where(x => x.Diff.Length > 0))
        {
            text.AppendLine();
            text.AppendLine($"### {KindTitle(group)}");
            text.AppendLine();
            if (diffBudget <= 0)
            {
                text.AppendLine($"Example: `{group.Ids[0]}`, in diff.txt (the job's artifact).");
                continue;
            }
            var length = Math.Min(diffBudget, maxKindDiffLength);
            var diff = group.Diff.Length > length ? group.Diff[..Math.Max(group.Diff.LastIndexOf('\n', length), 0)] + "\n... (truncated, see diff.txt in the job's artifact)" : group.Diff.TrimEnd();
            diffBudget -= diff.Length;
            text.AppendLine($"<details><summary>Example: <code>{group.Ids[0]}</code></summary>");
            text.AppendLine();
            text.AppendLine("````diff");
            text.AppendLine(diff);
            text.AppendLine("````");
            text.AppendLine();
            text.AppendLine("</details>");
        }
        return text.ToString();
    }

    const string ChangedLinesExplanation = "Each line the change adds (+) or removes (-), with how many permutations have it, once lines that only moved or shifted cancel out "
        + "(numbered IDs, the IDs that names point to, the numbers added to a repeated name, constant buffer offsets and sizes, member indices). One source change gives a few lines found in many permutations: "
        + "read from the top. Lines found in few permutations are worth a look. Under a line, the .sdsl line it comes from (new for +, base for -).";

    static string ChangedLineText(ChangedLine line) => $"{line.Line[0]}{line.Ids.Count,5}  {line.Line[1..]}";

    // Each changed line, and under it its source line when not the same as the line above's
    static IEnumerable<string> ChangedLinesText(IEnumerable<ChangedLine> lines, bool fullPaths, Func<ChangedLine, string>? suffix = null)
    {
        string? previousSource = null;
        foreach (var line in lines)
        {
            yield return ChangedLineText(line) + suffix?.Invoke(line);
            if (line.Source is not { } source)
            {
                previousSource = null;
                continue;
            }
            var file = fullPaths ? source.File : Path.GetFileName(source.File);
            var sourceText = $"{"",8}at {file}:{source.Line}  {source.Code}{(source.OtherPlaces > 0 ? $"  (and {source.OtherPlaces} more places)" : "")}";
            if (sourceText != previousSource)
                yield return sourceText;
            previousSource = sourceText;
        }
    }

    static string KindTitle(KindGroup group) => group.Risk.Meaning is { } meaning ? $"{group.Kind}: {meaning}" : group.Kind;

    // "`StrideForwardShadingEffect` (200), `StrideForwardShadingEffect.ShadowMapCaster` (90), and 3 more effects"
    static string EffectList(List<string> ids, int maxEffects = 5)
    {
        var effects = ids.GroupBy(EffectName).OrderByDescending(x => x.Count()).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
        var list = string.Join(", ", effects.Take(maxEffects).Select(x => $"`{x.Key}` ({x.Count()})"));
        return effects.Count > maxEffects ? $"{list}, and {effects.Count - maxEffects} more effects" : list;
    }

    // "Direct3D11/StrideForwardShadingEffect.ShadowMapCaster.0123456789" is a permutation of StrideForwardShadingEffect.ShadowMapCaster,
    // "Direct3D11/RenderTests.ArrayAssign" one of RenderTests
    static string EffectName(string id)
    {
        var name = id[(id.IndexOf('/') + 1)..];
        if (name.StartsWith("RenderTests.", StringComparison.Ordinal))
            return "RenderTests";
        var dot = name.LastIndexOf('.');
        return dot > 0 && name.Length - dot == 11 && name[(dot + 1)..].All(char.IsAsciiHexDigitLower) ? name[..dot] : name;
    }

    /// <summary>
    /// Compiles the corpus (STRIDE_SHADER_CORPUS_FILTER keeps the ids containing it) into <paramref name="outputDirectory"/>:
    /// one readable file per part and entry, <c>hashes.txt</c> with their hashes, and <c>uncovered.txt</c>.
    /// </summary>
    /// <param name="uncovered">The engine shader classes no permutation mixes (from the explain dumps), with their file.</param>
    static SortedDictionary<string, string> Run(string outputDirectory, out string? filter, out SortedDictionary<string, string> uncovered)
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
        var sourceFiles = new ConcurrentDictionary<string, byte>(StringComparer.Ordinal);
        Parallel.For(0, results.Length, index =>
        {
            var stopwatch = Stopwatch.StartNew();
            var result = ShaderCorpus.Compile(items[index / platforms.Length], platforms[index % platforms.Length], loader, optimize: true);
            var line = new StringBuilder();
            foreach (var (part, suffix, hashed, written) in Parts(result))
            {
                File.WriteAllText(Path.Combine(outputDirectory, result.Key + suffix), written);
                line.Append($" {part}={ShaderCorpus.ShortHash(hashed)}");
            }
            if (result.StrictLocations != null)
                File.WriteAllText(Path.Combine(outputDirectory, result.Key + ".spvasm" + LocationsSuffix), LocationsText(result.StrictLocations, sourceFiles));
            if (result.LegalizedLocations != null)
                File.WriteAllText(Path.Combine(outputDirectory, result.Key + ".legal.spvasm" + LocationsSuffix), LocationsText(result.LegalizedLocations, sourceFiles));
            if (result.Optimized != null)
                File.WriteAllText(Path.Combine(outputDirectory, result.Key + OptimizedSuffix), result.Optimized);
            results[index] = (result, line.ToString().TrimStart(), stopwatch.Elapsed.TotalSeconds);
        });
        foreach (var file in sourceFiles.Keys)
        {
            var copy = Path.Combine(outputDirectory, SourcesDirectory, file);
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(Path.Combine(ShaderCorpus.RepositoryRoot, file), copy);
        }
        total.Stop();

        var current = new SortedDictionary<string, string>(StringComparer.Ordinal);
        var mixed = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (result, line, _) in results)
        {
            current[result.Key] = line;
            foreach (System.Text.RegularExpressions.Match match in MixedClass().Matches(result.Explain ?? ""))
                mixed.Add(match.Groups[1].Value);
        }
        File.WriteAllText(Path.Combine(outputDirectory, HashesFile), string.Concat(current.Select(x => $"{x.Key} {x.Value}\n")));
        uncovered = new SortedDictionary<string, string>(ShaderCorpus.EngineShaderClasses().Where(x => !mixed.Contains(x.Key)).ToDictionary(), StringComparer.Ordinal);
        if (string.IsNullOrEmpty(filter))
            File.WriteAllText(Path.Combine(outputDirectory, UncoveredFile), string.Concat(uncovered.OrderBy(x => x.Value, StringComparer.Ordinal).Select(x => $"{x.Key}  # {x.Value}\n")));

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

    // Only the lines of the platforms this run compiles: the others are neither checked nor "removed"
    static SortedDictionary<string, string> ReadHashes(string path)
    {
        var platformPrefixes = ShaderCorpus.SelectedPlatforms.Select(x => $"{x}/").ToArray();
        var hashes = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in File.ReadAllLines(path))
        {
            var separator = line.IndexOf(' ');
            if (separator > 0 && platformPrefixes.Any(prefix => line.StartsWith(prefix, StringComparison.Ordinal)))
                hashes[line[..separator]] = line[(separator + 1)..];
        }
        return hashes;
    }

    // Missing when the baseline ran with a filter: then every class counts as listed
    static HashSet<string> ReadUncovered(string directory)
    {
        var path = Path.Combine(directory, UncoveredFile);
        return File.Exists(path)
            ? File.ReadAllLines(path).Select(x => x.Split('#')[0].Trim()).Where(x => x.Length > 0).ToHashSet(StringComparer.Ordinal)
            : ShaderCorpus.EngineShaderClasses().Keys.ToHashSet(StringComparer.Ordinal);
    }

    static Dictionary<string, string> ParseParts(string value) => value.Split(' ').Select(x => x.Split('=')).ToDictionary(x => x[0], x => x[1]);

    const string OptimizedSuffix = ".optimized.spvasm";

    // A class line of an explain dump: "  mixin X"
    [System.Text.RegularExpressions.GeneratedRegex(@"^ +mixin ([A-Za-z_]\w*)", System.Text.RegularExpressions.RegexOptions.Multiline)]
    private static partial System.Text.RegularExpressions.Regex MixedClass();

    static readonly string[] DiffSuffixes = [".error.txt", ".explain.txt", ".reflection.txt", ".legal.spvasm", ".spvasm", OptimizedSuffix];

    static Report Compare(SortedDictionary<string, string> baseline, SortedDictionary<string, string> current, string? filter, string outputDirectory, string baseDirectory)
    {
        var differences = new List<(string Id, string Kind)>();
        var changes = new List<string>();
        var namesOnly = new List<string>();
        var stale = 0;
        var unchanged = 0;

        foreach (var (id, value) in current)
        {
            if (!baseline.TryGetValue(id, out var expected))
            {
                differences.Add((id, "added"));
                changes.Add($"{id}\tadded\t-\t{value}");
                continue;
            }
            if (expected == value)
            {
                if (value.StartsWith("error=", StringComparison.Ordinal))
                    stale++;
                else
                    unchanged++;
                // The hashes ignore names; with the base files at hand, still say where only names changed
                if (DiffSuffixes.Any(suffix => FileDiffers(baseDirectory, outputDirectory, id + suffix)))
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
                // Legalization keeps functions and declaration order: a function that moved or lost a duplicate changes it.
                // Fully optimized with canonical IDs, the code that runs can still be the same.
                else if (differing.Contains("legal") && File.Exists(Path.Combine(baseDirectory, id + OptimizedSuffix)))
                    kind += SameOptimizedCode(baseDirectory, outputDirectory, id) ? " (same optimized code)" : " (optimized code differs too)";
            }
            differences.Add((id, kind));
            changes.Add($"{id}\t{kind}\t{expected}\t{value}");
        }
        if (string.IsNullOrEmpty(filter))
        {
            foreach (var (id, expected) in baseline.Where(x => !current.ContainsKey(x.Key)))
            {
                differences.Add((id, "removed"));
                changes.Add($"{id}\tremoved\t{expected}\t-");
            }
        }

        var diffs = differences.Select(x => x.Id).Concat(namesOnly).ToDictionary(x => x, x => FileDiffs(baseDirectory, outputDirectory, x));
        var kinds = differences
            .GroupBy(x => x.Kind)
            .Select(x => new KindGroup(x.Key, x.Select(y => y.Id).ToList(), string.Concat(diffs[x.First().Id].Select(y => y.Text))))
            .OrderBy(x => x.Risk.Order).ThenByDescending(x => x.Ids.Count).ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ToList();
        var changedLines = ChangedLines(differences.Select(x => (x.Id, diffs[x.Id])), baseDirectory, outputDirectory);

        var text = new StringBuilder();
        text.AppendLine($"Shader corpus (against the baseline {baseDirectory}): {current.Count} compiles, {unchanged} unchanged, {stale} stale (failed to compile before too), {differences.Count} differences");
        foreach (var (id, kind) in differences)
            text.AppendLine($"{(kind is "added" or "removed" ? kind : "changed"),-8} {id}{(kind is "added" or "removed" ? "" : $": {kind}")}");
        foreach (var id in namesOnly)
            text.AppendLine($"names    {id} (not a difference: only names changed)");
        if (changedLines.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Changed lines. {ChangedLinesExplanation}");
            // Where the permutations are few, which ones
            foreach (var line in ChangedLinesText(changedLines, fullPaths: true, x => x.Ids.Count <= 3 ? $"  (in {string.Join(", ", x.Ids)})" : ""))
                text.AppendLine(line);
        }

        var diff = "";
        if (diffs.Count > 0)
        {
            text.AppendLine();
            text.AppendLine($"Files of this run: {outputDirectory}");
            diff = string.Concat(diffs.Values.SelectMany(x => x).Select(x => x.Text));
            File.WriteAllText(Path.Combine(outputDirectory, "diff.txt"), diff);
        }
        return new Report(differences, kinds, changedLines, namesOnly.Count, [], text, diff, changes);
    }

    // Per permutation, the lines its files add and remove, normalized so that a line that only moved or shifted (an offset
    // after a new member, a renumbered ID) is removed and added again, and cancels out. Most used first.
    static List<ChangedLine> ChangedLines(IEnumerable<(string Id, List<(string Suffix, string Text)> FileDiffs)> permutations, string baseDirectory, string outputDirectory)
    {
        var lines = new Dictionary<string, (List<string> Ids, Dictionary<string, int> Locations)>(StringComparer.Ordinal);
        foreach (var (id, fileDiffs) in permutations)
        {
            // The strict and legalized files share most lines: a permutation counts once per line, and once per source line
            var permutationLines = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            foreach (var (suffix, text) in fileDiffs)
            {
                // The optimized code says the same again in another form; a file that is new or gone (added or removed
                // permutation, now fails) is all changed lines, the example diffs show it
                if (suffix == OptimizedSuffix || text.Contains("\n--- /dev/null", StringComparison.Ordinal) || text.Contains("\n+++ /dev/null", StringComparison.Ordinal))
                    continue;
                var baseLocations = ReadLocations(Path.Combine(baseDirectory, id + suffix + LocationsSuffix));
                var newLocations = ReadLocations(Path.Combine(outputDirectory, id + suffix + LocationsSuffix));
                var baseNames = ReadIdNames(Path.Combine(baseDirectory, id + suffix));
                var newNames = ReadIdNames(Path.Combine(outputDirectory, id + suffix));
                var net = new Dictionary<string, (int Count, HashSet<string> Added, HashSet<string> Removed)>(StringComparer.Ordinal);
                int baseLine = 0, newLine = 0;
                foreach (var line in text.Split('\n'))
                {
                    if (HunkHeader().Match(line) is { Success: true } hunk)
                    {
                        baseLine = int.Parse(hunk.Groups[1].Value, CultureInfo.InvariantCulture);
                        newLine = int.Parse(hunk.Groups[2].Value, CultureInfo.InvariantCulture);
                    }
                    else if (line is ['+' or '-', ..] && !line.StartsWith("+++ ", StringComparison.Ordinal) && !line.StartsWith("--- ", StringComparison.Ordinal))
                    {
                        var added = line[0] == '+';
                        var normalized = NormalizeChangedLine(suffix, line[1..], added ? newNames : baseNames);
                        if (!net.TryGetValue(normalized, out var entry))
                            entry = (0, [], []);
                        var location = added ? newLocations.ElementAtOrDefault(newLine++ - 1) : baseLocations.ElementAtOrDefault(baseLine++ - 1);
                        if (location != null)
                            (added ? entry.Added : entry.Removed).Add(location);
                        net[normalized] = (entry.Count + (added ? 1 : -1), entry.Added, entry.Removed);
                    }
                    else if (line.StartsWith(' '))
                    {
                        baseLine++;
                        newLine++;
                    }
                }
                foreach (var (line, (count, added, removed)) in net.Where(x => x.Value.Count != 0))
                {
                    var key = (count > 0 ? "+" : "-") + line;
                    if (!permutationLines.TryGetValue(key, out var locations))
                        permutationLines.Add(key, locations = new HashSet<string>(StringComparer.Ordinal));
                    // A copy of the line found on both sides at the same source line only moved: its source line is not where
                    // the change comes from (unless it is the only one)
                    var (side, other) = count > 0 ? (added, removed) : (removed, added);
                    var changed = side.Except(other, StringComparer.Ordinal).ToList();
                    locations.UnionWith(changed.Count > 0 ? changed : side);
                }
            }
            foreach (var (line, locations) in permutationLines)
            {
                if (!lines.TryGetValue(line, out var entry))
                    lines.Add(line, entry = ([], new Dictionary<string, int>(StringComparer.Ordinal)));
                entry.Ids.Add(id);
                foreach (var location in locations)
                    entry.Locations[location] = entry.Locations.GetValueOrDefault(location) + 1;
            }
        }

        // The source line found in most permutations; its text from the copies of the .sdsl files each run keeps
        var sourceFiles = new Dictionary<string, string[]>(StringComparer.Ordinal);
        SourceLine? Source(string line, Dictionary<string, int> locations)
        {
            if (locations.Count == 0)
                return null;
            var location = locations.MaxBy(x => (x.Value, x.Key)).Key;
            var separator = location.LastIndexOf(':');
            var (file, number) = (location[..separator], int.Parse(location[(separator + 1)..], CultureInfo.InvariantCulture));
            var path = Path.Combine(line[0] == '+' ? outputDirectory : baseDirectory, SourcesDirectory, file);
            if (!sourceFiles.TryGetValue(path, out var fileLines))
                sourceFiles.Add(path, fileLines = File.Exists(path) ? File.ReadAllLines(path) : []);
            var code = fileLines.ElementAtOrDefault(number - 1)?.Trim() ?? "";
            const int maxCodeLength = 120;
            return new SourceLine(file, number, code.Length > maxCodeLength ? code[..maxCodeLength] + "..." : code, locations.Count - 1);
        }

        // A line and its replacement next to each other: by text, removed first
        return lines.Select(x => new ChangedLine(x.Key, x.Value.Ids, Source(x.Key, x.Value.Locations)))
            .OrderByDescending(x => x.Ids.Count).ThenBy(x => x.Line[1..], StringComparer.Ordinal).ThenBy(x => x.Line[0])
            .ToList();
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^@@ -(\d+)(?:,\d+)? \+(\d+)(?:,\d+)? @@")]
    private static partial System.Text.RegularExpressions.Regex HunkHeader();

    // Next to each .spvasm file, "<file>.lines": the .sdsl files of the module (relative to the repository), an empty line,
    // then for each line of the .spvasm file "<file index>:<line>" or nothing. The .sdsl files themselves are copied to
    // SourcesDirectory, so that a baseline kept in a cache still has the text of its lines.
    const string LocationsSuffix = ".lines";
    const string SourcesDirectory = "sdsl";

    static string LocationsText(IReadOnlyList<SourceLocation?> locations, ConcurrentDictionary<string, byte> usedFiles)
    {
        var files = new Dictionary<string, int?>(StringComparer.Ordinal);
        var fileList = new List<string>();
        var text = new StringBuilder();
        foreach (var location in locations)
        {
            if (location is { } l)
            {
                if (!files.TryGetValue(l.File, out var index))
                {
                    // Files outside the repository have no location
                    var relative = Path.GetRelativePath(ShaderCorpus.RepositoryRoot, l.File).Replace('\\', '/');
                    if (!relative.StartsWith("../", StringComparison.Ordinal) && !Path.IsPathRooted(relative))
                    {
                        index = fileList.Count;
                        fileList.Add(relative);
                        usedFiles.TryAdd(relative, 0);
                    }
                    files.Add(l.File, index);
                }
                if (index != null)
                    text.Append(CultureInfo.InvariantCulture, $"{index}:{l.Line}");
            }
            text.Append('\n');
        }
        return string.Concat(fileList.Select(x => x + "\n")) + "\n" + text;
    }

    // "<file>:<line>" per line of the .spvasm file (index 0 for line 1), null where none; empty for a baseline written
    // before locations were kept
    static string?[] ReadLocations(string path)
    {
        if (!File.Exists(path))
            return [];
        var lines = File.ReadAllLines(path);
        var separator = Array.IndexOf(lines, "");
        var files = lines[..separator];
        return lines[(separator + 1)..].Select(x => x.Split(':') is [var file, var line] ? $"{files[int.Parse(file, CultureInfo.InvariantCulture)]}:{line}" : null).ToArray();
    }

    // The name of each named ID of a .spvasm file: the disassembler shows a named ID after its name, and repeated names (the
    // same constant declared again, two locals of the same name) get "_1", "_2"... in order, so one more shifts the others
    static Dictionary<string, string> ReadIdNames(string path)
    {
        var names = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!path.EndsWith(".spvasm", StringComparison.Ordinal) || !File.Exists(path))
            return names;
        foreach (var line in File.ReadLines(path))
        {
            if (IdName().Match(line) is { Success: true } name)
                names.TryAdd(name.Groups[1].Value, name.Groups[2].Value);
        }
        return names;
    }

    static string NormalizeChangedLine(string suffix, string line, Dictionary<string, string> idNames)
    {
        line = line.Trim();
        if (suffix == ".reflection.txt")
            return ReflectionNumbers().Replace(ReflectionOffset().Replace(line, ""), "${1}#");
        // Names: the quoted name, not the ID it names, and a named ID as its name. Member indices and offsets shift after
        // a new member.
        line = NamedId().Replace(line, "OpName ");
        line = Id().Replace(line, x => idNames.TryGetValue(x.Value, out var name) ? "%" + name : x.Value);
        line = NumberedIdInName().Replace(line, "id_");
        line = MemberIndex().Replace(line, "$1 ");
        line = MemberOffset().Replace(line, "$1 #");
        return NumberedId().Replace(line, "%_");
    }

    // "  432     8 ShadowMapTextureSize => ...": offset and size
    [System.Text.RegularExpressions.GeneratedRegex(@"^\d+\s+\d+\s+")]
    private static partial System.Text.RegularExpressions.Regex ReflectionOffset();

    // "cbuffer PerView size 1632", "slot 3+1"
    [System.Text.RegularExpressions.GeneratedRegex(@"(size |slot )\d+(\+\d+)?")]
    private static partial System.Text.RegularExpressions.Regex ReflectionNumbers();

    [System.Text.RegularExpressions.GeneratedRegex(@"^OpName %\S+ ")]
    private static partial System.Text.RegularExpressions.Regex NamedId();

    // OpName %float_0_5_1 "float_0.5"
    [System.Text.RegularExpressions.GeneratedRegex(@"^OpName (%\S+) ""(.*)""\s*$")]
    private static partial System.Text.RegularExpressions.Regex IdName();

    [System.Text.RegularExpressions.GeneratedRegex(@"%\S+")]
    private static partial System.Text.RegularExpressions.Regex Id();

    // "OpMemberName %type_PerView 17 ...", "OpMemberDecorate %type_PerView 18 Offset 660"
    [System.Text.RegularExpressions.GeneratedRegex(@"^(OpMember(?:Name|Decorate) %\S+) \d+ ")]
    private static partial System.Text.RegularExpressions.Regex MemberIndex();

    [System.Text.RegularExpressions.GeneratedRegex(@"^(OpMemberDecorate %\S+ (?:Offset|MatrixStride)) \d+")]
    private static partial System.Text.RegularExpressions.Regex MemberOffset();

    [System.Text.RegularExpressions.GeneratedRegex(@"%\d+\b")]
    private static partial System.Text.RegularExpressions.Regex NumberedId();

    // A name the disassembler builds out of an unnamed ID: %fn_void_ptr_Function_id133
    [System.Text.RegularExpressions.GeneratedRegex(@"(?<=_)id\d+(?=_|\s|$)")]
    private static partial System.Text.RegularExpressions.Regex NumberedIdInName();

    static bool FileDiffers(string baseDirectory, string outputDirectory, string fileName)
    {
        var before = Path.Combine(baseDirectory, fileName);
        var after = Path.Combine(outputDirectory, fileName);
        if (File.Exists(before) != File.Exists(after))
            return true;
        return File.Exists(before) && File.ReadAllText(before) != File.ReadAllText(after);
    }

    // The optimizer keeps the order of the declarations before the functions (types, constants, variables), which follows
    // the order the mixer merged shaders in. Types and constants are named after their content, so they compare as a set.
    static bool SameOptimizedCode(string baseDirectory, string outputDirectory, string id)
    {
        static string Canonical(string path)
        {
            var lines = File.ReadAllLines(path);
            var firstFunction = Array.FindIndex(lines, x => x.Contains(" = OpFunction ", StringComparison.Ordinal));
            if (firstFunction == -1)
                firstFunction = lines.Length;
            return string.Join('\n', lines[..firstFunction].Order(StringComparer.Ordinal).Concat(lines[firstFunction..]));
        }
        var after = Path.Combine(outputDirectory, id + OptimizedSuffix);
        return File.Exists(after) && Canonical(Path.Combine(baseDirectory, id + OptimizedSuffix)) == Canonical(after);
    }

    // Unified diff of each file of a permutation that changed, through git (always there for a repository checkout)
    static List<(string Suffix, string Text)> FileDiffs(string baseDirectory, string outputDirectory, string id)
    {
        var diffs = new List<(string, string)>();
        foreach (var suffix in DiffSuffixes)
        {
            if (!FileDiffers(baseDirectory, outputDirectory, id + suffix))
                continue;
            var before = Path.Combine(baseDirectory, id + suffix);
            var after = Path.Combine(outputDirectory, id + suffix);
            var text = new StringBuilder(GitDiff(File.Exists(before) ? before : "/dev/null", File.Exists(after) ? after : "/dev/null"));

            // Short headers: "base/<file>" and "new/<file>" instead of two absolute folders (git quotes and doubles backslashes)
            foreach (var (directory, label) in new[] { (baseDirectory, "base"), (outputDirectory, "new") })
            {
                text.Replace(directory.Replace("\\", "\\\\") + "\\\\", label + "/");
                text.Replace(directory.Replace('\\', '/') + "/", label + "/");
                text.Replace(directory + Path.DirectorySeparatorChar, label + "/");
            }
            diffs.Add((suffix, text.ToString()));
        }
        return diffs;
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
