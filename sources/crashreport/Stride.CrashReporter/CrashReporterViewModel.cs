// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace Stride.CrashReporter;

/// <summary>
/// The reporter window's model: the run's crash groups and one Send decision for the lot. Sending a group
/// auto-suppresses its future popups and drops its files; whatever the user did not send is kept or deleted
/// by the Keep/Delete choice taken when the window closes (a plain X or Escape deletes, the tidy default).
/// </summary>
internal sealed class CrashReporterViewModel : ObservableObject
{
    private readonly CrashSession session;
    private readonly Action requestClose;

    private bool canSend;
    private bool isSending;
    private bool finalized;
    private bool keepFiles;
    private bool isReportVisible;
    private string? sendStatus;

    public CrashReporterViewModel(CrashSession session, Action requestClose)
    {
        this.session = session;
        this.requestClose = requestClose;

        Groups = new ObservableCollection<CrashGroupViewModel>(session.Groups.Select(crash => new CrashGroupViewModel(crash, session.DumpSize(crash))));
        Title = $"{ApplicationName(session.Groups)} crash report";
        Header = ComputeHeader(session.Groups);
        FullReport = string.Join("\n\n----------------------------------------\n\n", Groups.Select(group => group.ReportText));
        canSend = !session.IsDisabled;

        SendCommand = new RelayCommand(OnSend, () => canSend && !isSending && !finalized);
        CloseAndDeleteCommand = new RelayCommand(() => Close(keep: false));
        CloseAndKeepCommand = new RelayCommand(() => Close(keep: true));
        ViewReportCommand = new RelayCommand(() => IsReportVisible = !IsReportVisible);
        OpenFolderCommand = new RelayCommand(OnOpenFolder);
    }

    public ObservableCollection<CrashGroupViewModel> Groups { get; }

    public string Title { get; }

    public string Header { get; }

    public string FullReport { get; }

    public bool CanSend
    {
        get => canSend;
        private set { if (SetProperty(ref canSend, value)) ((RelayCommand)SendCommand).RaiseCanExecuteChanged(); }
    }

    public bool IsSending
    {
        get => isSending;
        private set { if (SetProperty(ref isSending, value)) ((RelayCommand)SendCommand).RaiseCanExecuteChanged(); }
    }

    public bool IsReportVisible
    {
        get => isReportVisible;
        set => SetProperty(ref isReportVisible, value);
    }

    public string? SendStatus
    {
        get => sendStatus;
        private set => SetProperty(ref sendStatus, value);
    }

    public ICommand SendCommand { get; }
    public ICommand CloseAndDeleteCommand { get; }
    public ICommand CloseAndKeepCommand { get; }
    public ICommand ViewReportCommand { get; }
    public ICommand OpenFolderCommand { get; }

    private async Task OnSend()
    {
        IsSending = true;
        SendStatus = "Sending crash reports…";

        var failed = 0;
        string? lastError = null;
        // Send only checked groups. A successful send quietens that crash for the rest of a GameStudio session (so
        // repeated builds don't re-pop it) but does not persistently suppress it; "Don't show again" is the separate,
        // durable opt-out applied at window close. A sent crash's files are dropped; a failed send is left on disk
        // for 'stride crash send'. Unchecked groups wait for the Keep/Delete choice at window close.
        foreach (var group in Groups.Where(group => group.Send))
        {
            try
            {
                await session.SendAsync(group.Crash, group.IncludeDump, group.IncludeAssetDefinition);
                session.SuppressForSession(group.Crash);
                session.Remove(group.Crash);
            }
            catch (Exception exception)
            {
                failed++;
                lastError = exception.Message;
            }
        }

        IsSending = false;
        CanSend = false; // consent is per crash; don't offer a second send
        SendStatus = failed == 0
            ? (session.IsSessionScoped
                ? "Thank you. The crash report has been sent; you won't be asked about it again this session."
                : "Thank you. The crash report has been sent.")
            : $"{failed} report(s) could not be sent ({lastError}); they were kept for a later 'stride crash send'.";
    }

    private void Close(bool keep)
    {
        keepFiles = keep;
        requestClose();
    }

    /// <summary>
    /// Called once when the window closes. Applies each group's "don't show again", then either drops the
    /// leftover files or keeps them, per the Keep/Delete choice (a plain X or Escape takes the delete default).
    /// </summary>
    public void OnClosed()
    {
        if (finalized)
            return;
        finalized = true;

        foreach (var group in Groups)
        {
            if (group.DontShowAgain)
                session.SuppressPersistent(group.Crash);
            if (!keepFiles)
                session.Remove(group.Crash);
        }
        session.CleanupIfEmpty();
    }

    // Reveal the run directory in the OS file manager so the user can inspect the report and dump. Pure
    // reveal: whether the files survive is the Keep/Delete choice made at close, not a side effect of this.
    private void OnOpenFolder()
    {
        var directory = session.RunDirectory;
        if (!Directory.Exists(directory))
            return;

        try
        {
            var start = OperatingSystem.IsWindows() ? new ProcessStartInfo("explorer.exe", $"\"{directory}\"")
                : OperatingSystem.IsMacOS() ? new ProcessStartInfo("open", $"\"{directory}\"")
                : new ProcessStartInfo("xdg-open", directory);
            start.UseShellExecute = true;
            Process.Start(start);
        }
        catch (Exception)
        {
            // Revealing the folder is best effort.
        }
    }

    // The crashing tool's name, so the window makes clear which app crashed (e.g. the asset compiler, not the
    // GameStudio that spawned this reporter).
    private static string ApplicationName(IReadOnlyList<Stride.CrashReport.StoredCrash> groups)
        => groups.Select(crash => crash.Application).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? "A Stride tool";

    private static string ComputeHeader(IReadOnlyList<Stride.CrashReport.StoredCrash> groups)
    {
        var count = groups.Count;
        var crashes = count == 1 ? "a crash" : $"{count} distinct crashes";
        return $"{ApplicationName(groups)} hit {crashes} during the last build. Sending the report helps us fix them.";
    }
}
