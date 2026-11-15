// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Xunit;

namespace Stride.Core.Assets.Tests;

public class TestCompanionDependencies
{
    [Fact]
    public void CompanionsComeBackInADependencyListRebuiltFromTheLockFile()
    {
        var session = new PackageSession();
        var game = new Package { Meta = { Name = "Game" } };
        var gameProject = new SolutionProject(game, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(gameProject);
        var plugin = AddPackage(session, "Plugin");
        var pluginAssets = AddPackage(session, "Plugin.Assets");
        var pluginEditor = AddPackage(session, "Plugin.Editor.Wpf");
        gameProject.FlattenedDependencies.Add(new Dependency(plugin));

        session.AddCompanionLink(plugin, pluginAssets);
        session.AddCompanionLink(pluginAssets, pluginEditor);
        Assert.Contains(gameProject.FlattenedDependencies, x => x.Package == pluginAssets);
        Assert.Contains(gameProject.FlattenedDependencies, x => x.Package == pluginEditor);

        // The lock file lists the plugin only
        gameProject.FlattenedDependencies.Clear();
        gameProject.FlattenedDependencies.Add(new Dependency(plugin));
        session.AddCompanionDependencies(gameProject);

        Assert.Single(gameProject.FlattenedDependencies, x => x.Package == pluginAssets);
        Assert.Single(gameProject.FlattenedDependencies, x => x.Package == pluginEditor);
    }

    [Fact]
    public void CompanionsOfAnotherPackageOrRemovedOnesAreNotAdded()
    {
        var session = new PackageSession();
        var game = new Package { Meta = { Name = "Game" } };
        var gameProject = new SolutionProject(game, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(gameProject);
        var plugin = AddPackage(session, "Plugin");
        var pluginAssets = AddPackage(session, "Plugin.Assets");
        var otherPlugin = AddPackage(session, "OtherPlugin");
        var otherAssets = AddPackage(session, "OtherPlugin.Assets");
        session.AddCompanionLink(plugin, pluginAssets);
        session.AddCompanionLink(otherPlugin, otherAssets);
        session.Projects.Remove(session.Projects.First(x => x.Package == pluginAssets));

        gameProject.FlattenedDependencies.Add(new Dependency(plugin));
        session.AddCompanionDependencies(gameProject);

        Assert.Single(gameProject.FlattenedDependencies);
    }

    private static Package AddPackage(PackageSession session, string name)
    {
        var package = new Package { Meta = { Name = name } };
        session.Projects.Add(new StandalonePackage(package) { IsDependencyPackage = true });
        return package;
    }
}
