// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Graphics;
using Stride.Samples.Generator;

namespace Stride.SampleScreenshotRunner;

/// <summary>
/// Library entry point for the embed-and-run screenshot regression pipeline. For each sample:
/// regenerate from template → <c>dotnet build -p:StrideAutoTesting=true</c> → launch the exe →
/// wait for done.json or timeout → copy the resulting <c>screenshot-test/</c> artifact dir under
/// the chosen output root. Designed to be called in-process by xunit theory tests; subprocess
/// boundaries (build, sample exe) stay subprocesses for crash isolation.
/// </summary>
public static class ScreenshotRunner
{
    private const int LaunchTimeoutSeconds = 120;
    internal const string SamplesGeneratedDirName = "samplesGenerated";

    private static readonly object InitLock = new();
    private static bool initialized;
    private static List<DiscoveredFixture>? cachedFixtures;
    private static string? cachedFixturesWorktree;

    /// <summary>
    /// One-time setup: initialize MSBuild, load Stride templates. Safe to call multiple times.
    /// </summary>
    public static void Initialize()
    {
        lock (InitLock)
        {
            if (initialized)
                return;
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            StrideDefaultAssetsPlugin.LoadDefaultTemplates();
            initialized = true;
        }
    }

    /// <summary>
    /// Regenerate <paramref name="sampleName"/> from its template, build it with
    /// <c>StrideAutoTesting=true -p:Configuration=&lt;configuration&gt;</c>, launch the exe, wait for it
    /// to write done.json or hit the 60s timeout, copy the captured screenshots + done.json +
    /// error.log into <c>&lt;captureRoot&gt;/&lt;sampleName&gt;/</c>. Build and launch stdout/stderr land
    /// in build.log and launch.log under that dir — callers (e.g. the xunit wrapper) can dump them
    /// to <c>ITestOutputHelper</c> after this returns.
    /// </summary>
    public static SampleResult RunOne(string sampleName, string captureRoot, string worktreeRoot, bool headless = true, string configuration = "Debug")
    {
        Initialize();

        var fixtures = LoadFixtures(worktreeRoot);
        var fixture = fixtures.FirstOrDefault(f => string.Equals(f.SampleName, sampleName, StringComparison.OrdinalIgnoreCase));
        if (fixture is null)
        {
            return new SampleResult
            {
                Name = sampleName,
                Status = "unknown-sample",
                Detail = $"No fixture in tests/Stride.Samples.Tests/<{sampleName}>.cs. Available: {string.Join(", ", fixtures.Select(f => f.SampleName))}",
            };
        }

        EnsureSamplesGeneratedTargets(worktreeRoot);
        var sampleDir = Path.Combine(worktreeRoot, SamplesGeneratedDirName, fixture.SampleName);
        var regenResult = RegenerateSample(fixture, sampleDir);
        if (regenResult is not null)
            return regenResult;

        return RunSample(sampleDir, captureRoot, headless, configuration);
    }

    /// <summary>Returns the catalog of discovered fixtures (cached per <paramref name="worktreeRoot"/>).</summary>
    public static IReadOnlyList<DiscoveredFixture> LoadFixtures(string worktreeRoot)
    {
        Initialize();
        lock (InitLock)
        {
            if (cachedFixtures is not null && string.Equals(cachedFixturesWorktree, worktreeRoot, StringComparison.OrdinalIgnoreCase))
                return cachedFixtures;
            var catalog = LoadTemplateCatalogByGuid();
            cachedFixtures = DiscoverFixtures(worktreeRoot, catalog);
            cachedFixturesWorktree = worktreeRoot;
            return cachedFixtures;
        }
    }

    /// <summary>
    /// Walks <see cref="TemplateManager.FindTemplates"/> (same source GameStudio's New Project
    /// wizard reads) and returns it keyed by template GUID.
    /// </summary>
    private static Dictionary<Guid, string> LoadTemplateCatalogByGuid()
    {
        var session = new PackageSession();
        return TemplateManager.FindTemplates(session)
            .Where(t => t is TemplateDotNetNewDescription)
            .ToDictionary(t => t.Id, t => t.DefaultOutputName ?? t.Name);
    }

    /// <summary>
    /// Scans <c>tests/Stride.Samples.Tests/*.cs</c> for <c>[ScreenshotTest(TemplateId = "...")]</c> attribute
    /// usages. Cross-references each GUID with the template catalog to confirm the template still
    /// exists and to get its current name. Skips files that don't carry the attribute.
    /// </summary>
    private static List<DiscoveredFixture> DiscoverFixtures(string worktreeRoot, Dictionary<Guid, string> catalog)
    {
        var fixturesDir = Path.Combine(worktreeRoot, "tests", "Stride.Samples.Tests");
        if (!Directory.Exists(fixturesDir))
            return [];

        var idPattern = new Regex(
            @"\[\s*ScreenshotTest\s*\(\s*TemplateId\s*=\s*""([^""]+)""\s*\)\s*\]",
            RegexOptions.Compiled);

        var fixtures = new List<DiscoveredFixture>();
        foreach (var file in Directory.EnumerateFiles(fixturesDir, "*.cs"))
        {
            var match = idPattern.Match(File.ReadAllText(file));
            if (!match.Success)
                continue;
            if (!Guid.TryParse(match.Groups[1].Value, out var guid))
            {
                Console.Error.WriteLine($"[{Path.GetFileName(file)}] TemplateId is not a valid GUID; skipping.");
                continue;
            }
            if (!catalog.TryGetValue(guid, out var templateName))
            {
                Console.Error.WriteLine($"[{Path.GetFileName(file)}] TemplateId {guid} not found in template catalog; skipping.");
                continue;
            }
            fixtures.Add(new DiscoveredFixture(file, guid, templateName));
        }
        return fixtures;
    }

