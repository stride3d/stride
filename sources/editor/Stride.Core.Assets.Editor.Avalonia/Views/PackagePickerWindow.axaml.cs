// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

/// <summary>
/// The window of the package picker (<see cref="PackagePickerViewModel"/>) of the Add dependency command: it closes with
/// <c>true</c> when the user picks a package.
/// </summary>
public sealed partial class PackagePickerWindow : Window
{
    public PackagePickerWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model of the picker.
    /// </summary>
    public PackagePickerViewModel? Picker => DataContext as PackagePickerViewModel;

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        SearchBox.Focus();
    }

    private void OnPackageDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (Picker?.HasSelection == true)
            Close(true);
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        Close(Picker?.HasSelection == true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
