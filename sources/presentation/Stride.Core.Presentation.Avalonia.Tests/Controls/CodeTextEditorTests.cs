// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaEdit.Editing;
using Stride.Core.Presentation.Avalonia.Controls;
using TextMateSharp.Grammars;

namespace Stride.Core.Presentation.Avalonia.Tests.Controls;

public sealed class CodeTextEditorTests
{
    private const string Script = "public sealed class Player : SyncScript\n{\n    public override void Update() { }\n}\n";

    private static (Window Window, CodeTextEditor Editor) Show(CodeTextEditor editor)
    {
        var window = new Window { Width = 600, Height = 400, Content = editor };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return (window, editor);
    }

    [AvaloniaFact]
    public void TheEditorHasTheTemplateOfAvaloniaEdit()
    {
        var (_, editor) = Show(new CodeTextEditor());

        // The control shares the theme of AvaloniaEdit's TextEditor: without it nothing is drawn
        Assert.NotNull(editor.GetVisualDescendants().OfType<TextArea>().SingleOrDefault());
        Assert.True(editor.ShowLineNumbers);
        Assert.False(editor.WordWrap);
    }

    [AvaloniaFact]
    public void LoadingATextRaisesNoEditAndCannotBeUndone()
    {
        var (_, editor) = Show(new CodeTextEditor());
        var edits = new List<string>();
        editor.TextEdited += (_, text) => edits.Add(text);

        editor.LoadText(Script);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(Script, editor.Text);
        Assert.Empty(edits);
        Assert.False(editor.CanUndo);
    }

    [AvaloniaFact]
    public void TypingRaisesAnEditWithTheNewText()
    {
        var (window, editor) = Show(new CodeTextEditor());
        editor.LoadText(Script);
        var edits = new List<string>();
        editor.TextEdited += (_, text) => edits.Add(text);

        editor.TextArea.Focus();
        editor.CaretOffset = 0;
        window.KeyTextInput("// ");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("// " + Script, editor.Text);
        Assert.Equal("// " + Script, edits.Last());
        Assert.True(editor.CanUndo);
    }

    [AvaloniaFact]
    public void ANullTextLoadsAnEmptyDocument()
    {
        var (_, editor) = Show(new CodeTextEditor());
        editor.LoadText(Script);

        editor.LoadText(null);

        Assert.Equal(string.Empty, editor.Text);
    }

    [AvaloniaFact]
    public void TheHighlightingFollowsTheThemeVariant()
    {
        var (window, editor) = Show(new CodeTextEditor());
        editor.LoadText(Script);
        Dispatcher.UIThread.RunJobs();
        var initial = editor.HighlightingTheme;
        Assert.Equal(window.ActualThemeVariant == ThemeVariant.Dark ? ThemeName.DarkPlus : ThemeName.LightPlus, initial);

        window.RequestedThemeVariant = initial == ThemeName.LightPlus ? ThemeVariant.Dark : ThemeVariant.Light;
        Dispatcher.UIThread.RunJobs();

        Assert.NotEqual(initial, editor.HighlightingTheme);
        Assert.Equal(window.ActualThemeVariant == ThemeVariant.Dark ? ThemeName.DarkPlus : ThemeName.LightPlus, editor.HighlightingTheme);
    }
}
