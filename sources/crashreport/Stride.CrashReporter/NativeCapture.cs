// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Runtime.InteropServices;
using Stride.CrashReport;

namespace Stride.CrashReporter;

/// <summary>
/// The <c>--capture</c> entry. A crashing host's native trigger spawned us with its pid, faulting thread, and the
/// address of its <c>EXCEPTION_POINTERS</c>, and is frozen on a named event. We write a triage minidump of it from
/// the outside (carrying the exception record, so the dump has an exception stream), release it, then record a
/// <see cref="StoredCrash"/> the window can show. Capturing from this healthy process — not the corrupt, dying one
/// — is what makes the dump reliable. Windows-only; a no-op elsewhere.
/// </summary>
internal static class NativeCapture
{
    /// <summary>
    /// Runs the out-of-process capture and populates <paramref name="runDirectory"/> with the dump and a
    /// <see cref="StoredCrash"/>. Never throws: on any failure the window still opens for whatever is on disk.
    /// </summary>
    public static void Capture(string[] args, string runDirectory)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var captureIndex = Array.IndexOf(args, "--capture");
            var processId = int.Parse(args[captureIndex + 1], CultureInfo.InvariantCulture);
            var threadId = uint.Parse(args[captureIndex + 2], CultureInfo.InvariantCulture);
            var exceptionPointers = (IntPtr)(long)ulong.Parse(args[captureIndex + 3], CultureInfo.InvariantCulture);
            var eventName = GetOption(args, "--event");

            // The crashing host's identity travels on our command line (it runs in a different process). Placeholders if absent.
            var context = (
                Application: GetOption(args, "--app") ?? "Unknown",
                Version: GetOption(args, "--version") ?? "unknown",
                Environment: GetOption(args, "--env") ?? "local");

            Directory.CreateDirectory(runDirectory);
            var dumpPath = Path.Combine(runDirectory, $"native-{processId}.dmp");
            // Opt-in full-memory dump (STRIDE_CRASH_DUMP=full): unscrubbed, multi-GB, never sent — kept locally only.
            var fullMemory = CrashPolicy.FullMemoryDump();
            var captured = MinidumpWriter.TryWriteTargetProcess(processId, threadId, exceptionPointers, dumpPath, fullMemory);

            if (captured)
            {
                var frame = NativeCrashReporting.FaultingFrameFromDump(dumpPath);
                var crash = BuildStoredCrash(dumpPath, frame, context);
                crash.DumpIsFullMemory = fullMemory;
                // Symbolicate while the host is still frozen: walk its managed threads (ClrMD, reading the live
                // process) so a native crash reports the crashing thread's managed stack, not just a message. The
                // triage dump carries no process memory, so this must read the process, not the dump — hence now.
                DumpStackWalk.Enrich(crash, processId, threadId, frame);
                // Dedup on the managed fault site (stable, meaningful) when symbolication found one, else the native
                // fault frame -- mirrors the compiler's native adopt path so both hosts group the same crash alike.
                var faultSite = crash.Exceptions.FirstOrDefault()?.Frames.FirstOrDefault(f => !string.IsNullOrEmpty(f.Module))?.Function;
                crash.Signature = NativeCrashReporting.NativeSignature(faultSite ?? frame, dumpPath);
                // Store through the run so the files follow the store's naming (crash-<sig>.json + .dmp): that is
                // what lets Remove and dedup find them later. The dump already exists (the walk needed it), so the
                // callback moves it into place -- a rename, even for a multi-GB full dump.
                CrashStore.OpenRun(runDirectory).Add(crash, destination => TryMoveDump(dumpPath, destination));
            }

            // Release the frozen host only once the dump AND the report are on disk, so a consumer that inspects
            // the run the moment the host exits sees a complete capture. The host is already dying, so the extra
            // freeze — a dump parse, a stack walk, and a small write — is harmless. Signal even on failure, so it
            // isn't left frozen.
            SignalEvent(eventName);
        }
        catch (Exception)
        {
            // Best effort: a capture failure must not stop the window from opening.
        }
    }

    // Move the pre-written dump to the store's path for it. On failure the crash keeps pointing at the original
    // file (set in BuildStoredCrash) rather than failing the capture.
    private static bool TryMoveDump(string source, string destination)
    {
        try
        {
            File.Move(source, destination, overwrite: true);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
    }

    private static StoredCrash BuildStoredCrash(string dumpPath, string frame,
        (string Application, string Version, string Environment) context)
    {
        var data = new CrashReportData
        {
            ["Application"] = context.Application,
            ["Exception"] = NativeCrashReporting.NativeCrashMessage(frame),
        };
        if (!string.IsNullOrEmpty(frame))
            data["FaultingFrame"] = frame;
        CrashReportAnonymizer.Scrub(data);

        var crash = StoredCrash.FromReportData(data);
        crash.Application = context.Application;
        crash.Version = context.Version;
        crash.Environment = context.Environment;
        crash.TimestampUtc = DateTime.UtcNow.ToString("o");
        // Signature is set by the caller after symbolication, to prefer the managed fault site over the native frame.
        crash.DumpFileName = Path.GetFileName(dumpPath);
        return crash;
    }

    private static string? GetOption(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static void SignalEvent(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return;
        const uint EventModifyState = 0x0002;
        var handle = OpenEventW(EventModifyState, false, name);
        if (handle != IntPtr.Zero)
        {
            SetEvent(handle);
            CloseHandle(handle);
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr OpenEventW(uint desiredAccess, bool inheritHandle, string name);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetEvent(IntPtr handle);
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);
}
