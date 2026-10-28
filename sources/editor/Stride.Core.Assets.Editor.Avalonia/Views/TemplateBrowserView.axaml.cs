// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The browser of a <see cref="TemplateDescriptionCollectionViewModel"/>: its groups, the templates of the selected group and the
/// description of the selected template. Shared by the project selection window, the new project window and (later) the asset
/// templates.
/// </summary>
public sealed partial class TemplateBrowserView : UserControl
{
    public TemplateBrowserView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the user double-clicks a template: the host accepts the selection, as its OK button does.
    /// </summary>
    public event EventHandler? TemplateActivated;

    /// <summary>
    /// Gets the templates shown.
    /// </summary>
    public TemplateDescriptionCollectionViewModel? Collection => DataContext as TemplateDescriptionCollectionViewModel;

    private void OnTemplateDoubleTapped(object? sender, TappedEventArgs e)
    {
        // Only a double-click on an item counts, not one on the empty part of the list
        if (Collection?.SelectedTemplate is not null && (e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true) is not null)
            TemplateActivated?.Invoke(this, EventArgs.Empty);
    }
}
