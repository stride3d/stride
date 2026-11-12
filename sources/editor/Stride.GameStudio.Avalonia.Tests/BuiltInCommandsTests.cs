// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The editor's own commands in the command registry: the main menu and the main window's shortcuts, registered once by the
/// editor's plugins, with the shortcuts the window binds; and a shortcut scoped to an editor.
/// </summary>
public sealed class BuiltInCommandsTests
{
    private const string SheetLocation = "Sheet";

    private static IEnumerable<string> IdsOf(Type type) => type.GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(x => x.IsLiteral && x.Name.EndsWith("Id", StringComparison.Ordinal))
        .Select(x => (string)x.GetRawConstantValue()!);

    [AvaloniaFact]
    public async Task TheMainMenuAndTheWindowShortcutsAreTheEditorsCommandsRegisteredOnce()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var host = fixture.Plugins;
            var builtIns = IdsOf(typeof(GameStudioCommands)).Concat(IdsOf(typeof(SessionCommands))).ToList();
            Assert.Equal(26 + 9, builtIns.Count);
            Assert.Equal(builtIns.Count, builtIns.Distinct().Count());
            foreach (var id in builtIns)
            {
                var command = Assert.Single(host.Commands.Items, x => x.Id == id);
                Assert.True(host.IsBuiltInCommand(command), id);
                Assert.NotNull(command.Category);
                Assert.True(command.ShowInPalette, id);
                // The main menu shows them from its own items
                Assert.Empty(command.Placements);
            }

            // No conflict among them, and none with the plugins of the session
            Assert.Empty(host.ResolveCommandGestures().Conflicts);

