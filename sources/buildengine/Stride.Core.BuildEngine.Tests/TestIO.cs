// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;
using Stride.Core.Storage;
using Stride.Core.BuildEngine.Tests.Commands;
using Stride.Core.IO;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.BuildEngine.Tests
{
    public class TestIO
    {
        private static void CommonSingleOutput(bool executeRemotely)
        {
            var builder = Utils.CreateBuilder(true);
            CommandBuildStep step = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1", ExecuteRemotely = executeRemotely });
            builder.Run(Builder.Mode.Build);
            builder.WriteIndexFile(false);

            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url1", out outputId);
            Assert.True(objectIdFound);
            Assert.Equal(step.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url1")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestSingleOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{1A89566A-D39D-4858-8F65-12FA64C03DED}");
            CommonSingleOutput(false);
        }

        [Fact(Skip = "Need check")]
        public void TestRemoteSingleOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{78F451F2-CA9D-40C7-A084-396B4E87D1FF}");
            CommonSingleOutput(true);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestTwoCommandsSameOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{3F3C2132-911E-465C-B98F-020278ED4512}");

            var builder = Utils.CreateBuilder(true);
            CommandBuildStep step = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            CommandBuildStep childStep = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            BuildStep.LinkBuildSteps(step, childStep);
            builder.Run(Builder.Mode.Build);
            builder.WriteIndexFile(false);

            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step.Result.OutputObjects.Keys);
            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), childStep.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url1", out outputId);
            Assert.True(objectIdFound);
            Assert.Equal(childStep.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url1")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestSingleCommandTwiceWithInputChange()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{71938BC2-6876-406E-84E9-4F4E862651D5}");

            var builder1 = Utils.CreateBuilder(false);
            CommandBuildStep step1 = builder1.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            builder1.Run(Builder.Mode.Build);

            // Modifying input file
            Utils.GenerateSourceFile("input1", "{5794B336-55F9-400A-B99D-DA61C9F09CCE}", true);

            var builder2 = Utils.CreateBuilder(true);
            CommandBuildStep step2 = builder2.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            builder2.Run(Builder.Mode.Build);
            builder2.WriteIndexFile(false);

            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step1.Result.OutputObjects.Keys);
            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step2.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url1", out outputId);
            Assert.True(objectIdFound);

            Assert.Equal(ResultStatus.Successful, step1.Status);
            Assert.Equal(ResultStatus.Successful, step2.Status);
            Assert.NotEqual(step1.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url1")], outputId);
            Assert.Equal(step2.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url1")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestUseBuildCacheOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{32E4EDF4-E8AA-4D13-B111-9BD8AA2A8B07}");

            var builder1 = Utils.CreateBuilder(false);
            builder1.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            builder1.Run(Builder.Mode.Build);

            var builder2 = Utils.CreateBuilder(true);
            CommandBuildStep step = builder2.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            builder2.Run(Builder.Mode.Build);
            builder2.WriteIndexFile(false);

            Assert.Equal(ResultStatus.NotTriggeredWasSuccessful, step.Status);
            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url1", out outputId);
            Assert.True(objectIdFound);
            Assert.Equal(step.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url1")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestSpawnCommandOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{05D6BCFA-B1FE-4AD1-920F-0352A6DEC02D}");
            Utils.GenerateSourceFile("input2", "{B9D01D6C-4048-4814-A2DF-9D317A492B10}");

            var builder = Utils.CreateBuilder(true);
            var command = new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" };
            command.CommandsToSpawn.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input2")), OutputUrl = "/db/url2" });
            CommandBuildStep step = builder.Root.Add(command);
            builder.Run(Builder.Mode.Build);
            builder.WriteIndexFile(false);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url2", out outputId);
            Assert.True(objectIdFound);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestInputFromPreviousOutput()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{E7635471-8551-4C80-9E37-A1EBAFC3869E}");

            var builder = Utils.CreateBuilder(true);
            CommandBuildStep step = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            CommandBuildStep childStep = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url2" });
            BuildStep.LinkBuildSteps(step, childStep);
            builder.Run(Builder.Mode.Build);
            builder.WriteIndexFile(false);

            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step.Result.OutputObjects.Keys);
            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url2"), childStep.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url2", out outputId);
            Assert.True(objectIdFound);
            Assert.Equal(childStep.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url2")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestInputFromPreviousOutputWithCache()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{E60A3248-B4D8-43F6-9A73-975FD9A653FC}");

            var builder1 = Utils.CreateBuilder(false);
            CommandBuildStep step = builder1.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            CommandBuildStep childStep = builder1.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url2" });
            BuildStep.LinkBuildSteps(step, childStep);
            builder1.Run(Builder.Mode.Build);

            var builder2 = Utils.CreateBuilder(true);
            step = builder2.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" });
            childStep = builder2.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url2" });
            BuildStep.LinkBuildSteps(step, childStep);
            builder2.Run(Builder.Mode.Build);
            builder2.WriteIndexFile(false);

            Assert.Equal(ResultStatus.NotTriggeredWasSuccessful, step.Status);
            Assert.Equal(ResultStatus.NotTriggeredWasSuccessful, childStep.Status);

            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url1"), step.Result.OutputObjects.Keys);
            Assert.Contains(new ObjectUrl(UrlType.Content, "/db/url2"), childStep.Result.OutputObjects.Keys);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId outputId;
            bool objectIdFound = indexMap.TryGetValue("/db/url2", out outputId);
            Assert.True(objectIdFound);
            Assert.Equal(childStep.Result.OutputObjects[new ObjectUrl(UrlType.Content, "/db/url2")], outputId);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestInputDependencies()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{A7246DF6-3A68-40E2-BA58-6C9A0EFF552B}");
            Utils.GenerateSourceFile("inputDeps", "{8EE7A4BC-88E1-4CC8-B03F-1E6EA8B23955}");

            var builder = Utils.CreateBuilder(false);
            var inputDep = new ObjectUrl(UrlType.File, Utils.GetSourcePath("inputDeps"));
            var command = new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1" };
            command.InputDependencies.Add(inputDep);
            CommandBuildStep step = builder.Root.Add(command);
            builder.Run(Builder.Mode.Build);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.UseTransaction = true;
            indexMap.LoadNewValues();

            ObjectId inputDepId;
            bool inputDepFound = step.Result.InputDependencyVersions.TryGetValue(inputDep, out inputDepId);
            Assert.True(inputDepFound);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestInputDependenciesChange()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{A7246DF6-3A68-40E2-BA58-6C9A0EFF552B}");
            Utils.GenerateSourceFile("inputDeps", "{8EE7A4BC-88E1-4CC8-B03F-1E6EA8B23955}");
            var inputDep = new ObjectUrl(UrlType.File, Utils.GetSourcePath("inputDeps"));

            var builder1 = Utils.CreateBuilder(false);
            CommandBuildStep step1 = builder1.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            builder1.Run(Builder.Mode.Build);

            Utils.GenerateSourceFile("inputDeps", "{E505A61B-5F2A-4BB8-8F6C-3788C76BAE5F}", true);
            
            var builder2 = Utils.CreateBuilder(false);
            CommandBuildStep step2 = builder2.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            builder2.Run(Builder.Mode.Build);

            var indexMap = ContentIndexMap.Load(VirtualFileSystem.ApplicationDatabaseIndexPath);
            indexMap.LoadNewValues();

            Assert.Equal(ResultStatus.Successful, step1.Status);
            Assert.Equal(ResultStatus.Successful, step2.Status);
            ObjectId inputDepId1;
            ObjectId inputDepId2;
            Assert.True(step1.Result.InputDependencyVersions.TryGetValue(inputDep, out inputDepId1));
            Assert.True(step2.Result.InputDependencyVersions.TryGetValue(inputDep, out inputDepId2));
            Assert.NotEqual(inputDepId1, inputDepId2);
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestConcurrencyReadWriteAccess()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{99D73F8B-587A-4869-97AE-4A7185D88AC9}");
            var inputDep = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1"));

            var builder = Utils.CreateBuilder(false);
            CommandBuildStep step = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            CommandBuildStep concurrencyStep1 = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            CommandBuildStep concurrencyStep2 = builder.Root.Add(new InputOutputTestCommand { Delay = 150, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url2", InputDependencies = { inputDep } });
            BuildStep.LinkBuildSteps(step, concurrencyStep1);
            BuildStep.LinkBuildSteps(step, concurrencyStep2);

            builder.Run(Builder.Mode.Build);
            var logger = (LoggerResult)builder.Logger;
            Assert.Contains("Command InputOutputTestCommand /db/url1 > /db/url1 is writing /db/url1 while command InputOutputTestCommand /db/url1 > /db/url2 is reading it", logger.Messages.Select(x => x.Text));
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestConcurrencyWriteAccess()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{99D73F8B-587A-4869-97AE-4A7185D88AC9}");
            Utils.GenerateSourceFile("input2", "{9FEABA51-4CE6-4DB0-9866-45A7492FD1B7}");
            var inputDep = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1"));

            var builder = Utils.CreateBuilder(false);
            CommandBuildStep step1 = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1")), OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            CommandBuildStep step2 = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input2")), OutputUrl = "/db/url2", InputDependencies = { inputDep } });
            CommandBuildStep concurrencyStep1 = builder.Root.Add(new InputOutputTestCommand { Delay = 100, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/url", InputDependencies = { inputDep } });
            CommandBuildStep concurrencyStep2 = builder.Root.Add(new InputOutputTestCommand { Delay = 150, Source = new ObjectUrl(UrlType.Content, "/db/url2"), OutputUrl = "/db/url", InputDependencies = { inputDep } });
            BuildStep.LinkBuildSteps(step1, concurrencyStep1);
            BuildStep.LinkBuildSteps(step2, concurrencyStep2);

            builder.Run(Builder.Mode.Build);
            var logger = (LoggerResult)builder.Logger;
            Assert.Contains(logger.Messages, x => x.Text.Contains("Commands InputOutputTestCommand /db/url2 > /db/url and InputOutputTestCommand /db/url1 > /db/url are both writing /db/url at the same time"));
        }

        [Fact(Skip = "BuildEngine tests are deprecated")]
        public void TestConcurrencyReadWriteAccess2()
        {
            Utils.CleanContext();
            Utils.GenerateSourceFile("input1", "{99D73F8B-587A-4869-97AE-4A7185D88AC9}");
            var inputDep = new ObjectUrl(UrlType.File, Utils.GetSourcePath("input1"));

            var buildStepList1 = new ListBuildStep();
            var step = new ListBuildStep();
            step.Add(new InputOutputTestCommand { Delay = 100, Source = inputDep, OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            buildStepList1.Add(step);
            buildStepList1.Add(new InputOutputTestCommand { Delay = 1500, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/dump1", InputDependencies = { inputDep } });
            var buildStepList2 = new ListBuildStep();
            step = new ListBuildStep();
            step.Add(new InputOutputTestCommand { Delay = 100, Source = inputDep, OutputUrl = "/db/url1", InputDependencies = { inputDep } });
            buildStepList2.Add(step);
            buildStepList2.Add(new InputOutputTestCommand { Delay = 1500, Source = new ObjectUrl(UrlType.Content, "/db/url1"), OutputUrl = "/db/dump2", InputDependencies = { inputDep } });

            var builder = Utils.CreateBuilder(false);
            builder.ThreadCount = 1;
            builder.Root.Add(buildStepList1);
            builder.Root.Add(buildStepList2);

            var buildResult = builder.Run(Builder.Mode.Build);
            Assert.Equal(BuildResultCode.Successful, buildResult);
        }
    }
}
