// Dev-redirect NuGet stub generator for Stride.
// Usage: dotnet run build/GenerateDevPackages.cs -- [options]
//
// Generates stub .nupkg files that redirect to dev-built DLLs (and natives, and analyzers),
// eliminating the ~50s NuGet packing overhead on every incremental build.
//
// Two ways in:
//   - A full run (by hand, from the repo root): packs the solution (no build; the outputs must exist), then for
//     each nupkg strips what the redirect makes dead weight, injects build/<PkgId>.props/.targets, deploys to
//     NugetDev + mirrors into bin/packages, invalidates the NuGet cache, and writes the stamp (what was
//     deployed), the stubs file (per stub, the fingerprint of the fixed inputs it was made with) and the inputs
//     manifest (content hashes of the csprojs and build/ assets). Never required: the build refreshes stubs
//     one by one (below); a full run is just faster for the whole set, e.g. on a fresh checkout.
//   - --adopt <nupkg> (from Stride.DevPackages.targets, right after a project's Build packed it in-process):
//     the same processing for that one package, plus its lines in the stamp and manifest. Its output is only
//     shown by the build from normal verbosity on (the build prints its own one-liner at minimal).

using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// --- Parse arguments ---
var strideRoot = "";
var configuration = "Debug";
var solution = "";
var version = "";
var disable = false;
var adopt = ""; // --adopt <nupkg>: a build just packed this one project; stub and deploy only it
var nugetDevDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "nugetdev");

for (int i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--stride-root" when i + 1 < args.Length: strideRoot = args[++i]; break;
        case "--configuration" when i + 1 < args.Length: configuration = args[++i]; break;
        case "--solution" when i + 1 < args.Length: solution = args[++i]; break;
        case "--version" when i + 1 < args.Length: version = args[++i]; break;
        case "--nuget-dev" when i + 1 < args.Length: nugetDevDir = args[++i]; break;
        case "--disable": disable = true; break;
        case "--adopt" when i + 1 < args.Length: adopt = Path.GetFullPath(args[++i]); break;
    }
}

// --- Resolve defaults ---
// The build passes --stride-root; by hand, run from the repo root. (A file-based app is compiled into a temp
// folder, so its own location says nothing about the repo.)
if (string.IsNullOrEmpty(strideRoot))
    strideRoot = Directory.GetCurrentDirectory();

if (string.IsNullOrEmpty(solution))
{
    solution = Directory.GetFiles(Path.Combine(strideRoot, "build"), "Stride.slnx").FirstOrDefault()
        ?? Path.Combine(strideRoot, "build", "Stride.slnx");
}

if (string.IsNullOrEmpty(version))
{
    // Package versions use the committed MajorMinor.Patch (see StrideVersionTasks.cs). The -devN suffix comes from
    // the generated overlay when present.
    var generatedFile = Path.Combine(strideRoot, "sources", "shared", "SharedAssemblyInfo.Generated.cs");
    var plainFile = Path.Combine(strideRoot, "sources", "shared", "SharedAssemblyInfo.cs");
    var plainText = File.ReadAllText(plainFile);
    var mmMatch = Regex.Match(plainText, @"MajorMinor\s*=\s*""([^""]+)""");
    var patchMatch = Regex.Match(plainText, @"\bPatch\s*=\s*""([^""]+)""");
    var suffixMatch = Regex.Match(File.ReadAllText(File.Exists(generatedFile) ? generatedFile : plainFile), @"NuGetVersionSuffix\s*=\s*""([^""]*)""");
    if (!mmMatch.Success || !patchMatch.Success) throw new Exception("Could not determine version from SharedAssemblyInfo");
    version = mmMatch.Groups[1].Value + "." + patchMatch.Groups[1].Value + (suffixMatch.Success ? suffixMatch.Groups[1].Value : "");
}

if (adopt.Length == 0) // an adopt runs inside a build: its one line is printed by the build itself
{
    Console.WriteLine($"Stride version: {version}");
    Console.WriteLine($"Dev root: {strideRoot}");
    Console.WriteLine($"Configuration: {configuration}");
    Console.WriteLine($"Solution: {solution}");
    Console.WriteLine($"NugetDev: {nugetDevDir}");
}

// --- Toggle path (--disable): flip the flag in Stride.Local.props and exit.
// Stub cleanup runs as a side-effect of the next build (_StrideCleanDevPackages target
// reads the manifest stamp and deletes only the stubs the script generated). ---
if (disable)
{
    var changed = SetDevPackagesFlag(strideRoot, enable: false);
    // Clean up now rather than on the next build: with the flag off nothing declares the stubs as outputs, so
    // Visual Studio's fast up-to-date check may not run MSBuild (and its cleanup target) for a while. That
    // target stays as the safety net for a flag flipped by hand.
    var stamp = Path.Combine(nugetDevDir, $".devpackages-{version}");
    var removed = 0;
    if (File.Exists(stamp))
    {
        foreach (var name in File.ReadAllLines(stamp).Where(l => !string.IsNullOrWhiteSpace(l)))
            foreach (var dir in new[] { nugetDevDir, Path.Combine(strideRoot, "bin", "packages") })
                if (File.Exists(Path.Combine(dir, name))) { File.Delete(Path.Combine(dir, name)); removed++; }
        foreach (var side in new[] { "", ".inputs", ".stubs" })
            if (File.Exists(stamp + side)) File.Delete(stamp + side);
    }
    Console.WriteLine(changed ? "Disabled StrideDevPackages in build/Stride.Local.props." : "StrideDevPackages was already off.");
    Console.WriteLine($"Removed {removed} stub package file(s); the next build packs real packages again.");
    return 0;
}

// --- Step 0: The project map, the previous stamp, the inputs ---
// A build that changed a project adopts it by itself (--adopt, run by Stride.DevPackages.targets after that
// project's Build), so a manual run is always a full run: needed on a fresh checkout and when the script, the
// central package versions or the solution changed (every stub depends on those).
var projectMap = BuildProjectMap(solution, strideRoot);
Console.WriteLine($"Found {projectMap.Count} project mappings");
var projects = projectMap.Values.Distinct().ToList(); // the map lists a project under its package id and its assembly name
var stampPath = Path.Combine(nugetDevDir, $".devpackages-{version}");
var fixedInputs = new[]
{
    Path.Combine(strideRoot, "build", "GenerateDevPackages.cs"),
    Path.Combine(strideRoot, "sources", "Directory.Packages.props"),
    solution,
};
var previousStamp = File.Exists(stampPath) ? File.ReadAllLines(stampPath).Where(l => !string.IsNullOrWhiteSpace(l)).ToList() : new List<string>();
// Every stub depends on the fixed inputs; each stub records the fingerprint it was generated with (the .stubs
// file), and a project's build compares its own stub's against the current one (StrideCheckDevPackageInputs).
var fixedFingerprint = Fingerprint(fixedInputs);
string StubName(ProjectInfo p) => $"{p.PackageId}.{version}.nupkg";


