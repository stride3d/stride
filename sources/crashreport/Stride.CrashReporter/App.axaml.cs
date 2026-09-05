// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace Stride.CrashReporter;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && Program.Session is { } session)
        {
            var window = new CrashReporterWindow { Topmost = true };
            var viewModel = new CrashReporterViewModel(session, window.Close, suggestedName => PickSavePathAsync(window, suggestedName));
            window.DataContext = viewModel;
            // The window closing (Send then Close, a plain Close, or the X) is what commits the dismiss/cleanup.
            window.Closed += (_, _) => viewModel.OnClosed();
            desktop.MainWindow = window;
        }

        base.OnFrameworkInitializationCompleted();
    }

    // The OS save dialog for the on-demand full memory dump; null when the user cancels.
    private static async Task<string?> PickSavePathAsync(Window window, string suggestedName)
    {
        var file = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save full memory dump",
            SuggestedFileName = suggestedName,
            DefaultExtension = "dmp",
            FileTypeChoices = new[] { new FilePickerFileType("Minidump") { Patterns = new[] { "*.dmp" } } },
        });
        return file?.TryGetLocalPath();
    }
}
