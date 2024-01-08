// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.CrashReport;

namespace Stride.Launcher.Crash;

internal sealed class CrashReportViewModel : ViewModelBase
{
    public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";

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
        DontSendCommand = new AnonymousCommand(ServiceProvider, OnDontSend);
        OpenPrivacyPolicyCommand = new AnonymousCommand(ServiceProvider, OnOpenPrivacyPolicy);
#if DEBUG
        SendCommand = new AnonymousTaskCommand(ServiceProvider, OnSend);
#else
        SendCommand = DisabledCommand.Instance;
#endif
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
    public ICommandBase DontSendCommand { get; }
    public ICommandBase OpenPrivacyPolicyCommand { get; }
    public ICommandBase SendCommand { get; }
    public ICommandBase ViewReportCommand { get; }

    private void Close()
    {
        exitToken.Cancel();
    }

    private Task OnCopyReport()
    {
        return setClipboard.Invoke(Report.ToJson());
    }

    private void OnDontSend()
    {
        Close();
    }

    private void OnOpenPrivacyPolicy()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = PrivacyPolicyUrl,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        // FIXME: catch only specific exceptions?
        catch (Exception)
        {
            var error = "An error occurred while opening the browser. You can access the privacy policy at the following url:"
                + Environment.NewLine + Environment.NewLine + PrivacyPolicyUrl;
            // TODO: display error
        }
    }
    
#if DEBUG
    private async Task OnSend()
    {
        if (!await SendReport(Report))
        {
            // TODO: display error
        }

        Close();
    }
#endif // DEBUG

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
            ["ThreadName"] = args.ThreadName,
#if DEBUG
            ["ProcessID"] = Process.GetCurrentProcess().Id.ToString(),
#endif
            ["CurrentDirectory"] = Environment.CurrentDirectory,
            ["OsArch"] = Environment.Is64BitOperatingSystem ? "x64" : "x86",
            ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
            ["Exception"] = args.Exception.FormatFull(),
        };
    }

    private static async Task<bool> SendReport(CrashReportData report)
    {
        try
        {
            await CrashReporter.Report(report);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
