// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Headless.XUnit;
using Stride.Assets.Materials;
using Stride.Assets.Sprite;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// A session whose load changed assets (here a sprite sheet and a material on the same URL: one of them gets another URL) is not
/// saved when it opens; closing the main window asks, as for any unsaved change, and leaves the files as they were on "Don't save".
/// </summary>
public sealed class LoadChangesCloseTests
{
    [AvaloniaFact]
    public async Task ClosingTheWindowAsksBeforeSavingWhatTheLoadChanged()
    {
        using var fixture = ShellSessionFixture.Create(("Echo", new SpriteSheetAsset()), ("Echo", new MaterialAsset()));
        var dialogs = fixture.ReplaceDialogService();
        await fixture.OpenAsync();
        var files = AssetFiles(fixture);
        Assert.Equal(["Echo.sdmat", "Echo.sdsheet"], files);
        Assert.Contains(fixture.Session.AllAssets, x => x.Url == ShellSessionFixture.Url("Echo (2)"));
        Assert.True(fixture.Session.HasUnsavedChanges);
        Assert.Empty(dialogs.Messages);

        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        dialogs.PickButton = buttons => buttons.Single(x => Equals(x.Content, "Don't save")).Result;
        try
        {
            window.Close();
            await WindowTestHelper.WaitUntilAsync(() => !window.IsVisible, "the window to close");

            Assert.Equal("The project has unsaved changes. Do you want to save it?", Assert.Single(dialogs.Messages));
            Assert.Equal(files, AssetFiles(fixture));
        }
        finally
        {
            if (window.IsVisible)
                await window.CloseAsync();
        }
    }

    private static IReadOnlyList<string> AssetFiles(ShellSessionFixture fixture)
    {
        var assets = Path.Combine(fixture.PackageDirectory, "Assets");
        return [.. Directory.EnumerateFiles(assets).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal)];
    }
}
