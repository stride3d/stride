// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;
using Xunit;

namespace Stride.Core.BuildEngine.Tests;

public class TestListBuildStep
{
    private static readonly TimeSpan RunTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void StepsThatEndWhileTheListStartsWaitingForThemEndTheList()
    {
        Utils.CleanContext();

        // The steps end at once on the other worker threads, while the list subscribes to their end one after the other
        for (var i = 0; i < 200; ++i)
        {
            var builder = Utils.CreateBuilder(false);
            for (var j = 0; j < 50; ++j)
                builder.Root.Add(new ImmediateBuildStep());

            var run = Task.Run(() => builder.Run(Builder.Mode.Build, false));
            Assert.True(run.Wait(RunTimeout), $"The build did not end (run {i}).");
            Assert.Equal(BuildResultCode.Successful, run.Result);
        }
    }

    private sealed class ImmediateBuildStep : BuildStep
    {
        public override string Title => nameof(ImmediateBuildStep);

        public override Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext) => Task.FromResult(ResultStatus.Successful);

        public override string ToString() => Title;
    }
}
