// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public void Sample(string name)
        {
            var worktree = WorktreeRoot();
            var captureRoot = Path.Combine(worktree, "screenshot-out");
            var sampleOut = Path.Combine(captureRoot, name);
            if (Directory.Exists(sampleOut))
                Directory.Delete(sampleOut, recursive: true);

            // Capture: regenerate sample, build with StrideAutoTesting=true, launch the sample exe
            // in its own process, collect the harness's screenshots + done.json into <captureRoot>/<name>/.
            var capture = ScreenshotRunner.RunOne(name, captureRoot, worktree);
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
            var sampleDir = Path.Combine(worktree, "samplesGenerated", name);
            if (Directory.Exists(sampleDir))
            {
                try { Directory.Delete(sampleDir, recursive: true); }
                catch (Exception ex) { output.WriteLine($"[cleanup] failed to delete '{sampleDir}': {ex.Message}"); }
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
