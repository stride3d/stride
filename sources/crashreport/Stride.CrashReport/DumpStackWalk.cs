// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Diagnostics.Runtime;

namespace Stride.CrashReport;

/// <summary>
/// Symbolicates a native crash against the local build: walks the crashing process's managed threads with
/// ClrMD and fills the crashing thread's managed stack (as a synthetic <see cref="StoredException"/>) and the
/// other threads' stacks into a <see cref="StoredCrash"/>. A native crash carries no managed exception, so
/// without this it reports as a bare message; with it, it renders and dedups like a managed one. Symbolication
/// is local — the DAC and the crash's own modules are on this machine — so no per-build symbol upload is needed.
/// <para>
/// Two sources. <see cref="Enrich"/> reads a still-frozen live process, for the out-of-process capture whose
/// triage dump has no process memory to walk. <see cref="EnrichFromDump"/> reads a saved dump, for the
/// compiler's post-mortem <c>createdump</c> path where the process is already gone; those dumps do carry the
/// CLR memory. Best-effort throughout: any failure leaves the crash as the bare-message report.
/// </para>
/// </summary>
public static class DumpStackWalk
{
    /// <summary>
    /// Enriches <paramref name="crash"/> by walking the managed threads of the frozen process
    /// <paramref name="processId"/>. Call while the crash trigger still has the target frozen (before releasing
    /// it), so the stacks reflect the crash moment. <paramref name="faultingOsThreadId"/> selects the crashing
    /// thread; <paramref name="faultingFrame"/>, when known, is prepended as the native fault location.
    /// </summary>
    public static void Enrich(StoredCrash crash, int processId, uint faultingOsThreadId, string faultingFrame)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            // Passive attach with all threads suspended: a read-only walk of the frozen, dying target. It never
            // resumes execution there, so it can't perturb the crash; ClrMD resumes our suspends on dispose.
            using var target = DataTarget.AttachToProcess(processId, suspend: true);
            if (target.ClrVersions.Length == 0)
                return; // no managed runtime; leave the bare-message report as-is
            Populate(crash, target.ClrVersions[0].CreateRuntime(), faultingOsThreadId, faultingFrame);
        }
        catch
        {
            // best effort: an unsymbolicated crash still sends as the bare-message report
        }
    }

    /// <summary>
    /// Enriches <paramref name="crash"/> by walking a saved minidump at <paramref name="dumpPath"/> (the runtime's
    /// <c>createdump</c> output, which carries the CLR memory a managed walk needs). The crashing thread is taken
    /// from the dump's exception stream when present (Linux/macOS createdump), else from the CLR's fault marker or
    /// the in-flight exception the runtime left on the faulting thread -- which is set for native-code access
    /// violations too, so the crasher is named even on Windows (no exception stream until dotnet/runtime#133065).
    /// <paramref name="faultingFrame"/>, when known, is prepended as the native fault location.
    /// </summary>
    public static void EnrichFromDump(StoredCrash crash, string dumpPath, string faultingFrame)
    {
        try
        {
            using var target = DataTarget.LoadDump(dumpPath);
            if (target.ClrVersions.Length == 0)
                return;
            Populate(crash, target.ClrVersions[0].CreateRuntime(), MinidumpReader.FaultingThreadId(dumpPath), faultingFrame);
        }
        catch
        {
            // best effort: an unwalkable dump still sends as the bare-message report
        }
    }

    // Walks the runtime's threads and writes the crashing thread's stack (a synthetic exception), the crashed
    // thread id, and the other threads' stacks into the crash. The crashing thread is the one matching
    // crashingOsThreadId (the dump's exception stream, or the live trigger's tid) when known; otherwise it is the
    // one carrying the CLR's FaultingExceptionFrame, the marker the runtime pushes only on the thread that took a
    // hardware fault. That marker is what makes Windows createdump work despite having no exception stream -- the
    // faulting thread fast-fails through ntdll, so it can't be told apart by "which thread is running".
    private static void Populate(StoredCrash crash, ClrRuntime runtime, uint? crashingOsThreadId, string faultingFrame)
    {
        var walked = new List<(ClrThread Thread, List<ClrStackFrame> Stack, int FaultIndex, bool HasException)>();
        foreach (var thread in runtime.Threads)
        {
            if (!thread.IsAlive || thread.ManagedThreadId == 0)
                continue;
            List<ClrStackFrame> stack;
            try { stack = thread.EnumerateStackTrace().ToList(); }
            catch { continue; } // a thread ClrMD can't walk; skip it, don't abort the whole dump
            if (!stack.Any(f => f.Kind == ClrStackFrameKind.ManagedMethod && f.Method is not null))
                continue; // pure-native thread
            walked.Add((thread, stack, FaultFrameIndex(stack), HasInFlightException(thread)));
        }

        // Pick the crashing thread: the exception stream / live trigger's tid when known; else the CLR fault
        // marker (managed hardware faults); else the thread the runtime left an in-flight exception on -- which,
        // for a fatal crash, is the faulting thread even for a native-code access violation (no marker, no
        // exception stream), so this is what lets the dump alone name the crasher on Windows.
        var crashingThread =
            (crashingOsThreadId is uint id ? walked.FirstOrDefault(w => w.Thread.OSThreadId == id).Thread : null)
            ?? walked.FirstOrDefault(w => w.FaultIndex >= 0).Thread
            ?? walked.FirstOrDefault(w => w.HasException).Thread;

        var others = new List<StoredThread>();
        foreach (var (thread, stack, faultIndex, _) in walked)
        {
            // Report the OS thread id, not the managed one: it matches the dump's/debugger's thread ids (the
            // managed id is often 0 in a dump) so a maintainer can line a Sentry thread up with the minidump.
            if (crashingThread is not null && thread.OSThreadId == crashingThread.OSThreadId)
            {
                // Start at the fault site: the frames above the marker are the CLR's exception-dispatch plumbing.
                var frames = ManagedFramesFrom(stack, faultIndex >= 0 ? faultIndex : 0);
                var faultSite = frames.Count > 0 ? frames[0].Function : null;
                if (!string.IsNullOrEmpty(faultingFrame))
                    frames.Insert(0, new StoredFrame { Function = faultingFrame });
                var message = string.IsNullOrEmpty(faultSite)
                    ? NativeCrashReporting.NativeCrashMessage(faultingFrame)
                    : $"Native access violation in {faultSite}.";
                crash.Exceptions = new List<StoredException>
                {
                    new() { Type = "NativeCrash", Message = message, Frames = frames },
                };
                crash.CrashedThreadId = (int)thread.OSThreadId;
                SetReportException(crash, message); // so the reporter window title and report.txt name the fault site too
            }
            else
            {
                others.Add(new StoredThread { Id = (int)thread.OSThreadId, Frames = ManagedFramesFrom(stack, 0) });
            }
        }

        if (others.Count > 0)
            crash.Threads = others;
    }

    // ClrThread.CurrentException can throw ("Cannot construct a ClrException with a null Type") for a dump whose
    // exception object has no resolvable type; guard it so one thread can't abort the walk (the crasher is still
    // found via the FaultingExceptionFrame).
    private static bool HasInFlightException(ClrThread thread)
    {
        try { return thread.CurrentException is not null; }
        catch { return false; }
    }

    // Index of the CLR's FaultingExceptionFrame in a raw stack, or -1. The runtime pushes this frame at the point
    // a hardware fault (access violation, etc.) was taken, so it marks the crashing thread and its fault site.
    private static int FaultFrameIndex(List<ClrStackFrame> stack)
    {
        for (var i = 0; i < stack.Count; i++)
            if (stack[i].Kind != ClrStackFrameKind.ManagedMethod
                && (stack[i].FrameName ?? "").Contains("Faulting", StringComparison.OrdinalIgnoreCase))
                return i;
        return -1;
    }

    private static void SetReportException(StoredCrash crash, string message)
    {
        var entry = crash.Report.FirstOrDefault(e => e.Key == "Exception");
        if (entry is not null)
            entry.Value = message;
    }

    // A ClrMD thread's managed frames, newest-first (as .NET and Sentry callers expect). Native and
    // runtime-helper frames are dropped: for a native crash the fault frame is already surfaced separately,
    // and what maintainers want is the managed stack that led into it. Shared with the live-crash snapshot path.
    internal static List<StoredFrame> ManagedFrames(ClrThread thread) => ManagedFramesFrom(thread.EnumerateStackTrace().ToList(), 0);

    private static List<StoredFrame> ManagedFramesFrom(List<ClrStackFrame> stack, int start)
    {
        var frames = new List<StoredFrame>();
        for (var i = Math.Max(0, start); i < stack.Count; i++)
        {
            var frame = stack[i];
            if (frame.Kind != ClrStackFrameKind.ManagedMethod || frame.Method is null)
                continue;
            var method = frame.Method;
            // Drop P/Invoke marshalling thunks (IL_STUB_*): synthetic boundary frames with no source, and a
            // useless fault-site name / dedup key -- keep the real caller as the crash site instead.
            if (method.Name is not null && method.Name.StartsWith("IL_STUB", StringComparison.Ordinal))
                continue;
            frames.Add(new StoredFrame
            {
                Function = method.Type is not null ? method.Type.Name + "." + method.Name : method.Name,
                // ClrModule.Name is the module's full on-disk path; keep only the assembly name (its file name),
                // both because that is what Sentry expects and to avoid shipping a local, user-identifying path.
                Module = ModuleName(method.Type?.Module),
            });
        }
        return frames;
    }

    private static string ModuleName(ClrModule module)
    {
        var path = module?.Name;
        return string.IsNullOrEmpty(path) ? null : Path.GetFileNameWithoutExtension(path);
    }
}
