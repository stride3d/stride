// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;

namespace Xenko.Core.Assets.Tests
{
  
    public class TestPackage : TestBase
    {
        [Fact]
        public void TestBasicPackageCreateSaveLoad()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            var dirPath = DirectoryTestBase + @"TestBasicPackageCreateSaveLoad";

            string testGenerated1 = Path.Combine(dirPath, "TestPackage_TestBasicPackageCreateSaveLoad_Generated1.xkpkg");
            string testGenerated2 = Path.Combine(dirPath,"TestPackage_TestBasicPackageCreateSaveLoad_Generated2.xkpkg");
            string referenceFilePath = Path.Combine(dirPath,"TestPackage_TestBasicPackageCreateSaveLoad_Reference.xkpkg");

            // Force the PackageId to be the same each time we run the test
            // Usually the PackageId is unique and generated each time we create a new project
            var project = new Package { Id = Guid.Empty, FullPath = testGenerated1 };
            var sharedProfile = new PackageProfile("Shared", new AssetFolder("."));
            project.Profiles.Add(sharedProfile);
            var projectReference = new ProjectReference(Guid.Empty, Path.Combine(dirPath, "test.csproj"), ProjectType.Executable);
            sharedProfile.ProjectReferences.Add(projectReference);

            var session = new PackageSession(project);
            // Write the solution when saving
            session.SolutionPath = Path.Combine(dirPath, "TestPackage_TestBasicPackageCreateSaveLoad_Generated1.sln");

            // Delete the solution before saving it 
            if (File.Exists(session.SolutionPath))
            {
                File.Delete(session.SolutionPath);
            }

            var result = new LoggerResult();
            session.Save(result);
            Assert.False(result.HasErrors);

            // Reload the raw package and if UFile and UDirectory were saved relative
            var rawPackage = AssetFileSerializer.Load<Package>(testGenerated1).Asset;
            var rawPackageSharedProfile = rawPackage.Profiles.FirstOrDefault();
            Assert.NotNull(rawPackageSharedProfile);
            var rawSourceFolder = rawPackage.Profiles.First().AssetFolders.FirstOrDefault();
            Assert.NotNull(rawSourceFolder);
            Assert.Equal(".", (string)rawSourceFolder.Path);
            Assert.Equal("test.csproj", (string)rawPackageSharedProfile.ProjectReferences[0].Location);

            // Reload the package directly from the xkpkg
            var project2Result = PackageSession.Load(testGenerated1);
            AssertResult(project2Result);
            var project2 = project2Result.Session.LocalPackages.FirstOrDefault();
            Assert.NotNull(project2);
            Assert.Equal(project.Id, project2.Id);
            Assert.True(project2.Profiles.Count > 0);
            Assert.True(project2.Profiles.First().AssetFolders.Count > 0);
            Assert.Equal(project2, project2Result.Session.CurrentPackage); // Check that the current package is setup when loading a single package
            var sourceFolder = project.Profiles.First().AssetFolders.First().Path;
            Assert.Equal(sourceFolder, project2.Profiles.First().AssetFolders.First().Path);

            // Reload the package from the sln
            var sessionResult = PackageSession.Load(session.SolutionPath);
            Assert.False(sessionResult.HasErrors);

            var sessionReload = sessionResult.Session;
            Assert.Single(sessionReload.LocalPackages);
            Assert.Equal(project.Id, sessionReload.LocalPackages.First().Id);
            Assert.Single(sessionReload.LocalPackages.First().Profiles);

            var sharedProfileReload = sessionReload.LocalPackages.First().Profiles.First();
            Assert.Single(sharedProfileReload.ProjectReferences);
            Assert.Equal(projectReference, sharedProfileReload.ProjectReferences[0]);
        }

        [Fact]
        public void TestPackageAndAssetIdChange()
        {
            var project = new Package();
            var assetItem = new AssetItem("test", new AssetObjectTest());
            var asset = assetItem.Asset;
            project.Assets.Add(assetItem);

            // Can't change an asset id once it is loaded into a project
            Assert.Throws<InvalidOperationException>(() => asset.Id = AssetId.Empty);

            project.Assets.Remove(assetItem);

            // Can change Id once the asset was removed from the project
            asset.Id = AssetId.Empty;
        }

