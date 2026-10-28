// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Stride.AssetConstants.targets: which package's assets a project passes to its generators as AdditionalFiles.
    /// </summary>
    public class TestAssetConstantsTargets : IDisposable
    {
        private static readonly string TargetsPath = Path.Combine(AppContext.BaseDirectory, "MSBuild", "Stride.AssetConstants.targets");

        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-asset-constants-{Guid.NewGuid():N}");

        [ModuleInitializer]
        internal static void RegisterMSBuild()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        }

        public TestAssetConstantsTargets()
        {
            // A game library with its sdpkg next to it, and an asset in its asset folder
            Directory.CreateDirectory(Path.Combine(directory, "MyGame.Game", "Assets"));
            Directory.CreateDirectory(Path.Combine(directory, "MyGame.Windows"));
            File.WriteAllText(Path.Combine(directory, "MyGame.Game", "MyGame.Game.sdpkg"), """
                !Package
                Meta:
                    Name: MyGame.Game
                AssetFolders:
                    -   Path: !dir Assets
                """);
            File.WriteAllText(Path.Combine(directory, "MyGame.Game", "Assets", "Texture.sdtex"), "!Texture");
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void GameLibraryCollectsItsPackage()
        {
            var files = CollectAdditionalFiles("MyGame.Game", "MyGame.Game", @"$(MSBuildThisFileDirectory)MyGame.Game.sdpkg", projectReference: null);

            Assert.Equal(["Texture.sdtex"], files);
        }

        [Fact]
        public void PlatformHeadDoesNotCollectItsGameLibraryPackage()
        {
            // Generating the game's asset URL constants again in the head duplicates them
            var files = CollectAdditionalFiles("MyGame.Windows", "MyGame.Windows", @"$(MSBuildThisFileDirectory)..\MyGame.Game\MyGame.Game.sdpkg", @"..\MyGame.Game\MyGame.Game.csproj");

            Assert.Empty(files);
        }

        [Fact]
        public void PlatformHeadCollectsItsOwnPackage()
        {
            File.WriteAllText(Path.Combine(directory, "MyGame.Windows", "MyGame.Windows.sdpkg"), """
                !Package
                Meta:
                    Name: MyGame.Windows
                AssetFolders:
                    -   Path: !dir Assets
                """);
            Directory.CreateDirectory(Path.Combine(directory, "MyGame.Windows", "Assets"));
            File.WriteAllText(Path.Combine(directory, "MyGame.Windows", "Assets", "HeadTexture.sdtex"), "!Texture");

            var files = CollectAdditionalFiles("MyGame.Windows", "MyGame.Windows", @"$(MSBuildThisFileDirectory)..\MyGame.Game\MyGame.Game.sdpkg", @"..\MyGame.Game\MyGame.Game.csproj");

            Assert.Equal(["HeadTexture.sdtex"], files);
        }

        private string[] CollectAdditionalFiles(string projectDirectory, string projectName, string currentPackagePath, string? projectReference)
        {
            var path = Path.Combine(directory, projectDirectory, projectName + ".csproj");
            var reference = projectReference is not null ? $"""<ProjectReference Include="{projectReference}" />""" : "";
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    <StrideCurrentPackagePath>{currentPackagePath}</StrideCurrentPackagePath>
                  </PropertyGroup>
                  <ItemGroup>
                    {reference}
                  </ItemGroup>
                  <Import Project="{TargetsPath}" />
                </Project>
                """);

            var project = new ProjectInstance(path, null, null, new ProjectCollection());
            Assert.True(project.Build("StrideCollectAssetConstants", null));
            // The content type map is an AdditionalFile too, whatever the package
            return project.GetItems("AdditionalFiles")
                .Where(x => x.GetMetadataValue("StrideAssetContentTypeMap") != "true")
                .Select(x => Path.GetFileName(x.EvaluatedInclude))
                .Order(StringComparer.Ordinal)
                .ToArray();
        }
    }
}
