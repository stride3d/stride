// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Stride.Assets.Presentation;
using Stride.Assets.Presentation.Templates;
using Stride.Assets.Templates;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.GameStudio.AutoTesting;
using Stride.Tests.ScreenshotComparator;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Editor.Tests;

/// <summary>
/// xunit orchestrator for the GameStudio editor screenshot regression pipeline. Each [Theory] entry
/// spawns Stride.GameStudio.AutoTesting.exe in a subprocess (one fixture per process for WPF
/// singleton-state isolation), waits for it to exit, then runs <see cref="ScreenshotComparator"/>
/// against committed baselines under <c>tests/editor/baselines/dpi100/&lt;fixture&gt;/&lt;frame&gt;.png</c>.
/// </summary>
[CollectionDefinition("EditorScreenshots", DisableParallelization = true)]
public class EditorScreenshotsCollection { }

[Collection("EditorScreenshots")]
public class EditorScreenshotTests
{
    // Detect the runtime DPI so capture and baseline directories are labeled honestly. Both this
    // test process and the AutoTesting runner subprocess call the same helper and converge on
    // the same string, so the snapshot/copy paths line up.
    private static readonly string Dpi = "dpi" + DpiUtil.DetectDpiPercent();

    private readonly ITestOutputHelper output;
    public EditorScreenshotTests(ITestOutputHelper output) => this.output = output;

    public static IEnumerable<object[]> Fixtures()
    {
        // (fixtureName, optional template GUID to instantiate and upgrade before opening, timeout-minutes,
        //  comparison prompt — ScriptEditor ignores the script's literal text (template content can change)
        //  and only checks it renders as syntax-highlighted C# in the editor theme)
        yield return new object?[] { "EmptyEditor",   (Guid?)null,                                             3, EditorComparisonPrompt.Default };
        yield return new object?[] { "TopDownCreate", (Guid?)null,                                             8, EditorComparisonPrompt.Default };
        yield return new object?[] { "TopDownLoad",   (Guid?)new Guid("A363FBC5-89EF-4E7A-B870-6D070813D034"), 5, EditorComparisonPrompt.Default };
        yield return new object?[] { "ScriptEditor",  (Guid?)new Guid("81d2adea-37b1-4711-834c-0d73a05c206c"), 6, EditorComparisonPrompt.ScriptEditor };
        yield return new object?[] { "NewGameEditor", (Guid?)null,                                             5, EditorComparisonPrompt.Default };
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Capture(string fixtureName, Guid? templateGuid, int timeoutMin, EditorComparisonPrompt prompt)
    {
        var worktree = WorktreeRoot();
        var captureRoot = Path.Combine(worktree, "ui-test-out-" + Dpi);
        var fixtureCapture = Path.Combine(captureRoot, fixtureName);
        if (Directory.Exists(fixtureCapture))
            Directory.Delete(fixtureCapture, recursive: true);

        var dll = typeof(EditorScreenshotTests).Assembly.Location;
        var exe = ResolveAutoTestingExe(dll, worktree);
        var args = new List<string> { "--test-dll", dll, "--test-name", fixtureName };
        if (templateGuid is { } guid)
        {
            var generated = GenerateSampleFromTemplate(guid, fixtureName);
            args.Add(generated);
        }

        // Clean the runner-side output dir so stale files from a previous fixture invocation
        // don't leak into this fixture's capture set.
        var runnerOut = Path.Combine(Path.GetDirectoryName(dll)!, "ui-test-out-" + Dpi);
        if (Directory.Exists(runnerOut)) Directory.Delete(runnerOut, recursive: true);

        var psi = new ProcessStartInfo(exe)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = worktree,
        };
        foreach (var a in args) psi.ArgumentList.Add(a);

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.WriteLine($"[stdout] {e.Data}"); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) output.WriteLine($"[stderr] {e.Data}"); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        if (!proc.WaitForExit(timeoutMin * 60_000))
        {
            try { proc.Kill(); } catch { }
            throw new TimeoutException($"{fixtureName} timed out after {timeoutMin}min");
        }
        Assert.True(proc.ExitCode == 0, $"{fixtureName} exit={proc.ExitCode}");

        // Snapshot per-fixture: the runner writes to <test-dll-dir>/ui-test-out-dpi100/{screenshots,
        // log.txt, done.json}; relocate that into <worktree>/ui-test-out-dpi100/<fixture>/ so the
        // comparator can find <newDir>/<sample>/screenshots/<frame>.png.
        if (!Directory.Exists(runnerOut))
            throw new DirectoryNotFoundException($"Runner output dir not found: {runnerOut}");
        Directory.CreateDirectory(fixtureCapture);
        CopyAll(runnerOut, fixtureCapture);

        // Diag logs live in $TEMP and are overwritten by each fixture's runner — copy them into
        // the per-fixture capture dir before the next [Theory] entry runs.
        var temp = Path.GetTempPath();
        foreach (var diag in new[] { "autotest-diag.log", "gs-diag.log" })
        {
            var src = Path.Combine(temp, diag);
            if (File.Exists(src))
            {
                try { File.Copy(src, Path.Combine(fixtureCapture, diag), overwrite: true); } catch { }
            }
        }

        // Compare against baselines. Filter to this fixture so the same captureRoot can host
        // multiple fixtures' captures across test invocations.
        var baselineDir = Path.Combine(worktree, "tests", "editor", "baselines", Dpi);
        var results = ScreenshotComparator.Compare(captureRoot, baselineDir,
            sampleFilter: fixtureName, defaultPrompt: prompt,
            deferWhenVisionUnavailable: true);

        foreach (var r in results)
        {
            var lpips = r.Lpips.HasValue ? $"lpips={r.Lpips.Value:F4}" : "";
            output.WriteLine($"[compare] {r.Status,-12} {r.Frame,-25} thr={r.Threshold:F2} {lpips}{(r.Detail is null ? "" : "  " + r.Detail)}");
        }

