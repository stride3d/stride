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
using Stride.Core.Assets.Tests.Helpers;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.GameStudio.Avalonia.Views.Panels;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The extensions section of the Plugins panel, on a real session: an extension added from a <c>.nupkg</c> file or a folder
/// shows as added until Game Studio restarts, and an extension is switched off and removed for every project. The list of
/// extensions is kept in memory and the feed, the packages and the NuGet folders are in the fixture's temporary folder: the tests
/// never touch the user's settings or NuGet folders.
/// </summary>
public sealed class ExtensionsPanelTests
{
    private static readonly string[] CommandLine = ["/DisablePreview"];

    [AvaloniaFact]
    public async Task AnExtensionAddedFromAPackageFileWaitsForTheRestartThenIsSwitchedOffAndRemoved()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        var extensions = UseExtensions(fixture);
        await fixture.OpenAsync();
        using var downloads = new LocalPackageFeed(Path.Combine(fixture.Directory, "downloads"));
        var package = downloads.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, title: "A tool");
        var dialogs = fixture.ReplaceDialogService();
        IReadOnlyList<FilePickerFilter>? offered = null;
        dialogs.PickFile = filters =>
        {
            offered = filters;
            return new UFile(package);
        };
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var (view, panel) = await ShowPanelAsync(fixture, window);
            Assert.True(panel.CanManageExtensions);
            Assert.DoesNotContain(panel.Plugins, x => x.IsExtension);

            // "Add an extension", "From a package file (.nupkg)...": the file picker offers the NuGet packages
            await ClickMenuItemAsync(view, "AddExtensionFromFileItem");
            await WindowTestHelper.WaitUntilAsync(() => panel.Plugins.Any(x => x.Id == "Tool.Editor"), "the extension in the panel");
            Assert.Equal(["*.nupkg"], Assert.Single(offered!).Patterns);
            Assert.Empty(dialogs.Messages);
            var installed = Assert.Single(extensions.GetExtensions());
            // A toolkit-neutral package: both editors load it
            Assert.Equal(("Tool.Editor", "1.0.0", null, false), (installed.Id, installed.Version, installed.Toolkit, installed.IsOff));
            Assert.True(File.Exists(Path.Combine(extensions.FeedDirectory, "Tool.Editor.1.0.0.nupkg")));

            // Selected, added for the next opening: in the Extensions group, pending, with its switch and Remove
            var item = panel.Plugins.Single(x => x.Id == "Tool.Editor");
            Assert.Same(item, panel.SelectedPlugin);
            Assert.Equal((PluginGroup.Extensions, "Added", "Extension (installed for every project)"), (item.Group, item.StateText, item.SourceText));
            Assert.Equal("A tool", item.DisplayName);
            Assert.True(item.IsPending);
            Assert.Equal("Added: it loads when Game Studio opens a project again (Restart to apply).", item.PendingText);
            Assert.True(item.CanSwitch);
            Assert.True(item.IsOn);
            Assert.True(item.CanRemove);
            await WindowTestHelper.PumpAsync();
            Assert.True(view.GetVisualDescendants().OfType<Border>().Single(x => x.Name == "RestartBar").IsVisible);
            Assert.True(view.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "RemoveExtensionButton").IsVisible);
            Assert.Contains(view.GetVisualDescendants().OfType<ToggleSwitch>(), x => x.Name == "PluginSwitch" && x.IsVisible && x.DataContext == item);

            // Switched off, after a question that says it is for every project: it stays installed, nothing is pending
            Assert.True(await item.SetOnAsync(false));
            Assert.Contains("in every project", Assert.Single(dialogs.Messages), StringComparison.Ordinal);
            Assert.True(Assert.Single(extensions.GetExtensions()).IsOff);
            Assert.False(item.IsOn);
            Assert.False(item.IsPending);
            Assert.False(panel.NeedsRestart);
            // And on again: no question
            Assert.True(await item.SetOnAsync(true));
            Assert.Single(dialogs.Messages);
            Assert.False(Assert.Single(extensions.GetExtensions()).IsOff);
            Assert.True(item.IsPending);

            // Removed, after a question: it leaves the list, the user's list and the feed
            dialogs.Messages.Clear();
            view.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "RemoveExtensionButton").Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => panel.Plugins.All(x => x.Id != "Tool.Editor"), "the extension out of the panel");
            Assert.StartsWith("Remove the extension A tool?", Assert.Single(dialogs.Messages), StringComparison.Ordinal);
            Assert.Empty(extensions.GetExtensions());
            Assert.False(File.Exists(Path.Combine(extensions.FeedDirectory, "Tool.Editor.1.0.0.nupkg")));
            Assert.False(panel.NeedsRestart);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AFolderAddsItsExtensionsSaysWhatItRefusedAndRestartToApplyOpensTheSessionAgain()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        var extensions = UseExtensions(fixture);
        await fixture.OpenAsync();
        // A folder of packages, as dotnet pack writes them: an extension with its Avalonia views, another one, a package of the
        // WPF editor and a runtime library
        using var downloads = new LocalPackageFeed(Path.Combine(fixture.Directory, "downloads"));
        var views = new CompanionPackage { Name = "Tool.Editor.Avalonia", Version = new PackageVersion("1.0.0"), Kind = PackageKind.Editor, Toolkit = EditorToolkits.Avalonia };
        downloads.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, companions: [views]);
        downloads.AddPackage("Tool.Editor.Avalonia", "1.0.0", PackageKind.Editor, EditorToolkits.Avalonia);
        downloads.AddPackage("Other.Editor", "2.0.0", PackageKind.Editor);
        downloads.AddPackage("Old.Editor.Wpf", "1.0.0", PackageKind.Editor, EditorToolkits.Wpf);
        downloads.AddPackage("Library", "1.0.0", PackageKind.Runtime);
        var dialogs = fixture.ReplaceDialogService();
        dialogs.PickFolder = () => new UDirectory(downloads.FeedDirectory);
        var launcher = UseProcessLauncher(fixture);
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var (view, panel) = await ShowPanelAsync(fixture, window);

            await ClickMenuItemAsync(view, "AddExtensionFromFolderItem");
            await WindowTestHelper.WaitUntilAsync(() => dialogs.Messages.Count > 0, "the message of what was refused");

            // The two extensions, not the companion of the first: it comes with it
            Assert.Equal(["Other.Editor", "Tool.Editor"], extensions.GetExtensions().Select(x => x.Id).Order());
            Assert.True(File.Exists(Path.Combine(extensions.FeedDirectory, "Tool.Editor.Avalonia.1.0.0.nupkg")));
            var message = Assert.Single(dialogs.Messages);
            Assert.Contains("Old.Editor.Wpf", message, StringComparison.Ordinal);
            Assert.Contains("WPF", message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Library", message, StringComparison.Ordinal);
            Assert.Equal(["Other.Editor", "Tool.Editor"], panel.Plugins.Where(x => x.IsExtension).Select(x => x.Id).Order());
            Assert.All(panel.Plugins.Where(x => x.IsExtension), x => Assert.Equal(("Added", true), (x.StateText, x.IsPending)));

            // "Restart to apply": the same session in a new editor
            await WindowTestHelper.PumpAsync();
            var restart = view.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "RestartButton");
            Assert.True(restart.IsEffectivelyVisible);
            restart.Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => launcher.Starts.Count > 0, "the start of the new editor");
            Assert.Equal([.. CommandLine, fixture.PackagePath.ToOSPath()], Assert.Single(launcher.Starts));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ALoadedExtensionSwitchedOffOrRemovedChangesInEveryProjectAtTheNextOpening()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        // The NuGet setup of the session: a nuget.config above the project, with a global packages folder of its own and no
        // source but the extensions' feed. The fixture deletes its folder.
        var nuget = new LocalPackageFeed(fixture.Directory);
        var extensions = UseExtensions(fixture, nuget.PackagesDirectory);
        var package = nuget.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, title: "A tool");
        Assert.Empty(extensions.InstallFromFile(package, EditorToolkits.Avalonia).Problems);
        await fixture.OpenAsync();
        Assert.Contains(fixture.Main.Session!.PackageSession.Projects, x => x.Package.Meta.Name == "Tool.Editor");
        // Restored with the project's NuGet settings, not the user's
        Assert.True(Directory.Exists(Path.Combine(nuget.PackagesDirectory, "tool.editor", "1.0.0")));
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var (_, panel) = await ShowPanelAsync(fixture, window);
            var item = panel.Plugins.Single(x => x.Id == "Tool.Editor");
            Assert.Equal((PluginGroup.Extensions, "On", true, true), (item.Group, item.StateText, item.IsLoaded, item.CanSwitch));
            Assert.False(item.IsPending);
            Assert.False(panel.NeedsRestart);

            // Switched off: loaded until the next opening, in every project
            Assert.True(await item.SetOnAsync(false));
            Assert.Contains("in every project", Assert.Single(dialogs.Messages), StringComparison.Ordinal);
            Assert.True(item.IsPending);
            Assert.Equal("Changed: it applies in every project when Game Studio opens a project again (Restart to apply).", item.PendingText);
            Assert.True(panel.NeedsRestart);

            // Removed: it stays in the list while it is loaded, without its switch
            Assert.True(await item.RemoveAsync());
            Assert.Empty(extensions.GetExtensions());
            item = panel.Plugins.Single(x => x.Id == "Tool.Editor");
            Assert.False(item.IsInstalled);
            Assert.False(item.CanSwitch);
            Assert.False(item.CanRemove);
            Assert.Equal("Removed: it goes when Game Studio opens a project again (Restart to apply).", item.PendingText);
            Assert.True(panel.NeedsRestart);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ASwitchedOffExtensionRemovedSaysSoUntilTheNextOpening()
    {
        using var fixture = ShellSessionFixture.Create(("Sheet", new SpriteSheetAsset()));
        using var downloads = new LocalPackageFeed(Path.Combine(fixture.Directory, "downloads"));
        var extensions = UseExtensions(fixture);
        var package = downloads.AddPackage("Tool.Editor", "1.0.0", PackageKind.Editor, title: "A tool");
        Assert.Empty(extensions.InstallFromFile(package, EditorToolkits.Avalonia).Problems);
        Assert.True(extensions.SetOff("Tool.Editor", true));
        await fixture.OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var (_, panel) = await ShowPanelAsync(fixture, window);
            var item = panel.Plugins.Single(x => x.Id == "Tool.Editor");
            Assert.Equal(("Off: switched off", false, false), (item.StateText, item.IsLoaded, item.IsPending));

            // Removed: nothing changes at the next opening (it was off), and it says it was removed
            Assert.True(await item.RemoveAsync());
            Assert.Single(dialogs.Messages);
            item = panel.Plugins.Single(x => x.Id == "Tool.Editor");
            Assert.Equal(("Off: removed", false, false, false), (item.StateText, item.CanSwitch, item.CanRemove, item.IsPending));
            Assert.Equal("It was removed: it leaves this list when Game Studio opens a project again.", item.StateDetail);
            Assert.False(panel.NeedsRestart);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// Gives the editor extensions of the test's own: a list in memory and a feed in the fixture's folder. The session reads
    /// them when it opens.
    /// </summary>
    /// <param name="fixture">The fixture, before its session opens.</param>
    /// <param name="globalPackagesFolder">The global packages folder of the session's NuGet setup, or null when it restores
    /// nothing.</param>
    private static EditorExtensionService UseExtensions(ShellSessionFixture fixture, string? globalPackagesFolder = null)
    {
        var container = new SettingsContainer();
        var key = new SettingsKey<List<EditorExtensionList>>(InternalSettings.ExtensionPackages.Name, container, () => new List<EditorExtensionList>());
        var profile = container.CreateSettingsProfile(true);
        var store = new EditorExtensionStore(() => profile, key, () => { });
        var packages = globalPackagesFolder ?? Path.Combine(fixture.Directory, "packages");
        var service = new EditorExtensionService(store, Path.Combine(fixture.Directory, "extensions"), BuiltInPlugins.EditorVersion,
            (id, version) => Path.Combine(packages, id.ToLowerInvariant(), version.ToString()));
        fixture.Plugins.Extensions = service;
        return service;
    }

    private static async Task<(PluginsView View, PluginsPanelViewModel Panel)> ShowPanelAsync(ShellSessionFixture fixture, MainWindow window)
    {
        fixture.Main.Panels.Show(BuiltInPanels.Plugins);
        await WindowTestHelper.PumpAsync();
        var view = Assert.Single(window.GetVisualDescendants().OfType<PluginsView>());
        return (view, Assert.IsType<PluginsPanelViewModel>(view.DataContext));
    }

    /// <summary>
    /// Opens the menu of "Add an extension" and clicks one of its items.
    /// </summary>
    private static async Task ClickMenuItemAsync(PluginsView view, string name)
    {
        var button = view.GetVisualDescendants().OfType<DropDownButton>().Single(x => x.Name == "AddExtensionButton");
        Assert.True(button.IsEffectivelyVisible);
        var flyout = Assert.IsType<MenuFlyout>(button.Flyout);
        flyout.ShowAt(button);
        await WindowTestHelper.PumpAsync();
        // Its command, bound while the menu is open
        var command = flyout.Items.OfType<MenuItem>().Single(x => x.Name == name).Command;
        Assert.NotNull(command);
        flyout.Hide();
        command.Execute(null);
    }

    private static RecordingProcessLauncher UseProcessLauncher(ShellSessionFixture fixture)
    {
        var launcher = new RecordingProcessLauncher();
        launcher.Service = new EditorProcessService(launcher, StartupOptions.Parse(CommandLine));
        fixture.ServiceProvider.RegisterService(launcher.Service);
        return launcher;
    }
}
