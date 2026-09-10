// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace Stride.CrashReporter;

/// <summary>
/// The reporter window's model: the run's crash groups, a Send decision for the lot, and an explicit Save.
/// Sending a group auto-suppresses its future popups and, unless the files were saved, drops them. Keeping is
/// opt-in: Save copies the run (including the local-only full dump) to persistent storage on demand. Nothing is
/// kept implicitly — closing without a Save discards the leftover files. Send closes the window on success.
/// When the crashed host is still alive and waiting on this window (a GameStudio managed crash), a full memory
/// dump of it can be written on demand to a file the user picks.
/// </summary>
internal sealed class CrashReporterViewModel : ObservableObject
{
    private const string SaveButtonSaveText = "Save a copy";
    private const string SaveButtonOpenText = "Open folder";

    private readonly CrashSession session;
    private readonly Action requestClose;
    private readonly Func<string, Task<string?>>? pickSavePath;

    private bool canSend;
    private bool isSending;
    private bool isSavingDump;
    private bool finalized;
    private bool saved; // the user clicked Save: the run was copied to persistent storage and must not be dropped
    private string saveButtonText = SaveButtonSaveText;
    private bool isReportVisible;
    private string? sendStatus;
    private string feedbackName = "";
    private string feedbackEmail = "";
    private string feedbackMessage = "";

