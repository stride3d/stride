// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Stride.AssetCompiler.targets: which programs compile their assets as a game does (StrideIsExecutable).
    /// </summary>
    public sealed class TestAssetCompilerExecutableTargets : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-asset-compiler-executable-{Guid.NewGuid():N}");

        public TestAssetCompilerExecutableTargets()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Theory]
        [InlineData("Exe")]
        [InlineData("WinExe")]
        public void GameHeadWithoutPackageIsAGame(string outputType)
        {
            // The Linux and macOS heads of a game have no sdpkg: their assets come from the game library they reference
            Assert.True(IsExecutable(new() { ["OutputType"] = outputType }));
        }

        [Fact]
        public void TestProgramWithoutPackageIsNotAGame()
        {
            // A test framework such as xunit.v3 makes a test project a program
            Assert.False(IsExecutable(new() { ["OutputType"] = "Exe", ["IsTestProject"] = "true" }));
        }

        [Fact]
        public void TestProgramWithItsOwnPackageIsAGame()
        {
            File.WriteAllText(Path.Combine(directory, "MyProject.sdpkg"), "!Package");

            Assert.True(IsExecutable(new() { ["OutputType"] = "Exe", ["IsTestProject"] = "true" }));
        }

        [Fact]
        public void TestProgramNamingAPackageIsAGame()
        {
            Assert.True(IsExecutable(new() { ["OutputType"] = "Exe", ["IsTestProject"] = "true", ["StrideCurrentPackagePath"] = @"$(MSBuildThisFileDirectory)..\MyGame.Game\MyGame.Game.sdpkg" }));
        }

        [Fact]
        public void TestProgramAskingForAssetsIsAGame()
        {
            // As Stride's own test projects do
            Assert.True(IsExecutable(new() { ["OutputType"] = "WinExe", ["IsTestProject"] = "true", ["StrideIsExecutable"] = "true" }));
        }

        [Fact]
        public void LibraryIsNotAGame()
        {
            Assert.False(IsExecutable(new() { ["OutputType"] = "Library" }));
        }

        private bool IsExecutable(Dictionary<string, string> properties)
        {
            var path = Path.Combine(directory, "MyProject.csproj");
            var propertyElements = string.Join(Environment.NewLine, properties.Select(x => $"<{x.Key}>{x.Value}</{x.Key}>"));
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    {propertyElements}
                  </PropertyGroup>
                  <Import Project="{TargetsPath()}" />
                </Project>
                """);

            var project = new ProjectInstance(path, null, null, new ProjectCollection());
            return project.GetPropertyValue("StrideIsExecutable") == "true";
        }

        private static string TargetsPath()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                var targets = Path.Combine(current.FullName, "sources", "assets", "Stride.AssetCompiler", "build", "Stride.AssetCompiler.targets");
                if (File.Exists(targets))
                    return targets;
            }
            throw new InvalidOperationException($"Could not find Stride.AssetCompiler.targets above {AppContext.BaseDirectory}.");
        }
    }
}
