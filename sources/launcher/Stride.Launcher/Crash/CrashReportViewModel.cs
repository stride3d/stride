// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Launcher.Assets.Localization;

namespace Stride.Launcher.Crash;

internal sealed class CrashReportViewModel : ViewModelBase
{
    private readonly IDispatcherService dispatcherService;

    private readonly Func<string?, Task> setClipboard;
    private readonly CancellationTokenSource exitToken;
    private readonly CrashReportData report;

    private bool isReportVisible;

    public CrashReportViewModel(CrashReportArgs args, Func<string?, Task> setClipboard, CancellationTokenSource exitToken)
        : base(new ViewModelServiceProvider())
    {
        this.exitToken = exitToken;
        this.setClipboard = setClipboard;

        ServiceProvider.RegisterService(dispatcherService = DispatcherService.Create());
        ServiceProvider.RegisterService(new DialogService(dispatcherService) { ApplicationName = Launcher.ApplicationName });             

        report = ComputeReport(args);

        CopyReportCommand = new AnonymousTaskCommand(ServiceProvider, OnCopyReport);
        CloseCommand = new AnonymousCommand(ServiceProvider, OnClose);
        OpenIssueCommand = new AnonymousTaskCommand(ServiceProvider, OnOpenIssue);
        ViewReportCommand = new AnonymousCommand(ServiceProvider, OnViewReport);
    }

    public bool IsReportVisible
    {
        get => isReportVisible;
        set => SetValue(ref isReportVisible, value);
    }

    public CrashReportData Report
    {
        get => report;
    }

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
        catch (Exception ex)
        {
            DialogService.MainWindow!.Topmost = false;
            await ServiceProvider.Get<IDialogService>().MessageBoxAsync(Strings.ErrorOpeningBrowser, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnViewReport()
    {
        IsReportVisible = true;
    }

    private static CrashReportData ComputeReport(CrashReportArgs args)
    {
        return new CrashReportData
        {
            ["Application"] = Launcher.ApplicationName,
            ["ThreadName"] = args.ThreadName,
#if DEBUG
            ["ProcessID"] = Environment.ProcessId,
            ["CurrentDirectory"] = Environment.CurrentDirectory,
#endif
            ["OsArch"] = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ["OsVersion"] = Environment.OSVersion,
            ["ProcessorCount"] = Environment.ProcessorCount,
            ["Exception"] = args.Exception.FormatFull(),
        };
    }
}
