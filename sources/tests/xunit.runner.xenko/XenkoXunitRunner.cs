using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Logging.Serilog;
using Avalonia.ReactiveUI;
using xunit.runner.xenko.ViewModels;
using xunit.runner.xenko.Views;

namespace xunit.runner.xenko
{
    public class XenkoXunitRunner
    {
        // Initialization code. Don't use any Avalonia, third-party APIs or any
        // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
        // yet and stuff might break.
        public static void Main(string[] args, Action<bool> setInteractiveMode = null) => BuildAvaloniaApp().Start((app, args2) => AppMain(app, args2, setInteractiveMode), args);

        // Avalonia configuration, don't remove; also used by visual designer.
        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToDebug()
                .UseReactiveUI();

        // Your application's entry point. Here you can initialize your MVVM framework, DI
        // container, etc.
        private static void AppMain(Application app, string[] args, Action<bool> setInteractiveMode)
        {
            var window = new MainWindow
            {
                DataContext = new MainWindowViewModel
                {
                    Tests =
                    {
                        SetInteractiveMode = setInteractiveMode,
                        IsInteractiveMode = true,
                    }
                }
            };

            app.Run(window);
        }
    }
}
