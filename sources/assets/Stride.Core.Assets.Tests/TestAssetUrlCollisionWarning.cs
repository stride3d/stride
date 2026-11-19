// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Two assets of a package with the same URL (two files that differ only by their extension): the one loaded second gets another
/// URL, as before, and the load logs a warning that names both files, the URL and the new URL.
/// </summary>
public class TestAssetUrlCollisionWarning : TestBase
{
    [Fact]
    public void CleaningTwoAssetsOfOneUrlLogsOneWarning()
    {
        var first = new AssetItem("Echo", new AssetObjectTest()) { SourceFolder = "Assets" };
        var second = new AssetItem("Echo", new AssetObjectTestSub()) { SourceFolder = "Assets" };
        var log = new LoggerResult();
        var outputs = new List<AssetItem>();

        AssetCollision.Clean(null, [first, second], outputs, new AssetResolver(), true, false, log);

        Assert.Equal(["Echo", "Echo (2)"], outputs.Select(x => x.Location.FullPath));
        var warning = Assert.IsType<AssetLogMessage>(Assert.Single(log.Messages));
        Assert.Equal(LogMessageType.Warning, warning.Type);
        Assert.Equal(AssetMessageCode.AssetUrlCollision, warning.MessageCode);
        // The log message starts with the URL of its asset
        Assert.Equal("Echo (2): [Echo.sdtestsub] has the same URL as [Echo.sdtest], [Echo]: it gets the URL [Echo (2)]. Rename one of the two files to choose their URLs.", warning.Text);
        // The message is about the asset that got the new URL
        Assert.Equal(outputs[1].Id, warning.AssetReference!.Id);
        Assert.Equal("Echo (2)", warning.AssetReference.Location);
    }

    [Fact]
    public void CleaningWithoutALogStillGivesANewUrl()
    {
        var outputs = new List<AssetItem>();

        AssetCollision.Clean(null, [new AssetItem("Echo", new AssetObjectTest()), new AssetItem("Echo", new AssetObjectTestSub())], outputs, new AssetResolver(), true, false);

        Assert.Equal(["Echo", "Echo (2)"], outputs.Select(x => x.Location.FullPath));
    }

    [Fact]
    public void CleaningAssetsOfDistinctUrlsLogsNothing()
    {
        var log = new LoggerResult();

        AssetCollision.Clean(null, [new AssetItem("Echo", new AssetObjectTest()), new AssetItem("Riddles", new AssetObjectTestSub())], new List<AssetItem>(), new AssetResolver(), true, false, log);

        Assert.Empty(log.Messages);
    }

    [Fact]
    public void LoadingAPackageWithTwoAssetsOfOneUrlWarnsOnceAndDirtiesThePackage()
    {
        var directory = Path.Combine(DirectoryTestBase, nameof(TestAssetUrlCollisionWarning));
        if (Directory.Exists(directory))
            Directory.Delete(directory, true);
        Directory.CreateDirectory(directory);
        try
        {
            // A project that holds no asset types makes the package editable, as in a game
            File.WriteAllText(Path.Combine(directory, "MyGame.csproj"),
                """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <StrideContainsAssetTypes>false</StrideContainsAssetTypes>
                  </PropertyGroup>
                </Project>
                """);
            var packagePath = Path.Combine(directory, "MyGame.sdpkg");
            AssetFileSerializer.Save(packagePath, new Package
            {
                FullPath = packagePath,
                Meta = { Name = "MyGame", Version = new PackageVersion("1.0.0") },
                AssetFolders = { new AssetFolder("Assets") },
            }, null);
            AssetFileSerializer.Save(Path.Combine(directory, "Assets", "Echo.sdtest"), new AssetObjectTest { Name = "Echo" }, null);
            AssetFileSerializer.Save(Path.Combine(directory, "Assets", "Echo.sdtestsub"), new AssetObjectTestSub { Value = 1 }, null);

            var result = new PackageSessionResult();
            PackageSession.LoadWithoutMSBuild(packagePath, ProjectType.Library, result, projectFiles: []);
            using var session = result.Session;

            var warning = Assert.Single(result.Messages.OfType<AssetLogMessage>(), x => x.MessageCode == AssetMessageCode.AssetUrlCollision);
            Assert.Equal(LogMessageType.Warning, warning.Type);
            Assert.Contains("[Echo.sdtest]", warning.Text);
            Assert.Contains("[Echo.sdtestsub]", warning.Text);
            // The URLs of a loaded package start with its name; which of the two assets gets the new URL depends on the load order
            Assert.Contains("[/MyGame/Echo (2)]", warning.Text);
            var package = Assert.Single(session.LocalPackages);
            Assert.Equal(["/MyGame/Echo", "/MyGame/Echo (2)"], package.Assets.Select(x => x.Location.FullPath).Order(StringComparer.Ordinal));
            Assert.True(package.IsDirty);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }
}
