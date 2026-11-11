// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.UI.Assets;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Editor.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The Avalonia UI library editor, as the WPF one: a library has several roots, each exported by its name, and the root of
/// the last selected element is the active one (in bold).
/// </summary>
public sealed class UILibraryEditorTests
{
    [AvaloniaFact]
    public async Task EachRootOfTheLibraryIsExportedByItsName()
    {
        await using var library = await OpenLibraryAsync();
        Assert.IsType<UILibraryEditorView>(library.View);
        Assert.IsType<UILibraryEditorViewModel>(library.Editor);

        // The empty area adds roots: a library keeps them all
        var grid = await library.AddRootAsync("Grid");
        var canvas = await library.AddRootAsync("Canvas");
        Assert.Equal([grid, canvas], library.Root.Children);
        Assert.Equal([grid.Name, canvas.Name], Exports(library).OrderBy(x => x.Key == grid.Id.ObjectId ? 0 : 1).Select(x => x.Value));
    }

    [AvaloniaFact]
    public async Task RenamingARootRenamesItsExportAndUndoRestoresIt()
    {
        await using var library = await OpenLibraryAsync();
        var grid = await library.AddRootAsync("Grid");
        var name = grid.Name;
        var history = library.Fixture.Session.ActionHistory;
        await WindowTestHelper.PumpAsync();
        await library.SelectAsync(grid);

        library.Window.KeyPressQwerty(PhysicalKey.F2, RawInputModifiers.None);
        await WindowTestHelper.WaitUntilAsync(() => grid.IsEditing, "the rename");
        var box = library.Row(grid).GetVisualDescendants().OfType<TextBox>().Single(x => x.IsEffectivelyVisible);
        await WindowTestHelper.WaitUntilAsync(() => box.IsFocused, "the focus of the name box");
        box.SelectAll();
        library.Window.KeyTextInput("Layout");
        library.Window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        await WindowTestHelper.WaitUntilAsync(() => !grid.IsEditing, "the end of the rename");
        Assert.Equal("Layout", Exports(library)[grid.Id.ObjectId]);

        // The rename and the export are undone together
        await WindowTestHelper.PumpAsync();
        history.UndoCommand.Execute();
        Assert.Equal(name, grid.Name);
        Assert.Equal(name, Exports(library)[grid.Id.ObjectId]);
    }

    [AvaloniaFact]
    public async Task TheRootOfTheSelectionIsTheActiveRootInBold()
    {
        await using var library = await OpenLibraryAsync();
        var grid = await library.AddRootAsync("Grid");
        var canvas = await library.AddRootAsync("Canvas");
        await library.DropAsync([library.Factory("Button")], library.RowCenter(canvas));
        var button = Assert.Single(canvas.Children);
        await library.WaitForRowAsync(button);

        await library.SelectAsync(grid);
        Assert.Same(grid, library.Editor.ActiveRoot);
        await WindowTestHelper.WaitUntilAsync(() => IsBold(library, grid) && !IsBold(library, canvas), "the active root in bold");

        // A child selects its root
        await library.SelectAsync(button);
        Assert.Same(canvas, library.Editor.ActiveRoot);
        await WindowTestHelper.WaitUntilAsync(() => IsBold(library, canvas) && !IsBold(library, grid), "the new active root in bold");
    }

    private static bool IsBold(UIEditorFixture library, UIElementViewModel root)
        => library.Row(root).GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == root.Name).FontWeight == FontWeight.Bold;

    private static IReadOnlyDictionary<Guid, string> Exports(UIEditorFixture library)
        => ((UILibraryAsset)library.Editor.Asset.Asset).PublicUIElements.ToDictionary(x => x.Key, x => x.Value);

    private static Task<UIEditorFixture> OpenLibraryAsync() => UIEditorFixture.OpenAsync<UILibraryAsset>("Library");
}
