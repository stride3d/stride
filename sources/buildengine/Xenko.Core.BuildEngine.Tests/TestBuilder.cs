// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using NUnit.Framework;

using System.Collections.Generic;
using System.Linq;
using Xenko.Core.BuildEngine.Tests.Commands;

namespace Xenko.Core.BuildEngine.Tests
{
    // These tests are deprecated, let's ignore them
    [TestFixture, Ignore("BuildEngine tests are deprecated")]
    public class TestBuilder
    {
        [Test]
        public void TestBlockingCommands()
        {
            Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();

            for (int i = 0; i < 100; ++i)
                commands.Add(new DummyBlockingCommand { Delay = 100 });

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps)
                Assert.That(step.Status, Is.EqualTo(ResultStatus.Successful));
        }

        [Test]
        public void TestAwaitingCommands()
        {
            Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();
            
            for (int i = 0; i < 100; ++i)
                commands.Add(new DummyAwaitingCommand { Delay = 500 });

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps)
                Assert.That(step.Status, Is.EqualTo(ResultStatus.Successful));
        }

        [Test]
        public void TestSpawnAndAwaitCommands()
        {
            Utils.CleanContext();
            ExecuteSimpleBuilder(ResultStatus.Successful);
        }

        [Test]
        public void TestRetrievingResultFromCache()
        {
            Utils.CleanContext();
            ExecuteSimpleBuilder(ResultStatus.Successful);
            TestCommand.ResetCounter();
            ExecuteSimpleBuilder(ResultStatus.NotTriggeredWasSuccessful);
        }

        [Test]
        public void TestSameCommandParallelExecution()
        {
            Utils.CleanContext();
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();

            for (int i = 0; i < 100; ++i)
            {
                TestCommand.ResetCounter();
                commands.Add(new DummyBlockingCommand { Delay = 100 });
            }

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            builder.Run(Builder.Mode.Build);

            int successful = 0;
            int notTriggeredWasSuccessful = 0;

            foreach (BuildStep step in steps)
            {
                if (step.Status == ResultStatus.Successful)
                    ++successful;
                if (step.Status == ResultStatus.NotTriggeredWasSuccessful)
                    ++notTriggeredWasSuccessful;
            }

            Assert.That(successful, Is.EqualTo(1));
            Assert.That(notTriggeredWasSuccessful, Is.EqualTo(commands.Count - 1));
        }

        private static void ExecuteSimpleBuilder(ResultStatus expectedResult)
        {
            var builder = Utils.CreateBuilder(false);
            var commands = new List<Command>();

            for (int i = 0; i < 10; ++i)
                commands.Add(new DummyBlockingCommand { Delay = 100 });

            IEnumerable<BuildStep> steps = builder.Root.Add(commands);
            builder.Run(Builder.Mode.Build);

            foreach (BuildStep step in steps)
                Assert.That(step.Status, Is.EqualTo(expectedResult));
        }

    }
}
