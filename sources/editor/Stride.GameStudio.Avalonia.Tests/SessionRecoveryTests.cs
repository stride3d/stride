// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor;
using Stride.Core.Assets.Editor.Components.Recovery;
using Stride.Core.Assets.Editor.Settings;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.ViewModels;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The recovery copies of the session in the shell: the question asked when an editor that closed unexpectedly left some (restore,
/// discard, decide later), the autosave settings, and the sign of the status bar.
/// </summary>
public sealed class SessionRecoveryTests
{
    private const string AssetLocation = "Text";

    [AvaloniaFact]
    public async Task RestoringBringsBackTheChangesAsUnsaved()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        var folder = WriteCopiesOfAGoneEditor(fixture, "recovered");
        var dialogs = fixture.ReplaceDialogService();
        var buttons = new List<int>();
        dialogs.PickButton = x => { buttons.AddRange(x.Select(b => b.Result)); return SessionRecoveryViewModel.RestoreResult; };

        await fixture.OpenAsync();
        var asset = fixture.Asset(AssetLocation);
        await WindowTestHelper.WaitUntilAsync(() => Label(asset) == "recovered", "the restore of the copies");

        Assert.Contains("closed unexpectedly", Assert.Single(dialogs.Messages));
        Assert.Equal([SessionRecoveryViewModel.RestoreResult, SessionRecoveryViewModel.DiscardResult, SessionRecoveryViewModel.LaterResult], buttons);
        Assert.True(asset.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        // The project is not written, and the copies are this editor's now
        Assert.Equal("saved", ReadLabel(fixture));
        await WindowTestHelper.WaitUntilAsync(() => !Directory.Exists(folder), "the removal of the old copies");
        Assert.True(File.Exists(Path.Combine(fixture.Main.Recovery!.Service.ProcessFolder!, RecoveryStore.ManifestFileName)));
    }

    [AvaloniaFact]
    public async Task DiscardingRemovesTheCopies()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        var folder = WriteCopiesOfAGoneEditor(fixture, "recovered");
        var dialogs = fixture.ReplaceDialogService();
        dialogs.PickButton = _ => SessionRecoveryViewModel.DiscardResult;

        await fixture.OpenAsync();
        await WindowTestHelper.WaitUntilAsync(() => !Directory.Exists(folder), "the removal of the copies");

        Assert.Single(dialogs.Messages);
        var asset = fixture.Asset(AssetLocation);
        Assert.Equal("saved", Label(asset));
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.Null(fixture.Main.Recovery!.Service.FindRecovery());
    }

    [AvaloniaFact]
    public async Task DecidingLaterKeepsTheCopiesForTheNextTime()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        var folder = WriteCopiesOfAGoneEditor(fixture, "recovered");
        var dialogs = fixture.ReplaceDialogService();
        dialogs.PickButton = _ => SessionRecoveryViewModel.LaterResult;

        try
        {
            await fixture.OpenAsync();
            await WindowTestHelper.PumpAsync();

            Assert.Single(dialogs.Messages);
            Assert.Equal("saved", Label(fixture.Asset(AssetLocation)));
            Assert.False(fixture.Session.HasUnsavedAssets());
            Assert.True(Directory.Exists(folder));
            Assert.NotNull(fixture.Main.Recovery!.Service.FindRecovery());
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    [AvaloniaFact]
    public async Task NoQuestionWithoutCopiesAndACleanCloseRemovesThem()
    {
        using var fixture = ShellSessionFixture.Create((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        var dialogs = fixture.ReplaceDialogService();
        await fixture.OpenAsync();
        var recovery = fixture.Main.Recovery!;
        Assert.Empty(dialogs.Messages);

        Edit(fixture, fixture.Asset(AssetLocation), "edited");
        Assert.True(await recovery.Service.WriteCopiesAsync());
        var folder = recovery.Service.ProcessFolder!;
        Assert.True(Directory.Exists(folder));

        // Don't save
        dialogs.PickButton = _ => 2;
        Assert.True(await fixture.Main.CloseSessionAsync());
        fixture.Main.CloseSessionServices();

        Assert.False(Directory.Exists(folder));
        Assert.Null(fixture.Main.Recovery);
        Assert.Equal("saved", ReadLabel(fixture));
    }

    [AvaloniaFact]
    public async Task TheSettingsTurnTheCopiesOffAndSetTheirInterval()
    {
        var profile = EditorSettings.SettingsContainer.CurrentProfile;
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        var service = fixture.Main.Recovery!.Service;
        try
        {
            Assert.True(service.IsEnabled);
            Assert.Equal(TimeSpan.FromMinutes(5), service.Interval);

            GameStudioEditorSettings.AutosaveIntervalMinutes.SetValue(1, profile);
            GameStudioEditorSettings.AutosaveEnabled.SetValue(false, profile);
            profile.ValidateSettingsChanges();

            Assert.Equal(TimeSpan.FromMinutes(1), service.Interval);
            Assert.False(service.IsEnabled);

            // Less than a minute is a minute
            GameStudioEditorSettings.AutosaveIntervalMinutes.SetValue(0, profile);
            profile.ValidateSettingsChanges();
            Assert.Equal(TimeSpan.FromMinutes(1), service.Interval);
        }
        finally
        {
            GameStudioEditorSettings.AutosaveIntervalMinutes.SetValue(5, profile);
            GameStudioEditorSettings.AutosaveEnabled.SetValue(true, profile);
            profile.ValidateSettingsChanges();
        }
        Assert.True(service.IsEnabled);

        // The settings window lists them under their category
        var settings = new SettingsViewModel(fixture.ServiceProvider, profile);
        try
        {
            var category = Assert.Single(settings.Categories, x => x.Name == GameStudioEditorSettings.Autosave);
            Assert.NotNull(category);
        }
        finally
        {
            settings.Destroy();
        }
    }

    [AvaloniaFact]
    public async Task TheStatusBarShowsWhenTheLastCopyWasWritten()
    {
        using var fixture = await ShellSessionFixture.OpenAsync((AssetLocation, new TextPropertiesAsset { Label = "saved" }));
        fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        await window.ShowAsync();
        try
        {
            var sign = window.GetVisualDescendants().OfType<TextBlock>().Single(x => x.Name == "RecoveryStatus");
            Assert.False(sign.IsVisible);

            Edit(fixture, fixture.Asset(AssetLocation), "edited");
            Assert.True(await fixture.Main.Recovery!.Service.WriteCopiesAsync());
            await WindowTestHelper.PumpAsync();

            Assert.True(sign.IsVisible);
            Assert.StartsWith("Recovery copy at ", sign.Text);

            // Saved: the copies are gone, and the sign with them
            Assert.True(await fixture.Session.SaveSessionAsync());
            await WindowTestHelper.PumpAsync();
            Assert.False(sign.IsVisible);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// Writes the recovery copies that an editor which is gone left for the session of the fixture: the asset with another label.
    /// </summary>
    /// <returns>The folder of that editor.</returns>
    private static string WriteCopiesOfAGoneEditor(ShellSessionFixture fixture, string label)
    {
        var assetPath = Path.Combine(fixture.PackageDirectory, "Assets", AssetLocation + ".sdtextpropertiestest");
        var asset = (TextPropertiesAsset)AssetFileSerializer.Load<TextPropertiesAsset>(assetPath).Asset;
        asset.Label = label;
        using var stream = new MemoryStream();
        AssetFileSerializer.Save(stream, asset, null);

        var store = new RecoveryStore(EditorPath.RecoveryPath, fixture.PackagePath.ToOSPath());
        // No process has this identifier
        var folder = store.WriteMarker(new RecoveryMarker { ProcessId = int.MaxValue, ProcessStartTicks = 1, SessionPath = store.SessionPath });
        var manifest = new RecoveryManifest
        {
            Time = DateTime.UtcNow,
            Packages =
            {
                new RecoveredPackage
                {
                    Name = ShellSessionFixture.PackageName,
                    Path = fixture.PackagePath.ToOSPath(),
                    Assets = { new RecoveredAsset { Id = (Guid)asset.Id, Location = AssetLocation, FilePath = "Assets/" + AssetLocation + ".sdtextpropertiestest", File = 0 } },
                },
            },
        };
        store.WriteCopies(folder, manifest, [stream.ToArray()]);
        return folder;
    }

    private static string? Label(Core.Assets.Presentation.ViewModels.AssetViewModel asset) => ((TextPropertiesAsset)asset.Asset).Label;

    private static string? ReadLabel(ShellSessionFixture fixture)
    {
        var assetPath = Path.Combine(fixture.PackageDirectory, "Assets", AssetLocation + ".sdtextpropertiestest");
        return ((TextPropertiesAsset)AssetFileSerializer.Load<TextPropertiesAsset>(assetPath).Asset).Label;
    }

    private static void Edit(ShellSessionFixture fixture, Core.Assets.Presentation.ViewModels.AssetViewModel asset, string label)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph!.RootNode[nameof(TextPropertiesAsset.Label)].Update(label);
        undoRedo.SetName(transaction, "Edit " + label);
    }
}
