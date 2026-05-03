// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace Stride.Launcher;

internal sealed class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        return (int)Launcher.Main(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    /// <summary>
    /// Returns path of Launcher (we can't use Assembly.GetEntryAssembly().Location in .NET Core, especially with self-publish).
    /// </summary>
    /// <returns></returns>
    internal static string? GetExecutablePath() => Environment.ProcessPath;

    internal static void RunNewApp<TApp>(Func<TApp, CancellationToken> appMain, string[]? args = null)
        where TApp : Application, new()
    {
        // Note: we need a new app because the main one may be already shutting down
        var appBuilder = AppBuilder.Configure<TApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

        if (Application.Current is null)
        {
            appBuilder = appBuilder
                .SetupWithClassicDesktopLifetime(args ?? [], x => x.ShutdownMode = ShutdownMode.OnExplicitShutdown);
            var app = appBuilder.Instance!;
            app.Run(appMain((TApp)app));
        }
        else
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                // First hide the main window
                ((IClassicDesktopStyleApplicationLifetime?)Application.Current.ApplicationLifetime)?.MainWindow?.Hide();

                var app = appBuilder.Instance!;
                app.Run(appMain((TApp)app));
            });
        }
    }
}
