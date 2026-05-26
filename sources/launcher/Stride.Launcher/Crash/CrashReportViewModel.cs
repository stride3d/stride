// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.Crash.ViewModels;

internal sealed class CrashReportViewModel : ViewModelBase
{
    private readonly string applicationName;
    private readonly CancellationTokenSource exitToken;
    private readonly Func<string?, Task> setClipboard;

    private bool isReportVisible;

    public CrashReportViewModel(string applicationName, CrashReportArgs args, Func<string?, Task> setClipboard, CancellationTokenSource exitToken)
        : base(new ViewModelServiceProvider())
    {
        this.applicationName = applicationName;
        this.exitToken = exitToken;
        this.setClipboard = setClipboard;

        var dispatcher = DispatcherService.Create();
        ServiceProvider.RegisterService(dispatcher);
        ServiceProvider.RegisterService(new DialogService(dispatcher) { ApplicationName = applicationName });

        Report = ComputeReport(args);

        CopyReportCommand = new AnonymousTaskCommand(ServiceProvider, OnCopyReport);
        CloseCommand = new AnonymousCommand(ServiceProvider, OnClose);
        OpenIssueCommand = new AnonymousTaskCommand(ServiceProvider, OnOpenIssue);
        ViewReportCommand = new AnonymousCommand(ServiceProvider, OnViewReport);
    }

    public string ApplicationName => applicationName;

    public bool IsReportVisible
    {
        get => isReportVisible;
        set => SetValue(ref isReportVisible, value);
    }

    public CrashReportData Report { get; }

    public ICommandBase CopyReportCommand { get; }
    public ICommandBase CloseCommand { get; }
    public ICommandBase OpenIssueCommand { get; }
    public ICommandBase ViewReportCommand { get; }

    private void OnClose()
    {
        exitToken.Cancel();
    }

    private Task OnCopyReport()
    {
        return setClipboard.Invoke(Report.ToJson());
    }

    private async Task OnOpenIssue()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/stride3d/stride/issues/new?labels=bug&template=bug_report.md&",
                UseShellExecute = true
            });
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
            DialogService.MainWindow!.Topmost = false;
            // FIXME: localize resource string
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync("An error occurred while trying to open a web browser", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnViewReport()
    {
        IsReportVisible = true;
    }

    private CrashReportData ComputeReport(CrashReportArgs args)
    {
        return new()
        {
            ["Application"] = applicationName,
            ["ThreadName"] = args.ThreadName,
#if DEBUG
            ["ProcessID"] = Environment.ProcessId,
            ["CurrentDirectory"] = Environment.CurrentDirectory,
#endif
            ["OsArch"] = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ["OsVersion"] = Environment.OSVersion,
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["Exception"] = args.Exception.FormatFull(),
            ["LastLogs"] = FormatLogs(args.Logs),
        };

        static string FormatLogs(string[] logs)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < logs.Length; i++)
            {
                var log = logs[i];
                builder.AppendLine($"{i + 1}: {log}");
            }
            return builder.ToString();
        }
    }
}
