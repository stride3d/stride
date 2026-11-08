// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Quantum.Presenters;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The paste and replace commands of the property grid: they check the content of the clipboard when it answers at once, and never
/// wait for a clipboard that answers later (the property grid evaluates them on the UI thread).
/// </summary>
public sealed class TestPropertyPaste
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    [Fact]
    public async Task PasteIsEnabledOnlyForCopiedPropertiesWhenTheClipboardAnswersAtOnce()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Oak", "oak"));
        var target = await CopyWoodAndSelectOakAsync(fixture);

        Assert.True(new PastePropertyCommand().CanExecute(target, null));
        Assert.True(new ReplacePropertyCommand().CanExecute(target, null));

        // A list cannot be pasted into a text property
        fixture.Clipboard.Text = await CopyListAsync(fixture);
        Assert.False(new PastePropertyCommand().CanExecute(target, null));
        Assert.False(new ReplacePropertyCommand().CanExecute(target, null));
    }

    [Fact]
    public async Task PasteDoesNotWaitForTheClipboardAndPastesWhatItAnswers()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Oak", "oak"));
        var target = await CopyWoodAndSelectOakAsync(fixture);
        var copied = fixture.Clipboard.Text;
        var answer = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Clipboard.PendingRead = answer.Task;
        var command = new PastePropertyCommand();

        // Waiting for the answer here would never end on the UI thread: the command stays enabled, the paste checks the text
        Assert.True(await Task.Run(() => command.CanExecute(target, null)).WaitAsync(Timeout));

        var paste = command.Execute(target.Single(), null, null);
        answer.SetResult(copied);
        await paste.WaitAsync(Timeout);

        Assert.Equal("wood", Value(fixture, "Textures/Oak"));
    }

    [Fact]
    public async Task APasteOfTextThatCannotBePastedChangesNothing()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Oak", "oak"));
        var target = await CopyWoodAndSelectOakAsync(fixture);
        var list = await CopyListAsync(fixture);
        var answer = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Clipboard.PendingRead = answer.Task;
        var undoRedo = fixture.Session.UndoRedoService!;
        var transactions = 0;
        undoRedo.Done += (_, e) => transactions += e.Transaction.Operations.Count > 0 ? 1 : 0;

        var paste = new ReplacePropertyCommand().Execute(target.Single(), null, null);
        answer.SetResult(list);
        await paste.WaitAsync(Timeout);

        Assert.Equal("oak", Value(fixture, "Textures/Oak"));
        Assert.Equal(0, transactions);
    }

    /// <summary>
    /// Copies the value of the wood asset to the clipboard, and returns the node presenters of the value of the oak asset.
    /// </summary>
    private static async Task<IReadOnlyCollection<INodePresenter>> CopyWoodAndSelectOakAsync(EditorSessionFixture fixture)
    {
        var source = await ValuePresentersAsync(fixture, "Textures/Wood");
        await new CopyPropertyCommand().Execute(source.Single(), null, null);
        Assert.False(string.IsNullOrEmpty(fixture.Clipboard.Text));
        return await ValuePresentersAsync(fixture, "Textures/Oak");
    }

    /// <summary>
    /// Copies a list property, which cannot be pasted into the text property of the test assets, and returns the copied text.
    /// </summary>
    private static async Task<string> CopyListAsync(EditorSessionFixture fixture)
    {
        var asset = new TestListEditorAsset();
        asset.Reorderable.Add("item");
        var lists = fixture.Package.CreateAsset(fixture.Package.AssetMountPoint, new AssetItem("Lists", asset), false, null);
        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([lists]);
        var presenters = graph!.RootNode.GetChild(nameof(TestListEditorAsset.Reorderable))!.NodePresenters;
        var previous = fixture.Clipboard.Text;
        await new CopyPropertyCommand().Execute(presenters.Single(), null, null);
        var text = fixture.Clipboard.Text;
        Assert.False(string.IsNullOrEmpty(text));
        fixture.Clipboard.Text = previous;
        return text;
    }

    private static async Task<IReadOnlyCollection<INodePresenter>> ValuePresentersAsync(EditorSessionFixture fixture, string location)
    {
        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([Asset(fixture, location)]);
        return graph!.RootNode.GetChild(nameof(TestEditorAsset.Value))!.NodePresenters;
    }

    private static string? Value(EditorSessionFixture fixture, string location) => ((TestEditorAsset)Asset(fixture, location).Asset).Value;

    private static AssetViewModel Asset(EditorSessionFixture fixture, string location)
    {
        return fixture.Session.AllAssets.Single(x => x.Url.EndsWith(location, StringComparison.Ordinal));
    }
}
