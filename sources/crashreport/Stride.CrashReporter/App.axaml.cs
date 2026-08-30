// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Stride.CrashReporter;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Program.Session is { } session)
        {
            var window = new CrashReporterWindow { Topmost = true };
            var viewModel = new CrashReporterViewModel(session, window.Close);
            window.DataContext = viewModel;
            // The window closing (Send then Close, a plain Close, or the X) is what commits the dismiss/cleanup.
            window.Closed += (_, _) => viewModel.OnClosed();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }
}
