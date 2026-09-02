// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
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

            Directory.CreateDirectory(runDirectory);
            var dumpPath = Path.Combine(runDirectory, $"native-{processId}.dmp");
            var captured = MinidumpWriter.TryWriteTargetProcess(processId, threadId, exceptionPointers, dumpPath);
            if (captured)
            {
                var frame = NativeCrashReporting.FaultingFrameFromDump(dumpPath);
                var context = ReadContext(runDirectory);
                WriteStoredCrash(runDirectory, processId, dumpPath, frame, context);
            }

            // Release the frozen host only once the dump AND the report are on disk, so a consumer that inspects
            // the run the moment the host exits sees a complete capture. The host is already dying, so the extra
            // freeze — a dump parse and a small write — is harmless. Signal even on failure, so it isn't left frozen.
            SignalEvent(eventName);
        }
        catch (Exception)
        {
            // Best effort: a capture failure must not stop the window from opening.
        }
    }

    private static void WriteStoredCrash(string runDirectory, int processId, string dumpPath, string frame,
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
        crash.Signature = NativeCrashReporting.NativeSignature(frame, dumpPath);
        crash.DumpFileName = Path.GetFileName(dumpPath);
        File.WriteAllText(Path.Combine(runDirectory, $"crash-native-{processId}.json"), crash.ToJson());
    }

    // The crashing host writes its identity here at startup (healthy code); we run in a different process and
    // can't read its assembly metadata. Falls back to placeholders when absent.
    private static (string Application, string Version, string Environment) ReadContext(string runDirectory)
    {
        try
        {
            var path = Path.Combine(runDirectory, "native-context.json");
            if (File.Exists(path))
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                return (Read(root, "Application", "Unknown"), Read(root, "Version", "unknown"), Read(root, "Environment", "local"));
            }
        }
        catch (Exception)
        {
            // fall through to defaults
        }
        return ("Unknown", "unknown", "local");
    }

    private static string Read(JsonElement element, string name, string fallback)
        => element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString()! : fallback;

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
