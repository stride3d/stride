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
            // A live host's routed build crash opens as a window owned by that host (above it, not above other
            // apps, minimized with it, not modal). A host crash has no usable owner -- the host is frozen or gone --
            // so the window is topmost on its own to stay above the dead host's frame.
            var owned = WindowOwnership.CanOwn(session.OwnerWindow);
            // No taskbar button when owned: like any dialog GameStudio owns, it is found through GameStudio and
            // minimizes with it. A host crash keeps the button, being the only live window of a dead app.
            var window = new CrashReporterWindow { Topmost = !owned, ShowInTaskbar = !owned };
            if (owned)
                window.Opened += (_, _) => WindowOwnership.TrySetOwner(window, session.OwnerWindow);
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
            FileTypeChoices = [new FilePickerFileType("Minidump") { Patterns = ["*.dmp"] }],
        });
        return file?.TryGetLocalPath();
    }
}