    private static SampleResult RunSample(string sampleDir, string outputRoot, bool headless, string configuration)
    {
        sampleDir = Path.GetFullPath(sampleDir);
        outputRoot = Path.GetFullPath(outputRoot);
        var sampleName = Path.GetFileName(sampleDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var sampleOut = Path.Combine(outputRoot, sampleName);
        if (Directory.Exists(sampleOut))
            Directory.Delete(sampleOut, recursive: true);
        Directory.CreateDirectory(sampleOut);

        var stopwatch = Stopwatch.StartNew();
        var result = new SampleResult { Name = sampleName, Status = "unknown" };

        var windowsCsproj = Path.Combine(sampleDir, $"{sampleName}.Windows", $"{sampleName}.Windows.csproj");
        if (!File.Exists(windowsCsproj))
        {
            result.Status = "missing-csproj";
            result.Detail = windowsCsproj;
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        var graphicsApi = GraphicsDevice.Platform.ToString();
        Console.WriteLine($"[{sampleName}] building (Configuration={configuration}, StrideGraphicsApi={graphicsApi})...");
        var buildLog = Path.Combine(sampleOut, "build.log");
        var buildOk = RunProcess("dotnet", $"build \"{windowsCsproj}\" -p:StrideAutoTesting=true -p:Configuration={configuration} -p:StrideGraphicsApi={graphicsApi}", buildLog, sampleDir, env: null, timeoutSeconds: 300);
        if (!buildOk)
        {
            result.Status = "build-failed";
            result.Detail = buildLog;
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        // Output path varies: generated samples set RuntimeIdentifier=win-x64, hand-checked-in samples don't.
        var exeCandidates = new[]
        {
            Path.Combine(sampleDir, "Bin", "Windows", configuration, "win-x64", $"{sampleName}.Windows.exe"),
            Path.Combine(sampleDir, "Bin", "Windows", configuration, $"{sampleName}.Windows.exe"),
        };
        var exePath = exeCandidates.FirstOrDefault(File.Exists);
        if (exePath is null)
        {
            result.Status = "missing-exe";
            result.Detail = string.Join(" | ", exeCandidates);
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        var inProcessOutputDir = Path.Combine(Path.GetDirectoryName(exePath)!, "screenshot-test");
        if (Directory.Exists(inProcessOutputDir))
            Directory.Delete(inProcessOutputDir, recursive: true);

        Console.WriteLine($"[{sampleName}] launching...");
        var launchLog = Path.Combine(sampleOut, "launch.log");
        var env = new Dictionary<string, string>();
        if (headless)
            env["STRIDE_GRAPHICS_SOFTWARE_RENDERING"] = "1";
        var launchOk = RunProcess(exePath, "", launchLog, Path.GetDirectoryName(exePath)!, env, LaunchTimeoutSeconds);

        // Whether the process exited cleanly or not, copy whatever's in the in-process output dir.
        if (Directory.Exists(inProcessOutputDir))
        {
            CopyDirectory(inProcessOutputDir, sampleOut);
        }

        result.Duration = stopwatch.Elapsed;

        var donePath = Path.Combine(sampleOut, "done.json");
        if (File.Exists(donePath))
        {
            try
            {
                using var stream = File.OpenRead(donePath);
                var doc = JsonDocument.Parse(stream);
                if (doc.RootElement.TryGetProperty("status", out var statusEl))
                    result.Status = statusEl.GetString() ?? "unknown";
                if (doc.RootElement.TryGetProperty("screenshots", out var shotsEl) && shotsEl.ValueKind == JsonValueKind.Array)
                    result.ScreenshotCount = shotsEl.GetArrayLength();
            }
            catch (Exception ex)
            {
                result.Status = "done-json-parse-error";
                result.Detail = ex.Message;
            }

            // A clean run must also exit cleanly: a crash or hang after done.json is written (e.g.
            // an AOT teardown segfault, or a non-zero exit / timeout-kill) must not be masked by an
            // "ok" status — otherwise late crashes pass silently.
            if (result.Status == "ok" && !launchOk)
            {
                result.Status = "crashed-after-done";
                result.Detail = "Process exited non-zero after writing done.json (status=ok); see launch.log.";
            }
        }
        else
        {
            result.Status = launchOk ? "no-done-json" : "crashed-no-done-json";
        }

        return result;
    }

    private static bool RunProcess(string fileName, string arguments, string logPath, string workingDir, IDictionary<string, string>? env, int timeoutSeconds)
    {
        var psi = new ProcessStartInfo(fileName, arguments)
        {
            WorkingDirectory = workingDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        if (env is not null)
        {
            foreach (var (k, v) in env)
                psi.Environment[k] = v;
        }

        using var process = Process.Start(psi)!;
        using var log = new StreamWriter(File.Create(logPath));
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) log.WriteLine("[stderr] " + e.Data); };
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (!process.WaitForExit(TimeSpan.FromSeconds(timeoutSeconds)))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            // Kill returns before the OS finishes tearing down the process and releasing its file
            // handles; without this WaitForExit the caller races the kernel and intermittently
            // fails to copy / overwrite files the killed process had open.
            try { process.WaitForExit(TimeSpan.FromSeconds(10)); } catch { }
            log.WriteLine($"[runner] killed after {timeoutSeconds}s timeout");
            return false;
        }
        return process.ExitCode == 0;
    }

    /// <summary>
    /// Regenerate one sample from its template into <paramref name="targetDir"/>, then copy the
    /// fixture file into <c>&lt;targetDir&gt;/&lt;SampleName&gt;.Game/Tests/ScreenshotTest.cs</c>.
    /// Returns null on success; a failure SampleResult on regen error.
    /// </summary>
    private static SampleResult? RegenerateSample(DiscoveredFixture fixture, string targetDir)
    {
        Console.WriteLine($"[{fixture.SampleName}] regenerating from template {fixture.TemplateId}");
        var stopwatch = Stopwatch.StartNew();
        var logger = new LoggerResult();
        try
        {
            SampleGenerator.Generate(new UDirectory(targetDir), fixture.TemplateId, fixture.SampleName, logger);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[{fixture.SampleName}] regen failed: {ex.Message}");
            return new SampleResult
            {
                Name = fixture.SampleName,
                Status = "regen-failed",
                Detail = ex.Message + "\n" + logger.ToText(),
                Duration = stopwatch.Elapsed,
            };
        }

        var fixtureDest = Path.Combine(targetDir, $"{fixture.SampleName}.Game", "Tests", "ScreenshotTest.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(fixtureDest)!);
        File.Copy(fixture.FixtureFile, fixtureDest, overwrite: true);
        return null;
    }

    /// <summary>
    /// Write samplesGenerated/Directory.Build.targets from SampleAutoTesting.targets.template,
    /// substituting the in-tree engine version so the harness PackageReference resolves to the
    /// locally built dev package. MSBuild auto-imports it for every regenerated sample under it.
    /// Lives above the per-sample dir because SampleGenerator wipes the sample dir at the start of
    /// regeneration.
    /// </summary>
    private static void EnsureSamplesGeneratedTargets(string worktreeRoot)
    {
        var samplesGeneratedDir = Path.Combine(worktreeRoot, SamplesGeneratedDirName);
        Directory.CreateDirectory(samplesGeneratedDir);

        var templatePath = Path.Combine(worktreeRoot, "samples", "Tests", "Templates", "SampleAutoTesting.targets.template");
        var content = File.ReadAllText(templatePath)
            .Replace("$STRIDE_DEV_VERSION$", GetStrideDevVersion(worktreeRoot));

        File.WriteAllText(Path.Combine(samplesGeneratedDir, "Directory.Build.targets"), content);
    }

    /// <summary>
    /// The in-tree engine NuGet version the samples resolve against. Read back from the deployed
    /// Stride.Engine dev stub in bin/packages (stride-local): the worktree -devN suffix is only
    /// patched into SharedAssemblyInfo.cs transiently during a pack (it reads empty at rest), so the
    /// stub filename is the authoritative source. Falls back to the bare PublicVersion if no stub.
    /// </summary>
    private static string GetStrideDevVersion(string worktreeRoot)
    {
        var sharedInfo = File.ReadAllText(Path.Combine(worktreeRoot, "sources", "shared", "SharedAssemblyInfo.cs"));
        var publicVersion = Regex.Match(sharedInfo, @"PublicVersion\s*=\s*""([^""]*)""").Groups[1].Value;

        var packagesDir = Path.Combine(worktreeRoot, "bin", "packages");
        if (Directory.Exists(packagesDir))
        {
            var match = Directory.EnumerateFiles(packagesDir, $"Stride.Engine.{publicVersion}*.nupkg")
                .Select(f => Path.GetFileNameWithoutExtension(f)["Stride.Engine.".Length..])
                .FirstOrDefault(v => v == publicVersion || v.StartsWith(publicVersion + "-", StringComparison.Ordinal));
            if (match is not null)
                return match;
        }
        return publicVersion;
    }

    private static void CopyDirectory(string source, string dest)
    {
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(source, file);
            var target = Path.Combine(dest, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}

/// <summary>Fixture discovered in <c>tests/Stride.Samples.Tests/&lt;Name&gt;.cs</c>: GUID + resolved template name + source file.</summary>
public sealed record DiscoveredFixture(string FixtureFile, Guid TemplateId, string SampleName);

/// <summary>Outcome of a single sample run.</summary>
public sealed class SampleResult
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int ScreenshotCount { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Detail { get; set; }
}
