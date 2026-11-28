// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The 4.5.7 project upgrade: Android 13 (API 33) is the minimum, so the SupportedOSPlatformVersion of an Android
    /// project is raised to 33.
    /// </summary>
    public class TestAndroidProjectUpgrade : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-android-upgrade-{Guid.NewGuid():N}");

        public TestAndroidProjectUpgrade()
        {
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Theory]
        [InlineData("21")]
        [InlineData("24")]
        [InlineData("21.0")]
        public void RaisesTheMinimumOfAnAndroidProject(string minimum)
        {
            // An Android project as Game Studio made it before 4.5.7
            var (context, log) = Upgrade($"""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0-android</TargetFramework>
                    <SupportedOSPlatformVersion>{minimum}</SupportedOSPlatformVersion>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal("33", Property(context, "SupportedOSPlatformVersion"));
            Assert.True(context.IsDirty);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Info && m.Text.Contains("API 33", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData("33")]
        [InlineData("34.0")]
        public void LeavesAMinimumThatIsHighEnough(string minimum)
        {
            var (context, log) = Upgrade($"""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0-android</TargetFramework>
                    <SupportedOSPlatformVersion>{minimum}</SupportedOSPlatformVersion>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal(minimum, Property(context, "SupportedOSPlatformVersion"));
            Assert.False(context.IsDirty);
            Assert.Empty(log.Messages);
        }

        [Fact]
        public void SetsTheMinimumOfAnAndroidProjectWithoutOne()
        {
            // The .NET for Android default would be 21
            var (context, _) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0-android</TargetFramework>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal("33", Property(context, "SupportedOSPlatformVersion"));
            Assert.Single(context.Project.Xml.PropertyGroups);
            Assert.True(context.IsDirty);
        }

        [Fact]
        public void LeavesTheOtherPlatformsOfAProjectOfSeveralPlatforms()
        {
            var (context, _) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFrameworks>net10.0-android;net10.0-ios</TargetFrameworks>
                  </PropertyGroup>
                  <PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">
                    <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
                  </PropertyGroup>
                  <PropertyGroup Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'ios'">
                    <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
                  </PropertyGroup>
                </Project>
                """);

            var values = context.Project.Xml.Properties.Where(x => x.Name == "SupportedOSPlatformVersion").Select(x => x.Value);
            Assert.Equal(["33", "15.0"], values);
            Assert.True(context.IsDirty);
        }

        [Fact]
        public void LeavesAProjectOfAnotherPlatform()
        {
            var (context, log) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0-ios</TargetFramework>
                    <SupportedOSPlatformVersion>15.0</SupportedOSPlatformVersion>
                  </PropertyGroup>
                </Project>
                """);

            Assert.Equal("15.0", Property(context, "SupportedOSPlatformVersion"));
            Assert.False(context.IsDirty);
            Assert.Empty(log.Messages);
        }

        [Fact]
        public void ReportsALowerMinimumFromAnImportedFileAsLeft()
        {
            // As from a Directory.Build.targets, which comes after the project file
            File.WriteAllText(Path.Combine(directory, "Platform.targets"), """
                <Project>
                  <PropertyGroup>
                    <SupportedOSPlatformVersion>24</SupportedOSPlatformVersion>
                  </PropertyGroup>
                </Project>
                """);
            var (context, log) = Upgrade("""
                <Project>
                  <PropertyGroup>
                    <TargetFramework>net10.0-android</TargetFramework>
                    <SupportedOSPlatformVersion>21</SupportedOSPlatformVersion>
                  </PropertyGroup>
                  <Import Project="Platform.targets" />
                </Project>
                """);

            Assert.Equal("33", Property(context, "SupportedOSPlatformVersion"));
            var unit = Assert.Single(context.FailedUnits);
            Assert.Equal(Path.Combine(directory, "Game.Android.csproj"), unit.Path);
            Assert.Contains("imported file", unit.Error, StringComparison.Ordinal);
            Assert.Contains(log.Messages, m => m.Type == LogMessageType.Warning && m.Text.Contains("imported file", StringComparison.Ordinal));
        }

        private static string Property(ProjectUpgradeContext context, string name)
            => context.Project.Xml.Properties.Single(x => x.Name == name).Value;

        private (ProjectUpgradeContext Context, LoggerResult Log) Upgrade(string projectXml)
        {
            var path = Path.Combine(directory, "Game.Android.csproj");
            File.WriteAllText(path, projectXml);
            var project = new Project(path, null, null, new Microsoft.Build.Evaluation.ProjectCollection());
            var log = new LoggerResult();
            var context = new ProjectUpgradeContext(project, null!, new UFile(path), log);
            StridePackageUpgrader.UpgradeAndroidToApi33(context);
            return (context, log);
        }
    }
}
