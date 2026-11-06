// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.IO;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The string, char, file and directory text boxes of the property grid. They wrote their text back on every keystroke,
/// which made one history entry per key; like the WPF editor, they now commit on Enter or when the focus leaves them.
/// </summary>
public sealed class TextPropertyTests
{
    private const string AssetLocation = "Text";
    private const string OtherAssetLocation = "OtherText";

    [AvaloniaTheory]
    [InlineData(nameof(TextPropertiesAsset.Label))]
    [InlineData(nameof(TextPropertiesAsset.Separator))]
    [InlineData(nameof(TextPropertiesAsset.SourceFile))]
    [InlineData(nameof(TextPropertiesAsset.OutputDirectory))]
    public async Task ShowingAValueWritesNothingBack(string member)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ValueOf(asset, member);
            var textBox = await ShowAndFindTextBoxAsync(fixture, window, [asset], member);

            Assert.Equal(AsDisplayed(original), textBox.Text);
            Assert.Equal(original, ValueOf(asset, member));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(TextPropertiesAsset.Label), "word", "word")]
    [InlineData(nameof(TextPropertiesAsset.Separator), "word", "w")]
    [InlineData(nameof(TextPropertiesAsset.SourceFile), "other/file.png", "other/file.png")]
    [InlineData(nameof(TextPropertiesAsset.OutputDirectory), "other/directory", "other/directory")]
    public async Task TypingAWordThenEnterMakesOneHistoryEntry(string member, string typed, string expected)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ValueOf(asset, member);
            var textBox = await ShowAndFindTextBoxAsync(fixture, window, [asset], member);

            Assert.True(textBox.Focus());
            textBox.SelectAll();
            // One key at a time, as the user types
            foreach (var key in typed)
            {
                window.KeyTextInput(key.ToString());
            }
            await WindowTestHelper.PumpAsync();
            // Typing alone commits nothing
            Assert.Equal(original, ValueOf(asset, member));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            Assert.Equal(expected, ValueOf(asset, member));
            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            // Leaving the box afterwards adds nothing
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();
            Assert.Single(history.Transactions);

            // The single entry undoes the whole word
            history.UndoCommand.Execute();
            Assert.Equal(original, ValueOf(asset, member));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(TextPropertiesAsset.Label))]
    [InlineData(nameof(TextPropertiesAsset.Separator))]
    [InlineData(nameof(TextPropertiesAsset.SourceFile))]
    [InlineData(nameof(TextPropertiesAsset.OutputDirectory))]
    public async Task EscapeGivesTheBoundValueBack(string member)
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ValueOf(asset, member);
            var textBox = await ShowAndFindTextBoxAsync(fixture, window, [asset], member);

            Assert.True(textBox.Focus());
            textBox.SelectAll();
            foreach (var key in "nonsense")
            {
                window.KeyTextInput(key.ToString());
            }
            window.KeyPressQwerty(PhysicalKey.Escape, RawInputModifiers.None);
            await WindowTestHelper.PumpAsync();
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();

            Assert.Equal(AsDisplayed(original), textBox.Text);
            Assert.Equal(original, ValueOf(asset, member));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaTheory]
    [InlineData(nameof(TextPropertiesAsset.Label))]
    [InlineData(nameof(TextPropertiesAsset.Separator))]
    [InlineData(nameof(TextPropertiesAsset.SourceFile))]
    [InlineData(nameof(TextPropertiesAsset.OutputDirectory))]
    public async Task ShowingDifferentValuesWritesNothingBack(string member)
    {
        var other = new TextPropertiesAsset { Label = "other", Separator = ',', SourceFile = new UFile("other/file.png"), OutputDirectory = new UDirectory("other/directory") };
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()), (OtherAssetLocation, other));
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var otherAsset = fixture.Asset(OtherAssetLocation);
            var original = ValueOf(asset, member);
            var otherOriginal = ValueOf(otherAsset, member);
            var textBox = await ShowAndFindTextBoxAsync(fixture, window, [asset, otherAsset], member);
            // Visiting the box without typing writes nothing either
            Assert.True(textBox.Focus());
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();

            Assert.True(string.IsNullOrEmpty(textBox.Text), $"The box shows '{textBox.Text}'");
            Assert.Equal(original, ValueOf(asset, member));
            Assert.Equal(otherOriginal, ValueOf(otherAsset, member));
            Assert.Empty(fixture.Session.ActionHistory.Transactions);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static TextPropertiesAsset NewAsset() => new()
    {
        Label = "label",
        Separator = ';',
        SourceFile = new UFile("original/file.png"),
        OutputDirectory = new UDirectory("original/directory"),
    };

    private static async Task<TextBox> ShowAndFindTextBoxAsync(ShellSessionFixture fixture, MainWindow window, IReadOnlyCollection<AssetViewModel> assets, string member)
    {
        // Selecting assets generates their properties asynchronously; wait for the grid to show the box of the member
        fixture.Session.AssetCollection.SelectAssets(assets);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        TextBox? textBox = null;
        await WindowTestHelper.WaitUntilAsync(() => (textBox = grid.GetVisualDescendants().OfType<TextBox>().SingleOrDefault(x => IsEditorOf(x, member))) is not null, $"the box of {member}");
        // and settle: a second generation would replace the box
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        Assert.True(textBox!.IsAttachedToVisualTree());
        return textBox;
    }

    private static bool IsEditorOf(TextBox textBox, string member)
    {
        // The header of a property has a hidden box to rename it, and the char template a numeric box (a TextBox in a
        // NumericTextBox) for the code of the character
        return textBox.GetType() == typeof(TextBox)
            && textBox.DataContext is NodeViewModel node && node.Name == member
            && textBox.GetVisualAncestors().OfType<Border>().Any(x => x.Name == "PART_Editor")
            && !textBox.GetVisualAncestors().OfType<NumericTextBox>().Any();
    }

    private static void MoveFocusAway(MainWindow window, TextBox textBox)
    {
        // The asset view's filter box is always there to take the focus
        var other = window.GetVisualDescendants().OfType<TextBox>().First(x => x != textBox && x.IsEffectivelyVisible && x.DataContext is not NodeViewModel);
        Assert.True(other.Focus());
        Assert.False(textBox.IsFocused);
    }

    /// <summary>
    /// The file and directory boxes show the paths with the separator of the system.
    /// </summary>
    private static string? AsDisplayed(string? value) => value?.Replace('/', Path.DirectorySeparatorChar);

    /// <summary>
    /// The value of a member of the asset as text. The session loads relative paths as absolute ones (relative to the
    /// asset file), which the boxes show.
    /// </summary>
    private static string? ValueOf(AssetViewModel asset, string member)
    {
        return typeof(TextPropertiesAsset).GetProperty(member)!.GetValue(asset.Asset)?.ToString();
    }
}
