using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

if (args.Length < 1)
{
    Console.Error.WriteLine("usage: ParallelBuildAuditor <build.binlog>");
    return 2;
}

var binlogPath = args[0];
if (!File.Exists(binlogPath))
{
    Console.Error.WriteLine($"binlog not found: {binlogPath}");
    return 2;
}

string[] legitAxes =
[
    "TargetFramework",
    "Configuration",
    "Platform",
    "RuntimeIdentifier",
    "StrideGraphicsApi",
];

Console.WriteLine($"reading {binlogPath}...");

var groups = new Dictionary<string, List<(string FullSig, IReadOnlyDictionary<string, string> Globals, string Parent, string Targets)>>(StringComparer.OrdinalIgnoreCase);
var contextToProject = new Dictionary<int, string>();
var observed = 0;

var src = new BinaryLogReplayEventSource();
src.AnyEventRaised += (_, e) =>
{
    if (e is not ProjectStartedEventArgs ps) return;

    var globals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (ps.GlobalProperties != null)
    {
        foreach (var kv in ps.GlobalProperties)
            globals[kv.Key] = kv.Value ?? "";
    }

    // Skip Restore-phase events.
    if (globals.TryGetValue("MSBuildIsRestoring", out var restoring) && restoring == "true") return;
    if (ps.TargetNames != null && ps.TargetNames.Contains("Restore", StringComparison.OrdinalIgnoreCase)) return;

    // Skip cross-project-reference probing (GetTargetFrameworks, GetTargetPath, etc.).
    // MSBuild invokes these with BuildProjectReferences=false to ask the referenced
    // project for its TFM list, output path, etc. before dispatching the real build.
    // Different globals here are normal, not a parallel-build symptom.
    if (globals.TryGetValue("BuildProjectReferences", out var bpr) && bpr == "false") return;
    string[] probingTargets =
    [
        "GetTargetFrameworks", "GetTargetFrameworksWithPlatformForSingleTargetFramework",
        "GetTargetPath", "GetTargetPathWithTargetPlatformMoniker",
        "GetNativeManifest", "GetCopyToOutputDirectoryItems",
        "_GetProjectReferenceTargetFrameworkProperties",
        "GetAllRuntimeIdentifiers",
        "_StrideQueryGraphicsApis",
    ];
    if (!string.IsNullOrEmpty(ps.TargetNames))
    {
        var requested = ps.TargetNames.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (requested.All(t => probingTargets.Contains(t, StringComparer.OrdinalIgnoreCase))) return;
    }

    // Strip noise injected by the SLN/CLI driver, not by the project graph itself.
    string[] noiseKeys =
    [
        "MSBuildIsRestoring", "MSBuildRestoreSessionId", "ExcludeRestorePackageImports",
        "_RestoreSolutionFileUsed",
        "SolutionDir", "SolutionExt", "SolutionFileName", "SolutionName", "SolutionPath",
        "EnableDefaultCompileItems", "EnableDefaultEmbeddedResourceItems", "EnableDefaultNoneItems",
        "NuGetInteractive",
        "BuildProjectReferences", "TargetFrameworks",
    ];
    foreach (var k in noiseKeys) globals.Remove(k);

    observed++;

    var legitSig = string.Join(';', legitAxes.Select(a => $"{a}={(globals.TryGetValue(a, out var v) ? v : "")}"));
    var fullSig = string.Join(';', globals.OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => $"{kv.Key}={kv.Value}"));
    var key = $"{ps.ProjectFile}|{legitSig}";

    var parentCtx = ps.ParentProjectBuildEventContext?.ProjectContextId ?? -1;
    var parent = parentCtx >= 0 && contextToProject.TryGetValue(parentCtx, out var pp) ? pp : "<root>";
    if (ps.BuildEventContext != null)
    {
        contextToProject[ps.BuildEventContext.ProjectContextId] = ps.ProjectFile ?? "";
    }

    if (!groups.TryGetValue(key, out var list))
    {
        list = new List<(string, IReadOnlyDictionary<string, string>, string, string)>();
        groups[key] = list;
    }
    if (!list.Any(x => x.FullSig == fullSig))
    {
        list.Add((fullSig, globals, parent, ps.TargetNames ?? ""));
    }
};

src.Replay(binlogPath);

var dupes = groups
    .Where(kv => kv.Value.Count > 1)
    .OrderByDescending(kv => kv.Value.Count)
    .ToList();

Console.WriteLine($"projects observed: {observed}");
Console.WriteLine($"groups (project + legit-axes): {groups.Count}");
Console.WriteLine($"groups with leaked-property duplicates: {dupes.Count}");
Console.WriteLine();

if (Environment.GetEnvironmentVariable("PARALLEL_AUDIT_VERBOSE") == "1")
{
    Console.WriteLine("--- per-group breakdown (first 20):");
    foreach (var (key, list) in groups.Take(20))
    {
        Console.WriteLine($"  {key}");
        Console.WriteLine($"    globals keys: {string.Join(",", list[0].Globals.Keys.OrderBy(x => x, StringComparer.Ordinal))}");
    }
    Console.WriteLine();
}

foreach (var (key, list) in dupes)
{
    var parts = key.Split('|', 2);
    var projectFile = parts[0];
    var legitSig = parts.Length > 1 ? parts[1] : "";

    Console.WriteLine($"=== {projectFile}");
    Console.WriteLine($"    legit axes: {legitSig}");
    Console.WriteLine($"    distinct global-property sets: {list.Count}");
    for (int i = 0; i < list.Count; i++)
    {
        Console.WriteLine($"    #{i + 1} parent: {list[i].Parent} (targets: {(string.IsNullOrEmpty(list[i].Targets) ? "<default>" : list[i].Targets)})");
    }

    var allKeys = list.SelectMany(e => e.Globals.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    var differing = allKeys
        .Where(k => list.Select(e => e.Globals.TryGetValue(k, out var v) ? v : "<unset>").Distinct().Count() > 1)
        .Except(legitAxes, StringComparer.OrdinalIgnoreCase)
        .OrderBy(x => x, StringComparer.Ordinal)
        .ToList();

    if (differing.Count == 0)
    {
        Console.WriteLine($"    (no differing non-legit properties — likely an axis we should add)");
    }
    else
    {
        Console.WriteLine($"    differing properties:");
        foreach (var dk in differing)
        {
            var values = list
                .Select((e, i) =>
                {
                    var v = e.Globals.TryGetValue(dk, out var raw) ? raw : "<unset>";
                    if (v.Length > 60) v = v[..60].Replace('\n', ' ').Replace('\r', ' ') + "…";
                    return $"#{i + 1}={v}";
                })
                .ToList();
            Console.WriteLine($"      {dk}: {string.Join(", ", values)}");
        }
    }
    Console.WriteLine();
}

return dupes.Count > 0 ? 1 : 0;
