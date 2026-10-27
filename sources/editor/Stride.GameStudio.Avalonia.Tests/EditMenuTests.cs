// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Edit menu of the shell and its shortcuts.
/// </summary>
public sealed class EditMenuTests
{
    private const string AssetLocation = "Text";

    [AvaloniaFact]
    public async Task UndoAndRedoFollowTheHistoryOfTheSession()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var undo = MenuItem(window, "UndoMenuItem");
            var redo = MenuItem(window, "RedoMenuItem");
            Assert.Equal(new KeyGesture(Key.Z, KeyModifiers.Control), undo.InputGesture);
            Assert.Equal(new KeyGesture(Key.Y, KeyModifiers.Control), redo.InputGesture);
            Assert.False(await IsEnabledInOpenMenuAsync(undo));

            Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
            await WindowTestHelper.PumpAsync();
            Assert.False(await IsEnabledInOpenMenuAsync(undo));
            Assert.False(await IsEnabledInOpenMenuAsync(redo));

            Edit(fixture, fixture.Asset(AssetLocation), "edited");
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanUndo, "the history");
            Assert.True(await IsEnabledInOpenMenuAsync(undo));
            Assert.False(await IsEnabledInOpenMenuAsync(redo));

            fixture.Main.UndoCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanRedo, "the history");
            Assert.False(await IsEnabledInOpenMenuAsync(undo));
            Assert.True(await IsEnabledInOpenMenuAsync(redo));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// The shortcuts work before the Edit menu has been opened once: the hot key of an item of a closed menu fires only if the
    /// item was enabled when the menu last opened, so the shortcuts are handled by the window.
    /// </summary>
    [AvaloniaFact]
    public async Task CtrlZUndoesWithoutOpeningTheEditMenu()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            Edit(fixture, asset, "edited");
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanUndo, "the history");

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            window.KeyReleaseQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Equal("label", asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Retrieve());
            // The history enables Redo asynchronously after the undo
            await WindowTestHelper.WaitUntilAsync(() => fixture.Main.RedoCommand.IsEnabled, "redo");

            window.KeyPressQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            window.KeyReleaseQwerty(PhysicalKey.Y, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Equal("edited", asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Retrieve());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// A text box with the focus undoes its own text: the window's shortcut runs only when the focused control left the key alone.
    /// </summary>
    [AvaloniaFact]
    public async Task ATextBoxKeepsItsOwnCtrlZ()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "label" }));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            Edit(fixture, asset, "edited");
            await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActionHistory.CanUndo, "the history");

            var textBox = window.GetVisualDescendants().OfType<TextBox>().First(x => x.IsEffectivelyVisible);
            Assert.True(textBox.Focus());
            window.KeyTextInput("abc");
            await WindowTestHelper.PumpAsync();
            Assert.Equal("abc", textBox.Text);

            window.KeyPressQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            window.KeyReleaseQwerty(PhysicalKey.Z, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Equal("edited", asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Retrieve());
            Assert.True(string.IsNullOrEmpty(textBox.Text), $"The box shows '{textBox.Text}'");
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static MenuItem MenuItem(Window window, string name) => window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == name);

    /// <summary>
    /// Whether the item is enabled when the user opens its menu: Avalonia evaluates the command of an item of a closed menu
    /// only when the menu opens.
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

    /// <summary>
    /// Sets the label of the asset the way the property grid does: through its property graph, in a transaction of the history.
    /// </summary>
    private static void Edit(ShellSessionFixture fixture, AssetViewModel asset, string label)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TextPropertiesAsset.Label)].Update(label);
        undoRedo.SetName(transaction, "Edit label");
    }
}
