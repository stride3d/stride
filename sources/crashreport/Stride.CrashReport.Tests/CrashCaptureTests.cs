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
                // Post-mortem parse of the dump must recover the same faulting frame the live handler wrote. This is
                // the Linux/macOS signature path (there createdump leaves only the dump, with no live computation),
                // validated here on Windows against the vectored handler's live value.
                if (expectExceptionStream && dumps.Length > 0)
                    Assert.Equal(markerFrame, NativeCrashReporting.FaultingFrameFromDump(dumps[0]));
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
            Assert.False(string.IsNullOrEmpty(crash.DumpFileName));
            Assert.True(File.Exists(Path.Combine(Path.GetDirectoryName(json)!, crash.DumpFileName)), "the dump referenced by the crash must exist");

            // The reporter walks the dump (ClrMD) so a native crash reports the crashing thread's managed stack,
            // not just a message: a synthetic exception with frames, and the crashed thread flagged.
            var exception = Assert.Single(crash.Exceptions);
            Assert.Equal("NativeCrash", exception.Type);
            Assert.NotNull(crash.CrashedThreadId);
            // At least one frame carries a module — a managed frame the walk symbolicated locally (the synthetic
            // native fault frame has none), proving the stack came from the walk and not just the bare message.
            Assert.Contains(exception.Frames, frame => !string.IsNullOrEmpty(frame.Module));
            // #3: identical native crashes must dedup. The signature is the managed fault site the walk resolved
            // (stable across builds, unlike a native module+0x<rva> that shifts with every rebuild); the native frame
            // is only the fallback when no managed frame could be symbolicated.
            var faultSite = exception.Frames.First(frame => !string.IsNullOrEmpty(frame.Module)).Function;
            Assert.Equal("NativeCrash|" + faultSite, crash.Signature);
        }
        finally
        {
            TryDelete(dir);
        }
    }

    [Fact]
    public void TargetProcessDumpWithoutExceptionRecordIsPlainSnapshot()
    {
        // The reporter dumps a still-live host on demand after a *managed* crash: there is no native exception
        // record to carry, so the writer must accept a zero exception pointer and produce a dump with no exception
        // stream (a plain snapshot) instead of asking dbghelp to read address 0 in the target.
        if (!OperatingSystem.IsWindows())
            return; // MiniDumpWriteDump is Windows-only

        var dir = CreateWorkDirectory("selfdump");
        try
        {
            var path = Path.Combine(dir, "self.dmp");
            Assert.True(MinidumpWriter.TryWriteTargetProcess(Environment.ProcessId, 0, IntPtr.Zero, path, fullMemory: false));
            Assert.True(new FileInfo(path).Length > 0);
            Assert.False(HasExceptionStream(path));
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
