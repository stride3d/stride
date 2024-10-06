using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Stride.StorageTool;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var bundlePath = desktop.Args?.Length > 0 ? desktop.Args[0] : null;
            desktop.MainWindow = new MainWindow(bundlePath);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
