// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The edit history of the session enables Undo and Redo as soon as an edit, undo or redo is done, without waiting for the
/// subscribers of the asset changes (thumbnails, previews, references, the editor game), which can take long.
/// </summary>
public sealed class ActionHistoryViewModelTests
{
    [Fact]
    public async Task UndoIsEnabledBeforeTheAssetChangesAreHandled()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var history = fixture.Session.ActionHistory;
        using var subscribers = new BlockedSubscriber(fixture.Session);

        subscribers.Block();
        Edit(fixture, wood, "oak");

        Assert.True(history.UndoCommand.IsEnabled);
        Assert.Equal("Edit oak", Assert.Single(history.Transactions).Name);
    }

    [Fact]
    public async Task RedoIsEnabledBeforeTheAssetChangesOfTheUndoAreHandled()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var history = fixture.Session.ActionHistory;
        using var subscribers = new BlockedSubscriber(fixture.Session);
        Edit(fixture, wood, "oak");
        await ExclusiveDispatcherService.WaitUntilAsync(() => history.UndoCommand.IsEnabled, "the edit in the history");

        subscribers.Block();
        fixture.Session.UndoRedoService!.Undo();

        Assert.True(history.RedoCommand.IsEnabled);
        Assert.False(history.UndoCommand.IsEnabled);
        Assert.False(Assert.Single(history.Transactions).IsDone);
    }

    private static void Edit(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update(value);
        undoRedo.SetName(transaction, "Edit " + value);
    }

    /// <summary>
    /// A subscriber of the asset changes that, once blocked, waits until it is disposed, as a slow thumbnail or preview would.
    /// </summary>
    private sealed class BlockedSubscriber : IDisposable
    {
        private readonly ISessionViewModel session;
        private readonly ManualResetEventSlim gate = new(true);

        public BlockedSubscriber(ISessionViewModel session)
        {
            this.session = session;
            session.AssetPropertiesChanged += Wait;
        }

        public void Block() => gate.Reset();

        public void Dispose()
        {
            session.AssetPropertiesChanged -= Wait;
            gate.Set();
        }

        private void Wait(object? sender, AssetChangedEventArgs e) => gate.Wait();
    }
}
