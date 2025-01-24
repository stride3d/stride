// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Text;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;

namespace Stride.GameStudio.Avalonia.Desktop.Crash;

internal sealed class CrashReportViewModel : ViewModelBase
{
    private readonly NullDispatcherService dispatcherService = new();

    private readonly Func<string?, Task> setClipboard;
    private readonly CancellationTokenSource exitToken;
    private readonly CrashReportData report;

    private bool isReportVisible;
    private bool rememberEmail;

    public CrashReportViewModel(CrashReportArgs args, Func<string?, Task> setClipboard, CancellationTokenSource exitToken)
        : base(new ViewModelServiceProvider())
    {
        this.exitToken = exitToken;
        this.setClipboard = setClipboard;
        ServiceProvider.RegisterService(dispatcherService);

        report = ComputeReport(args);

        CopyReportCommand = new AnonymousTaskCommand(ServiceProvider, OnCopyReport);
        CloseCommand = new AnonymousCommand(ServiceProvider, OnClose);
        SendCommand = DisabledCommand.Instance;
        ViewReportCommand = new AnonymousCommand(ServiceProvider, OnViewReport);
    }

    public string? Description
    {
        get => report["UserMessage"];
        set => SetValue(() => report["UserMessage"] = value, nameof(Description), nameof(Report));
    }

    public string? EmailAddress
    {
        get => report["UserEmail"];
        set => SetValue(() => report["UserEmail"] = value, nameof(EmailAddress), nameof(Report));
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

    public bool RememberEmail
    {
        get => rememberEmail;
        set => SetValue(ref rememberEmail, value);
    }

    public ICommandBase CopyReportCommand { get; }
    public ICommandBase CloseCommand { get; }
    public ICommandBase SendCommand { get; }
    public ICommandBase ViewReportCommand { get; }

    private void OnClose()
    {
        exitToken.Cancel();
    }

    private Task OnCopyReport()
    {
        return setClipboard.Invoke(Report.ToJson());
    }

    private void OnViewReport()
    {
        IsReportVisible = true;
    }

    private static CrashReportData ComputeReport(CrashReportArgs args)
    {
        return new CrashReportData
        {
            ["Application"] = "GameStudio",
            ["UserEmail"] = "",
            ["UserMessage"] = "",
            ["StrideVersion"] = StrideVersion.NuGetVersion,
            ["ThreadName"] = args.ThreadName,
#if DEBUG
            ["CrashLocation"] = args.Location.ToString(),
            ["ProcessID"] = Environment.ProcessId.ToString(),
#endif
            ["CurrentDirectory"] = Environment.CurrentDirectory,
            ["OsArch"] = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
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
