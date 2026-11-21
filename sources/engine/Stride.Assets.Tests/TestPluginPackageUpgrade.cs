// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using Microsoft.Build.Evaluation;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
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

            // A navigation mesh needs Stride.Navigation, its runtime type's package, even next to Stride.Physics
            Assert.Equal(new[] { "Stride.BepuPhysics", "Stride.Navigation", "Stride.Video" }, missing.Order());
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

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void LeavesThePackagesToAddWhenStrideEngineComesFromAnImportedFile(bool videoImported, bool leftToAdd)
        {
            // As from a Directory.Build.props, where the user adds the packages by hand before a retry
            File.WriteAllText(Path.Combine(directory, "Assets", "Intro.sdvid"), "");
            var video = videoImported ? "<PackageReference Include=\"Stride.Video\" Version=\"4.5.0\" />" : "";
            File.WriteAllText(Path.Combine(directory, "Engine.props"), $"""
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="4.5.0" />
                    {video}
                  </ItemGroup>
                </Project>
                """);
            var path = Path.Combine(directory, "Game.csproj");
            File.WriteAllText(path, """
                <Project>
                  <Import Project="Engine.props" />
                </Project>
                """);
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var context = new ProjectUpgradeContext(project, null!, new UFile(path), new LoggerResult());

            StridePackageUpgrader.AddPluginPackageReferences(context);

            Assert.False(context.IsDirty);
            if (leftToAdd)
                Assert.Contains("Stride.Video", Assert.Single(context.FailedUnits).Error, StringComparison.Ordinal);
            else
                Assert.Empty(context.FailedUnits);
        }
    }
}
