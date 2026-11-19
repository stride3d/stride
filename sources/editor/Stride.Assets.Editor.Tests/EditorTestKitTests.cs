// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Sprite;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The test kit of editor plugins (Stride.Assets.Editor.Testing) as a plugin uses it: a session of Stride's assets with the plugins of
/// the test, stand-in views for the editors it opens, and services that fail when the test didn't give them.
/// </summary>
public sealed class EditorTestKitTests
{
    [Fact]
    public async Task ASessionOpensItsAssetsAndTheEditorsThatHaveAStandInView()
    {
        string directory;
        await using (var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions
        {
            // The editor plugin knows the editor of a sprite sheet
            Plugins = [new StrideEditorPlugin { EnableThumbnailService = false }],
            StandInEditors = [typeof(SpriteSheetEditorViewModel)],
        },
            ("Sprites/Sheet", new SpriteSheetAsset())))
        {
            directory = fixture.Directory;
            Assert.True(Directory.Exists(directory));
            var sheet = fixture.Asset<SpriteSheetViewModel>("Sprites/Sheet");
            Assert.Equal("/TestGame/Sprites/Sheet", sheet.Url);
            Assert.True(sheet.IsEditable, "The package of the test is a project of the solution, hence editable.");

            var collection = fixture.Session.EditorCollection;
            collection.OpenAssetEditor(sheet);
            Assert.IsType<SpriteSheetEditorViewModel>(Assert.Single(collection.Editors));
            Assert.True(await collection.CloseAllEditorsAsync(false));
        }

        // The session, its background work and its folder are gone
        Assert.False(Directory.Exists(directory));
    }

    [Fact]
    public async Task TheDialogAndClipboardServicesFailUnlessTheTestGivesThem()
    {
        await using (var fixture = await StrideEditorTestSession.OpenAsync([]))
        {
            var dialogs = fixture.ServiceProvider.Get<IEditorDialogService>();
            await Assert.ThrowsAsync<NotSupportedException>(() => dialogs.MessageBoxAsync("Delete?", MessageBoxButton.YesNo));
            await Assert.ThrowsAsync<NotSupportedException>(() => fixture.ServiceProvider.Get<IClipboardService>().GetTextAsync());
        }

        // A fake of the test replaces the stand-in
        var clipboard = new FakeClipboardService();
        await using var withClipboard = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions { Services = [clipboard] });
        Assert.Same(clipboard, withClipboard.ServiceProvider.Get<IClipboardService>());
    }

    [Fact]
    public async Task TheTestScriptsTheAnswersOfTheDialogServiceAndReadsTheQuestions()
    {
        var dialogs = new TestEditorDialogService();
        dialogs.Answers.Enqueue(MessageBoxResult.Yes);
        dialogs.Answers.Enqueue(MessageBoxResult.No);
        dialogs.Answer = MessageBoxResult.Cancel;
        await using var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions { Services = [dialogs] });
        var asker = new Asker(fixture.ServiceProvider.Get<IEditorDialogService>());

        // The scripted answers reach the code that asked, in order, then the default answer
        Assert.Equal("overwritten", await asker.OverwriteAsync("Sprites/Sheet"));
        Assert.Equal("kept", await asker.OverwriteAsync("Sprites/Other"));
        Assert.Equal("cancelled", await asker.OverwriteAsync("Sprites/Last"));
        // A message box with buttons of its own takes the button of the answer
        dialogs.Answers.Enqueue(MessageBoxResult.No);
        Assert.Equal((int)MessageBoxResult.No, await asker.PickAsync());

        Assert.Equal(["Overwrite Sprites/Sheet?", "Overwrite Sprites/Other?", "Overwrite Sprites/Last?", "Which one?"], dialogs.Messages);
        Assert.Empty(dialogs.Answers);
        // The pickers cancel unless the test picks
        Assert.Null(await fixture.ServiceProvider.Get<IEditorDialogService>().OpenFilePickerAsync());
    }

    [Fact]
    public async Task TheSessionsOwnSaveRunsWithTheProgressWindowThatShowsNothing()
    {
        await using var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions(),
            ("Sprites/Sheet", new SpriteSheetAsset()));
        var sheet = fixture.Asset<SpriteSheetViewModel>("Sprites/Sheet");
        var file = Path.Combine(fixture.Directory, fixture.PackageName, "Assets", "Sprites", "Sheet.sdsheet");
        File.Delete(file);
        // The package keeps the asset to save, as an edit leaves it
        sheet.AssetItem.IsDirty = true;

        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.True(File.Exists(file), "The save wrote the asset.");
        Assert.False(sheet.AssetItem.IsDirty);
        var progress = Assert.IsType<TestProgressWindowService>(fixture.ServiceProvider.Get<IProgressWindowService>());
        Assert.Equal(500, Assert.Single(progress.Shown).MinDelay);
    }

    /// <summary>
    /// Code that asks the user, as a plugin's service does.
    /// </summary>
    private sealed class Asker(IEditorDialogService dialogs)
    {
        public async Task<string> OverwriteAsync(string location)
        {
            return await dialogs.MessageBoxAsync($"Overwrite {location}?", MessageBoxButton.YesNoCancel) switch
            {
                MessageBoxResult.Yes => "overwritten",
                MessageBoxResult.No => "kept",
                _ => "cancelled",
            };
        }

        public Task<int> PickAsync()
            => dialogs.MessageBoxAsync("Which one?", [new DialogButtonInfo { Content = "First", Result = 1 }, new DialogButtonInfo { Content = "Second", Result = (int)MessageBoxResult.No }]);
    }

    [Fact]
    public async Task DisposingTheSessionRunsTheCloseActionsOfThePluginsBeforeWaitingForTheBackgroundWork()
    {
        var plugin = new BackgroundServicePlugin();
        BackgroundService service;
        var watch = new Stopwatch();
        await using (var fixture = await StrideEditorTestSession.OpenAsync(new StrideEditorTestSessionOptions
        {
            Plugins = [plugin],
            InitializeSession = true,
        }))
        {
            service = fixture.Session.ServiceProvider.Get<BackgroundService>();
            Assert.False(service.Work.IsCompleted, "The service runs its background work while the session is open.");
            watch.Start();
        }
        watch.Stop();

        // The close action stopped the work: the teardown did not wait 30 seconds for it
        Assert.True(service.Stopped);
        Assert.True(service.Work.IsCompleted);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"The teardown took {watch.Elapsed}.");
    }

    /// <summary>
    /// A plugin with a session service that runs until the plugin leaves the session.
    /// </summary>
    private sealed class BackgroundServicePlugin : AssetsPlugin
    {
        public override void InitializeSession(ISessionPluginContext context)
        {
            var service = new BackgroundService();
            context.Services.Register(service);
            context.Session.TrackBackgroundWork(service.Work);
            context.OnClose(service.Stop);
        }
    }

    /// <summary>
    /// A session service whose background work runs until it is stopped.
    /// </summary>
    private sealed class BackgroundService
    {
        private readonly CancellationTokenSource stop = new();

        public BackgroundService()
        {
            Work = Task.Delay(Timeout.Infinite, stop.Token).ContinueWith(_ => { }, TaskScheduler.Default);
        }

        public Task Work { get; }

        public bool Stopped => stop.IsCancellationRequested;

        public void Stop() => stop.Cancel();
    }
}
