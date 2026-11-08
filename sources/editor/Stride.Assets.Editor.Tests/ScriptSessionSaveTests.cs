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
/// The script editor and the session save, as in the WPF editor: the unsaved edits of a script make it dirty, and saving the
/// session saves them, through the editor, which writes the file itself.
/// </summary>
public sealed class ScriptSessionSaveTests
{
    private const string Script = "public sealed class Player : SyncScript\n{\n    public override void Update() { }\n}\n";
    private const string Edited = "// Moves the player\n" + Script;
    private const string External = "// Changed by another program\n" + Script;

    [Fact]
    public async Task AnEditMakesTheScriptDirtyUntilTheTextIsBackToTheFile()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);

        editor.OnTextEdited(Edited);

        Assert.True(script.HasUnsavedText);
        Assert.True(script.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        Assert.True(fixture.Session.HasUnsavedChanges);
        // The editor writes the file: the session does not write the script for an edit of its text
        Assert.False(script.AssetItem.IsDirty);

        editor.OnTextEdited(Script);

        Assert.False(script.HasUnsavedText);
        Assert.False(script.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.False(fixture.Session.HasUnsavedChanges);
    }

    [Fact]
    public async Task SavingTheSessionSavesTheEditsOfTheOpenEditor()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        editor.OnTextEdited(Edited);

        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Equal(Edited, File.ReadAllText(PathOf(script)));
        Assert.Equal(Edited, script.Asset.Text);
        Assert.False(editor.IsModified);
        Assert.False(script.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.False(fixture.Session.HasUnsavedChanges);
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task SavingTheSessionKeepsTheEditsWhenTheUserKeepsAChangeOfAnotherProgram()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        var dialogs = Dialogs(fixture);
        editor.OnTextEdited(Edited);
        File.WriteAllText(PathOf(script), External);

        // The editor asks before overwriting; the user keeps the file of the other program
        dialogs.Answer = MessageBoxResult.Cancel;
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Single(dialogs.Messages);
        Assert.Equal(External, File.ReadAllText(PathOf(script)));
        // Nothing is lost: the edits stay, and the script stays dirty
        Assert.True(editor.IsModified);
        Assert.True(script.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
    }

    [Fact]
    public async Task SavingTheSessionTakesTheChangeOfAnotherProgramWhenTheTextHasNoEdit()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        File.WriteAllText(PathOf(script), External);

        // The editor did not get the focus since: only the session save sees the change
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Equal(External, script.Asset.Text);
        Assert.Equal(External, editor.Text);
        Assert.Equal(External, File.ReadAllText(PathOf(script)));
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task SavingARenamedScriptKeepsTheChangeOfAnotherProgram()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        var oldPath = PathOf(script);
        File.WriteAllText(oldPath, External);

        script.Name = "Hero";
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.NotEqual(oldPath, PathOf(script));
        Assert.False(File.Exists(oldPath));
        Assert.Equal(External, File.ReadAllText(PathOf(script)));
        Assert.Equal(External, editor.Text);
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SavingARenamedScriptWithoutAnEditorKeepsTheChangeOfAnotherProgram(bool openAndCloseTheEditorFirst)
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var script = fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single();
        if (openAndCloseTheEditorFirst)
        {
            OpenEditor(fixture);
            Assert.True(await fixture.Session.EditorCollection.CloseAssetEditorAsync(script, false));
        }
        var oldPath = PathOf(script);
        File.WriteAllText(oldPath, External);

        script.Name = "Hero";
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.False(File.Exists(oldPath));
        Assert.Equal(External, File.ReadAllText(PathOf(script)));
        Assert.Equal(External, script.Asset.Text);
    }

    [Fact]
    public async Task EachChangeOfAnotherProgramIsTakenAtTheNextSave()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var script = fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single();
        File.WriteAllText(PathOf(script), External);
        Assert.True(await fixture.Session.SaveSessionAsync());
        Assert.Equal(External, script.Asset.Text);

        // A second change after the save, then a rename
        const string again = "// Changed again\n" + Script;
        File.WriteAllText(PathOf(script), again);
        File.SetLastWriteTimeUtc(PathOf(script), DateTime.UtcNow.AddSeconds(5));
        script.Name = "Hero";
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Equal(again, File.ReadAllText(PathOf(script)));
    }

    [Fact]
    public async Task ARenameBeforeTheSaveKeepsTheText()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        var reloads = new List<string>();
        editor.TextReloaded += (_, text) => reloads.Add(text);

        // The file keeps its old name until the session saves: the editor gets the focus in between
        script.Name = "Hero";
        editor.CheckExternalChanges();

        Assert.Empty(reloads);
        Assert.Equal(Script, editor.Text);
        Assert.Equal(Script, script.Asset.Text);

        Assert.True(await fixture.Session.SaveSessionAsync());
        editor.CheckExternalChanges();

        Assert.Empty(reloads);
        Assert.Equal(Script, File.ReadAllText(PathOf(script)));
        Assert.Equal(Script, editor.Text);
    }

    [Fact]
    public async Task SavingTheSessionSavesTheEditsOfARenamedScriptToTheNewFile()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        var oldPath = PathOf(script);
        editor.OnTextEdited(Edited);

        script.Name = "Hero";
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.False(File.Exists(oldPath));
        Assert.Equal(Edited, File.ReadAllText(PathOf(script)));
        Assert.False(editor.IsModified);
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task EditsAfterASavedRenameAreSavedToTheNewFile()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        script.Name = "Hero";
        Assert.True(await fixture.Session.SaveSessionAsync());

        editor.OnTextEdited(Edited);
        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.Equal(Edited, File.ReadAllText(PathOf(script)));
        Assert.False(editor.IsModified);
        Assert.Empty(Dialogs(fixture).Messages);
    }

    [Fact]
    public async Task AScriptThatCannotBeWrittenFailsTheSessionSave()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Scripts/Player", new ScriptSourceFileAsset { Text = Script }));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var (script, editor) = OpenEditor(fixture);
        editor.OnTextEdited(Edited);

        // The folder of the script cannot be made again: a file took its place
        var folder = Path.GetDirectoryName(PathOf(script))!;
        Directory.Delete(folder, true);
        File.WriteAllText(folder, string.Empty);

        Assert.False(await fixture.Session.SaveSessionAsync());

        Assert.True(editor.IsModified);
        Assert.True(script.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        // The progress window keeps the error of the script for the user
        Assert.True(Dialogs(fixture).ProgressWindows.Last().ShouldStayOpen());
    }

    [Fact]
    public async Task ClosingTheSessionAsksOnceForTheEditsOfTheScript()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        var dialogs = Dialogs(fixture);
        editor.OnTextEdited(Edited);

        // The question of the session (Save, Don't save, Cancel), and Save
        dialogs.PickButton = buttons => buttons.Single(x => x.IsDefault).Result;
        Assert.True(await fixture.Session.CloseAsync());

        Assert.Single(dialogs.Messages);
        Assert.Equal(Edited, File.ReadAllText(PathOf(script)));
        // The editor has nothing left to ask
        Assert.True(await fixture.Session.EditorCollection.CloseAllEditorsAsync(false));
        Assert.Single(dialogs.Messages);
    }

    [Fact]
    public async Task DroppingTheEditsMakesTheScriptCleanAgain()
    {
        await ExclusiveDispatcherService.SwitchTo();
        await using var fixture = await OpenAsync();
        var (script, editor) = OpenEditor(fixture);
        editor.OnTextEdited(Edited);

        // Don't save: the editor closes with its edits
        Assert.True(await fixture.Session.EditorCollection.CloseAssetEditorAsync(script, false));

        Assert.False(script.HasUnsavedText);
        Assert.False(script.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.False(fixture.Session.HasUnsavedChanges);
        Assert.Equal(Script, File.ReadAllText(PathOf(script)));
    }

    private static async Task<AssetsSessionFixture> OpenAsync()
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Player", new ScriptSourceFileAsset { Text = Script }));
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        return fixture;
    }

    private static (ScriptSourceFileViewModel Script, ScriptSourceFileEditorViewModel Editor) OpenEditor(AssetsSessionFixture fixture)
    {
        var script = fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single();
        fixture.Session.EditorCollection.OpenAssetEditor(script);
        return (script, Assert.IsType<ScriptSourceFileEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor));
    }

    private static string PathOf(ScriptSourceFileViewModel script) => script.AssetItem.FullPath.ToOSPath();

    private static FakeEditorDialogService Dialogs(AssetsSessionFixture fixture) => (FakeEditorDialogService)fixture.ServiceProvider.Get<IDialogService>();
}