// Stubs of projects that left the solution go (full runs only; an adopt touches one project).
var removedStubs = adopt.Length > 0 ? new List<string>() : previousStamp
    .Where(name => !name.StartsWith("Stride.Templates.", StringComparison.OrdinalIgnoreCase)
                   && !projects.Any(p => string.Equals(StubName(p), name, StringComparison.OrdinalIgnoreCase)))
    .ToList();

// --- Step 1: Pack fresh nupkgs (a full run; an --adopt was handed its nupkg by the build) ---
var tempPackDir = Path.Combine(Path.GetTempPath(), "stride-devpackages-pack");
if (adopt.Length == 0)
{
    if (Directory.Exists(tempPackDir)) Directory.Delete(tempPackDir, true);
    Directory.CreateDirectory(tempPackDir);
    Console.WriteLine("\nPacking fresh packages...");
    var packExitCode = PackSolution();
    if (packExitCode != 0)
    {
        Console.Error.WriteLine($"ERROR: dotnet pack failed with exit code {packExitCode}; stubs not regenerated (feeds keep the previous state).");
        return packExitCode;
    }
}

// StrideSkipAutoPack=true: disables Sdk.targets' GeneratePackageOnBuild=true. We're explicitly
// invoking Pack via dotnet pack, so the on-build auto-pack would create a Pack->_PackAsBuildAfterTarget
// ->GenerateNuspec->Pack circular dependency and fail every engine project.
// StrideDevPackages=false: forces engine projects through normal build (not the dev-redirect path)
// regardless of the caller's Stride.Local.props.
// StridePackAssets=false: skip the asset/.sdpkg packing step. The dev-redirect consumes assets +
// shader source straight from the checkout (NugetStore.GetRealPath -> StrideDevProjectDirectory),
// so packed asset content would be dead weight; skipping it also drops the slow per-package copy.
// StrideDevPackagesGenerating=true: StrideDevPackages=false would otherwise trip the flag-off cleanup target
// (_StrideCleanDevPackages) inside this very build and wipe the current stubs before we know the pack is
// complete. With it, the previous stubs stay until step 3 overwrites them, so a failed run leaves a working feed.
// Output -> tempPackDir (not NugetDev); we deploy stubs there explicitly in step 3.
// Packed without building first: nothing in a stub depends on an up-to-date binary (the DLL and natives are
// redirected placeholders, the nuspec is metadata, the build assets are files), only on the output existing. A
// missing output (fresh clone, new project) fails that pack, so the pack is retried with a build.
int PackSolution()
{
    var packProperties = $"-c {configuration} -p:StrideSkipAutoPack=true -p:StrideDevPackages=false -p:StrideDevPackagesGenerating=true -p:StridePackAssets=false -o \"{tempPackDir}\" --verbosity normal";
    // --no-build implies no restore, so restore explicitly first.
    var exitCode = RunProcess("dotnet", $"restore \"{solution}\" -p:StrideSkipAutoPack=true -p:StrideDevPackages=false --verbosity quiet", silent: true);
    if (exitCode == 0)
        exitCode = RunProcess("dotnet", $"pack \"{solution}\" --no-build {packProperties}", silent: true, onLine: OnPackLine);
    if (exitCode == 0)
        return 0;
    Console.WriteLine("  Some output is missing or the restore failed; building + packing instead...");
    Directory.Delete(tempPackDir, true);
    Directory.CreateDirectory(tempPackDir);
    return RunProcess("dotnet", $"pack \"{solution}\" {packProperties}", silent: true, onLine: OnPackLine);
}
void OnPackLine(string line)
{
    // "Successfully created package 'X.nupkg'." is emitted once per project at pack completion.
    var packMarker = "Successfully created package '";
    var packIdx = line.IndexOf(packMarker);
    if (packIdx >= 0)
    {
        var rest = line[(packIdx + packMarker.Length)..];
        var endQuote = rest.IndexOf('\'');
        if (endQuote >= 0)
            Console.WriteLine($"  packed  {Path.GetFileNameWithoutExtension(rest[..endQuote])}");
        return;
    }
    // "  ProjectName -> path\to\bin\...\ProjectName.dll" emitted once per project at build completion.
    var arrowIdx = line.IndexOf(" -> ");
    if (arrowIdx > 0 && line.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine($"  built   {line[..arrowIdx].Trim()}");
        return;
    }
    if (line.TrimStart().StartsWith("Determining projects to restore"))
        Console.WriteLine("  restore phase...");
    if (line.Contains(": error "))
        Console.WriteLine($"  {line.Trim()}");
}
var freshPackages = adopt.Length > 0 ? new[] { adopt } : Directory.GetFiles(tempPackDir, $"*.{version}.nupkg");

// First run on a fresh worktree: the -devN suffix doesn't exist until the pack itself assigns
// it (StrideEnsureWorktreeVersion writes the ledger + overlay), so the up-front derivation can
// be wrong. The pack output is the truth — read the version off Stride.Core's nupkg.
if (adopt.Length == 0 && freshPackages.Length == 0)
{
    var coreVersion = Directory.GetFiles(tempPackDir, "Stride.Core.*.nupkg")
        .Select(f => Regex.Match(Path.GetFileName(f), @"^Stride\.Core\.(\d.*)\.nupkg$"))
        .FirstOrDefault(m => m.Success)?.Groups[1].Value;
    if (coreVersion != null && coreVersion != version)
    {
        version = coreVersion;
        Console.WriteLine($"Version resolved from pack output: {version}");
        freshPackages = Directory.GetFiles(tempPackDir, $"*.{version}.nupkg");
    }
}
if (adopt.Length == 0)
    Console.WriteLine($"Packed {freshPackages.Length} packages");

