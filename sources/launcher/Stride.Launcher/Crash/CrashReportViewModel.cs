// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Avalonia.Services;
using Stride.Core.Presentation.Commands;
using Stride.Core.Presentation.Services;
using Stride.Core.Presentation.ViewModels;
using Stride.Core.Windows;
using CrashCore = Stride.CrashReport;

namespace Stride.Crash.ViewModels;

internal sealed class CrashReportViewModel : ViewModelBase
{
    // Sentry app id: no "Stride" prefix (every report already lands in a Stride project) so the tag is
    // "Launcher" and the release is launcher@version, matching the GameStudio convention.
    private const string SentryApplication = "Launcher";

    private readonly string applicationName;
    private readonly Exception exception;
    private readonly CancellationTokenSource exitToken;
    private readonly Func<string?, Task> setClipboard;

    private bool isReportVisible;
    private bool canSendReport;
    private bool isSending;
    private string? sendStatus;
    private string? feedbackMessage;
    private string? feedbackName;
    private string? feedbackEmail;

    public CrashReportViewModel(string applicationName, CrashReportArgs args, Func<string?, Task> setClipboard, CancellationTokenSource exitToken)
        : base(new ViewModelServiceProvider())
    {
        this.applicationName = applicationName;
        this.exception = args.Exception;
        this.exitToken = exitToken;
        this.setClipboard = setClipboard;

        var dispatcher = DispatcherService.Create();
        ServiceProvider.RegisterService(dispatcher);
        ServiceProvider.RegisterService(new DialogService(dispatcher) { ApplicationName = applicationName });

        Report = ComputeReport(args);
        // A source build has no baked destination; sending it is offered but goes to the dev channel (disclosed
        // in the window). A build that opted out (StrideSentryDsn=false) hides the button entirely.
        canSendReport = !CrashCore.CrashReportSender.IsDisabled;

        CopyReportCommand = new AnonymousTaskCommand(ServiceProvider, OnCopyReport);
        CloseCommand = new AnonymousCommand(ServiceProvider, OnClose);
        OpenIssueCommand = new AnonymousTaskCommand(ServiceProvider, OnOpenIssue);
        ViewReportCommand = new AnonymousCommand(ServiceProvider, OnViewReport);
        SendReportCommand = new AnonymousTaskCommand(ServiceProvider, OnSendReport);
    }

    public string ApplicationName => applicationName;

    public bool IsReportVisible
    {
        get => isReportVisible;
        set => SetValue(ref isReportVisible, value);
    }

    /// <summary>Whether the crash can be sent (hidden once opted out or already sent).</summary>
    public bool CanSendReport
    {
        get => canSendReport;
        set => SetValue(ref canSendReport, value);
    }

    public bool IsSending
    {
        get => isSending;
        set => SetValue(ref isSending, value);
    }

    public string? SendStatus
    {
        get => sendStatus;
        set => SetValue(ref sendStatus, value);
    }

    /// <summary>Optional "what were you doing" description; sent as feedback only if the user typed something.</summary>
    public string? FeedbackMessage
    {
        get => feedbackMessage;
        set => SetValue(ref feedbackMessage, value);
    }

    /// <summary>Optional contact name, sent only if the user typed it.</summary>
    public string? FeedbackName
    {
        get => feedbackName;
        set => SetValue(ref feedbackName, value);
    }

    /// <summary>Optional contact email, used only to follow up on this crash.</summary>
    public string? FeedbackEmail
    {
        get => feedbackEmail;
        set => SetValue(ref feedbackEmail, value);
    }

    public CrashReportData Report { get; }

    public ICommandBase CopyReportCommand { get; }
    public ICommandBase CloseCommand { get; }
    public ICommandBase OpenIssueCommand { get; }
    public ICommandBase ViewReportCommand { get; }
    public ICommandBase SendReportCommand { get; }

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

    private async Task OnSendReport()
    {
        IsSending = true;
        SendStatus = "Sending crash report…";
        try
        {
            // Bridge the launcher's report to the shared UI-agnostic sender.
            var data = new CrashCore.CrashReportData();
            foreach (var (key, value) in Report.Data)
                data[key] = value?.ToString();
            CrashCore.CrashReportAnonymizer.Scrub(data);

            var dsn = CrashCore.CrashReportSender.BuildDsn ?? CrashCore.CrashReportSender.DevChannelDsn;
            await CrashCore.CrashReportSender.SendAsync(data, SentryApplication, exception, dsn,
                feedbackName: FeedbackName, feedbackEmail: FeedbackEmail, feedbackMessage: FeedbackMessage);

            SendStatus = "Thank you. The crash report has been sent.";
            CanSendReport = false; // consent is per crash; don't offer a second send
        }
        catch (Exception)
        {
            SendStatus = "The crash report could not be sent. Please use New GitHub Issue instead.";
        }
        finally
        {
            IsSending = false;
        }
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
            ["CommandArgs"] = string.Join(" ", AppHelper.GetCommandLineArgs()),
#endif
            ["OSArch"] = RuntimeInformation.OSArchitecture,
            ["OSDescription"] = RuntimeInformation.OSDescription,
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
