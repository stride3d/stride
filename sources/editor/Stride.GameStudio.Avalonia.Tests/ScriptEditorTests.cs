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
/// The script editor in the shell: a script opens in the code editor, the edits of the user are saved with Ctrl+S, and a change
/// that another program makes to the file shows when the editor gets the focus back.
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
