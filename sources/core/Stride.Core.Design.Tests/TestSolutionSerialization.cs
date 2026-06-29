// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using Stride.Core.Solutions;
using Xunit;

namespace Stride.Core.Design.Tests;

public class TestSolutionSerialization
{
    // A solution with a custom Testing platform and a project that deliberately does not build in Testing.
    private const string CustomSolution =
        "Microsoft Visual Studio Solution File, Format Version 12.00\r\n" +
        "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Foreign\", \"Foreign\\Foreign.csproj\", \"{11111111-1111-1111-1111-111111111111}\"\r\n" +
        "EndProject\r\n" +
        "Project(\"{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}\") = \"Game\", \"Game\\Game.csproj\", \"{22222222-2222-2222-2222-222222222222}\"\r\n" +
        "EndProject\r\n" +
        "Global\r\n" +
        "\tGlobalSection(SolutionConfigurationPlatforms) = preSolution\r\n" +
        "\t\tDebug|Any CPU = Debug|Any CPU\r\n" +
        "\t\tRelease|Any CPU = Release|Any CPU\r\n" +
        "\t\tTesting|Any CPU = Testing|Any CPU\r\n" +
        "\tEndGlobalSection\r\n" +
        "\tGlobalSection(ProjectConfigurationPlatforms) = postSolution\r\n" +
        "\t\t{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n" +
        "\t\t{11111111-1111-1111-1111-111111111111}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n" +
        "\t\t{11111111-1111-1111-1111-111111111111}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n" +
        "\t\t{11111111-1111-1111-1111-111111111111}.Release|Any CPU.Build.0 = Release|Any CPU\r\n" +
        "\t\t{11111111-1111-1111-1111-111111111111}.Testing|Any CPU.ActiveCfg = Testing|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.ActiveCfg = Debug|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Debug|Any CPU.Build.0 = Debug|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Release|Any CPU.ActiveCfg = Release|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Release|Any CPU.Build.0 = Release|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Testing|Any CPU.ActiveCfg = Testing|Any CPU\r\n" +
        "\t\t{22222222-2222-2222-2222-222222222222}.Testing|Any CPU.Build.0 = Testing|Any CPU\r\n" +
        "\tEndGlobalSection\r\n" +
        "EndGlobal\r\n";

    [Fact]
    public void LoadAndSavePreservesUserConfiguration()
    {
        var dir = NewTempDir();
        try
        {
            var slnPath = Path.Combine(dir, "Custom.sln");
            File.WriteAllText(slnPath, CustomSolution);

            var solution = Solution.FromFile(slnPath);
            solution.Save();
            var written = File.ReadAllText(slnPath);

            // The custom Testing platform survives.
            Assert.Contains("Testing|Any CPU = Testing|Any CPU", written);
            // The legacy project type guid is NOT rewritten for a loaded project.
            Assert.Contains("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", written);
            // The Foreign project still does not build in Testing (no Build.0), while Game still does.
            Assert.DoesNotContain("{11111111-1111-1111-1111-111111111111}.Testing|Any CPU.Build.0", written);
            Assert.Contains("{22222222-2222-2222-2222-222222222222}.Testing|Any CPU.Build.0", written);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void NewSolutionGetsSdkGuidAndDefaultConfigurations()
    {
        var dir = NewTempDir();
        try
        {
            var slnPath = Path.Combine(dir, "New.sln");
            var solution = new Solution { FullPath = slnPath };
            solution.Projects.Add(new Project(
                Guid.Parse("33333333-3333-3333-3333-333333333333"), KnownProjectTypeGuid.CSharp,
                "New", Path.Combine(dir, "New", "New.csproj"), Guid.Empty, [], [], []));

            solution.Save();
            var written = File.ReadAllText(slnPath);

            Assert.Contains("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}", written);
            Assert.DoesNotContain("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", written);
            Assert.Contains("Debug|Any CPU = Debug|Any CPU", written);
            Assert.Contains("{33333333-3333-3333-3333-333333333333}.Release|Any CPU.Build.0", written);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SlnxRoundTripsAsXml()
    {
        var dir = NewTempDir();
        try
        {
            // Create as a .sln, then save the same model to a .slnx and confirm it is written as XML.
            var slnxPath = Path.Combine(dir, "Game.slnx");
            var solution = new Solution { FullPath = slnxPath };
            solution.Projects.Add(new Project(
                Guid.Parse("44444444-4444-4444-4444-444444444444"), KnownProjectTypeGuid.CSharp,
                "Game", Path.Combine(dir, "Game", "Game.csproj"), Guid.Empty, [], [], []));
            solution.Save();

            var written = File.ReadAllText(slnxPath);
            Assert.Contains("<Solution>", written);
            Assert.Contains("Game/Game.csproj", written);

            // It loads back through the same entry point.
            var reloaded = Solution.FromFile(slnxPath);
            Assert.Single(reloaded.Projects);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SlnxOmitsIdsAndRoundTripsTheStartupProject()
    {
        var dir = NewTempDir();
        try
        {
            var slnxPath = Path.Combine(dir, "Game.slnx");
            var solution = new Solution { FullPath = slnxPath };
            solution.Projects.Add(new Project(
                Guid.NewGuid(), KnownProjectTypeGuid.CSharp,
                "Game", Path.Combine(dir, "Game", "Game.csproj"), Guid.Empty, [], [], []));
            var windowsGuid = Guid.NewGuid();
            solution.Projects.Add(new Project(
                windowsGuid, KnownProjectTypeGuid.CSharp,
                "Game.Windows", Path.Combine(dir, "Game.Windows", "Game.Windows.csproj"), Guid.Empty, [], [], []));
            solution.StartupProjectGuid = windowsGuid;
            solution.Save();

            var written = File.ReadAllText(slnxPath);
            Assert.Contains("DefaultStartup=\"true\"", written);
            Assert.DoesNotContain("Id=\"", written);   // .slnx carries no project ids
            Assert.DoesNotContain("Type=\"", written);  // nor an explicit project type (inferred from .csproj)

            // The startup project survives a reload, matched back by its path.
            var reloaded = Solution.FromFile(slnxPath);
            var startup = reloaded.Projects.First(project => project.Guid == reloaded.StartupProjectGuid);
            Assert.EndsWith("Game.Windows.csproj", startup.FullPath.Replace('\\', '/'));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void SolutionFilterResolvesToUnderlyingSolution()
    {
        var dir = NewTempDir();
        try
        {
            var slnPath = Path.Combine(dir, "Custom.sln");
            File.WriteAllText(slnPath, CustomSolution);

            var filterPath = Path.Combine(dir, "Custom.slnf");
            File.WriteAllText(filterPath,
                "{ \"solution\": { \"path\": \"Custom.sln\", \"projects\": [ \"Game\\\\Game.csproj\" ] } }");

            var solution = Solution.FromFile(filterPath);
            // The underlying solution is opened, and saves target it rather than the filter.
            Assert.Equal(2, solution.Projects.Count);
            Assert.Equal(slnPath, solution.FullPath);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    private static string NewTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "sln-roundtrip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
