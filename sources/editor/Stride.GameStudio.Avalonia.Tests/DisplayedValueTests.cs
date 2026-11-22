// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using System.Numerics;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The editors of the property grid that show a value converted or rounded (a color as hexadecimal text, a number with
/// decimal places, an angle in degrees, a vector by components): showing an asset must leave its values, its dirty flag
/// and the undo stack as they were.
/// </summary>
public sealed class DisplayedValueTests
{
    private const string AssetLocation = "Displayed";
    private const string OtherAssetLocation = "OtherDisplayed";

    [AvaloniaFact]
    public async Task ShowingAnAssetWritesNothingBack()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ValuesOf(asset);
            await ShowAsync(fixture, window, [asset]);

            Assert.Empty(Problems(fixture, (asset, original)));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task VisitingEveryBoxWritesNothingBack()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ValuesOf(asset);
            var grid = await ShowAsync(fixture, window, [asset]);
            // Every text box of the editors, the numeric ones included (a box in a numeric box), as the Tab key would
            var boxes = grid.GetVisualDescendants().OfType<TextBox>()
                .Where(x => x.IsEffectivelyVisible && x.GetVisualAncestors().OfType<Border>().Any(b => b.Name == "PART_Editor"))
                .ToList();
            Assert.NotEmpty(boxes);
            foreach (var box in boxes)
            {
                box.Focus();
                await WindowTestHelper.PumpAsync();
            }
            MoveFocusAway(window, boxes[^1]);
            await WindowTestHelper.PumpAsync();

