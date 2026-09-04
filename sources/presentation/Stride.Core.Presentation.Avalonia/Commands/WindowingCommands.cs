using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Templates;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using System;
using System.Windows.Input;

namespace Stride.Core.Presentation.Avalonia.Commands;

public static class WindowingCommands
{
    public static ICommand CloseWindowCommand { get; } = new RelayCommand(Close);
    public static ICommand MinimizeWindowCommand { get; } = new RelayCommand(Minimize);
    public static ICommand MaximizeWindowCommand { get; } = new RelayCommand(Maximize);
    public static ICommand RestoreWindowCommand { get; } = new RelayCommand(Restore);
    public static EventHandler<PointerPressedEventArgs> DragWindowEvent = Drag;

    private static void Close(object obj)
    {
        if (obj is Window window)
            window.Close();
    }

    private static void Minimize(object obj)
    {
        if (obj is Window window)
            window.WindowState = WindowState.Minimized;
    }

    private static void Maximize(object obj)
    {
        if (obj is Window window)
            window.WindowState = WindowState.Maximized;
    }

    private static void Restore(object obj)
    {
        if (obj is Window window)
            window.WindowState = WindowState.Normal;
    }

    private static void Drag(object? sender, PointerPressedEventArgs e)
    {
        var control = sender as Control;
        var window = TopLevel.GetTopLevel(control) as Window;
        var point = e.GetCurrentPoint(control);

        window?.BeginMoveDrag(e);
    }
}