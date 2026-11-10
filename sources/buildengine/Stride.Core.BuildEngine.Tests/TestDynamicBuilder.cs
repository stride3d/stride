// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Stride.Core.BuildEngine.Tests;

public class TestDynamicBuilder
{
    private static readonly TimeSpan DisposeTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void DisposingRightAfterStartStopsTheBuilder()
    {
        Utils.CleanContext();

        // The editor closes a session that just opened: the builder is not running yet, so it cannot be cancelled
        for (var i = 0; i < 100; ++i)
        {
            var dynamicBuilder = new DynamicBuilder(Utils.CreateBuilder(false), new AnonymousBuildStepProvider(_ => null), "TestDynamicBuilder");
            dynamicBuilder.Start();
            Assert.True(Task.Run(dynamicBuilder.Dispose).Wait(DisposeTimeout), $"Dispose did not return (iteration {i}).");
        }
    }

    [Fact]
    public void DisposingAWaitingBuilderStopsIt()
    {
        Utils.CleanContext();
        var builder = Utils.CreateBuilder(false);
        var dynamicBuilder = new DynamicBuilder(builder, new AnonymousBuildStepProvider(_ => null), "TestDynamicBuilder");
        dynamicBuilder.Start();

        var clock = Stopwatch.StartNew();
        while (!builder.IsRunning)
        {
            Assert.True(clock.Elapsed < DisposeTimeout, "The builder did not start.");
            Thread.Sleep(10);
        }
        // Let the dynamic build step reach its wait for new work
        Thread.Sleep(100);

        Assert.True(Task.Run(dynamicBuilder.Dispose).Wait(DisposeTimeout), "Dispose did not return.");
    }
}
