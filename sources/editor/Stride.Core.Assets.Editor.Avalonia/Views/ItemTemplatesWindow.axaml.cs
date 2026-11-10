// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Components.AddAssets;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The dialog picking the template that imports a set of files (<see cref="AssetTemplatesViewModel"/>). It closes with the
/// picked template (<see cref="SelectedTemplate"/>), or with <c>null</c> when the user cancels.
/// </summary>
public sealed partial class ItemTemplatesWindow : Window
{
    public ItemTemplatesWindow()
    {
        InitializeComponent();
        TemplateList.AddHandler(PointerReleasedEvent, OnTemplatePointerReleased, RoutingStrategies.Bubble);
    }

    /// <summary>
    /// Gets the templates shown.
    /// </summary>
    public AssetTemplatesViewModel? Templates => DataContext as AssetTemplatesViewModel;

    /// <summary>
    /// Gets the picked template, or <c>null</c>.
    /// </summary>
    public TemplateDescriptionViewModel? SelectedTemplate { get; private set; }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Templates is { Templates.Count: > 0 } templates)
        {
            templates.SelectedTemplate = templates.Templates[0];
            TemplateList.Focus();
        }
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled)
            return;
        switch (e.Key)
        {
            case Key.Enter:
                e.Handled = true;
                Validate(Templates?.SelectedTemplate);
                break;
            case Key.Escape:
                e.Handled = true;
                Close(null);
                break;
        }
    }

    private void OnTemplatePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.InitialPressMouseButton != MouseButton.Left)
            return;
        if ((e.Source as Visual)?.FindAncestorOfType<ListBoxItem>(true)?.DataContext is AssetTemplateItemViewModel item)
            Validate(item);
    }

    private void Validate(AssetTemplateItemViewModel? item)
    {
        SelectedTemplate = item?.Template;
        Close(SelectedTemplate);
    }
}
