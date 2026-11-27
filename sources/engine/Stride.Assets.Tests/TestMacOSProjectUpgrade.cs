// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.5.5 project upgrade: macOS is osx-arm64 only, so osx-x64 becomes osx-arm64 in a project file.
    /// </summary>
    public class TestMacOSProjectUpgrade : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-macos-upgrade-{Guid.NewGuid():N}");

        public TestMacOSProjectUpgrade()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void ChangesTheRuntimeIdentifierOfAMacOSProject()
        {
            // A macOS project as Game Studio made it before 4.5
            var (context, log) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <RuntimeIdentifier>osx-x64</RuntimeIdentifier>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal("osx-arm64", Property(context, "RuntimeIdentifier"));
            Assert.True(context.IsDirty);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Info && m.Text.Contains("osx-arm64 only", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("win-x64;osx-x64;linux-x64", "win-x64;osx-arm64;linux-x64")]
        [InlineData("osx-x64;osx-arm64", "osx-arm64")]
        [InlineData("osx-x64; osx-arm64", "osx-arm64")]
        [InlineData("osx-arm64;osx-x64", "osx-arm64")]
        public void ChangesTheRuntimeIdentifiers(string runtimeIdentifiers, string expected)
        {
            var (context, _) = Upgrade($"""
                <Project>
                  <PropertyGroup>
                    <RuntimeIdentifiers>{runtimeIdentifiers}</RuntimeIdentifiers>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal(expected, Property(context, "RuntimeIdentifiers"));
            Assert.True(context.IsDirty);
        }

        [Fact]
        public void ChangesRuntimesPathsAndConditions()
        {
            var (context, _) = Upgrade("""
                <Project>
                  <PropertyGroup Condition="'$(RuntimeIdentifier)' == 'osx-x64'">
                    <NativeFolder>$(OutputPath)runtimes/osx-x64</NativeFolder>
                  </PropertyGroup>
                  <ItemGroup>
                    <None Include="runtimes\osx-x64\native\libgame.dylib" Link="runtimes\osx-x64\native\libgame.dylib" />
                  </ItemGroup>
                </Project>
                """);

            var xml = context.Project.Xml;
            Assert.Equal("'$(RuntimeIdentifier)' == 'osx-arm64'", xml.PropertyGroups.Single().Condition);
            Assert.Equal("$(OutputPath)runtimes/osx-arm64", Property(context, "NativeFolder"));
            var item = xml.Items.Single();
            Assert.Equal(@"runtimes\osx-arm64\native\libgame.dylib", item.Include);
            Assert.Equal(@"runtimes\osx-arm64\native\libgame.dylib", item.Metadata.Single(x => x.Name == "Link").Value);
            Assert.True(context.IsDirty);
        }

        [Fact]
        public void LeavesAProjectWithoutOsxX64()
        {
            // The project of a game made from the 4.5 templates: no runtime identifier
            var (context, log) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <StridePlatform>macOS</StridePlatform>
                  </PropertyGroup>
                  <ItemGroup>
                    <ProjectReference Include="..\Game\Game.csproj" />
                  </ItemGroup>
                </Project>
                """);

            Assert.False(context.IsDirty);
            Assert.Empty(log.Messages);
        }

        [Fact]
        public void WarnsWhenOsxX64ComesFromAnImportedFile()
        {
            // As from a Directory.Build.props: nothing in the project file to change
            File.WriteAllText(Path.Combine(directory, "Platform.props"), """
                <Project>
                  <PropertyGroup>
                    <RuntimeIdentifier>osx-x64</RuntimeIdentifier>
                  </PropertyGroup>
                </Project>
                """);
            var (context, log) = Upgrade("""
                <Project>
                  <Import Project="Platform.props" />
                </Project>
                """);

            Assert.False(context.IsDirty);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Warning && m.Text.Contains("imported file", StringComparison.Ordinal));
        }

        private static string Property(ProjectUpgradeContext context, string name)
            => context.Project.Xml.Properties.Single(x => x.Name == name).Value;

        private (ProjectUpgradeContext Context, LoggerResult Log) Upgrade(string projectXml)
        {
            var path = Path.Combine(directory, "Game.macOS.csproj");
            File.WriteAllText(path, projectXml);
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var log = new LoggerResult();
            var context = new ProjectUpgradeContext(project, null!, new UFile(path), log);
            StridePackageUpgrader.UpgradeMacOSToArm64(context);
            return (context, log);
        }
    }
}
