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
        // Send only checked groups; a sent crash is suppressed and its files dropped, a failed send is left on
        // disk for 'stride crash send'. Unchecked groups wait for the Keep/Delete choice at window close.
        foreach (var group in Groups.Where(group => group.Send))
        {
            try
            {
                await session.SendAsync(group.Crash, group.IncludeDump, group.IncludeAssetDefinition);
                session.Suppress(group.Crash);
                session.Remove(group.Crash);
            }
            catch (Exception)
            {
                failed++;
            }
        }

        IsSending = false;
        CanSend = false; // consent is per crash; don't offer a second send
        SendStatus = failed == 0
            ? "Thank you. The crash report has been sent."
            : $"{failed} report(s) could not be sent; they were kept for a later 'stride crash send'.";
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
                session.Suppress(group.Crash);
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

    private static string ComputeHeader(IReadOnlyList<Stride.CrashReport.StoredCrash> groups)
    {
        var application = groups.Select(crash => crash.Application).FirstOrDefault(name => !string.IsNullOrEmpty(name)) ?? "A Stride tool";
        var count = groups.Count;
        var crashes = count == 1 ? "a crash" : $"{count} distinct crashes";
        return $"{application} hit {crashes} during the last build. Sending the report helps us fix them.";
    }
}
