// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Services;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The main window's menus and toolbar bound to the commands by id, with the user's shortcuts; New, Open and Exit without a
/// session; the command palette; and the Keyboard shortcuts page of the settings.
/// </summary>
public sealed class CommandShellTests
{
    private const string SheetLocation = "Sheet";

    [AvaloniaFact]
    public async Task TheMenuItemsAndTheToolbarBindTheEditorsCommandsById()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var view = window.View;
            var items = view.MainMenu.Items.OfType<MenuItem>().SelectMany(Flatten).Where(x => x.Command is CommandReference).ToList();
            Assert.True(items.Count >= 25, $"Only {items.Count} items bound by id");
            foreach (var item in items)
            {
                var reference = (CommandReference)item.Command!;
                Assert.Contains(fixture.Plugins.Commands.Items, x => x.Id == reference.Id);
                Assert.NotNull(reference.Target);
                Assert.False(string.IsNullOrEmpty(item.Header as string), reference.Id);
            }

            // The title of the command, unless the item keeps its own header for its access key; the shortcut of the command
            Assert.Equal("Undo", MenuItem(window, "UndoMenuItem").Header);
            Assert.Equal("_New...", MenuItem(window, "NewMenuItem").Header);
            Assert.Equal(KeyGesture.Parse("Ctrl+S"), MenuItem(window, "SaveMenuItem").InputGesture);
            Assert.Equal(KeyGesture.Parse("Ctrl+Z"), MenuItem(window, "UndoMenuItem").InputGesture);
            Assert.Equal(KeyGesture.Parse("Ctrl+Shift+D"), MenuItem(window, "DebugWindowMenuItem").InputGesture);
            // The toolbar's buttons end their tool tips with the shortcut
            Assert.Equal("Build the project (F6)", ToolTip.GetTip(Button(window, "BuildProjectButton")));
            Assert.Equal("Open in IDE", ToolTip.GetTip(Button(window, "OpenInIDEButton")));

