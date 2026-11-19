// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Stride.Assets.Editor.Testing;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Avalonia.Tests;

/// <summary>
/// A session of the editor test kit on the UI thread of the toolkit, as a headless test of a plugin's view opens one.
/// </summary>
public sealed class EditorTestKitViewTests
{
    [AvaloniaFact]
    public async Task AViewOfASessionOnTheUIThreadFollowsTheChangesOfTheSessionWithoutLeavingTheUIThread()
    {
        await using var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions
        {
            Dispatcher = DispatcherService.Create(),
        }, ("Sprites/Sheet", new SpriteSheetAsset()));
        Assert.True(Dispatcher.UIThread.CheckAccess(), "The test goes on on the UI thread once the session is open.");
        Assert.True(fixture.Session.Dispatcher.CheckAccess(), "The UI thread is the session's dispatcher.");

        var sheet = fixture.Asset<AssetViewModel>("Sprites/Sheet");
        var name = new TextBlock { [!TextBlock.TextProperty] = new Binding(nameof(AssetViewModel.Name)) };
        var window = new Window { Content = name, DataContext = sheet };
        window.Show();
        try
        {
            Assert.Equal("Sheet", name.Text);

            // A change through the session: an undoable rename
            sheet.Name = "Renamed";
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Renamed", name.Text);
            fixture.Session.ServiceProvider.Get<IUndoRedoService>().Undo();
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Sheet", name.Text);

            // What the session's services post from another thread reaches the view on its thread
            await Task.Run(() => fixture.Session.Dispatcher.InvokeAsync(() => sheet.Name = "Posted"));
            Assert.True(Dispatcher.UIThread.CheckAccess(), "The test is back on the UI thread.");
            Dispatcher.UIThread.RunJobs();
            Assert.Equal("Posted", name.Text);
        }
        finally
        {
            window.Close();
        }
    }
}
