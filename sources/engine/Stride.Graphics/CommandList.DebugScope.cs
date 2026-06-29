// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Graphics;

/// <summary>
///   Per-CL portion of the backend-shared scope diagnostics. The CL keeps a small list of
///   per-scope counter buckets; the actual scope tree lives on <see cref="GraphicsDevice"/> and
///   is mutated only by the rendering thread that calls <c>BeginProfile</c>/<c>EndProfile</c>.
///   Worker CLs read <see cref="GraphicsDevice"/>'s current scope frame at record time (no
///   sync needed — the main thread is blocked while workers run inside Dispatcher.For), bump
///   their own local counter for that frame, and the device aggregates at submit.
/// </summary>
public partial class CommandList
{
    private readonly List<DebugPerScopeCounters> debugLocalCounters = new();

    internal enum DebugCounterKind { Draw, Dispatch, Clear, Copy, Barrier }

    internal void RecordDebugCounter(DebugCounterKind kind)
    {
        var device = GraphicsDevice;
        if (device is null || device.IsDebugMode == false)
            return;
        var frame = device.GetDebugCurrentFrame();
        if (frame is null)
            return;

        // Find or create the entry for this frame. List is small (one per scope touched);
        // search from the tail since the active frame is usually the most recently touched.
        var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(debugLocalCounters);
        int idx = -1;
        for (int i = span.Length - 1; i >= 0; i--)
        {
            if (ReferenceEquals(span[i].Frame, frame)) { idx = i; break; }
        }
        if (idx < 0)
        {
            debugLocalCounters.Add(new DebugPerScopeCounters { Frame = frame });
            idx = debugLocalCounters.Count - 1;
            span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(debugLocalCounters);
        }
        ref var entry = ref span[idx];
        switch (kind)
        {
            case DebugCounterKind.Draw:     entry.Draws++; break;
            case DebugCounterKind.Dispatch: entry.Dispatches++; break;
            case DebugCounterKind.Clear:    entry.Clears++; break;
            case DebugCounterKind.Copy:     entry.Copies++; break;
            case DebugCounterKind.Barrier:  entry.Barriers++; break;
        }
    }

    /// <summary>
    ///   Detaches this CL's per-scope counters list (called at submit by the device aggregator).
    /// </summary>
    internal List<DebugPerScopeCounters> DebugScopeExtractLocalCounters()
    {
        if (debugLocalCounters.Count == 0)
            return null;
        var ret = new List<DebugPerScopeCounters>(debugLocalCounters);
        debugLocalCounters.Clear();
        return ret;
    }

    /// <summary>
    ///   Resets the per-CL counter list when a new recording session begins. Backend
    ///   <c>Reset</c> implementations should call this.
    /// </summary>
    internal void DebugScopeResetForNewRecording()
    {
        debugLocalCounters.Clear();
    }
}
