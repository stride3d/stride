// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Presentation.Windows;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The banner of the main window while the last upgrade of a project of the session is unfinished (see
/// <see cref="PackageUpgrader.UnfinishedMetadata"/>), left items to check by hand in its <see cref="UpgradeReportWriter.FileName"/>,
/// or keeps the backup its records name.
/// It opens the reports, runs again the steps an unfinished upgrade left or marks it as finished, puts back the files the last
/// upgrade changed while its records name its backup, and removes the reports and the backups once their items are handled.
/// </summary>
/// <remarks>The items of a report, which the user ticks by hand, are not read: the file existing is the signal.</remarks>
public sealed class UpgradeReportBannerViewModel : ViewModelBase, IDisposable
{
    private readonly IDispatcherService? dispatcher;
    private readonly List<FileSystemWatcher> watchers = [];
    private readonly ConcurrentDictionary<string, string> owners = new(StringComparer.OrdinalIgnoreCase);
    private SessionViewModel? session;
    private IReadOnlyList<string> reports = [];
    private IReadOnlyList<UnfinishedProject> unfinished = [];
    private IReadOnlyList<ProjectBackups> backups = [];
    private IReadOnlyList<string> revertFolders = [];
    private bool isVisible;
    private bool hasReports;
    private bool isUnfinished;
    private bool canRetry;
    private bool canRevert;
    private bool canRemoveReports;
    private string removeReportsLabel = string.Empty;
    private string message = string.Empty;
    private ICommandBase? reloadCommand;
    private int updateQueued;
    private int readVersion;
    private Dictionary<string, string> reportNames = [];

