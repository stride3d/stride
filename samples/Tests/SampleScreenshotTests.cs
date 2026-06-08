// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Stride.Graphics;
using Stride.SampleScreenshotRunner;
using Stride.Tests.ScreenshotComparator;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Samples.Tests
{
    /// <summary>
    /// xunit wrapper around the embed-and-run screenshot regression pipeline. One <see cref="Sample"/>
    /// theory entry per file in <c>tests/Stride.Samples.Tests/*.cs</c>, so a developer can run a single sample's
    /// regression test from VS / Rider / `dotnet test --filter` without going through GH Actions.
    /// CI uses the same path — the workflow just runs `dotnet test` and lets xunit drive every sample.
    ///
    /// Each entry calls <see cref="ScreenshotRunner.RunOne"/> (regen + build + launch sample as a
    /// subprocess + collect screenshots) then <see cref="ScreenshotComparator.Compare"/> (in-proc
    /// LPIPS scoring). Process boundaries are kept where they matter — the sample game runs in its
    /// own process for crash isolation — but the orchestrator and comparator are now in-proc calls.
    /// </summary>
    [CollectionDefinition("Screenshots", DisableParallelization = true)]
    public class ScreenshotsCollection { }

    [Collection("Screenshots")]
    public class SampleScreenshotTests
    {
        private readonly ITestOutputHelper output;
        public SampleScreenshotTests(ITestOutputHelper output) => this.output = output;

        public static IEnumerable<object[]> Samples()
        {
            var fixturesDir = Path.Combine(WorktreeRoot(), "tests", "Stride.Samples.Tests");
            if (!Directory.Exists(fixturesDir))
                yield break;
            foreach (var file in Directory.EnumerateFiles(fixturesDir, "*.cs"))
                yield return new object[] { Path.GetFileNameWithoutExtension(file) };
        }

        [Theory]
        [MemberData(nameof(Samples))]
        public void Sample(string name) => RunSampleTest(name, aot: false);

        /// <summary>
        /// AOT guard: publish one canonical sample with NativeAOT (PublishAot, self-contained, win-x64)
        /// and run it through the same capture + LPIPS compare as the JIT theory. Catches trim/AOT
        /// regressions — ILC compile breaks, reflection-based serialization creeping back, native
        /// teardown crashes — that a JIT run wouldn't surface.
        /// </summary>
        [Fact]
        public void TopDownRPGAot()
        {
            // AOT publish is expensive and engine AOT-cleanliness is mostly API-agnostic, so one API
            // is enough. Run on Direct3D11 only — a no-op on the D3D12/Vulkan matrix legs.
            if (GraphicsDevice.Platform != GraphicsPlatform.Direct3D11)
            {
                output.WriteLine($"[aot] skipped: AOT lane runs on Direct3D11 only (current: {GraphicsDevice.Platform}).");
                return;
            }
            RunSampleTest("TopDownRPG", aot: true);
        }

        /// <summary>
        /// Trim guard: publish a sample trimmed with the WinForms backend disabled and assert
        /// System.Windows.Forms gets dropped. Proves the WinFormsBackendEnabled gating actually lets
        /// trimming/AOT remove the Win32 windowing + input stack (and catches any un-gated WinForms ref).
        /// </summary>
        [Fact]
        public void TopDownRPGTrimsWinForms()
        {
            // Publish is expensive and the gating is API-agnostic; run on Direct3D11 only.
            if (GraphicsDevice.Platform != GraphicsPlatform.Direct3D11)
            {
                output.WriteLine($"[trim] skipped: runs on Direct3D11 only (current: {GraphicsDevice.Platform}).");
                return;
            }

            var worktree = WorktreeRoot();
            var publishDir = ScreenshotRunner.PublishTrimmed("TopDownRPG", worktree,
                new Dictionary<string, string> { ["Stride.Games.WinFormsBackendEnabled"] = "false" });
            output.WriteLine($"[trim] publish dir: {publishDir}");

            // Sanity-check we're inspecting a real publish — otherwise the WinForms check could pass
            // trivially against a wrong/empty directory.
            Assert.True(Directory.Exists(publishDir), $"Publish dir does not exist: {publishDir}");
            var exe = Path.Combine(publishDir, "TopDownRPG.Windows.exe");
            Assert.True(File.Exists(exe), $"Trimmed publish is missing the app exe (wrong dir?): {exe}");

            var winforms = Path.Combine(publishDir, "System.Windows.Forms.dll");
            Assert.False(File.Exists(winforms),
                $"System.Windows.Forms.dll should have been trimmed with WinFormsBackendEnabled=false, but it's present: {winforms}");

            if (Environment.GetEnvironmentVariable("STRIDE_TESTS_KEEP") != "1")
            {
                var sampleDir = Path.Combine(worktree, ScreenshotRunner.SamplesGeneratedDirName, "TopDownRPG");
                if (Directory.Exists(sampleDir))
                {
                    try { Directory.Delete(sampleDir, recursive: true); }
                    catch (Exception ex) { output.WriteLine($"[cleanup] failed to delete '{sampleDir}': {ex.Message}"); }
                }
            }
        }

        private void RunSampleTest(string name, bool aot)
        {
            var worktree = WorktreeRoot();
            var captureRoot = Path.Combine(worktree, aot ? "screenshot-out-aot" : "screenshot-out");
            var sampleOut = Path.Combine(captureRoot, name);
            if (Directory.Exists(sampleOut))
                Directory.Delete(sampleOut, recursive: true);

            // Capture: regenerate sample, build (or AOT-publish) with StrideAutoTesting=true, launch the
            // sample exe in its own process, collect the harness's screenshots + done.json into
            // <captureRoot>/<name>/.
            var capture = ScreenshotRunner.RunOne(name, captureRoot, worktree, aot: aot);
            output.WriteLine($"[capture] {capture.Status} screenshots={capture.ScreenshotCount} duration={capture.Duration.TotalSeconds:F1}s");
            if (capture.Detail is not null) output.WriteLine($"[capture] detail: {capture.Detail}");

            // Relay subprocess output (sample build + sample exe stdout/stderr) into the test report
            // so failure modes that happen inside those subprocesses are visible without artifact download.
            DumpLog(Path.Combine(sampleOut, "build.log"), "build.log");
            DumpLog(Path.Combine(sampleOut, "launch.log"), "launch.log");
            DumpLog(Path.Combine(sampleOut, "error.log"), "error.log");

            Assert.True(capture.Status == "ok", $"Capture status was '{capture.Status}' (expected 'ok'). Detail: {capture.Detail}");

            // Compare: in-proc LPIPS against committed baselines. --sample isolates this test from
            // earlier theory entries that may have left captures in the same captureRoot.
            // ScreenshotComparator defaults to <bin>/models/lpips_alex.onnx, which the shared
            // Stride.ScreenshotComparator project copies into our output via ProjectReference.
            var baselineDir = Path.Combine(worktree, "tests", "Stride.Samples.Tests");
            var results = ScreenshotComparator.Compare(captureRoot, baselineDir, sampleFilter: name);

            foreach (var r in results)
            {
                var d = r.Lpips.HasValue ? $"lpips={r.Lpips.Value:F4} thr={r.Threshold:F2}" : "";
                output.WriteLine($"[compare] {r.Status,-7} {r.Frame,-20} {d}{(r.Detail is null ? "" : "  " + r.Detail)}");
            }
            var failures = results.Where(r => r.Status is "drift" or "error" or "new").ToList();
            Assert.Empty(failures);

            // Test passed — wipe the regenerated sample dir to keep working trees small. Skip on
            // failure so the post-mortem still has the regenerated project for local debugging.
            // STRIDE_TESTS_KEEP=1 also preserves the dir on pass (for ad-hoc poking at the built sample).
            if (Environment.GetEnvironmentVariable("STRIDE_TESTS_KEEP") != "1")
            {
                var sampleDir = Path.Combine(worktree, ScreenshotRunner.SamplesGeneratedDirName, name);
                if (Directory.Exists(sampleDir))
                {
                    try { Directory.Delete(sampleDir, recursive: true); }
                    catch (Exception ex) { output.WriteLine($"[cleanup] failed to delete '{sampleDir}': {ex.Message}"); }
                }
            }
        }

        private void DumpLog(string path, string label)
        {
            if (!File.Exists(path)) return;
            output.WriteLine($"--- {label} ---");
            foreach (var line in File.ReadAllLines(path))
                output.WriteLine(line);
        }

        /// <summary>Walk up from the cwd until we hit a <c>NuGet.config</c> — the worktree root.</summary>
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
}
