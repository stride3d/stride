// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.Runtime;

namespace Stride.CrashReport;

/// <summary>Snapshots the process at a fatal crash and returns the other threads' managed callstacks (the crashing
/// thread's stack comes from the exception). ClrMD copy-on-write snapshot, no heap dump. Windows-only; empty on failure.</summary>
public static class ThreadSnapshot
{
    /// <summary>Reports the current (crashing) thread's id/name and returns the other threads' stacks.</summary>
    public static List<StoredThread> CaptureAtCurrentThread(out int crashedThreadId, out string crashedThreadName)
    {
        var current = System.Threading.Thread.CurrentThread;
        crashedThreadId = current.ManagedThreadId;
        crashedThreadName = current.Name;
        return CaptureOtherThreads(current.ManagedThreadId);
    }

    /// <summary>The other managed threads' callstacks, excluding <paramref name="crashingThreadManagedId"/>. Call as
    /// close to the fault as possible (a filter, or the unhandled handler) so they reflect the crash moment.</summary>
    public static List<StoredThread> CaptureOtherThreads(int crashingThreadManagedId)
    {
        var threads = new List<StoredThread>();
        if (!OperatingSystem.IsWindows())
            return threads;

        try
        {
            using var target = DataTarget.CreateSnapshotAndAttach(Environment.ProcessId);
            if (target.ClrVersions.Length == 0)
                return threads;
            var runtime = target.ClrVersions[0].CreateRuntime();

            foreach (var thread in runtime.Threads)
            {
                if (!thread.IsAlive || thread.ManagedThreadId == 0 || thread.ManagedThreadId == crashingThreadManagedId)
                    continue;

                var frames = new List<StoredFrame>();
                foreach (var frame in thread.EnumerateStackTrace())
                {
                    if (frame.Kind != ClrStackFrameKind.ManagedMethod || frame.Method is null)
                        continue;
                    var method = frame.Method;
                    frames.Add(new StoredFrame
                    {
                        Function = method.Type is not null ? method.Type.Name + "." + method.Name : method.Name,
                        Module = method.Type?.Module?.Name,
                    });
                }

                if (frames.Count == 0)
                    continue; // pure-native thread
                threads.Add(new StoredThread { Id = thread.ManagedThreadId, Frames = frames });
            }
        }
        catch
        {
            // best effort
        }

        return threads;
    }
}
