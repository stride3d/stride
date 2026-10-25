// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Services;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The script editor (<see cref="ScriptSourceFileEditorViewModel"/>): the edits of its text, its save to the file, and the
/// changes that another program makes to the file.
/// </summary>
public sealed class ScriptSourceFileEditorTests
{
    private const string Script = "public sealed class Player : SyncScript\n{\n    public override void Update() { }\n}\n";
    private const string Edited = "// Moves the player\n" + Script;
    private const string External = "// Changed by another program\n" + Script;

    [Fact]
    public async Task TheEditorShowsTheTextOfTheScript()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);

        Assert.Equal(Script, editor.Text);
        Assert.False(editor.IsModified);
        Assert.False(editor.SaveCommand.IsEnabled);
    }

    [Fact]
    public async Task AnEditEnablesTheSaveUntilTheTextIsBackToTheFile()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);

        editor.OnTextEdited(Edited);
        Assert.True(editor.IsModified);
        Assert.True(editor.SaveCommand.IsEnabled);

        editor.OnTextEdited(Script);
        Assert.False(editor.IsModified);
        Assert.False(editor.SaveCommand.IsEnabled);
    }

    [Fact]
    public async Task SavingWritesTheFileAndTheScript()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);
        editor.OnTextEdited(Edited);

        await editor.SaveAsync();

        Assert.Equal(Edited, File.ReadAllText(PathOf(editor)));
        Assert.Equal(Edited, editor.Asset.Asset.Text);
        Assert.False(editor.IsModified);
        Assert.False(editor.SaveCommand.IsEnabled);
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task AnExternalChangeIsReloadedWhenTheTextHasNoEdit()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);
        var reloads = new List<string>();
        editor.TextReloaded += (_, text) => reloads.Add(text);

        File.WriteAllText(PathOf(editor), External);
        editor.CheckExternalChanges();

        Assert.Equal([External], reloads);
        Assert.Equal(External, editor.Text);
        Assert.Equal(External, editor.Asset.Asset.Text);
        Assert.False(editor.IsModified);
    }

    [Fact]
    public async Task AnExternalChangeDoesNotReplaceTheEdits()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);
        var reloads = new List<string>();
        editor.TextReloaded += (_, text) => reloads.Add(text);
        editor.OnTextEdited(Edited);

        File.WriteAllText(PathOf(editor), External);
        editor.CheckExternalChanges();

        Assert.Empty(reloads);
        Assert.Equal(Edited, editor.Text);
        Assert.True(editor.IsModified);
    }

    [Fact]
    public async Task SavingOverAnExternalChangeAsksFirst()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);
        var dialogs = Dialogs(fixture);
        editor.OnTextEdited(Edited);
        File.WriteAllText(PathOf(editor), External);

        // The user keeps the change of the other program
        dialogs.Answer = MessageBoxResult.Cancel;
        await editor.SaveAsync();
        Assert.Single(dialogs.Messages);
        Assert.Equal(External, File.ReadAllText(PathOf(editor)));
        Assert.True(editor.IsModified);

        // The user overwrites it
        dialogs.Answer = MessageBoxResult.OK;
        await editor.SaveAsync();
        Assert.Equal(2, dialogs.Messages.Count);
        Assert.Equal(Edited, File.ReadAllText(PathOf(editor)));
        Assert.False(editor.IsModified);
    }

    [Fact]
    public async Task SavingWithoutEditDoesNothing()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);
        File.WriteAllText(PathOf(editor), External);

        await editor.SaveAsync();

        Assert.Equal(External, File.ReadAllText(PathOf(editor)));
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task TheFontSizeStaysInItsRange()
    {
        using var fixture = await OpenAsync();
        var editor = NewEditor(fixture);

        editor.FontSize = 2;
        Assert.Equal(ScriptSourceFileEditorViewModel.MinimumFontSize, editor.FontSize);
        editor.FontSize = 200;
        Assert.Equal(ScriptSourceFileEditorViewModel.MaximumFontSize, editor.FontSize);
        editor.FontSize = 16;
        Assert.Equal(16, editor.FontSize);
    }

    private static async Task<AssetsSessionFixture> OpenAsync()
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Player", new ScriptSourceFileAsset { Text = Script }));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        return fixture;
    }

    private static ScriptSourceFileEditorViewModel NewEditor(AssetsSessionFixture fixture)
    {
        // A script is a source file of the project, which the session names after its path in the project
        return new ScriptSourceFileEditorViewModel(fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single());
    }

    private static string PathOf(ScriptSourceFileEditorViewModel editor) => editor.Asset.AssetItem.FullPath.ToOSPath();

    private static FakeEditorDialogService Dialogs(AssetsSessionFixture fixture) => (FakeEditorDialogService)fixture.ServiceProvider.Get<IDialogService>();
}