if (freshPackages.Length == 0)
{
    Console.Error.WriteLine($"ERROR: No packages found for version {version}");
    return 1;
}

// --- Step 2: refuse a short pack ---
// A partial pack (a concurrent build holding files, a project that failed to pack) used to be deployed as if
// complete: the stamp was rewritten with the few stubs produced, the rest were gone, and every consumer silently
// fell back to whatever stale package the NuGet cache still held. Every packed project that had a stub before
// must have produced one now, else stop and leave the feed as it was.
var fresh = freshPackages.Select(Path.GetFileName).ToHashSet(StringComparer.OrdinalIgnoreCase);
var missing = (adopt.Length > 0 ? new List<ProjectInfo>() : projects)
    .Select(StubName)
    .Where(name => previousStamp.Contains(name, StringComparer.OrdinalIgnoreCase) && !fresh.Contains(name))
    .ToList();
if (missing.Count > 0)
{
    Console.Error.WriteLine($"ERROR: the pack produced {fresh.Count} package(s) but {missing.Count} previously deployed stub(s) are missing; stubs not regenerated (feeds keep the previous state):");
    foreach (var name in missing)
        Console.Error.WriteLine($"  {name}");
    return 1;
}

// --- Step 3: Process each package ---
Directory.CreateDirectory(nugetDevDir);
var stubCount = 0;
var skipCount = 0;
var failures = 0;
var generatedStubs = new List<string>();
ProjectInfo? adoptedProject = null; // --adopt: the one project whose manifest lines get refreshed
var nugetPackagesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

