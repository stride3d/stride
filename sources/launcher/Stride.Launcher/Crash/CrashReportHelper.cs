// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Stride.Core.Extensions;
using Stride.Core.Windows;
using Stride.CrashReport;

namespace Stride.Launcher.Crash;

internal static class CrashReportHelper
{
    private const string ApplicationName = "Launcher";

    public static void SendReport(CrashReportArgs args)
    {
        var report = new CrashReportData
        {
            ["Application"] = ApplicationName,
            ["ThreadName"] = args.ThreadName ?? "",
#if DEBUG
            ["ProcessID"] = Environment.ProcessId.ToString(),
            ["CurrentDirectory"] = Environment.CurrentDirectory,
            ["CommandArgs"] = string.Join(" ", AppHelper.GetCommandLineArgs()),
#endif
            ["OSArch"] = RuntimeInformation.OSArchitecture.ToString(),
            ["OSDescription"] = RuntimeInformation.OSDescription,
            ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
            ["Cpu"] = AppHelper.GetCpuName(),
            ["Exception"] = args.Exception.FormatFull(),
            ["LastLogs"] = FormatLogs(args.Logs),
        };

        foreach (var (key, value) in AppHelper.GetMemoryInfo())
            report[key] = value;

        CrashReportAnonymizer.Scrub(report);

        // Unattended sessions (CI, remote/service sessions) must not block on a dialog; STRIDE_CRASH_MODE=save/send/off
        // are explicit overrides. Only the interactive case shows a window.
        switch (CrashPolicy.ResolveAction())
        {
            case CrashAction.Ignore:
                return;
            case CrashAction.Save:
                SaveToStore(report, args);
                return;
            case CrashAction.Send:
                try
                {
                    if (CrashReportSender.IsDisabled)
                        throw new InvalidOperationException("Crash sending is disabled in this build.");
                    CrashReportSender.SendAsync(report, ApplicationName, args.Exception, CrashReportSender.ResolveDsn(),
                        threads: args.Threads, crashedThreadId: args.ThreadId, crashedThreadName: args.ThreadName).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    e.Ignore();
                    SaveToStore(report, args); // a failed send must not lose the report
                }
                return;
        }

        // Attended: the same out-of-process reporter the other hosts use. We are still alive here (the runtime
        // waits for this handler), so the report and a dump go to the store, the reporter is spawned with our pid,
        // and we block until it closes -- the freeze an in-process modal dialog gave, and what lets the reporter
        // write a full memory dump of this live process on demand. No reporter: the run stays for 'stride crash send'.
        var run = SaveToStore(report, args);
        if (run == null)
            return;
        try
        {
            using var reporter = NativeCrashReporting.TrySpawnHostCrashReporter(run.Directory);
            reporter?.WaitForExit();
        }
        catch (Exception e)
        {
            e.Ignore();
        }
    }

    /// <summary>
    /// Persists the report to the crash store with the structured exception and the thread snapshot, plus a
    /// triage dump of this process on Windows (the only platform <see cref="MinidumpWriter"/> supports; the
    /// Launcher, unlike GameStudio, also ships for Linux). Returns the run, or null when the store could not
    /// be written.
    /// </summary>
    private static CrashRun? SaveToStore(CrashReportData report, CrashReportArgs args)
    {
        try
        {
            var crash = StoredCrash.FromReportData(report);
            crash.Application = ApplicationName;
            var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            crash.Version = string.IsNullOrEmpty(informational) ? "unknown" : informational;
            crash.Environment = CrashReportSender.BuildEnvironment ?? "local";
            crash.TimestampUtc = DateTime.UtcNow.ToString("o");
            crash.Signature = CrashSignature.Compute(args.Exception, ApplicationName);
            crash.Exceptions = StoredException.Capture(args.Exception);
            crash.Threads = args.Threads?.ToList() ?? [];
            crash.CrashedThreadId = args.ThreadId;
            crash.CrashedThreadName = args.ThreadName;

            var run = new CrashStore(ApplicationName).CreateRun();
            if (OperatingSystem.IsWindows())
            {
                if (CrashPolicy.FullMemoryDump())
                {
                    crash.DumpIsFullMemory = true;
                    // The analyzer can't see that OperatingSystem.IsWindows() above also guards this callback's
                    // later, deferred invocation by CrashRun.Add.
#pragma warning disable CA1416
                    run.Add(crash, path => MinidumpWriter.TryWriteFile(path, fullMemory: true));
#pragma warning restore CA1416
                }
                else
                {
                    run.Add(crash, MinidumpWriter.TryWrite());
                }
            }
            else
            {
                run.Add(crash);
            }
            return run;
        }
        catch (Exception e)
        {
            e.Ignore(); // saving the report must never mask the crash handling itself
            return null;
        }
    }

    private static string FormatLogs(string[] logs)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < logs.Length; i++)
        {
            builder.AppendLine($"{i + 1}: {logs[i]}");
        }
        return builder.ToString();
    }
}
