// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The dialog asking the name of a new code library or plugin project (<see cref="ProjectLibraryViewModel"/>). It closes with
/// <c>true</c> when the names are valid and the user accepts.
/// </summary>
public sealed partial class ProjectLibraryWindow : Window
{
    public ProjectLibraryWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the names asked.
    /// </summary>
    public ProjectLibraryViewModel? Library => DataContext as ProjectLibraryViewModel;

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        NameBox.Focus();
        NameBox.SelectAll();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (Library is null)
            return;
        if (!Library.Validate(out var error))
        {
            ErrorLabel.Text = error;
            ErrorLabel.IsVisible = true;
            return;
        }
        Close(true);
    }

    private void OnCancelClick(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
