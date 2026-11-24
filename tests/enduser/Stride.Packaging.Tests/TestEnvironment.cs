// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Xml.Linq;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Samples.Generator;
using Xunit;

namespace Stride.Packaging.Tests;

/// <summary>
/// Shared paths + headless generation/MSBuild bootstrap for the end-user packaging tests.
/// </summary>
internal static class TestEnvironment
{
    // Each case names its packages folder in its nuget.config, which NUGET_PACKAGES would override for all of them
    static TestEnvironment() => Environment.SetEnvironmentVariable("NUGET_PACKAGES", null);

    // Dev-redirect packages skip the engine's pack: bin/packages then holds stubs and stale packages
#if STRIDE_REDIRECT_PACKAGES
    private static readonly bool BuiltWithRedirectPackages = true;
#else
    private static readonly bool BuiltWithRedirectPackages = false;
#endif

    /// <summary>Repo root: walk up from the test bin output to the directory holding nuget.config.</summary>
    public static string WorktreeRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NuGet.config")) || File.Exists(Path.Combine(dir.FullName, "nuget.config")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate worktree root from " + AppContext.BaseDirectory);
    }

    /// <summary>The locally-built package feed; the engine build/pack must have populated it.</summary>
    public static string BinPackages()
    {
        if (BuiltWithRedirectPackages)
            throw new InvalidOperationException("These tests need the real packages in bin/packages, which dev-redirect packages don't pack: "
                + "run them with -p:StrideRedirectPackages=false (dotnet test tests/enduser/Stride.Packaging.Tests -p:StrideRedirectPackages=false).");
        var binPackages = Path.Combine(WorktreeRoot(), "bin", "packages");
        if (!Directory.Exists(binPackages))
            throw new DirectoryNotFoundException($"bin/packages not found at {binPackages}; build the engine first.");
        return binPackages;
    }

    /// <summary>The platform variant matching the host OS, as used by the dotnet-new templates.</summary>
    public static string HostPlatform => OperatingSystem.IsLinux() ? "linux"
                                       : OperatingSystem.IsMacOS() ? "macos"
                                       : "windows";

    /// <summary>Committed plugin/consumer fixtures (packed/built as subprocesses).</summary>
    public static string FixturesDir() => Path.Combine(WorktreeRoot(), "tests", "enduser", "Stride.Packaging.Tests", "Fixtures");

    /// <summary>
    /// The locally-built Stride NuGet version to pin fixtures to. Prefers a prerelease (the dev
    /// auto-deploy stamp, e.g. 4.4.0.2-dev1); CI's clean feed has exactly one.
    /// </summary>
    public static string ResolveStrideVersion()
    {
        const string prefix = "Stride.Engine.";
        var versions = Directory.EnumerateFiles(BinPackages(), "Stride.Engine.*.nupkg")
            .Select(f => Path.GetFileNameWithoutExtension(f)[prefix.Length..])
            .ToList();
        if (versions.Count == 0)
            throw new InvalidOperationException($"No Stride.Engine package found in {BinPackages()}.");
        var prerelease = versions.Where(v => v.Contains('-')).ToList();
        return (prerelease.Count > 0 ? prerelease : versions).OrderByDescending(v => v, StringComparer.Ordinal).First();
    }

    /// <summary>Recursively copy a directory, skipping bin/obj.</summary>
    public static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileName(dir);
            if (name is "bin" or "obj")
                continue;
            Directory.CreateDirectory(dir.Replace(source, dest));
        }
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") ||
                file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
                continue;
            File.Copy(file, file.Replace(source, dest), overwrite: true);
        }
    }

    /// <summary>The temp folder that holds one tree per case.</summary>
    public static string CasesRoot => Path.Combine(Path.GetTempPath(), "stride-packaging-tests");

    private static readonly Lazy<string> globalJson = new(() =>
    {
        var options = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        var repo = JsonNode.Parse(File.ReadAllText(Path.Combine(WorktreeRoot(), "global.json")), documentOptions: options)!;
        return new JsonObject { ["sdk"] = repo["sdk"]!.DeepClone() }.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    });

    /// <summary>
    /// Writes in a case tree the SDK selection of the repo's global.json, so its builds run on the SDK the repo builds with,
    /// not on the newest SDK of the machine, which the temp folder would get.
    /// </summary>
    public static void WriteGlobalJson(string caseDir)
    {
        var path = Path.Combine(caseDir, "global.json");
        if (!File.Exists(path))
            File.WriteAllText(path, globalJson.Value);
    }

    /// <summary>The case tree, directly under <see cref="CasesRoot"/>, that holds <paramref name="path"/>.</summary>
    public static string CaseDirectory(string path)
    {
        var relative = Path.GetRelativePath(CasesRoot, Path.GetFullPath(path));
        var caseName = relative.Split(Path.DirectorySeparatorChar)[0];
        if (Path.IsPathRooted(relative) || caseName is "." or "..")
            throw new ArgumentException($"[{path}] is not in a case tree under [{CasesRoot}].", nameof(path));
        return Path.Combine(CasesRoot, caseName);
    }

    /// <summary>
    /// The NuGet packages folder the cases share, one per test run, which none of them writes to: the engine packages,
    /// restored before the first case (<see cref="RestoreSharedPackages"/>). Each case restores into a folder of its
    /// own (<see cref="CasePackagesFolder"/>) with this one as its fallback folder, so the plugin packages each case
    /// packs again at the same version stay apart.
    /// </summary>
    public static string SharedPackagesFolder { get; } = Path.Combine(CasesRoot, $"nuget-{Guid.NewGuid():N}");

    /// <summary>The packages folder of the case tree that holds <paramref name="path"/>.</summary>
    public static string CasePackagesFolder(string path) => Path.Combine(CaseDirectory(path), "packages");

    private static readonly Lazy<string> sharedPackages = new(() => RestoreSharedPackages(TestContext.Current.TestOutputHelper!));

    /// <summary>Restores <see cref="SharedPackagesFolder"/> once, the first case to ask logging the restore.</summary>
    public static string EnsureSharedPackages() => sharedPackages.Value;

    /// <summary>
    /// Restores the engine packages the cases use and the asset compiler, with their dependencies (the asset side's are
    /// close to 1 GB), and downloads every other package of this version in <see cref="BinPackages"/> (the companions the sessions load).
    /// </summary>
    private static string RestoreSharedPackages(ITestOutputHelper output)
    {
        var version = ResolveStrideVersion();
        string[] references = ["Stride.Engine", "Stride.UI", "Stride.Audio", "Stride.Particles", "Stride.Physics", "Stride.BepuPhysics", "Stride.SpriteStudio.Runtime", "Stride.Video", "Stride.Voxels", "Stride.AssetCompiler"];
        var suffix = $".{version}.nupkg";
        var downloads = Directory.EnumerateFiles(BinPackages(), "Stride*" + suffix)
            .Select(file => Path.GetFileName(file)[..^suffix.Length])
            .Where(id => !references.Contains(id, StringComparer.OrdinalIgnoreCase))
            .Order(StringComparer.Ordinal);

        var restoreDir = SharedPackagesFolder + "-restore";
        Directory.CreateDirectory(restoreDir);
        NuGetConsumerFeed.WriteStrictNuGetConfig(restoreDir, SharedPackagesFolder, fallbackFolder: null);
        var project = Path.Combine(restoreDir, "SharedPackages.csproj");
        // The games build for net10.0; on Windows, a session restores its companions for net10.0-windows7.0, whose
        // dependency groups differ (Stride.Input's SharpDX)
        var frameworks = OperatingSystem.IsWindows() ? "net10.0;net10.0-windows7.0" : "net10.0";
        new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup", new XElement("TargetFrameworks", frameworks)),
            new XElement("ItemGroup",
                references.Select(id => new XElement("PackageReference", new XAttribute("Include", id), new XAttribute("Version", version))),
                downloads.Select(id => new XElement("PackageDownload", new XAttribute("Include", id), new XAttribute("Version", $"[{version}]"))))))
            .Save(project);

        var watch = Stopwatch.StartNew();
        var result = Dotnet.Exec(["restore", project, "-nr:false", "-v:m"], restoreDir, output, timeoutMin: 15);
        if (result.ExitCode != 0)
            throw new InvalidOperationException($"The restore of the shared packages failed with exit {result.ExitCode}.");
        output.WriteLine($"Restored the shared packages in {watch.Elapsed.TotalSeconds:F0} s");
        DeleteCaseDirectory(restoreDir, output);
        return SharedPackagesFolder;
    }

    /// <summary>
    /// Taken by the work of the tests in the test process (sessions, generators, MSBuild setup), whose static state
    /// is not meant for several threads; the tests run at the same time otherwise (<see cref="ParallelTestFramework"/>).
    /// </summary>
    public static readonly Lock InProcessLock = new();

    /// <summary>
    /// Deletes a temp tree (a case's, or <see cref="SharedPackagesFolder"/>) once done with it: the packages folder holds well
    /// over 1 GB. Set STRIDE_KEEP_TEST_DIRS to keep them for a look after the run.
    /// </summary>
    public static void DeleteCaseDirectory(string caseDir, ITestOutputHelper? output = null)
    {
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STRIDE_KEEP_TEST_DIRS")) || !Directory.Exists(caseDir))
            return;
        try
        {
            // Extracted packages can hold read-only files
            foreach (var file in Directory.EnumerateFiles(caseDir, "*", SearchOption.AllDirectories))
                File.SetAttributes(file, FileAttributes.Normal);
            Directory.Delete(caseDir, recursive: true);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A file still in use (a build server): left for the OS temp cleanup
            output?.WriteLine($"Could not delete [{caseDir}]: {e.Message}");
        }
    }

    private static bool msbuildReady;

    /// <summary>Generate a sample from its template GUID into a fresh temp directory; returns the .slnx path.</summary>
    public static string GenerateSample(Guid templateGuid, string sampleName)
    {
        lock (InProcessLock)
        {
            if (!msbuildReady)
            {
                PackageSessionPublicHelper.FindAndSetMSBuildVersion();
                DotNetNewTemplateBridge.RegisterProjectTemplates();
                msbuildReady = true;
            }

            var outputDir = Path.Combine(CasesRoot, sampleName);
            var logger = new LoggerResult();
            var session = SampleGenerator.Generate(new UDirectory(outputDir), templateGuid, sampleName, logger);
            return session.SolutionPath.ToOSPath();
        }
    }
}
