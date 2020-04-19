// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using Xunit;
using Stride.Core.BuildEngine.Tests.Commands;
using Stride.Core.Diagnostics;

namespace Stride.Core.BuildEngine.Tests
{
    public class TestDependencies
    {
        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestCommandDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new DummyBlockingCommand { Delay = 100 }, new DummyBlockingCommand { Delay = 100 }, ResultStatus.Successful, ResultStatus.Successful);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestFailedDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new FailingCommand(), new DummyBlockingCommand { Delay = 100 }, ResultStatus.Failed, ResultStatus.NotTriggeredPrerequisiteFailed);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestCancelledDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new DummyBlockingCommand { Delay = 1000000 }, new DummyBlockingCommand { Delay = 100 }, ResultStatus.Cancelled, ResultStatus.NotTriggeredPrerequisiteFailed, true);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestExceptionDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new ExceptionCommand(), new DummyBlockingCommand { Delay = 100 }, ResultStatus.Failed, ResultStatus.NotTriggeredPrerequisiteFailed);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestMultipleDependencies()
        {
            Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);

            var firstStep = builder.Root.Add(new DummyBlockingCommand { Delay = 100 });
            var parentStep = builder.Root.Add(new DummyBlockingCommand { Delay = 100 });
            var step1 = builder.Root.Add(new DummyBlockingCommand { Delay = 100 });
            var step2 = builder.Root.Add(new DummyBlockingCommand { Delay = 200 });
            var finalStep = builder.Root.Add(new DummyBlockingCommand { Delay = 100 });

            BuildStep.LinkBuildSteps(firstStep, parentStep);
            BuildStep.LinkBuildSteps(parentStep, step1);
            BuildStep.LinkBuildSteps(parentStep, step2);
            BuildStep.LinkBuildSteps(step1, finalStep);
            BuildStep.LinkBuildSteps(step2, finalStep);

            builder.Run(Builder.Mode.Build);

            Assert.Equal(ResultStatus.Successful, firstStep.Status);
            Assert.Equal(ResultStatus.Successful, parentStep.Status);
            Assert.Equal(ResultStatus.Successful, step1.Status);
            Assert.Equal(ResultStatus.Successful, step2.Status);
            Assert.Equal(ResultStatus.Successful, finalStep.Status);
        }

        private static void CommandDependenciesCommon(Logger logger, Command command1, Command command2, ResultStatus expectedStatus1, ResultStatus expectedStatus2, bool cancelled = false)
        {
            var builder = Utils.CreateBuilder(false);

            var step2 = builder.Root.Add(command2);
            var step1 = builder.Root.Add(command1);

            BuildStep.LinkBuildSteps(step1, step2);

            if (cancelled)
            {
                var cancelThread = new Thread(() =>
                {
                    Thread.Sleep(1000);
                    logger.Warning("Cancelling build!");
                    builder.CancelBuild();
                });
                cancelThread.Start();
            }

            builder.Run(Builder.Mode.Build);

            Assert.Equal(expectedStatus1, step1.Status);
            Assert.Equal(expectedStatus2, step2.Status);
        }

    }
}
