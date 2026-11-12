// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Commands;
using Stride.Assets.Sprite;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The commands and toolbar widgets of a plugin in the Avalonia shell (<see cref="MainCommandSlots"/>, <see cref="CommandSlot"/>):
/// the main menus, the Tools menu, the toolbar, the context menus and the shortcuts.
/// </summary>
public sealed class PluginCommandsTests
{
    private const string SheetLocation = "Sheet";

    [AvaloniaFact]
    public async Task TheCommandsOfAPluginComeAndGoInTheMenusTheToolbarAndTheShortcuts()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var view = window.View;
            var xamlFile = await DescribeMenuAsync(view.FileMenu);
            var runs = new List<CommandContext>();
            TestWidgetViewModel? widget = null;
            var plugin = new CommandsTestPlugin(context =>
            {
                // Two groups in the File menu: the first by order, then registration
                context.Commands.Register(Command("Tests.FileA", "File A", runs, CommandLocations.MainMenuFile, "Tests.First", order: 1));
                context.Commands.Register(Command("Tests.FileB", "File B", runs, CommandLocations.MainMenuFile, "Tests.First", order: 0));
                context.Commands.Register(Command("Tests.FileC", "File C", runs, CommandLocations.MainMenuFile, "Tests.Second", order: 5, gesture: "Ctrl+Alt+K"));
                context.Commands.Register(Command("Tests.Tools", "Tool", runs, CommandLocations.MainMenuTools));
                context.Commands.Register(Command("Tests.Toolbar", "Toolbar", runs, CommandLocations.MainToolbar));
                context.ToolbarWidgets.Register(new ToolbarWidgetDescriptor { Id = "Tests.Widget", CreateViewModel = _ => widget = new TestWidgetViewModel() });
                context.ViewTypes.Register(typeof(TestWidgetViewModel), typeof(TestWidgetView));
            });

            // Before the plugin: no Tools menu
            Assert.False(view.ToolsMenu.IsVisible);

            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();

            // At the end of the File menu, before Exit, in their groups
            List<string> expected = [.. xamlFile.Take(xamlFile.Count - 2), "-", "File B", "File A", "-", "File C", .. xamlFile.TakeLast(2)];
            Assert.Equal(expected, await DescribeMenuAsync(view.FileMenu));
            // The Tools menu shows with its command
            Assert.True(view.ToolsMenu.IsVisible);
            Assert.Equal(["Tool"], await DescribeMenuAsync(view.ToolsMenu));
            // The toolbar: the button after the build buttons, then the widget with the view its plugin registered
            var button = Assert.Single(view.MainToolBar.Items.OfType<Button>(), x => Equals(x.Tag, "Tests.Toolbar"));
            Assert.True(view.MainToolBar.Items.IndexOf(button) > view.MainToolBar.Items.IndexOf(view.ReloadGameCodeButton));
            Assert.Equal("Toolbar", ToolTip.GetTip(button));
            Assert.NotNull(widget);
            Assert.Same(widget, Assert.Single(view.MainToolBar.Items.OfType<TestWidgetView>()).DataContext);

            // Each runs in the context of where it is
            Assert.True(button.Command!.CanExecute(null));
            button.Command.Execute(null);
            await MenuItemOf(view.FileMenu, "Tests.FileA").ExecuteAsync();
            await MenuItemOf(view.ToolsMenu, "Tests.Tools").ExecuteAsync();
            Assert.Equal([CommandLocations.MainToolbar, CommandLocations.MainMenuFile, CommandLocations.MainMenuTools], runs.Select(x => x.Location));
            Assert.All(runs, x => Assert.Same(fixture.Session, x.Session));

