// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The Add asset dialog: an <see cref="AddItemView"/> over the templates of an <see cref="AddItemTemplateCollectionViewModel"/>.
/// It closes with the validated template (<see cref="SelectedTemplate"/>), or with <c>null</c> when the user cancels.
/// </summary>
public sealed partial class AddItemWindow : Window
{
    private bool validated;

    public AddItemWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the templates shown.
    /// </summary>
    public AddItemTemplateCollectionViewModel? Templates => DataContext as AddItemTemplateCollectionViewModel;

    /// <summary>
    /// Gets the validated template, or <c>null</c>.
    /// </summary>
    public ITemplateDescriptionViewModel? SelectedTemplate { get; private set; }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Items.Reset();
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!e.Handled && e.Key == Key.Escape)
        {
            e.Handled = true;
            Close(null);
        }
    }

    private void OnItemValidated(object? sender, TemplateValidatedEventArgs e)
    {
        // Prevent re-entrancy
        if (validated)
            return;

        validated = true;
        SelectedTemplate = e.Template;
        Close(e.Template);
    }
}
