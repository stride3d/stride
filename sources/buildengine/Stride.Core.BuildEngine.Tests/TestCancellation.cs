// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using System.Collections.Generic;
using System.Threading;
using Stride.Core.BuildEngine.Tests.Commands;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine.Tests
{
    public class TestCancellation
    {
        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestCancellationToken()
        {
            Logger logger = Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();

            for (var i = 0; i < 8; ++i)
                commands.Add(new DummyBlockingCommand { Delay = 1000000 });
            for (var i = 0; i < 8; ++i)
                commands.Add(new DummyAwaitingCommand { Delay = 1000000 });

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            var cancelThread = new Thread(() =>
            {
                Thread.Sleep(1000);
                logger.Warning("Cancelling build!");
                builder.CancelBuild();
            });
            cancelThread.Start();
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps)
                Assert.Equal(ResultStatus.Cancelled, step.Status);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestCancelCallback()
        {
            Logger logger = Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();

            for (var i = 0; i < 10; ++i)
                commands.Add(new BlockedCommand());

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            var cancelThread = new Thread(() =>
            {
                Thread.Sleep(1000);
                logger.Warning("Cancelling build!");
                builder.CancelBuild();
            });
            cancelThread.Start();
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps)
                Assert.Equal(ResultStatus.Cancelled, step.Status);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestCancelPrerequisites()
        {
            Logger logger = Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var steps1 = new List<BuildStep>();
            var steps2 = new List<BuildStep>();
            var steps3 = new List<BuildStep>();

            for (var i = 0; i < 4; ++i)
            {
                BuildStep parentStep = builder.Root.Add(new DummyBlockingCommand { Delay = 10 });

                steps1.Add(parentStep);
                for (var j = 0; j < 4; ++j)
                {
                    BuildStep step = builder.Root.Add(new DummyBlockingCommand { Delay = 1000000 });
                    BuildStep.LinkBuildSteps(parentStep, step);
                    steps2.Add(step);
                    for (var k = 0; k < 4; ++k)
                    {
                        BuildStep childStep = builder.Root.Add(new DummyBlockingCommand { Delay = 1000000 });
                        BuildStep.LinkBuildSteps(step, childStep);
                        steps3.Add(childStep);
                    }
                }
            }

            var cancelThread = new Thread(() =>
            {
                Thread.Sleep(1000);
                logger.Warning("Cancelling build!");
                builder.CancelBuild();
            });
            cancelThread.Start();
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps1)
                Assert.Equal(ResultStatus.Successful, step.Status);
            foreach (BuildStep step in steps2)
                Assert.Equal(ResultStatus.Cancelled, step.Status);
            foreach (BuildStep step in steps3)
                Assert.Equal(ResultStatus.NotTriggeredPrerequisiteFailed, step.Status);
        }
    }
}
