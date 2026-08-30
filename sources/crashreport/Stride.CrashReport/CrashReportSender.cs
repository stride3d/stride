// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sentry;

namespace Stride.CrashReport;

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

    /// <summary>Environment tag baked in at build time (release/nightly), if any; headless captures fall back to this.</summary>
    public static string BuildEnvironment { get; } = GetMetadata("SentryEnvironment");

    public static Task SendAsync(CrashReportData report, string applicationName, Exception exception, string dsn, bool includeMinidump = false,
        string feedbackName = null, string feedbackEmail = null, string feedbackMessage = null)
    {
        // In-process hosts (GameStudio, launcher) crash as themselves: identity comes from the running
        // assembly, and, if asked, we dump the still-live faulting process on the spot.
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var (version, commit) = SplitVersion(informational);
        var minidump = includeMinidump && OperatingSystem.IsWindows() ? MinidumpWriter.TryWrite() : null;
        return SendCoreAsync(report, applicationName, version, commit, GetMetadata("SentryEnvironment") ?? "local",
            exception, dsn, minidump, feedbackName, feedbackEmail, feedbackMessage);
    }

    /// <summary>
    /// Sends a crash a headless tool captured earlier and wrote to disk. The originating process is gone,
    /// so identity (app / version / environment) and the dump come from the <paramref name="crash"/> file,
    /// never from this reporter's own assembly — otherwise every compiler crash would be tagged as the
    /// reporter (crashreporter@x instead of assetcompiler@y).
    /// </summary>
    public static Task SendAsync(StoredCrash crash, byte[] dump, string dsn,
        string feedbackName = null, string feedbackEmail = null, string feedbackMessage = null)
    {
        var (version, commit) = SplitVersion(crash.Version);
        var environment = string.IsNullOrEmpty(crash.Environment) ? "local" : crash.Environment;
        // No live Exception object survives the handoff; the event is rebuilt from the stored report text.
        return SendCoreAsync(crash.ToReportData(), crash.Application, version, commit, environment,
            exception: null, dsn, dump, feedbackName, feedbackEmail, feedbackMessage);
    }

    // The Sentry release drops the +g<sha> build metadata so it matches the NuGet version and git tag; the
    // commit rides along as a tag instead of giving every build its own release entry.
    private static (string version, string commit) SplitVersion(string informational)
    {
        informational ??= "unknown";
        var plus = informational.IndexOf('+');
        var version = plus >= 0 ? informational[..plus] : informational;
        var commit = plus >= 0 ? informational[(plus + 1)..].TrimStart('g') : null;
        return (version, commit);
    }

    private static async Task SendCoreAsync(CrashReportData report, string applicationName, string version, string commit, string environment,
        Exception exception, string dsn, byte[] minidump,
        string feedbackName, string feedbackEmail, string feedbackMessage)
    {
        // Application name doubles as the "application" tag and, lowercased, the release package id (e.g.
        // "GameStudio" -> gamestudio@version). No "Stride" prefix: every report already lands in a Stride project.
        var package = applicationName.Replace(" ", "").ToLowerInvariant();

        using var sdk = SentrySdk.Init(options =>
        {
            options.Dsn = dsn;
            options.Release = $"{package}@{version}";
            options.Environment = environment;
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
            if (commit != null)
                scope.SetTag("commit", commit);
            MapReport(scope, report);
        });

        var sentryEvent = exception != null
            ? new SentryEvent(exception)
            : new SentryEvent { Message = new SentryMessage { Formatted = report["Exception"] ?? "Unknown crash" } };
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
        // Keep only the install id, no location. A concrete non-routable IP is used rather than null so
        // Sentry does not fall back to the forwarded client IP for geolocation; 0.0.0.0 geolocates to nothing.
        sentryEvent.User.IpAddress = "0.0.0.0";

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
