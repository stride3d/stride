// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Packaging.Tests;

/// <summary>
/// Smoke-tests the packed Stride packages as a real external consumer: generate a sample,
/// pin Stride.* to the locally-built bin/packages feed via a strict nuget.config, then
/// <c>dotnet build</c> it as a subprocess. Catches what editor screenshot tests can't —
/// the <c>dotnet build</c> CLI path (vs Stride's in-process VSProjectHelper), and packaging
/// integrity: a package missing from the pack or a broken nuspec fails restore here.
/// </summary>
[Collection("Packaging")]
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
    }
}
