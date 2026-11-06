// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Stride.GameStudio.Avalonia.Views;

internal sealed partial class ProgressWindow : Window
{
    public ProgressWindow()
    {
        InitializeComponent();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }

    private void OnClick_Close(object? sender, RoutedEventArgs args)
    {
        Close();
    }
}