foreach (var pkgPath in freshPackages)
{
    var pkgFileName = Path.GetFileName(pkgPath);
    var pkgId = Regex.Replace(pkgFileName, $@"\.{Regex.Escape(version)}\.nupkg$", "", RegexOptions.IgnoreCase);

    // Content-only packages — nothing to redirect; deployed as-is below.
    if (pkgId.StartsWith("Stride.Templates.", StringComparison.OrdinalIgnoreCase))
        continue;

    if (!projectMap.TryGetValue(pkgId, out var projInfo))
    {
        Console.WriteLine($"  SKIP {pkgId} (no matching project)");
        skipCount++;
        continue;
    }

    Console.Write($"  {pkgId}...");

    try
    {
        ProcessPackage(pkgPath, pkgId, projInfo, projectMap, nugetDevDir, nugetPackagesDir, version, strideRoot, configuration);
        generatedStubs.Add(pkgFileName);
        adoptedProject = projInfo;
        stubCount++;
        Console.WriteLine(" OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" ERROR: {ex.Message}");
        failures++;
    }
}

// Templates are real content packages: no DLLs to redirect, so no stub injection — but our pack
// ran with StrideSkipAutoPack=true, which also suppressed their own auto-pack-deploy, so deploy
// the fresh nupkgs here. Globbed separately: content-versioned ones (Samples, Starters) can carry
// a different version than the engine. Listed in the manifest so cleanup/pruning covers them;
// after a flag-off their auto-pack (independent of StrideDevPackages) repopulates the feeds.
var freshTemplates = adopt.Length > 0
    ? freshPackages.Where(p => Path.GetFileName(p).StartsWith("Stride.Templates.", StringComparison.OrdinalIgnoreCase))
    : Directory.GetFiles(tempPackDir, "Stride.Templates.*.nupkg");
foreach (var tplPath in freshTemplates)
{
    var tplName = Path.GetFileName(tplPath);
    var tplMatch = Regex.Match(tplName, @"^(?<id>.+?)\.(?<ver>\d.*)\.nupkg$");
    if (!tplMatch.Success) continue;

    Console.Write($"  {tplMatch.Groups["id"].Value}...");
    File.Copy(tplPath, Path.Combine(nugetDevDir, tplName), overwrite: true);
    InvalidateNuGetCache(nugetPackagesDir, tplMatch.Groups["id"].Value, tplMatch.Groups["ver"].Value);
    generatedStubs.Add(tplName);
    Console.WriteLine(" OK (deployed as-is)");
}

// --- Step 4: Write the stamp, the stubs file and the inputs manifest.
// Stamp: newline list of deployed nupkg filenames (stubs + as-is templates); the cleanup target reads it to
// delete only what we deployed.
// Stubs: "<nupkg>\t<fingerprint>" per stub, the fingerprint of the fixed inputs (this script, the central package
// versions, the solution) it was generated with; a project's build compares its own stub's with the current one.
// Inputs: the scanned csprojs and each project's build/ & buildTransitive/ *.targets|props (packed into the
// nupkg). One line per file, "<sha256>\t<path>": the build check uses the file mtime only as a fast prefilter
// and confirms a newer file by content hash, so a touch, a checkout or a rebase that leaves the content
// unchanged changes nothing. Line endings are normalized before hashing (autocrlf must not count as a change).
// A full run rewrites all three from scratch. An adopt adds or replaces its own lines, under a process-wide
// mutex: several projects of one build may adopt at the same time. ---
var binPackagesDir = Path.Combine(strideRoot, "bin", "packages");
string ManifestLine(string path) => ContentHash(path) + "\t" + Path.GetFullPath(path);
string StubLine(string name) => name + "\t" + fixedFingerprint;
using (var mutex = new Mutex(false, @"Global\StrideDevPackages"))
{
    try { mutex.WaitOne(); } catch (AbandonedMutexException) { /* the holder died; the files are whole lines, so proceed */ }
    try
    {
        if (adopt.Length == 0)
        {
            foreach (var name in removedStubs)
            {
                foreach (var dir in new[] { nugetDevDir, binPackagesDir })
                    if (File.Exists(Path.Combine(dir, name)))
                        File.Delete(Path.Combine(dir, name));
                Console.WriteLine($"  removed {name} (project no longer in the solution)");
            }
            File.WriteAllLines(stampPath, generatedStubs);
            File.WriteAllLines(stampPath + ".stubs", generatedStubs.Select(StubLine));
            File.WriteAllLines(stampPath + ".inputs",
                projects.Select(p => p.CsprojPath).Concat(projects.SelectMany(BuildAssets)).Distinct().Select(ManifestLine));
        }
        else
        {
            IEnumerable<string> Lines(string path) => File.Exists(path) ? File.ReadAllLines(path).Where(l => !string.IsNullOrWhiteSpace(l)) : Enumerable.Empty<string>();
            bool Mine(string line) => generatedStubs.Contains(line.Split('\t', 2)[0], StringComparer.OrdinalIgnoreCase);
            File.WriteAllLines(stampPath, Lines(stampPath).Union(generatedStubs, StringComparer.OrdinalIgnoreCase));
            File.WriteAllLines(stampPath + ".stubs", Lines(stampPath + ".stubs").Where(l => !Mine(l)).Concat(generatedStubs.Select(StubLine)));
            if (adoptedProject != null)
                File.WriteAllLines(stampPath + ".inputs",
                    Lines(stampPath + ".inputs").Where(l => !l.Contains('\t') || !UnderDir(l.Split('\t', 2)[1], adoptedProject.ProjectDir))
                        .Concat(new[] { adoptedProject.CsprojPath }.Concat(BuildAssets(adoptedProject)).Select(ManifestLine)));
        }
    }
    finally
    {
        mutex.ReleaseMutex();
    }
}

// Also mirror the stubs into bin/packages (not refreshed while auto-pack is skipped) so the
// repo nuget.config's stride-local mapping keeps resolving; the flag-off cleanup removes them.
Directory.CreateDirectory(binPackagesDir);
foreach (var stubName in generatedStubs)
    File.Copy(Path.Combine(nugetDevDir, stubName), Path.Combine(binPackagesDir, stubName), overwrite: true);
Console.WriteLine($"Mirrored {generatedStubs.Count} stub package(s) into bin/packages");

// Prune this worktree's superseded versions from NugetDev (shared across worktrees, so it grows
// on every version bump otherwise). The -devN suffix identifies the owner: any older stamp with
// the same suffix belongs to this worktree; its manifest lists exactly what was deployed, so
// delete those files (and their bin/packages mirrors) plus the stamp. Other suffixes untouched.
var suffixIdx = version.IndexOf('-');
if (adopt.Length == 0 && suffixIdx >= 0)
{
    var suffix = version[suffixIdx..];
    foreach (var oldStamp in Directory.GetFiles(nugetDevDir, ".devpackages-*"))
    {
        var stampName = Path.GetFileName(oldStamp);
        if (stampName.EndsWith(".inputs")) continue;
        var oldVersion = stampName[".devpackages-".Length..];
        if (oldVersion == version || !oldVersion.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;

        var removed = 0;
        foreach (var pkgName in File.ReadAllLines(oldStamp).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            foreach (var dir in new[] { nugetDevDir, binPackagesDir })
            {
                var stale = Path.Combine(dir, pkgName);
                if (File.Exists(stale)) { File.Delete(stale); removed++; }
            }
        }
        File.Delete(oldStamp);
        foreach (var side in new[] { ".inputs", ".stubs" })
            if (File.Exists(oldStamp + side)) File.Delete(oldStamp + side);
        Console.WriteLine($"Pruned superseded {oldVersion}: {removed} package file(s)");
    }
}

// Cleanup temp (an adopt used none)
if (adopt.Length == 0)
    try { Directory.Delete(tempPackDir, true); } catch { }

// --- Step 5: Auto-enable StrideDevPackages in build/Stride.Local.props.
// Bootstraps the file from its template if missing (mirrors _StrideBootstrapLocalProps in
// the Stride SDK). Subsequent builds will read the flag and skip auto-pack. ---
var flagChanged = adopt.Length == 0 && SetDevPackagesFlag(strideRoot, enable: true);

if (failures > 0)
{
    // The stamp was written without the failed ones: an incremental run kept their previous stubs, a full run
    // left them out, so the build-side "stub missing" check names them on the next build.
    Console.Error.WriteLine($"ERROR: {failures} stub(s) could not be generated (see above).");
    return 1;
}

Console.WriteLine($"\nDone! Generated {stubCount} stubs, skipped {skipCount}.");
if (flagChanged)
    Console.WriteLine("Enabled StrideDevPackages in build/Stride.Local.props.");
return 0;

// ============================================================
// Helper methods
// ============================================================

static bool SetDevPackagesFlag(string strideRoot, bool enable)
{
    var localPropsPath = Path.Combine(strideRoot, "build", "Stride.Local.props");
    var templatePath = Path.Combine(strideRoot, "sources", "sdk", "Stride.Build.Sdk", "Sdk", "Stride.Local.props.template");

    // Bootstrap from template if the local props file doesn't exist yet. Mirrors what
    // _StrideBootstrapLocalProps does on the next build anyway, just earlier so we have
    // a file to edit right now.
    if (!File.Exists(localPropsPath) && File.Exists(templatePath))
        File.Copy(templatePath, localPropsPath);

    if (!File.Exists(localPropsPath))
        return false;

    var doc = XDocument.Load(localPropsPath, LoadOptions.PreserveWhitespace);
    var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
    var desired = enable ? "true" : "false";

    var element = doc.Descendants(ns + "StrideDevPackages").FirstOrDefault();
    if (element != null)
    {
        if (element.Value == desired)
            return false;
        element.Value = desired;
    }
    else
    {
        // Insert into the first non-conditional PropertyGroup with the existence-check Condition
        // pattern that matches the rest of the template — keeps -p:StrideDevPackages=... wins.
        var propertyGroup = doc.Descendants(ns + "PropertyGroup")
            .FirstOrDefault(pg => pg.Attribute("Condition") == null);
        if (propertyGroup == null)
            return false;
        propertyGroup.Add(new XElement(ns + "StrideDevPackages",
            new XAttribute("Condition", "'$(StrideDevPackages)' == ''"),
            desired));
    }

    doc.Save(localPropsPath);
    return true;
}

// Is <path> inside <dir>? Separator-terminated so a sibling whose name merely extends dir's (Stride.Core vs
// Stride.Core.Design) does not match.
static bool UnderDir(string path, string dir)
    => path.StartsWith(dir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);

// The build/ assets a project packs (its *.props/*.targets under build/, buildTransitive/, buildMultiTargeting/):
// inputs of its stub alongside the csproj.
static IEnumerable<string> BuildAssets(ProjectInfo project)
    => new[] { "build", "buildTransitive", "buildMultiTargeting" }
        .Select(sub => Path.Combine(project.ProjectDir, sub))
        .Where(Directory.Exists)
        .SelectMany(dir => Directory.EnumerateFiles(dir, "*.targets", SearchOption.AllDirectories)
            .Concat(Directory.EnumerateFiles(dir, "*.props", SearchOption.AllDirectories)));

// SHA-256 of a file's content with CRLF folded to LF, lowercase hex. Mirrored by StrideCheckDevPackageInputs
// (sources/targets/StrideDevPackagesTasks.cs); keep the two in step.
static string ContentHash(string path)
{
    var bytes = File.ReadAllBytes(path);
    var normalized = new byte[bytes.Length];
    var length = 0;
    for (var i = 0; i < bytes.Length; i++)
    {
        if (bytes[i] == (byte)'\r' && i + 1 < bytes.Length && bytes[i + 1] == (byte)'\n')
            continue;
        normalized[length++] = bytes[i];
    }
    return Convert.ToHexString(SHA256.HashData(normalized.AsSpan(0, length))).ToLowerInvariant();
}

// The fingerprint of the fixed inputs every stub depends on: their content hashes joined by '\n', hashed again.
// Mirrored by StrideCheckDevPackageInputs; keep the two in step.
static string Fingerprint(IEnumerable<string> paths)
    => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", paths.Select(p => File.Exists(p) ? ContentHash(p) : "missing"))))).ToLowerInvariant();

static int RunProcess(string fileName, string arguments, bool silent = false, Action<string>? onLine = null)
{
    var psi = new ProcessStartInfo(fileName, arguments)
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
    };
    var proc = Process.Start(psi)!;
    var stdoutLines = new List<string>();
    var stderr = "";
    var readStdout = Task.Run(() =>
    {
        string? line;
        while ((line = proc.StandardOutput.ReadLine()) != null)
        {
            stdoutLines.Add(line);
            onLine?.Invoke(line);
        }
    });
    var readStderr = Task.Run(() => stderr = proc.StandardError.ReadToEnd());
    proc.WaitForExit();
    Task.WaitAll(readStdout, readStderr);
    if (proc.ExitCode != 0 && !silent)
    {
        if (stdoutLines.Count > 0) Console.WriteLine(string.Join('\n', stdoutLines));
        if (!string.IsNullOrWhiteSpace(stderr)) Console.Error.WriteLine(stderr);
    }
    return proc.ExitCode;
}

static Dictionary<string, ProjectInfo> BuildProjectMap(string solution, string strideRoot)
{
    var map = new Dictionary<string, ProjectInfo>(StringComparer.OrdinalIgnoreCase);
    var slnDir = Path.GetDirectoryName(solution)!;

    var psi = new ProcessStartInfo("dotnet", $"sln \"{solution}\" list")
    {
        UseShellExecute = false,
        RedirectStandardOutput = true,
    };
    var proc = Process.Start(psi)!;
    var output = proc.StandardOutput.ReadToEnd();
    proc.WaitForExit();

    foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
    {
        var trimmed = line.Trim();
        if (!trimmed.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)) continue;

        var csprojPath = Path.GetFullPath(Path.Combine(slnDir, trimmed));
        if (!File.Exists(csprojPath)) continue;

        var content = File.ReadAllText(csprojPath);
        var projName = Path.GetFileNameWithoutExtension(csprojPath);
        var projDir = Path.GetDirectoryName(csprojPath)!;

        var pkgId = projName;
        var m = Regex.Match(content, @"<PackageId>([^<]+)</PackageId>");
        if (m.Success) pkgId = m.Groups[1].Value;

        var asmName = projName;
        m = Regex.Match(content, @"<AssemblyName>([^<]+)</AssemblyName>");
        if (m.Success) asmName = m.Groups[1].Value;

        var isGraphicsDependent = Regex.IsMatch(content, @"<StrideGraphicsApiDependent\s*>true</StrideGraphicsApiDependent>");

        var info = new ProjectInfo(projDir, asmName, isGraphicsDependent, csprojPath, pkgId);

        // Don't overwrite — first match in solution wins
        map.TryAdd(pkgId, info);
        if (!string.Equals(asmName, pkgId, StringComparison.OrdinalIgnoreCase))
            map.TryAdd(asmName, info);
    }

    return map;
}

