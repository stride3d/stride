// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.5 upgrade adds the plugin packages whose asset files a project has, since Stride.Assets no longer holds
    /// those asset types.
    /// </summary>
    public class TestPluginPackageUpgrade : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-plugin-upgrade-{Guid.NewGuid():N}");

        public TestPluginPackageUpgrade()
        {
            Directory.CreateDirectory(Path.Combine(directory, "Assets", "Physics"));
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void AddsThePackagesOfTheAssetFilesNotReferenced()
        {
            File.WriteAllText(Path.Combine(directory, "Assets", "Intro.sdvid"), "");
            File.WriteAllText(Path.Combine(directory, "Assets", "Physics", "Rock.sdhull"), "");
            File.WriteAllText(Path.Combine(directory, "Assets", "Physics", "Ground.sdphy"), "");
            File.WriteAllText(Path.Combine(directory, "Assets", "Physics", "Level.sdnavmesh"), "");

            var missing = StridePackageUpgrader.FindMissingPluginPackages(directory, ["Stride.Engine", "Stride.Physics"]);

            Assert.Equal(new[] { "Stride.BepuPhysics", "Stride.Video" }, missing.Order());
        }

        [Fact]
        public void IgnoresBuildOutputAndOtherFiles()
        {
            Directory.CreateDirectory(Path.Combine(directory, "obj", "stride"));
            Directory.CreateDirectory(Path.Combine(directory, "bin"));
            File.WriteAllText(Path.Combine(directory, "obj", "stride", "Intro.sdvid"), "");
            File.WriteAllText(Path.Combine(directory, "bin", "Rock.sdhull"), "");
            File.WriteAllText(Path.Combine(directory, "Assets", "MainScene.sdscene"), "");

            Assert.Empty(StridePackageUpgrader.FindMissingPluginPackages(directory, ["Stride.Engine"]));
        }
    }
}
