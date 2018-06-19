// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading;
using NUnit.Framework;
using Xenko.Core.BuildEngine.Tests.Commands;
using Xenko.Core.Diagnostics;

namespace Xenko.Core.BuildEngine.Tests
{
    [TestFixture, Ignore("BuildEngine tests are deprecated")]
    class TestDependencies
    {
        [Test]
        public void TestCommandDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new DummyBlockingCommand { Delay = 100 }, new DummyBlockingCommand { Delay = 100 }, ResultStatus.Successful, ResultStatus.Successful);
        }

        [Test]
        public void TestFailedDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new FailingCommand(), new DummyBlockingCommand { Delay = 100 }, ResultStatus.Failed, ResultStatus.NotTriggeredPrerequisiteFailed);
        }

        [Test]
        public void TestCancelledDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new DummyBlockingCommand { Delay = 1000000 }, new DummyBlockingCommand { Delay = 100 }, ResultStatus.Cancelled, ResultStatus.NotTriggeredPrerequisiteFailed, true);
        }

        [Test]
        public void TestExceptionDependencies()
        {
            Logger logger = Utils.CleanContext();
            CommandDependenciesCommon(logger, new ExceptionCommand(), new DummyBlockingCommand { Delay = 100 }, ResultStatus.Failed, ResultStatus.NotTriggeredPrerequisiteFailed);
        }

        [Test]
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

            Assert.That(firstStep.Status, Is.EqualTo(ResultStatus.Successful));
            Assert.That(parentStep.Status, Is.EqualTo(ResultStatus.Successful));
            Assert.That(step1.Status, Is.EqualTo(ResultStatus.Successful));
            Assert.That(step2.Status, Is.EqualTo(ResultStatus.Successful));
            Assert.That(finalStep.Status, Is.EqualTo(ResultStatus.Successful));
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

            Assert.That(step1.Status, Is.EqualTo(expectedStatus1));
            Assert.That(step2.Status, Is.EqualTo(expectedStatus2));
        }

    }
}
