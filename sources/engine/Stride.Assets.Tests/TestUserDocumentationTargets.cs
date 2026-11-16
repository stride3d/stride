// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// Stride.Core.UserDocumentation.targets: the package of a project that writes a .usrdoc file carries it beside the assembly.
    /// </summary>
    public sealed class TestUserDocumentationTargets : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-user-documentation-{Guid.NewGuid():N}");

        public TestUserDocumentationTargets()
        {
            Stride.Core.Assets.PackageSessionPublicHelper.FindAndSetMSBuildVersion();
            Directory.CreateDirectory(Path.Combine(directory, "bin"));
        }

        public void Dispose()
        {
            Directory.Delete(directory, recursive: true);
        }

        [Fact]
        public void PackageCarriesTheUserDocumentation()
        {
            // The file _StrideCopyUserDocumentation copied beside the assembly
            File.WriteAllText(Path.Combine(directory, "bin", "Plugin.usrdoc"), "P:Plugin.MyComponent.Speed=How fast it goes.");
            var project = CreateProject(generateDocumentationFile: true);

            Assert.Contains(".usrdoc", project.GetPropertyValue("AllowedOutputExtensionsInPackageBuildOutputFolder").Split(';'));
            Assert.Contains("_StrideRegisterUserDocOutputs", project.GetPropertyValue("TargetsForTfmSpecificBuildOutput").Split(';'));
            Assert.True(project.Build("_StrideRegisterUserDocOutputs", null));
            var output = Assert.Single(project.GetItems("BuildOutputInPackage"));
            Assert.Equal("Plugin.usrdoc", Path.GetFileName(output.EvaluatedInclude));
        }

        [Fact]
        public void OuterBuildAllowsTheUserDocumentation()
        {
            // The pack task filters the files with the allowed extensions of the outer build of a multi-targeted project,
            // which has no DocumentationFile
            var project = CreateProject(generateDocumentationFile: true, documentationFile: false);

            Assert.Contains(".usrdoc", project.GetPropertyValue("AllowedOutputExtensionsInPackageBuildOutputFolder").Split(';'));
        }

        [Fact]
        public void ProjectWithoutDocumentationFileAddsNothing()
        {
            var project = CreateProject(generateDocumentationFile: false);

            Assert.DoesNotContain("_StrideRegisterUserDocOutputs", project.GetPropertyValue("TargetsForTfmSpecificBuildOutput").Split(';'));
        }

        private ProjectInstance CreateProject(bool generateDocumentationFile, bool documentationFile = true)
        {
            var path = Path.Combine(directory, "Plugin.csproj");
            File.WriteAllText(path, $"""
                <Project>
                  <PropertyGroup>
                    <AssemblyName>Plugin</AssemblyName>
                    <OutputPath>bin{Path.DirectorySeparatorChar}</OutputPath>
                    <GenerateDocumentationFile>{(generateDocumentationFile ? "true" : "false")}</GenerateDocumentationFile>
                    <DocumentationFile Condition="'$(GenerateDocumentationFile)' == 'true' And {(documentationFile ? "true" : "false")}">obj{Path.DirectorySeparatorChar}Plugin.xml</DocumentationFile>
                  </PropertyGroup>
                  <Import Project="{TargetsPath()}" />
                </Project>
                """);
            return new ProjectInstance(path, null, null, new ProjectCollection());
        }

        private static string TargetsPath()
        {
            for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
            {
                var targets = Path.Combine(current.FullName, "sources", "core", "Stride.Core", "build", "Stride.Core.UserDocumentation.targets");
                if (File.Exists(targets))
                    return targets;
            }
            throw new InvalidOperationException($"Could not find Stride.Core.UserDocumentation.targets above {AppContext.BaseDirectory}.");
        }
    }
}
