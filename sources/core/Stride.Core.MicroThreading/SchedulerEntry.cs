// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.MicroThreading;

/// <summary>
/// Either a microthread or an action with priority.
/// </summary>
internal class SchedulerEntry
{
    public long Priority;
    public int BinarySearchHelper;
    public Scheduler.ExecutionQueue? CurrentQueue;
    public Scheduler.ExecutionQueue? PreviousQueue;
    public Action? Action;
    public MicroThread? MicroThread;
    public object? Token;
    public ProfilingKey? ProfilingKey;
}
