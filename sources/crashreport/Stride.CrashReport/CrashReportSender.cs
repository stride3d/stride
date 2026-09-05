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
/// Sends crash reports to Sentry. Official builds (and forks) bake their destination in through the
/// StrideSentryDsn property; source builds without one send to the dev channel.
/// </summary>
public static class CrashReportSender
{
    /// <summary>
    /// Sentry project collecting reports from source builds, kept apart from the release project so local
    /// stacks don't pollute its grouping. Only ever used after the user chose to send.
    /// </summary>
    public const string DevChannelDsn = "https://91a43cb8256376131ba96ff24a749567@crash.stride3d.net/4511870298357840";

    /// <summary>DSN baked in at build time, if any.</summary>
    public static string BuildDsn { get; } = GetMetadata("SentryDsn");

    /// <summary>True when the build opted out of crash sending entirely (StrideSentryDsn=false).</summary>
    public static bool IsDisabled { get; } = GetMetadata("SentryDisabled") == "true";

    /// <summary>Environment tag baked in at build time (release/nightly), if any; headless captures fall back to this.</summary>
    public static string BuildEnvironment { get; } = GetMetadata("SentryEnvironment");

    /// <summary>The crash-reporting privacy policy, linked from the hosts' About pages.</summary>
    public const string PrivacyPolicyUrl = "https://stride3d.net/legal/privacy-policy";

    /// <summary>The DSN to send to: an explicit override wins, else the build DSN, else the dev channel.</summary>
    public static string ResolveDsn(string overrideDsn = null)
        => !string.IsNullOrEmpty(overrideDsn) ? overrideDsn
            : string.IsNullOrEmpty(BuildDsn) ? DevChannelDsn : BuildDsn;

    public static Task SendAsync(CrashReportData report, string applicationName, Exception exception, string dsn, bool includeMinidump = false,
        string feedbackName = null, string feedbackEmail = null, string feedbackMessage = null,
        IReadOnlyList<StoredThread> threads = null, int? crashedThreadId = null, string crashedThreadName = null)
    {
        // In-process hosts (GameStudio, launcher) crash as themselves: identity comes from the running
        // assembly, and, if asked, we dump the still-live faulting process on the spot.
        var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var (version, commit) = SplitVersion(informational);
        var minidump = includeMinidump && OperatingSystem.IsWindows() ? MinidumpWriter.TryWrite() : null;
        return SendCoreAsync(report, applicationName, version, commit, BuildEnvironment ?? "local",
            exception, dsn, minidump, attachments: null, feedbackName, feedbackEmail, feedbackMessage,
            fingerprint: null, structuredExceptions: null, // a live exception groups by, and renders, its own stack
            threads, crashedThreadId, crashedThreadName);
    }

    /// <summary>
    /// Sends a crash a headless tool captured earlier and wrote to disk. The originating process is gone, so
    /// identity (app / version / environment) and the dump come from the <paramref name="crash"/> file, not
    /// from this reporter's own assembly.
    /// </summary>
    public static Task SendAsync(StoredCrash crash, byte[] dump, string dsn,
        IReadOnlyList<(string Name, byte[] Bytes)> attachments = null,
        string feedbackName = null, string feedbackEmail = null, string feedbackMessage = null)
    {
        var (version, commit) = SplitVersion(crash.Version);
        var environment = string.IsNullOrEmpty(crash.Environment) ? "local" : crash.Environment;
        // No live Exception object survives the handoff; the event is rebuilt from the stored report text.
        return SendCoreAsync(crash.ToReportData(), crash.Application, version, commit, environment,
            exception: null, dsn, dump, attachments, feedbackName, feedbackEmail, feedbackMessage,
            fingerprint: crash.Signature, structuredExceptions: crash.Exceptions,
            crash.Threads, crash.CrashedThreadId, crash.CrashedThreadName);
    }

    // Drop the +g<sha> metadata so the release matches the NuGet version and git tag; the commit travels as a tag.
    private static (string version, string commit) SplitVersion(string informational)
    {
        informational ??= "unknown";
        var plus = informational.IndexOf('+');
        var version = plus >= 0 ? informational[..plus] : informational;
        var commit = plus >= 0 ? informational[(plus + 1)..].TrimStart('g') : null;
        return (version, commit);
    }

