// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System.Xml.Linq;
using Stride.Assets.Templates;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Templates;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    public class TestDotNetNewTemplateGenerator
    {
        [Theory]
        [InlineData("""<PackageReference Include="Stride.Engine" Version="4.4.0" />""", "4.4.0")]
        [InlineData("""<PackageReference Include="Stride.Engine" PrivateAssets="contentfiles;analyzers" Version="4.4.0" />""", "4.4.0")]
        [InlineData("""<PackageReference Include="Stride.Engine"><Version>4.4.0-beta7</Version></PackageReference>""", "4.4.0-beta7")]
        [InlineData("""<PackageReference Include="Stride.Graphics" Version="4.4.0" />""", null)]
        public void ContentEngineVersion(string reference, string? expected)
        {
            var directory = Directory.CreateTempSubdirectory("stride-template-tests").FullName;
            try
            {
                var project = Path.Combine(directory, "Game.csproj");
                File.WriteAllText(project, $"""<Project><ItemGroup>{reference}</ItemGroup></Project>""");

                Assert.Equal(expected is null ? null : new PackageVersion(expected), DotNetNewTemplateGenerator.GetContentEngineVersion([project]));
            }
            finally
            {
                Directory.Delete(directory, recursive: true);
            }
        }

        [Fact]
        public void GeneratedProjectsRecordTheCurrentVersion()
        {
            var (game, windows) = Generate(StridePackageUpgrader.CurrentVersion);
            try
            {
                Assert.False(DotNetNewTemplateGenerator.IsOlderContent(Path.GetDirectoryName(Path.GetDirectoryName(game))!));
                Assert.Equal(StridePackageUpgrader.CurrentVersion, ReadRecord(game));
                Assert.Equal(StridePackageUpgrader.CurrentVersion, ReadRecord(windows));
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(game))!, recursive: true);
            }
        }

        [Fact]
        public void OlderContentKeepsItsVersionForTheUpgrade()
        {
            var (game, windows) = Generate("4.4.0");
            try
            {
                Assert.True(DotNetNewTemplateGenerator.IsOlderContent(Path.GetDirectoryName(Path.GetDirectoryName(game))!));
                Assert.Equal("4.4.0", XDocument.Load(game).Descendants("PackageReference").Single().Attribute("Version")?.Value);
                Assert.Equal("4.4.0", ReadRecord(game));
                Assert.Null(ReadRecord(windows));
            }
            finally
            {
                Directory.Delete(Path.GetDirectoryName(Path.GetDirectoryName(game))!, recursive: true);
            }
        }

        // Generates a game library written for Stride contentVersion, with its record, and a platform project that references it
        private static (string Game, string Windows) Generate(string contentVersion)
        {
            var directory = Directory.CreateTempSubdirectory("stride-template-tests").FullName;
            var game = Path.Combine(directory, "Game", "Game.csproj");
            var windows = Path.Combine(directory, "Game.Windows", "Game.Windows.csproj");
            Directory.CreateDirectory(Path.GetDirectoryName(game)!);
            Directory.CreateDirectory(Path.GetDirectoryName(windows)!);
            File.WriteAllText(game, $"""
                <Project>
                  <ItemGroup>
                    <PackageReference Include="Stride.Engine" Version="{contentVersion}" />
                    <StridePackageUpgradeRecord Include="Stride" Version="{contentVersion}" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(windows, """<Project><ItemGroup><ProjectReference Include="..\Game\Game.csproj" /></ItemGroup></Project>""");

            var parameters = new SessionTemplateGeneratorParameters { OutputDirectory = new UDirectory(directory), Logger = new LoggerResult() };
            Assert.True(new Generator().Upgrade(parameters));
            return (game, windows);
        }

        private static string? ReadRecord(string project)
        {
            var record = XDocument.Load(project).Descendants(PackageUpgrader.UpgradeRecordItem).SingleOrDefault();
            if (record is not null)
                Assert.Equal(StridePackageUpgrader.UpgraderName, (string?)record.Attribute("Include"));
            return (string?)record?.Attribute("Version");
        }

        private sealed class Generator : DotNetNewTemplateGenerator
        {
            public bool Upgrade(SessionTemplateGeneratorParameters parameters) => UpgradeGeneratedProjects(parameters);
        }
    }
}
