// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Stride.Assets.Sprite;
using Stride.Core.IO;
using Stride.GameStudio.Avalonia.Helpers;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// File > Open recent lists the recent sessions as WPF does: the version and the path of each, then "Clear list".
/// </summary>
public sealed class RecentSessionsMenuTests
{
    [AvaloniaFact]
    public async Task EachEntryShowsItsVersionAndItsPathThenClearList()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var older = new UFile(Path.Combine(fixture.Directory, "Older", "Older.sln"));
        fixture.Main.MRU.AddFile(older, "4.0");
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var menu = window.GetLogicalDescendants().OfType<MenuItem>().Single(x => x.Name == "RecentSessionsMenu");
            Assert.True(menu.IsEnabled);
            var entries = menu.Items.OfType<MenuItem>().Where(x => x.Header is RecentSessionViewModel).ToList();
            Assert.Equal(fixture.Main.RecentFiles.Count, entries.Count);

            // The most recent first: the session that was just added, then the opened one
            var olderEntry = (RecentSessionViewModel)entries[0].Header!;
            Assert.Equal("4.0", olderEntry.Version);
            Assert.Equal(older.ToOSPath(), olderEntry.DisplayPath);
            Assert.Equal(older.ToOSPath(), ToolTip.GetTip(entries[0]));
            Assert.Same(olderEntry.OpenCommand, entries[0].Command);
            var openedEntry = (RecentSessionViewModel)entries[1].Header!;
            Assert.Equal(StrideGameStudio.EditorVersionMajor, openedEntry.Version);
            Assert.Equal(fixture.PackagePath.ToOSPath(), openedEntry.DisplayPath);

            // The entry shows the version and the path
            var fileMenu = Assert.IsType<MenuItem>(menu.Parent);
            fileMenu.Open();
            await WindowTestHelper.PumpAsync();
            menu.Open();
            await WindowTestHelper.PumpAsync();
            var texts = entries[0].GetVisualDescendants().OfType<TextBlock>().Select(x => x.Text).ToList();
            Assert.Contains("4.0", texts);
            Assert.Contains(older.ToOSPath(), texts);
            menu.Close();
            fileMenu.Close();
            await WindowTestHelper.PumpAsync();

            // "Clear list" closes the submenu, after a separator, and empties it
            Assert.IsType<Separator>(menu.Items[^2]);
            var clear = Assert.IsType<MenuItem>(menu.Items[^1]);
            Assert.Equal("Clear list", clear.Header);
            Assert.Same(fixture.Main.ClearRecentFilesCommand, clear.Command);
            clear.Command!.Execute(null);
            await WindowTestHelper.PumpAsync();
            Assert.Empty(fixture.Main.RecentFiles);
            Assert.False(menu.IsEnabled);
            Assert.DoesNotContain(menu.Items, x => x is MenuItem { Header: RecentSessionViewModel });
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task SavingMovesTheSessionToTheTopOfTheList()
    {
        using var fixture = await ShellSessionFixture.OpenAsync(("Sheet", new SpriteSheetAsset()));
        var other = new UFile(Path.Combine(fixture.Directory, "Other", "Other.sln"));
        fixture.Main.MRU.AddFile(other, StrideGameStudio.EditorVersionMajor);
        Assert.Equal(other, fixture.Main.RecentFiles[0].FilePath);

        fixture.Main.SaveCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => fixture.Main.RecentFiles[0].FilePath == fixture.PackagePath, "the saved session on top");
    }
}
