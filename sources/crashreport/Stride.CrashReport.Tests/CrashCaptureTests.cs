// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Diagnostics;
using Xunit;

namespace Stride.CrashReport.Tests;

/// <summary>
/// Pins the native-crash capture truth table by crashing a probe process per exception class and asserting
/// what got captured. The capture depends on runtime internals (corrupted-state fast-fail, vectored handler
/// ordering, the managed null-check range), so a .NET upgrade can silently change it — these tests turn that
/// into a red build. Expectations are per-platform: native capture is Windows-only today and the rows flip
/// as Linux/macOS support lands.
/// </summary>
public class CrashCaptureTests
{
    public static TheoryData<string, bool, bool, bool, bool> Cases()
    {
        var windows = OperatingSystem.IsWindows();
        var data = new TheoryData<string, bool, bool, bool, bool>
        {
            // mode, expectExitZero, expectDump, expectMarker, expectExceptionStream
            { "managedthrow", true, false, false, false },    // ordinary throw/catch must be untouched
            { "managednull", true, false, false, false },     // managed hardware null-check must stay a catchable NRE
            { "av", false, windows, windows, true },          // wild-pointer native AV: the core capture, tagged
            { "nullnative", false, windows, windows, true },  // native null deref: needs the instruction-based filter
            { "stackoverflow", false, false, false, false },  // known in-process gap: no dump, but must terminate
        };
        if (windows)
            data.Add("raise", false, true, true, false);      // FirstChance leg: dump, but no raw fault context to tag
        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void HandlerCapturesExpectedCrashClasses(string mode, bool expectExitZero, bool expectDump, bool expectMarker, bool expectExceptionStream)
    {
        var dir = CreateWorkDirectory(mode);
        try
        {
            var (exitCode, timedOut, output) = RunProbe(dir, mode, extraEnvironment: null);

            Assert.False(timedOut, $"probe must terminate, not hang (mode={mode}, output: {output})");
            Assert.Equal(expectExitZero, exitCode == 0);
            var dumps = Directory.GetFiles(dir, "*.dmp");
            Assert.Equal(expectDump, dumps.Length > 0);
            var markerPath = Path.Combine(dir, "callback-marker.txt");
            Assert.Equal(expectMarker, File.Exists(markerPath));
            if (dumps.Length > 0)
                Assert.Equal(expectExceptionStream, HasExceptionStream(dumps[0]));
            // The vectored-handler path resolves the faulting frame (module+0x<rva>) from the exception record;
            // the FirstChance/SEH path (raise) has no record, so no frame — the same split as the exception stream.
            if (File.Exists(markerPath))
            {
                var markerFrame = File.ReadAllLines(markerPath).ElementAtOrDefault(1) ?? "";
                Assert.Equal(expectExceptionStream, markerFrame.Contains("+0x"));
            }
        }
        finally
        {
            TryDelete(dir);
        }
    }

    // Minidump stream directory scan: stream type 6 = ExceptionStream, the record that makes a debugger
    // auto-select the faulting thread.
    private static bool HasExceptionStream(string dumpPath)
    {
        var bytes = File.ReadAllBytes(dumpPath);
        Assert.Equal("MDMP"u8.ToArray(), bytes[..4]);
        var streamCount = BitConverter.ToInt32(bytes, 8);
        var directoryRva = BitConverter.ToInt32(bytes, 12);
        for (int i = 0; i < streamCount; i++)
            if (BitConverter.ToUInt32(bytes, directoryRva + i * 12) == 6)
                return true;
        return false;
    }

    [Fact]
    public void NativeCrashLandsInStoreAsStoredCrash()
    {
        var dir = CreateWorkDirectory("store");
        try
        {
            var (exitCode, timedOut, output) = RunProbe(dir, "store",
                extraEnvironment: new Dictionary<string, string> { ["STRIDE_CRASH_DIR"] = dir });

            Assert.False(timedOut, $"probe must terminate, not hang (output: {output})");
            Assert.NotEqual(0, exitCode);

            var appDir = Path.Combine(dir, "testprobe");
            if (!OperatingSystem.IsWindows())
            {
                // Native capture is a no-op off Windows today; this row flips when the monitor lands.
                Assert.False(Directory.Exists(appDir) && Directory.EnumerateFiles(appDir, "*", SearchOption.AllDirectories).Any());
                return;
            }

            var json = Assert.Single(Directory.GetFiles(appDir, "crash-native-*.json", SearchOption.AllDirectories));
            var crash = StoredCrash.FromJson(File.ReadAllText(json));
            Assert.Equal("TestProbe", crash.Application);
            Assert.StartsWith("NativeCrash|", crash.Signature);
            // #3: the signature now carries the faulting frame (module+0x<rva>) so identical native crashes dedup,
            // instead of a per-dump name that never groups. The probe's AV faults inside a native module.
            Assert.Contains("+0x", crash.Signature);
            Assert.False(string.IsNullOrEmpty(crash.DumpFileName));
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(json)!, crash.DumpFileName)), "the dump referenced by the crash must exist");
        }
        finally
        {
            TryDelete(dir);
        }
    }

    private static string CreateWorkDirectory(string mode)
    {
        var dir = Path.Combine(Path.GetTempPath(), $"stride-crash-tests-{mode}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static (int ExitCode, bool TimedOut, string Output) RunProbe(string dir, string mode, Dictionary<string, string>? extraEnvironment)
    {
        var startInfo = CreateProbeStartInfo(dir, mode);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;

        // Deterministic environment: silent capture (no reporter spawn attempts), no runtime createdump
        // muddying the assertions (CI sets it globally), no reporter override leaking in.
        startInfo.Environment["STRIDE_CRASH_MODE"] = "save";
        startInfo.Environment["DOTNET_DbgEnableMiniDump"] = "0";
        startInfo.Environment.Remove("STRIDE_CRASH_REPORTER");
        startInfo.Environment.Remove("STRIDE_CRASH_DIR");
        if (extraEnvironment != null)
            foreach (var (key, value) in extraEnvironment)
                startInfo.Environment[key] = value;

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
        if (!process.WaitForExit(60_000))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            return (process.ExitCode, true, output);
        }
        return (process.ExitCode, false, output);
    }

    private static ProcessStartInfo CreateProbeStartInfo(string dir, string mode)
    {
        var baseDirectory = AppContext.BaseDirectory;
        var apphost = Path.Combine(baseDirectory, OperatingSystem.IsWindows() ? "Stride.CrashReport.TestProbe.exe" : "Stride.CrashReport.TestProbe");
        if (File.Exists(apphost))
            return new ProcessStartInfo(apphost) { ArgumentList = { dir, mode } };

        // No apphost beside the tests: run the managed assembly through the dotnet host.
        var assembly = Path.Combine(baseDirectory, "Stride.CrashReport.TestProbe.dll");
        return new ProcessStartInfo("dotnet") { ArgumentList = { assembly, dir, mode } };
    }

    private static void TryDelete(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }
}
