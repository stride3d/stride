// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class ProjectVersionReaderTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "stride-version-tests", Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(root, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void RestoredVersionWinsWhenUpToDate()
    {
        var project = WriteProject("4.3.0.2503", DateTime.UtcNow.AddMinutes(-10));
        WriteAssets("4.3.0.2507", DateTime.UtcNow);

        Assert.False(ProjectVersionReader.IsRestoreStale(project));
        Assert.Equal("4.3.0.2507", ReadVersion(project));
    }

    [Fact]
    public void ProjectEditedAfterItsRestoreUsesItsOwnVersion()
    {
        WriteAssets("4.3.0.2507", DateTime.UtcNow.AddMinutes(-10));
        var project = WriteProject("4.3.0.2503", DateTime.UtcNow);

        Assert.True(ProjectVersionReader.IsRestoreStale(project));
        Assert.Equal("4.3.0.2503", ReadVersion(project));
    }

    [Fact]
    public void ProjectNeverRestoredUsesItsOwnVersion()
    {
        var project = WriteProject("[4.4.0-beta8]", DateTime.UtcNow);

        Assert.True(ProjectVersionReader.IsRestoreStale(project));
        Assert.Equal("4.4.0-beta8", ReadVersion(project));
    }

    [Fact]
    public void CentralVersionEditedAfterTheRestoreIsUsed()
    {
        var project = Write("Game/Game.csproj", """<Project><ItemGroup><PackageReference Include="Stride.Engine" /></ItemGroup></Project>""", DateTime.UtcNow.AddMinutes(-20));
        WriteAssets("4.3.0.2507", DateTime.UtcNow.AddMinutes(-10));
        Write("Directory.Packages.props", """<Project><ItemGroup><PackageVersion Include="Stride.Engine" Version="4.3.0.2503" /></ItemGroup></Project>""", DateTime.UtcNow);

        Assert.True(ProjectVersionReader.IsRestoreStale(project));
        Assert.Equal("4.3.0.2503", ReadVersion(project));
    }

    [Fact]
    public void PlatformProjectAsksTheGameProject()
    {
        // The platform project has the engine in its assets file through its reference to the game project
        var platform = Write("Game.Windows/Game.Windows.csproj", """<Project><ItemGroup><ProjectReference Include="..\Game\Game.csproj" /></ItemGroup></Project>""", DateTime.UtcNow.AddMinutes(-20));
        Write("Game.Windows/obj/project.assets.json", """{ "libraries": { "Stride.Engine/4.3.0.2507": { "type": "package" }, "Game/1.0.0": { "type": "project" } } }""", DateTime.UtcNow.AddMinutes(-10));
        WriteAssets("4.3.0.2507", DateTime.UtcNow.AddMinutes(-10));
        WriteProject("4.3.0.2503", DateTime.UtcNow.AddMinutes(-20));
        Assert.Equal("4.3.0.2507", ReadVersion(platform));

        // Only the game project is edited
        WriteProject("4.3.0.2503", DateTime.UtcNow);
        Assert.Equal("4.3.0.2503", ReadVersion(platform));
    }

    [Theory]
    [InlineData("$(StrideVersion)")]
    [InlineData("4.3.*")]
    [InlineData("[4.3,4.4)")]
    public void VersionNotWrittenAsASingleOneComesFromTheRestore(string declared)
    {
        WriteAssets("4.3.0.2507", DateTime.UtcNow.AddMinutes(-10));
        var project = WriteProject(declared, DateTime.UtcNow);

        Assert.Equal("4.3.0.2507", ReadVersion(project));
    }

    private static string? ReadVersion(string project) => ProjectVersionReader.ReadVersion(project, "Stride.Engine", "Xenko.Engine");

    private string WriteProject(string version, DateTime time)
        => Write("Game/Game.csproj", $"""<Project><ItemGroup><PackageReference Include="Stride.Engine" Version="{version}" /></ItemGroup></Project>""", time);

    private void WriteAssets(string version, DateTime time)
        => Write("Game/obj/project.assets.json", $$"""{ "libraries": { "Stride.Core/{{version}}": { "type": "package" }, "Stride.Engine/{{version}}": { "type": "package" } } }""", time);

    private string Write(string relative, string content, DateTime time)
    {
        var path = Path.GetFullPath(Path.Combine(root, relative));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        File.SetLastWriteTimeUtc(path, time);
        return path;
    }
}