    private static async Task SendCoreAsync(CrashReportData report, string applicationName, string version, string commit, string environment,
        Exception exception, string dsn, byte[] minidump, IReadOnlyList<(string Name, byte[] Bytes)> attachments,
        string feedbackName, string feedbackEmail, string feedbackMessage, string fingerprint,
        IReadOnlyList<StoredException> structuredExceptions,
        IReadOnlyList<StoredThread> threads, int? crashedThreadId, string crashedThreadName)
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
            // Not raised at the fault site, so the current stack/assemblies are the reporter's, not the crash. Keep
            // them only for an in-process crash; group via the fingerprint below.
            options.AttachStacktrace = false;
            options.ReportAssembliesMode = exception != null ? ReportAssembliesMode.Version : ReportAssembliesMode.None;
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
            // Opt-in files the user checked in the reporter (e.g. the failing asset's definition), already scrubbed.
            if (attachments != null)
                foreach (var (name, bytes) in attachments)
                    scope.AddAttachment(bytes, name);
            scope.SetTag("application", applicationName);
            if (commit != null)
                scope.SetTag("commit", commit);
            MapReport(scope, report);
        });

        SentryEvent sentryEvent;
        if (exception != null)
            sentryEvent = new SentryEvent(exception);                       // live crash: the SDK builds the frames
        else if (structuredExceptions is { Count: > 0 })
            sentryEvent = BuildEventFromStored(structuredExceptions);       // handoff managed crash: rebuild the frames
        else
            sentryEvent = new SentryEvent { Message = new SentryMessage { Formatted = report["Exception"] ?? "Unknown crash" } }; // native / no frames
        sentryEvent.Level = SentryLevel.Fatal;
        // Group by the crash's signature, not the reporter's stack or message.
        if (!string.IsNullOrEmpty(fingerprint))
            sentryEvent.SetFingerprint(new[] { fingerprint });

        // Link the crashing thread to the exception so Sentry shows its stack there and the snapshot stacks for the rest.
        if (crashedThreadId is int crashedId && sentryEvent.SentryExceptions != null)
            foreach (var sentryException in sentryEvent.SentryExceptions)
                sentryException.ThreadId = crashedId;
        var threadList = BuildThreads(threads, crashedThreadId, crashedThreadName);
        if (threadList.Count > 0)
            sentryEvent.SentryThreads = threadList;

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

    // A real Sentry exception event from the stored frames, so a handoff managed crash renders as a proper stacktrace.
    private static SentryEvent BuildEventFromStored(IReadOnlyList<StoredException> stored)
    {
        var exceptions = new List<Sentry.Protocol.SentryException>();
        // Sentry lists the chain innermost-first; ours is outermost-first, so reverse.
        foreach (var ex in stored.AsEnumerable().Reverse())
            exceptions.Add(new Sentry.Protocol.SentryException { Type = ex.Type, Value = ex.Message, Stacktrace = ToStacktrace(ex.Frames) });
        return new SentryEvent { SentryExceptions = exceptions };
    }

    // A Sentry stacktrace from stored frames. .NET frames are newest-first; Sentry wants oldest-first, so reverse.
    private static SentryStackTrace ToStacktrace(IReadOnlyList<StoredFrame> frames)
    {
        var stacktrace = new SentryStackTrace();
        foreach (var frame in ((IEnumerable<StoredFrame>)frames).Reverse())
            stacktrace.Frames.Add(new SentryStackFrame
            {
                Function = frame.Function,
                Module = frame.Module,
                FileName = frame.File,
                LineNumber = frame.Line > 0 ? frame.Line : (int?)null,
                InApp = frame.Module?.StartsWith("Stride", StringComparison.Ordinal) == true,
            });
        return stacktrace;
    }

    // A Sentry thread list from the snapshot, plus the crashing thread flagged (its stack comes from the exception).
    private static IReadOnlyList<SentryThread> BuildThreads(IReadOnlyList<StoredThread> threads, int? crashedThreadId, string crashedThreadName)
    {
        var list = new List<SentryThread>();
        if (crashedThreadId is int id)
            list.Add(new SentryThread { Id = id, Name = crashedThreadName, Crashed = true, Current = true });
        foreach (var t in threads ?? (IReadOnlyList<StoredThread>)Array.Empty<StoredThread>())
            list.Add(new SentryThread { Id = t.Id, Name = t.Name, Crashed = false, Stacktrace = ToStacktrace(t.Frames) });
        return list;
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
