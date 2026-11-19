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
    /// Stride.AssetCompiler.targets: where a project built against the engine's source tree finds the asset compiler.
    /// </summary>
    public sealed class TestAssetCompilerPathTargets : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-asset-compiler-path-{Guid.NewGuid():N}");

        public TestAssetCompilerPathTargets()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();

            // An engine checkout whose asset compiler was built in Debug only
            var compiler = Path.Combine(directory, "stride", "sources", "assets", "Stride.AssetCompiler", "bin", "Debug", "net10.0", "Stride.AssetCompiler.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(compiler)!);
            File.WriteAllText(compiler, "");
            Directory.CreateDirectory(Path.Combine(directory, "Plugin"));
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void DevRedirectProjectUsesTheEngineConfiguration()
        {
            // The dev package stubs set StrideDevConfiguration to the configuration the engine was built in
            var command = PrepareAssetCompiler(new() { ["Configuration"] = "Release", ["StrideDevConfiguration"] = "Debug" });

            Assert.Equal(Path.Combine(directory, "stride", "sources", "assets", "Stride.AssetCompiler", "bin", "Debug", "net10.0", "Stride.AssetCompiler.dll"), Normalize(command));
        }

        [Fact]
        public void InTreeProjectUsesItsOwnConfiguration()
        {
            // No stub in the source tree: the compiler is the one built with the project
            var command = PrepareAssetCompiler(new() { ["Configuration"] = "Debug" });

            Assert.Equal(Path.Combine(directory, "stride", "sources", "assets", "Stride.AssetCompiler", "bin", "Debug", "net10.0", "Stride.AssetCompiler.dll"), Normalize(command));
        }

        private string PrepareAssetCompiler(Dictionary<string, string> properties)
        {
            var path = Path.Combine(directory, "Plugin", "Plugin.csproj");
            var propertyElements = string.Join(Environment.NewLine, properties.Select(x => $"<{x.Key}>{x.Value}</{x.Key}>"));
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    <StrideRoot>{Path.Combine(directory, "stride")}{Path.DirectorySeparatorChar}</StrideRoot>
                    {propertyElements}
                  </PropertyGroup>
                  <Import Project="{TargetsPath()}" />
                </Project>
                """);

            var project = new ProjectInstance(path, null, null, new ProjectCollection());
            Assert.True(project.Build("_StridePrepareAssetCompiler", null));
            return project.GetPropertyValue("StrideCompileAssetCommand");
        }

        private static string Normalize(string path) => Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar));

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
