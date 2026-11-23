// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// <see cref="VSProjectHelper.RestoreNugetPackages"/> reports a restore that failed.
/// </summary>
public class TestRestoreNugetPackages : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), $"stride-restore-test-{Guid.NewGuid():N}");

    public TestRestoreNugetPackages()
    {
        // An empty local feed, and nothing else
        Directory.CreateDirectory(Path.Combine(directory, "feed"));
        new XDocument(new XElement("configuration",
            new XElement("config", new XElement("add", new XAttribute("key", "globalPackagesFolder"), new XAttribute("value", Path.Combine(directory, "packages")))),
            new XElement("packageSources", new XElement("clear"), new XElement("add", new XAttribute("key", "feed"), new XAttribute("value", Path.Combine(directory, "feed"))))))
            .Save(Path.Combine(directory, "nuget.config"));
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }

    private string WriteProject(string? packageId)
    {
        var path = Path.Combine(directory, "Game.csproj");
        new XDocument(new XElement("Project", new XAttribute("Sdk", "Microsoft.NET.Sdk"),
            new XElement("PropertyGroup", new XElement("TargetFramework", "net10.0")),
            new XElement("ItemGroup", packageId is null ? null : new XElement("PackageReference", new XAttribute("Include", packageId), new XAttribute("Version", "1.0.0")))))
            .Save(path);
        return path;
    }

    [Fact]
    public async Task AFailedRestoreThrowsWithItsErrors()
    {
        var project = WriteProject("Stride.Restore.Test.Missing");
        var log = new LoggerResult();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => VSProjectHelper.RestoreNugetPackages(log, project));

        Assert.Contains("NU1101", exception.Message);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("NU1101"));
    }

    [Fact]
    public async Task ARestoreThatSucceedsDoesNotThrow()
    {
        var project = WriteProject(null);

        await VSProjectHelper.RestoreNugetPackages(new LoggerResult(), project);

        Assert.True(File.Exists(Path.Combine(directory, "obj", "project.assets.json")));
    }
}
