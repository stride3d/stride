// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Scripts;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The script editor in the shell: a script opens in the code editor, the edits of the user are saved with Ctrl+S, a change
/// that another program makes to the file shows when the editor gets the focus back, and closing the window or the session
/// asks the user about the unsaved edits.
/// </summary>
public sealed class ScriptEditorTests
{
    private const string Script = "public sealed class Player : SyncScript\n{\n    public override void Update() { }\n}\n";

    [AvaloniaFact]
    public async Task OpeningAScriptShowsItsTextInTheCodeEditor()
    {
        using var fixture = await OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var (editor, view) = await OpenEditorAsync(fixture, window);

            Assert.Equal(Script, CodeEditor(view).Text);
            Assert.False(editor.IsModified);
            Assert.False(SaveButton(view).IsEffectivelyEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task TypingEnablesTheSaveAndCtrlSWritesTheFile()
    {
        using var fixture = await OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var (editor, view) = await OpenEditorAsync(fixture, window);
            var code = CodeEditor(view);
            code.TextArea.Focus();
            code.CaretOffset = 0;

            window.KeyTextInput("// ");
            await WindowTestHelper.PumpAsync();
            Assert.True(editor.IsModified);
            Assert.True(SaveButton(view).IsEffectivelyEnabled);

            window.KeyPress(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            window.KeyRelease(Key.S, RawInputModifiers.Control, PhysicalKey.S, "s");
            await WindowTestHelper.WaitUntilAsync(() => !editor.IsModified, "the save of the script");

            Assert.Equal("// " + Script, File.ReadAllText(editor.Asset.AssetItem.FullPath.ToOSPath()));
            Assert.False(SaveButton(view).IsEffectivelyEnabled);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnExternalChangeShowsWhenTheEditorGetsTheFocus()
    {
        using var fixture = await OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var (editor, view) = await OpenEditorAsync(fixture, window);
            var code = CodeEditor(view);
            const string external = "// Changed by another program\n" + Script;

            File.WriteAllText(editor.Asset.AssetItem.FullPath.ToOSPath(), external);
            code.TextArea.Focus();
            await WindowTestHelper.PumpAsync();

            Assert.Equal(external, code.Text);
            Assert.False(editor.IsModified);
            // A reload is not an edit of the user
            Assert.False(code.CanUndo);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task AnExternalChangeShowsWhenTheWindowIsActivatedAgain()
    {
        using var fixture = await OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var (editor, view) = await OpenEditorAsync(fixture, window);
            var code = CodeEditor(view);
            code.TextArea.Focus();
            await WindowTestHelper.PumpAsync();
            const string external = "// Changed by another program\n" + Script;

            // The user comes back from another program: the focus stays in the editor, the window is activated
            File.WriteAllText(editor.Asset.AssetItem.FullPath.ToOSPath(), external);
            window.Activate();
            await WindowTestHelper.PumpAsync();

            Assert.Equal(external, code.Text);
            Assert.False(editor.IsModified);
        }
        finally
        {
            await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ClosingTheWindowSavesTheEditsWhenTheUserSaysSo()
    {
        using var fixture = await OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        try
        {
            var (editor, view) = await TypeAsync(fixture, window, "// ");
            // The main view model forgets the session when the editor exits
            var session = fixture.Session;

            // The first button saves; the window closes once the file is written
            await window.CloseAsync();
            await WindowTestHelper.WaitUntilAsync(() => closed, "the close of the window");

            Assert.Single(dialogs.Messages);
            Assert.Equal("// " + Script, File.ReadAllText(editor.Asset.AssetItem.FullPath.ToOSPath()));
            Assert.Empty(session.EditorCollection.Editors);
        }
        finally
        {
            if (!closed)
                await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task CancellingTheCloseKeepsTheWindowAndTheEdits()
    {
        using var fixture = await OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        try
        {
            var (editor, view) = await TypeAsync(fixture, window, "// ");

            dialogs.PickButton = buttons => buttons.Single(x => x.IsCancel).Result;
            await window.CloseAsync();

            Assert.Single(dialogs.Messages);
            Assert.False(closed);
            Assert.Same(editor, fixture.Session.EditorCollection.ActiveEditor);
            Assert.True(editor.IsModified);
            Assert.Equal("// " + Script, CodeEditor(view).Text);
            Assert.Equal(Script, File.ReadAllText(editor.Asset.AssetItem.FullPath.ToOSPath()));

            // Don't save: the window closes and the file keeps its text
            dialogs.PickButton = buttons => buttons.Single(x => !x.IsDefault && !x.IsCancel).Result;
            await window.CloseAsync();
            await WindowTestHelper.WaitUntilAsync(() => closed, "the close of the window");
            Assert.Equal(Script, File.ReadAllText(editor.Asset.AssetItem.FullPath.ToOSPath()));
        }
        finally
        {
            if (!closed)
                await window.CloseAsync();
        }
    }

    [AvaloniaFact]
    public async Task ClosingTheSessionAsksAboutTheEditsToo()
    {
        using var fixture = await OpenAsync();
        var dialogs = fixture.ReplaceDialogService();
        var window = new MainWindow { DataContext = fixture.Main };
        window.Show();
        try
        {
            var (editor, _) = await TypeAsync(fixture, window, "// ");

            // File > Close, then Cancel
            dialogs.PickButton = buttons => buttons.Single(x => x.IsCancel).Result;
            fixture.Main.CloseCommand.Execute();
            await WindowTestHelper.PumpAsync();

            Assert.Single(dialogs.Messages);
            Assert.Same(fixture.Session, fixture.Main.Session);
            Assert.Same(editor, fixture.Session.EditorCollection.ActiveEditor);
            Assert.True(editor.IsModified);
        }
        finally
        {
            dialogs.PickButton = buttons => buttons.Single(x => !x.IsDefault && !x.IsCancel).Result;
            await window.CloseAsync();
        }
    }

    /// <summary>
    /// Opens the editor of the script and types a text at its start.
    /// </summary>
    private static async Task<(ScriptSourceFileEditorViewModel Editor, ScriptSourceFileEditorView View)> TypeAsync(ShellSessionFixture fixture, Window window, string text)
    {
        var (editor, view) = await OpenEditorAsync(fixture, window);
        var code = CodeEditor(view);
        code.TextArea.Focus();
        code.CaretOffset = 0;
        window.KeyTextInput(text);
        await WindowTestHelper.PumpAsync();
        Assert.True(editor.IsModified);
        return (editor, view);
    }

    private static Task<ShellSessionFixture> OpenAsync()
    {
        return ShellSessionFixture.OpenAsync(("Player", new ScriptSourceFileAsset { Text = Script }));
    }

    /// <summary>
    /// Opens the editor of the script of the session, the way a double-click in the asset view does.
    /// </summary>
    private static async Task<(ScriptSourceFileEditorViewModel Editor, ScriptSourceFileEditorView View)> OpenEditorAsync(ShellSessionFixture fixture, Window window)
    {
        // A script is a source file of the project, which the session names after its path in the project
        var script = fixture.Session.AllAssets.OfType<ScriptSourceFileViewModel>().Single();
        fixture.Session.AssetCollection.SelectAssets([script]);
        await WindowTestHelper.PumpAsync();
        fixture.Session.EditSelectedContentCommand.Execute();
        await WindowTestHelper.PumpAsync();

        var editor = Assert.IsType<ScriptSourceFileEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
        var view = window.GetVisualDescendants().OfType<ScriptSourceFileEditorView>().Single();
        return (editor, view);
    }

    private static CodeTextEditor CodeEditor(ScriptSourceFileEditorView view) => view.GetVisualDescendants().OfType<CodeTextEditor>().Single();

    private static Button SaveButton(ScriptSourceFileEditorView view) => view.GetVisualDescendants().OfType<Button>().Single(x => x.Name == "SaveButton");
}
