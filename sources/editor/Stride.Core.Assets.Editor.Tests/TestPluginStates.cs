// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Xml.Linq;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The Editor packages a session leaves out: those the user switched off for it (<see cref="DisabledEditorCompanionStore"/>), and
/// in safe mode every one that is not built-in (<see cref="BuiltInPlugins"/>); their <see cref="PluginState.Off"/> state in the host.
/// </summary>
public sealed class TestPluginStates : IDisposable
{
    private static readonly PackageVersion EditorPackageVersion = new($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor}.0");

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.PluginStates." + Guid.NewGuid().ToString("N"));
    private readonly SettingsContainer container = new();
    private readonly SettingsKey<List<DisabledEditorCompanions>> key;
    private readonly SettingsProfile profile;

    public TestPluginStates()
    {
        Directory.CreateDirectory(directory);
        key = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, container, () => new List<DisabledEditorCompanions>());
        profile = container.CreateSettingsProfile(true);
    }

    public void Dispose()
    {
        Directory.Delete(directory, true);
    }

    [Fact]
    public void ASwitchedOffPackageIsLeftOutAndTheOthersLoad()
    {
        var skipped = new List<(string, PluginOffReason)>();
        var filter = new EditorCompanionFilter(["Plugin.Editor"], safeMode: false, (name, reason) => skipped.Add((name, reason)));
        var log = new LoggerResult();

        Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
        Assert.True(filter.ShouldLoad(Declaring("Other"), Companion("Other.Editor"), log));
        // Compared as package ids are, whatever the case
        Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("plugin.EDITOR"), log));

        Assert.Equal([("Plugin.Editor", PluginOffReason.SwitchedOff), ("plugin.EDITOR", PluginOffReason.SwitchedOff)], skipped);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Info && x.Text.Contains("[Plugin.Editor], declared by [Plugin], is not loaded: it is switched off for this session.", StringComparison.Ordinal));
    }

    [Fact]
    public void ABuiltInPackageCantBeSwitchedOff()
    {
        var skipped = new List<string>();
        var filter = new EditorCompanionFilter(["Stride.Physics.Editor"], safeMode: false, (name, _) => skipped.Add(name));
        var log = new LoggerResult();

        Assert.True(filter.ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log));

        Assert.Empty(skipped);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("it is a plugin of the engine and can't be", StringComparison.Ordinal));
    }

    [Fact]
    public void SafeModeLoadsTheBuiltInPackagesOnly()
    {
        var skipped = new List<(string, PluginOffReason)>();
        var filter = new EditorCompanionFilter([], safeMode: true, (name, reason) => skipped.Add((name, reason)));
        var log = new LoggerResult();

        foreach (var name in BuiltInPlugins.PackageNames)
            Assert.True(filter.ShouldLoad(Declaring("Stride.Engine.Plugin"), Companion(name, EditorPackageVersion), log), name);
        Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
        Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor.Avalonia"), log));
        // An engine plugin of another Stride version is not this editor's
        var otherMinor = new PackageVersion($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor + 1}.0");
        Assert.False(filter.ShouldLoad(Declaring("Stride.UI"), Companion("Stride.UI.Editor", otherMinor), log));
        // A name that only looks like the engine's is not built-in
        Assert.False(filter.ShouldLoad(Declaring("Stride.Community"), Companion("Stride.Community.Editor", EditorPackageVersion), log));

        Assert.Equal(
            [("Plugin.Editor", PluginOffReason.SafeMode), ("Plugin.Editor.Avalonia", PluginOffReason.SafeMode), ("Stride.UI.Editor", PluginOffReason.SafeMode), ("Stride.Community.Editor", PluginOffReason.SafeMode)],
            skipped);
        Assert.Contains(log.Messages, x => x.Text.Contains("is not loaded: Game Studio runs in safe mode.", StringComparison.Ordinal));
    }

    [Fact]
    public void ACompanionWithoutVersionHasTheVersionOfItsDeclaringPackage()
    {
        var filter = new EditorCompanionFilter([], safeMode: true);
        var declaring = Declaring("Stride.UI");
        declaring.Meta.Version = EditorPackageVersion;

        Assert.True(filter.ShouldLoad(declaring, new CompanionPackage { Name = "Stride.UI.Editor", Kind = PackageKind.Editor }, new LoggerResult()));
    }

    [Fact]
    public void TheBuiltInListHasTheEditorPackagesOfTheEnginePlugins()
    {
        // The Editor projects of sources/plugins, as their project files declare them
        var root = FindRepositoryRoot();
        var editorProjects = Directory.EnumerateFiles(Path.Combine(root, "sources", "plugins"), "*.csproj", SearchOption.AllDirectories)
            .Where(path => XDocument.Load(path).Descendants("StridePackageKind").Any(x => x.Value == "Editor"))
            .Select(Path.GetFileNameWithoutExtension)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.NotEmpty(editorProjects);
        Assert.Equal(editorProjects, BuiltInPlugins.PackageNames.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(typeof(BuiltInPlugins).Assembly.GetName().Version!.Major, BuiltInPlugins.EditorVersion.Major);
        Assert.False(BuiltInPlugins.IsBuiltIn("Stride.UI.Editor", null));
        Assert.False(BuiltInPlugins.IsBuiltIn(null, EditorPackageVersion));
    }

    [Fact]
    public void TheHostRecordsTheLeftOutPackagesAsOff()
    {
        var host = new RecordingEditorPluginHost([]) { DisabledCompanions = CreateStore() };
        var changes = new List<(AssetsPlugin? Plugin, string Name, PluginState? State)>();
        host.PluginStatusChanged += (_, e) => changes.Add((e.Plugin, e.Name, e.Status?.State));
        var game = new UFile("/Projects/Game/Game.sln");
        host.DisabledCompanions.Set(game, "Plugin.Editor", switchedOff: true);
        try
        {
            // The editors give it to the sessions they open
            var filter = ((IAssetsPluginService)host).GetCompanionPackageFilter(game);
            Assert.NotNull(filter);
            var log = new LoggerResult();
            Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
            // Asked again (a dependency added later): one status, one change
            Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
            Assert.True(filter.ShouldLoad(Declaring("Other"), Companion("Other.Editor"), log));

            var status = Assert.Single(host.PluginStatuses);
            Assert.Null(status.Plugin);
            Assert.Equal("Plugin.Editor", status.Name);
            Assert.Equal(PluginState.Off, status.State);
            Assert.Equal(PluginOffReason.SwitchedOff, status.OffReason);
            Assert.Empty(status.Errors);
            Assert.Equal([((AssetsPlugin?)null, "Plugin.Editor", (PluginState?)PluginState.Off)], changes);

            // Another session has nothing switched off
            Assert.True(host.GetCompanionPackageFilter(new UFile("/Projects/Other/Other.sln")).ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
            // Nor a new project, which has no file yet
            Assert.True(host.GetCompanionPackageFilter(null).ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
        }
        finally
        {
            host.Dispose();
        }

        // The host forgets them with the session
        Assert.Empty(host.PluginStatuses);
        Assert.Equal(((AssetsPlugin?)null, "Plugin.Editor", (PluginState?)null), changes[^1]);
    }

    [Fact]
    public void InSafeModeTheHostRecordsEveryThirdPartyPackageAsOffInSafeMode()
    {
        var host = new RecordingEditorPluginHost([]) { SafeMode = true };
        try
        {
            var filter = host.GetCompanionPackageFilter(new UFile("/Projects/Game/Game.sln"));
            var log = new LoggerResult();
            Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));
            Assert.True(filter.ShouldLoad(Declaring("Stride.Particles"), Companion("Stride.Particles.Editor", EditorPackageVersion), log));

            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal(("Plugin.Editor", PluginState.Off, (PluginOffReason?)PluginOffReason.SafeMode), (status.Name, status.State, status.OffReason));
            Assert.Equal("Plugin.Editor: Off, SafeMode", status.ToString());
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void WithoutStoreNothingIsSwitchedOff()
    {
        var host = new RecordingEditorPluginHost([]);
        try
        {
            Assert.Null(host.DisabledCompanions);
            Assert.False(host.SafeMode);
            Assert.True(host.GetCompanionPackageFilter(new UFile("/Projects/Game/Game.sln")).ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), new LoggerResult()));
            Assert.Empty(host.PluginStatuses);
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void TheStoreSwitchesAPackageOffAndOnPerSession()
    {
        var store = CreateStore();
        var game = new UFile("/Projects/Game/Game.sln");
        var other = new UFile("/Projects/Other/Other.sln");

        Assert.True(store.Set(game, "Plugin.Editor", switchedOff: true));
        Assert.False(store.Set(game, "plugin.editor", switchedOff: true));
        Assert.True(store.Set(game, "Second.Editor", switchedOff: true));
        Assert.True(store.Set(other, "Plugin.Editor", switchedOff: true));

        Assert.Equal(["Plugin.Editor", "Second.Editor"], store.Get(game));
        // The path is compared as the editors do, whatever its case
        Assert.Equal(["Plugin.Editor", "Second.Editor"], store.Get(new UFile("/projects/game/GAME.sln")));
        Assert.Equal(["Plugin.Editor"], store.Get(other));
        Assert.Empty(store.Get(new UFile("/Projects/None/None.sln")));

        // Switched on again; a session with nothing left has no entry
        Assert.True(store.Set(game, "PLUGIN.Editor", switchedOff: false));
        Assert.False(store.Set(game, "Plugin.Editor", switchedOff: false));
        Assert.Equal(["Second.Editor"], store.Get(game));
        Assert.True(store.Set(other, "Plugin.Editor", switchedOff: false));
        Assert.Equal([game], key.GetValue(profile, true).Select(x => x.FilePath));
    }

    [Fact]
    public void TheEntriesAreWrittenUnderTheirOwnKeyAndReadBack()
    {
        var path = Path.Combine(directory, "GameStudioInternal.conf");
        var game = new UFile("C:/Projects/My Game/My Game.sln");
        var store = new DisabledEditorCompanionStore(() => profile, key, () => Assert.True(container.SaveSettingsProfile(profile, path)));

        store.Set(game, "Plugin.Editor", switchedOff: true);

        var text = File.ReadAllText(path);
        Assert.Contains("Internal/DisabledEditorCompanions:", text);
        Assert.Contains("Plugin.Editor", text);

        // A new editor reads the file
        var reader = new SettingsContainer();
        var readerKey = new SettingsKey<List<DisabledEditorCompanions>>(key.Name, reader, () => new List<DisabledEditorCompanions>());
        var loaded = reader.LoadSettingsProfile(path, false, null, false);
        Assert.NotNull(loaded);
        var entry = Assert.Single(readerKey.GetValue(loaded, true));
        Assert.Equal(game, entry.FilePath);
        Assert.Equal(["Plugin.Editor"], entry.Packages);
    }

    private DisabledEditorCompanionStore CreateStore()
    {
        // The profile stands for the file: what was saved is what the next store loads
        return new DisabledEditorCompanionStore(() => profile, key, () => { });
    }

    private static Package Declaring(string name) => new() { Meta = { Name = name, Version = new PackageVersion("1.0.0") } };

    private static CompanionPackage Companion(string name, PackageVersion? version = null)
    {
        return new CompanionPackage { Name = name, Version = version ?? new PackageVersion("1.0.0"), Kind = PackageKind.Editor };
    }

    private static string FindRepositoryRoot()
    {
        for (var current = new DirectoryInfo(AppContext.BaseDirectory); current is not null; current = current.Parent)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "sources", "plugins")))
                return current.FullName;
        }
        throw new DirectoryNotFoundException("The repository's root (sources/plugins) is not above the tests' directory.");
    }
}
