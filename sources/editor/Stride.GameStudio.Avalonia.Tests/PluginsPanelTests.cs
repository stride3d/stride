// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Plugins;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Diagnostics;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.GameStudio.Avalonia.Views.Panels;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Plugins panel and the safe mode banner in the main window, on a real session: the panel lists the session's plugins and
/// follows them, a third-party plugin's switch shows "Restart to apply", and both restarts open the same session in a new
/// editor with the right command line.
/// </summary>
public sealed class PluginsPanelTests
{
    /// <summary>
    /// The command line of this editor: the options a new one gets again.
    /// </summary>
    private static readonly string[] CommandLine = ["/DisablePreview", "--graphics-api", "Vulkan"];

    [AvaloniaFact]
    public async Task ThePanelIsHiddenByDefaultListsThePluginsAndFollowsAPluginThatFails()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            // Hidden by default, in the View menu
            Assert.False(panels.IsVisible(BuiltInPanels.Plugins));
            var entry = Assert.Single(fixture.Main.ViewMenu.Items, x => x.PanelId == BuiltInPanels.Plugins);
            Assert.Equal("Plugins", entry.Header);
            // No banner out of safe mode
            Assert.False(window.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "SafeModeBanner").IsVisible);

            panels.Show(BuiltInPanels.Plugins);
            await WindowTestHelper.PumpAsync();
            var view = Assert.Single(window.GetVisualDescendants().OfType<PluginsView>());
            var panel = Assert.IsType<PluginsPanelViewModel>(view.DataContext);

            // The editor's own plugins: built in, on, without a switch
            Assert.NotEmpty(panel.Plugins);
            Assert.All(panel.Plugins, x => Assert.Equal((PluginState.Joined, true, false), (x.State, x.IsBuiltIn, x.CanSwitch)));
            // Game Studio's own assemblies come in their own group, with Game Studio's icon in the icon column
            Assert.Equal(PluginGroup.GameStudio, panel.Plugins[0].Group);
            Assert.All(panel.Plugins.Where(x => x.Group == PluginGroup.GameStudio), x => Assert.True(x.IsPartOfGameStudio));
            var icons = view.GetVisualDescendants().OfType<Image>().Where(x => x.Name == "GameStudioIcon").ToList();
            Assert.NotEmpty(icons);
            Assert.All(icons, x => Assert.True(x.IsVisible && x.Source is not null && Grid.GetColumn(x) == 0));
            Assert.All(view.GetVisualDescendants().OfType<ToggleSwitch>().Where(x => x.Name == "PluginSwitch"), x => Assert.False(x.IsVisible));
            Assert.False(view.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "RestartBar").IsVisible);
            Assert.Equal($"{panel.Plugins.Count} plugins", panel.Summary);

            // A contribution of a plugin fails while the panel is open: the plugin comes first, with its error
            var plugin = Assert.Single(fixture.Plugins.SessionPlugins.OfType<NotebookTestPlugin>());
            Assert.False(fixture.Plugins.TryRun(plugin, PluginContributions.Command("Tests.Explode"), () => throw new InvalidOperationException("Boom")));
            await WindowTestHelper.PumpAsync();
            var failed = panel.Plugins[0];
            Assert.True(failed.NeedsAttention);
            Assert.Equal("On, with errors", failed.StateText);
            Assert.Equal("Plugins (1)", panel.Title);
            panel.SelectedPlugin = failed;
            await WindowTestHelper.PumpAsync();
            var errors = view.GetVisualDescendants().OfType<ItemsControl>().Single(x => x.Name == "ErrorList");
            var error = Assert.IsType<PluginErrorViewModel>(Assert.Single(errors.Items));
            Assert.Equal("Command Tests.Explode", error.Contribution);
            Assert.Contains(view.GetVisualDescendants().OfType<SelectableTextBlock>(), x => x.Text == "Boom");

            // The session closes: the panel goes, and empties
            Assert.True(await fixture.Main.CloseSessionAsync());
            fixture.Main.CloseSessionServices();
            Assert.Empty(panel.Plugins);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ASwitchedOffPluginGoesOnFromTheNextOpeningAndRestartToApplyOpensTheSessionAgain()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        // A list of the test's own, not the user's
        var store = CreateStore();
        fixture.Plugins.DisabledCompanions = store;
        store.Set(fixture.PackagePath, "Plugin.Editor", switchedOff: true);
        await fixture.OpenAsync();
        // The package the session left out (the test package declares none: its filter is asked directly)
        Assert.False(fixture.Plugins.GetCompanionPackageFilter(fixture.PackagePath).ShouldLoad(Declaring("Plugin"), Companion("Plugin.Editor"), new LoggerResult()));
        var launcher = UseProcessLauncher(fixture, CommandLine);
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            fixture.Main.Panels.Show(BuiltInPanels.Plugins);
            await WindowTestHelper.PumpAsync();
            var view = Assert.Single(window.GetVisualDescendants().OfType<PluginsView>());
            var panel = Assert.IsType<PluginsPanelViewModel>(view.DataContext);
            var item = panel.Plugins[0];
            Assert.Equal(("Plugin.Editor", "Off: switched off", false), (item.Id, item.StateText, item.IsOn));
            Assert.True(item.CanSwitch);
            // Its switch shows, the built-in ones have none
            var switches = view.GetVisualDescendants().OfType<ToggleSwitch>().Where(x => x.Name == "PluginSwitch").ToList();
            Assert.Single(switches, x => x.IsVisible);
            Assert.Same(item, switches.Single(x => x.IsVisible).DataContext);

            // Switched on: no question, the list forgets it, and the panel offers to restart
            switches.Single(x => x.IsVisible).Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => store.Get(fixture.PackagePath).Count == 0, "the list without the plugin");
            await WindowTestHelper.PumpAsync();
            Assert.True(item.IsPending);
            var bar = view.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "RestartBar");
            Assert.True(bar.IsVisible);
            var restart = view.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "RestartButton");
            Assert.Equal("Restart to apply", restart.Content);

            // "Restart to apply": the same session in a new editor, with the same options
            restart.Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count > 0, "the start of the new editor");
            Assert.Equal([.. CommandLine, fixture.PackagePath.ToOSPath()], Assert.Single(launcher.Starts));
            Assert.Null(fixture.Main.Session);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task InSafeModeTheBannerCountsThePluginsOffOpensThePanelAndRestartsNormally()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        fixture.Plugins.DisabledCompanions = CreateStore();
        fixture.Plugins.SafeMode = true;
        await fixture.OpenAsync();
        var launcher = UseProcessLauncher(fixture, [.. CommandLine, "--safe-mode"]);
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var banner = window.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "SafeModeBanner");
            var message = window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Name == "SafeModeMessage");
            Assert.True(banner.IsVisible);
            // The test package has no plugin of its own
            Assert.Equal("Safe mode: only the plugins of Stride are loaded.", message.Text);

            // Two third-party packages and a built-in one: the two are off
            var filter = fixture.Plugins.GetCompanionPackageFilter(fixture.PackagePath);
            var log = new LoggerResult();
            Assert.False(filter.ShouldLoad(Declaring("First"), Companion("First.Editor"), log));
            Assert.False(filter.ShouldLoad(Declaring("Second"), Companion("Second.Editor"), log));
            var engine = new PackageVersion($"{BuiltInPlugins.EditorVersion.Major}.{BuiltInPlugins.EditorVersion.Minor}.0");
            Assert.True(filter.ShouldLoad(Declaring("Stride.Physics"), Companion("Stride.Physics.Editor", engine), log));
            await WindowTestHelper.PumpAsync();
            Assert.Equal("Safe mode: 2 plugins of this project are off. Only the plugins of Stride are loaded.", message.Text);

            // The banner opens the Plugins panel, which lists them off in safe mode
            window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "OpenPluginsPanelButton").Command!.Execute(null);
            await WindowTestHelper.PumpAsync();
            Assert.True(fixture.Main.Panels.IsVisible(BuiltInPanels.Plugins));
            var panel = Assert.IsType<PluginsPanelViewModel>(Assert.Single(window.GetVisualDescendants().OfType<PluginsView>()).DataContext);
            Assert.Equal(["First.Editor", "Second.Editor"], panel.Plugins.Where(x => x.Group == PluginGroup.Off).Select(x => x.Id));
            Assert.All(panel.Plugins.Where(x => x.Group == PluginGroup.Off), x => Assert.Equal("Off: safe mode", x.StateText));
            Assert.Equal("Restart normally", panel.RestartText);

            // "Restart normally": the same session in a new editor, without --safe-mode
            window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "RestartNormallyButton").Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count > 0, "the start of the new editor");
            Assert.Equal([.. CommandLine, fixture.PackagePath.ToOSPath()], Assert.Single(launcher.Starts));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task RestartToApplyKeepsSafeModeOffAndTheOtherOptions()
    {
        // From safe mode, "Restart to apply" restarts normally too: what was switched applies
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var launcher = UseProcessLauncher(fixture, ["--safe-mode", "/DisableThumbnails"]);

        Assert.True(await ((IEditorRestartService)fixture.Main).RestartAsync(safeMode: false));

        Assert.Equal(["/DisableThumbnails", fixture.PackagePath.ToOSPath()], Assert.Single(launcher.Starts));
    }

    [AvaloniaFact]
    public async Task ARestartIntoSafeModeAddsTheOption()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var launcher = UseProcessLauncher(fixture, ["/DisableThumbnails"]);

        Assert.True(await ((IEditorRestartService)fixture.Main).RestartAsync(safeMode: true));

        Assert.Equal(["/DisableThumbnails", "--safe-mode", fixture.PackagePath.ToOSPath()], Assert.Single(launcher.Starts));
    }

    /// <summary>
    /// Gives the editor a process service that records the processes it starts instead of starting them.
    /// </summary>
    private static RecordingProcessLauncher UseProcessLauncher(ShellSessionFixture fixture, string[] commandLine)
    {
        var launcher = new RecordingProcessLauncher();
        launcher.Service = new EditorProcessService(launcher, StartupOptions.Parse(commandLine));
        fixture.ServiceProvider.RegisterService(launcher.Service);
        return launcher;
    }

    /// <summary>
    /// A list of switched-off packages kept in memory: the tests never write the user's settings.
    /// </summary>
    private static DisabledEditorCompanionStore CreateStore()
    {
        var container = new SettingsContainer();
        var key = new SettingsKey<List<DisabledEditorCompanions>>(InternalSettings.DisabledEditorCompanions.Name, container, () => new List<DisabledEditorCompanions>());
        var profile = container.CreateSettingsProfile(true);
        return new DisabledEditorCompanionStore(() => profile, key, () => { });
    }

    private static Package Declaring(string name) => new() { Meta = { Name = name, Version = new PackageVersion("1.0.0") } };

    private static CompanionPackage Companion(string name, PackageVersion? version = null)
    {
        return new CompanionPackage { Name = name, Version = version ?? new PackageVersion("1.0.0"), Kind = PackageKind.Editor };
    }
}
