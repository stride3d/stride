// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.2 and 4.3 project upgrades: .NET 6 to .NET 8, then .NET 8 to .NET 10.
    /// </summary>
    public class TestTargetFrameworkUpgrade : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-tfm-upgrade-{Guid.NewGuid():N}");

        public TestTargetFrameworkUpgrade()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Theory]
        // The executables of a 4.1 New Game
        [InlineData(PlatformType.Windows, "net6.0-windows", "net8.0-windows", "net10.0-windows")]
        [InlineData(PlatformType.Linux, "net6.0", "net8.0", "net10.0")]
        [InlineData(PlatformType.macOS, "net6.0", "net8.0", "net10.0")]
        [InlineData(PlatformType.Android, "net6.0-android", "net8.0-android", "net10.0-android")]
        [InlineData(PlatformType.iOS, "net6.0-ios", "net8.0-ios", "net10.0-ios")]
        // A Windows executable from 4.1 that had no -windows
        [InlineData(PlatformType.Windows, "net6.0", "net8.0-windows", "net10.0-windows")]
        // A Windows executable whose platform is unknown, because the old Stride.Core.targets wasn't restored
        [InlineData(PlatformType.Shared, "net6.0-windows", "net8.0-windows", "net10.0-windows")]
        public void KeepsThePlatformOfAnExecutable(PlatformType platform, string tfm41, string tfm42, string tfm43)
        {
            var path = WriteProject("TargetFramework", tfm41);

            var context = Upgrade(path, ProjectType.Executable, platform, StridePackageUpgrader.UpgradeProjectTo42);
            Assert.Equal(tfm42, Property(context, "TargetFramework"));
            Assert.True(context.IsDirty);

            context = Upgrade(path, ProjectType.Executable, platform, StridePackageUpgrader.UpgradeProjectTo43);
            Assert.Equal(tfm43, Property(context, "TargetFramework"));
            Assert.True(context.IsDirty);
        }

        [Fact]
        public void KeepsEachPlatformOfAGameProjectThatIsItsOwnExecutable()
        {
            // Loaded with its first entry, so its platform is Windows
            var path = WriteProject("TargetFrameworks", "net6.0-windows;net6.0-android;net6.0-ios");

            var context = Upgrade(path, ProjectType.Executable, PlatformType.Windows, StridePackageUpgrader.UpgradeProjectTo42);
            Assert.Equal("net8.0-windows;net8.0-android;net8.0-ios", Property(context, "TargetFrameworks"));

            context = Upgrade(path, ProjectType.Executable, PlatformType.Windows, StridePackageUpgrader.UpgradeProjectTo43);
            Assert.Equal("net10.0-windows;net10.0-android;net10.0-ios", Property(context, "TargetFrameworks"));
        }

        [Theory]
        [InlineData(ProjectType.Executable, "net6.0-windows;net8.0-windows;netstandard2.0", "net8.0-windows;netstandard2.0")]
        [InlineData(ProjectType.Library, "netstandard2.0;net6.0", "netstandard2.0;net8.0")]
        public void LeavesTheEntriesOfOtherVersions(ProjectType type, string tfm41, string tfm42)
        {
            var path = WriteProject("TargetFrameworks", tfm41);

            var context = Upgrade(path, type, PlatformType.Windows, StridePackageUpgrader.UpgradeProjectTo42);

            Assert.Equal(tfm42, Property(context, "TargetFrameworks"));
        }

        [Fact]
        public void TargetsOneFrameworkInTheGameLibrary()
        {
            // The Game library of a 4.1 New Game: the New Game library of 4.3 targets net10.0 on every platform
            var path = WriteProject("TargetFrameworks", "net6.0-windows;net6.0-android");

            var context = Upgrade(path, ProjectType.Library, PlatformType.Shared, StridePackageUpgrader.UpgradeProjectTo42);
            Assert.Equal("net8.0", Property(context, "TargetFrameworks"));

            context = Upgrade(path, ProjectType.Library, PlatformType.Shared, StridePackageUpgrader.UpgradeProjectTo43);
            Assert.Equal("net10.0", Property(context, "TargetFrameworks"));
        }

        [Theory]
        [InlineData("net10.0-android")]
        [InlineData("netstandard2.0")]
        public void LeavesAnotherVersion(string tfm)
        {
            var path = WriteProject("TargetFramework", tfm);

            var context = Upgrade(path, ProjectType.Executable, PlatformType.Android, StridePackageUpgrader.UpgradeProjectTo43);

            Assert.Equal(tfm, Property(context, "TargetFramework"));
            Assert.False(context.IsDirty);
        }

        [Fact]
        public void LeavesAProjectWithoutTargetFramework()
        {
            var path = Path.Combine(directory, "Game.csproj");
            File.WriteAllText(path, "<Project />");

            var context = Upgrade(path, ProjectType.Executable, PlatformType.Windows, StridePackageUpgrader.UpgradeProjectTo42);

            Assert.False(context.IsDirty);
        }

        private string WriteProject(string property, string value)
        {
            var path = Path.Combine(directory, "Game.csproj");
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    <{property}>{value}</{property}>
                  </PropertyGroup>
                </Project>
                """);
            return path;
        }

        private static string Property(ProjectUpgradeContext context, string name)
            => context.Project.Xml.Properties.Single(x => x.Name == name).Value;

        // Each step runs on the project as the previous one saved it
        private static ProjectUpgradeContext Upgrade(string path, ProjectType type, PlatformType platform, Action<ProjectUpgradeContext> step)
        {
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var solutionProject = new SolutionProject(new Package(), Guid.NewGuid(), path) { Type = type, Platform = platform };
            var context = new ProjectUpgradeContext(project, solutionProject, new UFile(path), new LoggerResult());
            step(context);
            project.Save();
            return context;
        }
    }
}
