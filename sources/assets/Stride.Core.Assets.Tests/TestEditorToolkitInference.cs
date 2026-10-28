// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The toolkit the build records for an Editor package (<c>_StrideInferEditorToolkit</c> in
/// Stride.AssetBuildManifest.targets), evaluated by MSBuild on fixture projects that import the targets file only: no
/// SDK, no restore, so each case takes about a second.
/// </summary>
public class TestEditorToolkitInference(ITestOutputHelper output)
{
    [Theory]
    [InlineData("""<ProjectReference Include="../Stride.Assets.Presentation.Common/Stride.Assets.Presentation.Common.csproj" /><PackageReference Include="Stride.Core" />""", "", null)]
    [InlineData("""<ProjectReference Include="../Stride.Assets.Presentation.Wpf/Stride.Assets.Presentation.Wpf.csproj" />""", "", EditorToolkits.Wpf)]
    [InlineData("""<PackageReference Include="Stride.Assets.Presentation.Wpf" />""", "", EditorToolkits.Wpf)]
    [InlineData("", "<UseWPF>true</UseWPF>", EditorToolkits.Wpf)]
    [InlineData("""<ProjectReference Include="../Stride.Core.Presentation.Avalonia/Stride.Core.Presentation.Avalonia.csproj" />""", "", EditorToolkits.Avalonia)]
    [InlineData("""<PackageReference Include="Dock.Avalonia" />""", "", EditorToolkits.Avalonia)]
    [InlineData("""<PackageReference Include="Avalonia" />""", "", EditorToolkits.Avalonia)]
    [InlineData("""<PackageReference Include="Avalonia.Desktop" />""", "", EditorToolkits.Avalonia)]
    // An explicit toolkit wins over the references
    [InlineData("""<PackageReference Include="Avalonia" /><ProjectReference Include="../Stride.Assets.Presentation.Wpf/Stride.Assets.Presentation.Wpf.csproj" />""", "<StrideEditorToolkit>Wpf</StrideEditorToolkit>", EditorToolkits.Wpf)]
    public void EditorPackageToolkitComesFromItsReferences(string items, string properties, string? toolkit)
    {
        var (exitCode, text) = Evaluate(items, "<StridePackageKind>Editor</StridePackageKind>" + properties, "GetStridePackageIdentity");

        Assert.True(exitCode == 0, text);
        var identity = ParseIdentity(text);
        Assert.Equal(toolkit ?? string.Empty, identity.GetProperty("Toolkit").GetString());
        Assert.Equal("Editor", identity.GetProperty("Kind").GetString());
    }

    [Fact]
    public void OnlyEditorPackagesHaveAToolkit()
    {
        var (exitCode, text) = Evaluate("""<PackageReference Include="Avalonia" />""", "<StridePackageKind>Assets</StridePackageKind>", "GetStridePackageIdentity");

        Assert.True(exitCode == 0, text);
        Assert.Equal(string.Empty, ParseIdentity(text).GetProperty("Toolkit").GetString());
    }

    [Theory]
    [InlineData("""<ProjectReference Include="../Stride.Assets.Presentation.Wpf/Stride.Assets.Presentation.Wpf.csproj" /><ProjectReference Include="../Stride.Core.Presentation.Avalonia/Stride.Core.Presentation.Avalonia.csproj" />""", "", "Stride.Assets.Presentation.Wpf", "Stride.Core.Presentation.Avalonia")]
    [InlineData("""<PackageReference Include="Avalonia" />""", "<UseWPF>true</UseWPF>", "UseWPF", "Avalonia")]
    public void EditorPackageBuiltAgainstBothEditorsFails(string items, string properties, string wpfCause, string avaloniaCause)
    {
        var (exitCode, text) = Evaluate(items, "<StridePackageKind>Editor</StridePackageKind>" + properties, "GetStridePackageIdentity");

        Assert.NotEqual(0, exitCode);
        var error = Assert.Single(text.Split('\n'), x => x.Contains("error", StringComparison.Ordinal) && x.Contains("built against both editors", StringComparison.Ordinal));
        Assert.Contains($"Wpf through {wpfCause}", error, StringComparison.Ordinal);
        Assert.Contains($"Avalonia through {avaloniaCause}", error, StringComparison.Ordinal);
        Assert.Contains("one companion package per toolkit", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EditorToolkits.Wpf, false)]
    [InlineData(EditorToolkits.Avalonia, false)]
    [InlineData("avalonia", false)]
    [InlineData("Avalnia", true)]
    public void UnknownToolkitWarns(string toolkit, bool warns)
    {
        var (exitCode, text) = Evaluate(
            $"""<StrideCompanionPackage Include="Plugin.Editor.View" Version="1.0.0" Kind="Editor" Toolkit="{toolkit}" />""",
            $"<StridePackageKind>Editor</StridePackageKind><StrideEditorToolkit>{toolkit}</StrideEditorToolkit>",
            "_StrideCheckEditorToolkit;_StrideCheckCompanionDeclarations");

        Assert.True(exitCode == 0, text);
        // One warning for the package's own toolkit, one for its companion's
        Assert.Equal(warns ? 2 : 0, text.Split('\n').Count(x => x.Contains("not a known editor toolkit", StringComparison.Ordinal)));
    }

    private static JsonElement ParseIdentity(string text)
    {
        using var json = JsonDocument.Parse(text[text.IndexOf('{')..]);
        return json.RootElement.GetProperty("TargetResults").GetProperty("GetStridePackageIdentity").GetProperty("Items").EnumerateArray().Single().Clone();
    }

    // Runs the targets on a project that imports Stride.AssetBuildManifest.targets only
    private (int ExitCode, string Output) Evaluate(string items, string properties, string targets)
    {
        var directory = Path.Combine(Path.GetTempPath(), "stride-toolkit-inference", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var project = Path.Combine(directory, "Plugin.Editor.csproj");
            File.WriteAllText(project,
                $"""
                <Project>
                  <PropertyGroup>{properties}</PropertyGroup>
                  <ItemGroup>{items}</ItemGroup>
                  <Import Project="{TargetsFile}" />
                </Project>
                """);

            var psi = new ProcessStartInfo
            {
                FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
                WorkingDirectory = directory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            foreach (var argument in new[] { "msbuild", project, "-nologo", "-nodeReuse:false", $"-t:{targets}" })
                psi.ArgumentList.Add(argument);
            if (targets == "GetStridePackageIdentity")
                psi.ArgumentList.Add("-getTargetResult:GetStridePackageIdentity");
            // The test host's MSBuild settings (set by dotnet test) must not leak into this build
            foreach (var name in new[] { "MSBuildExtensionsPath", "MSBUILD_EXE_PATH", "MSBuildSDKsPath", "MSBuildLoadMicrosoftTargetsReadOnly" })
                psi.Environment.Remove(name);

            using var process = Process.Start(psi)!;
            // Read both streams at once: reading one to the end first can deadlock once the other's pipe fills
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            process.WaitForExit();
            var text = (stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult()).Trim();
            output.WriteLine($"$ dotnet msbuild -t:{targets} ({items} {properties})");
            output.WriteLine(text);
            return (process.ExitCode, text);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string TargetsFile
    {
        get
        {
            // The test runs from its output directory inside the repository
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                var targets = Path.Combine(directory.FullName, "sources", "core", "Stride.Core", "build", "Stride.AssetBuildManifest.targets");
                if (File.Exists(targets))
                    return targets;
            }
            throw new InvalidOperationException("The test does not run inside the Stride repository.");
        }
    }
}
