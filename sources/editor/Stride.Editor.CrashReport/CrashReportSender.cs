// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sentry;

namespace Stride.Editor.CrashReport;

/// <summary>
/// Sends crash reports to Sentry. Official builds bake their destination in through the StrideSentryDsn
/// property; source builds have no destination and let the user pick one per crash.
/// </summary>
public static class CrashReportSender
{
    /// <summary>
    /// Sentry project collecting reports from source builds. Offered as an explicit choice in the crash
    /// window, never used silently.
    /// </summary>
    public const string DevChannelDsn = "https://91a43cb8256376131ba96ff24a749567@crash.stride3d.net/4511870298357840";

    /// <summary>DSN baked in at build time, if any.</summary>
    public static string BuildDsn { get; } = GetMetadata("SentryDsn");

    /// <summary>True when the build opted out of crash sending entirely (StrideSentryDsn=false).</summary>
    public static bool IsDisabled { get; } = GetMetadata("SentryDisabled") == "true";

    public static async Task SendAsync(CrashReportData report, string applicationName, Exception exception, string dsn, bool includeMinidump = false,
        string feedbackName = null, string feedbackEmail = null, string feedbackMessage = null)
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var package = applicationName.Replace(" ", "").ToLowerInvariant();
        var minidump = includeMinidump ? MinidumpWriter.TryWrite() : null;

        using var sdk = SentrySdk.Init(options =>
        {
            options.Dsn = dsn;
            options.Release = $"{package}@{version}";
            options.Environment = GetMetadata("SentryEnvironment") ?? "local";
            options.IsGlobalModeEnabled = true;
            options.AutoSessionTracking = false;
            // No user identity beyond the SDK's random installation id; a contact email only travels
            // through the feedback when the user typed one
            options.SendDefaultPii = false;
            options.SetBeforeSend((sentryEvent, _) => Anonymize(sentryEvent));
        });

        SentrySdk.ConfigureScope(scope =>
        {
            scope.AddAttachment(Encoding.UTF8.GetBytes(report.ToString()), "report.txt");
            // Deliberately not AttachmentType.Minidump: that would make Sentry synthesize a second event
            // from the dump; this is a plain file for maintainers to download into a debugger
            if (minidump != null)
                scope.AddAttachment(minidump, "minidump.dmp");
            scope.SetTag("application", applicationName);
            MapReport(scope, report);
        });

        var sentryEvent = exception != null
            ? new SentryEvent(exception)
            : new SentryEvent { Message = new SentryMessage { Formatted = report["Exception"] } };
        sentryEvent.Level = SentryLevel.Fatal;

        var eventId = SentrySdk.CaptureEvent(sentryEvent);

