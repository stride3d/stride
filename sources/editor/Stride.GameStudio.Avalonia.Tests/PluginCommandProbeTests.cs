// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Presentation.Commands;
using Stride.GameStudio.Avalonia.Docking;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The two toolkit questions of the plugin command slots (risks R1 and R4 of the Phase 4 study), probed with Avalonia alone
/// before the slots are built on them: a menu item can be inserted between the items that a menu declares in XAML and
/// removed again, and a key binding added to the main window at run time works in a floating window.
/// </summary>
public sealed class PluginCommandProbeTests
{
    [AvaloniaFact]
    public async Task AnItemGoesInAndOutOfTheFileMenuBetweenItsXamlItems()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var menu = window.View.GetLogicalDescendants().OfType<Menu>().First();
            var file = (MenuItem)menu.Items[0]!;
            var xamlItems = file.Items.Cast<object>().ToList();
            var exit = (MenuItem)xamlItems[^1];
            var runs = 0;
            var probe = new MenuItem { Header = "Probe", Command = new AnonymousCommand(fixture.ServiceProvider, () => ++runs) };
            var separator = new Separator();

            // Before Exit: between the XAML items, while the menu items keep their order
            file.Items.Insert(file.Items.Count - 1, separator);
            file.Items.Insert(file.Items.Count - 1, probe);
            object[] expected = [.. xamlItems.Take(xamlItems.Count - 1), separator, probe, exit];
            Assert.Equal(expected, file.Items.Cast<object>());
            file.Open();
            await WindowTestHelper.PumpAsync();
            Assert.True(probe.IsEffectivelyVisible);
            Assert.NotNull(probe.GetVisualParent());
            probe.Command!.Execute(null);
            Assert.Equal(1, runs);
            file.Close();
            await WindowTestHelper.PumpAsync();

            // Out again: the XAML items are as they were, and the menu opens without it
            file.Items.Remove(probe);
            file.Items.Remove(separator);
            Assert.Equal(xamlItems, file.Items.Cast<object>());
            file.Open();
            await WindowTestHelper.PumpAsync();
            Assert.Null(probe.GetVisualParent());
            Assert.True(exit.IsEffectivelyVisible);
            file.Close();
            await WindowTestHelper.PumpAsync();
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AKeyBindingAddedToTheMainWindowAtRunTimeWorksInAFloatingWindow()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var runs = 0;
            var binding = new KeyBinding { Gesture = KeyGesture.Parse("Ctrl+Shift+K"), Command = new AnonymousCommand(fixture.ServiceProvider, () => ++runs) };
            window.KeyBindings.Add(binding);

            // In the main window
            window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            window.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Equal(1, runs);

            // In a floating window of panels
            var panels = fixture.Main.Panels;
            panels.Factory.FloatDockable(panels.FindDockable(BuiltInPanels.SolutionExplorer)!);
            await WindowTestHelper.PumpAsync();
            var host = Assert.IsType<StrideHostWindow>(Assert.Single(panels.Layout!.Windows!).Host);
            host.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            host.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Equal(2, runs);

            // Removed at run time: gone from both
            window.KeyBindings.Remove(binding);
            host.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            host.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            window.KeyReleaseQwerty(PhysicalKey.K, RawInputModifiers.Control | RawInputModifiers.Shift);
            Assert.Equal(2, runs);
        }
        finally
        {
            await window.CloseAsync();
        }
    }
}