            Assert.Empty(Problems(fixture, (asset, original)));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ShowingAssetsWithDifferentValuesWritesNothingBack()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()), (OtherAssetLocation, OtherAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var otherAsset = fixture.Asset(OtherAssetLocation);
            var original = ValuesOf(asset);
            var otherOriginal = ValuesOf(otherAsset);
            await ShowAsync(fixture, window, [asset, otherAsset]);

            Assert.Empty(Problems(fixture, (asset, original), (otherAsset, otherOriginal)));
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TypingAColorThenEnterMakesOneHistoryEntry()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, NewAsset()));
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var asset = fixture.Asset(AssetLocation);
            var original = ((DisplayedValuesAsset)asset.Asset).LightColor;
            var grid = await ShowAsync(fixture, window, [asset]);
            var textBox = ColorTextBox(grid, nameof(DisplayedValuesAsset.LightColor));

            Assert.True(textBox.Focus());
            textBox.SelectAll();
            window.KeyTextInput("#204080");
            await WindowTestHelper.PumpAsync();
            // Typing alone commits nothing
            Assert.Equal(original, ((DisplayedValuesAsset)asset.Asset).LightColor);

            window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
            var history = fixture.Session.ActionHistory;
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == 1, "the history entry");
            Assert.Equal(new Color3(0x20 / 255f, 0x40 / 255f, 0x80 / 255f), ((DisplayedValuesAsset)asset.Asset).LightColor);

            history.UndoCommand.Execute();
            await WindowTestHelper.PumpAsync();
            Assert.Equal(original, ((DisplayedValuesAsset)asset.Asset).LightColor);
            // Showing the restored color writes nothing back, even when the box that had the focus loses it: the entry is
            // still there to redo
            await WindowTestHelper.WaitUntilAsync(() => history.CanRedo, "the redo");
            MoveFocusAway(window, textBox);
            await WindowTestHelper.PumpAsync();
            Assert.Equal(original, ((DisplayedValuesAsset)asset.Asset).LightColor);
            Assert.Single(history.Transactions);
            Assert.True(history.CanRedo);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    private static DisplayedValuesAsset NewAsset() => new()
    {
        ByteColor = new Color(255, 128, 64, 192),
        // 0.5 shows as 7F (0.498), 0.25 as 3F (0.247)
        LightColor = new Color3(1f, 0.5f, 0.25f),
        TintColor = new Color4(1f, 0.5f, 0.25f, 0.75f),
        Ratio = 1.23456789f,
        TwoDecimals = 1.23456f,
        Precise = 1.234567890123,
        Size = new Vector2(1.23456f, -2.34567f),
        Offset = new Vector3(1.23456f, -2.34567f, 3.45678f),
        Weights = new Vector4(0.12345f, 0.23456f, 0.34567f, 0.45678f),
        Rotation = Quaternion.CreateFromYawPitchRoll(0.1234f, 0.2345f, 0.3456f),
        Transform = Matrix4x4.CreateScale(1.23456f) * Matrix4x4.CreateRotationY(0.3456f) * Matrix4x4.CreateTranslation(1.23456f, 2.34567f, 3.45678f),
        Region = new RectangleF(1.23456f, 2.34567f, 3.45678f, 4.56789f),
        Angle = new AngleSingle(0.123456f, AngleType.Radian),
        Duration = TimeSpan.FromTicks(12_345_678_901),
        Time = new DateTime(2026, 11, 18, 1, 2, 3).AddTicks(4_567_891),
    };

    private static DisplayedValuesAsset OtherAsset() => new()
    {
        ByteColor = new Color(1, 2, 3, 4),
        LightColor = new Color3(0.1f, 0.2f, 0.3f),
        TintColor = new Color4(0.1f, 0.2f, 0.3f, 0.4f),
        Ratio = 9.87654321f,
        TwoDecimals = 9.87654f,
        Precise = 9.876543210987,
        Size = new Vector2(9.87654f, 8.76543f),
        Offset = new Vector3(9.87654f, 8.76543f, 7.65432f),
        Weights = new Vector4(0.98765f, 0.87654f, 0.76543f, 0.65432f),
        Rotation = Quaternion.CreateFromYawPitchRoll(0.9876f, 0.8765f, 0.7654f),
        Transform = Matrix4x4.CreateTranslation(9.87654f, 8.76543f, 7.65432f),
        Region = new RectangleF(9.87654f, 8.76543f, 7.65432f, 6.54321f),
        Angle = new AngleSingle(0.987654f, AngleType.Radian),
        Duration = TimeSpan.FromTicks(98_765_432_109),
        Time = new DateTime(2025, 1, 2, 3, 4, 5).AddTicks(9_876_543),
    };

    private static async Task<PropertyGridView> ShowAsync(ShellSessionFixture fixture, MainWindow window, IReadOnlyCollection<AssetViewModel> assets)
    {
        // Selecting assets generates their properties asynchronously; wait for the grid to show the editors of the members
        fixture.Session.AssetCollection.SelectAssets(assets);
        var grid = window.GetVisualDescendants().OfType<PropertyGridView>().Single();
        await WindowTestHelper.WaitUntilAsync(() => grid.GetVisualDescendants().OfType<Color3Editor>().Any(x => IsEditorOf(x, nameof(DisplayedValuesAsset.LightColor)))
            && grid.GetVisualDescendants().OfType<DateTimeEditor>().Any(x => IsEditorOf(x, nameof(DisplayedValuesAsset.Time))), "the editors of the members");
        // and settle: a second generation would replace the editors
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        return grid;
    }

    private static bool IsEditorOf(Control editor, string member) => editor.DataContext is NodeViewModel node && node.Name == member;

    private static TextBox ColorTextBox(PropertyGridView grid, string member)
    {
        var editor = grid.GetVisualDescendants().OfType<Color3Editor>().Single(x => IsEditorOf(x, member));
        return editor.GetVisualDescendants().OfType<TextBox>().Single(x => x.Name == "TextBox");
    }

    private static void MoveFocusAway(MainWindow window, TextBox textBox)
    {
        // The asset view's filter box is always there to take the focus
        var other = window.GetVisualDescendants().OfType<TextBox>().First(x => x != textBox && x.IsEffectivelyVisible && x.DataContext is not NodeViewModel);
        Assert.True(other.Focus());
        Assert.False(textBox.IsFocused);
    }

    /// <summary>
    /// Every member of the asset with its value, for a message that names the members that changed.
    /// </summary>
    private static IReadOnlyList<string> ValuesOf(AssetViewModel asset)
    {
        return typeof(DisplayedValuesAsset).GetProperties()
            .Where(x => x.DeclaringType == typeof(DisplayedValuesAsset))
            .Select(x => $"{x.Name} = {Exact(x.GetValue(asset.Asset))}")
            .ToList();
    }

    /// <summary>
    /// What showing the assets changed: the members whose value changed, the assets made dirty and the history entries.
    /// </summary>
    private static IReadOnlyList<string> Problems(ShellSessionFixture fixture, params (AssetViewModel Asset, IReadOnlyList<string> Original)[] assets)
    {
        var problems = new List<string>();
        foreach (var (asset, original) in assets)
        {
            problems.AddRange(Changes(original, ValuesOf(asset)).Select(x => $"{asset.Name}: {x}"));
            if (asset.IsDirty)
                problems.Add($"{asset.Name} is dirty");
        }
        problems.AddRange(fixture.Session.ActionHistory.Transactions.Select(x => $"History entry: {x.Name}"));
        return problems;
    }

    /// <summary>
    /// The members whose value changed, as "before -> after".
    /// </summary>
    private static IReadOnlyList<string> Changes(IReadOnlyList<string> before, IReadOnlyList<string> after)
    {
        return before.Zip(after).Where(x => x.First != x.Second).Select(x => $"{x.First} -> {x.Second}").ToList();
    }

    /// <summary>
    /// The value with every bit of its floating-point components, so that a rounding shows.
    /// </summary>
    private static string Exact(object? value) => value switch
    {
        float f => f.ToString("R"),
        double d => d.ToString("R"),
        DateTime t => t.Ticks.ToString(),
        TimeSpan t => t.Ticks.ToString(),
        AngleSingle a => a.Radians.ToString("R"),
        _ when value is not null && value.GetType().IsPrimitive => value.ToString()!,
        _ when value is not null && value.GetType().IsValueType => string.Join(", ", value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Select(x => Exact(x.GetValue(value)))),
        _ => value?.ToString() ?? "null",
    };
}
