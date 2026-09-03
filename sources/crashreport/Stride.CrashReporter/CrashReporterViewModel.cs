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
    private string feedbackName = "";
    private string feedbackEmail = "";
    private string feedbackMessage = "";

    public CrashReporterViewModel(CrashSession session, Action requestClose)
    {
        this.session = session;
        this.requestClose = requestClose;

        Groups = new ObservableCollection<CrashGroupViewModel>(session.Groups.Select(crash => new CrashGroupViewModel(crash, session.DumpSize(crash))));
        Title = $"{ApplicationName(session.Groups)} crash report";
        Header = ComputeHeader(session.Groups);
        FullReport = string.Join("\n\n----------------------------------------\n\n", Groups.Select(group => group.ReportText));
        canSend = !session.IsDisabled;
        ShowSendControls = canSend; // keep the disclosure + feedback fields laid out after a send (they just grey out)

        SendCommand = new RelayCommand(OnSend, () => canSend && !isSending && !finalized);
        CloseAndDeleteCommand = new RelayCommand(() => Close(keep: false));
        CloseAndKeepCommand = new RelayCommand(() => Close(keep: true), () => HasRemainingFiles);
        ViewReportCommand = new RelayCommand(() => IsReportVisible = !IsReportVisible);
        OpenFolderCommand = new RelayCommand(OnOpenFolder, () => HasRemainingFiles);
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

    /// <summary>Whether the send UI is shown at all (false only when sending is disabled from the start). Stays true
    /// after a send so the disclosure and feedback fields keep their place and simply disable.</summary>
    public bool ShowSendControls { get; }

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

    /// <summary>Optional contact name, sent verbatim with the report (typing it is the consent).</summary>
    public string FeedbackName { get => feedbackName; set => SetProperty(ref feedbackName, value); }

    /// <summary>Optional contact email for follow-up, sent verbatim with the report.</summary>
    public string FeedbackEmail { get => feedbackEmail; set => SetProperty(ref feedbackEmail, value); }

    /// <summary>Optional "what were you doing" note, sent verbatim with the report.</summary>
    public string FeedbackMessage { get => feedbackMessage; set => SetProperty(ref feedbackMessage, value); }

    public ICommand SendCommand { get; }
    public ICommand CloseAndDeleteCommand { get; }
    public ICommand CloseAndKeepCommand { get; }
    public ICommand ViewReportCommand { get; }
    public ICommand OpenFolderCommand { get; }

    // A sent group's files are dropped; Open Folder and Keep only make sense while some group is still unsent.
    private bool HasRemainingFiles => Groups.Any(group => !group.IsSent);

    private async Task OnSend()
    {
        IsSending = true;
        SendStatus = "Sending crash reports…";

        var failed = 0;
        string? lastError = null;
        // A send quietens that crash for this GameStudio session (not persistently — that's "Don't show again") and
        // drops its files; a failed send is left for 'stride crash send'. Unchecked groups wait for Keep/Delete on close.
        foreach (var group in Groups.Where(group => group.Send))
        {
            try
            {
                await session.SendAsync(group.Crash, group.IncludeDump, group.IncludeAssetDefinition,
                    FeedbackName, FeedbackEmail, FeedbackMessage);
                session.SuppressForSession(group.Crash);
                session.Remove(group.Crash);
                group.IsSent = true; // locks this group's options; its files are gone
            }
            catch (Exception exception)
            {
                failed++;
                lastError = exception.Message;
            }
        }

        IsSending = false;
        CanSend = false; // consent is per crash; don't offer a second send
        // Files were dropped for the sent groups; refresh the folder/keep buttons that depend on them.
        ((RelayCommand)OpenFolderCommand).RaiseCanExecuteChanged();
        ((RelayCommand)CloseAndKeepCommand).RaiseCanExecuteChanged();
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

    /// <summary>Called once on close: applies "don't show again", then drops or keeps the leftover files per the
    /// Keep/Delete choice (a plain X or Escape deletes).</summary>
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
        if (keepFiles)
            session.KeepRun(); // move a kept transient run into the durable store
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
