// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Graphics;

/// <summary>
///   One per-scope counter bucket on a CommandList — written by the CL's recording thread,
///   merged into the device-wide scope tree at submit time.
/// </summary>
internal struct DebugPerScopeCounters
{
    public DebugScopeFrame Frame;
    public int Draws;
    public int Dispatches;
    public int Clears;
    public int Copies;
    public int Barriers;
}

/// <summary>
///   Tree node for a single <see cref="CommandList.BeginProfile"/> /<see cref="CommandList.EndProfile"/> pair.
///   Children are recorded in entry order so the tree preserves the temporal sequence of nested scopes.
///   Counters are aggregated from per-CL <c>localCounters</c> at submit time (under the queue lock).
/// </summary>
internal sealed class DebugScopeFrame
{
    public string Name;
    public DebugScopeFrame Parent;
    public List<DebugScopeFrame> Children;
    public int Draws;
    public int Dispatches;
    public int Clears;
    public int Copies;
    public int Barriers;
    /// <summary>
    ///   Number of distinct CommandLists whose work has been merged into this scope. Set by the
    ///   submit-time aggregator; <c>1</c> in the common single-CL case. Larger when parallel
    ///   recording (Dispatcher.For etc.) had multiple CLs touch the same scope.
    /// </summary>
    public int ExecutedCLs;
    /// <summary>
    ///   Validation errors/warnings attributed to this scope (only when running with the
    ///   ID3D12InfoQueue1 callback path, where the active leaf at message time is known).
    ///   The queue-poll fallback can't attribute, so these stay zero there.
    /// </summary>
    public int Errors;
    public int Warnings;
    /// <summary>
    ///   <see langword="true"/> once a matching <c>EndProfile</c> ran; <see langword="false"/> if the scope
    ///   was still open when its containing tree was dumped (= the active scope at error time).
    /// </summary>
    public bool ClosedByPop;

    public void AddChild(DebugScopeFrame child)
    {
        Children ??= new List<DebugScopeFrame>();
        Children.Add(child);
    }
}
