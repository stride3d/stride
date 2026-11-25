// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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
/// <see cref="PackageUpgrader.UnfinishedMetadata"/>), or left items to check by hand in its <see cref="UpgradeReportWriter.FileName"/>.
/// It opens the reports, runs again the steps an unfinished upgrade left or marks it as finished, and removes the reports once
/// their items are handled.
/// </summary>
/// <remarks>The items of a report, which the user ticks by hand, are not read: the file existing is the signal.</remarks>
public sealed class UpgradeReportBannerViewModel : ViewModelBase, IDisposable
{
    private readonly IDispatcherService? dispatcher;
    private readonly List<FileSystemWatcher> watchers = [];
    private SessionViewModel? session;
    private IReadOnlyList<string> reports = [];
    private IReadOnlyList<(string Name, string ProjectPath, IReadOnlyList<string> Steps)> unfinished = [];
    private bool isVisible;
    private bool isUnfinished;
    private bool canRemoveReports;
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
        RetryCommand = new AnonymousTaskCommand(serviceProvider, RetryAsync);
        MarkFinishedCommand = new AnonymousTaskCommand(serviceProvider, MarkFinishedAsync);
        RemoveReportsCommand = new AnonymousTaskCommand(serviceProvider, RemoveReportsAsync);
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
    /// The full paths of the <see cref="UpgradeReportWriter.FileName"/> files of the projects of the session.
    /// </summary>
    public IReadOnlyList<string> Reports { get => reports; private set => SetValue(ref reports, value); }

    /// <summary>
    /// Whether the banner shows: the last upgrade of a project of the session is unfinished, or a project has a report.
    /// </summary>
    public bool IsVisible { get => isVisible; private set => SetValue(ref isVisible, value); }

    /// <summary>
    /// Whether the last upgrade of a project of the session is unfinished: it can be marked as finished, or its steps run again.
    /// </summary>
    public bool IsUnfinished { get => isUnfinished; private set => SetValue(ref isUnfinished, value); }

    /// <summary>
    /// Whether the reports can be removed: no upgrade is unfinished, and a project has a report.
    /// </summary>
    public bool CanRemoveReports { get => canRemoveReports; private set => SetValue(ref canRemoveReports, value); }

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
    /// Marks the unfinished upgrades as finished, once the user confirms that their steps are done by hand.
    /// </summary>
    public ICommandBase MarkFinishedCommand { get; }

    /// <summary>
    /// Deletes the reports, once the user confirms that their items are handled.
    /// </summary>
    public ICommandBase RemoveReportsCommand { get; }