    public CrashReporterViewModel(CrashSession session, Action requestClose, Func<string, Task<string?>>? pickSavePath = null)
    {
        this.session = session;
        this.requestClose = requestClose;
        this.pickSavePath = pickSavePath;

        Groups = new ObservableCollection<CrashGroupViewModel>(session.Groups.Select(crash => new CrashGroupViewModel(crash, session.DumpPath(crash), session.DumpSize(crash))));
        Title = $"{ApplicationName(session.Groups)} crash report";
        Header = ComputeHeader(session.Groups, duringBuild: session.IsSessionScoped);
        FullReport = string.Join("\n\n----------------------------------------\n\n", Groups.Select(group => group.ReportText));
        canSend = !session.IsDisabled;
        ShowSendControls = canSend; // keep the disclosure + feedback fields laid out after a send (they just grey out)

        SendCommand = new RelayCommand(OnSend, () => canSend && !isSending && !finalized);
        SaveCommand = new RelayCommand(OnSave, () => saved || HasRemainingFiles);
        CloseCommand = new RelayCommand(requestClose);
        ViewReportCommand = new RelayCommand(() => IsReportVisible = !IsReportVisible);
        SaveFullDumpCommand = new RelayCommand(OnSaveFullDump, () => CanSaveFullDump && !isSavingDump);
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

    /// <summary>The Save button's caption: "Save a copy" until the run is saved, then "Open folder" to reveal it.</summary>
    public string SaveButtonText { get => saveButtonText; private set => SetProperty(ref saveButtonText, value); }

    public ICommand SendCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ViewReportCommand { get; }
    public ICommand SaveFullDumpCommand { get; }

    /// <summary>Whether the on-demand full memory dump is offered: the crashed host is alive and waiting on this window.</summary>
    public bool CanSaveFullDump => session.CanDumpHost && pickSavePath != null;

    /// <summary>True while the full dump is being written; Close is held off so the write is not cut short.</summary>
    public bool IsSavingDump
    {
        get => isSavingDump;
        private set { if (SetProperty(ref isSavingDump, value)) ((RelayCommand)SaveFullDumpCommand).RaiseCanExecuteChanged(); }
    }

    // Save is offered while some group still has files worth keeping: an unsent one, or a sent full-dump crash
    // whose local-only dump the send deliberately did not drop. Once saved, the button stays enabled to reveal.
    public bool HasRemainingFiles => Groups.Any(group => !group.IsSent || group.IsFullMemoryDump);

    private async Task OnSend()
    {
        IsSending = true;
        SendStatus = "Sending crash reports…";

        var failed = 0;
        string? lastError = null;
        // A send quietens that crash for this GameStudio session (not persistently — that's "Don't show again") and
        // drops its files; a failed send is left for 'stride crash send'. If the run was saved, nothing is dropped.
        foreach (var group in Groups.Where(group => group.Send))
        {
            try
            {
                await session.SendAsync(group.Crash, group.IncludeDump, group.IncludeAssetDefinition,
                    FeedbackName, FeedbackEmail, FeedbackMessage);
                session.SuppressForSession(group.Crash);
                // Drop the sent group's files, unless the user saved the run (then keep everything) or this is a
                // full memory dump (never uploaded, so its only copy is local — a Save is the way to keep it).
                if (!saved && !group.IsFullMemoryDump)
                    session.Remove(group.Crash);
                group.IsSent = true; // locks this group's send options
            }
            catch (Exception exception)
            {
                failed++;
                lastError = exception.Message;
            }
        }

        IsSending = false;
        CanSend = false; // consent is per crash; don't offer a second send

        if (failed == 0)
        {
            requestClose(); // Send always closes on success; keeping the dump is the separate, explicit Save step
            return;
        }

        // A partial failure keeps the window open so the user can still Save or retry via 'stride crash send'.
        ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        OnPropertyChanged(nameof(HasRemainingFiles));
        SendStatus = $"{failed} report(s) could not be sent ({lastError}); they were kept for a later 'stride crash send'.";
    }

    /// <summary>Called once on close: applies "don't show again", then discards the leftover files unless the user
    /// saved them. Keeping is opt-in via Save, so any close path (button, window X, Escape) that follows no Save
    /// drops the transient files.</summary>
    public void OnClosed()
    {
        if (finalized)
            return;
        finalized = true;

        foreach (var group in Groups)
        {
            if (group.DontShowAgain)
                session.SuppressPersistent(group.Crash);
            if (!saved)
                session.Remove(group.Crash); // nothing was saved; discard the leftover transient files
        }
        session.CleanupIfEmpty();
    }

    // Save copies the run (report + the local-only full dump) into persistent storage on demand, then turns the
    // button into a reveal for that folder. Keeping is opt-in: without a Save, close discards the transient files.
    private void OnSave()
    {
        if (!saved)
        {
            session.KeepRun(); // move the transient run into the durable store (a no-op if it is already durable)
            saved = true;
            SaveButtonText = SaveButtonOpenText;
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
            SendStatus = "Saved to persistent storage.";
        }
        else
        {
            RevealDirectory(session.RunDirectory); // second click: show where the saved files now live
        }
    }

    // On-demand full memory dump of the host that crashed and waits on this window, written from here (a healthy
    // process) straight to a path the user picks: never into the run, never sent. Unscrubbed and possibly several
    // GB, hence one explicit click per dump.
    private async Task OnSaveFullDump()
    {
        var path = await pickSavePath!($"StrideCrashFullDump-{DateTime.UtcNow:yyyyMMdd-HHmmss}.dmp");
        if (string.IsNullOrEmpty(path))
            return;

        IsSavingDump = true;
        SendStatus = "Writing the full memory dump… this can take a while.";
        var written = await Task.Run(() => session.TryWriteHostFullDump(path));
        IsSavingDump = false;
        SendStatus = written
            ? $"Full memory dump saved to {path}. It is not anonymized: share it only with people you trust."
            : "The full memory dump could not be written.";
    }

    // Open a directory in the OS file manager. Best effort.
    private static void RevealDirectory(string directory)
    {
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

    // A crash routed from a build reads "during the last build"; a host's own crash (--capture, --host-pid) reads as such.
    private static string ComputeHeader(IReadOnlyList<Stride.CrashReport.StoredCrash> groups, bool duringBuild)
    {
        var count = groups.Count;
        var crashes = count == 1 ? "a crash" : $"{count} distinct crashes";
        return duringBuild
            ? $"{ApplicationName(groups)} hit {crashes} during the last build. Sending the report helps us fix them."
            : $"{ApplicationName(groups)} has crashed. Sending the report helps us fix it.";
    }
}
