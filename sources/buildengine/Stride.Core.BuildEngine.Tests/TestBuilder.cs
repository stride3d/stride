// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

using System.Collections.Generic;
using System.Linq;
using Stride.Core.BuildEngine.Tests.Commands;

namespace Stride.Core.BuildEngine.Tests
{
    // These tests are deprecated, let's ignore them
    public class TestBuilder
    {
        [Fact(Skip = "BuildEngine tests are deprecated")]
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
                Assert.Equal(ResultStatus.Successful, step.Status);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
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
                Assert.Equal(ResultStatus.Successful, step.Status);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestSpawnAndAwaitCommands()
        {
            Utils.CleanContext();
            ExecuteSimpleBuilder(ResultStatus.Successful);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestRetrievingResultFromCache()
        {
            Utils.CleanContext();
            ExecuteSimpleBuilder(ResultStatus.Successful);
            TestCommand.ResetCounter();
            ExecuteSimpleBuilder(ResultStatus.NotTriggeredWasSuccessful);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
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

            Assert.Equal(1, successful);
            Assert.Equal(commands.Count - 1, notTriggeredWasSuccessful);
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
                Assert.Equal(expectedResult, step.Status);
        }

    }
}
