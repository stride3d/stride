// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Xunit;
using Stride.Core.Assets.Analysis;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests
{
    public class TestAssetReferenceAnalysis : TestBase
    {
        /// <summary>
        /// Tests that updating an asset reference that is pointing to an invalid GUID but a valid location is updated with the new GUID.
        /// </summary>
        [Fact]
        public void TestUpdateAssetUrl()
        {
            var projectDir = new UFile(Path.Combine(Environment.CurrentDirectory, "testxk"));
            
            // Create a project with an asset reference a raw file
            var project = new Package { FullPath = projectDir };
            var assetItem = new AssetItem("test", new AssetObjectTest() { Reference =  new AssetReference(AssetId.Empty, "good/location")});
            project.Assets.Add(assetItem);
            var goodAsset = new AssetObjectTest();
            project.Assets.Add(new AssetItem("good/location", goodAsset));

            // Add the project to the session to make sure analysis will run correctly
            var session = new PackageSession(project);

            // Create a session with this project
            var analysis = new PackageAnalysis(project,
                new PackageAnalysisParameters()
                    {
                        IsProcessingAssetReferences = true,
                        ConvertUPathTo = UPathType.Absolute,
                        IsProcessingUPaths = true
                    });
            var result = analysis.Run();
            Assert.False(result.HasErrors);
            Assert.Single(result.Messages);
            Assert.Contains("changed", result.Messages[0].ToString());

            var asset = (AssetObjectTest)assetItem.Asset;
            Assert.Equal(goodAsset.Id, asset.Reference.Id);
            Assert.Equal("good/location", asset.Reference.Location);
        }

        [Fact]
        public void TestMoveAssetWithUFile()
        {
            var projectDir = new UFile(Path.Combine(Environment.CurrentDirectory, "testxk"));
            var rawAssetPath = new UFile("../image.png");
            var assetPath = new UFile("sub1/sub2/test");

            // Create a project with an asset reference a raw file
            var project = new Package { FullPath = projectDir };
            project.AssetFolders.Clear();
            project.AssetFolders.Add(new AssetFolder("."));
            var asset = new AssetObjectTest() { RawAsset = new UFile(rawAssetPath) };
            var assetItem = new AssetItem(assetPath, asset);
            project.Assets.Add(assetItem);

            // Run an asset reference analysis on this project
            var analysis = new PackageAnalysis(project,
                new PackageAnalysisParameters()
                    {
                        ConvertUPathTo = UPathType.Absolute,
                        IsProcessingUPaths = true
                    });
            var result = analysis.Run();
            Assert.False(result.HasErrors);
            Assert.Equal(UPath.Combine(project.RootDirectory, new UFile("sub1/image.png")), asset.RawAsset);

            project.Assets.Remove(assetItem);
            assetItem = new AssetItem("sub1/test", asset);
            project.Assets.Add(assetItem);
            result = analysis.Run();
            Assert.False(result.HasErrors);
            Assert.Equal(UPath.Combine(project.RootDirectory, new UFile("sub1/image.png")), asset.RawAsset);

            project.Assets.Remove(assetItem);
            assetItem = new AssetItem("test", asset);
            project.Assets.Add(assetItem);
            result = analysis.Run();
            Assert.False(result.HasErrors);
            Assert.Equal(UPath.Combine(project.RootDirectory, new UFile("sub1/image.png")), asset.RawAsset);

            analysis.Parameters.ConvertUPathTo = UPathType.Relative;
            result = analysis.Run();
            Assert.False(result.HasErrors);
            Assert.Equal(new UFile("sub1/image.png"), asset.RawAsset);
        }
    }
}
