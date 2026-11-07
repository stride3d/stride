// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Diagnostics;
using Xunit;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The editor's filter of the Editor companions a session loads (<see cref="PackageSession.CompanionPackageFilter"/>): a
/// companion it skips is not loaded, nor are its own companions; an Assets companion is never filtered.
/// </summary>
public class TestCompanionPackageFilter
{
    private static readonly PackageVersion Version = new("1.0.0");

    [Fact]
    public void ACompanionInTheSkipSetIsNotLoaded()
    {
        var (session, game, companion) = CreateSession("Plugin.Editor");
        var filter = new SkipSet("Plugin.Editor");
        session.CompanionPackageFilter = filter;

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.DoesNotContain(game.FlattenedDependencies, x => x.Package == companion);
        Assert.Equal(["Plugin.Editor"], filter.Skipped);
    }

    [Fact]
    public void ACompanionOutOfTheSkipSetLoads()
    {
        var (session, game, companion) = CreateSession("Plugin.Editor");
        var filter = new SkipSet("Other.Editor");
        session.CompanionPackageFilter = filter;

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Contains(game.FlattenedDependencies, x => x.Package == companion);
        Assert.Equal(["Plugin.Editor"], filter.Asked);
        Assert.Empty(filter.Skipped);
    }

    [Fact]
    public void AnUnknownNameInTheSkipSetChangesNothing()
    {
        var (session, game, companion) = CreateSession("Plugin.Editor");
        session.CompanionPackageFilter = new SkipSet("Not.A.Package.Of.This.Session");
        var log = new LoggerResult();

        session.LoadCompanionPackages(null, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Contains(game.FlattenedDependencies, x => x.Package == companion);
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
    }

    [Fact]
    public void WithoutAFilterEveryCompanionLoads()
    {
        var (session, game, companion) = CreateSession("Plugin.Editor");

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Null(session.CompanionPackageFilter);
        Assert.Contains(game.FlattenedDependencies, x => x.Package == companion);
    }

    [Fact]
    public void AnAssetsCompanionIsNeverFiltered()
    {
        var session = new PackageSession();
        var (game, plugin) = AddGameAndPlugin(session);
        var (assets, declaration) = AddCompanion(session, "Plugin.Assets", PackageKind.Assets, toolkit: null);
        plugin.CompanionPackages.Add(declaration);
        var filter = new SkipSet("Plugin.Assets");
        session.CompanionPackageFilter = filter;

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Contains(game.FlattenedDependencies, x => x.Package == assets);
        Assert.Empty(filter.Asked);
    }

    [Fact]
    public void TheCompanionsOfASkippedCompanionAreNotLoadedEither()
    {
        var (session, game, companion) = CreateSession("Plugin.Editor");
        var (view, viewDeclaration) = AddCompanion(session, "Plugin.Editor.Avalonia", PackageKind.Editor, EditorToolkits.Avalonia);
        companion.CompanionPackages.Add(viewDeclaration);
        session.CompanionPackageFilter = new SkipSet("Plugin.Editor");

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.DoesNotContain(game.FlattenedDependencies, x => x.Package == companion);
        Assert.DoesNotContain(game.FlattenedDependencies, x => x.Package == view);
    }

    [Fact]
    public void ACompanionOfAnotherToolkitIsNotAskedFor()
    {
        var session = new PackageSession();
        var (_, plugin) = AddGameAndPlugin(session);
        var (_, declaration) = AddCompanion(session, "Plugin.Editor.Wpf", PackageKind.Editor, EditorToolkits.Wpf);
        plugin.CompanionPackages.Add(declaration);
        var filter = new SkipSet();
        session.CompanionPackageFilter = filter;

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Empty(filter.Asked);
    }

    [Fact]
    public void ASkippedCompanionProjectOfTheSolutionDoesNotLoadItsAssembly()
    {
        var session = new PackageSession();
        var (game, plugin) = AddGameAndPlugin(session);
        var companion = new Package { Meta = { Name = "Plugin.Editor", Version = Version } };
        var companionProject = new SolutionProject(companion, Guid.NewGuid(), "Plugin.Editor.csproj") { Type = ProjectType.Library };
        session.Projects.Add(companionProject);
        plugin.CompanionPackages.Add(new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor });
        session.CompanionPackageFilter = new SkipSet("Plugin.Editor");

        session.LoadCompanionPackages(null, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.True(companionProject.IsSkippedByThisHost);
        Assert.False(companionProject.ShouldLoadAssemblyInEditor);
        Assert.DoesNotContain(game.FlattenedDependencies, x => x.Package == companion);
    }

    [Fact]
    public void EachCompanionIsAskedForAndLoggedOnceWhateverTheNumberOfLoads()
    {
        var (session, _, _) = CreateSession("Plugin.Editor");
        var plugin = session.Packages.Single(x => x.Meta.Name == "Plugin");
        var (_, wpfDeclaration) = AddCompanion(session, "Plugin.Editor.Wpf", PackageKind.Editor, EditorToolkits.Wpf);
        plugin.CompanionPackages.Add(wpfDeclaration);
        var filter = new SkipSet("Plugin.Editor");
        session.CompanionPackageFilter = filter;
        var log = new LoggerResult();

        session.LoadCompanionPackages(null, log, loadEditorPackages: true, EditorToolkits.Avalonia);
        session.LoadCompanionPackages(null, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Equal(["Plugin.Editor"], filter.Asked);
        Assert.Single(log.Messages, x => x.Text.StartsWith("[Plugin.Editor.Wpf]", StringComparison.Ordinal) && x.Text.Contains("is written for the Wpf editor", StringComparison.Ordinal));
    }

    [Fact]
    public void TheSessionKeepsTheFilterOfItsFirstLoad()
    {
        var session = new PackageSession();
        var first = new SkipSet();
        session.LoadMissingDependencies(new LoggerResult(), new PackageLoadParameters { CompanionPackageFilter = first, LoadEditorPackages = true });
        session.LoadMissingDependencies(new LoggerResult(), new PackageLoadParameters { CompanionPackageFilter = new SkipSet(), LoadEditorPackages = true });

        Assert.Same(first, session.CompanionPackageFilter);
    }

    private sealed class SkipSet(params string[] names) : ICompanionPackageFilter
    {
        private readonly HashSet<string> names = new(names, StringComparer.OrdinalIgnoreCase);

        public List<string> Asked { get; } = [];

        public List<string> Skipped { get; } = [];

        public bool ShouldLoad(Package? declaringPackage, CompanionPackage companion, ILogger log)
        {
            Asked.Add(companion.Name!);
            if (!names.Contains(companion.Name!))
                return true;
            Skipped.Add(companion.Name!);
            return false;
        }
    }

    // A game depending on Plugin, which declares one neutral Editor companion, already in the session
    private static (PackageSession Session, PackageContainer Game, Package Companion) CreateSession(string name)
    {
        var session = new PackageSession();
        var (game, plugin) = AddGameAndPlugin(session);
        var (companion, declaration) = AddCompanion(session, name, PackageKind.Editor, toolkit: null);
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

    private static (Package Companion, CompanionPackage Declaration) AddCompanion(PackageSession session, string name, PackageKind kind, string? toolkit)
    {
        var companion = new Package { Meta = { Name = name, Version = Version }, Kind = kind, Toolkit = toolkit };
        session.Projects.Add(new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true });
        return (companion, new CompanionPackage { Name = name, Version = Version, Kind = kind, Toolkit = toolkit });
    }
}
