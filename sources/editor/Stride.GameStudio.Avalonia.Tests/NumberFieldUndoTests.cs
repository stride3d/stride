// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// Once a number typed in the property grid is written (Enter), Undo undoes that edit even while the field has the focus, as
/// in the other text boxes of the editor once their edit is written; before Enter, Undo undoes the typing. Losing the focus
/// afterwards writes nothing.
/// </summary>
public sealed class NumberFieldUndoTests
{
    private const string AssetLocation = "Typed";

    [AvaloniaFact]
    public async Task CtrlZAfterEnterUndoesTheWrittenValue()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new DisplayedValuesAsset { Ratio = 1f, Offset = new Vector3(1, 2, 3) }));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        // After it opens, as the window restores its saved size then: wide enough for the grid to give the editors their width
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var grid = await ShowAsync(fixture, window, asset);
            var service = fixture.Session.UndoRedoService;
            var xBox = Editor<Vector3Editor>(grid, nameof(DisplayedValuesAsset.Offset)).GetVisualDescendants().OfType<NumericTextBox>().First();

            await TypeAsync(window, xBox, "4");
            Press(window, PhysicalKey.Enter, RawInputModifiers.None);
            await WindowTestHelper.WaitUntilAsync(() => Offset(asset) == new Vector3(4, 2, 3), "the written value");
            Assert.Single(service.RetrieveAllTransactions());

            // The field still has the focus
            Press(window, PhysicalKey.Z, RawInputModifiers.Control);
            await WindowTestHelper.WaitUntilAsync(() => Offset(asset) == new Vector3(1, 2, 3), "the undone value");
            Assert.True(service.CanRedo);
            Assert.Equal("1", xBox.Text);

            // Another field takes the focus: nothing is written
            Assert.True(Editor<NumericTextBox>(grid, nameof(DisplayedValuesAsset.Ratio)).GetVisualDescendants().OfType<TextBox>().Single().Focus());
            await WindowTestHelper.PumpAsync();
            Assert.Equal(new Vector3(1, 2, 3), Offset(asset));
            Assert.Single(service.RetrieveAllTransactions());
            Assert.True(service.CanRedo);

            fixture.Session.ActionHistory.RedoCommand.Execute();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(new Vector3(4, 2, 3), Offset(asset));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CtrlZBeforeEnterUndoesTheTyping()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new DisplayedValuesAsset { Ratio = 1f, Offset = new Vector3(1, 2, 3) }));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        window.Width = 1600;
        window.Height = 1000;
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var grid = await ShowAsync(fixture, window, asset);
            var service = fixture.Session.UndoRedoService;
            var xBox = Editor<Vector3Editor>(grid, nameof(DisplayedValuesAsset.Offset)).GetVisualDescendants().OfType<NumericTextBox>().First();

            await TypeAsync(window, xBox, "4");
            Press(window, PhysicalKey.Z, RawInputModifiers.Control);
            await WindowTestHelper.PumpAsync();
            Assert.Equal("1", xBox.Text);

            Assert.True(Editor<NumericTextBox>(grid, nameof(DisplayedValuesAsset.Ratio)).GetVisualDescendants().OfType<TextBox>().Single().Focus());
            await WindowTestHelper.PumpAsync();
            Assert.Equal(new Vector3(1, 2, 3), Offset(asset));
            Assert.Empty(service.RetrieveAllTransactions());
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TheEditMenuUndoAfterEnterUndoesTheWrittenValueOfAnEntity()
    {
        await using var scene = await SceneTreeDropTests.OpenSceneAsync();
        var service = scene.Fixture.Session.UndoRedoService;
        var box = scene.Entity("Box");
        scene.Editor.SelectedContent.Clear();
        scene.Editor.SelectedContent.Add(box);
        await WindowTestHelper.WaitUntilAsync(() => scene.Window.GetVisualDescendants().OfType<Vector3Editor>().Any(x => IsEditorOf(x, "Position")), "the position");
        // Settle: a second generation would replace the editors
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        var xBox = scene.Window.GetVisualDescendants().OfType<Vector3Editor>().First(x => IsEditorOf(x, "Position")).GetVisualDescendants().OfType<NumericTextBox>().First();
        var steps = service.RetrieveAllTransactions().Count();

        await TypeAsync(scene.Window, xBox, "4");
        Press(scene.Window, PhysicalKey.Enter, RawInputModifiers.None);
        await WindowTestHelper.WaitUntilAsync(() => box.AssetSideEntity.Transform.Position.X == 4, "the written value");
        Assert.Equal(steps + 1, service.RetrieveAllTransactions().Count());

        // Edit > Undo, with the field as the panel the user was in
        var undo = scene.Window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == "UndoMenuItem");
        Assert.True(undo.Command!.CanExecute(undo.CommandParameter));
        undo.Command.Execute(undo.CommandParameter);
        await WindowTestHelper.WaitUntilAsync(() => box.AssetSideEntity.Transform.Position.X == 0, "the undone value");
        Assert.True(service.CanRedo);

        // The tree takes the focus: nothing is written
        Assert.True(scene.Tree.Focus());
        await WindowTestHelper.PumpAsync();
        Assert.Equal(0, box.AssetSideEntity.Transform.Position.X);
        Assert.Equal(steps + 1, service.RetrieveAllTransactions().Count());
        Assert.True(service.CanRedo);
    }

    private static void Press(Window window, PhysicalKey key, RawInputModifiers modifiers)
    {
        window.KeyPressQwerty(key, modifiers);
        window.KeyReleaseQwerty(key, modifiers);
    }

    private static async Task TypeAsync(Window window, NumericTextBox box, string text)
    {
        var textBox = box.GetVisualDescendants().OfType<TextBox>().Single();
        Assert.True(textBox.Focus());
        textBox.SelectAll();
        window.KeyTextInput(text);
        await WindowTestHelper.PumpAsync();
    }

    private static Vector3 Offset(AssetViewModel asset) => ((DisplayedValuesAsset)asset.Asset).Offset;

    private static async Task<PropertyGridView> ShowAsync(ShellSessionFixture fixture, MainWindow window, AssetViewModel asset)
    {
        fixture.Session.AssetCollection.SelectAssets([asset]);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        await WindowTestHelper.WaitUntilAsync(() => grid.GetVisualDescendants().OfType<Vector3Editor>().Any(x => IsEditorOf(x, nameof(DisplayedValuesAsset.Offset))), "the editors of the members");
        // Settle: a second generation would replace the editors
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return grid;
    }

    private static T Editor<T>(PropertyGridView grid, string member) where T : Control
    {
        return grid.GetVisualDescendants().OfType<T>().First(x => IsEditorOf(x, member));
    }

    private static bool IsEditorOf(Control editor, string member) => editor.DataContext is NodeViewModel node && node.Name == member;
}
