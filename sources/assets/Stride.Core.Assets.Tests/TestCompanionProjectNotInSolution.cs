// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The error of a companion that could not be loaded: a companion whose project the declaring project names was left out of the solution.
/// </summary>
public class TestCompanionProjectNotInSolution
{
    private static readonly PackageVersion Version = new("1.0.0");

    [Fact]
    public void CompanionProjectLeftOutOfTheSolutionIsNamed()
    {
        var project = new UFile(Path.Combine(Path.GetTempPath(), "Plugin", "Plugin.Editor", "Plugin.Editor.csproj"));

        var log = Load(project);

        var error = Assert.Single(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("[Plugin.Editor]", StringComparison.Ordinal) && x.Text.Contains("[Plugin]", StringComparison.Ordinal));
        Assert.Equal($"The Editor package [Plugin.Editor] of [Plugin] is the project [{project.ToOSPath()}], which is not in the solution: add it to the solution.", error.Text);
    }

    [Fact]
    public void PackedCompanionNotFoundKeepsItsMessage()
    {
        var log = Load(project: null);

        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text == "Editor package [Plugin.Editor] version [1.0.0], declared by [Plugin], was not found; assets of that package cannot be compiled or edited.");
    }

    // A game depending on Plugin, which declares an Editor companion that is neither in the session nor in the feed
    private static LoggerResult Load(UFile? project)
    {
        using var feed = new LocalPackageFeed();
        var session = new PackageSession();
        var game = new SolutionProject(new Package { Meta = { Name = "Game", Version = Version } }, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(game);
        var plugin = new Package { Meta = { Name = "Plugin", Version = Version } };
        plugin.CompanionPackages.Add(new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor, Project = project });
        session.Projects.Add(new StandalonePackage(plugin) { IsDependencyPackage = true });
        game.FlattenedDependencies.Add(new Dependency(plugin));
        var log = new LoggerResult();

        session.LoadCompanionPackages(feed.Root, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Plugin.Editor");
        return log;
    }
}
