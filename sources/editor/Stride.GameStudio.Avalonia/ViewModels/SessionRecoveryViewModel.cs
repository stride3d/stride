// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Assets.Editor.Components.Recovery;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Settings;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Settings;

namespace Stride.GameStudio.Avalonia.ViewModels;

/// <summary>
/// The recovery copies of the open session in the shell: the service follows the autosave settings, the status bar shows when the
/// last copies were written, and the copies of an editor that closed unexpectedly are offered for restore when the session opens.
/// </summary>
internal sealed class SessionRecoveryViewModel : DispatcherViewModel, IDisposable
{
    /// <summary>
    /// The answers to the question asked when recovery copies are found.
    /// </summary>
    public const int RestoreResult = 1;
    public const int DiscardResult = 2;
    public const int LaterResult = 3;

    private readonly SessionViewModel session;
    private bool disposed;

    public SessionRecoveryViewModel(IViewModelServiceProvider serviceProvider, SessionViewModel session, UFile sessionPath, string rootPath)
        : base(serviceProvider)
    {
        this.session = session;
        Service = new SessionRecoveryService(session, sessionPath, rootPath);
        ApplySettings();
        GameStudioEditorSettings.AutosaveEnabled.ChangesValidated += SettingsChanged;
        GameStudioEditorSettings.AutosaveIntervalMinutes.ChangesValidated += SettingsChanged;
        Service.CopiesWritten += CopiesWritten;
        session.PropertyChanged += SessionPropertyChanged;
    }

    /// <summary>
    /// Gets the service that writes and restores the recovery copies.
    /// </summary>
    public SessionRecoveryService Service { get; }

    /// <summary>
    /// Gets the text of the status bar: when the last recovery copies were written, or <c>null</c> when there are none.
    /// </summary>
    public string? Status { get; private set => SetValue(ref field, value); }

    /// <summary>
    /// Asks whether to restore the recovery copies that an editor which closed unexpectedly left, then restores or discards them, or
    /// keeps them for the next time.
    /// </summary>
    /// <returns>The answer: <see cref="RestoreResult"/>, <see cref="DiscardResult"/> or <see cref="LaterResult"/>.</returns>
    public async Task<int> AskToRestoreAsync(SessionRecovery recovery)
    {
        ArgumentNullException.ThrowIfNull(recovery);
        var message = string.Format(
            Tr._p("Message", "Game Studio closed unexpectedly while this project had unsaved changes. Recovery copies of {0} asset(s) were kept at {1:g}.\r\n\r\nDo you want to restore them? The restored changes are unsaved: save the project to keep them, or undo them."),
            recovery.AssetCount, recovery.Time);
        var buttons = new[]
        {
            new DialogButtonInfo { Content = Tr._p("Button", "Restore"), Result = RestoreResult, IsDefault = true },
            new DialogButtonInfo { Content = Tr._p("Button", "Discard"), Result = DiscardResult },
            new DialogButtonInfo { Content = Tr._p("Button", "Decide later"), Result = LaterResult, IsCancel = true },
        };
        var result = await ServiceProvider.Get<IDialogService>().MessageBoxAsync(message, buttons, MessageBoxImage.Question);
        if (disposed)
            return LaterResult;

        switch (result)
        {
            case RestoreResult:
                if (!await Service.RestoreAsync(recovery))
                    await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Tr._p("Message", "Some recovered changes could not be restored. The Output lists them."), MessageBoxButton.OK, MessageBoxImage.Warning);
                return RestoreResult;
            case DiscardResult:
                Service.Discard(recovery);
                return DiscardResult;
            default:
                // The copies stay: the question comes back the next time the project opens
                return LaterResult;
        }
    }

    /// <summary>
    /// Stops the copies and removes them: the session closes cleanly, saved or not.
    /// </summary>
    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        GameStudioEditorSettings.AutosaveEnabled.ChangesValidated -= SettingsChanged;
        GameStudioEditorSettings.AutosaveIntervalMinutes.ChangesValidated -= SettingsChanged;
        Service.CopiesWritten -= CopiesWritten;
        session.PropertyChanged -= SessionPropertyChanged;
        Service.Dispose();
    }

    /// <summary>
    /// Applies the autosave settings to the service: on or off, and the interval.
    /// </summary>
    public void ApplySettings()
    {
        Service.Interval = GameStudioEditorSettings.GetAutosaveInterval();
        Service.IsEnabled = GameStudioEditorSettings.AutosaveEnabled.GetValue();
        if (!Service.IsEnabled)
            Status = null;
    }

    private void SettingsChanged(object? sender, ChangesValidatedEventArgs e) => Dispatcher.Invoke(ApplySettings);

    private void CopiesWritten(object? sender, EventArgs e)
    {
        Status = Service.LastCopyTime is { } time ? string.Format(Tr._p("StatusBar", "Recovery copy at {0:t}"), time) : null;
    }

    private void SessionPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // A save that leaves nothing unsaved removes the copies
        if (e.PropertyName == nameof(SessionViewModel.HasUnsavedChanges) && !session.HasUnsavedChanges)
            Status = null;
    }
}
