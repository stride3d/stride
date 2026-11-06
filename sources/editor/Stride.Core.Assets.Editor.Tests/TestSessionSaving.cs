// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Saving the session as the WPF editor does: an edit makes the asset and the session dirty, a save writes the asset and makes
/// them clean, undoing back to the save makes them clean again, a failed save keeps them dirty, and closing a dirty session
/// asks whether to save it.
/// </summary>
public sealed class TestSessionSaving
{
    [Fact]
    public async Task EditingAnAssetMakesTheSessionDirty()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        var wood = fixture.Session.AllAssets.Single(x => x.Name == "Wood");
        var stone = fixture.Session.AllAssets.Single(x => x.Name == "Stone");
        Assert.False(fixture.Session.HasUnsavedAssets());

        Edit(fixture, wood, "oak");

        Assert.True(wood.IsDirty);
        Assert.True(wood.AssetItem.IsDirty);
        Assert.False(stone.IsDirty);
        Assert.True(fixture.Package.IsDirty);
        Assert.True(fixture.Session.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
    }

    [Fact]
    public async Task SavingWritesTheEditAndMakesTheSessionClean()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        Edit(fixture, wood, "oak");
        // The action history records the transaction asynchronously, after the asset properties changed notification
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.Transactions.Count > 0, TimeSpan.FromSeconds(10)));

        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Equal("oak", fixture.ReadAsset("Textures/Wood").Value);
        Assert.False(wood.IsDirty);
        Assert.False(fixture.Session.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
        // The edit history marks where the session was saved
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.Transactions.Last().IsSavePoint, TimeSpan.FromSeconds(10)));
        // The progress window of the opening, then the one of the save
        Assert.Equal(2, fixture.Dialogs.ProgressWindows.Count);
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task UndoingBackToTheSaveMakesTheSessionClean()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var undoRedo = fixture.Session.UndoRedoService!;
        Edit(fixture, wood, "oak");
        Assert.True(await fixture.Session.SaveSessionAsync());

        Edit(fixture, wood, "pine");
        Assert.True(wood.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());

        // Back to the text saved on disk
        undoRedo.Undo();
        Assert.Equal("oak", ((TestEditorAsset)wood.Asset).Value);
        Assert.False(wood.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());

        // Before the save: dirty again, as in WPF
        undoRedo.Undo();
        Assert.Equal("wood", ((TestEditorAsset)wood.Asset).Value);
        Assert.True(wood.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());

        undoRedo.Redo();
        Assert.False(wood.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
    }

    [Fact]
    public async Task TheUnsavedStateFollowsEditsUndoRedoAndSaves()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var undoRedo = fixture.Session.UndoRedoService!;
        var changes = 0;
        fixture.Session.PropertyChanged += (_, e) => changes += e.PropertyName == nameof(fixture.Session.HasUnsavedChanges) ? 1 : 0;
        Assert.False(fixture.Session.HasUnsavedChanges);

        Edit(fixture, wood, "oak");
        Assert.True(fixture.Session.HasUnsavedChanges);

        undoRedo.Undo();
        Assert.False(fixture.Session.HasUnsavedChanges);

        undoRedo.Redo();
        Assert.True(fixture.Session.HasUnsavedChanges);

        Assert.True(await fixture.Session.SaveSessionAsync());
        Assert.False(fixture.Session.HasUnsavedChanges);
        Assert.Equal(4, changes);
    }

    [Fact]
    public async Task AFailedSaveKeepsTheSessionDirty()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        Edit(fixture, wood, "oak");

        // The file of the asset cannot be written: a folder took its place
        var path = wood.AssetItem.FullPath.ToOSPath();
        File.Delete(path);
        Directory.CreateDirectory(path);

        Assert.False(await fixture.Session.SaveSessionAsync());

        Assert.True(wood.IsDirty);
        Assert.True(fixture.Session.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        // The progress window keeps the error for the user, as WPF's does
        var progress = fixture.Dialogs.ProgressWindows.Last();
        Assert.True(progress.WorkDone);
        Assert.True(progress.ShouldStayOpen());
    }

    [Fact]
    public async Task ClosingACleanSessionDoesNotAsk()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));

        Assert.True(await fixture.Session.CloseAsync());

        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task ClosingADirtySessionAsksSaveDontSaveOrCancel()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        Edit(fixture, wood, "oak");
        IReadOnlyCollection<DialogButtonInfo>? asked = null;

        // Cancel: the session stays open and dirty
        fixture.Dialogs.PickButton = buttons => (asked = buttons).Single(x => x.IsCancel).Result;
        Assert.False(await fixture.Session.CloseAsync());
        Assert.Equal(["Save", "Don't save", "Cancel"], asked!.Select(x => x.Content));
        Assert.True(asked!.First().IsDefault);
        Assert.True(fixture.Session.HasUnsavedAssets());
        Assert.Equal("wood", fixture.ReadAsset("Textures/Wood").Value);

        // Don't save: the session closes, the file keeps its value
        fixture.Dialogs.PickButton = buttons => buttons.Single(x => !x.IsDefault && !x.IsCancel).Result;
        Assert.True(await fixture.Session.CloseAsync());
        Assert.Equal("wood", fixture.ReadAsset("Textures/Wood").Value);

        // Save: the session closes once the file is written
        fixture.Dialogs.PickButton = buttons => buttons.Single(x => x.IsDefault).Result;
        Assert.True(await fixture.Session.CloseAsync());
        Assert.Equal("oak", fixture.ReadAsset("Textures/Wood").Value);
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.Equal(3, fixture.Dialogs.Messages.Count);
    }

    [Fact]
    public async Task ClosingAfterAFailedSaveTellsTheUserAndKeepsTheSession()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        Edit(fixture, wood, "oak");
        var path = wood.AssetItem.FullPath.ToOSPath();
        File.Delete(path);
        Directory.CreateDirectory(path);

        fixture.Dialogs.PickButton = buttons => buttons.Single(x => x.IsDefault).Result;
        Assert.False(await fixture.Session.CloseAsync());

        Assert.Equal(2, fixture.Dialogs.Messages.Count);
        Assert.Contains("couldn't be saved", fixture.Dialogs.Messages.Last());
        Assert.True(fixture.Session.HasUnsavedAssets());
    }

    /// <summary>
    /// Sets the value of the asset the way the property grid does: through its property graph, in a transaction of the history.
    /// </summary>
    private static void Edit(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update(value);
        undoRedo.SetName(transaction, "Edit " + value);
    }
}
