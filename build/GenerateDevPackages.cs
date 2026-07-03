// Dev-redirect NuGet stub generator for Stride.
// Usage: dotnet run build/GenerateDevPackages.cs -- [options]
//
// Generates stub .nupkg files that redirect to dev-built DLLs,
// eliminating the ~50s NuGet packing overhead on every incremental build.
//
// Steps:
//   1. Builds + packs fresh nupkgs into a temp dir
//   2. Injects build/<PkgId>.props/.targets redirects into each engine nupkg (templates deploy as-is)
//   3. Deploys to NugetDev + mirrors into bin/packages, invalidates NuGet cache
//   4. Writes a stamp manifest (staleness check / cleanup) and prunes superseded versions

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

// --- Parse arguments ---
var strideRoot = "";
var configuration = "Debug";
var solution = "";
var version = "";
var disable = false;
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
    }
}

// --- Resolve defaults ---
if (string.IsNullOrEmpty(strideRoot))
{
    // Walk up from this script's location to find repo root
    var dir = AppContext.BaseDirectory;
    while (dir != null)
    {
        if (Directory.Exists(Path.Combine(dir, "sources", "targets")))
        {
            strideRoot = dir;
            break;
        }
        dir = Path.GetDirectoryName(dir);
    }
    if (string.IsNullOrEmpty(strideRoot))
    {
        // Fallback: assume CWD
        strideRoot = Directory.GetCurrentDirectory();
    }
}

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

Console.WriteLine($"Stride version: {version}");
Console.WriteLine($"Dev root: {strideRoot}");
Console.WriteLine($"Configuration: {configuration}");
Console.WriteLine($"Solution: {solution}");
Console.WriteLine($"NugetDev: {nugetDevDir}");

// --- Toggle path (--disable): flip the flag in Stride.Local.props and exit.
// Stub cleanup runs as a side-effect of the next build (_StrideCleanDevPackages target
// reads the manifest stamp and deletes only the stubs the script generated). ---
if (disable)
{
    var changed = SetDevPackagesFlag(strideRoot, enable: false);
    Console.WriteLine(changed
        ? "Disabled StrideDevPackages in build/Stride.Local.props. Next build will clean up the stubs."
        : "StrideDevPackages was already off (no change).");
    return 0;
}

// --- Step 1: Pack fresh nupkgs ---
var tempPackDir = Path.Combine(Path.GetTempPath(), "stride-devpackages-pack");
if (Directory.Exists(tempPackDir)) Directory.Delete(tempPackDir, true);
Directory.CreateDirectory(tempPackDir);

Console.WriteLine("\nBuilding + packing fresh packages...");
// StrideSkipAutoPack=true: disables Sdk.targets' GeneratePackageOnBuild=true. We're explicitly
// invoking Pack via dotnet pack, so the on-build auto-pack would create a Pack->_PackAsBuildAfterTarget
// ->GenerateNuspec->Pack circular dependency and fail every engine project.
// StrideDevPackages=false: forces engine projects through normal build (not the dev-redirect path)
// regardless of the caller's Stride.Local.props.
// StridePackAssets=false: skip the asset/.sdpkg packing step. The dev-redirect consumes assets +
// shader source straight from the checkout (NugetStore.GetRealPath -> StrideDevProjectDirectory),
// so packed asset content would be dead weight; skipping it also drops the slow per-package copy.
// Output -> tempPackDir (not NugetDev); we deploy stubs there explicitly in step 3.
// No --no-build: self-bootstraps a fresh checkout in one go.
var packExitCode = RunProcess("dotnet", $"pack \"{solution}\" -c {configuration} -p:StrideSkipAutoPack=true -p:StrideDevPackages=false -p:StridePackAssets=false -o \"{tempPackDir}\" --verbosity normal", silent: true, onLine: line =>
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
});
if (packExitCode != 0)
{
    Console.Error.WriteLine($"ERROR: dotnet pack failed with exit code {packExitCode}; stubs not regenerated (feeds keep the previous state).");
    return packExitCode;
}

var freshPackages = Directory.GetFiles(tempPackDir, $"*.{version}.nupkg");

// First run on a fresh worktree: the -devN suffix doesn't exist until the pack itself assigns
// it (StrideEnsureWorktreeVersion writes the ledger + overlay), so the up-front derivation can
// be wrong. The pack output is the truth — read the version off Stride.Core's nupkg.
if (freshPackages.Length == 0)
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
Console.WriteLine($"Packed {freshPackages.Length} packages");

if (freshPackages.Length == 0)
{
    Console.Error.WriteLine($"ERROR: No packages found for version {version}");
    return 1;
}

// --- Step 2: Build project map from solution ---
var projectMap = BuildProjectMap(solution, strideRoot);
Console.WriteLine($"Found {projectMap.Count} project mappings");

// --- Step 3: Process each package ---
Directory.CreateDirectory(nugetDevDir);
var stubCount = 0;
var skipCount = 0;
var generatedStubs = new List<string>();
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
        ProcessPackage(pkgPath, pkgId, projInfo, nugetDevDir, nugetPackagesDir, version, strideRoot, configuration);
        generatedStubs.Add(pkgFileName);
        stubCount++;
        Console.WriteLine(" OK");
    }
    catch (Exception ex)
    {
        Console.WriteLine($" ERROR: {ex.Message}");
    }
}

