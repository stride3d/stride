// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The user's editor extensions (<see cref="EditorExtensionService"/>): installed from a <c>.nupkg</c> or a folder into a local
/// feed of the user, listed per editor major.minor version, and loaded by the session loader with every session, through the
/// same filter (safe mode, switched off) and reference check as a project's plugins. Every test writes its packages, feed,
/// NuGet folders and settings under a temporary folder of its own, and deletes it.
/// </summary>
public sealed class TestEditorExtensions : IDisposable
{
    private static readonly Version Editor = BuiltInPlugins.EditorVersion;

    private readonly LocalPackageFeed nuget = new();
    private readonly string downloads;
    private readonly SettingsContainer container = new();
    private readonly SettingsKey<List<EditorExtensionList>> key;
    private readonly SettingsProfile profile;

    public TestEditorExtensions()
    {
        downloads = Path.Combine(nuget.Root, "downloads");
        Directory.CreateDirectory(downloads);
        key = new SettingsKey<List<EditorExtensionList>>(InternalSettings.ExtensionPackages.Name, container, () => new List<EditorExtensionList>());
        profile = container.CreateSettingsProfile(true);
    }

    public void Dispose() => nuget.Dispose();

    [Fact]
    public void AListedExtensionLoadsWithTheSession()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor", title: "A tool");
        var installed = host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        Assert.Empty(installed.Problems);
        Assert.Equal("Tool.Editor", Assert.Single(installed.Installed).Id);
        Assert.True(File.Exists(Path.Combine(host.Extensions.FeedDirectory, "Tool.Editor.1.0.0.nupkg")));
        try
        {
            using var session = OpenSession(host, out var log);

            Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            Assert.Equal(["Tool.Editor"], session.LoadedSourceCompanionPackages);
            Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
            var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor");
            Assert.Equal(PluginSource.Extension, info.Source);
            Assert.True(info.IsExtension);
            Assert.False(info.CanSwitchOff);
            Assert.Equal(PluginState.Joined, info.State);
            Assert.Equal("A tool", info.DisplayName);
            Assert.Equal(new PackageVersion("1.0.0"), info.Version);
            Assert.Equal(new Version(Editor.Major, Editor.Minor), info.EditorApiVersion);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionNotListedDoesNotLoad()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor");
        // In the feed (installed, then removed from the list by another editor), but not listed
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        CreateStore().Remove(Editor, "Tool.Editor");
        try
        {
            using var session = OpenSession(host, out var log);

            Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            Assert.Empty(session.SourceCompanionPackages!);
            Assert.DoesNotContain(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor");
            Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionListedForAnotherEditorVersionDoesNotLoad()
    {
        // Installed by the editors of the next minor version, in their own feed
        var other = new EditorExtensionService(CreateStore(), FeedRoot, new Version(Editor.Major, Editor.Minor + 1));
        AddExtensionPackage("Tool.Editor");
        Assert.Single(other.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia).Installed);
        var host = CreateHost();
        try
        {
            Assert.Empty(host.Extensions!.GetExtensions());
            Assert.NotEqual(other.FeedDirectory, host.Extensions.FeedDirectory);

            using var session = OpenSession(host, out _);

            Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            Assert.Single(other.GetExtensions());
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionIsOffInSafeMode()
    {
        var host = CreateHost();
        host.SafeMode = true;
        AddExtensionPackage("Tool.Editor");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        try
        {
            using var session = OpenSession(host, out var log);

            Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal("Tool.Editor", status.Name);
            Assert.Equal(PluginState.Off, status.State);
            Assert.Equal(PluginOffReason.SafeMode, status.OffReason);
            Assert.Contains(log.Messages, x => x.Text == "The extension [Tool.Editor] is not loaded: Game Studio runs in safe mode.");
            var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor");
            Assert.Equal((PluginState.Off, PluginOffReason.SafeMode, PluginSource.Extension), (info.State, info.OffReason, info.Source));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ASwitchedOffExtensionDoesNotLoadAnywhere()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        Assert.True(host.Extensions.SetOff("Tool.Editor", true));
        Assert.False(host.Extensions.SetOff("Tool.Editor", true));
        try
        {
            // Whatever the session
            foreach (var solution in new[] { "Game.sln", "Other.sln" })
            {
                using var session = OpenSession(host, out var log, solution);

                Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
                Assert.Contains(log.Messages, x => x.Text == "The extension [Tool.Editor] is not loaded: it is switched off.");
                var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor");
                Assert.Equal((PluginState.Off, PluginOffReason.SwitchedOff), (info.State, info.OffReason));
            }

            // On again: the next session loads it
            Assert.True(host.Extensions.SetOff("Tool.Editor", false));
            using var next = OpenSession(host, out _);
            Assert.Contains(next.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionBuiltForANewerEditorIsRefusedByTheReferenceCheck()
    {
        var host = CreateHost();
        var assembly = WriteAssembly("Newer.Editor", ("Stride.Editor", new Version(Editor.Major, Editor.Minor + 1, 0, 0)));
        nuget.AddPackage("Newer.Editor", "1.0.0", PackageKind.Editor, assemblies: new Dictionary<string, byte[]> { ["Newer.Editor.dll"] = assembly }, directory: downloads);
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Newer.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        try
        {
            using var session = OpenSession(host, out var log);

            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains($"needs Game Studio {Editor.Major}.{Editor.Minor + 1}", StringComparison.Ordinal));
            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal((PluginState.Incompatible, "Newer.Editor"), (status.State, status.Name));
            var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Newer.Editor");
            Assert.Equal((PluginSource.Extension, PluginState.Incompatible), (info.Source, info.State));
            Assert.IsType<IncompatiblePluginException>(Assert.Single(info.Errors).Exception);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ARemovedExtensionLeavesTheFeedAndTheNextSession()
    {
        var host = CreateHost();
        nuget.AddPackage("Tool.Library", "1.0.0", PackageKind.Runtime, directory: downloads);
        AddExtensionPackage("Tool.Editor", dependencies: [("Tool.Library", "1.0.0")]);
        AddExtensionPackage("Other.Editor");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        host.Extensions.InstallFromFile(Path.Combine(downloads, "Other.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        var feed = host.Extensions.FeedDirectory;
        Assert.Equal(["Other.Editor.1.0.0.nupkg", "Tool.Editor.1.0.0.nupkg", "Tool.Library.1.0.0.nupkg"], Directory.GetFiles(feed).Select(Path.GetFileName).Order());
        try
        {
            using (var session = OpenSession(host, out _))
            {
                // Removed while a session is open: it keeps it, and the panel says it goes at the restart
                Assert.True(host.Extensions.Remove("Tool.Editor"));
                Assert.False(host.Extensions.Remove("Tool.Editor"));
                Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
                Assert.Null(host.Extensions.Find("Tool.Editor"));
            }

            // Its package and its dependency left the feed; the other extension's stayed
            Assert.Equal(["Other.Editor.1.0.0.nupkg"], Directory.GetFiles(feed).Select(Path.GetFileName));
            using var next = OpenSession(host, out var log);
            Assert.DoesNotContain(next.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            Assert.Contains(next.Projects, x => x.Package.Meta.Name == "Other.Editor");
            Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionInstalledAfterTheSessionOpenedWaitsForTheNextOne()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor", title: "A tool");
        try
        {
            using var session = OpenSession(host, out _);
            host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);

            var info = Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor");
            Assert.Equal((PluginState.Off, PluginOffReason.AddedSinceStart, PluginSource.Extension), (info.State, info.OffReason, info.Source));
            // Not restored yet: its title is read from the package in the feed
            Assert.Equal("A tool", info.DisplayName);
            Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AnExtensionThatTheProjectAlsoHasIsTheProjects()
    {
        var host = CreateHost();
        AddExtensionPackage("Plugin.Editor");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Plugin.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        try
        {
            using var session = new PackageSession { SolutionPath = Path.Combine(nuget.Root, "Game.sln") };
            var plugin = new Package { Meta = { Name = "Plugin", Version = new PackageVersion("1.0.0") } };
            session.Projects.Add(new StandalonePackage(plugin) { IsDependencyPackage = true });
            plugin.CompanionPackages.Add(new CompanionPackage { Name = "Plugin.Editor", Version = new PackageVersion("1.0.0"), Kind = PackageKind.Editor });
            var companion = new Package { Meta = { Name = "Plugin.Editor", Version = new PackageVersion("1.0.0") }, Kind = PackageKind.Editor };
            session.Projects.Add(new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true });
            var log = new LoggerResult();

            session.LoadMissingDependencies(log, CreateLoadParameters(host));

            Assert.Single(session.Projects, x => x.Package.Meta.Name == "Plugin.Editor");
            Assert.Empty(session.LoadedSourceCompanionPackages);
            var infos = PluginCatalog.Collect(host, session).Where(x => x.Id == "Plugin.Editor").ToList();
            Assert.Equal([(PluginSource.Package, PluginState.Joined, (PluginOffReason?)null), (PluginSource.Extension, PluginState.Off, PluginOffReason.InProject)],
                infos.Select(x => (x.Source, x.State, x.OffReason)));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void AFolderInstallsItsEditorPackagesWithTheirCompanionsAndDependencies()
    {
        var service = CreateService();
        var view = new CompanionPackage { Name = "Tool.Editor.Avalonia", Version = new PackageVersion("1.0.0"), Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia };
        var wpfView = new CompanionPackage { Name = "Tool.Editor.Wpf", Version = new PackageVersion("1.0.0"), Kind = PackageKind.Editor, Toolkit = EditorToolkits.Wpf };
        nuget.AddPackage("Tool", "1.0.0", PackageKind.Runtime, directory: downloads);
        AddExtensionPackage("Tool.Editor", companions: [view, wpfView], dependencies: [("Tool", "1.0.0")]);
        nuget.AddPackage("Tool.Editor.Avalonia", "1.0.0", PackageKind.Editor, EditorToolkits.Avalonia, directory: downloads);
        nuget.AddPackage("Tool.Editor.Wpf", "1.0.0", PackageKind.Editor, EditorToolkits.Wpf, directory: downloads);
        // An older build next to it: the highest version is installed
        nuget.AddPackage("Tool.Editor", "0.9.0", PackageKind.Editor, directory: downloads);

        var result = service.InstallFromFolder(downloads, EditorToolkits.Avalonia);

        Assert.Empty(result.Problems);
        var installed = Assert.Single(result.Installed);
        Assert.Equal(("Tool.Editor", "1.0.0", (string?)null, false), (installed.Id, installed.Version, installed.Toolkit, installed.IsOff));
        Assert.Equal(["Tool.1.0.0.nupkg", "Tool.Editor.1.0.0.nupkg", "Tool.Editor.Avalonia.1.0.0.nupkg", "Tool.Editor.Wpf.1.0.0.nupkg"],
            Directory.GetFiles(service.FeedDirectory).Select(Path.GetFileName).Order());
        Assert.Equal(["Tool.Editor"], service.GetExtensions().Select(x => x.Id));
    }

    [Fact]
    public void ACompanionOfAnExtensionLoadsFromTheFeed()
    {
        var host = CreateHost();
        var view = new CompanionPackage { Name = "Tool.Editor.Avalonia", Version = new PackageVersion("1.0.0"), Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia };
        AddExtensionPackage("Tool.Editor", companions: [view]);
        nuget.AddPackage("Tool.Editor.Avalonia", "1.0.0", PackageKind.Editor, EditorToolkits.Avalonia, directory: downloads);
        host.Extensions!.InstallFromFolder(downloads, EditorToolkits.Avalonia);
        try
        {
            using var session = OpenSession(host, out var log);

            Assert.Contains(session.Projects, x => x.Package.Meta.Name == "Tool.Editor.Avalonia");
            Assert.False(log.HasErrors, string.Join(Environment.NewLine, log.Messages));
            // One entry for the extension and its views
            var infos = PluginCatalog.Collect(host, session);
            Assert.Single(infos, x => x.Id == "Tool.Editor");
            Assert.DoesNotContain(infos, x => x.Id == "Tool.Editor.Avalonia");
        }
        finally
        {
            host.Dispose();
        }
    }

    [Theory]
    [InlineData("Game", PackageKind.Runtime, null, "is a Runtime package")]
    [InlineData("Game.Assets", PackageKind.Assets, null, "is a Assets package")]
    [InlineData("Tool.Editor.Wpf", PackageKind.Editor, EditorToolkits.Wpf, "is written for the Wpf Game Studio")]
    [InlineData("Stride.Physics.Editor", PackageKind.Editor, null, "is a plugin of the engine")]
    public void APackageThatIsNotAnExtensionOfThisEditorIsRefused(string id, PackageKind kind, string? toolkit, string reason)
    {
        var service = CreateService();
        var path = nuget.AddPackage(id, "1.0.0", kind, toolkit, directory: downloads);

        var result = service.InstallFromFile(path, EditorToolkits.Avalonia);

        Assert.Empty(result.Installed);
        Assert.Contains(reason, Assert.Single(result.Problems), StringComparison.Ordinal);
        Assert.Empty(service.GetExtensions());
        Assert.False(Directory.Exists(service.FeedDirectory) && Directory.EnumerateFiles(service.FeedDirectory).Any());
    }

    [Fact]
    public void AFileThatIsNotAStridePackageIsRefused()
    {
        var service = CreateService();
        var notAPackage = Path.Combine(downloads, "readme.nupkg");
        File.WriteAllText(notAPackage, "not a zip");
        var folder = Path.Combine(downloads, "empty");
        Directory.CreateDirectory(folder);

        Assert.Single(service.InstallFromFile(notAPackage, EditorToolkits.Avalonia).Problems);
        Assert.Contains("holds no Editor package", Assert.Single(service.InstallFromFolder(folder, EditorToolkits.Avalonia).Problems), StringComparison.Ordinal);
        Assert.Empty(service.GetExtensions());
    }

    [Fact]
    public void AnotherBuildAtTheSameVersionReplacesTheRestoredOne()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor", title: "First build");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        try
        {
            using (OpenSession(host, out _))
                Assert.True(Directory.Exists(Path.Combine(nuget.PackagesDirectory, "tool.editor", "1.0.0")));
            File.Delete(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"));
            AddExtensionPackage("Tool.Editor", title: "Second build");

            var result = host.Extensions.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);

            Assert.Empty(result.Problems);
            using var session = OpenSession(host, out _);
            Assert.Equal("Second build", Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor").DisplayName);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void ARestoredPackageThatIsNotTheInstalledFileDoesNotLoad()
    {
        var host = CreateHost();
        AddExtensionPackage("Tool.Editor", title: "Installed build");
        host.Extensions!.InstallFromFile(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), EditorToolkits.Avalonia);
        try
        {
            // Another source served another package with the same id and version: it is in the global packages folder
            using (OpenSession(host, out _))
                Assert.True(Directory.Exists(Path.Combine(nuget.PackagesDirectory, "tool.editor", "1.0.0")));
            File.Delete(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"));
            AddExtensionPackage("Tool.Editor", title: "Installed again, outside Game Studio");
            File.Copy(Path.Combine(downloads, "Tool.Editor.1.0.0.nupkg"), Path.Combine(host.Extensions.FeedDirectory, "Tool.Editor.1.0.0.nupkg"), overwrite: true);

            using var session = OpenSession(host, out var log);

            Assert.DoesNotContain(session.Projects, x => x.Package.Meta.Name == "Tool.Editor");
            Assert.Empty(session.LoadedSourceCompanionPackages);
            Assert.Contains(log.Messages, x => x.Type == LogMessageType.Error && x.Text.StartsWith("The package [Tool.Editor] version [1.0.0] that NuGet restored", StringComparison.Ordinal));
            Assert.Equal(PluginState.Failed, Assert.Single(PluginCatalog.Collect(host, session), x => x.Id == "Tool.Editor").State);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheListIsKeptPerEditorVersionAndReadBack()
    {
        var store = CreateStore();
        var current = new EditorExtension { Id = "Tool.Editor", Version = "1.0.0" };
        var next = new Version(Editor.Major, Editor.Minor + 1);

        Assert.True(store.Set(Editor, current));
        Assert.False(store.Set(Editor, current));
        Assert.True(store.Set(next, new EditorExtension { Id = "Tool.Editor", Version = "2.0.0", Toolkit = EditorToolkits.Avalonia }));
        Assert.True(store.SetOff(Editor, "Tool.Editor", true));

        var lists = key.GetValue(profile, true);
        Assert.Equal([EditorExtensionStore.GetVersionKey(Editor), EditorExtensionStore.GetVersionKey(next)], lists.Select(x => x.EditorVersion));
        Assert.Equal(("Tool.Editor", "1.0.0", true), (store.Get(Editor)[0].Id, store.Get(Editor)[0].Version, store.Get(Editor)[0].IsOff));
        Assert.Equal(("2.0.0", EditorToolkits.Avalonia, false), (store.Get(next)[0].Version, store.Get(next)[0].Toolkit, store.Get(next)[0].IsOff));

        Assert.True(store.Remove(next, "Tool.Editor"));
        Assert.Single(key.GetValue(profile, true));
        Assert.Empty(store.Get(next));
    }

    private string FeedRoot => Path.Combine(nuget.Root, "extensions");

    private EditorExtensionStore CreateStore()
    {
        // The profile stands for the file: what was saved is what the next store loads
        return new EditorExtensionStore(() => profile, key, () => { });
    }

    private EditorExtensionService CreateService()
    {
        return new EditorExtensionService(CreateStore(), FeedRoot, Editor, (id, version) => Path.Combine(nuget.PackagesDirectory, id.ToLowerInvariant(), version.ToString()));
    }

    private RecordingEditorPluginHost CreateHost() => new([]) { Extensions = CreateService() };

    private void AddExtensionPackage(string id, string? title = null, IEnumerable<CompanionPackage>? companions = null, IEnumerable<(string, string)>? dependencies = null)
    {
        // Built against this editor, as a real extension is
        var assembly = WriteAssembly(id, ("Stride.Editor", new Version(Editor.Major, Editor.Minor, 0, 0)));
        nuget.AddPackage(id, "1.0.0", PackageKind.Editor, companions: companions, dependencies: dependencies, assemblies: new Dictionary<string, byte[]> { [id + ".dll"] = assembly }, directory: downloads, title: title);
    }

    private static PackageLoadParameters CreateLoadParameters(EditorPluginHost host) => new()
    {
        LoadEditorPackages = true,
        EditorToolkit = EditorToolkits.Avalonia,
        AssemblyLoadCheck = host.AssemblyLoadCheck,
        CompanionPackageFilter = host.GetCompanionPackageFilter(null),
        CompanionPackageSource = host.GetCompanionPackageSource(),
        LoadAssetsOfUnknownType = true,
    };

    // A session of the test's NuGet setup (its nuget.config is in the solution's folder), loaded as the editor loads one
    private PackageSession OpenSession(EditorPluginHost host, out LoggerResult log, string solution = "Game.sln")
    {
        var session = new PackageSession { SolutionPath = Path.Combine(nuget.Root, solution) };
        log = new LoggerResult();
        var parameters = CreateLoadParameters(host);
        session.LoadMissingDependencies(log, parameters);
        session.LoadMissingAssets(log, session.Packages.ToList(), parameters);
        return session;
    }

    /// <summary>
    /// An assembly with no code that references the given assemblies, as a plugin built against them would.
    /// </summary>
    private static byte[] WriteAssembly(string name, params (string Name, Version Version)[] references)
    {
        var metadata = new MetadataBuilder();
        metadata.AddModule(0, metadata.GetOrAddString(name + ".dll"), metadata.GetOrAddGuid(Guid.NewGuid()), default, default);
        metadata.AddAssembly(metadata.GetOrAddString(name), new Version(1, 0, 0, 0), default, default, default, AssemblyHashAlgorithm.Sha1);
        var runtime = typeof(object).Assembly.GetName();
        metadata.AddAssemblyReference(metadata.GetOrAddString(runtime.Name!), runtime.Version!, default, metadata.GetOrAddBlob(runtime.GetPublicKeyToken() ?? []), default, default);
        foreach (var (referenceName, version) in references)
            metadata.AddAssemblyReference(metadata.GetOrAddString(referenceName), version, default, default, default, default);
        metadata.AddTypeDefinition(default, default, metadata.GetOrAddString("<Module>"), default, MetadataTokens.FieldDefinitionHandle(1), MetadataTokens.MethodDefinitionHandle(1));

        var peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var image = new BlobBuilder();
        peBuilder.Serialize(image);
        return image.ToArray();
    }
}
