// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Reflection;
using Stride.CrashReport;

// Last-resort handler for unexpected CLI crashes: saves a report to the crash store and — on an
// interactive terminal — offers to send it right away; otherwise prints how to send it later with
// 'stride crash send'. Expected failures are handled per-command; anything reaching this is a bug.
internal static class CliCrashHandler
{
    private const string ApplicationName = "Cli";

    public static async Task<int> ReportAsync(Exception exception)
    {
        Console.Error.WriteLine($"stride: unexpected error: {exception}");

        if (CrashPolicy.ResolveMode() == CrashMode.Off)
            return 1;

        // Snapshot the other threads (the crashing thread's stack comes from the exception).
        var threads = ThreadSnapshot.CaptureAtCurrentThread(out var crashedThreadId, out var crashedThreadName);

        CrashRun run;
        StoredCrash crash;
        try
        {
            var data = new CrashReportData
            {
                ["Application"] = ApplicationName,
                ["Exception"] = exception.ToString(),
            };
            CrashReportAnonymizer.Scrub(data);

            crash = StoredCrash.FromReportData(data);
            crash.Application = ApplicationName;
            var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            crash.Version = string.IsNullOrEmpty(informational) ? "unknown" : informational;
            crash.Environment = CrashReportSender.BuildEnvironment ?? "local";
            crash.TimestampUtc = DateTime.UtcNow.ToString("o");
            crash.Signature = CrashSignature.Compute(exception, ApplicationName);
            crash.Exceptions = StoredException.Capture(exception);
            crash.Threads = threads;
            crash.CrashedThreadId = crashedThreadId;
            crash.CrashedThreadName = crashedThreadName;

            run = new CrashStore(ApplicationName).CreateRun();
            run.Add(crash);
        }
        catch
        {
            return 1; // saving the report must never mask the failure itself
        }

        switch (CrashPolicy.ResolveAction())
        {
            case CrashAction.Send:
                // Auto-submit (STRIDE_CRASH_MODE=send, e.g. Stride's own CI). Keep the files: the runner is
                // ephemeral and an artifact step may still want them; failure keeps them for retry anyway.
                await TrySendAsync(run, crash, deleteAfterSend: false);
                break;

            case CrashAction.Report when !Console.IsInputRedirected && !CrashReportSender.IsDisabled:
                // Attended with a real terminal: the console is our consent dialog.
                Console.Error.Write("Send this crash report to the Stride team? [y/N] ");
                var answer = Console.ReadLine();
                if (answer != null && answer.Trim().StartsWith("y", StringComparison.OrdinalIgnoreCase)
                    && await TrySendAsync(run, crash, deleteAfterSend: true))
                    break;
                PrintSavedHint(run);
                break;

            default:
                PrintSavedHint(run);
                break;
        }

        return 1;
    }

    private static async Task<bool> TrySendAsync(CrashRun run, StoredCrash crash, bool deleteAfterSend)
    {
        try
        {
            var dsn = CrashReportSender.ResolveDsn();
            await CrashReportSender.SendAsync(crash, run.ReadSendableDump(crash), dsn);
            if (deleteAfterSend)
                run.Delete();
            Console.Error.WriteLine("Crash report sent. Thank you!");
            return true;
        }
        catch (Exception sendException)
        {
            Console.Error.WriteLine($"Could not send the crash report: {sendException.Message}");
            return false;
        }
    }

    private static void PrintSavedHint(CrashRun run)
    {
        Console.Error.WriteLine($"Crash report saved to {run.Directory}");
        if (!CrashReportSender.IsDisabled)
            Console.Error.WriteLine($"Submit it with: stride crash send \"{run.Directory}\"");
    }
}
