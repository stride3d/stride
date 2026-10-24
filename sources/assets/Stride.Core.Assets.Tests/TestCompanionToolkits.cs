// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Which host loads which companion (<see cref="PackageSession.LoadCompanionPackages"/>): the asset compiler takes the
/// Assets companions, an editor takes the neutral Editor companions and those of its own toolkit.
/// </summary>
public class TestCompanionToolkits
{
    private static readonly PackageVersion Version = new("1.0.0");

    [Theory]
    [InlineData(EditorToolkits.Wpf)]
    [InlineData(EditorToolkits.Avalonia)]
    public void NeutralEditorCompanionLoadsInEveryEditor(string editorToolkit)
    {
        var (session, game, companion) = CreateSession(toolkit: null);

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, editorToolkit);

        Assert.Contains(game.FlattenedDependencies, x => x.Package == companion);
    }

    [Theory]
    [InlineData(EditorToolkits.Wpf, EditorToolkits.Wpf, true)]
    [InlineData(EditorToolkits.Wpf, EditorToolkits.Avalonia, false)]
    [InlineData(EditorToolkits.Avalonia, EditorToolkits.Avalonia, true)]
    [InlineData(EditorToolkits.Avalonia, EditorToolkits.Wpf, false)]
    public void ToolkitEditorCompanionLoadsInItsOwnEditorOnly(string companionToolkit, string editorToolkit, bool loaded)
    {
        var (session, game, companion) = CreateSession(companionToolkit);
        var log = new LoggerResult();

        session.LoadCompanionPackages(null, log, loadEditorPackages: true, editorToolkit);

        Assert.Equal(loaded, game.FlattenedDependencies.Any(x => x.Package == companion));
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
        if (!loaded)
            Assert.Contains(log.Messages, x => x.Text.Contains($"written for the {companionToolkit} editor", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(EditorToolkits.Wpf)]
    [InlineData(EditorToolkits.Avalonia)]
    public void AssetCompilerLoadsNoEditorCompanion(string? companionToolkit)
    {
        var (session, game, companion) = CreateSession(companionToolkit);
        var (assets, assetsDeclaration) = AddCompanion(session, "Plugin.Assets", PackageKind.Assets, toolkit: null);
        session.Packages.Single(x => x.Meta.Name == "Plugin").CompanionPackages.Add(assetsDeclaration);

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: false, editorToolkit: null);

        Assert.DoesNotContain(game.FlattenedDependencies, x => x.Package == companion);
        Assert.Contains(game.FlattenedDependencies, x => x.Package == assets);
    }

    [Theory]
    [InlineData("wpf")]
    [InlineData("WPF")]
    public void ToolkitCompareIgnoresCase(string editorToolkit)
    {
        var (session, game, companion) = CreateSession(EditorToolkits.Wpf);

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, editorToolkit);

        Assert.Contains(game.FlattenedDependencies, x => x.Package == companion);
    }

    [Theory]
    [InlineData(EditorToolkits.Wpf, EditorToolkits.Wpf, false)]
    [InlineData(EditorToolkits.Wpf, EditorToolkits.Avalonia, true)]
    [InlineData(EditorToolkits.Avalonia, EditorToolkits.Wpf, true)]
    [InlineData(null, EditorToolkits.Avalonia, false)]
    public void SkippedCompanionProjectOfTheSolutionIsMarked(string? companionToolkit, string editorToolkit, bool skipped)
    {
        var session = new PackageSession();
        var (game, plugin) = AddGameAndPlugin(session);
        var companion = new Package { Meta = { Name = "Plugin.Editor.View", Version = Version } };
        var companionProject = new SolutionProject(companion, Guid.NewGuid(), "Plugin.Editor.View.csproj");
        session.Projects.Add(companionProject);
        plugin.CompanionPackages.Add(new CompanionPackage { Name = "Plugin.Editor.View", Version = Version, Kind = PackageKind.Editor, Toolkit = companionToolkit });

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, editorToolkit);

        Assert.Equal(skipped, companionProject.IsSkippedByThisHost);
        Assert.Equal(!skipped, companionProject.ShouldLoadAssemblyInEditor);
        Assert.Equal(!skipped, game.FlattenedDependencies.Any(x => x.Package == companion));
    }

    // A game depending on Plugin, which declares one Editor companion, already in the session
    private static (PackageSession Session, PackageContainer Game, Package Companion) CreateSession(string? toolkit)
    {
        var session = new PackageSession();
        var (game, plugin) = AddGameAndPlugin(session);
        var (companion, declaration) = AddCompanion(session, "Plugin.Editor", PackageKind.Editor, toolkit);
        plugin.CompanionPackages.Add(declaration);
        return (session, game, companion);
    }

    private static (PackageContainer Game, Package Plugin) AddGameAndPlugin(PackageSession session)
    {
        var game = new SolutionProject(new Package { Meta = { Name = "Game", Version = Version } }, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(game);
        var plugin = new Package { Meta = { Name = "Plugin", Version = Version } };
        session.Projects.Add(new StandalonePackage(plugin) { IsDependencyPackage = true });
        game.FlattenedDependencies.Add(new Dependency(plugin));
        return (game, plugin);
    }

    // A packed companion states its kind and toolkit, which must match the declaration
    private static (Package Companion, CompanionPackage Declaration) AddCompanion(PackageSession session, string name, PackageKind kind, string? toolkit)
    {
        var companion = new Package { Meta = { Name = name, Version = Version }, Kind = kind, Toolkit = toolkit };
        session.Projects.Add(new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true });
        return (companion, new CompanionPackage { Name = name, Version = Version, Kind = kind, Toolkit = toolkit });
    }
}