        [Fact(Skip = "Need check")]
        public void TestPackageLoadingWithAssets()
        {
            var basePath = Path.Combine(DirectoryTestBase, @"TestPackage");
            var projectPath = Path.Combine(basePath, "TestPackageLoadingWithAssets.xkpkg");

            var sessionResult = PackageSession.Load(projectPath);
            AssertResult(sessionResult);
            var session = sessionResult.Session;

            var rootPackageId = new Guid("4102BF96-796D-4800-9983-9C227FAB7BBD");

            var project = session.Packages.Find(rootPackageId);
            Assert.NotNull(project);
            Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

            Assert.True(1 == project.LocalDependencies.Count, "Expecting subproject");

            Assert.NotEqual(AssetId.Empty, project.Assets.First().Id);

            // Check for UPathRelativeTo
            var profile = project.Profiles.FirstOrDefault();
            Assert.NotNull(profile);
            var folder = profile.AssetFolders.FirstOrDefault();
            Assert.NotNull(folder);
            Assert.NotNull(folder.Path);
            Assert.NotNull(folder.Path.IsAbsolute);

            // Save project back to disk on a different location
            project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackage2\TestPackage2.xkpkg");
            var subPackage = session.Packages.Find(Guid.Parse("281321F0-7664-4523-B1DC-3CFC26F80F77"));
            subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackage2\SubPackage\SubPackage.xkpkg");
            var result = new LoggerResult();
            session.Save(result);

            var project2Result = PackageSession.Load(DirectoryTestBase + @"TestPackage2\TestPackage2.xkpkg");
            AssertResult(project2Result);
            var project2 = project2Result.Session.Packages.Find(rootPackageId);
            Assert.NotNull(project2);
            Assert.Equal(3, project2.Assets.Count);
        }

        [Fact(Skip = "Need check")]
        public void TestMovingAssets()
        {
            var basePath = Path.Combine(DirectoryTestBase, @"TestPackage");
            var projectPath = Path.Combine(basePath, "TestPackageLoadingWithAssets.xkpkg");

            var rootPackageId = new Guid("4102BF96-796D-4800-9983-9C227FAB7BBD");
            var testAssetId = new AssetId("C2D80EF9-2160-43B2-9FEE-A19A903A0BE0");

            // Load the project from the original location
            var sessionResult1 = PackageSession.Load(projectPath);
            {
                AssertResult(sessionResult1);
                var session = sessionResult1.Session;
                var project = session.Packages.Find(rootPackageId);
                Assert.NotNull(project);

                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Find the second asset that was referencing the changed asset
                var testAssetItem = session.FindAsset(testAssetId);
                Assert.NotNull(testAssetItem);

                var testAsset = (AssetObjectTest)testAssetItem.Asset;
                Assert.Equal(new UFile(Path.Combine(basePath, "SubFolder/TestAsset.xktest")), testAsset.RawAsset);

                // First save a copy of the project to TestPackageMovingAssets1
                project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets1\TestPackage2.xkpkg");
                var subPackage = session.Packages.Find(Guid.Parse("281321F0-7664-4523-B1DC-3CFC26F80F77"));
                subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets1\SubPackage\SubPackage.xkpkg");
                var result = new LoggerResult();
                session.Save(result);
            }

            // Reload the project from the location TestPackageMovingAssets1
            var sessionResult2 = PackageSession.Load(DirectoryTestBase + @"TestPackageMovingAssets1\TestPackage2.xkpkg");
            {
                AssertResult(sessionResult2);
                var session = sessionResult2.Session;
                var project = session.Packages.Find(rootPackageId);
                Assert.NotNull(project);
                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Move asset into a different directory
                var assetItem = project.Assets.Find(new AssetId("28D0DE9C-8913-41B1-B50E-848DD8A7AF65"));
                Assert.NotNull(assetItem);
                project.Assets.Remove(assetItem);

                var newAssetItem = new AssetItem("subTest/TestAsset2", assetItem.Asset);
                project.Assets.Add(newAssetItem);

                // Save the whole project to a different location
                project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets2\TestPackage2.xkpkg");
                var subPackage = session.Packages.Find(Guid.Parse("281321F0-7664-4523-B1DC-3CFC26F80F77"));
                subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets2\SubPackage\SubPackage.xkpkg");
                var result = new LoggerResult();
                session.Save(result);
            }

            // Reload the project from location TestPackageMovingAssets2
            var sessionResult3 = PackageSession.Load(DirectoryTestBase + @"TestPackageMovingAssets2\TestPackage2.xkpkg");
            {
                AssertResult(sessionResult3);
                var session = sessionResult3.Session;
                var project = session.Packages.Find(rootPackageId);
                Assert.NotNull(project);
                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Find the second asset that was referencing the changed asset
                var assetItemChanged = session.FindAsset(testAssetId);
                Assert.NotNull(assetItemChanged);

                // Check that references were correctly updated
                var assetChanged = (AssetObjectTest)assetItemChanged.Asset;
                Assert.Equal(new UFile(Path.Combine(Environment.CurrentDirectory, DirectoryTestBase) + "/TestPackage/SubFolder/TestAsset.xktest"), assetChanged.RawAsset);
                var text = File.ReadAllText(assetItemChanged.FullPath);
                Assert.Contains("../../TestPackage/SubFolder/TestAsset.xktest", text);

                Assert.Equal("subTest/TestAsset2", assetChanged.Reference.Location);
            }
        }

        private void AssertResult(LoggerResult log)
        {
            foreach (var logMessage in log.Messages)
            {
                Console.WriteLine(logMessage);
            }
            Assert.False(log.HasErrors);
        }

        static void Main()
        {
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < 10; i++)
            {
                var session = PackageSession.Load(@"E:\Code\SengokuRun\SengokuRun\WindowsLauncher\GameAssets\Assets.xkpkg");
            }
            var elapsed = clock.ElapsedMilliseconds;
            Console.WriteLine("{0}ms", elapsed);
        }
    }
}
