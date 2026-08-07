// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
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
    /// window, never used silently. Empty until the Sentry project exists.
    /// </summary>
    public const string DevChannelDsn = "";

    /// <summary>DSN baked in at build time, if any.</summary>
    public static string BuildDsn { get; } = GetMetadata("SentryDsn");

    /// <summary>True when the build opted out of crash sending entirely (StrideSentryDsn=false).</summary>
    public static bool IsDisabled { get; } = GetMetadata("SentryDisabled") == "true";

    public static async Task SendAsync(CrashReportData report, string applicationName, Exception exception, string dsn)
    {
        var version = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";
        var package = applicationName.Replace(" ", "").ToLowerInvariant();

        using var sdk = SentrySdk.Init(options =>
        {
            options.Dsn = dsn;
            options.Release = $"{package}@{version}";
            options.Environment = GetMetadata("SentryEnvironment") ?? "local";
            options.IsGlobalModeEnabled = true;
            options.AutoSessionTracking = false;
            options.SetBeforeSend((sentryEvent, _) => Anonymize(sentryEvent));
        });

        SentrySdk.ConfigureScope(scope =>
        {
            scope.AddAttachment(Encoding.UTF8.GetBytes(report.ToString()), "report.txt");
            scope.SetTag("application", applicationName);
        });

        var sentryEvent = exception != null
            ? new SentryEvent(exception)
            : new SentryEvent { Message = new SentryMessage { Formatted = report["Exception"] } };
        sentryEvent.Level = SentryLevel.Fatal;

        SentrySdk.CaptureEvent(sentryEvent);
        await SentrySdk.FlushAsync(TimeSpan.FromSeconds(15));
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
