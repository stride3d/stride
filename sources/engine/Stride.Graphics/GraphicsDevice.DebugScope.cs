// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Text;

using Stride.Core.Diagnostics;

namespace Stride.Graphics;

/// <summary>
///   Backend-shared diagnostic state on the device. The active scope stack and the in-progress
///   per-frame scope tree both live here, mutated only by the rendering thread that calls
///   <c>BeginProfile</c>/<c>EndProfile</c> (in Stride: the main rendering thread).
///
///   Worker CommandLists (e.g. recorded inside <c>Dispatcher.For</c>) never call BeginProfile,
///   so the stack/tree state isn't contended. They simply read the current frame pointer at
///   record time, accumulate per-CL counters, and the device merges them into the tree at submit
///   under the queue lock.
/// </summary>
public partial class GraphicsDevice
{
    internal static readonly Logger DebugLog = GlobalLogger.GetLogger("GraphicsDebug");

    // Active scope stack (Tier 1: always on, including release — used for log annotation).
    // Mirrors the active BeginProfile/EndProfile state.
    private readonly Stack<string> debugScopeStack = new();

    // Tier 2: scope tree, only built when IsDebugMode is set.
    private DebugScopeFrame debugTreeRoot;
    private DebugScopeFrame debugCurrentFrame;

    /// <summary>
    ///   Set by backend message callbacks/poll loops when a draw-relevant validation issue
    ///   fires during the current frame. Cleared after dump in <see cref="DebugEndFrame"/>.
    /// </summary>
    internal bool debugSawDrawIssue;

    /// <summary>
    ///   Kill switch that forces every validation message to be treated as GPU-side and skip
    ///   leaf attribution / tree-dump triggering. Backends already detect GPU-side messages
    ///   per-message (D3D11/D3D12 via the <c>D3D*_MESSAGE_ID_GPU_BASED_VALIDATION_*</c>
    ///   enum entries; Vulkan via <c>pMessageIdName</c>), so this is only needed if those
    ///   heuristics ever miss a backend's GBV namespace and produce noisy misattributed
    ///   dumps. Defaults to <see langword="false"/>; Stride doesn't enable any GBV mode today.
    /// </summary>
    public bool DebugGpuValidationEnabled { get; set; }

    /// <summary>
    ///   Pushes a scope onto the active scope stack. Backend BeginProfile implementations call
    ///   this. Tier 1 (stack) is always maintained; Tier 2 (tree) only when IsDebugMode is set.
    /// </summary>
    internal void PushDebugScope(string name)
    {
        debugScopeStack.Push(name);
        if (!IsDebugMode)
            return;

        debugTreeRoot ??= new DebugScopeFrame { Name = null }; // synthetic root
        var parent = debugCurrentFrame ?? debugTreeRoot;
        var frame = new DebugScopeFrame { Name = name, Parent = debugCurrentFrame };
        parent.AddChild(frame);
        debugCurrentFrame = frame;
    }

    internal void PopDebugScope()
    {
        if (debugScopeStack.Count > 0)
            debugScopeStack.Pop();

        if (IsDebugMode && debugCurrentFrame is not null)
        {
            debugCurrentFrame.ClosedByPop = true;
            debugCurrentFrame = debugCurrentFrame.Parent;
        }
    }

    /// <summary>
    ///   Marks the end of the rendering frame. Called by the game loop after the frame's
    ///   command lists have been submitted (so per-CL counter aggregation has already merged
    ///   into the tree) and before <c>Present</c>. Drains backend debug messages, dumps the
    ///   scope tree if any error fired, then resets the tree for the next frame.
    /// </summary>
    /// <summary>
    ///   Test toggle: when <see langword="true"/>, the scope tree is dumped every frame even
    ///   if no validation issue fired. Useful for verifying counter values (e.g. <c>CLs=N</c>
    ///   parallelism markers) against a known-clean scene. Off by default; flip it from a
    ///   breakpoint or a debug-only command for ad-hoc inspection.
    /// </summary>
    internal static bool DebugAlwaysDumpTree;

    internal void DebugEndFrame()
    {
        if (!IsDebugMode)
            return;
        if (DebugAlwaysDumpTree)
            debugSawDrawIssue = true;
        DrainDebugMessages();
        debugTreeRoot = null;
        debugCurrentFrame = null;
    }

    /// <summary>
    ///   Drains backend-specific debug-message queue. Backend partials override / fill this in.
    ///   No-op by default; D3D12 partial pumps the InfoQueue.
    /// </summary>
    partial void DrainDebugMessages();

    /// <summary>The currently-open scope frame, or <see langword="null"/> if no scope is active.</summary>
    internal DebugScopeFrame GetDebugCurrentFrame() => debugCurrentFrame;

