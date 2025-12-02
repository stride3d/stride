// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.MicroThreading;

/// <summary>
/// Either a microthread or an action with priority.
/// </summary>
internal record struct SchedulerEntry
{
    public int BinarySearchHelper { get; init; }
    public Scheduler.ExecutionQueue? PreviousQueue { get; init; }
    public Action? Action { get; init; }
    public MicroThread? MicroThread { get; init; }
    public object? Token { get; init; }
    public ProfilingKey? ProfilingKey { get; init; }
}
