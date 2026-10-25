// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The view of the script editor: a code editor that shows the text of the view model and reports the edits of the user to it,
/// and reloads the file when another program changed it (checked when the view shows and when the editor gets the focus).
/// </summary>
[AssetEditorView<ScriptSourceFileEditorViewModel>]
public sealed partial class ScriptSourceFileEditorView : UserControl, IAssetEditorView
{
    private ScriptSourceFileEditorViewModel? viewModel;

    public ScriptSourceFileEditorView()
    {
        InitializeComponent();
        Editor.TextEdited += OnEditorTextEdited;
        // The text area of the editor handles the focus event it raises
        Editor.AddHandler(GotFocusEvent, OnEditorGotFocus, handledEventsToo: true);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (viewModel is not null)
            viewModel.TextReloaded -= OnTextReloaded;

        viewModel = DataContext as ScriptSourceFileEditorViewModel;
        Editor.LoadText(viewModel?.Text);

        if (viewModel is not null)
            viewModel.TextReloaded += OnTextReloaded;
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        viewModel?.CheckExternalChanges();
    }

    private void OnEditorTextEdited(object? sender, string text) => viewModel?.OnTextEdited(text);

    private void OnEditorGotFocus(object? sender, FocusChangedEventArgs e) => viewModel?.CheckExternalChanges();

    private void OnTextReloaded(object? sender, string text) => Editor.LoadText(text);
}
