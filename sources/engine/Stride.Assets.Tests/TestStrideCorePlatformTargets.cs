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
    /// Stride.Core.targets: the platform of a project that references Stride's packages (StridePlatform).
    /// </summary>
    public sealed class TestStrideCorePlatformTargets : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-core-platform-{Guid.NewGuid():N}");

        public TestStrideCorePlatformTargets()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            Directory.CreateDirectory(directory);
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void ProjectWithoutPlatformGetsTheHostPlatform()
        {
            // A plain library or test project (net10.0, no runtime identifier)
            var expected = OperatingSystem.IsLinux() ? "Linux" : OperatingSystem.IsMacOS() ? "macOS" : "Windows";

            Assert.Equal(expected, Platform(new()));
        }

        [Theory]
        [InlineData("win-x64", "Windows")]
        [InlineData("linux-x64", "Linux")]
        [InlineData("osx-arm64", "macOS")]
        public void RuntimeIdentifierGivesThePlatform(string runtimeIdentifier, string expected)
        {
            Assert.Equal(expected, Platform(new() { ["RuntimeIdentifier"] = runtimeIdentifier }));
        }

        [Fact]
        public void TargetPlatformGivesThePlatform()
        {
            // A Windows head (net10.0-windows), whatever the host
            Assert.Equal("Windows", Platform(new() { ["TargetPlatformIdentifier"] = "Windows" }));
        }

        private string Platform(Dictionary<string, string> properties)
        {
            var root = CheckoutRoot();
            var path = Path.Combine(directory, "MyLibrary.csproj");
            var propertyElements = string.Join(Environment.NewLine, properties.Select(x => $"<{x.Key}>{x.Value}</{x.Key}>"));
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    <StrideRoot>{root}{Path.DirectorySeparatorChar}</StrideRoot>
                    {propertyElements}
                  </PropertyGroup>
                  <Import Project="{Path.Combine(root, "sources", "core", "Stride.Core", "build", "Stride.Core.targets")}" />
                </Project>
                """);

            var project = new ProjectInstance(path, null, null, new ProjectCollection());
            return project.GetPropertyValue("StridePlatform");
        }

        private static string CheckoutRoot()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                if (File.Exists(Path.Combine(current.FullName, "sources", "core", "Stride.Core", "build", "Stride.Core.targets")))
                    return current.FullName;
            }
            throw new InvalidOperationException($"Could not find Stride.Core.targets above {AppContext.BaseDirectory}.");
        }
    }
}
