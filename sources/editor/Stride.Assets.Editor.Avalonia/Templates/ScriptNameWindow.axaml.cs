// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;
using Stride.Assets.Editor.Templates;

namespace Stride.Assets.Editor.Avalonia.Templates;

/// <summary>
/// The dialog asking the class name, the namespace and the template of a new script (<see cref="ScriptNameViewModel"/>). It closes
/// with <c>true</c> when the names are valid and the user accepts.
/// </summary>
public sealed partial class ScriptNameWindow : Window
{
    public ScriptNameWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the names asked.
    /// </summary>
    public ScriptNameViewModel? Script => DataContext as ScriptNameViewModel;

    /// <inheritdoc/>
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        ClassNameBox.Focus();
        ClassNameBox.SelectAll();
    }

    private void OnOkClick(object? sender, RoutedEventArgs e)
    {
        if (Script is null)
            return;
        if (!Script.Validate(out var error))
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
