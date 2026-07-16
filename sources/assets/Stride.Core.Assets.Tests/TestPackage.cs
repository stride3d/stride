// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests
{
  
    public class TestPackage : TestBase
    {
        [Fact]
        public void TestDirectLoadDerivesPackageName()
        {
            var dirPath = DirectoryTestBase + @"TestDirectLoadDerivesPackageName";
            Directory.CreateDirectory(dirPath);
            var packagePath = Path.Combine(dirPath, "MyPkgName.sdpkg");

            // .sdpkg never stores a name, so write a nameless package then load it without a session.
            AssetFileSerializer.Save(packagePath, new Package(), null);

            // A direct Package.Load must still derive Meta.Name from the file name (was null before).
            var loadResult = new LoggerResult();
            var package = Package.Load(loadResult, packagePath);
            Assert.False(loadResult.HasErrors);
            Assert.NotNull(package);
            Assert.Equal("MyPkgName", package.Meta.Name);
        }

        [Fact(Skip = "Need check: we don't work with package directly anymore, they are considered external")]
        public void TestBasicPackageCreateSaveLoad()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            var dirPath = DirectoryTestBase + @"TestBasicPackageCreateSaveLoad";

            string testGenerated1 = Path.Combine(dirPath, "TestPackage_TestBasicPackageCreateSaveLoad_Generated1.sdpkg");

            // Force the PackageId to be the same each time we run the test
            // Usually the PackageId is unique and generated each time we create a new project
            var project = new Package { FullPath = testGenerated1 };
            project.AssetFolders.Clear();
            project.AssetFolders.Add(new AssetFolder("."));

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
            var rawSourceFolder = rawPackage.AssetFolders.FirstOrDefault();
            Assert.NotNull(rawSourceFolder);
            Assert.Equal(".", (string)rawSourceFolder.Path);

            // Reload the package directly from the sdpkg
            var project2Result = PackageSession.Load(testGenerated1);
            AssertResult(project2Result);
            var project2 = project2Result.Session.LocalPackages.FirstOrDefault();
            Assert.NotNull(project2);
            Assert.True(project2.AssetFolders.Count > 0);
            var sourceFolder = project.AssetFolders.First().Path;
            Assert.Equal(sourceFolder, project2.AssetFolders.First().Path);
        }

        [Fact]
        public void TestNamespacedAssetMoveDeletesOldFileOnSave()
        {
            var dirPath = Path.Combine(DirectoryTestBase, "TestNamespacedAssetMoveDeletesOldFileOnSave");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(dirPath);

            // A SolutionProject container (standalone packages are read-only, never saved) with a
            // resolved namespace, like the editor loader produces.
            var package = new Package { FullPath = Path.Combine(dirPath, "MyGame.sdpkg") };
            package.AssetFolders.Add(new AssetFolder("Assets"));
            var project = new SolutionProject(package, Guid.NewGuid(), Path.Combine(dirPath, "MyGame.csproj")) { AssetNamespace = "MyGame" };
            var session = new PackageSession();
            session.Projects.Add(project);
            var assetItem = new AssetItem("Ground", new AssetObjectTest());
            package.Assets.Add(assetItem);
            assetItem.IsDirty = true;
            Assert.Equal("/MyGame/Ground", assetItem.Location.FullPath);

            var result = new LoggerResult();
            session.Save(result);
            Assert.False(result.HasErrors);
            var oldFile = Path.Combine(dirPath, "Assets", "Ground.sdtest");
            Assert.True(File.Exists(oldFile));

            // Rename + move into a subfolder: the next save must delete the previous file at its
            // bare disk path (the previous-state snapshot is a detached clone with rooted locations).
            package.Assets.Remove(assetItem);
            var movedItem = assetItem.Clone(newLocation: new UFile("Sub/Ground2"));
            package.Assets.Add(movedItem);
            movedItem.IsDirty = true;
            result = new LoggerResult();
            session.Save(result);
            Assert.False(result.HasErrors, string.Join("\n", result.Messages));
            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(Path.Combine(dirPath, "Assets", "Sub", "Ground2.sdtest")));
        }

        [Fact]
        public void TestNamespacedSaveRelativizesSamePrefixReferences()
        {
            var dirPath = Path.Combine(DirectoryTestBase, "TestNamespacedSaveRelativizesSamePrefixReferences");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(dirPath);

            var package = new Package { FullPath = Path.Combine(dirPath, "MyGame.sdpkg") };
            package.AssetFolders.Add(new AssetFolder("Assets"));
            var project = new SolutionProject(package, Guid.NewGuid(), Path.Combine(dirPath, "MyGame.csproj")) { AssetNamespace = "MyGame" };
            var session = new PackageSession();
            session.Projects.Add(project);

            var target = new AssetItem("Target", new AssetObjectTest());
            package.Assets.Add(target);
            // Same-prefix references save bare; foreign prefixes stay rooted
            var source = new AssetObjectTest();
            var sourceItem = new AssetItem("Source", source);
            package.Assets.Add(sourceItem);
            source.Reference = new AssetReference(target.Id, target.Location);
            sourceItem.IsDirty = true;
            var other = new AssetObjectTest { Reference = new AssetReference(AssetId.New(), "/OtherPkg/Thing") };
            var otherItem = new AssetItem("Other", other);
            package.Assets.Add(otherItem);
            otherItem.IsDirty = true;

            var result = new LoggerResult();
            session.Save(result);
            Assert.False(result.HasErrors, string.Join("\n", result.Messages));

            var sourceYaml = File.ReadAllText(Path.Combine(dirPath, "Assets", "Source.sdtest"));
            Assert.Contains($"{target.Id}:Target", sourceYaml);
            Assert.DoesNotContain("/MyGame/", sourceYaml);
            Assert.Contains(":/OtherPkg/Thing", File.ReadAllText(Path.Combine(dirPath, "Assets", "Other.sdtest")));
            // The in-memory reference stays canonical (the transform is write-only)
            Assert.Equal("/MyGame/Target", source.Reference.Location);
        }

        [Fact]
        public void TestSaveKeepsAuthoredPackageName()
        {
            var dirPath = Path.Combine(DirectoryTestBase, "TestSaveKeepsAuthoredPackageName");
            if (Directory.Exists(dirPath))
                Directory.Delete(dirPath, true);
            Directory.CreateDirectory(dirPath);

            // The session renames Meta.Name to the csproj-derived identity; the authored name must
            // survive a save (it is the package's namespace identity).
            var package = new Package { FullPath = Path.Combine(dirPath, "MyGame.Game.sdpkg"), AuthoredName = "MyGame" };
            package.Meta.Name = "MyGame.Game";
            var project = new SolutionProject(package, Guid.NewGuid(), Path.Combine(dirPath, "MyGame.Game.csproj"));
            var session = new PackageSession();
            session.Projects.Add(project);
            package.IsDirty = true;

            var result = new LoggerResult();
            session.Save(result);
            Assert.False(result.HasErrors);
            Assert.Matches(@"(?m)^\s*Name: MyGame\s*$", File.ReadAllText(package.FullPath));
            Assert.Equal("MyGame.Game", package.Meta.Name);
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
            var projectPath = Path.Combine(basePath, "TestPackageLoadingWithAssets.sdpkg");

            var sessionResult = PackageSession.Load(projectPath);
            AssertResult(sessionResult);
            var session = sessionResult.Session;

            var project = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackageLoadingWithAssets");
            Assert.NotNull(project);
            Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

            Assert.True(1 == project.Container.FlattenedDependencies.Count, "Expecting subproject");

            Assert.NotEqual(AssetId.Empty, project.Assets.First().Id);

            // Check for UPathRelativeTo
            var folder = project.AssetFolders.FirstOrDefault();
            Assert.NotNull(folder);
            Assert.NotNull(folder.Path);
            Assert.True(folder.Path.IsAbsolute);

            // Save project back to disk on a different location
            project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackage2\TestPackage2.sdpkg");
            var subPackage = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "SubPackage");
            subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackage2\SubPackage\SubPackage.sdpkg");
            var result = new LoggerResult();
            session.Save(result);

            var project2Result = PackageSession.Load(Path.Combine(DirectoryTestBase, "TestPackage2", "TestPackage2.sdpkg"));
            AssertResult(project2Result);
            var project2 = project2Result.Session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackage2");
            Assert.NotNull(project2);
            Assert.Equal(3, project2.Assets.Count);
        }

        [Fact(Skip = "Need check")]
        public void TestMovingAssets()
        {
            var basePath = Path.Combine(DirectoryTestBase, @"TestPackage");
            var projectPath = Path.Combine(basePath, "TestPackageLoadingWithAssets.sdpkg");

            var testAssetId = new AssetId("C2D80EF9-2160-43B2-9FEE-A19A903A0BE0");

            // Load the project from the original location
            var sessionResult1 = PackageSession.Load(projectPath);
            {
                AssertResult(sessionResult1);
                var session = sessionResult1.Session;
                var project = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackageLoadingWithAssets");
                Assert.NotNull(project);

                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Find the second asset that was referencing the changed asset
                var testAssetItem = session.FindAsset(testAssetId);
                Assert.NotNull(testAssetItem);

                var testAsset = (AssetObjectTest)testAssetItem.Asset;
                Assert.Equal(new UFile(Path.Combine(basePath, "SubFolder/TestAsset.sdtest")), testAsset.RawAsset);

                // First save a copy of the project to TestPackageMovingAssets1
                project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets1\TestPackage2.sdpkg");
                var subPackage = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "SubPackage");
                subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets1\SubPackage\SubPackage.sdpkg");
                var result = new LoggerResult();
                session.Save(result);
            }

            // Reload the project from the location TestPackageMovingAssets1
            var sessionResult2 = PackageSession.Load(DirectoryTestBase + @"TestPackageMovingAssets1\TestPackage2.sdpkg");
            {
                AssertResult(sessionResult2);
                var session = sessionResult2.Session;
                var project = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackage2");
                Assert.NotNull(project);
                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Move asset into a different directory
                var assetItem = project.Assets.Find(new AssetId("28D0DE9C-8913-41B1-B50E-848DD8A7AF65"));
                Assert.NotNull(assetItem);
                project.Assets.Remove(assetItem);

                var newAssetItem = new AssetItem("subTest/TestAsset2", assetItem.Asset);
                project.Assets.Add(newAssetItem);

                // Save the whole project to a different location
                project.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets2\TestPackage2.sdpkg");
                var subPackage = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackage2");
                subPackage.FullPath = Path.Combine(DirectoryTestBase, @"TestPackageMovingAssets2\SubPackage\SubPackage.sdpkg");
                var result = new LoggerResult();
                session.Save(result);
            }

            // Reload the project from location TestPackageMovingAssets2
            var sessionResult3 = PackageSession.Load(DirectoryTestBase + @"TestPackageMovingAssets2\TestPackage2.sdpkg");
            {
                AssertResult(sessionResult3);
                var session = sessionResult3.Session;
                var project = session.Packages.Single(x => x.FullPath.GetFileNameWithoutExtension() == "TestPackage2");
                Assert.NotNull(project);
                Assert.True(3 == project.Assets.Count, "Invalid number of assets loaded");

                // Find the second asset that was referencing the changed asset
                var assetItemChanged = session.FindAsset(testAssetId);
                Assert.NotNull(assetItemChanged);

                // Check that references were correctly updated
                var assetChanged = (AssetObjectTest)assetItemChanged.Asset;
                Assert.Equal(new UFile(Path.Combine(Environment.CurrentDirectory, DirectoryTestBase) + "/TestPackage/SubFolder/TestAsset.sdtest"), assetChanged.RawAsset);
                var text = File.ReadAllText(assetItemChanged.FullPath);
                Assert.Contains("../../TestPackage/SubFolder/TestAsset.sdtest", text);

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
    }
}
