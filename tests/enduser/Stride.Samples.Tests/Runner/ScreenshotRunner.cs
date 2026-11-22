// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Stride.Assets.Templates;
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
    private const int BuildTimeoutSeconds = 300;
    private const int BuildSamplesTimeoutSeconds = 1800;
    // A cold NativeAOT publish (ILC compile + native link) is far slower than a JIT build.
    private const int AotPublishTimeoutSeconds = 1200;
    internal const string SamplesGeneratedDirName = "samplesGenerated";
    // A folder that every sample of the run shares as its cache folder (compiled effects, parsed shaders, pipeline cache)
    private const string SharedEffectCacheVariable = "STRIDE_TESTS_EFFECT_CACHE";

    // Per-host info used to locate the regenerated sample's project + exe. The dotnet-new templates
    // emit one project per platform under <SampleName>.<Folder>/, with OutputPath=Bin/<Folder>/...
    // and only Windows uses a .exe extension.
    private static readonly (string Folder, string Rid, string ExeExt) HostPlatform =
        OperatingSystem.IsLinux()  ? ("Linux",   "linux-x64", "") :
        OperatingSystem.IsMacOS()  ? ("macOS",   RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "osx-arm64" : "osx-x64", "") :
                                     ("Windows", "win-x64",   ".exe");

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
            // Registers the dotnet-new project templates (NewGame + Starters + Samples) with
            // TemplateManager so SampleGenerator's FindTemplates lookup resolves the requested sample.
            DotNetNewTemplateBridge.RegisterProjectTemplates();
            initialized = true;
        }
    }

    /// <summary>
    /// Regenerate <paramref name="sampleName"/> from its template, build it with
    /// <c>StrideAutoTesting=true -p:Configuration=&lt;configuration&gt;</c> (both done for every sample of the run
    /// before its first test, see <see cref="PreparedSamples"/>), launch the exe, wait for it
    /// to write done.json or hit the 60s timeout, copy the captured screenshots + done.json +
    /// error.log into <c>&lt;captureRoot&gt;/&lt;sampleName&gt;/</c>. Build and launch stdout/stderr land
    /// in build.log and launch.log under that dir — callers (e.g. the xunit wrapper) can dump them
    /// to <c>ITestOutputHelper</c> after this returns.
    /// </summary>
    /// <param name="aot">
    /// When true, build the sample with <c>dotnet publish -p:PublishAot=true</c> (self-contained,
    /// host RID) instead of a JIT <c>dotnet build</c>, and launch the published native exe. Exercises
    /// the engine's NativeAOT/trim path end to end.
    /// </param>
    public static SampleResult RunOne(string sampleName, string captureRoot, string worktreeRoot, bool headless = true, string configuration = "Debug", bool aot = false)
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
                Detail = $"No fixture in tests/enduser/Stride.Samples.Tests/Fixtures/<{sampleName}>.cs. Available: {string.Join(", ", fixtures.Select(f => f.SampleName))}",
            };
        }

        EnsureSamplesGeneratedTargets(worktreeRoot);
        var regenResult = PreparedSamples.Take(fixture, worktreeRoot, configuration, out var build);
        if (regenResult is not null)
            return regenResult;

        return RunSample(SampleDirectory(worktreeRoot, fixture.SampleName), captureRoot, headless, configuration, aot, build);
    }

    /// <summary>
    /// Regenerate <paramref name="sampleName"/> and publish it trimmed (self-contained, win-x64) with
    /// the given feature <paramref name="switches"/> forced via RuntimeHostConfigurationOption
    /// (Trim=true), WITHOUT running it. Returns the publish directory so the caller can assert which
    /// assemblies survived trimming — e.g. that disabling a backend switch actually drops its
    /// assemblies. Throws on regen/publish failure.
    /// </summary>
    public static string PublishTrimmed(string sampleName, string worktreeRoot, IReadOnlyDictionary<string, string> switches, string configuration = "Debug")
    {
        Initialize();

        var fixture = LoadFixtures(worktreeRoot).FirstOrDefault(f => string.Equals(f.SampleName, sampleName, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"No fixture in tests/enduser/Stride.Samples.Tests/Fixtures/<{sampleName}>.cs.");

        EnsureSamplesGeneratedTargets(worktreeRoot);
        var regenResult = PreparedSamples.Take(fixture, worktreeRoot, configuration, out _);
        if (regenResult is not null)
            throw new InvalidOperationException($"Regen failed for '{sampleName}': {regenResult.Detail}");

        var sampleDir = SampleDirectory(worktreeRoot, fixture.SampleName);

        // Force the requested feature switches for the trimmer, for this publish only (Directory.Build.props
        // is auto-imported by the per-platform project under it): a later test may take the folder.
        var options = string.Join(Environment.NewLine, switches.Select(kv =>
            $"    <RuntimeHostConfigurationOption Include=\"{kv.Key}\" Value=\"{kv.Value}\" Trim=\"true\" />"));
        var switchesProps = Path.Combine(sampleDir, "Directory.Build.props");
        File.WriteAllText(switchesProps,
            $"<Project>{Environment.NewLine}  <ItemGroup>{Environment.NewLine}{options}{Environment.NewLine}  </ItemGroup>{Environment.NewLine}</Project>{Environment.NewLine}");

        var platformCsproj = PlatformProject(sampleDir, fixture.SampleName);
        var graphicsApi = GraphicsDevice.Platform.ToString();
        var publishLog = Path.Combine(sampleDir, "publish-trimmed.log");
        Console.WriteLine($"[{fixture.SampleName}] trimmed publish (Configuration={configuration}, StrideGraphicsApi={graphicsApi})...");
        var args = $"publish \"{platformCsproj}\" -c {configuration} -r {HostPlatform.Rid} -p:PublishTrimmed=true -p:SelfContained=true -p:StrideGraphicsApi={graphicsApi}";
        try
        {
            if (!RunProcess("dotnet", args, publishLog, sampleDir, env: null, AotPublishTimeoutSeconds))
                throw new InvalidOperationException($"Trimmed publish failed for '{sampleName}'; see {publishLog}");
        }
        finally
        {
            File.Delete(switchesProps);
        }

        return Path.Combine(sampleDir, "Bin", HostPlatform.Folder, configuration, HostPlatform.Rid, "publish");
    }

    /// <summary>The folder a sample is regenerated in.</summary>
    internal static string SampleDirectory(string worktreeRoot, string sampleName) => Path.Combine(worktreeRoot, SamplesGeneratedDirName, sampleName);

    private static string PlatformProject(string sampleDir, string sampleName)
        => Path.Combine(sampleDir, $"{sampleName}.{HostPlatform.Folder}", $"{sampleName}.{HostPlatform.Folder}.csproj");

    /// <summary>
    /// Builds the regenerated <paramref name="sampleNames"/> in one build, with the arguments <see cref="RunSample"/> builds
    /// one with, and returns the build of each. Null when the build failed without naming a sample.
    /// </summary>
    internal static Dictionary<string, SampleBuild>? BuildSamples(string worktreeRoot, IReadOnlyList<string> sampleNames, string configuration)
    {
        var root = Path.Combine(worktreeRoot, SamplesGeneratedDirName);
        var solution = Path.Combine(root, "Samples.slnx");
        new XDocument(new XElement("Solution", sampleNames.Select(name =>
            new XElement("Project", new XAttribute("Path", PlatformProject(SampleDirectory(worktreeRoot, name), name)))))).Save(solution);

        var log = Path.Combine(root, "build.log");
        File.Delete(log);
        Console.WriteLine($"building {sampleNames.Count} samples (Configuration={configuration}, StrideGraphicsApi={GraphicsDevice.Platform})...");
        var (exitCode, output) = SampleGenerator.RunDotnet(
            ["build", solution, "-p:StrideAutoTesting=true", $"-p:Configuration={configuration}", $"-p:StrideGraphicsApi={GraphicsDevice.Platform}"],
            root, log, BuildSamplesTimeoutSeconds);

        // MSBuild names the project of each error, and of each output it writes
        var builds = sampleNames.ToDictionary(name => name, name =>
        {
            var sampleDir = SampleDirectory(worktreeRoot, name) + Path.DirectorySeparatorChar;
            var lines = output.Where(x => x.Contains(sampleDir, StringComparison.OrdinalIgnoreCase)).ToList();
            return new SampleBuild(lines.Any(x => x.Contains(": error ", StringComparison.Ordinal)), lines);
        }, StringComparer.OrdinalIgnoreCase);

        // A build killed at its timeout (-1) leaves samples without errors half built
        return exitCode == 0 || (exitCode > 0 && builds.Values.Any(x => x.Failed)) ? builds : null;
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
    /// Scans <c>tests/enduser/Stride.Samples.Tests/Fixtures/*.cs</c> for <c>[ScreenshotTest(TemplateId = "...")]</c> attribute
    /// usages. Cross-references each GUID with the template catalog to confirm the template still
    /// exists and to get its current name. Skips files that don't carry the attribute.
    /// </summary>
    private static List<DiscoveredFixture> DiscoverFixtures(string worktreeRoot, Dictionary<Guid, string> catalog)
    {
        var fixturesDir = Path.Combine(worktreeRoot, "tests", "enduser", "Stride.Samples.Tests", "Fixtures");
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

    private static SampleResult RunSample(string sampleDir, string outputRoot, bool headless, string configuration, bool aot, SampleBuild? build)
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

        var platformCsproj = PlatformProject(sampleDir, sampleName);
        if (!File.Exists(platformCsproj))
        {
            result.Status = "missing-csproj";
            result.Detail = platformCsproj;
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        var graphicsApi = GraphicsDevice.Platform.ToString();
        var buildLog = Path.Combine(sampleOut, "build.log");
        bool buildOk;
        if (aot)
        {
            Console.WriteLine($"[{sampleName}] AOT publishing (Configuration={configuration}, StrideGraphicsApi={graphicsApi})...");
            var publishArgs = $"publish \"{platformCsproj}\" -c {configuration} -r {HostPlatform.Rid} -p:PublishAot=true -p:SelfContained=true " +
                              $"-p:StrideAutoTesting=true -p:StrideAutoTestingAot=true -p:StrideGraphicsApi={graphicsApi}";
            buildOk = RunProcess("dotnet", publishArgs, buildLog, sampleDir, AotBuildEnv(), AotPublishTimeoutSeconds);
        }
        else if (build is not null)
        {
            Console.WriteLine($"[{sampleName}] built with the other samples of the run (the whole output: {SamplesGeneratedDirName}/build.log)");
            File.WriteAllLines(buildLog, build.Output);
            buildOk = !build.Failed;
        }
        else
        {
            Console.WriteLine($"[{sampleName}] building (Configuration={configuration}, StrideGraphicsApi={graphicsApi})...");
            buildOk = RunProcess("dotnet", $"build \"{platformCsproj}\" -p:StrideAutoTesting=true -p:Configuration={configuration} -p:StrideGraphicsApi={graphicsApi}", buildLog, sampleDir, env: null, BuildTimeoutSeconds);
        }
        if (!buildOk)
        {
            result.Status = "build-failed";
            result.Detail = buildLog;
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        // AOT lands the self-contained native exe in the publish/ subfolder
        var exeName = $"{sampleName}.{HostPlatform.Folder}{HostPlatform.ExeExt}";
        var exePath = Path.Combine(sampleDir, "Bin", HostPlatform.Folder, configuration, HostPlatform.Rid, aot ? "publish" : "", exeName);
        if (!File.Exists(exePath))
        {
            result.Status = "missing-exe";
            result.Detail = exePath;
            result.Duration = stopwatch.Elapsed;
            return result;
        }

        var inProcessOutputDir = Path.Combine(Path.GetDirectoryName(exePath)!, "screenshot-test");
        if (Directory.Exists(inProcessOutputDir))
            Directory.Delete(inProcessOutputDir, recursive: true);

        // The game reads and writes its effects in the cache folder next to its exe
        var sharedEffectCache = Environment.GetEnvironmentVariable(SharedEffectCacheVariable);
        var gameCache = Path.Combine(Path.GetDirectoryName(exePath)!, "cache");
        var restoredCache = string.IsNullOrEmpty(sharedEffectCache) ? null : RestoreEffectCache(sharedEffectCache, gameCache);

        Console.WriteLine($"[{sampleName}] launching...");
        var launchLog = Path.Combine(sampleOut, "launch.log");
        var env = new Dictionary<string, string>();
        if (headless)
            env["STRIDE_GRAPHICS_SOFTWARE_RENDERING"] = "1";
        var launchOk = RunProcess(exePath, "", launchLog, Path.GetDirectoryName(exePath)!, env, LaunchTimeoutSeconds);

        // Not after a crash or a kill: a file cut off mid-write would stay in the cache, as effect files are never rewritten
        if (restoredCache is not null)
        {
            var saved = launchOk ? SaveEffectCache(gameCache, sharedEffectCache!, restoredCache) : 0;
            Console.WriteLine($"[{sampleName}] effect cache: {restoredCache.Count} files restored, {(launchOk ? $"{saved} saved" : "nothing saved after a failed run")}");
        }

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

    /// <summary>
    /// Environment for the AOT publish subprocess: prepend the VS Installer dir to PATH. The NativeAOT
    /// ILC link step shells out to <c>vswhere.exe</c> to locate the MSVC linker, and it must be on PATH.
    /// vswhere ships in this fixed location regardless of VS edition (Community/Pro/BuildTools), so we
    /// don't go through Build.Locator (which resolves the in-proc MSBuild, already set in Initialize).
    /// Returns null if the dir isn't present, leaving PATH untouched.
    /// </summary>
    private static IDictionary<string, string>? AotBuildEnv()
    {
        var pf86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        var installer = Path.Combine(pf86, "Microsoft Visual Studio", "Installer");
        if (!Directory.Exists(installer))
            return null;
        return new Dictionary<string, string>
        {
            ["PATH"] = installer + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
        };
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
            // A hang here is almost always a teardown deadlock (the sample wrote done.json then
            // failed to exit). Snapshot the hung process's thread stacks before killing it so the
            // next CI occurrence ships the actual deadlock callstack as an artifact.
            try { CaptureHangDump(process, Path.GetDirectoryName(logPath)!, log); } catch (Exception e) { log.WriteLine($"[runner] hang dump failed: {e.Message}"); }

            try { process.Kill(entireProcessTree: true); } catch (Exception e) { log.WriteLine($"[runner] kill failed: {e.Message}"); }
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
    /// Captures a heap process dump of a hung sample into the per-sample output dir so it rides along
    /// with the uploaded artifacts. On Windows uses <c>MiniDumpWriteDump</c> (Windows' <c>createdump</c>
    /// can only dump the current process); elsewhere uses the runtime's <c>createdump</c>, which still
    /// takes a target pid. Best-effort: any failure is logged and swallowed.
    /// </summary>
    private static void CaptureHangDump(Process process, string outputDir, TextWriter log)
    {
        var dumpPath = Path.Combine(outputDir, "hang-dump.dmp");
        log.WriteLine($"[runner] capturing hang dump of pid {process.Id} -> {dumpPath}");

        if (OperatingSystem.IsWindows())
        {
            // Prefer a full-memory dump (managed + native callstacks under SOS). If the full read
            // fails — graphics processes can have mapped regions that aren't fully readable — fall
            // back to a stacks dump (Normal + ThreadInfo + IndirectlyReferencedMemory + HandleData),
            // which still carries every thread's native callstack, enough to pinpoint the deadlock.
            if (!TryMiniDump(process, dumpPath, 0x2 | 0x4 | 0x1000, "full", log))
                TryMiniDump(process, dumpPath, 0x0 | 0x1000 | 0x40 | 0x4, "stacks", log);
            return;
        }

        var createdump = Path.Combine(RuntimeEnvironment.GetRuntimeDirectory(), "createdump");
        if (!File.Exists(createdump))
        {
            log.WriteLine($"[runner] createdump not found at '{createdump}'; skipping hang dump");
            return;
        }
        var psi = new ProcessStartInfo(createdump, $"--withheap --name \"{dumpPath}\" {process.Id}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using var dumper = Process.Start(psi);
        if (dumper is null)
        {
            log.WriteLine("[runner] failed to start createdump");
            return;
        }
        var stdout = dumper.StandardOutput.ReadToEnd();
        var stderr = dumper.StandardError.ReadToEnd();
        if (!dumper.WaitForExit(TimeSpan.FromSeconds(120)))
        {
            try { dumper.Kill(entireProcessTree: true); } catch { }
            log.WriteLine("[runner] createdump timed out");
            return;
        }
        foreach (var line in stdout.Split('\n', StringSplitOptions.RemoveEmptyEntries)) log.WriteLine($"[createdump] {line.TrimEnd()}");
        foreach (var line in stderr.Split('\n', StringSplitOptions.RemoveEmptyEntries)) log.WriteLine($"[createdump:stderr] {line.TrimEnd()}");
        log.WriteLine($"[runner] createdump exited {dumper.ExitCode}");
    }

    private static bool TryMiniDump(Process process, string dumpPath, int dumpType, string label, TextWriter log)
    {
        try
        {
            using var fs = new FileStream(dumpPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            if (MiniDumpWriteDump(process.Handle, (uint)process.Id, fs.SafeFileHandle.DangerousGetHandle(),
                                  dumpType, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
            {
                log.WriteLine($"[runner] hang dump ({label}) written: {fs.Length} bytes");
                return true;
            }
            log.WriteLine($"[runner] MiniDumpWriteDump ({label}) failed (win32 error {Marshal.GetLastWin32Error()})");
            return false;
        }
        catch (Exception ex)
        {
            log.WriteLine($"[runner] hang dump ({label}) threw: {ex.Message}");
            return false;
        }
    }

    [DllImport("Dbghelp.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile, int dumpType,
                                                 IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

    /// <summary>
    /// Regenerate one sample from its template into <paramref name="targetDir"/>, then copy the
    /// fixture file into <c>&lt;targetDir&gt;/&lt;SampleName&gt;.Game/Tests/ScreenshotTest.cs</c>.
    /// Returns null on success; a failure SampleResult on regen error.
    /// </summary>
    internal static SampleResult? RegenerateSample(DiscoveredFixture fixture, string targetDir)
    {
        var failure = GenerateSample(fixture, targetDir, upgrade: true);
        if (failure is null)
            AddFixture(fixture, targetDir);
        return failure;
    }

    /// <summary>
    /// Generates one sample from its template into <paramref name="targetDir"/>, without its fixture. Returns null on
    /// success; a failure SampleResult on regen error.
    /// </summary>
    /// <param name="upgrade">Whether to bring a sample written for an older engine to this one.</param>
    internal static SampleResult? GenerateSample(DiscoveredFixture fixture, string targetDir, bool upgrade)
    {
        Console.WriteLine($"[{fixture.SampleName}] regenerating from template {fixture.TemplateId}");
        var stopwatch = Stopwatch.StartNew();
        var logger = new LoggerResult();
        try
        {
            SampleGenerator.Generate(new UDirectory(targetDir), fixture.TemplateId, fixture.SampleName, logger, upgrade);
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
        return null;
    }

    /// <summary>Copies the fixture file of a sample generated in <paramref name="targetDir"/> into its game project.</summary>
    internal static void AddFixture(DiscoveredFixture fixture, string targetDir)
    {
        var fixtureDest = Path.Combine(targetDir, $"{fixture.SampleName}.Game", "Tests", "ScreenshotTest.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(fixtureDest)!);
        File.Copy(fixture.FixtureFile, fixtureDest, overwrite: true);
    }

    /// <summary>
    /// Write samplesGenerated/Directory.Build.targets from SampleAutoTesting.targets.template,
    /// substituting the in-tree engine version so the harness PackageReference resolves to the
    /// locally built dev package. MSBuild auto-imports it for every regenerated sample under it.
    /// Lives above the per-sample dir because SampleGenerator wipes the sample dir at the start of
    /// regeneration. Next to it goes the NuGet config of the repository's samples: a sample written for an
    /// older engine restores that release before its upgrade, so Stride.* also comes from nuget.org there.
    /// A Directory.Build.props gives the platform projects the host RID.
    /// </summary>
    private static void EnsureSamplesGeneratedTargets(string worktreeRoot)
    {
        var samplesGeneratedDir = Path.Combine(worktreeRoot, SamplesGeneratedDirName);
        Directory.CreateDirectory(samplesGeneratedDir);
        File.Copy(Path.Combine(worktreeRoot, "samples", "nuget.config"), Path.Combine(samplesGeneratedDir, "nuget.config"), overwrite: true);

        var templatePath = Path.Combine(worktreeRoot, "tests", "enduser", "Stride.Samples.Tests", "Templates", "SampleAutoTesting.targets.template");
        var content = File.ReadAllText(templatePath)
            .Replace("$STRIDE_DEV_VERSION$", GetStrideDevVersion(worktreeRoot));

        File.WriteAllText(Path.Combine(samplesGeneratedDir, "Directory.Build.targets"), content);

        // The host RID keeps the natives of other platforms (over 300 MB a sample) out of the output. It goes in
        // the platform projects, not on the command line: a solution build refuses -r (NETSDK1134).
        new XDocument(new XElement("Project",
            new XElement("PropertyGroup",
                new XAttribute("Condition", $"'$(StrideAutoTesting)' == 'true' And $(MSBuildProjectName.EndsWith('.{HostPlatform.Folder}'))"),
                new XElement("RuntimeIdentifier", HostPlatform.Rid)))).Save(Path.Combine(samplesGeneratedDir, "Directory.Build.props"));
    }

    /// <summary>
    /// The in-tree engine NuGet version the samples resolve against (e.g. 4.4.0-dev2). The worktree
    /// -devN suffix is overlaid into SharedAssemblyInfo.Generated.cs at build time — the base
    /// SharedAssemblyInfo.cs reads an empty suffix at rest — so the overlay is the authoritative,
    /// per-worktree source. Falls back to a deployed bin/packages stub, then the bare PublicVersion.
    /// </summary>
    private static string GetStrideDevVersion(string worktreeRoot)
    {
        var sharedDir = Path.Combine(worktreeRoot, "sources", "shared");
        var sharedInfo = File.ReadAllText(Path.Combine(sharedDir, "SharedAssemblyInfo.cs"));
        var majorMinor = Regex.Match(sharedInfo, @"MajorMinor\s*=\s*""([^""]*)""").Groups[1].Value;
        var patch = Regex.Match(sharedInfo, @"\bPatch\s*=\s*""([^""]*)""").Groups[1].Value;
        var publicVersion = majorMinor + "." + patch;

        // Prefer the generated overlay's suffix: it carries this worktree's -devN token, which the
        // shared NugetDev feed can't disambiguate (every worktree's stub matches Stride.Engine.4.4.0*).
        var generated = Path.Combine(sharedDir, "SharedAssemblyInfo.Generated.cs");
        if (File.Exists(generated))
        {
            var suffix = Regex.Match(File.ReadAllText(generated), @"NuGetVersionSuffix\s*=\s*""([^""]*)""").Groups[1].Value;
            if (!string.IsNullOrEmpty(suffix))
                return publicVersion + suffix;
        }

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

    /// <summary>
    /// Copies the shared effect cache into the game's cache folder, and returns the size and time of each file copied,
    /// so that <see cref="SaveEffectCache"/> finds what the game wrote.
    /// </summary>
    private static Dictionary<string, (long Length, DateTime LastWriteTimeUtc)> RestoreEffectCache(string sharedCache, string gameCache)
    {
        var restored = new Dictionary<string, (long, DateTime)>();
        if (!Directory.Exists(sharedCache))
            return restored;

        foreach (var file in Directory.EnumerateFiles(sharedCache, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(sharedCache, file);
            var target = Path.Combine(gameCache, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            var copy = new FileInfo(target);
            restored[rel] = (copy.Length, copy.LastWriteTimeUtc);
        }
        return restored;
    }

    /// <summary>
    /// Copies to the shared effect cache the files the game added to its cache folder or wrote again: a parsed shader
    /// whose source changed and the pipeline cache keep their names.
    /// </summary>
    private static int SaveEffectCache(string gameCache, string sharedCache, Dictionary<string, (long Length, DateTime LastWriteTimeUtc)> restored)
    {
        if (!Directory.Exists(gameCache))
            return 0;

        var saved = 0;
        foreach (var file in Directory.EnumerateFiles(gameCache, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(gameCache, file);
            var info = new FileInfo(file);
            if (restored.TryGetValue(rel, out var before) && before == (info.Length, info.LastWriteTimeUtc))
                continue;

            var target = Path.Combine(sharedCache, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
            saved++;
        }
        return saved;
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

/// <summary>Fixture discovered in <c>tests/enduser/Stride.Samples.Tests/Fixtures/&lt;Name&gt;.cs</c>: GUID + resolved template name + source file.</summary>
public sealed record DiscoveredFixture(string FixtureFile, Guid TemplateId, string SampleName);

/// <summary>The build of a sample with the others of the run: the lines of the build output that name it.</summary>
internal sealed record SampleBuild(bool Failed, IReadOnlyList<string> Output);

/// <summary>Outcome of a single sample run.</summary>
public sealed class SampleResult
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int ScreenshotCount { get; set; }
    public TimeSpan Duration { get; set; }
    public string? Detail { get; set; }
}
