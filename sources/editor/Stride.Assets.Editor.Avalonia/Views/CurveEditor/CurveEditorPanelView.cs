// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Assets.Editor.ViewModels.CurveEditor;

namespace Stride.Assets.Editor.Avalonia.Views.CurveEditor;

/// <summary>
/// The view of the curve editor panel (<see cref="CurveEditorPanelViewModel"/>), which the plugin registers for the panel: the
/// <see cref="CurveEditorView"/> of the panel's editor.
/// </summary>
public sealed class CurveEditorPanelView : UserControl
{
    private readonly CurveEditorView editorView = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="CurveEditorPanelView"/> class.
    /// </summary>
    public CurveEditorPanelView()
    {
        Content = editorView;
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        editorView.DataContext = (DataContext as CurveEditorPanelViewModel)?.Editor;
    }
}