// Templates are real content packages: no DLLs to redirect, so no stub injection — but our pack
// ran with StrideSkipAutoPack=true, which also suppressed their own auto-pack-deploy, so deploy
// the fresh nupkgs here. Globbed separately: content-versioned ones (Samples, Starters) can carry
// a different version than the engine. Listed in the manifest so cleanup/pruning covers them;
// after a flag-off their auto-pack (independent of StrideDevPackages) repopulates the feeds.
foreach (var tplPath in Directory.GetFiles(tempPackDir, "Stride.Templates.*.nupkg"))
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

// --- Step 4: Write stamp + inputs manifests.
// Stamp: newline list of deployed nupkg filenames (stubs + as-is templates); the cleanup
// target reads it to delete only what we deployed.
// Inputs: absolute paths of the csprojs we scanned; the staleness target reads it so the
// check tracks exactly the projects that contributed (no glob false-positives from WPF tmp
// projects, preprocessor-staged template csprojs, etc.). ---
var stampPath = Path.Combine(nugetDevDir, $".devpackages-{version}");
File.WriteAllLines(stampPath, generatedStubs);
File.WriteAllLines(stampPath + ".inputs", projectMap.Values.Select(p => p.CsprojPath).Distinct());

// Also mirror the stubs into bin/packages (not refreshed while auto-pack is skipped) so the
// repo nuget.config's stride-local mapping keeps resolving; the flag-off cleanup removes them.
var binPackagesDir = Path.Combine(strideRoot, "bin", "packages");
Directory.CreateDirectory(binPackagesDir);
foreach (var stubName in generatedStubs)
    File.Copy(Path.Combine(nugetDevDir, stubName), Path.Combine(binPackagesDir, stubName), overwrite: true);
Console.WriteLine($"Mirrored {generatedStubs.Count} stub package(s) into bin/packages");

// Prune this worktree's superseded versions from NugetDev (shared across worktrees, so it grows
// on every version bump otherwise). The -devN suffix identifies the owner: any older stamp with
// the same suffix belongs to this worktree; its manifest lists exactly what was deployed, so
// delete those files (and their bin/packages mirrors) plus the stamp. Other suffixes untouched.
var suffixIdx = version.IndexOf('-');
if (suffixIdx >= 0)
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
        if (File.Exists(oldStamp + ".inputs")) File.Delete(oldStamp + ".inputs");
        Console.WriteLine($"Pruned superseded {oldVersion}: {removed} package file(s)");
    }
}

// Cleanup temp
try { Directory.Delete(tempPackDir, true); } catch { }

// --- Step 5: Auto-enable StrideDevPackages in build/Stride.Local.props.
// Bootstraps the file from its template if missing (mirrors _StrideBootstrapLocalProps in
// the Stride SDK). Subsequent builds will read the flag and skip auto-pack. ---
var flagChanged = SetDevPackagesFlag(strideRoot, enable: true);

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

        var info = new ProjectInfo(projDir, asmName, isGraphicsDependent, csprojPath);

        // Don't overwrite — first match in solution wins
        map.TryAdd(pkgId, info);
        if (!string.Equals(asmName, pkgId, StringComparison.OrdinalIgnoreCase))
            map.TryAdd(asmName, info);
    }

    return map;
}

static void ProcessPackage(string pkgPath, string pkgId, ProjectInfo projInfo, string nugetDevDir,
    string nugetPackagesDir, string version, string strideRoot, string configuration)
{
    using var zip = ZipFile.Open(pkgPath, ZipArchiveMode.Update);

    // Keep lib/<own>.dll intact — NuGet's normal asset resolution (compile/runtime, plus
    // IncludeAssets/PrivateAssets filtering) needs an actual entry to operate on. Stripping
    // it broke composition with consumers that filter via IncludeAssets="build;buildTransitive"
    // (the build/<PkgId>.targets below substitutes the dev DLL only when NuGet would have
    // included our package's compile/runtime asset for the consumer).

    // Inject the redirect metadata + targets into both build/ and buildTransitive/ so consumers
    // see them regardless of asset-flow filtering on transitive paths. Merge into any existing
    // <PkgId>.props/.targets the package already shipped (e.g. CompilerApp's StrideCompileAsset
    // chain, Stride.Core/Graphics native-runtime targets) — overwriting would destroy them.
    var propsContent = GenerateRedirectProps(pkgId, projInfo, strideRoot, configuration);
    var targetsContent = GenerateRedirectTargets(pkgId, projInfo, version, strideRoot, configuration);

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
static string GenerateRedirectTargets(string pkgId, ProjectInfo projInfo, string version, string strideRoot, string configuration)
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
                  BeforeTargets="ResolveLockFileReferences;ResolveLockFileCopyLocalFiles">

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
          </Target>
        </Project>
        """;
}

record ProjectInfo(string ProjectDir, string AssemblyName, bool IsGraphicsDependent, string CsprojPath);
