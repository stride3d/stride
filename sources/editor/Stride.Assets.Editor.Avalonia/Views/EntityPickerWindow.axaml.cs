// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Assets.Editor.ViewModels;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The window of the entity picker (<see cref="EntityPickerViewModel"/>) of the Pick buttons of the entity and component
/// properties: it closes with <c>true</c> when the user picks an entity, or a component.
/// </summary>
public sealed partial class EntityPickerWindow : Window
{
    public EntityPickerWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model of the picker.
    /// </summary>
    public EntityPickerViewModel? Picker => DataContext as EntityPickerViewModel;

    /// <summary>
    /// Gets whether the window closed on a pick, shown modally or not.
    /// </summary>
    public bool Picked { get; private set; }

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SearchBox.Focus();
    }

    private void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Picker?.SelectionValid == true)
            ClosePicked();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (Picker?.SelectionValid == true)
            ClosePicked();
        else
            Close(false);
    }

    private void ClosePicked()
    {
        Picked = true;
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
