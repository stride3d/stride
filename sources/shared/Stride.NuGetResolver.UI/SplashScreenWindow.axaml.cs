using System.Reflection.Emit;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using NuGet.Common;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Stride.NuGetResolver
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SplashScreenWindow : Window
    {
        private int lineCounter;

        public SplashScreenWindow()
        {
            this.InitializeComponent();
        }

        public void AppendMessage(LogLevel level, string message)
        {
            if (level == LogLevel.Error)
            {
                CloseButton.IsVisible = true;
                Message.Text = "Error restoring NuGet packages!";
                Message.Foreground = new SolidColorBrush(Colors.Red);
            }
            Log.Text += ($"[{level}] {message}{Environment.NewLine}");
            Log.ScrollToLine(++lineCounter);
        }

        public void CloseCommand(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public void SetupLog(LogLevel level, string message)
        {
            Dispatcher.UIThread.InvokeAsync(() => AppendMessage(level, message));
        }
        
        public void CloseApp()
        {
            Dispatcher.UIThread.InvokeAsync(() => Close());
        }
        public void InvokeShutDown()
        {
            Dispatcher.UIThread.InvokeShutdown();
        }
    }

    public class NugetResolverApp
    {
        public static void Run(AppBuilder.AppMainDelegate AppMain)
        {
            AppBuilder.Configure<Application>()
                .UsePlatformDetect()
                .Start(AppMain, []);
        }
    }
}