    /// <summary>
    /// Looks again for the reports of the projects of the session, then, off the UI thread, for their unfinished upgrades.
    /// </summary>
    /// <returns>A task that completes once the unfinished upgrades are read.</returns>
    public Task Update()
    {
        var projects = Projects().ToList();
        reportNames = projects.ToDictionary(x => x.Report, x => x.Name, StringComparer.OrdinalIgnoreCase);
        Reports = projects.Select(x => x.Report).Where(File.Exists).ToList();
        Refresh();

        // Reading a record evaluates its project with MSBuild: it can take a second
        var version = Interlocked.Increment(ref readVersion);
        return Task.Run(() => projects
            .Where(x => x.ProjectPath.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase) && File.Exists(x.ProjectPath))
            .Select(x => (x.Name, x.ProjectPath, Steps: ReadUnfinishedSteps(x.ProjectPath)))
            .Where(x => x.Steps.Count > 0)
            .ToList())
            .ContinueWith(task =>
            {
                if (version != Volatile.Read(ref readVersion))
                    return;
                if (dispatcher is null)
                    Set();
                else
                    dispatcher.Invoke(Set);

                void Set()
                {
                    unfinished = task.Result;
                    Refresh();
                }
            }, TaskContinuationOptions.OnlyOnRanToCompletion);
    }

    // What the banner shows, from the reports and the unfinished upgrades last read
    private void Refresh()
    {
        IsUnfinished = unfinished.Count > 0;
        IsVisible = IsUnfinished || Reports.Count > 0;
        CanRemoveReports = !IsUnfinished && Reports.Count > 0;
        Message = (unfinished.Count, Reports.Count) switch
        {
            (1, _) => string.Format(Tr._p("Message", "The last upgrade of {0} is unfinished, with {1} step(s) left: retry them, or do them by hand and mark the upgrade as finished."), unfinished[0].Name, unfinished[0].Steps.Count),
            (> 1, _) => string.Format(Tr._p("Message", "The last upgrades of {0} projects are unfinished: retry their steps left, or do them by hand and mark the upgrades as finished."), unfinished.Count),
            (_, 0) => string.Empty,
            (_, 1) => string.Format(Tr._p("Message", "The last upgrade of {0} left items to check by hand: see {1}, then remove it."), reportNames.GetValueOrDefault(Reports[0]), UpgradeReportWriter.FileName),
            _ => string.Format(Tr._p("Message", "The last upgrades of {0} projects left items to check by hand: see their {1}, then remove them."), Reports.Count, UpgradeReportWriter.FileName),
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopWatching();
        session = null;
    }

    // The projects of the session, each with the path of its report: next to the project file, or the package file without one
    private IEnumerable<(string Name, string ProjectPath, string Report)> Projects()
        => session?.PackageSession.Projects
            .Select(x => (x.Package.Meta.Name, Path: x is SolutionProject project ? project.FullPath : x.Package.FullPath))
            .Where(x => x.Path is not null)
            .Select(x => (x.Name, x.Path!.ToOSPath(), Path.Combine(x.Path!.GetFullDirectory().ToOSPath(), UpgradeReportWriter.FileName))) ?? [];

    // The steps the unfinished upgrades of a project left; none for a project MSBuild can't evaluate, which the log of the
    // session already reports
    private static IReadOnlyList<string> ReadUnfinishedSteps(string projectPath)
    {
        try
        {
            return VSProjectHelper.GetUnfinishedUpgrades(projectPath).SelectMany(x => x.Steps).ToList();
        }
        catch (Exception)
        {
            return [];
        }
    }

    // Follows the reports, created and deleted, and the project files, whose records change, in the folders of the projects
    private void Watch()
    {
        StopWatching();
        foreach (var directory in Projects().Select(x => Path.GetDirectoryName(x.Report)!).Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
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
        var retries = await Task.Run(() => projects.Select(x => packageSession.RetryUnfinishedUpgrades(x, log)).ToList());
        await Update();

        var left = retries.SelectMany(x => x.StepsLeft).ToList();
        var changed = retries.Any(x => x.ChangedProject);
        var text = new StringBuilder();
        text.AppendLine(left.Count == 0
            ? Tr._p("Message", "The steps left are done: the upgrade is finished.")
            : string.Format(Tr._p("Message", "These steps are still left: {0}. Code steps don't run again: do them by hand, as {1} next to the project lists them, then mark the upgrade as finished."), string.Join(", ", left.Select(x => UpgradeStepInfo.IsStepId(x) ? $"`{x}`" : x)), UpgradeReportWriter.FileName));
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
        await ServiceProvider.Get<IDialogService>().MessageBoxAsync(text.ToString(), MessageBoxButton.OK, left.Count == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        if (changed)
            ReloadCommand?.Execute();
    }

    private async Task MarkFinishedAsync()
    {
        var text = new StringBuilder();
        text.AppendLine(Tr._p("Message", "Mark the last upgrade as finished? Do it once you did by hand every step it left:"));
        foreach (var (name, _, steps) in unfinished)
        {
            text.AppendLine();
            text.AppendLine($"**{name}**: {string.Join(", ", steps.Select(x => UpgradeStepInfo.IsStepId(x) ? $"`{x}`" : x))}");
        }
        if (await ConfirmAsync(text.ToString(), Tr._p("Button", "Mark as finished")) != true)
            return;

        foreach (var (name, projectPath, _) in unfinished)
        {
            try
            {
                VSProjectHelper.MarkUpgradesFinished(projectPath);
            }
            catch (Exception e) when (e is Microsoft.Build.Exceptions.InvalidProjectFileException or IOException or UnauthorizedAccessException)
            {
                await ServiceProvider.Get<IDialogService>().MessageBoxAsync(string.Format(Tr._p("Message", "Could not mark the upgrade of {0} as finished: {1}"), name, e.Message), MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        await Update();
    }

    private async Task RemoveReportsAsync()
    {
        var text = string.Format(Tr._p("Message", "Remove {0}? Do it once you handled every item it lists."), string.Join(", ", Reports));
        if (await ConfirmAsync(text, Tr._p("Button", "Remove")) != true)
            return;

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