            // Its shortcut, which its menu item shows
            Assert.Equal(KeyGesture.Parse("Ctrl+Alt+K"), view.FileMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "Tests.FileC")).InputGesture);
            runs.Clear();
            PressCtrlAltK(window);
            Assert.Equal("Tests.FileC", Assert.Single(runs).Selection.Single());
            Assert.Equal(CommandLocations.Shortcut, runs[0].Location);

            // The plugin leaves: its entries, its Tools menu, its button and its shortcut go; its widget is destroyed
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.Equal(xamlFile, await DescribeMenuAsync(view.FileMenu));
            Assert.False(view.ToolsMenu.IsVisible);
            Assert.DoesNotContain(view.MainToolBar.Items.OfType<Control>(), x => x.Tag is string tag && tag.StartsWith("Tests.", StringComparison.Ordinal));
            Assert.Empty(view.MainToolBar.Items.OfType<TestWidgetView>());
            Assert.True(widget!.WasDestroyed);
            runs.Clear();
            PressCtrlAltK(window);
            Assert.Empty(runs);

            // It comes back
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.Equal(expected, await DescribeMenuAsync(view.FileMenu));
            Assert.True(view.ToolsMenu.IsVisible);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ACommandThatFailsLeavesTheMenusAloneAndThePluginStays()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var messages = new List<ILogMessage>();
        GlobalLogger.GlobalMessageLogged += messages.Add;
        try
        {
            var view = window.View;
            var runs = new List<CommandContext>();
            var plugin = new CommandsTestPlugin(context =>
            {
                context.Commands.Register(new CommandDescriptor
                {
                    Id = "Tests.Fails",
                    Title = () => "Fails",
                    ExecuteAsync = _ => throw new InvalidOperationException("The command failed"),
                    Placements = [new CommandPlacement(CommandLocations.MainMenuHelp)],
                });
                context.Commands.Register(Command("Tests.Works", "Works", runs, CommandLocations.MainMenuHelp));
            });
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.Contains("Fails", await DescribeMenuAsync(view.HelpMenu));

            Assert.False(await MenuItemOf(view.HelpMenu, "Tests.Fails").ExecuteAsync());
            await WindowTestHelper.PumpAsync();

            // Turned off: out of the menu, logged with its plugin and recorded; the other command stays and works
            var help = await DescribeMenuAsync(view.HelpMenu);
            Assert.DoesNotContain("Fails", help);
            Assert.Contains("Works", help);
            Assert.True(await MenuItemOf(view.HelpMenu, "Tests.Works").ExecuteAsync());
            Assert.Single(runs);
            var status = fixture.Plugins.GetPluginStatus(plugin)!;
            Assert.Contains(PluginContributions.Command("Tests.Fails"), status.TurnedOffContributions);
            Assert.Contains(messages, x => x.Type == LogMessageType.Error && x.Text.Contains("Tests.Fails") && x.Text.Contains(typeof(CommandsTestPlugin).FullName!));
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= messages.Add;
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AToggleShowsItsStateInItsMenuAndThePaletteGivesTheSelectedAssets()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var view = window.View;
            var on = true;
            var plugin = new CommandsTestPlugin(context => context.Commands.Register(new CommandDescriptor
            {
                Id = "Tests.Toggle",
                Title = () => "Toggle",
                IsChecked = _ => on,
                ExecuteAsync = _ =>
                {
                    on = !on;
                    return Task.CompletedTask;
                },
                Placements = [new CommandPlacement(CommandLocations.MainMenuTools)],
            }));
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();

            view.ToolsMenu.Open();
            await WindowTestHelper.PumpAsync();
            var item = view.ToolsMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "Tests.Toggle"));
            Assert.Equal(MenuItemToggleType.CheckBox, item.ToggleType);
            Assert.True(item.IsChecked);
            view.ToolsMenu.Close();
            await WindowTestHelper.PumpAsync();

            Assert.True(await MenuItemOf(view.ToolsMenu, "Tests.Toggle").ExecuteAsync());
            view.ToolsMenu.Open();
            await WindowTestHelper.PumpAsync();
            Assert.False(view.ToolsMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, "Tests.Toggle")).IsChecked);
            view.ToolsMenu.Close();
            await WindowTestHelper.PumpAsync();

            // The palette's context: the selected assets of the asset view, unless the shell gives a selection
            var sheet = fixture.Asset(SheetLocation);
            fixture.Session.AssetCollection.SelectAssetCommand.Execute(sheet);
            await WindowTestHelper.PumpAsync();
            var palette = CommandContext.ForPalette(fixture.Session);
            Assert.Equal(CommandLocations.Palette, palette.Location);
            Assert.Same(sheet, Assert.Single(palette.Selection));
            Assert.Equal(["Other"], CommandContext.ForPalette(fixture.Session, ["Other"]).Selection);

            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheContextMenusGiveTheirSelectionToTheCommands()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var runs = new List<CommandContext>();
            var plugin = new CommandsTestPlugin(context =>
            {
                context.Commands.Register(Command("Tests.Asset", "On assets", runs, CommandLocations.AssetViewContext));
                context.Commands.Register(Command("Tests.Solution", "On folders", runs, CommandLocations.SolutionExplorerContext));
                context.Commands.Register(Command("Tests.Property", "On a property", runs, CommandLocations.PropertyGridContext));
            });
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();
            var asset = fixture.Asset(SheetLocation);

            // The asset view: the selected assets, after the menu's own items
            fixture.Session.AssetCollection.SelectAssets([asset]);
            await WindowTestHelper.PumpAsync();
            var list = window.GetVisualDescendants().OfType<ListBox>().Single(x => x.Name == "AssetList");
            var items = await OpenContextMenuAsync(list.ContextMenu!, list);
            Assert.Equal(["-", "On assets"], Describe(items.TakeLast(2)));
            await RunAsync(list.ContextMenu!, "Tests.Asset");
            Assert.Equal([asset], runs[^1].Selection);
            Assert.Equal(CommandLocations.AssetViewContext, runs[^1].Location);

            // A property of the grid: its node, and the item it opened on
            var property = await ShowPropertyAsync(window, nameof(TextPropertiesAsset.Label));
            items = await OpenContextMenuAsync(property.ContextMenu!, property);
            Assert.Contains("On a property", Describe(items));
            await RunAsync(property.ContextMenu!, "Tests.Property");
            Assert.Same(property.DataContext, Assert.Single(runs[^1].Selection));
            Assert.Same(property, runs[^1].Source);

            // The solution explorer: the selected folders
            var folder = asset.Directory;
            var locations = fixture.Session.AssetCollection.SelectedLocations;
            locations.Clear();
            locations.Add(folder);
            await WindowTestHelper.PumpAsync();
            var tree = window.GetVisualDescendants().OfType<TreeView>().Single(x => x.Name == "SolutionTree");
            items = await OpenContextMenuAsync(tree.ContextMenu!, tree);
            Assert.Equal("On folders", Describe(items).Last());
            await RunAsync(tree.ContextMenu!, "Tests.Solution");
            Assert.Equal([folder], runs[^1].Selection);

            // The plugin leaves: the next opening has none of its entries
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.AssetCollection.SelectAssets([asset]);
            await WindowTestHelper.PumpAsync();
            items = await OpenContextMenuAsync(list.ContextMenu!, list);
            Assert.DoesNotContain("On assets", Describe(items));
            Assert.NotEqual("-", Describe(items).Last());
            list.ContextMenu!.Close();
            await WindowTestHelper.PumpAsync();
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheEntityTreeMenuGivesTheSelectedEntitiesToTheCommands()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var fixture = scene.Fixture;
        var runs = new List<CommandContext>();
        var plugin = new CommandsTestPlugin(context => context.Commands.Register(Command("Tests.Entity", "On entities", runs, SceneLocations.EntityTreeContext)));
        fixture.Plugins.PluginsDiscovered([plugin]);
        await WindowTestHelper.PumpAsync();
        try
        {
            var box = scene.Entity("Box");
            scene.Editor.SelectedContent.Clear();
            scene.Editor.SelectedContent.Add(box);
            await WindowTestHelper.PumpAsync();

            // On an entity: the item menu, with the selected entities
            scene.Row("Box").RaiseEvent(new ContextRequestedEventArgs());
            await WindowTestHelper.PumpAsync();
            var menu = (ContextMenu)scene.View.Resources["ItemContextMenu"]!;
            Assert.True(menu.IsOpen);
            Assert.Equal("On entities", Describe(menu.Items).Last());
            await RunAsync(menu, "Tests.Entity");
            Assert.Equal([box], runs[^1].Selection);
            Assert.Equal(SceneLocations.EntityTreeContext, runs[^1].Location);
        }
        finally
        {
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
        }
    }

    [AvaloniaFact]
    public async Task AShortcutWorksInAFloatingWindowAndOnlyIfTheEditorDoesNotUseIt()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((SheetLocation, new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        var messages = new List<ILogMessage>();
        GlobalLogger.GlobalMessageLogged += messages.Add;
        try
        {
            var runs = new List<CommandContext>();
            var plugin = new CommandsTestPlugin(context =>
            {
                // No placement: reached by its shortcut only
                context.Commands.Register(Command("Tests.Shortcut", "Shortcut", runs, location: null, gesture: "Ctrl+Alt+K"));
                // Save's shortcut: the editor keeps it
                context.Commands.Register(Command("Tests.Save", "Not save", runs, location: null, gesture: "Ctrl+S"));
            });
            fixture.Plugins.PluginsDiscovered([plugin]);
            await WindowTestHelper.PumpAsync();
            Assert.Contains(messages, x => x.Type == LogMessageType.Warning && x.Text.Contains("Tests.Save") && x.Text.Contains("Ctrl+S"));
            var bound = window.View.CommandSlots!.Shortcuts.Select(x => x.Descriptor.Id).ToList();
            Assert.Contains("Tests.Shortcut", bound);
            Assert.DoesNotContain("Tests.Save", bound);
            // Save keeps it: the editor's own command
            Assert.Equal("Stride.File.Save", window.View.CommandSlots.Shortcuts.Single(x => x.Gesture.Equals(KeyGesture.Parse("Ctrl+S"))).Descriptor.Id);

            var panels = fixture.Main.Panels;
            panels.Factory.FloatDockable(panels.FindDockable(BuiltInPanels.SolutionExplorer)!);
            await WindowTestHelper.PumpAsync();
            var host = Assert.IsType<StrideHostWindow>(Assert.Single(panels.Layout!.Windows!).Host);
            PressCtrlAltK(host);
            Assert.Equal("Tests.Shortcut", Assert.Single(runs).Selection.Single());

            // The plugin leaves: its shortcut does nothing
            fixture.Plugins.PluginsRemoved([plugin]);
            await WindowTestHelper.PumpAsync();
            PressCtrlAltK(host);
            Assert.Single(runs);
        }
        finally
        {
            GlobalLogger.GlobalMessageLogged -= messages.Add;
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheNestedPropertiesToggleOfThePropertyMenuExpandsAndCollapses()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Nested", new NestedPropertiesAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            fixture.Session.AssetCollection.SelectAssets([fixture.Asset("Nested")]);
            var outer = await ShowPropertyAsync(window, nameof(NestedPropertiesAsset.Outer));
            outer.IsExpanded = false;
            await WindowTestHelper.PumpAsync();

            // Expand: the property and everything under it
            var items = await OpenContextMenuAsync(outer.ContextMenu!, outer);
            Assert.Equal("Expand all nested properties", Describe(items).Last());
            await RunAsync(outer.ContextMenu!, PropertyGridCommands.ToggleNestedPropertiesId);
            await WindowTestHelper.PumpAsync();
            Assert.True(outer.IsExpanded);
            var inner = outer.GetVisualDescendants().OfType<PropertyViewItem>().Single(x => x.DataContext is NodeViewModel { Name: nameof(NestedPropertiesOuter.Inner) });
            Assert.True(inner.IsExpanded);

            // Collapse: the menu says so now
            items = await OpenContextMenuAsync(outer.ContextMenu!, outer);
            Assert.Equal("Collapse all nested properties", Describe(items).Last());
            await RunAsync(outer.ContextMenu!, PropertyGridCommands.ToggleNestedPropertiesId);
            await WindowTestHelper.PumpAsync();
            Assert.False(inner.IsExpanded);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static CommandDescriptor Command(string id, string title, List<CommandContext> runs, string? location, string? group = null, int order = 0, string? gesture = null)
    {
        return new CommandDescriptor
        {
            Id = id,
            Title = () => title,
            DefaultGesture = gesture,
            ExecuteAsync = context =>
            {
                // A shortcut has no selection: the test records which command ran
                runs.Add(context.Location == CommandLocations.Shortcut ? new CommandContext { Session = context.Session, Location = context.Location, Selection = [id] } : context);
                return Task.CompletedTask;
            },
            Placements = location is null ? [] : [new CommandPlacement(location, group, order)],
        };
    }

    private static void PressCtrlAltK(TopLevel topLevel)
    {
        topLevel.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Alt);
        topLevel.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Alt);
    }

    /// <summary>
    /// Opens a main menu (which fills its slot), and gives its visible items.
    /// </summary>
    private static async Task<List<string>> DescribeMenuAsync(MenuItem menu)
    {
        menu.Open();
        await WindowTestHelper.PumpAsync();
        try
        {
            return Describe(menu.Items);
        }
        finally
        {
            menu.Close();
            await WindowTestHelper.PumpAsync();
        }
    }

    private static DescriptorCommand MenuItemOf(MenuItem menu, string id)
    {
        var item = menu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, id));
        return Assert.IsType<DescriptorCommand>(item.Command);
    }

    /// <summary>
    /// Opens a context menu the way a right-click does (its Opening event fills its slot), and gives its visible items.
    /// </summary>
    private static async Task<List<Control>> OpenContextMenuAsync(ContextMenu menu, Control control)
    {
        control.RaiseEvent(new ContextRequestedEventArgs());
        await WindowTestHelper.PumpAsync();
        Assert.True(menu.IsOpen);
        return menu.Items.OfType<Control>().Where(x => x.IsVisible).ToList();
    }

    /// <summary>
    /// Runs the command of an open context menu, and closes it.
    /// </summary>
    private static async Task RunAsync(ContextMenu menu, string id)
    {
        var command = Assert.IsType<DescriptorCommand>(menu.Items.OfType<MenuItem>().Single(x => Equals(x.Tag, id)).Command);
        Assert.True(command.CanExecute(null));
        menu.Close();
        await WindowTestHelper.PumpAsync();
        Assert.True(await command.ExecuteAsync());
    }

    private static List<string> Describe(IEnumerable<object?> items)
    {
        return items.OfType<Control>().Where(x => x.IsVisible).Select(x => x is MenuItem item ? item.Header?.ToString() ?? "" : "-").ToList();
    }

    private static async Task<PropertyViewItem> ShowPropertyAsync(MainWindow window, string member)
    {
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        PropertyViewItem? item = null;
        await WindowTestHelper.WaitUntilAsync(() => (item = grid.GetVisualDescendants().OfType<PropertyViewItem>().FirstOrDefault(x => x.DataContext is NodeViewModel node && node.Name == member)) is not null, $"the property {member}");
        // and settle: a second generation would replace the item
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return item!;
    }
}

/// <summary>
/// A plugin that registers what a test gives it. It has no parameterless constructor, so that the editor does not load it
/// with the other plugins of this assembly: a test joins it to the session.
/// </summary>
public sealed class CommandsTestPlugin(Action<IEditorPluginContext> register) : AssetsPlugin
{
    public override void Initialize(IEditorPluginContext context)
    {
        base.Initialize(context);
        register(context);
    }
}

/// <summary>
/// The view model of the toolbar widget of <see cref="PluginCommandsTests"/>.
/// </summary>
public sealed class TestWidgetViewModel : IDestroyable
{
    public bool WasDestroyed { get; private set; }

    public void Destroy() => WasDestroyed = true;
}

/// <summary>
/// The view of <see cref="TestWidgetViewModel"/>.
/// </summary>
public sealed class TestWidgetView : TextBlock
{
    public TestWidgetView()
    {
        Text = "Widget";
    }
}
