// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Translation;

namespace Stride.Core.Assets.Editor.ViewModels;

/// <summary>
/// The banner of the main window while a project of the session has an <see cref="UpgradeReportWriter.FileName"/> file: its
/// last upgrades left items to do by hand. It opens the files, and goes away once they are deleted.
/// </summary>
/// <remarks>The file existing is the only signal: its items, which the user ticks by hand, are not read.</remarks>
public sealed class UpgradeReportBannerViewModel : ViewModelBase, IDisposable
{
    private readonly IDispatcherService? dispatcher;
    private readonly List<FileSystemWatcher> watchers = [];
    private SessionViewModel? session;
    private IReadOnlyList<string> reports = [];
    private bool isVisible;
    private string message = string.Empty;
    private int updateQueued;

    /// <param name="serviceProvider">The editor's services.</param>
    public UpgradeReportBannerViewModel(IViewModelServiceProvider serviceProvider)
        : base(serviceProvider)
    {
        dispatcher = serviceProvider.TryGet<IDispatcherService>();
        OpenCommand = new AnonymousCommand(serviceProvider, Open);
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
    /// The full paths of the <see cref="UpgradeReportWriter.FileName"/> files of the projects of the session.
    /// </summary>
    public IReadOnlyList<string> Reports { get => reports; private set => SetValue(ref reports, value); }

    /// <summary>
    /// Whether the banner shows: a project of the session has a report.
    /// </summary>
    public bool IsVisible { get => isVisible; private set => SetValue(ref isVisible, value); }

    /// <summary>
    /// What the banner says.
    /// </summary>
    public string Message { get => message; private set => SetValue(ref message, value); }

    /// <summary>
    /// Opens each report with the application of the system for Markdown files.
    /// </summary>
    public ICommandBase OpenCommand { get; }

    /// <summary>
    /// Looks again for the reports of the projects of the session.
    /// </summary>
    public void Update()
    {
        var projects = Projects().ToList();
        Reports = projects.Select(x => x.Report).Where(File.Exists).ToList();
        IsVisible = Reports.Count > 0;
        Message = Reports.Count switch
        {
            0 => string.Empty,
            1 => string.Format(Tr._p("Message", "The last upgrade of {0} left items to do by hand: see {1}."), projects.First(x => x.Report == Reports[0]).Name, UpgradeReportWriter.FileName),
            _ => string.Format(Tr._p("Message", "The last upgrades of {0} projects left items to do by hand: see their {1}."), Reports.Count, UpgradeReportWriter.FileName),
        };
    }

    /// <inheritdoc />
    public void Dispose()
    {
        StopWatching();
        session = null;
    }

    // The projects of the session, each with the path of its report: next to the project file, or the package file without one
    private IEnumerable<(string Name, string Report)> Projects()
        => session?.PackageSession.Projects
            .Select(x => (x.Package.Meta.Name, Path: x is SolutionProject project ? project.FullPath : x.Package.FullPath))
            .Where(x => x.Path is not null)
            .Select(x => (x.Name, Path.Combine(x.Path!.GetFullDirectory().ToOSPath(), UpgradeReportWriter.FileName))) ?? [];

    // Follows the creation and deletion of the reports in the folders of the projects of the session
    private void Watch()
    {
        StopWatching();
        foreach (var directory in Projects().Select(x => Path.GetDirectoryName(x.Report)!).Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            var watcher = new FileSystemWatcher(directory, UpgradeReportWriter.FileName) { NotifyFilter = NotifyFilters.FileName };
            watcher.Created += ReportChanged;
            watcher.Deleted += ReportChanged;
            watcher.Renamed += ReportChanged;
            watcher.EnableRaisingEvents = true;
            watchers.Add(watcher);
        }
        Update();
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
            Update();
            return;
        }
        if (Interlocked.Exchange(ref updateQueued, 1) == 1)
            return;
        _ = dispatcher.InvokeAsync(() =>
        {
            Interlocked.Exchange(ref updateQueued, 0);
            Update();
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
}