        // The user's own words are sent as-is: typing them is the consent
        if (!string.IsNullOrWhiteSpace(feedbackName) || !string.IsNullOrWhiteSpace(feedbackEmail) || !string.IsNullOrWhiteSpace(feedbackMessage))
        {
            var feedback = new SentryFeedback(
                string.IsNullOrWhiteSpace(feedbackMessage) ? "No description provided." : feedbackMessage.Trim(),
                contactEmail: string.IsNullOrWhiteSpace(feedbackEmail) ? null : feedbackEmail.Trim(),
                name: string.IsNullOrWhiteSpace(feedbackName) ? null : feedbackName.Trim(),
                associatedEventId: eventId);
            SentrySdk.CaptureFeedback(feedback);
        }

        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Maps report entries onto Sentry structures: searchable tags, GPU/memory contexts, log lines and
    /// undo/redo actions as breadcrumbs, everything else as extra data. The full report text stays
    /// attached as report.txt, which is exactly what the window's View report shows.
    /// </summary>
    private static void MapReport(Scope scope, CrashReportData report)
    {
        var gpus = new Dictionary<string, Dictionary<string, string>>();
        var memory = new Dictionary<string, string>();
        string activeAdapter = null;

        foreach (var (key, value) in report.Data)
        {
            switch (key)
            {
                case "Exception":
                    continue; // the event itself
                case "Log":
                    foreach (var line in value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        scope.AddBreadcrumb(line, "log");
                    continue;
                case "LastActions":
                    AddActionBreadcrumbs(scope, value);
                    continue;
                case "StrideVersion":
                    scope.SetTag("stride.version", value);
                    continue;
                case "GraphicsPlatform":
                    scope.SetTag("graphics.api", value);
                    continue;
                case "GraphicsAdapter":
                    activeAdapter = value;
                    continue;
                case "OpenedAssets":
                    scope.Contexts["Opened Assets"] = value.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    continue;
            }

            var dot = key.IndexOf('.');
            if (dot > 0 && key.StartsWith("GPU", StringComparison.Ordinal))
            {
                var gpuName = key[..dot];
                if (!gpus.TryGetValue(gpuName, out var gpu))
                    gpus.Add(gpuName, gpu = []);
                gpu[key[(dot + 1)..]] = value;
                continue;
            }
            if (dot > 0 && key.StartsWith("Memory.", StringComparison.Ordinal))
            {
                memory[key[(dot + 1)..]] = value;
                continue;
            }

            scope.SetExtra(key, value);
        }

        foreach (var (name, properties) in gpus)
            scope.Contexts[name.ToLowerInvariant()] = properties;
        if (memory.Count > 0)
            scope.Contexts["memory"] = memory;

        // The adapter the application actually renders with, matched against the WMI inventory for driver info
        if (activeAdapter != null)
        {
            scope.SetTag("gpu.name", activeAdapter);
            scope.Contexts.Gpu.Name = activeAdapter;
            var wmiMatch = gpus.Values.FirstOrDefault(x => x.GetValueOrDefault("Name") == activeAdapter);
            if (wmiMatch != null)
            {
                scope.Contexts.Gpu.VendorName = wmiMatch.GetValueOrDefault("AdapterCompatibility");
                if (wmiMatch.TryGetValue("DriverVersion", out var driverVersion))
                {
                    scope.Contexts.Gpu.Version = driverVersion;
                    scope.SetTag("gpu.driver", driverVersion);
                }
            }
        }
    }

    /// <summary>
    /// Each top-level "* [Name]" line of the actions dump becomes one breadcrumb; its indented operation
    /// lines travel in the breadcrumb data.
    /// </summary>
    private static void AddActionBreadcrumbs(Scope scope, string lastActions)
    {
        string title = null;
        var operations = new List<string>();

        void Flush()
        {
            if (title == null)
                return;
            var data = operations.Count > 0
                ? new Dictionary<string, string> { ["operations"] = string.Join("\n", operations) }
                : null;
            scope.AddBreadcrumb(title, "action", data: data);
            operations.Clear();
        }

        foreach (var line in lastActions.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            if (line.StartsWith("* ", StringComparison.Ordinal))
            {
                Flush();
                title = line[2..].TrimEnd();
            }
            else if (title != null)
            {
                operations.Add(line.Trim());
            }
        }
        Flush();
    }

    /// <summary>
    /// The Sentry event carries its own copy of messages and stack frames, so it needs the same scrubbing
    /// as the report text.
    /// </summary>
    private static SentryEvent Anonymize(SentryEvent sentryEvent)
    {
        if (sentryEvent.Message != null)
        {
            sentryEvent.Message.Formatted = CrashReportAnonymizer.Scrub(sentryEvent.Message.Formatted);
            sentryEvent.Message.Message = CrashReportAnonymizer.Scrub(sentryEvent.Message.Message);
        }

        foreach (var exception in sentryEvent.SentryExceptions ?? [])
        {
            exception.Value = CrashReportAnonymizer.Scrub(exception.Value);
            foreach (var frame in exception.Stacktrace?.Frames ?? [])
            {
                frame.FileName = CrashReportAnonymizer.Scrub(frame.FileName);
                frame.AbsolutePath = CrashReportAnonymizer.Scrub(frame.AbsolutePath);
            }
        }

        return sentryEvent;
    }

    private static string GetMetadata(string key)
    {
        return typeof(CrashReportSender).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == key)?.Value;
    }
}