    /// <param name="serviceProvider">The editor's services.</param>
    public UpgradeReportBannerViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        dispatcher = serviceProvider.TryGet<IDispatcherService>();
        OpenCommand = new AnonymousCommand(serviceProvider, Open);
        RetryCommand = new AnonymousTaskCommand(serviceProvider, () => RunAsync(RetryAsync));
        RevertCommand = new AnonymousTaskCommand(serviceProvider, () => RunAsync(RevertAsync));
        MarkFinishedCommand = new AnonymousTaskCommand(serviceProvider, () => RunAsync(MarkFinishedAsync));
        RemoveReportsCommand = new AnonymousTaskCommand(serviceProvider, () => RunAsync(RemoveReportsAsync));
    }

    /// <summary>
    /// The session whose projects the banner follows, or <c>null</c>.
    /// </summary>
    public SessionViewModel? Session
    {
        get => session;
        set => SetValue(ref session, value, Watch);
    }

    /// <summary>
    /// The command of the editor that opens the session again, or <c>null</c>: after steps that ran again changed a project,
    /// the session loads it again.
    /// </summary>
    public ICommandBase? ReloadCommand { get => reloadCommand; set => SetValue(ref reloadCommand, value); }

    /// <summary>
    /// The full paths of the <see cref="UpgradeReportWriter.FileName"/> files of the projects of the session: next to their
    /// solution, or next to a project no solution lists.
    /// </summary>
    public IReadOnlyList<string> Reports { get => reports; private set => SetValue(ref reports, value); }

    /// <summary>
    /// Whether a project of the session has a report to open.
    /// </summary>
    public bool HasReports { get => hasReports; private set => SetValue(ref hasReports, value); }

    /// <summary>
    /// Whether the banner shows: the last upgrade of a project of the session is unfinished, a project has a report, or the
    /// records name a backup on this computer.
    /// </summary>
    public bool IsVisible { get => isVisible; private set => SetValue(ref isVisible, value); }

    /// <summary>
    /// Whether the last upgrade of a project of the session is unfinished: it can be marked as finished, or its steps run again.
    /// </summary>
    public bool IsUnfinished { get => isUnfinished; private set => SetValue(ref isUnfinished, value); }

    /// <summary>
    /// Whether a retry runs again a step an unfinished upgrade left: code steps don't run again.
    /// </summary>
    public bool CanRetry { get => canRetry; private set => SetValue(ref canRetry, value); }

    /// <summary>
    /// Whether the records of the last upgrade name a backup to put the files back from (see <see cref="PackageUpgrader.BackupMetadata"/>).
    /// </summary>
    public bool CanRevert { get => canRevert; private set => SetValue(ref canRevert, value); }

    /// <summary>
    /// Whether the reports and backups can be removed: no upgrade is unfinished, and a project has a report or a backup.
    /// </summary>
    public bool CanRemoveReports { get => canRemoveReports; private set => SetValue(ref canRemoveReports, value); }

    /// <summary>
    /// The text of the button that removes the reports, and the backups the records name.
    /// </summary>
    public string RemoveReportsLabel { get => removeReportsLabel; private set => SetValue(ref removeReportsLabel, value); }

    /// <summary>
    /// What the banner says.
    /// </summary>
    public string Message { get => message; private set => SetValue(ref message, value); }

    /// <summary>
    /// Opens each report with the application of the system for Markdown files.
    /// </summary>
    public ICommandBase OpenCommand { get; }

    /// <summary>
    /// Runs again the project steps the unfinished upgrades left, then opens the session again when they changed a project.
    /// </summary>
    public ICommandBase RetryCommand { get; }

    /// <summary>
    /// Puts back from their backups the files the last upgrade changed, once the user confirms, then opens the session again.
    /// </summary>
    public ICommandBase RevertCommand { get; }

    /// <summary>
    /// Marks the unfinished upgrades as finished, once the user confirms that their steps are done by hand.
    /// </summary>
    public ICommandBase MarkFinishedCommand { get; }

    /// <summary>
    /// Deletes the reports, and the backups the records name, once the user confirms that their items are handled.
    /// </summary>
    public ICommandBase RemoveReportsCommand { get; }

    /// <summary>
    /// Looks again for the reports of the projects of the session, then, off the UI thread, for their records: the unfinished
    /// upgrades, and the backups that revert the last upgrade.
    /// </summary>
    /// <returns>A task that completes once the records are read.</returns>
    public Task Update()
    {
        var projects = Projects().ToList();
        reportNames = projects.DistinctBy(x => x.Report, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Report, x => x.ReportName, StringComparer.OrdinalIgnoreCase);
        Reports = reportNames.Keys.Where(File.Exists).ToList();
        Refresh();

        // Reading a record evaluates its project with MSBuild: it can take a second
        var version = Interlocked.Increment(ref readVersion);
        return Task.Run(() =>
            {
                var records = projects
                    .Where(x => x.ProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && File.Exists(x.ProjectPath))
                    .Select(x => (x.Name, x.ProjectPath, Records: ReadRecords(x.ProjectPath)))
                    .ToList();
                var projectBackups = records.Where(x => x.Records.Backups.Count > 0).Select(x => new ProjectBackups(x.ProjectPath, x.Records.Backups, [.. x.Records.Backups.Where(Directory.Exists)])).ToList();
                return (Unfinished: records.Select(x => new UnfinishedProject(x.Name, x.ProjectPath, x.Records.Unfinished)).Where(x => x.Steps.Count > 0).ToList(),
                    Backups: projectBackups,
                    RevertFolders: projectBackups.SelectMany(x => x.Folders).Distinct(StringComparer.OrdinalIgnoreCase).Where(UpgradeBackup.CanOpen).ToList());
            })
            .ContinueWith(task =>
            {
                // A read that failed leaves the banner as the last one read it
                if (!task.IsCompletedSuccessfully || version != Volatile.Read(ref readVersion))
                    return;
                if (dispatcher is null)
                    Set();
                else
                    dispatcher.Invoke(Set);

                void Set()
                {
                    (unfinished, backups, revertFolders) = task.Result;
                    Refresh();
                }
            });
    }

    // What the banner shows, from the reports and the records last read
    private void Refresh()
    {
        IsUnfinished = unfinished.Count > 0;
        CanRetry = unfinished.Any(x => x.CanRetry);
        CanRevert = revertFolders.Count > 0;
        // A backup the records name stays until it is removed, also once the report is deleted by hand: a revert would put it back
        var hasBackup = backups.Any(x => x.ExistingFolders.Count > 0);
        HasReports = Reports.Count > 0;
        IsVisible = IsUnfinished || HasReports || hasBackup;
        CanRemoveReports = !IsUnfinished && (Reports.Count > 0 || hasBackup);
        RemoveReportsLabel = (Reports.Count > 0, hasBackup) switch
        {
            (true, true) => Tr._p("Button", "Remove the report and backup"),
            (false, true) => Tr._p("Button", "Remove the backup"),
            _ => Tr._p("Button", "Remove the report"),
        };
        Message = (unfinished.Count, Reports.Count, CanRetry) switch
        {
            (1, _, true) => string.Format(Tr._p("Message", "The last upgrade of {0} is unfinished, with {1} step(s) left: retry them, or do them by hand and mark the upgrade as finished."), unfinished[0].Name, unfinished[0].Steps.Count),
            (1, _, false) => string.Format(Tr._p("Message", "The last upgrade of {0} is unfinished, with {1} step(s) left that don't run again: do them by hand, then mark the upgrade as finished."), unfinished[0].Name, unfinished[0].Steps.Count),
            (> 1, _, true) => string.Format(Tr._p("Message", "The last upgrades of {0} projects are unfinished: retry their steps left, or do them by hand and mark the upgrades as finished."), unfinished.Count),
            (> 1, _, false) => string.Format(Tr._p("Message", "The last upgrades of {0} projects are unfinished, with steps left that don't run again: do them by hand, then mark the upgrades as finished."), unfinished.Count),
            (_, 0, _) => hasBackup ? Tr._p("Message", "The last upgrade keeps a backup of the files it changed: revert the upgrade, or remove the backup once you checked it.") : string.Empty,
            (_, 1, _) => string.Format(Tr._p("Message", "The last upgrade of {0} left items to check by hand: see {1}, then remove it."), reportNames.GetValueOrDefault(Reports[0]), UpgradeReportWriter.FileName),
            _ => string.Format(Tr._p("Message", "The last upgrades of {0} left items to check by hand: see their {1}, then remove them."), string.Join(", ", Reports.Select(x => reportNames.GetValueOrDefault(x))), UpgradeReportWriter.FileName),
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopWatching();
        session = null;
    }

    // The projects of the session, each with its report and the name of the solution or project the report is of (see
    // UpgradeReportWriter.FindOwner), from the project file, or the package file without one
    private IEnumerable<(string Name, string ProjectPath, string Report, string ReportName)> Projects()
        => session?.PackageSession.Projects
            .Select(x => (x.Package.Meta.Name, Path: x is SolutionProject project ? project.FullPath : x.Package.FullPath))
            .Where(x => x.Path is not null)
            .Select(x =>
            {
                var projectPath = x.Path!.ToOSPath();
                // Finding the solution reads the solutions of the folders above: once a project
                var owner = owners.GetOrAdd(projectPath, UpgradeReportWriter.FindOwner);
                return (x.Name, projectPath, UpgradeReportWriter.GetPath(owner), Path.GetFileNameWithoutExtension(owner));
            }) ?? [];

    // The unfinished upgrades of a project and the backups its records name; none for a project MSBuild can't evaluate, which the
    // log of the session already reports
    private static (IReadOnlyList<UnfinishedUpgrade> Unfinished, IReadOnlyList<string> Backups) ReadRecords(string projectPath)
    {
        try
        {
            return VSProjectHelper.GetUpgradeRecords(projectPath);
        }
        catch (Exception)
        {
            return ([], []);
        }
    }

    // A project whose last upgrades are unfinished
    private sealed record UnfinishedProject(string Name, string ProjectPath, IReadOnlyList<UnfinishedUpgrade> Upgrades)
    {
        public IReadOnlyList<UpgradeStepInfo> Steps { get; } = PackageSession.DescribeUnfinishedSteps(Upgrades);

        // A retry runs again a step an upgrade left
        public bool CanRetry => Steps.Any(x => x.CanRetry);
    }

    // A project whose records name the backup folders of the last upgrade, and those of them on this computer: the backup folders
    // are not in version control, while the records are
    private sealed record ProjectBackups(string ProjectPath, IReadOnlyList<string> Folders, IReadOnlyList<string> ExistingFolders);

    /// <summary>
    /// What a revert of the last upgrade does, for the question that offers it, with the files changed since the upgrade.
    /// </summary>
    /// <param name="backupFolders">The backup folders of the upgrade.</param>
    /// <remarks>Reads the files of the backups to tell which changed: call it off the UI thread.</remarks>
    public static string FormatRevertQuestion(IReadOnlyList<string> backupFolders)
    {
        const int MaxListed = 10;
        var changed = backupFolders
            .SelectMany(folder => (UpgradeBackup.Open(folder)?.GetChangedFiles() ?? []).Select(x => Path.GetRelativePath(Path.GetDirectoryName(folder)!, x)))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var text = new StringBuilder();
        text.AppendLine(string.Format(Tr._p("Message", "**Revert** puts back every file the upgrade changed, in every project, as it was before it, from {0}. It first keeps the files as they are now in a reverted folder of the backup. Game Studio then opens the project again, to upgrade it."),
            string.Join(", ", backupFolders.Select(x => Path.GetFileName(x)))));
        text.AppendLine();
        if (changed.Count == 0)
        {
            text.AppendLine(Tr._p("Message", "None of these files changed since the upgrade."));
            return text.ToString();
        }
        text.AppendLine(string.Format(Tr._p("Message", "These {0} file(s) changed since the upgrade: after the revert, their changes are only in the reverted folder."), changed.Count));
        text.AppendLine();
        foreach (var file in changed.Take(MaxListed))
            text.AppendLine($"- {file}");
        if (changed.Count > MaxListed)
            text.AppendLine(string.Format(Tr._p("Message", "- and {0} more"), changed.Count - MaxListed));
        return text.ToString();
    }

    /// <summary>
    /// What a retry of the steps unfinished upgrades left did, for a message box.
    /// </summary>
    /// <param name="left">The name of each project the retry ran on, with the steps it still leaves.</param>
    public static string FormatRetryResult(IReadOnlyCollection<(string Project, IReadOnlyList<UpgradeStepInfo> Steps)> left)
    {
        if (left.All(x => x.Steps.Count == 0))
            return Tr._p("Message", "The steps left are done: the upgrade is finished.");

        var text = new StringBuilder();
        text.AppendLine(Tr._p("Message", "These steps are still left:"));
        foreach (var (project, steps) in left.Where(x => x.Steps.Count > 0))
            AppendProjectSteps(text, project, steps);
        text.AppendLine();
        text.AppendLine(string.Format(Tr._p("Message", "Code steps don't run again: do them by hand, as {0} lists them, then mark the upgrade as finished."), UpgradeReportWriter.FileName));
        return text.ToString();
    }

    // The name of the project, then its steps, one per line
    private static void AppendProjectSteps(StringBuilder text, string project, IReadOnlyList<UpgradeStepInfo> steps)
    {
        text.AppendLine();
        text.AppendLine($"**{project}**");
        text.AppendLine();
        text.AppendLine(UpgradeReportWriter.FormatStepList(steps));
    }

    // Follows the reports, created and deleted, in the folders of the solutions, and the project files, whose records change
    private void Watch()
    {
        StopWatching();
        owners.Clear();
        foreach (var directory in Projects().SelectMany(x => new[] { Path.GetDirectoryName(x.Report)!, Path.GetDirectoryName(x.ProjectPath)! }).Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            var watcher = new FileSystemWatcher(directory) { NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite };
            watcher.Filters.Add(UpgradeReportWriter.FileName);
            watcher.Filters.Add("*.csproj");
            watcher.Created += ReportChanged;
            watcher.Deleted += ReportChanged;
            watcher.Renamed += ReportChanged;
            watcher.Changed += ReportChanged;
            watcher.EnableRaisingEvents = true;
            watchers.Add(watcher);
        }
        _ = Update();
    }

    private void StopWatching()
    {
        foreach (var watcher in watchers)
            watcher.Dispose();
        watchers.Clear();
    }

    // Raised on a thread of the watcher: read again on the UI thread, once for a burst of changes
    private void ReportChanged(object sender, FileSystemEventArgs e)
    {
        if (dispatcher is null)
        {
            _ = Update();
            return;
        }
        if (Interlocked.Exchange(ref updateQueued, 1) == 1)
            return;
        _ = dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref updateQueued, 0);
            _ = Update();
        });
    }

    private void Open()
    {
        foreach (var report in Reports)
        {
            try
            {
                Process.Start(new ProcessStartInfo(report) { UseShellExecute = true });
            }
            catch (Exception e) when (e is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                // No application opens Markdown files: the folder shows the file
                Process.Start(new ProcessStartInfo(Path.GetDirectoryName(report)!) { UseShellExecute = true });
            }
        }
    }

    private async Task RetryAsync()
    {
        if (session?.PackageSession is not { } packageSession)
            return;

        var projects = packageSession.Projects.OfType<SolutionProject>()
            .Where(x => x.FullPath is not null && unfinished.Any(y => string.Equals(y.ProjectPath, x.FullPath.ToOSPath(), StringComparison.OrdinalIgnoreCase)))
            .ToList();
        var log = new LoggerResult();
        // The steps evaluate and save the project files
        var retries = await Task.Run(() => packageSession.RetryUnfinishedUpgrades(projects, log));
        await Update();

        var left = projects.Zip(retries, (project, retry) => (project.Name, retry.StepsLeft)).ToList();
        var changed = retries.Any(x => x.ChangedProject);
        var text = new StringBuilder();
        text.AppendLine(FormatRetryResult(left));
        foreach (var error in log.Messages.Where(x => x.Type >= LogMessageType.Error))
        {
            text.AppendLine();
            text.AppendLine($"- {error.Text}");
        }
        if (changed && ReloadCommand is not null)
        {
            text.AppendLine();
            text.AppendLine(Tr._p("Message", "The steps changed the project: Game Studio opens it again to load it."));
        }
        await ServiceProvider.Get<IDialogService>().MessageBoxAsync(text.ToString(), MessageBoxButton.OK, left.All(x => x.StepsLeft.Count == 0) ? MessageBoxImage.Information : MessageBoxImage.Warning);
        if (changed)
            ReloadCommand?.Execute();
    }

    private async Task RevertAsync()
    {
        var folders = revertFolders;
        var question = await Task.Run(() => FormatRevertQuestion(folders));
        var text = Tr._p("Message", "Revert the last upgrade?") + Environment.NewLine + Environment.NewLine + question;
        if (session?.HasUnsavedChanges == true)
            text += Environment.NewLine + Tr._p("Message", "The unsaved changes of the project are lost: Game Studio opens it again from the files put back.");
        if (await ConfirmAsync(text, Tr._p("Button", "Revert")) != true)
            return;

        // Saving the session would write the upgraded project over the files put back, also when only some of them are
        session?.DropUnsavedChanges();
        var log = new LoggerResult();
        var revert = await Task.Run(() => PackageSession.RevertUpgrades(folders, log));
        await Update();
        if (revert.NotPutBack.Count == 0)
        {
            ReloadCommand?.Execute();
            return;
        }

        var error = new StringBuilder();
        error.AppendLine(string.Format(Tr._p("Message", "Some files could not be put back as they were: copy them from {0}, then open the project again."), string.Join(", ", folders)));
        foreach (var message in log.Messages.Where(x => x.Type >= LogMessageType.Error))
        {
            error.AppendLine();
            error.AppendLine($"- {message.Text}");
        }
        await ServiceProvider.Get<IDialogService>().MessageBoxAsync(error.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
    }

    private async Task MarkFinishedAsync()
    {
        var text = new StringBuilder();
        text.AppendLine(Tr._p("Message", "Mark the last upgrade as finished? Do it once you did by hand every step it left:"));
        foreach (var project in unfinished)
            AppendProjectSteps(text, project.Name, project.Steps);
        if (await ConfirmAsync(text.ToString(), Tr._p("Button", "Mark as finished")) != true)
            return;

        var log = new LoggerResult();
        var projectPaths = unfinished.Select(x => x.ProjectPath).ToList();
        await Task.Run(() => PackageSession.MarkUpgradesFinished(projectPaths, log));
        // A project that could not be marked, or a report that could not get the mark
        var errors = log.Messages.Where(x => x.Type >= LogMessageType.Error).Select(x => $"- {x.Text}").ToList();
        if (errors.Count > 0)
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Join(Environment.NewLine, errors), MessageBoxButton.OK, MessageBoxImage.Warning);
        await Update();
    }

    private async Task RemoveReportsAsync()
    {
        var folders = backups.SelectMany(x => x.ExistingFolders).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var text = (Reports.Count, folders.Count) switch
        {
            (_, 0) => string.Format(Tr._p("Message", "Remove {0}? Do it once you handled every item it lists."), string.Join(", ", Reports)),
            (0, _) => string.Format(Tr._p("Message", "Remove the backup {0}? The last upgrade can then no longer be reverted."), string.Join(", ", folders)),
            _ => string.Format(Tr._p("Message", "Remove {0}, and the backup {1}? Do it once you handled every item it lists: the last upgrade can then no longer be reverted."), string.Join(", ", Reports), string.Join(", ", folders)),
        };
        if (await ConfirmAsync(text, Tr._p("Button", "Remove")) != true)
            return;

        // The records first: a backup they still named would be offered for a revert
        foreach (var project in backups)
        {
            try
            {
                VSProjectHelper.RemoveUpgradeBackups(project.ProjectPath);
            }
            catch (Exception e) when (e is Microsoft.Build.Exceptions.InvalidProjectFileException or IOException or UnauthorizedAccessException)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Could not remove the backup from the records of {0}: {1}"), project.ProjectPath, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        foreach (var folder in folders)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Could not remove {0}: {1}"), folder, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        foreach (var report in Reports)
        {
            try
            {
                File.Delete(report);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Could not remove {0}: {1}"), report, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        await Update();
    }

    // Runs an action of a button, one at a time: each changes the project files and their records off the UI thread. The command
    // doesn't await it, so an error of a project file or a backup, which MSBuild or the file system raises, is shown here.
    private async Task RunAsync(Func<Task> action)
    {
        ICommandBase[] commands = [RetryCommand, RevertCommand, MarkFinishedCommand, RemoveReportsCommand];
        if (commands.Any(x => !x.IsEnabled))
            return;

        foreach (var command in commands)
            command.IsEnabled = false;
        try
        {
            await action();
        }
        catch (Exception e)
        {
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "The action could not finish: {0}"), e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            await Update();
        }
        finally
        {
            foreach (var command in commands)
                command.IsEnabled = true;
        }
    }

    // Whether the user confirms the action of the button
    private async Task<bool> ConfirmAsync(string text, string action)
    {
        var buttons = new[]
        {
            new DialogButtonInfo { Content = action, Result = 1 },
            new DialogButtonInfo { Content = Tr._p("Button", "Cancel"), Result = 0, IsDefault = true },
        };
        return await ServiceProvider.Get<IDialogService>().MessageBoxAsync(text, buttons, MessageBoxImage.Question) == 1;
    }
}
