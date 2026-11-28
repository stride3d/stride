// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.TemplateGenerator;
using Xunit;

namespace Stride.Templates.Tests;

/// <summary>
/// The solution of a sample template: generated from its projects, even when the sample folder has a solution of its own.
/// </summary>
public sealed class TemplatePreprocessorSolutionTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), $"stride-preprocessor-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, recursive: true);
    }

    [Fact]
    public void ASampleWithoutASolutionGetsOne()
    {
        Sample();

        var (succeeded, log) = Run();

        Assert.True(succeeded, log.ToText());
        AssertGenerated();
    }

    [Fact]
    public void TheSolutionOfTheSampleIsReplaced()
    {
        // As BepuSample's: the pack it references by its path in the samples tree, and its Windows project for any platform
        Sample();
        Write("Sample/Shipped.slnx", """
            <Solution>
              <Project Path="../../Templates/Packs/Pack/Pack.csproj" />
              <Project Path="Shipped.Game/Shipped.Game.csproj" />
              <Project Path="Shipped.Windows/Shipped.Windows.csproj" />
            </Solution>
            """);

        var (succeeded, log) = Run();

        Assert.True(succeeded, log.ToText());
        AssertGenerated();
        Assert.DoesNotContain("Packs", File.ReadAllText(Output("MyTemplate.slnx")));
    }

    private void AssertGenerated()
    {
        Assert.Equal(["MyTemplate.slnx"], Directory.EnumerateFiles(Output("."), "*.sln*").Select(Path.GetFileName));
        var solution = File.ReadAllText(Output("MyTemplate.slnx"));
        Assert.Contains("""<Project Path="MyTemplate.Game/MyTemplate.Game.csproj" />""", solution);
        Assert.Contains("#if (WindowsActive)", solution);
        Assert.Contains("""<Project Path="MyTemplate.Windows/MyTemplate.Windows.csproj" DefaultStartup="true" />""", solution);
    }

    // A sample template with a game package and a Windows project.
    private void Sample()
    {
        Write("Sample/Shipped.sdtpl", """
            !TemplateSample
            Id: 0a1b2c3d-0000-4000-8000-000000000010
            Name: "Sample: Shipped"
            Scope: Session
            Group: Samples
            DefaultOutputName: Shipped
            """);
        Write("Sample/Shipped.Game/Shipped.Game.csproj", """<Project Sdk="Microsoft.NET.Sdk" />""");
        Write("Sample/Shipped.Game/Shipped.Game.sdpkg", """
            !Package
            SerializedVersion: {Assets: 3.1.0.0}
            Meta:
                Name: Shipped.Game
                Version: 1.0.0
            """);
        Write("Sample/Shipped.Windows/Shipped.Windows.csproj", """
            <Project Sdk="Microsoft.NET.Sdk">
              <ItemGroup>
                <ProjectReference Include="..\Shipped.Game\Shipped.Game.csproj" />
              </ItemGroup>
            </Project>
            """);
    }

    private (bool Succeeded, LoggerResult Log) Run()
    {
        var log = new LoggerResult();
        var preprocessor = new TemplatePreprocessor { InputPath = Path.Combine(root, "Sample"), OutputDirectory = Path.Combine(root, "out"), SkipPrune = true };
        return (preprocessor.Run(log), log);
    }

    private string Output(string path) => Path.GetFullPath(Path.Combine(root, "out", path));

    private void Write(string path, string text)
    {
        var file = Path.GetFullPath(Path.Combine(root, path));
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, text);
    }
}
