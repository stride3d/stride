// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The companions a host adds to the sessions it loads (<see cref="PackageSession.CompanionPackageSource"/>): restored from the
/// source's own feed, asked to the filter with no declaring package, left out when the session already has the package, and
/// read once per session.
/// </summary>
public class TestCompanionPackageSource
{
    private static readonly PackageVersion Version = new("1.0.0");

    [Fact]
    public void ACompanionOfTheSourceIsRestoredFromItsFeedAndLoaded()
    {
        using var feed = new LocalPackageFeed();
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, title: "A tool");
        var source = new Source(feed.FeedDirectory, Declare("Tool.Editor"));
        var session = new PackageSession { CompanionPackageSource = source };
        var log = new LoggerResult();

        session.LoadCompanionPackages(feed.Root, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        var loaded = Assert.IsType<StandalonePackage>(session.Projects.Single(x => x.Package.Meta.Name == "Tool.Editor"));
        Assert.True(loaded.IsCompanionPackage);
        Assert.Equal(PackageKind.Editor, loaded.Package.Kind);
        Assert.Equal(["Tool.Editor"], session.LoadedSourceCompanionPackages);
        Assert.True(Directory.Exists(Path.Combine(feed.PackagesDirectory, "tool.editor", "1.0.0")));
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
    }

    [Fact]
    public void TheCompanionsOfACompanionOfTheSourceLoadWithIt()
    {
        using var feed = new LocalPackageFeed();
        var view = new CompanionPackage { Name = "Tool.Editor.Avalonia", Version = Version, Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia };
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, companions: [view]);
        feed.AddPackage("Tool.Editor.Avalonia", "1.0.0", PackageKind.Editor, EditorToolkits.Avalonia);
        var session = new PackageSession { CompanionPackageSource = new Source(feed.FeedDirectory, Declare("Tool.Editor")) };
        var log = new LoggerResult();

        session.LoadCompanionPackages(feed.Root, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor.Avalonia");
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
    }

