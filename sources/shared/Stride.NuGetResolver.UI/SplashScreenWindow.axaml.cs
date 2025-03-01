using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using NuGet.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Stride.NuGetResolver;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SplashScreenWindow : Window
{
    private int lineCounter;

    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    public void AppendMessage(LogLevel level, string message)
    {
        if (level == LogLevel.Error)
        {
            CloseButton.IsVisible = true;
            Message.Text = "Error restoring NuGet packages!";
            Message.Foreground = new SolidColorBrush(Colors.Red);
        }
        Log.Text += $"[{level}] {message}{Environment.NewLine}";
        Log.ScrollToLine(++lineCounter);
    }

    public void CloseCommand(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void SetupLog(LogLevel level, string message)
    {
        Dispatcher.UIThread.InvokeAsync(() => AppendMessage(level, message));
    }

    public void CloseApp()
    {
        Dispatcher.UIThread.InvokeAsync(() => Close());
    }

    public static void InvokeShutDown()
    {
        Dispatcher.UIThread.InvokeShutdown();
    }
}

public static class NugetResolverApp
{
    public static void Run(AppBuilder.AppMainDelegate AppMain)
    {
        AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .Start(AppMain, []);
    }
}
