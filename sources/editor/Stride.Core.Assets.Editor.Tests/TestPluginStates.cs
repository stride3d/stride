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
/// The Editor packages a session leaves out: those the user switched off for it (<see cref="DisabledEditorCompanionStore"/>), the
/// built-in ones that can be switched off included, and in safe mode every one that is not built-in (<see cref="BuiltInPlugins"/>);
/// their <see cref="PluginState.Off"/> state in the host.
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
    public void ASwitchedOffBuiltInPackageIsLeftOutWithItsToolkitCompanions()
    {
        var skipped = new List<(string, PluginOffReason)>();
        var filter = new EditorCompanionFilter(["Stride.UI.Editor"], safeMode: false, (name, reason) => skipped.Add((name, reason)));
        var log = new LoggerResult();

        Assert.False(filter.ShouldLoad(Declaring("Stride.UI"), Companion("Stride.UI.Editor", EditorPackageVersion), log));
        // Declared by the Editor package, so not asked while it is off; left out too if they are
        Assert.False(filter.ShouldLoad(Declaring("Stride.UI.Editor"), Companion("Stride.UI.Editor.Avalonia", EditorPackageVersion), log));
        Assert.False(filter.ShouldLoad(Declaring("Stride.UI.Editor"), Companion("Stride.UI.Editor.Wpf", EditorPackageVersion), log));
        // The other built-in packages load
        Assert.True(filter.ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log));
        Assert.True(filter.ShouldLoad(Declaring("Stride.Audio.Editor"), Companion("Stride.Audio.Editor.Avalonia", EditorPackageVersion), log));

        Assert.Equal([("Stride.UI.Editor", PluginOffReason.SwitchedOff), ("Stride.UI.Editor.Avalonia", PluginOffReason.SwitchedOff), ("Stride.UI.Editor.Wpf", PluginOffReason.SwitchedOff)], skipped);
        Assert.True(filter.IsBuiltInSwitchedOff("Stride.UI.Editor.Avalonia"));
        Assert.False(filter.IsBuiltInSwitchedOff("Stride.Physics.Editor"));
        // No warning: the user may switch it off
        Assert.DoesNotContain(log.Messages, x => x.Type == LogMessageType.Warning);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Info && x.Text.Contains("[Stride.UI.Editor], declared by [Stride.UI], is not loaded: it is switched off for this session.", StringComparison.Ordinal));
    }

    [Fact]
    public void EachSwitchableBuiltInPackageCanBeSwitchedOff()
    {
        var filter = new EditorCompanionFilter(BuiltInPlugins.SwitchablePackageNames, safeMode: false);
        var log = new LoggerResult();

        foreach (var name in BuiltInPlugins.PackageNames)
            Assert.False(filter.ShouldLoad(Declaring("Stride.Engine.Plugin"), Companion(name, EditorPackageVersion), log), name);
        // Of another Stride version, a package of the list is third-party: switched off as any other
        var otherMinor = new PackageVersion($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor + 1}.0");
        Assert.False(filter.ShouldLoad(Declaring("Stride.UI"), Companion("Stride.UI.Editor", otherMinor), log));
        Assert.DoesNotContain(log.Messages, x => x.Type == LogMessageType.Warning);
    }

    [Fact]
    public void AToolkitCompanionOfABuiltInPackageIsNotSwitchedOffOnItsOwn()
    {
        var skipped = new List<string>();
        var filter = new EditorCompanionFilter(["Stride.UI.Editor.Avalonia"], safeMode: false, (name, _) => skipped.Add(name));
        var log = new LoggerResult();

        Assert.True(filter.ShouldLoad(Declaring("Stride.UI"), Companion("Stride.UI.Editor", EditorPackageVersion), log));
        Assert.True(filter.ShouldLoad(Declaring("Stride.UI.Editor"), Companion("Stride.UI.Editor.Avalonia", EditorPackageVersion), log));

        Assert.Empty(skipped);
        Assert.Contains(log.Messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("[Stride.UI.Editor.Avalonia] is switched off for this session, but it is switched with [Stride.UI.Editor], not on its own: it is loaded.", StringComparison.Ordinal));
    }

    [Fact]
    public void SafeModeLoadsEveryBuiltInPackageWhateverTheUserSwitchedOff()
    {
        var skipped = new List<(string, PluginOffReason)>();
        var filter = new EditorCompanionFilter(BuiltInPlugins.SwitchablePackageNames.Append("Plugin.Editor"), safeMode: true, (name, reason) => skipped.Add((name, reason)));
        var log = new LoggerResult();

        foreach (var name in BuiltInPlugins.PackageNames)
            Assert.True(filter.ShouldLoad(Declaring("Stride.Engine.Plugin"), Companion(name, EditorPackageVersion), log), name);
        Assert.False(filter.ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), log));

        // Only the third-party package is off, and for safe mode
        Assert.Equal([("Plugin.Editor", PluginOffReason.SafeMode)], skipped);
        Assert.Contains(log.Messages, x => x.Text.Contains("[Stride.Physics.Editor], declared by [Stride.Engine.Plugin], is switched off for this session, but it is loaded: Game Studio runs in safe mode.", StringComparison.Ordinal));
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
        Assert.False(BuiltInPlugins.IsSwitchable("Stride.UI.Editor", null));
        Assert.Equal(typeof(BuiltInPlugins).Assembly.GetName().Version!.Major, BuiltInPlugins.EditorVersion.Major);
        Assert.False(BuiltInPlugins.IsBuiltIn("Stride.UI.Editor", null));
        Assert.False(BuiltInPlugins.IsBuiltIn(null, EditorPackageVersion));
    }

    [Fact]
    public void TheSwitchableListHasTheToolkitNeutralEditorPackagesOfTheEnginePlugins()
    {
        // The toolkit-neutral Editor projects of sources/plugins: the toolkit ones are their companions, switched with them
        var root = FindRepositoryRoot();
        var neutralProjects = Directory.EnumerateFiles(Path.Combine(root, "sources", "plugins"), "*.csproj", SearchOption.AllDirectories)
            .Select(path => (Path: path, Document: XDocument.Load(path)))
            .Where(x => x.Document.Descendants("StridePackageKind").Any(e => e.Value == "Editor") && !x.Document.Descendants("StrideEditorToolkit").Any())
            .Select(x => Path.GetFileNameWithoutExtension(x.Path))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Assert.Equal(7, neutralProjects.Count);
        Assert.Equal(neutralProjects, BuiltInPlugins.SwitchablePackageNames.Order(StringComparer.OrdinalIgnoreCase));
        Assert.True(BuiltInPlugins.IsSwitchable("Stride.Voxels.Editor", EditorPackageVersion));
        Assert.False(BuiltInPlugins.IsSwitchable("Stride.UI.Editor.Avalonia", EditorPackageVersion));
        // Each built-in package is switched with one of the list
        foreach (var name in BuiltInPlugins.PackageNames)
            Assert.Contains(BuiltInPlugins.GetSwitchedWith(name), BuiltInPlugins.SwitchablePackageNames);
        Assert.Equal("Stride.SpriteStudio.Editor", BuiltInPlugins.GetSwitchedWith("Stride.SpriteStudio.Editor.Wpf"));
        Assert.Equal("Stride.Audio.Editor", BuiltInPlugins.GetSwitchedWith("stride.audio.editor.avalonia"));
        Assert.Null(BuiltInPlugins.GetSwitchedWith("Stride.UI.Editor.Community"));
        Assert.Null(BuiltInPlugins.GetSwitchedWith(null));
    }

    [Fact]
    public void ABuiltInPackageIsSwitchedOffPerProject()
    {
        var host = new RecordingEditorPluginHost([]) { DisabledCompanions = CreateStore() };
        var game = new UFile("/Projects/Game/Game.sln");
        host.DisabledCompanions.Set(game, "Stride.Physics.Editor", switchedOff: true);
        try
        {
            var log = new LoggerResult();
            Assert.False(host.GetCompanionPackageFilter(game).ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log));
            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal(("Stride.Physics.Editor", PluginState.Off, (PluginOffReason?)PluginOffReason.SwitchedOff), (status.Name, status.State, status.OffReason));

            // Another project of the same user loads it
            Assert.True(host.GetCompanionPackageFilter(new UFile("/Projects/Other/Other.sln")).ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log));
            // And so does the same project in safe mode
            host.SafeMode = true;
            Assert.True(host.GetCompanionPackageFilter(game).ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log));
        }
        finally
        {
            host.Dispose();
        }
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
    public void InSafeModeAPluginClassOutsideTheEditorAndTheBuiltInPackagesDoesNotJoin()
    {
        // The test assembly stands for an assembly of the session (the game's, or a package it references): not an Editor
        // companion, so the filter never saw it
        var plugin = new TestEditorPluginHost.NoScanPlugin();
        var host = new RecordingEditorPluginHost([plugin]) { SafeMode = true, IsEditorAssembly = _ => false };
        var log = new LoggerResult();
        try
        {
            host.GetCompanionPackageFilter(new UFile("/Projects/Game/Game.sln"));
            host.RegisterSession(new FakeSession(), log);

            Assert.DoesNotContain(plugin, host.SessionPlugins);
            var name = typeof(TestEditorPluginHost.NoScanPlugin).Assembly.GetName().Name;
            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal((name, PluginState.Off, (PluginOffReason?)PluginOffReason.SafeMode), (status.Name, status.State, status.OffReason));
            Assert.Single(log.Messages, x => x.Text.Contains("does not join the session: Game Studio runs in safe mode", StringComparison.Ordinal));
        }
        finally
        {
            host.Dispose();
        }
    }

    [Fact]
    public void APluginClassOfASwitchedOffPackageDoesNotJoinHoweverItWasLoaded()
    {
        var plugin = new TestEditorPluginHost.NoScanPlugin();
        var other = new TestEditorPluginHost.NoScanPlugin();
        var name = typeof(TestEditorPluginHost.NoScanPlugin).Assembly.GetName().Name!;
        var host = new RecordingEditorPluginHost([plugin]) { DisabledCompanions = CreateStore(), IsEditorAssembly = _ => false };
        var game = new UFile("/Projects/Game/Game.sln");
        host.DisabledCompanions.Set(game, name, switchedOff: true);
        try
        {
            host.GetCompanionPackageFilter(game);
            host.RegisterSession(new FakeSession(), new LoggerResult());

            Assert.DoesNotContain(plugin, host.SessionPlugins);
            var status = Assert.Single(host.PluginStatuses);
            Assert.Equal((name, PluginState.Off, (PluginOffReason?)PluginOffReason.SwitchedOff), (status.Name, status.State, status.OffReason));
        }
        finally
        {
            host.Dispose();
        }

        // Without anything switched off, and out of safe mode, it joins; and so does a plugin of the editor itself in safe mode
        var normal = new RecordingEditorPluginHost([other]) { IsEditorAssembly = _ => false };
        var safe = new RecordingEditorPluginHost([plugin]) { SafeMode = true };
        try
        {
            normal.RegisterSession(new FakeSession(), new LoggerResult());
            safe.RegisterSession(new FakeSession(), new LoggerResult());
            Assert.Contains(other, normal.SessionPlugins);
            Assert.Contains(plugin, safe.SessionPlugins);
            Assert.Empty(safe.PluginStatuses.Where(x => x.State == PluginState.Off));
        }
        finally
        {
            normal.Dispose();
            safe.Dispose();
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
