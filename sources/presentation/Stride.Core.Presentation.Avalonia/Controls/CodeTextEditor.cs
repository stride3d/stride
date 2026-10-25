// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using TextMateSharp.Grammars;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// A code editor: AvaloniaEdit's <see cref="TextEditor"/> with line numbers and the C# highlighting of the TextMate grammars,
/// in the colors of the theme variant of the application (Dark+ or Light+).
/// </summary>
/// <remarks>
/// The application includes AvaloniaEdit's theme (<c>avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml</c>), which
/// this control shares.
/// </remarks>
public sealed class CodeTextEditor : TextEditor
{
    private readonly RegistryOptions registryOptions;
    private readonly TextMate.Installation textMate;
    private bool isLoadingText;

    public CodeTextEditor()
    {
        ShowLineNumbers = true;
        WordWrap = false;
        registryOptions = new RegistryOptions(HighlightingTheme);
        textMate = this.InstallTextMate(registryOptions);
        textMate.SetGrammar(registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".cs").Id));
        TextChanged += OnTextChanged;
    }

    /// <summary>
    /// Occurs when the user edits the text, with the new text; <see cref="LoadText"/> does not raise it.
    /// </summary>
    public event EventHandler<string>? TextEdited;

    /// <summary>
    /// Gets the colors of the highlighting, which follow the theme variant of the control.
    /// </summary>
    public ThemeName HighlightingTheme { get; private set; } = ThemeName.DarkPlus;

    /// <inheritdoc />
    protected override Type StyleKeyOverride => typeof(TextEditor);

    /// <summary>
    /// Shows a text, such as the content of a file: it raises no <see cref="TextEdited"/> and cannot be undone.
    /// </summary>
    /// <param name="text">The text to show; <c>null</c> empties the editor.</param>
    public void LoadText(string? text)
    {
        isLoadingText = true;
        try
        {
            Document.Text = text ?? string.Empty;
            Document.UndoStack.ClearAll();
        }
        finally
        {
            isLoadingText = false;
        }
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The variant inherited from the tree raises no change when it is the default one
        ApplyThemeVariant(ActualThemeVariant);
    }

    /// <inheritdoc />
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ThemeVariantScope.ActualThemeVariantProperty)
            ApplyThemeVariant(ActualThemeVariant);
    }

    private void ApplyThemeVariant(ThemeVariant variant)
    {
        var theme = variant == ThemeVariant.Dark ? ThemeName.DarkPlus : ThemeName.LightPlus;
        if (theme == HighlightingTheme)
            return;

        HighlightingTheme = theme;
        textMate.SetTheme(registryOptions.LoadTheme(theme));
    }

    private void OnTextChanged(object? sender, EventArgs e)
    {
        if (!isLoadingText)
            TextEdited?.Invoke(this, Text);
    }
}