            // The items follow their command's state: Cancel build only during a build
            Assert.True(await IsEnabledInOpenMenuAsync(MenuItem(window, "SaveMenuItem")));
            Assert.False(await IsEnabledInOpenMenuAsync(MenuItem(window, "CancelBuildMenuItem")));
            Assert.False(Button(window, "CancelBuildButton").IsEffectivelyEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AChangedShortcutRunsItsCommandAndShowsInItsMenuItemAndItsOldDefaultNoLongerRunsIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        try
        {
            var dialogs = fixture.ReplaceDialogService();
            fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Asset(SheetLocation).Directory);
            overrides.SetValue(new Dictionary<string, string>
            {
                [SessionCommands.AddAssetId] = "Ctrl+Alt+I",
                [GameStudioCommands.SaveId] = "Ctrl+Alt+S",
                [GameStudioCommands.BuildProjectId] = "Ctrl+Alt+B",
            });
            window.View.CommandSlots!.Update();

            // The menu items and the tool tips show the user's shortcuts
            Assert.Equal(KeyGesture.Parse("Ctrl+Alt+S"), MenuItem(window, "SaveMenuItem").InputGesture);
            Assert.Equal(KeyGesture.Parse("Ctrl+Alt+B"), MenuItem(window, "BuildProjectMenuItem").InputGesture);
            Assert.Equal("Build the project (Ctrl+Alt+B)", ToolTip.GetTip(Button(window, "BuildProjectButton")));

            // The old default does nothing now; the new shortcut runs the command
            Press(window, PhysicalKey.I, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Empty(dialogs.AddAssetDialogs);
            Press(window, PhysicalKey.I, RawInputModifiers.Control | RawInputModifiers.Alt);
            await WindowTestHelper.WaitUntilAsync(() => dialogs.AddAssetDialogs.Count == 1, "the add asset dialog");
        }
        finally
        {
            overrides.SetValue(new Dictionary<string, string>());
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AShortcutSetToNoneRunsNothingAndItsMenuItemShowsNone()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        try
        {
            var dialogs = fixture.ReplaceDialogService();
            fixture.Session.AssetCollection.SelectedLocations.Add(fixture.Asset(SheetLocation).Directory);
            overrides.SetValue(new Dictionary<string, string>
            {
                [SessionCommands.AddAssetId] = CommandGestureBindings.NoGesture,
                [GameStudioCommands.SaveId] = CommandGestureBindings.NoGesture,
            });
            window.View.CommandSlots!.Update();

            Assert.Null(MenuItem(window, "SaveMenuItem").InputGesture);
            Assert.DoesNotContain(window.View.CommandSlots.Shortcuts, x => x.Descriptor.Id is SessionCommands.AddAssetId or GameStudioCommands.SaveId);
            Press(window, PhysicalKey.I, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Empty(dialogs.AddAssetDialogs);
            // The item still runs it
            Assert.True(await IsEnabledInOpenMenuAsync(MenuItem(window, "SaveMenuItem")));
        }
        finally
        {
            overrides.SetValue(new Dictionary<string, string>());
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task NewOpenAndExitHaveTheirShortcutsWithoutASession()
    {
        using var fixture = ShellSessionFixture.Create((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        try
        {
            var slots = window.View.CommandSlots!;
            string? GestureOf(string id) => slots.Shortcuts.SingleOrDefault(x => x.Descriptor.Id == id)?.Gesture.ToString();
            Assert.Equal("Ctrl+N", GestureOf(GameStudioCommands.NewId));
            Assert.Equal("Ctrl+O", GestureOf(GameStudioCommands.OpenId));
            Assert.Equal("Alt+F4", GestureOf(GameStudioCommands.ExitId));
            Assert.Equal("Ctrl+Shift+D", GestureOf(GameStudioCommands.DebugWindowId));
            Assert.Equal("Ctrl+Shift+P", GestureOf(GameStudioCommands.CommandPaletteId));
            // Nothing that needs a session
            Assert.Null(GestureOf(GameStudioCommands.SaveId));
            Assert.Equal(KeyGesture.Parse("Ctrl+N"), MenuItem(window, "NewMenuItem").InputGesture);
            Assert.Equal(KeyGesture.Parse("Alt+F4"), MenuItem(window, "ExitMenuItem").InputGesture);
            // New and Exit are enabled as the main view model's commands (they need the desktop's main window); Save is not
            var newItem = (CommandReference)MenuItem(window, "NewMenuItem").Command!;
            Assert.Equal(fixture.Main.NewSessionCommand.CanExecute(null), newItem.CanExecute(null));
            Assert.Equal(fixture.Main.ExitCommand.CanExecute(null), ((CommandReference)MenuItem(window, "ExitMenuItem").Command!).CanExecute(null));
            Assert.False(await IsEnabledInOpenMenuAsync(MenuItem(window, "SaveMenuItem")));

            // Open (always enabled) runs by its shortcut
            Assert.True(PressHandled(window, PhysicalKey.O, RawInputModifiers.Control));

            // The user's shortcuts apply without a session too
            overrides.SetValue(new Dictionary<string, string>
            {
                [GameStudioCommands.OpenId] = "Ctrl+Alt+O",
                [GameStudioCommands.NewId] = "Ctrl+Alt+N",
                [GameStudioCommands.ExitId] = CommandGestureBindings.NoGesture,
            });
            slots.Update();
            Assert.Equal("Ctrl+Alt+O", GestureOf(GameStudioCommands.OpenId));
            Assert.Equal("Ctrl+Alt+N", GestureOf(GameStudioCommands.NewId));
            Assert.Null(GestureOf(GameStudioCommands.ExitId));
            Assert.Equal(KeyGesture.Parse("Ctrl+Alt+N"), MenuItem(window, "NewMenuItem").InputGesture);
            Assert.Null(MenuItem(window, "ExitMenuItem").InputGesture);
            Assert.False(PressHandled(window, PhysicalKey.O, RawInputModifiers.Control));
            Assert.True(PressHandled(window, PhysicalKey.O, RawInputModifiers.Control | RawInputModifiers.Alt));

            // A session opens: the registry's commands, with the same user's shortcuts
            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.WaitUntilAsync(() => GestureOf(GameStudioCommands.SaveId) is not null, "the session's shortcuts");
            Assert.Equal("Ctrl+Alt+O", GestureOf(GameStudioCommands.OpenId));
            Assert.Contains(fixture.Plugins.Commands.Items, x => x.Id == GameStudioCommands.OpenId);
        }
        finally
        {
            overrides.SetValue(new Dictionary<string, string>());
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ThePaletteSearchesTitlesCategoriesAndKeywordsAndRunsACommandWithTheRecentFirst()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var recent = GameStudioInternalSettings.RecentCommands;
        var previousRecent = recent.GetValue();
        var contexts = new List<CommandContext>();
        var plugin = new CommandsTestPlugin(context => context.Commands.Register(new CommandDescriptor
        {
            Id = "Tests.Recorder",
            Title = () => "Record the context",
            Category = "Tests",
            Keywords = ["probe"],
            ExecuteAsync = context =>
            {
                contexts.Add(context);
                return Task.CompletedTask;
            },
        }));
        try
        {
            recent.SetValue([]);
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();

            // Ctrl+Shift+P opens it, with the focus in its search box
            var assetList = window.GetVisualDescendants().OfType<ListBox>().First(x => x.Name == "AssetList");
            Assert.True(assetList.Focus());
            Press(window, PhysicalKey.P, RawInputModifiers.Control | RawInputModifiers.Shift);
            await WindowTestHelper.PumpAsync();
            var popup = window.View.CommandPaletteHost;
            Assert.True(popup.IsOpen);
            var palette = Assert.IsType<CommandPaletteViewModel>(window.View.CommandPalette.DataContext);
            Assert.DoesNotContain(palette.Results, x => x.Id == GameStudioCommands.CommandPaletteId);

            // By title, by category, by keyword, with the shortcuts
            palette.SearchText = "save";
            Assert.Equal(GameStudioCommands.SaveId, palette.Results.First().Id);
            Assert.Equal("Ctrl+S", palette.Results.First().Gesture);
            palette.SearchText = "project build";
            Assert.Equal(GameStudioCommands.BuildProjectId, palette.Results.First().Id);
            palette.SearchText = "compile";
            Assert.Equal(GameStudioCommands.BuildProjectId, Assert.Single(palette.Results).Id);
            palette.SearchText = "probe";
            Assert.Equal("Tests.Recorder", Assert.Single(palette.Results).Id);
            // The panels' toggles, with their checks
            palette.SearchText = "asset errors";
            var toggle = palette.Results.First(x => x.Id == PanelCommands.IdOf(BuiltInPanels.AssetLog));
            Assert.Equal(fixture.Main.Panels.IsVisible(BuiltInPanels.AssetLog), toggle.IsChecked);

            // Enter runs the selected command, through the guard, in the palette's context, from the control it was opened from
            palette.SearchText = "record the context";
            window.View.CommandPalette.SearchBox.Focus();
            Press(window, PhysicalKey.Enter, RawInputModifiers.None);
            await WindowTestHelper.WaitUntilAsync(() => contexts.Count == 1, "the command");
            Assert.False(popup.IsOpen);
            Assert.Equal(CommandLocations.Palette, contexts[0].Location);
            Assert.Same(assetList, contexts[0].Source);
            Assert.Equal(["Tests.Recorder"], recent.GetValue());

            // Opened again (from the View menu's command), it lists the recently used command first
            window.View.ShowCommandPalette(null);
            palette = Assert.IsType<CommandPaletteViewModel>(window.View.CommandPalette.DataContext);
            Assert.Equal("Tests.Recorder", palette.Results.First().Id);
            Assert.True(palette.Results.First().IsRecent);
            // Escape closes it
            Press(window, PhysicalKey.Escape, RawInputModifiers.None);
            await WindowTestHelper.PumpAsync();
            Assert.False(popup.IsOpen);
        }
        finally
        {
            recent.SetValue(previousRecent);
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheViewMenusTogglesAreCheckedAsTheirPanelsAreShown()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var panels = fixture.Main.Panels;
            var view = window.View;
            MenuItem Toggle() => view.ViewMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, BuiltInPanels.AssetLog));

            panels.Show(BuiltInPanels.AssetLog);
            await WindowTestHelper.PumpAsync();
            view.ViewMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(PanelCommands.IdOf(BuiltInPanels.AssetLog), Assert.IsType<CommandReference>(Toggle().Command).Id);
            Assert.Equal(MenuItemToggleType.CheckBox, Toggle().ToggleType);
            Assert.True(Toggle().IsChecked);
            view.ViewMenu.Close();

            panels.Hide(BuiltInPanels.AssetLog);
            await WindowTestHelper.PumpAsync();
            view.ViewMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.False(Toggle().IsChecked);
            view.ViewMenu.Close();

            // The item runs the panel's command: the panel shows
            Toggle().Command!.Execute(null);
            await WindowTestHelper.WaitUntilAsync(() => panels.IsVisible(BuiltInPanels.AssetLog), "the asset log");
            view.ViewMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.True(Toggle().IsChecked);
            view.ViewMenu.Close();
            await WindowTestHelper.PumpAsync();

            // Every panel opened once has its command; the View menu ends with the palette
            foreach (var panel in panels.Registry.Panels.Where(x => !x.AllowsMultipleInstances))
                Assert.Contains(fixture.Plugins.Commands.Items, x => x.Id == PanelCommands.IdOf(panel.Id) && x.IsChecked is not null);
            Assert.Equal(GameStudioCommands.CommandPaletteId, Assert.IsType<CommandReference>(view.ViewMenu.Items.OfType<MenuItem>().Last().Command).Id);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheShortcutsPageShowsTheConflictsRefusesAPlainKeyAndResets()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var key = GameStudioEditorSettings.ShortcutOverrides;
        var profile = EditorSettings.SettingsContainer.CurrentProfile;
        try
        {
            key.SetValue(new Dictionary<string, string>());
            var page = EditorDialogService.CreateShortcutsPage(fixture.ServiceProvider)!;
            var view = new KeyboardShortcutsView { DataContext = page };
            var host = new Window { Content = view, Width = 800, Height = 600 };
            await host.ShowAsync();
            try
            {
                var save = page.Find(GameStudioCommands.SaveId)!;
                var add = page.Find(SessionCommands.AddAssetId)!;
                Assert.Equal("Ctrl+S", save.Gesture);
                Assert.Equal("Ctrl+I", add.Gesture);
                Assert.False(save.HasConflict);

                // The recorder refuses a plain key, and says why
                page.SelectedShortcut = save;
                Assert.NotNull(view.Record(Key.K, KeyModifiers.None));
                Assert.True(view.RecorderMessage.IsVisible);
                Assert.Equal("Ctrl+S", save.Gesture);

                // Save takes Add asset's shortcut: the user's shortcut wins, the conflict shows on both lines
                Assert.Null(view.Record(Key.I, KeyModifiers.Control));
                Assert.Equal("Ctrl+I", save.Gesture);
                Assert.True(save.IsUserDefined);
                Assert.Null(add.Gesture);
                Assert.Contains("Save", add.Conflict);
                Assert.Contains("Add asset", save.Conflict);
                Assert.Contains(page.Conflicts, x => x.Command.Id == SessionCommands.AddAssetId && x.Winner?.Id == GameStudioCommands.SaveId);
                Assert.Equal("Ctrl+I", key.GetValue()[GameStudioCommands.SaveId]);

                // The search finds the lines by shortcut too
                page.SearchText = "Ctrl+I";
                Assert.Contains(save, page.Shortcuts);

                // None, then Reset, then Reset all
                page.SetNone(add);
                Assert.Null(add.Gesture);
                Assert.Equal(CommandGestureBindings.NoGesture, key.GetValue()[SessionCommands.AddAssetId]);
                page.Reset(save);
                Assert.Equal("Ctrl+S", save.Gesture);
                Assert.False(save.HasConflict);
                Assert.True(page.HasOverrides);
                page.ResetAll();
                Assert.Equal("Ctrl+I", add.Gesture);
                Assert.False(page.HasOverrides);
                Assert.Empty(key.GetValue());

                // Saved with the settings, the main window binds the user's shortcut
                page.SetGesture(save, "Ctrl+Alt+S");
                profile.ValidateSettingsChanges();
                await WindowTestHelper.WaitUntilAsync(() => Equals(MenuItem(window, "SaveMenuItem").InputGesture, KeyGesture.Parse("Ctrl+Alt+S")), "Save's new shortcut in its menu item");
            }
            finally
            {
                await host.CloseAsync();
            }
        }
        finally
        {
            key.SetValue(new Dictionary<string, string>());
            profile.ValidateSettingsChanges();
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheSettingsWindowHasTheShortcutsPageWhileCommandsAreRegistered()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new SettingsWindow
        {
            DataContext = new Core.Assets.Editor.ViewModels.SettingsViewModel(fixture.ServiceProvider, EditorSettings.SettingsContainer.CurrentProfile),
            Shortcuts = EditorDialogService.CreateShortcutsPage(fixture.ServiceProvider),
        };
        await window.ShowAsync();
        try
        {
            var page = window.GetLogicalDescendants().OfType<TabItem>().Single(x => x.Name == "ShortcutsPage");
            Assert.True(page.IsVisible);
            window.ShowShortcutsPage();
            Assert.True(page.IsSelected);
            Assert.NotEmpty(window.Shortcuts!.Shortcuts);
            Assert.Contains(window.Shortcuts.Shortcuts, x => x.Id == GameStudioCommands.KeyboardShortcutsId);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static IEnumerable<MenuItem> Flatten(MenuItem menu) => menu.Items.OfType<MenuItem>().SelectMany(Flatten).Prepend(menu);

    private static MenuItem MenuItem(Window window, string name) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == name);

    private static Button Button(Window window, string name) => window.GetVisualDescendants().OfType<Button>().Single(x => x.Name == name);

    private static void Press(TopLevel topLevel, PhysicalKey key, RawInputModifiers modifiers)
    {
        topLevel.KeyPressQwerty(key, modifiers);
        topLevel.KeyReleaseQwerty(key, modifiers);
    }

    // Whether the window handled the key (a shortcut ran)
    private static bool PressHandled(Window window, PhysicalKey key, RawInputModifiers modifiers)
    {
        var handled = false;
        void Probe(object? sender, KeyEventArgs e) => handled |= e.Handled;
        window.AddHandler(InputElement.KeyDownEvent, Probe, RoutingStrategies.Bubble, handledEventsToo: true);
        try
        {
            Press(window, key, modifiers);
        }
        finally
        {
            window.RemoveHandler(InputElement.KeyDownEvent, Probe);
        }
        return handled;
    }

    /// <summary>
    /// Whether the item is enabled when the user opens its menu.
    /// </summary>
    private static async Task<bool> IsEnabledInOpenMenuAsync(MenuItem item)
    {
        var menu = Assert.IsType<MenuItem>(item.Parent);
        menu.Open();
        await WindowTestHelper.PumpAsync();
        try
        {
            return item.IsEffectivelyEnabled;
        }
        finally
        {
            menu.Close();
            await WindowTestHelper.PumpAsync();
        }
    }
}
