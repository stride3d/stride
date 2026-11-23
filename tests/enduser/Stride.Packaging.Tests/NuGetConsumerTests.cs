// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Stride.Packaging.Tests;

/// <summary>
/// Smoke-tests the packed Stride packages as a real external consumer: generate a sample,
/// pin Stride.* to the locally-built bin/packages feed via a strict nuget.config, then
/// <c>dotnet build</c> it as a subprocess. Catches what editor screenshot tests can't —
/// the <c>dotnet build</c> CLI path (vs Stride's in-process VSProjectHelper), and packaging
/// integrity: a package missing from the pack or a broken nuspec fails restore here.
/// </summary>
public class NuGetConsumerTests
{
    private readonly ITestOutputHelper output;
    public NuGetConsumerTests(ITestOutputHelper output) => this.output = output;

    /// <summary>
    /// CustomEffect sample — smallest TemplateSample whose asset build runs the full
    /// SDSL → SPIR-V → SPIRV-Cross → HLSL → FXC shader pipeline, so the consumer build
    /// exercises real shader compilation, not just a C# project build.
    /// </summary>
    [Fact]
    public void BuildCustomEffectAsExternalConsumer()
    {
        var templateGuid = new Guid("16476A4C-C131-4F48-865A-288EC7D5445F");
        var sampleName = "ConsumerBuild_CustomEffect";
        TestEnvironment.EnsureSharedPackages();
        var slnPath = TestEnvironment.GenerateSample(templateGuid, sampleName);
        var sampleDir = Path.GetDirectoryName(slnPath)!;

        // Build the host-platform executable project directly: it triggers asset compilation
        // and references .Game.
        var platformSuffix = TestEnvironment.HostPlatform == "linux" ? "Linux"
                           : TestEnvironment.HostPlatform == "macos" ? "macOS"
                           : "Windows";
        var exeProject = Path.Combine(sampleDir, $"{sampleName}.{platformSuffix}", $"{sampleName}.{platformSuffix}.csproj");
        if (!File.Exists(exeProject))
            throw new FileNotFoundException($"Generated executable project not found: {exeProject}");

        NuGetConsumerFeed.WriteStrictNuGetConfig(sampleDir);

        var result = ConsumerBuild.Run(exeProject, sampleDir, output, timeoutMin: 10);
        Assert.True(result.ExitCode == 0, $"Consumer build of {sampleName} exited with {result.ExitCode}");

        // The game gets only the native libraries of its OS (_StrideTrimRuntimeAssets). The trim hooks MSBuild's
        // private copy lists, so a .NET SDK that renames them shows here as extra folders.
        var runtimes = Path.Combine(sampleDir, "Bin", platformSuffix, "Debug", "runtimes");
        Assert.True(Directory.Exists(runtimes), $"The game output has no runtimes folder: {runtimes}");
        string[] keptPrefixes = platformSuffix switch
        {
            "Windows" => ["win"],
            "Linux" => ["linux", "unix"],
            _ => ["osx", "unix"],
        };
        var otherPlatforms = Directory.GetDirectories(runtimes).Select(dir => Path.GetFileName(dir))
            .Where(rid => !keptPrefixes.Any(prefix => rid.StartsWith(prefix, StringComparison.Ordinal)))
            .ToList();
        Assert.True(otherPlatforms.Count == 0, $"The {platformSuffix} game output has the native libraries of other platforms: {string.Join(", ", otherPlatforms)}");
    }

    /// <summary>
    /// A rebuild of the same project with another graphics API must list that API's packages in the deps.json,
    /// not keep the one of the previous build: the API is not an input of the restore.
    /// </summary>
    [Fact]
    public void RebuildWithAnotherGraphicsApiRegeneratesDepsFile()
    {
        var caseDir = Path.Combine(TestEnvironment.CasesRoot, $"graphics-api-deps-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(caseDir);
            File.WriteAllText(Path.Combine(caseDir, "Consumer.csproj"), """
                <Project Sdk="Microsoft.NET.Sdk">
                  <PropertyGroup>
                    <TargetFramework>net10.0</TargetFramework>
                    <OutputType>Exe</OutputType>
                  </PropertyGroup>
                  <ItemGroup>
                    <PackageReference Include="Stride.Graphics" Version="$(StrideEngineVersion)" />
                  </ItemGroup>
                </Project>
                """);
            File.WriteAllText(Path.Combine(caseDir, "Program.cs"), "System.Console.WriteLine();");
            NuGetConsumerFeed.WriteStrictNuGetConfig(caseDir);

            var version = TestEnvironment.ResolveStrideVersion();
            var project = Path.Combine(caseDir, "Consumer.csproj");
            var depsFile = Path.Combine(caseDir, "bin", "Debug", "net10.0", "Consumer.deps.json");

            string Build(string api)
            {
                var result = ConsumerBuild.Run(project, caseDir, output, timeoutMin: 10,
                    $"-p:StrideGraphicsApi={api}", $"-p:StrideEngineVersion={version}", $"-p:RestorePackagesPath={TestEnvironment.RunPackagesFolder}");
                Assert.True(result.ExitCode == 0, $"Consumer build with {api} exited with {result.ExitCode}");
                return File.ReadAllText(depsFile);
            }

            var direct3D11 = Build("Direct3D11");
            Assert.Contains("Silk.NET.Direct3D.Compilers.dll", direct3D11);
            Assert.DoesNotContain("Vortice.Vulkan.dll", direct3D11);

            var vulkan = Build("Vulkan");
            Assert.Contains("Vortice.Vulkan.dll", vulkan);
            Assert.DoesNotContain("Silk.NET.Direct3D.Compilers.dll", vulkan);
        }
        finally
        {
            TestEnvironment.DeleteCaseDirectory(caseDir, output);
        }
    }
}