    /// <summary>
    ///   Name of the innermost active scope, or <see langword="null"/> if no scope is active.
    ///   Used as a short prefix on individual log messages; <see cref="GetDebugActiveScopePath"/>
    ///   gives the full root→leaf path for the tree dump's footer.
    /// </summary>
    internal string GetDebugLeafScopeName() => debugScopeStack.Count > 0 ? debugScopeStack.Peek() : null;

    /// <summary>
    ///   Returns the active scope path (root → leaf) as a single string, or <see langword="null"/>
    ///   if no scope is active. Used for annotating validation messages.
    /// </summary>
    internal string GetDebugActiveScopePath()
    {
        if (debugScopeStack.Count == 0)
            return null;
        var sb = new StringBuilder();
        bool first = true;
        var arr = debugScopeStack.ToArray(); // top → bottom (leaf → root)
        for (int i = arr.Length - 1; i >= 0; i--)
        {
            if (!first) sb.Append(" / ");
            sb.Append(arr[i]);
            first = false;
        }
        return sb.ToString();
    }

    /// <summary>
    ///   Aggregates a CL's per-scope counters into the device tree. Caller must hold the queue lock.
    ///   Increments <see cref="DebugScopeFrame.ExecutedCLs"/> on each touched frame so the dump
    ///   can show how many CLs participated when there's parallelism.
    /// </summary>
    internal void DebugAggregateLocalCounters(List<DebugPerScopeCounters> entries)
    {
        if (entries is null) return;
        foreach (var e in entries)
        {
            var f = e.Frame;
            f.Draws      += e.Draws;
            f.Dispatches += e.Dispatches;
            f.Clears     += e.Clears;
            f.Copies     += e.Copies;
            f.Barriers   += e.Barriers;
            f.ExecutedCLs++;
        }
    }

    /// <summary>Renders the current scope tree as a multi-line string and resets it.</summary>
    internal void DebugDumpTree()
    {
        if (debugTreeRoot is null || debugTreeRoot.Children is not { Count: > 0 })
            return;

        var sb = new StringBuilder();
        sb.AppendLine("Scope tree:");
        foreach (var child in debugTreeRoot.Children)
            AppendTree(sb, child, 0);

        var openLeaf = debugCurrentFrame;
        if (openLeaf is not null && openLeaf.Name is not null)
        {
            sb.Append("Active scope: ");
            AppendScopePath(sb, openLeaf);
            sb.AppendLine();
        }

        DebugLog.Error(sb.ToString().TrimEnd());
        debugTreeRoot = null;
        debugCurrentFrame = null;
    }

    private static void AppendTree(StringBuilder sb, DebugScopeFrame frame, int depth)
    {
        sb.Append(' ', depth * 2);
        // Visual marker so offending scopes pop out at a glance when scanning the tree.
        if (frame.Errors > 0)
            sb.Append("[!] ");
        else if (frame.Warnings > 0)
            sb.Append("[?] ");
        sb.Append(frame.Name ?? "(unnamed)");

        bool any = (frame.Draws | frame.Dispatches | frame.Clears | frame.Copies | frame.Barriers
                    | frame.Errors | frame.Warnings) != 0
                   || frame.ExecutedCLs > 1;
        if (any)
        {
            sb.Append(" (");
            bool first = true;
            void AppendStat(string key, int v)
            {
                if (v == 0) return;
                if (!first) sb.Append(", ");
                sb.Append(key).Append('=').Append(v);
                first = false;
            }
            AppendStat("Errors", frame.Errors);
            AppendStat("Warnings", frame.Warnings);
            AppendStat("Draws", frame.Draws);
            AppendStat("Dispatches", frame.Dispatches);
            AppendStat("Clears", frame.Clears);
            AppendStat("Copies", frame.Copies);
            AppendStat("Barriers", frame.Barriers);
            // Only show the CL count when it's a multi-CL scope (parallelism marker).
            if (frame.ExecutedCLs > 1)
                AppendStat("CLs", frame.ExecutedCLs);
            sb.Append(')');
        }
        if (!frame.ClosedByPop)
            sb.Append("  (OPEN)");
        sb.AppendLine();

        if (frame.Children is not null)
        {
            foreach (var c in frame.Children)
                AppendTree(sb, c, depth + 1);
        }
    }

    private static void AppendScopePath(StringBuilder sb, DebugScopeFrame leaf)
    {
        var stack = new Stack<string>();
        for (var f = leaf; f is not null && f.Name is not null; f = f.Parent)
            stack.Push(f.Name);
        bool first = true;
        while (stack.Count > 0) { if (!first) sb.Append(" / "); sb.Append(stack.Pop()); first = false; }
    }
}
