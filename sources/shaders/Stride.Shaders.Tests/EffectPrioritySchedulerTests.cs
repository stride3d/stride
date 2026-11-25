// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Stride.Shaders.Compiler;

namespace Stride.Shaders.Parsers.Tests;

/// <summary>
/// Game Studio compiles effects on an <see cref="EffectPriorityScheduler"/>: the waiting compilations run lowest priority first.
/// </summary>
public class EffectPrioritySchedulerTests
{
    private static readonly TimeSpan WaitTime = TimeSpan.FromSeconds(30);

    private static Task Run(Action action, TaskScheduler scheduler)
        => Task.Factory.StartNew(action, CancellationToken.None, TaskCreationOptions.None, scheduler);

    [Fact]
    public void WaitingTasksRunLowestPriorityFirst()
    {
        using var scheduler = new EffectPriorityScheduler(ThreadPriority.Normal, 1);
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();

        // Its one thread is busy while the others are queued
        var blocker = Run(() => { started.Set(); release.Wait(); }, scheduler);
        Assert.True(started.Wait(WaitTime));

        var order = new ConcurrentQueue<int>();
        var tasks = new[] { 5, -1, 0, 2 }.Select(priority => Run(() => order.Enqueue(priority), scheduler.GetOrCreatePriorityGroup(priority))).ToList();
        Assert.Equal(4, scheduler.QueuedTaskCount);

        release.Set();
        Assert.True(Task.WaitAll([blocker, .. tasks], WaitTime));
        Assert.Equal([-1, 0, 2, 5], order);
        Assert.Equal(0, scheduler.QueuedTaskCount);
    }

    [Fact]
    public void OnePriorityGivesOneGroup()
    {
        using var scheduler = new EffectPriorityScheduler(ThreadPriority.Normal, 1);

        Assert.Same(scheduler.GetOrCreatePriorityGroup(3), scheduler.GetOrCreatePriorityGroup(3));
        Assert.NotSame(scheduler.GetOrCreatePriorityGroup(3), scheduler.GetOrCreatePriorityGroup(4));
    }

    [Fact]
    public void NoThreadIsAnError()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EffectPriorityScheduler(ThreadPriority.Normal, 0));
    }
}