            // Each shortcut the window binds and each one the main menu shows is the shortcut of one of them, so that no
            // plugin can take it
            var shortcuts = window.View.CommandSlots!.Shortcuts;
            var gestures = window.KeyBindings.Select(x => x.Gesture!)
                .Concat(window.View.MainMenu.Items.OfType<MenuItem>().SelectMany(Flatten).Where(x => !CommandSlot.IsSlotItem(x)).Select(x => x.InputGesture).OfType<KeyGesture>())
                .ToList();
            Assert.True(gestures.Count >= 20, $"Only {gestures.Count} shortcuts");
            var unbound = gestures.Where(gesture => !shortcuts.Any(x => x.Gesture.Equals(gesture) && builtIns.Contains(x.Command.Descriptor.Id))).Select(x => x.ToString()).ToList();
            Assert.Empty(unbound);
            Assert.All(gestures, gesture => Assert.Single(shortcuts, x => x.Gesture.Equals(gesture)));
        }
        finally
        {
            await window.CloseAsync();
        }

        static IEnumerable<MenuItem> Flatten(MenuItem menu) => menu.Items.OfType<MenuItem>().SelectMany(Flatten).Prepend(menu);
    }

    [AvaloniaFact]
    public async Task TheEditorsCommandsRunTheCommandsOfTheMainViewModelAndTheSession()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var host = fixture.Plugins;
        var context = new CommandContext { Session = fixture.Session, Location = CommandLocations.Palette };
        CommandDescriptor Command(string id) => host.Commands.Items.Single(x => x.Id == id);

        // As their menu items: Save while a session is open, Cancel build only during a build
        Assert.True(host.CanExecuteCommand(Command(GameStudioCommands.SaveId), context));
        Assert.False(host.CanExecuteCommand(Command(GameStudioCommands.CancelBuildId), context));
        Assert.Equal(fixture.Main.Build.BuildProjectCommand.CanExecute(null), host.CanExecuteCommand(Command(GameStudioCommands.BuildProjectId), context));
        Assert.Equal(fixture.Session.AddDependencyCommand.CanExecute(null), host.CanExecuteCommand(Command(SessionCommands.AddDependencyId), context));

        // The asset view's Ctrl+Shift+C acts on the selected assets: nothing to copy without a selection
        fixture.Session.AssetCollection.SelectAssets([]);
        Assert.Equal(fixture.Session.AssetCollection.CopyAssetsRecursivelyCommand.CanExecute(null), host.CanExecuteCommand(Command(SessionCommands.CopyAssetsRecursivelyId), context));

        Assert.Equal(fixture.Main.ViewMenu.ResetLayoutCommand.CanExecute(null), host.CanExecuteCommand(Command(GameStudioCommands.ResetLayoutId), context));

        // Clear the recent list runs the main view model's command
        fixture.Main.ClearRecentFilesCommand.Execute();
        fixture.Main.AddRecentFile(fixture.PackagePath);
        Assert.NotEmpty(fixture.Main.RecentFiles);
        Assert.True(await host.ExecuteCommandAsync(Command(GameStudioCommands.ClearRecentId), context));
        Assert.Empty(fixture.Main.RecentFiles);
    }

    [AvaloniaFact]
    public async Task AShortcutOfAnEditorWorksOnlyInThatEditorAndGoesBeforeTheMainWindows()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var runs = new List<string>();
            var plugin = new CommandsTestPlugin(context =>
            {
                context.Commands.Register(new CommandDescriptor
                {
                    Id = "Tests.InSpriteEditor",
                    Title = () => "In the sprite editor",
                    DefaultGesture = "Ctrl+Alt+K",
                    GestureScope = typeof(SpriteSheetEditorViewModel),
                    ExecuteAsync = _ =>
                    {
                        runs.Add("editor");
                        return Task.CompletedTask;
                    },
                });
                context.Commands.Register(new CommandDescriptor
                {
                    Id = "Tests.InMainWindow",
                    Title = () => "In the main window",
                    DefaultGesture = "Ctrl+Alt+K",
                    ExecuteAsync = _ =>
                    {
                        runs.Add("main");
                        return Task.CompletedTask;
                    },
                });
            });
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();

            // Outside of the editor: the main window's
            var sheet = fixture.Asset(SheetLocation);
            fixture.Session.AssetCollection.SelectAssets([sheet]);
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Any(), "the sprite editor");
            var assetView = window.GetVisualDescendants().OfType<AssetExplorerView>().Single();
            var assetList = assetView.GetVisualDescendants().OfType<Control>().First(x => x.Focusable && x.IsEffectivelyVisible);
            Assert.True(assetList.Focus());
            PressCtrlAltK(window);
            Assert.Equal(["main"], runs);

            // In the editor: the editor's
            var editorView = window.GetVisualDescendants().OfType<SpriteSheetEditorView>().Single();
            var list = editorView.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "SpriteList");
            Assert.True(list.Focus());
            PressCtrlAltK(window);
            Assert.Equal(["main", "editor"], runs);

            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            PressCtrlAltK(window);
            Assert.Equal(2, runs.Count);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheUsersShortcutsRebindOrRemoveTheShortcutsOfCommands()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var overrides = GameStudioEditorSettings.ShortcutOverrides;
        var runs = new List<string>();
        var plugin = new CommandsTestPlugin(context =>
        {
            foreach (var (id, gesture) in new[] { ("Tests.Moved", "Ctrl+Alt+K"), ("Tests.Removed", "Ctrl+Alt+J") })
            {
                context.Commands.Register(new CommandDescriptor
                {
                    Id = id,
                    Title = () => id,
                    DefaultGesture = gesture,
                    ExecuteAsync = _ =>
                    {
                        runs.Add(id);
                        return Task.CompletedTask;
                    },
                });
            }
        });
        try
        {
            overrides.SetValue(new Dictionary<string, string>
            {
                ["Tests.Moved"] = "Ctrl+Alt+L",
                ["Tests.Removed"] = "None",
                [GameStudioCommands.SaveId] = "Ctrl+Alt+S",
            });
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();

            var shortcuts = window.View.CommandSlots!.Shortcuts;
            string? GestureOf(string id) => shortcuts.SingleOrDefault(x => x.Command.Descriptor.Id == id).Gesture?.ToString();
            Assert.Equal("Ctrl+Alt+L", GestureOf("Tests.Moved"));
            Assert.Null(GestureOf("Tests.Removed"));
            Assert.Equal("Ctrl+Alt+S", GestureOf(GameStudioCommands.SaveId));

            window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Alt);
            window.KeyPressQwerty(PhysicalKey.J, RawInputModifiers.Control | RawInputModifiers.Alt);
            Assert.Empty(runs);
            window.KeyPressQwerty(PhysicalKey.L, RawInputModifiers.Control | RawInputModifiers.Alt);
            Assert.Equal(["Tests.Moved"], runs);

            // Back to the defaults
            overrides.SetValue(new Dictionary<string, string>());
            window.View.CommandSlots.Update();
            Assert.Equal("Ctrl+Alt+K", window.View.CommandSlots.Shortcuts.Single(x => x.Command.Descriptor.Id == "Tests.Moved").Gesture.ToString());
        }
        finally
        {
            overrides.SetValue(new Dictionary<string, string>());
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            await window.CloseAsync();
        }
    }

    private static void PressCtrlAltK(TopLevel topLevel)
    {
        topLevel.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Alt);
        topLevel.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Alt);
    }
}
