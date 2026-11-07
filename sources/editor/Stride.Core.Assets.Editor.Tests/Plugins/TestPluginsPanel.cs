// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Plugins;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Tests.Plugins;

/// <summary>
/// The Plugins panel's view model (<see cref="PluginsPanelViewModel"/>): the plugins of a session with their states, what needs
/// attention first, the switch with its confirmation, "Restart to apply", the errors; and the safe mode banner
/// (<see cref="SafeModeBannerViewModel"/>).
/// </summary>
public sealed class TestPluginsPanel : IDisposable
{
    private static readonly PackageVersion Version = new("1.2.3");
    private static readonly PackageVersion EditorPackageVersion = new($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor}.0");

    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride.PluginsPanel." + Guid.NewGuid().ToString("N"));
    private readonly UFile game;
    private readonly SettingsContainer container = new();
    private readonly SettingsKey<List<DisabledEditorCompanions>> key;
    private readonly SettingsProfile profile;
    private readonly List<IDisposable> disposables = [];

    public TestPluginsPanel()
    {
        Directory.CreateDirectory(directory);
        game = new UFile(Path.Combine(directory, "Game.sln"));
        key = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, container, () => new List<DisabledEditorCompanions>());
        profile = container.CreateSettingsProfile(true);
    }

    public void Dispose()
    {
        foreach (var disposable in Enumerable.Reverse(disposables))
            disposable.Dispose();
        Directory.Delete(directory, true);
    }

    [Fact]
    public async Task ThePanelListsThePluginsWithTheirStatesWhatNeedsAttentionFirst()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, plugin, session) = CreateHost();
        RefuseIncompatible(host);
        var panel = CreatePanel(fixture, host, session);

        Assert.Equal(["Incompatible.Editor", "Off.Editor", "Plugin.Editor", "Stride.Physics.Editor"], panel.Plugins.Select(x => x.Id));
        Assert.Equal([PluginGroup.NeedsAttention, PluginGroup.Off, PluginGroup.Project, PluginGroup.BuiltIn], panel.Plugins.Select(x => x.Group));
        Assert.All(panel.Plugins, x => Assert.True(x.IsFirstOfGroup));
        Assert.Equal(["Incompatible", "Off: switched off", "On", "On"], panel.Plugins.Select(x => x.StateText));
        Assert.Equal("4 plugins, 1 incompatible, 1 off", panel.Summary);
        Assert.Equal(1, panel.AttentionCount);
        Assert.Equal("Plugins (1)", panel.Title);

        // The incompatible one says which Game Studio it needs, and its error is the check's
        var incompatible = panel.Plugins[0];
        Assert.Contains("needs Game Studio", incompatible.StateDetail);
        var error = Assert.Single(incompatible.Errors);
        Assert.Equal(PluginContributions.Load, error.Contribution);
        Assert.Equal("Incompatible.Editor", error.PluginName);

        // The switched-off one says why, and stays off in the list
        var off = panel.Plugins[1];
        Assert.Equal(PluginOffReason.SwitchedOff, off.OffReason);
        Assert.Equal("It is switched off for this project: it was not loaded.", off.StateDetail);
        Assert.False(off.IsOn);
        Assert.False(off.IsPending);

        // The project's plugin: its package, what it registered, its switch
        var project = panel.Plugins[2];
        Assert.Same(plugin, Assert.Single(project.Info.Statuses).Plugin);
        Assert.Equal("1.2.3", project.VersionText);
        Assert.Equal("Package", project.SourceText);
        Assert.Equal(["Panels: 1", "ViewTypes: 1", "Widget: 1"], project.Contributions);
        Assert.True(project.CanSwitch);
        Assert.True(project.IsOn);
        Assert.Empty(project.Errors);
        Assert.Equal(string.Empty, project.StateDetail);

        // A built-in one has no switch
        var builtIn = panel.Plugins[3];
        Assert.True(builtIn.IsBuiltIn);
        Assert.False(builtIn.CanSwitch);
        Assert.False(builtIn.ToggleCommand.CanExecute(null));
        Assert.False(await builtIn.SetOnAsync(false));
        Assert.Empty(fixture.Dialogs.Messages);

        Assert.False(panel.NeedsRestart);
        Assert.False(panel.ShowsRestart);
    }

    [Fact]
    public async Task ThePanelFollowsAPluginThatFailsWhileItIsOpen()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, plugin, session) = CreateHost();
        var panel = CreatePanel(fixture, host, session);
        var item = panel.Plugins.Single(x => x.Id == "Plugin.Editor");
        panel.SelectedPlugin = item;
        var before = DateTimeOffset.Now;

        // A command of the plugin throws: the guard turns it off and records the error
        Assert.False(host.TryRun(plugin, PluginContributions.Command("Plugin.Explode"), () => throw new InvalidOperationException("Boom")));

        Assert.Same(item, panel.SelectedPlugin);
        Assert.Equal(PluginGroup.NeedsAttention, item.Group);
        Assert.Equal("On, with errors", item.StateText);
        Assert.Equal("Plugin.Editor", panel.Plugins[0].Id);
        Assert.Equal("Plugins (1)", panel.Title);
        Assert.Equal("3 plugins, 1 with errors, 1 off", panel.Summary);
        var error = Assert.Single(item.Errors);
        Assert.Equal("Command Plugin.Explode", error.Contribution);
        Assert.True(error.IsTurnedOff);
        Assert.Equal("Boom", error.Message);
        Assert.InRange(error.Time, before, DateTimeOffset.Now);
        Assert.Equal(typeof(TestPluginContributions.PanelPlugin).FullName, error.PluginName);

        // Its errors are copied with their contribution, time and details
        item.CopyErrorsCommand.Execute();
        var copied = fixture.Clipboard.Text;
        Assert.NotNull(copied);
        Assert.StartsWith("My plugin (Plugin.Editor 1.2.3): On, with errors", copied);
        Assert.Contains("Command Plugin.Explode (turned off for this session)", copied);
        Assert.Contains("System.InvalidOperationException: Boom", copied);
        Assert.Contains(error.Time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture), copied);
        item.CopyErrorCommand.Execute(error);
        Assert.Equal(error.ToText(), fixture.Clipboard.Text);
    }

    [Fact]
    public async Task SwitchingOffAsksWithWhatGoesThenWritesTheListAndShowsRestartToApply()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, _, session) = CreateHost();
        var restart = new RecordingRestartService();
        var panel = CreatePanel(fixture, host, session, restart);
        var item = panel.Plugins.Single(x => x.Id == "Plugin.Editor");
        var store = host.DisabledCompanions!;

        // Cancelled: nothing changes
        fixture.Dialogs.PickButton = buttons => buttons.Single(x => x.IsCancel).Result;
        Assert.False(await item.SetOnAsync(false));
        var question = Assert.Single(fixture.Dialogs.Messages);
        Assert.StartsWith("Switching off My plugin takes away, from the next opening of this project:", question);
        Assert.Contains("- its panels (1)", question);
        Assert.EndsWith("It applies when Game Studio opens this project again: use Restart to apply.", question);
        Assert.True(item.IsOn);
        Assert.Equal(["Off.Editor"], store.Get(game));
        Assert.False(panel.NeedsRestart);

        // Confirmed: the list has it, and the panel offers to restart
        fixture.Dialogs.PickButton = buttons => buttons.Single(x => x.IsDefault).Result;
        Assert.True(await item.SetOnAsync(false));
        Assert.Equal(["Off.Editor", "Plugin.Editor"], store.Get(game));
        Assert.False(item.IsOn);
        Assert.True(item.IsPending);
        Assert.True(panel.NeedsRestart);
        Assert.True(panel.ShowsRestart);
        Assert.Equal("Restart to apply", panel.RestartText);

        // Switched on again: nothing to apply
        Assert.True(await item.SetOnAsync(true));
        Assert.False(panel.NeedsRestart);
        Assert.Equal(2, fixture.Dialogs.Messages.Count);

        // A plugin that is off goes on without a question, from the next opening
        var off = panel.Plugins.Single(x => x.Id == "Off.Editor");
        Assert.True(await off.SetOnAsync(true));
        Assert.Equal(2, fixture.Dialogs.Messages.Count);
        Assert.Empty(store.Get(game));
        Assert.True(off.IsPending);
        Assert.True(panel.NeedsRestart);

        // "Restart to apply" restarts into the same session, normally
        Assert.True(panel.RestartCommand.CanExecute(null));
        Assert.True(await panel.RestartAsync());
        Assert.Equal([false], restart.Calls);
    }

    [Fact]
    public async Task AnEntryThatNamesABuiltInPluginCanBeCleared()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, _, session) = CreateHost();
        host.DisabledCompanions!.Set(game, "Stride.Physics.Editor", switchedOff: true);
        var panel = CreatePanel(fixture, host, session);
        var builtIn = panel.Plugins.Single(x => x.Id == "Stride.Physics.Editor");

        Assert.True(builtIn.IsListedBuiltIn);
        Assert.False(builtIn.CanSwitch);
        Assert.Equal(["Off.Editor", "Stride.Physics.Editor"], host.DisabledCompanions.Get(game));

        builtIn.ClearEntryCommand.Execute();

        Assert.False(builtIn.IsListedBuiltIn);
        Assert.Equal(["Off.Editor"], host.DisabledCompanions.Get(game));
        Assert.False(builtIn.ClearEntry());
        // Not a switch of a plugin: nothing to restart for
        Assert.False(panel.NeedsRestart);
    }

    [Fact]
    public async Task WithoutTheSessionsFileOrTheListNothingCanBeSwitched()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, _, session) = CreateHost();
        var withoutFile = CreatePanel(fixture, host, session, sessionFile: null);
        Assert.False(withoutFile.CanSwitch);
        Assert.All(withoutFile.Plugins, x => Assert.False(x.CanSwitch));

        host.DisabledCompanions = null;
        var withoutList = CreatePanel(fixture, host, session);
        Assert.False(withoutList.CanSwitch);
        Assert.False(await withoutList.Plugins.Single(x => x.Id == "Plugin.Editor").SetOnAsync(false));
        Assert.Empty(fixture.Dialogs.Messages);
        // Nor restarted, without a service
        Assert.False(withoutList.RestartCommand.CanExecute(null));
        Assert.False(await withoutList.RestartAsync());
    }

    [Fact]
    public async Task InSafeModeTheSwitchesApplyWhenTheEditorRestartsNormally()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var store = CreateStore();
        var plugin = new TestPluginContributions.PanelPlugin();
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(object), DisabledCompanions = store, SafeMode = true };
        host.DeclareRegistry<TestPluginContributions.Widget>();
        disposables.Add(host);
        var session = new PackageSession { EditorToolkit = EditorToolkits.Avalonia };
        disposables.Add(session);
        var declaration = new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor };
        var declaring = AddDeclaringPackage(session, "Plugin", declaration);
        Assert.False(host.GetCompanionPackageFilter(game).ShouldLoad(declaring, declaration, new LoggerResult()));
        var restart = new RecordingRestartService();
        var panel = CreatePanel(fixture, host, session, restart);
        var item = Assert.Single(panel.Plugins, x => x.Id == "Plugin.Editor");

        Assert.Equal("Off: safe mode", item.StateText);
        Assert.Equal("Game Studio runs in safe mode: only the plugins of Stride load.", item.StateDetail);
        Assert.True(item.CanSwitch);
        Assert.True(item.IsOn);
        Assert.False(item.IsPending);
        Assert.True(panel.SafeMode);
        Assert.True(panel.ShowsRestart);
        Assert.Equal("Restart normally", panel.RestartText);

        fixture.Dialogs.PickButton = buttons => buttons.Single(x => x.IsDefault).Result;
        Assert.True(await item.SetOnAsync(false));
        Assert.Equal(["Plugin.Editor"], store.Get(game));
        Assert.False(item.IsPending);
        Assert.False(panel.NeedsRestart);

        Assert.True(await panel.RestartAsync());
        Assert.Equal([false], restart.Calls);
    }

    [Fact]
    public async Task ThePanelEmptiesWhenItIsDestroyed()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync();
        var (host, plugin, session) = CreateHost();
        var collected = 0;
        var panel = CreatePanel(fixture, host, session, collect: () => { collected++; return PluginCatalog.Collect(host, session); });
        panel.SelectedPlugin = panel.Plugins[0];
        Assert.Equal(1, collected);

        panel.Destroy();

        Assert.Empty(panel.Plugins);
        Assert.Null(panel.SelectedPlugin);
        // It no longer follows the host
        host.TryRun(plugin, PluginContributions.Command("Plugin.Explode"), () => throw new InvalidOperationException("Boom"));
        Assert.Equal(1, collected);
        Assert.Empty(panel.Plugins);
    }

    [Fact]
    public void TheBannerShowsOnlyInSafeModeWithTheCountOfPluginsOff()
    {
        var fake = new FakeSession();
        var host = new RecordingEditorPluginHost([]);
        disposables.Add(host);
        var restart = new RecordingRestartService();
        var opened = 0;
        var sessionOpen = false;
        var banner = new SafeModeBannerViewModel(fake.ServiceProvider, host, restart, () => opened++, () => sessionOpen);
        disposables.Add(banner);

        Assert.False(banner.IsVisible);
        Assert.Equal(0, banner.OffCount);

        // Turned on later (the offer after a crash): the editor updates it
        host.SafeMode = true;
        banner.Update();
        Assert.True(banner.IsVisible);
        Assert.Equal("Safe mode: only the plugins of Stride are loaded.", banner.Message);
        Assert.False(banner.OpenPluginsPanelCommand.CanExecute(null));

        // The session loads: each plugin left out counts, a built-in one does not
        var filter = host.GetCompanionPackageFilter(game);
        var log = new LoggerResult();
        filter.ShouldLoad(Declaring("First"), Companion("First.Editor"), log);
        Assert.Equal(1, banner.OffCount);
        Assert.Equal("Safe mode: 1 plugin of this project is off. Only the plugins of Stride are loaded.", banner.Message);
        filter.ShouldLoad(Declaring("Second"), Companion("Second.Editor"), log);
        filter.ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", EditorPackageVersion), log);
        Assert.Equal(2, banner.OffCount);
        Assert.Equal("Safe mode: 2 plugins of this project are off. Only the plugins of Stride are loaded.", banner.Message);

        sessionOpen = true;
        banner.Update();
        Assert.True(banner.OpenPluginsPanelCommand.CanExecute(null));
        banner.OpenPluginsPanelCommand.Execute();
        Assert.Equal(1, opened);

        // "Restart normally" restarts without safe mode
        banner.RestartNormallyCommand.Execute();
        Assert.Equal([false], restart.Calls);

        // Out of safe mode, no banner
        host.SafeMode = false;
        banner.Update();
        Assert.False(banner.IsVisible);
        Assert.Equal(0, banner.OffCount);
    }

    /// <summary>
    /// A host whose session declares three Editor packages: one that joins (its plugin is in this test assembly), one
    /// switched off for <see cref="game"/>, and one never loaded (left to a test); and a built-in package listed by the panel.
    /// </summary>
    private (RecordingEditorPluginHost Host, TestPluginContributions.PanelPlugin Plugin, PackageSession Session) CreateHost()
    {
        var store = CreateStore();
        store.Set(game, "Off.Editor", switchedOff: true);
        var plugin = new TestPluginContributions.PanelPlugin();
        var host = new RecordingEditorPluginHost([plugin]) { PluginViewBaseType = typeof(object), DisabledCompanions = store };
        host.DeclareRegistry<TestPluginContributions.Widget>();
        disposables.Add(host);

        var session = new PackageSession { EditorToolkit = EditorToolkits.Avalonia };
        disposables.Add(session);
        var root = WriteRestoredPackage("Plugin.Editor", "My plugin");
        AddDeclaringPackage(session, "Plugin", new CompanionPackage { Name = "Plugin.Editor", Version = Version, Kind = PackageKind.Editor });
        var companion = new Package { Meta = { Name = "Plugin.Editor", Version = Version }, Kind = PackageKind.Editor, FullPath = Path.Combine(root, "stride", "Plugin.Editor.sdpkg") };
        var companionContainer = new StandalonePackage(companion) { IsDependencyPackage = true, IsCompanionPackage = true };
        companionContainer.Assemblies.Add(typeof(TestPluginsPanel).Assembly.Location);
        session.Projects.Add(companionContainer);

        // Left out by the filter the editor gives the session
        var offDeclaration = new CompanionPackage { Name = "Off.Editor", Version = Version, Kind = PackageKind.Editor };
        var offDeclaring = AddDeclaringPackage(session, "Off", offDeclaration);
        Assert.False(host.GetCompanionPackageFilter(game).ShouldLoad(offDeclaring, offDeclaration, new LoggerResult()));

        var fake = new FakeSession();
        fake.ServiceProvider.RegisterService(host);
        host.RegisterSession(fake, new LoggerResult());
        return (host, plugin, session);
    }

    private PluginsPanelViewModel CreatePanel(EditorSessionFixture fixture, EditorPluginHost host, PackageSession session, IEditorRestartService? restart = null, Func<IReadOnlyList<PluginInfo>>? collect = null)
    {
        return CreatePanel(fixture, host, session, game, restart, collect);
    }

    private PluginsPanelViewModel CreatePanel(EditorSessionFixture fixture, EditorPluginHost host, PackageSession session, UFile? sessionFile, IEditorRestartService? restart = null, Func<IReadOnlyList<PluginInfo>>? collect = null)
    {
        var descriptor = BuiltInPanels.CreatePluginsPanel(_ => throw new NotSupportedException());
        var context = new PanelContext { Descriptor = descriptor, Session = fixture.Session, ServiceProvider = fixture.ServiceProvider };
        // The engine's Physics plugin, as the catalog lists a built-in package of the session
        var builtIn = new PluginInfo { Id = "Stride.Physics.Editor", DisplayName = "Stride.Physics.Editor", Version = EditorPackageVersion, Source = PluginSource.BuiltIn, State = PluginState.Joined };
        var panel = new PluginsPanelViewModel(context, host, collect ?? (() => [.. PluginCatalog.Collect(host, session), builtIn]), sessionFile, restart);
        disposables.Add(new Destroyer(panel));
        return panel;
    }

    private void RefuseIncompatible(RecordingEditorPluginHost host)
    {
        var running = typeof(EditorApiCheck).Assembly.GetName().Version!;
        var path = WriteAssembly("Incompatible.Editor", ("Stride.Editor", new Version(running.Major, running.Minor + 1, 0, 0)));
        Assert.False(host.AssemblyLoadCheck.CanLoad(new Package { Meta = { Name = "Incompatible" } }, path, new LoggerResult()));
    }

    private DisabledEditorCompanionStore CreateStore() => new(() => profile, key, () => { });

    // A package as NuGet restores it: its .nuspec at the root, its sdpkg under stride/
    private string WriteRestoredPackage(string id, string title)
    {
        var root = Path.Combine(directory, id.ToLowerInvariant(), Version.ToString());
        Directory.CreateDirectory(Path.Combine(root, "stride"));
        File.WriteAllText(Path.Combine(root, id.ToLowerInvariant() + ".nuspec"), $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{id}</id>
                <version>{Version}</version>
                <title>{title}</title>
                <authors>Someone</authors>
                <description>Edits the plugin's assets.</description>
              </metadata>
            </package>
            """);
        return root;
    }

    /// <summary>
    /// Writes an assembly with no code that references the given assemblies, as a plugin built against them would.
    /// </summary>
    private string WriteAssembly(string name, params (string Name, Version Version)[] references)
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
        var path = Path.Combine(directory, name + ".dll");
        File.WriteAllBytes(path, image.ToArray());
        return path;
    }

    private static Package AddDeclaringPackage(PackageSession session, string name, params CompanionPackage[] companions)
    {
        var package = new Package { Meta = { Name = name, Version = Version } };
        package.CompanionPackages.AddRange(companions);
        session.Projects.Add(new StandalonePackage(package) { IsDependencyPackage = true });
        return package;
    }

    private static Package Declaring(string name) => new() { Meta = { Name = name, Version = new PackageVersion("1.0.0") } };

    private static CompanionPackage Companion(string name, PackageVersion? version = null)
    {
        return new CompanionPackage { Name = name, Version = version ?? new PackageVersion("1.0.0"), Kind = PackageKind.Editor };
    }

    private sealed class RecordingRestartService : IEditorRestartService
    {
        public List<bool> Calls { get; } = [];

        public Task<bool> RestartAsync(bool safeMode)
        {
            Calls.Add(safeMode);
            return Task.FromResult(true);
        }
    }

    private sealed class Destroyer(PluginsPanelViewModel panel) : IDisposable
    {
        public void Dispose() => panel.Destroy();
    }
}
