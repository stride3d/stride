// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.5 project upgrade: Stride.Engine no longer brings Stride.Audio, so a project gets a Stride.Audio reference
    /// shaped like its Stride.Engine one.
    /// </summary>
    public class TestAudioProjectUpgrade : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-audio-upgrade-{Guid.NewGuid():N}");

        public TestAudioProjectUpgrade()
        {
            Directory.CreateDirectory(directory);
        }

        // The tests load project files with the installed MSBuild: registered before any assembly of it loads, as the
        // test host would otherwise serve the old copy next to the tests
        [ModuleInitializer]
        internal static void RegisterMSBuild()
        {
            PackageSessionPublicHelper.FindAndSetMSBuildVersion();
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void AddsStrideAudioShapedLikeStrideEngine()
        {
            var (context, log) = Upgrade("""
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="4.5.0" Condition="'$(Configuration)' != 'Fake'" PrivateAssets="none" />
                  </ItemGroup>
                </Project>
                """);

            var audio = Assert.Single(PackageReferences(context), x => x.Include == "Stride.Audio");
            Assert.Equal("'$(Configuration)' != 'Fake'", audio.Condition);
            Assert.Equal("4.5.0", audio.Metadata.Single(x => x.Name == "Version").Value);
            Assert.Equal("none", audio.Metadata.Single(x => x.Name == "PrivateAssets").Value);
            Assert.True(context.IsDirty);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Info && m.Text.Contains("remove it if this project does not use audio", StringComparison.Ordinal));
        }

        [Fact]
        public void KeepsAnExistingStrideAudio()
        {
            var (context, _) = Upgrade("""
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="4.5.0" />
                    <PackageReference Include="stride.audio" Version="4.5.0" />
                  </ItemGroup>
                </Project>
                """);

            Assert.Single(PackageReferences(context), x => string.Equals(x.Include, "Stride.Audio", StringComparison.OrdinalIgnoreCase));
            Assert.False(context.IsDirty);
        }

        [Fact]
        public void WarnsWhenStrideEngineComesFromAnImportedFile()
        {
            // As from a Directory.Build.props: no reference in the project file to copy
            File.WriteAllText(Path.Combine(directory, "Engine.props"), """
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="4.5.0" />
                  </ItemGroup>
                </Project>
                """);
            var (context, log) = Upgrade("""
                <Project>
                  <Import Project="Engine.props" />
                </Project>
                """);

            Assert.DoesNotContain(PackageReferences(context), x => x.Include == "Stride.Audio");
            Assert.False(context.IsDirty);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Warning && m.Text.Contains("imported file", StringComparison.Ordinal));
        }

        [Fact]
        public void LeavesAProjectWithoutStrideEngine()
        {
            // An executable gets Stride.Engine, and audio with it, through its game library
            var (context, log) = Upgrade("""
                <Project>
                  <ItemGroup>
                    <ProjectReference Include="..\Game\Game.csproj" />
                  </ItemGroup>
                </Project>
                """);

            Assert.Empty(PackageReferences(context));
            Assert.False(context.IsDirty);
            Assert.DoesNotContain(log.Messages, m => m.Type == LogMessageType.Warning);
        }

        [Theory]
        [InlineData("13", true)]
        [InlineData("12.0", true)]
        [InlineData("14", false)]
        [InlineData("latest", false)]
        public void WarnsWhenLangVersionIsBelowCSharp14(string languageVersion, bool warns)
        {
            // Game.Audio is an extension property now
            var (_, log) = Upgrade($"""
                <Project>
                  <PropertyGroup>
                    <LangVersion>{languageVersion}</LangVersion>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="4.5.0" />
                  </ItemGroup>
                </Project>
                """);

            Assert.Equal(warns, log.Messages.Any(m => m.Type == LogMessageType.Warning && m.Text.Contains("C# 14", StringComparison.Ordinal)));
        }

        private static ProjectItemElement[] PackageReferences(ProjectUpgradeContext context)
            => context.Project.Xml.Items.Where(x => x.ItemType == "PackageReference").ToArray();

        private (ProjectUpgradeContext Context, LoggerResult Log) Upgrade(string projectXml)
        {
            var path = Path.Combine(directory, "Game.csproj");
            File.WriteAllText(path, projectXml);
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var log = new LoggerResult();
            var context = new ProjectUpgradeContext(project, null!, new UFile(path), log);
            StridePackageUpgrader.UpgradeProjectTo45(context);
            return (context, log);
        }
    }
}
