// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Editor.Tests;

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
        var slnPath = EditorScreenshotTests.GenerateSampleFromTemplate(templateGuid, sampleName);
        var sampleDir = Path.GetDirectoryName(slnPath)!;

        // Build the .Windows executable project directly: the generated .sln has no
        // ProjectConfigurationPlatforms section, so `dotnet build <sln>` builds nothing.
        // The .Windows project triggers asset compilation and references .Game.
        var exeProject = Path.Combine(sampleDir, sampleName + ".Windows", sampleName + ".Windows.csproj");
        if (!File.Exists(exeProject))
            throw new FileNotFoundException($"Generated executable project not found: {exeProject}");

        WriteStrictNuGetConfig(sampleDir);

        var psi = new ProcessStartInfo("dotnet")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            WorkingDirectory = sampleDir,
        };
        psi.ArgumentList.Add("build");
        psi.ArgumentList.Add(exeProject);
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add("Debug");
        psi.ArgumentList.Add("-nr:false");
        psi.ArgumentList.Add("-v:m");

        using var proc = Process.Start(psi)!;
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) output.WriteLine($"[stdout] {e.Data}"); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) output.WriteLine($"[stderr] {e.Data}"); };
        proc.BeginOutputReadLine();
        proc.BeginErrorReadLine();

        const int timeoutMin = 10;
        if (!proc.WaitForExit(timeoutMin * 60_000))
        {
            try { proc.Kill(entireProcessTree: true); } catch { }
            throw new TimeoutException($"Consumer build of {sampleName} timed out after {timeoutMin}min");
        }
        Assert.True(proc.ExitCode == 0, $"Consumer build of {sampleName} exited with {proc.ExitCode}");
    }

    /// <summary>
    /// nuget.config resolving Stride.* only from the workspace bin/packages feed — no public
    /// fallback for first-party packages, so a missing-from-pack package fails as NU1101
    /// instead of silently resolving a public version. Mirrors the workspace's own
    /// packageSourceMapping (Stride.Dependencies.* etc. still flow from nuget.org).
    /// </summary>
    private static void WriteStrictNuGetConfig(string sampleDir)
    {
        var binPackages = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "bin", "packages"));
        if (!Directory.Exists(binPackages))
            throw new DirectoryNotFoundException($"bin/packages not found at {binPackages}; build the engine first.");

        File.WriteAllText(Path.Combine(sampleDir, "nuget.config"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
                <add key="stride-local" value="{binPackages}" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                  <package pattern="Stride.GNU.*" />
                  <package pattern="Stride.Mono.*" />
                  <package pattern="Stride.Dependencies.*" />
                  <package pattern="Stride.GraphX.*" />
                  <package pattern="Stride.Metrics" />
                  <package pattern="Stride.QuickGraph" />
                </packageSource>
                <packageSource key="stride-local">
                  <package pattern="Stride" />
                  <package pattern="Stride.*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """);
    }
}
