// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

internal sealed partial class DebugWindow : Window
{
    private readonly DebugWindowViewModel viewModel = null!;

    public DebugWindow()
    {
        InitializeComponent();
    }

    public required DebugWindowViewModel ViewModel
    {
        init => DataContext = viewModel = value;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        EditorDebugService.RegisterDebugWindow(viewModel);
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        EditorDebugService.UnregisterDebugWindow(viewModel);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
        }
    }
}
