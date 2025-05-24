// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.KeyDown += MainWindow_KeyDown;
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            (DataContext as ViewModels.MainViewModel)?.RunCurrentProjectCommand.Execute(null);
            e.Handled = true;
            Console.WriteLine("Run");
        }
    }

}