    [Fact]
    public void TheFilterIsAskedWithNoDeclaringPackage()
    {
        using var feed = new LocalPackageFeed();
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor);
        var filter = new Filter(load: false);
        var session = new PackageSession { CompanionPackageSource = new Source(feed.FeedDirectory, Declare("Tool.Editor")), CompanionPackageFilter = filter };

        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        var (declaring, name) = Assert.Single(filter.Asked);
        Assert.Null(declaring);
        Assert.Equal("Tool.Editor", name);
        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        Assert.Empty(session.LoadedSourceCompanionPackages);
        // Nothing was restored
        Assert.False(Directory.Exists(Path.Combine(feed.PackagesDirectory, "tool.editor")));
    }

    [Fact]
    public void ACompanionTheSessionAlreadyDeclaresIsNotAddedAgain()
    {
        var session = new PackageSession();
        var game = new SolutionProject(new Package { Meta = { Name = "Game", Version = Version } }, Guid.NewGuid(), "Game.csproj");
        session.Projects.Add(game);
        var plugin = new Package { Meta = { Name = "Plugin", Version = Version } };
        session.Projects.Add(new StandalonePackage(plugin) { IsDependencyPackage = true });
        game.FlattenedDependencies.Add(new Dependency(plugin));
        var companion = new Package { Meta = { Name = "Plugin.Editor", Version = Version }, Kind = PackageKind.Editor };
        session.Projects.Add(new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true });
        plugin.CompanionPackages.Add(new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor });
        var source = new Source(null, new CompanionPackage { Name = "Plugin.Editor", Version = new PackageVersion("2.0.0"), Kind = PackageKind.Editor });
        session.CompanionPackageSource = source;
        var log = new LoggerResult();

        session.LoadCompanionPackages(null, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Single(session.Projects, x => x.Package.Meta.Name == "Plugin.Editor");
        Assert.Empty(session.LoadedSourceCompanionPackages);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Info && x.Text.Contains("the session's one loads", StringComparison.Ordinal));
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
    }

    [Fact]
    public void ACompanionOfAnotherToolkitIsNotLoaded()
    {
        var source = new Source(null, new CompanionPackage { Name = "Tool.Editor.Wpf", Version = Version, Kind = PackageKind.Editor, Toolkit = EditorToolkits.Wpf });
        var filter = new Filter(load: true);
        var session = new PackageSession { CompanionPackageSource = source, CompanionPackageFilter = filter };
        var log = new LoggerResult();

        session.LoadCompanionPackages(null, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Empty(filter.Asked);
        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor.Wpf");
        Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
    }

    [Fact]
    public void APackageOfAnotherKindIsNotLoaded()
    {
        using var feed = new LocalPackageFeed();
        feed.AddPackage("Tool", "1.0.0", PackageKind.Runtime);
        var session = new PackageSession { CompanionPackageSource = new Source(feed.FeedDirectory, Declare("Tool")) };
        var log = new LoggerResult();

        session.LoadCompanionPackages(feed.Root, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool");
        Assert.Empty(session.LoadedSourceCompanionPackages);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("[Tool]", StringComparison.Ordinal));
    }

    [Fact]
    public void ARestoredPackageTheSourceDoesNotExpectIsNotLoaded()
    {
        using var feed = new LocalPackageFeed();
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor);
        var source = new Source(feed.FeedDirectory, Declare("Tool.Editor")) { Expected = false };
        var session = new PackageSession { CompanionPackageSource = source };

        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Equal(["Tool.Editor"], source.Checked);
        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        Assert.Empty(session.LoadedSourceCompanionPackages);
    }

    [Fact]
    public void ARestoredCompanionOfACompanionOfTheSourceIsCheckedToo()
    {
        using var feed = new LocalPackageFeed();
        var view = new CompanionPackage { Name = "Tool.Editor.Avalonia", Version = Version, Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia };
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, companions: [view]);
        feed.AddPackage("Tool.Editor.Avalonia", "1.0.0", PackageKind.Editor, EditorToolkits.Avalonia);
        var source = new Source(feed.FeedDirectory, Declare("Tool.Editor")) { Refused = ["Tool.Editor.Avalonia"] };
        var session = new PackageSession { CompanionPackageSource = source };

        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Equal(["Tool.Editor", "Tool.Editor.Avalonia"], source.Checked);
        Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor.Avalonia");
    }

    [Fact]
    public void AMissingPackageIsAnError()
    {
        using var feed = new LocalPackageFeed();
        var session = new PackageSession { CompanionPackageSource = new Source(feed.FeedDirectory, Declare("Not.There.Editor")) };
        var log = new LoggerResult();

        session.LoadCompanionPackages(feed.Root, log, loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Not.There.Editor");
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.Contains("[Not.There.Editor]", StringComparison.Ordinal));
    }

    [Fact]
    public void TheSourceIsReadOncePerSessionAndNotWithoutEditorPackages()
    {
        using var feed = new LocalPackageFeed();
        feed.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor);
        var source = new Source(feed.FeedDirectory, Declare("Tool.Editor"));
        var session = new PackageSession { CompanionPackageSource = source };

        // The asset compiler loads no editor package: the source is not read
        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: false, editorToolkit: null);
        Assert.Equal(0, source.Reads);
        Assert.Null(session.SourceCompanionPackages);

        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);
        session.LoadCompanionPackages(feed.Root, new LoggerResult(), loadEditorPackages: true, EditorToolkits.Avalonia);

        Assert.Equal(1, source.Reads);
        Assert.Single(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        Assert.Equal("Tool.Editor", Assert.Single(session.SourceCompanionPackages!).Name);
    }

    [Fact]
    public void TheSessionKeepsTheSourceOfItsFirstLoad()
    {
        var session = new PackageSession();
        var first = new Source(null);
        session.LoadMissingDependencies(new LoggerResult(), new PackageLoadParameters { CompanionPackageSource = first, LoadEditorPackages = true });
        session.LoadMissingDependencies(new LoggerResult(), new PackageLoadParameters { CompanionPackageSource = new Source(null), LoadEditorPackages = true });

        Assert.Same(first, session.CompanionPackageSource);
    }

    private static CompanionPackage Declare(string name) => new() { Name = name, Version = Version, Kind = PackageKind.Editor };

    private sealed class Source(string? feed, params CompanionPackage[] companions) : ICompanionPackageSource
    {
        public int Reads { get; private set; }

        public bool Expected { get; init; } = true;

        public HashSet<string> Refused { get; init; } = [];

        public List<string> Checked { get; } = [];

        public bool IsRestoredPackageExpected(string name, PackageVersion version, ILogger log)
        {
            Checked.Add(name);
            return Expected && !Refused.Contains(name);
        }

        public IReadOnlyList<string> PackageSources { get; } = feed is not null ? [feed] : [];

        public IReadOnlyList<CompanionPackage> GetCompanionPackages(ILogger log)
        {
            Reads++;
            return companions;
        }
    }

    private sealed class Filter(bool load) : ICompanionPackageFilter
    {
        public List<(Package? Declaring, string Name)> Asked { get; } = [];

        public bool ShouldLoad(Package? declaringPackage, CompanionPackage companion, ILogger log)
        {
            Asked.Add((declaringPackage, companion.Name!));
            return load;
        }
    }
}