static void ProcessPackage(string pkgPath, string pkgId, ProjectInfo projInfo, Dictionary<string, ProjectInfo> projectMap,
    string nugetDevDir, string nugetPackagesDir, string version, string strideRoot, string configuration)
{
    using var zip = ZipFile.Open(pkgPath, ZipArchiveMode.Update);

    // Keep lib/<own>.dll and runtimes/ intact — NuGet's normal asset resolution (compile/runtime, plus
    // IncludeAssets/PrivateAssets filtering) needs an actual entry to operate on. Stripping
    // it broke composition with consumers that filter via IncludeAssets="build;buildTransitive"
    // (the build/<PkgId>.targets below substitutes the dev DLL only when NuGet would have
    // included our package's compile/runtime asset for the consumer).

    // Dead weight under the redirect goes: assets and shaders are read from the checkout (the
    // StrideDevProjectDirectory hint in the props), and the assembly processor / asset compiler
    // from $(StrideRoot) (seeded by the props), so their stride/ content and tools/ closures are
    // never opened. The .sdpkg stays: it is how a package is recognized as a Stride package.
    var deadTools = pkgId is "Stride.Core" or "Stride.AssetCompiler";
    foreach (var entry in zip.Entries.Where(e =>
                 (e.FullName.StartsWith("stride/", StringComparison.OrdinalIgnoreCase) && !e.FullName.EndsWith(".sdpkg", StringComparison.OrdinalIgnoreCase))
                 || (deadTools && e.FullName.StartsWith("tools/", StringComparison.OrdinalIgnoreCase))).ToList())
        entry.Delete();

    // Analyzers the package ships that are built in this checkout (a generator, or a dependency of one
    // packed beside it): the targets below redirect them like the DLL, so a generator change reaches
    // consumers on their next build. The dev DLL is looked up in the project's bin/<configuration>/<tfm>
    // folders (netstandard2.0 for a classic analyzer, net10.0 for Stride's generators and their
    // dependencies); not built yet, or no matching project, and the package's copy stays.
    var analyzers = zip.Entries
        .Where(e => e.FullName.StartsWith("analyzers/", StringComparison.OrdinalIgnoreCase) && e.FullName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        .Select(e => Path.GetFileNameWithoutExtension(e.FullName))
        .Where(name => projectMap.ContainsKey(name))
        .Select(name => (Name: name,
                         RelProjDir: Path.GetRelativePath(strideRoot, projectMap[name].ProjectDir).Replace('\\', '/'),
                         Tfm: BuiltTfm(projectMap[name].ProjectDir, configuration, name)))
        .Where(a => a.Tfm != null)
        .Select(a => (a.Name, a.RelProjDir, Tfm: a.Tfm!))
        .ToList();

    // Inject the redirect metadata + targets into both build/ and buildTransitive/ so consumers
    // see them regardless of asset-flow filtering on transitive paths. Merge into any existing
    // <PkgId>.props/.targets the package already shipped (e.g. CompilerApp's StrideCompileAsset
    // chain, Stride.Core/Graphics native-runtime targets) — overwriting would destroy them.
    var propsContent = GenerateRedirectProps(pkgId, projInfo, strideRoot, configuration);
    var targetsContent = GenerateRedirectTargets(pkgId, projInfo, version, strideRoot, configuration, analyzers);

    foreach (var (path, content) in new[]
    {
        ($"build/{pkgId}.props", propsContent),
        ($"buildTransitive/{pkgId}.props", propsContent),
        ($"build/{pkgId}.targets", targetsContent),
        ($"buildTransitive/{pkgId}.targets", targetsContent),
    })
    {
        MergeIntoZipEntry(zip, path, content);
    }

    // Close zip before copying
    zip.Dispose();

    // Deploy to NugetDev
    var destPath = Path.Combine(nugetDevDir, Path.GetFileName(pkgPath));
    File.Copy(pkgPath, destPath, overwrite: true);

    InvalidateNuGetCache(nugetPackagesDir, pkgId, version);
}

// Delete the extracted copy's integrity files so the next consumer restore re-extracts from
// the freshly deployed .nupkg (NuGet otherwise skips same-version re-extraction).
static void InvalidateNuGetCache(string nugetPackagesDir, string pkgId, string version)
{
    var cacheDir = Path.Combine(nugetPackagesDir, pkgId.ToLowerInvariant(), version);
    if (Directory.Exists(cacheDir))
    {
        var sha512 = Path.Combine(cacheDir, $"{pkgId}.{version}.nupkg.sha512");
        var metadata = Path.Combine(cacheDir, ".nupkg.metadata");
        if (File.Exists(sha512)) File.Delete(sha512);
        if (File.Exists(metadata)) File.Delete(metadata);
    }
}

// Merge our generated <Project> content into a zip entry at entryPath, preserving any
// existing content (the original package's build/<PkgId>.props/.targets). New top-level
// children from `addition` are appended to the existing root Project; existing children
// stay in place. If the entry doesn't exist we create one from `addition`.
static void MergeIntoZipEntry(ZipArchive zip, string entryPath, string addition)
{
    XNamespace msbuildNs = "http://schemas.microsoft.com/developer/msbuild/2003";

    XDocument existingDoc;
    var existing = zip.GetEntry(entryPath);
    if (existing != null)
    {
        string existingText;
        using (var s = existing.Open())
        using (var r = new StreamReader(s))
            existingText = r.ReadToEnd();
        existingDoc = string.IsNullOrWhiteSpace(existingText)
            ? new XDocument(new XElement(msbuildNs + "Project"))
            : XDocument.Parse(existingText);
        existing.Delete();
    }
    else
    {
        existingDoc = new XDocument(new XElement(msbuildNs + "Project"));
    }

    var additionDoc = XDocument.Parse(addition);
    if (additionDoc.Root != null && existingDoc.Root != null)
    {
        // The addition is authored in the MSBuild 2003 namespace, but the original package targets
        // may use the namespace-less <Project> form (e.g. Stride.Engine's AOT targets). XLINQ would
        // then stamp the moved children with a redundant xmlns="...2003", which MSBuild rejects
        // (MSB4066). Normalize the moved subtree to the existing root's namespace so the merged file
        // stays consistent either way.
        var rootNs = existingDoc.Root.Name.Namespace;
        foreach (var child in additionDoc.Root.Elements().ToList())
        {
            child.Remove();
            foreach (var e in child.DescendantsAndSelf())
                e.Name = rootNs + e.Name.LocalName;
            existingDoc.Root.Add(child);
        }
    }

    var newEntry = zip.CreateEntry(entryPath);
    using var stream = newEntry.Open();
    using var writer = new StreamWriter(stream, Encoding.UTF8);
    existingDoc.Save(writer);
}

// Props content: marker + paths read by the runtime resolvers
// (AssemblyContainer.TryResolveDevRedirect / RestoreHelper.TryResolveDevRedirect). The
// Reference element sits under Condition="false" so MSBuild ignores it — the runtime
// resolvers parse the XML directly with XLINQ and don't evaluate conditions, so they still
// see the HintPath. Build-time substitution happens in GenerateRedirectTargets() below.
static string GenerateRedirectProps(string pkgId, ProjectInfo projInfo, string strideRoot, string configuration)
{
    var relProjDir = Path.GetRelativePath(strideRoot, projInfo.ProjectDir).Replace('\\', '/');

    var hintPath = projInfo.IsGraphicsDependent
        ? $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/net10.0/$(StrideGraphicsApi)/{projInfo.AssemblyName}.dll"
        : $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/net10.0/{projInfo.AssemblyName}.dll";

    return $"""
        <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <PropertyGroup>
            <StrideDevRedirect>true</StrideDevRedirect>
            <StrideDevRoot Condition="'$(StrideDevRoot)' == ''">{strideRoot}</StrideDevRoot>
            <StrideDevConfiguration Condition="'$(StrideDevConfiguration)' == ''">{configuration}</StrideDevConfiguration>
            <!-- Seed $(StrideRoot) so package targets that anchor on it (e.g. CompilerApp's
                 asset-compiler path lookup) resolve to the in-tree build output. Matches the
                 trailing-slash convention from sources/Directory.Build.props. -->
            <StrideRoot Condition="'$(StrideRoot)' == ''">$(StrideDevRoot)\</StrideRoot>
          </PropertyGroup>
          <ItemGroup Condition="false">
            <Reference Include="{projInfo.AssemblyName}">
              <HintPath>{hintPath}</HintPath>
              <!-- In-tree source project dir, read by NugetStore.GetRealPath so the asset compiler
                   consumes assets + shader source straight from the checkout instead of the stub. -->
              <StrideDevProjectDirectory>$(StrideDevRoot)/{relProjDir}</StrideDevProjectDirectory>
            </Reference>
          </ItemGroup>
        </Project>
        """;
}

// Targets content: hooks AfterTargets="ResolvePackageAssets" so the dev DLL substitution only
// fires when NuGet's asset resolution (which respects the consumer's IncludeAssets/PrivateAssets
// filtering) actually included the original lib/<own>.dll. If the consumer filtered the chain
// (e.g. IncludeAssets="build;buildTransitive" on Stride.AssetCompiler), the
// RuntimeCopyLocalItems/ResolvedCompileFileDefinitions entries for this package aren't there,
// the targets below find nothing to substitute, and we don't sneak runtime DLLs into the
// consumer's bin/ behind NuGet's back.
static string GenerateRedirectTargets(string pkgId, ProjectInfo projInfo, string version, string strideRoot, string configuration,
    List<(string Name, string RelProjDir, string Tfm)> analyzers)
{
    // Packages consumed only via build/buildTransitive (no compile/runtime asset flow — e.g.
    // CompilerApp invoked as a separate exe) have nothing in RuntimeCopyLocalItems for our
    // target to substitute. Emitting the target is still safe: the batched ItemGroup matches
    // zero items and is a no-op. We always emit and let item-set semantics handle the rest.

    var relProjDir = Path.GetRelativePath(strideRoot, projInfo.ProjectDir).Replace('\\', '/');
    var gfxSeg = projInfo.IsGraphicsDependent ? "/$(StrideGraphicsApi)" : "";
    // Redirect to the in-tree bin DLL matching the consumer's TFM when that project produced one
    // (e.g. net10.0-windows brings WinForms/WPF, net10.0-ios its iOS bits), else the portable net10.0
    // build. $(TargetFramework) is the short form, which matches the bin folder name; Exists() makes
    // it self-correcting per project/TFM.
    var portableDll = $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/net10.0{gfxSeg}/{projInfo.AssemblyName}.dll";
    var tfmDll = $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/$(TargetFramework){gfxSeg}/{projInfo.AssemblyName}.dll";
    // Fall back to the no-graphics-API TFM layout when the gfx-segmented dll is absent (e.g. iOS,
    // which has no per-API subdir), rather than the host net10.0 portable build.
    var tfmDllNoGfx = $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/$(TargetFramework)/{projInfo.AssemblyName}.dll";

    // Replace dots with underscores in target/property names; MSBuild rejects dotted target names.
    var safeId = pkgId.Replace('.', '_');

    return $$"""
        <Project xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
          <Target Name="_StrideDevRedirect_{{safeId}}"
                  AfterTargets="ResolvePackageAssets"
                  BeforeTargets="ResolveLockFileReferences;ResolveLockFileCopyLocalFiles;ResolveLockFileAnalyzers">

            <PropertyGroup>
              <_StrideDev_{{safeId}}_DevDll>{{portableDll}}</_StrideDev_{{safeId}}_DevDll>
              <_StrideDev_{{safeId}}_DevDll Condition="'$(TargetFramework)' != 'net10.0' And Exists('{{tfmDll}}')">{{tfmDll}}</_StrideDev_{{safeId}}_DevDll>
              <_StrideDev_{{safeId}}_DevDll Condition="'$(TargetFramework)' != 'net10.0' And !Exists('{{tfmDll}}') And Exists('{{tfmDllNoGfx}}')">{{tfmDllNoGfx}}</_StrideDev_{{safeId}}_DevDll>
            </PropertyGroup>

            <!-- Match by NuGetPackageId AND Filename: some packages ship sibling DLLs in their
                 own lib/ folder (e.g. Stride.Physics bundles BulletSharp.NetStandard.dll via
                 BuildOutputInPackage). Removing those siblings drops them from the consumer's
                 deps.json and breaks runtime resolution. -->
            <ItemGroup>
              <_StrideDev_{{safeId}}_RuntimeItems Include="@(RuntimeCopyLocalItems)"
                                                Condition="'%(RuntimeCopyLocalItems.NuGetPackageId)' == '{{pkgId}}' And '%(Filename)' == '{{projInfo.AssemblyName}}'" />
              <_StrideDev_{{safeId}}_CompileItems Include="@(ResolvedCompileFileDefinitions)"
                                                 Condition="'%(ResolvedCompileFileDefinitions.NuGetPackageId)' == '{{pkgId}}' And '%(Filename)' == '{{projInfo.AssemblyName}}'" />
            </ItemGroup>

            <ItemGroup Condition="'@(_StrideDev_{{safeId}}_RuntimeItems)' != '' And Exists('$(_StrideDev_{{safeId}}_DevDll)')">
              <RuntimeCopyLocalItems Remove="@(_StrideDev_{{safeId}}_RuntimeItems)" />
              <RuntimeCopyLocalItems Include="$(_StrideDev_{{safeId}}_DevDll)">
                <NuGetPackageId>{{pkgId}}</NuGetPackageId>
                <NuGetPackageVersion>{{version}}</NuGetPackageVersion>
                <CopyLocal>true</CopyLocal>
                <DestinationSubPath>{{projInfo.AssemblyName}}.dll</DestinationSubPath>
                <AssetType>runtime</AssetType>
                <!-- StrideAddReference filter in Stride.Core.targets needs this. -->
                <ExternallyResolved>true</ExternallyResolved>
              </RuntimeCopyLocalItems>
            </ItemGroup>

            <ItemGroup Condition="'@(_StrideDev_{{safeId}}_CompileItems)' != '' And Exists('$(_StrideDev_{{safeId}}_DevDll)')">
              <ResolvedCompileFileDefinitions Remove="@(_StrideDev_{{safeId}}_CompileItems)" />
              <ResolvedCompileFileDefinitions Include="$(_StrideDev_{{safeId}}_DevDll)">
                <NuGetPackageId>{{pkgId}}</NuGetPackageId>
                <NuGetPackageVersion>{{version}}</NuGetPackageVersion>
                <ExternallyResolved>true</ExternallyResolved>
                <Private>false</Private>
              </ResolvedCompileFileDefinitions>
            </ItemGroup>

            <!-- Natives: each runtimes/<rid>/native/ file the package ships is replaced by the checkout's copy of
                 the same file, which the native build puts at the same relative path next to the dev DLL. Item by
                 item and only when that file exists (else the package's copy stays), so a native rebuild reaches
                 consumers without regenerating stubs, and nothing the package doesn't ship (a dependency's natives
                 also present in bin/, .lib/.pdb side files) is dragged in. Two consumer shapes: a RID-less build
                 receives natives as RuntimeTargetsCopyLocalItems (DestinationSubDirectory = runtimes/<rid>/native/),
                 a RID-specific one as NativeCopyLocalItems (copied flat; PathInPackage keeps the package layout). -->
            <PropertyGroup Condition="Exists('$(_StrideDev_{{safeId}}_DevDll)')">
              <_StrideDev_{{safeId}}_DevDir>$([System.IO.Path]::GetDirectoryName('$(_StrideDev_{{safeId}}_DevDll)'))</_StrideDev_{{safeId}}_DevDir>
            </PropertyGroup>
            <ItemGroup Condition="'$(_StrideDev_{{safeId}}_DevDir)' != ''">
              <_StrideDev_{{safeId}}_PkgNatives Include="@(RuntimeTargetsCopyLocalItems)"
                                                Condition="'%(RuntimeTargetsCopyLocalItems.NuGetPackageId)' == '{{pkgId}}' And '%(RuntimeTargetsCopyLocalItems.AssetType)' == 'native'" />
              <_StrideDev_{{safeId}}_PkgNativesRid Include="@(NativeCopyLocalItems)"
                                                   Condition="'%(NativeCopyLocalItems.NuGetPackageId)' == '{{pkgId}}'" />
            </ItemGroup>

            <ItemGroup Condition="'@(_StrideDev_{{safeId}}_PkgNatives)' != ''">
              <!-- The transform keeps the package item's metadata (destination, package id, asset type), so the
                   dev file lands exactly where the package's would have. -->
              <_StrideDev_{{safeId}}_DevNatives Include="@(_StrideDev_{{safeId}}_PkgNatives->'$(_StrideDev_{{safeId}}_DevDir)/%(DestinationSubDirectory)%(Filename)%(Extension)')"
                                                Condition="Exists('$(_StrideDev_{{safeId}}_DevDir)/%(DestinationSubDirectory)%(Filename)%(Extension)')">
                <ExternallyResolved>true</ExternallyResolved>
              </_StrideDev_{{safeId}}_DevNatives>
              <RuntimeTargetsCopyLocalItems Remove="@(_StrideDev_{{safeId}}_PkgNatives)"
                                            Condition="Exists('$(_StrideDev_{{safeId}}_DevDir)/%(DestinationSubDirectory)%(Filename)%(Extension)')" />
              <RuntimeTargetsCopyLocalItems Include="@(_StrideDev_{{safeId}}_DevNatives)" />
            </ItemGroup>

            <ItemGroup Condition="'@(_StrideDev_{{safeId}}_PkgNativesRid)' != ''">
              <_StrideDev_{{safeId}}_DevNativesRid Include="@(_StrideDev_{{safeId}}_PkgNativesRid->'$(_StrideDev_{{safeId}}_DevDir)/%(PathInPackage)')"
                                                   Condition="Exists('$(_StrideDev_{{safeId}}_DevDir)/%(PathInPackage)')">
                <ExternallyResolved>true</ExternallyResolved>
              </_StrideDev_{{safeId}}_DevNativesRid>
              <NativeCopyLocalItems Remove="@(_StrideDev_{{safeId}}_PkgNativesRid)"
                                    Condition="Exists('$(_StrideDev_{{safeId}}_DevDir)/%(PathInPackage)')" />
              <NativeCopyLocalItems Include="@(_StrideDev_{{safeId}}_DevNativesRid)" />
            </ItemGroup>
        {{AnalyzerRedirects(pkgId, safeId, version, analyzers)}}
          </Target>
        </Project>
        """;
}

// The bin/<configuration>/<tfm> folder name where this checkout built <assemblyName>.dll, or null when
// it isn't built. Concrete, so a TargetFrameworks given as a property reference is no obstacle.
static string? BuiltTfm(string projectDir, string configuration, string assemblyName)
{
    var bin = Path.Combine(projectDir, "bin", configuration);
    if (!Directory.Exists(bin))
        return null;
    return Directory.GetDirectories(bin)
        .Where(dir => File.Exists(Path.Combine(dir, assemblyName + ".dll")))
        .Select(Path.GetFileName)
        .FirstOrDefault();
}

// One block per analyzer the package ships and this checkout builds: the checkout's DLL replaces
// the package's in ResolvedAnalyzers when it exists (else the package's copy stays).
static string AnalyzerRedirects(string pkgId, string safeId, string version, List<(string Name, string RelProjDir, string Tfm)> analyzers)
{
    var sb = new StringBuilder();
    foreach (var (name, relProjDir, tfm) in analyzers)
    {
        var devDll = $"$(StrideDevRoot)/{relProjDir}/bin/$(StrideDevConfiguration)/{tfm}/{name}.dll";
        var safeName = name.Replace('.', '_');
        sb.Append($$"""

            <!-- Analyzer {{name}}: the checkout's build replaces the package's copy when present. -->
            <ItemGroup Condition="Exists('{{devDll}}')">
              <_StrideDev_{{safeId}}_Analyzer_{{safeName}} Include="@(ResolvedAnalyzers)"
                                                          Condition="'%(ResolvedAnalyzers.NuGetPackageId)' == '{{pkgId}}' And '%(Filename)' == '{{name}}'" />
            </ItemGroup>
            <ItemGroup Condition="'@(_StrideDev_{{safeId}}_Analyzer_{{safeName}})' != ''">
              <ResolvedAnalyzers Remove="@(_StrideDev_{{safeId}}_Analyzer_{{safeName}})" />
              <ResolvedAnalyzers Include="{{devDll}}">
                <NuGetPackageId>{{pkgId}}</NuGetPackageId>
                <NuGetPackageVersion>{{version}}</NuGetPackageVersion>
                <ExternallyResolved>true</ExternallyResolved>
              </ResolvedAnalyzers>
            </ItemGroup>
        """);
    }
    return sb.ToString();
}

record ProjectInfo(string ProjectDir, string AssemblyName, bool IsGraphicsDependent, string CsprojPath, string PackageId);
