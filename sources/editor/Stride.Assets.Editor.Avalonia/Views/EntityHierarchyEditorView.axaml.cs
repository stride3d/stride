// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<EntityHierarchyEditorViewModel>]
public partial class EntityHierarchyEditorView : UserControl, IAssetEditorView
{
    public EntityHierarchyEditorView()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The game renders while its editor is shown
        (DataContext as EntityHierarchyEditorViewModel)?.ShowGame();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // F frames the last selected entity, from the hierarchy or the view, as in the WPF scene editor. The keys reach
        // the view model after the focused control, so typing an F in a text box doesn't move the camera.
        if (e.Handled || e.Key != Key.F || e.KeyModifiers != KeyModifiers.None || e.Source is TextBox)
            return;

        if (DataContext is EntityHierarchyEditorViewModel { FocusOnSelectionCommand: { IsEnabled: true } command })
        {
            command.Execute();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        (DataContext as EntityHierarchyEditorViewModel)?.HideGame();
        base.OnDetachedFromVisualTree(e);
    }
}