        // Frames over the LPIPS threshold that opted into the Claude vision tiebreak but ran without a
        // key (fork PRs get no secrets). Record them so the trusted vision gate (workflow_run, which
        // has the key) can rule on them. One manifest per fixture → no cross-test races.
        var deferred = results.Where(r => r.Status is "deferred").ToList();
        if (deferred.Count > 0)
            WriteDeferredManifest(fixtureCapture, fixtureName, deferred);

        // Hard regressions (LPIPS drift with no vision tiebreak, capture/compare errors, missing
        // baseline) fail regardless of any deferral.
        var failures = results.Where(r => r.Status is "drift" or "error" or "new").ToList();
        Assert.Empty(failures);

        // Deferred-only (no hard failures): the verdict belongs to the trusted vision gate. xunit's
        // Assert.Skip isn't available under this project's custom runner, so the test just passes here
        // and logs; the manifest above plus the required, fail-closed gate keep it honest.
        if (deferred.Count > 0)
            output.WriteLine($"[compare] DEFERRED {deferred.Count} frame(s) to the vision gate " +
                             $"(no ANTHROPIC_API_KEY in this run): {string.Join(", ", deferred.Select(r => r.Frame))}");
    }

    /// <summary>
    /// Write <c>&lt;fixtureCapture&gt;/vision-deferred.json</c> listing the frames this keyless run couldn't
    /// rule on. The trusted vision gate globs these across fixtures, re-judges each with the key, and
    /// fails closed if a listed frame can't be resolved.
    /// </summary>
    private static void WriteDeferredManifest(string fixtureCapture, string fixture, IReadOnlyList<ComparisonResult> deferred)
    {
        Directory.CreateDirectory(fixtureCapture);
        var payload = new
        {
            sample = fixture,
            frames = deferred.Select(r => new { frame = r.Frame, lpips = r.Lpips, threshold = r.Threshold, detail = r.Detail }).ToArray(),
        };
        File.WriteAllText(
            Path.Combine(fixtureCapture, "vision-deferred.json"),
            // Allow NaN/Infinity — a deferred frame's lpips can be non-finite, so serialization mustn't fail the keyless run.
            JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                WriteIndented = true,
                NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            }));
    }

    /// <summary>
    /// Instantiates a template sample (by .sdtpl Id GUID) into a per-fixture temp dir via
    /// <see cref="DotNetNewTemplateGenerator"/> — the same path GS's New Project wizard takes
    /// at runtime. Returns the absolute .slnx path the AutoTesting runner should open.
    /// </summary>
    internal static string GenerateSampleFromTemplate(Guid templateGuid, string sampleName)
    {
        var outputDir = Path.Combine(Path.GetTempPath(), "stride-editor-tests", sampleName);
        if (Directory.Exists(outputDir))
            Directory.Delete(outputDir, recursive: true);
        Directory.CreateDirectory(outputDir);

        PackageSessionPublicHelper.FindAndSetMSBuildVersion();

        var logger = new LoggerResult();
        var session = new PackageSession();
        var parameters = new SessionTemplateGeneratorParameters { Session = session, Unattended = true };
        DotNetNewTemplateGenerator.SetParameters(parameters, new Dictionary<string, string>
        {
            ["platforms"] = "windows",
        });

        StrideDefaultAssetsPlugin.LoadDefaultTemplates();
        var template = TemplateManager.FindTemplates(session).FirstOrDefault(t => t.Id == templateGuid)
            ?? throw new InvalidOperationException($"Template {templateGuid} not found in catalog");
        parameters.Description = template;
        parameters.Name = sampleName;
        parameters.Namespace = sampleName;
        parameters.OutputDirectory = outputDir;
        parameters.Logger = logger;

        session.SolutionPath = UPath.Combine<UFile>(outputDir, sampleName + ".slnx");

        var generator = new DotNetNewTemplateGenerator();
        if (!generator.PrepareForRun(parameters).Result)
            throw new InvalidOperationException($"PrepareForRun failed for {sampleName}:\n{logger.ToText()}");
        if (!generator.Run(parameters))
            throw new InvalidOperationException($"Run failed for {sampleName}:\n{logger.ToText()}");

        if (logger.HasErrors)
            throw new InvalidOperationException($"Generating sample {sampleName} produced errors:\n{logger.ToText()}");

        return session.SolutionPath.ToOSPath();
    }

    private static string ResolveAutoTestingExe(string testDllPath, string worktree)
    {
        // tests\editor\bin\<cfg>\<tfm>\Stride.Editor.Tests.dll → mirror the cfg+tfm into the runner's
        // sources\editor\Stride.GameStudio.AutoTesting\bin tree.
        var dllDir = new DirectoryInfo(Path.GetDirectoryName(testDllPath)!);
        var tfm = dllDir.Name;
        var cfg = dllDir.Parent!.Name;
        var path = Path.Combine(worktree, "sources", "editor", "Stride.GameStudio.AutoTesting",
            "bin", cfg, tfm, "Stride.GameStudio.AutoTesting.exe");
        if (!File.Exists(path))
            throw new FileNotFoundException($"AutoTesting runner exe not found at: {path}", path);
        return path;
    }

    private static void CopyAll(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.EnumerateFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.EnumerateDirectories(src))
            CopyAll(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    /// <summary>Walks up from cwd until it finds a NuGet.config — that's the worktree root.</summary>
    private static string WorktreeRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "NuGet.config")) || File.Exists(Path.Combine(dir.FullName, "nuget.config")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new InvalidOperationException("Could not locate worktree root from " + Environment.CurrentDirectory);
    }
}
