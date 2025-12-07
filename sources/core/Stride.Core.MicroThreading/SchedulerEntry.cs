// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.MicroThreading;

/// <summary>
/// Either a microthread or an action with priority.
/// </summary>
internal class SchedulerEntry
{
    /// <summary>
    /// A hint to accelerate searches in collections when inserting at the edges,
    /// should always have a value greater than the one at n-1.
    /// </summary>
    internal int BinarySearchHelper;

    /// <summary>
    /// The queue this is scheduled to run on, or null if it hasn't been scheduled
    /// </summary>
    internal Scheduler.ExecutionQueue? CurrentQueue;

    /// <summary>
    /// The queue this was previously scheduled to run on, or null if it has never been scheduled
    /// </summary>
    /// <remarks>
    /// Provides additional acceleration for entries that are frequently scheduled
    /// </remarks>
    internal Scheduler.ExecutionQueue? PreviousQueue;

    /// <summary>
    /// The action this entry will run
    /// </summary>
    /// <remarks>This is mutually exclusive with <see cref="MicroThread"/>, where <see cref="MicroThread"/> would run in its stead when set</remarks>
    public Action? Action;

    /// <summary>
    /// The microthread whose <see cref="MicroThread.Callbacks"/> this entry will run
    /// </summary>
    /// <remarks>This is mutually exclusive with <see cref="Action"/>, where <see cref="MicroThread"/> would run in its stead when set</remarks>
    public MicroThread? MicroThread;

    /// <summary>
    /// An object you can attach to this entry for any purpose
    /// </summary>
    public object? Token;

    /// <summary>
    /// The profiling key to use while executing this entry
    /// </summary>
    /// <remarks>
    /// <see cref="MicroThreadProfilingKeys.ProfilingKey"/> will be used when this is null
    /// </remarks>
    public ProfilingKey? ProfilingKey;
}
