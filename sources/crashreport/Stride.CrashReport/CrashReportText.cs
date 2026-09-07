// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Stride.CrashReport;

/// <summary>
/// Renders everything a stored crash sends as one reviewable text, so "you can review the full report before
/// sending" holds: the report key/values (what <c>report.txt</c> carries), the exception chain with its frames,
/// the thread snapshot, and the event's identity (release, environment, grouping key), in the report's own
/// <c>Key: value</c> style. Attachments and feedback depend on choices made in the reporter, so the caller passes
/// those entries in (e.g. <c>AttachedFile: &lt;path&gt;</c>). Messages and file paths are scrubbed here exactly as the
/// sender scrubs them, so the text shows what leaves, not what was captured.
/// </summary>
public static class CrashReportText
{
    public static string Format(StoredCrash crash, IEnumerable<string> alsoSent = null)
    {
        var text = new StringBuilder();
        text.Append(crash.ToReportData().ToString()); // the report.txt attachment, verbatim

        if (crash.Exceptions.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("=== Call stack (sent as the event's stack trace) ===");
            for (var i = 0; i < crash.Exceptions.Count; i++)
            {
                var exception = crash.Exceptions[i];
                text.Append(i == 0 ? "" : "Caused by: ").Append(exception.Type).Append(": ")
                    .AppendLine(CrashReportAnonymizer.Scrub(exception.Message));
                AppendFrames(text, exception.Frames);
            }
        }

        if (crash.CrashedThreadId != null || crash.Threads.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("=== Threads (sent as the event's thread list) ===");
            if (crash.CrashedThreadId is int crashedId)
                text.Append("Thread ").Append(crashedId).Append(ThreadName(crash.CrashedThreadName))
                    .AppendLine(" (crashed; its stack is the call stack above)");
            foreach (var thread in crash.Threads)
            {
                text.Append("Thread ").Append(thread.Id).AppendLine(ThreadName(thread.Name));
                AppendFrames(text, thread.Frames);
            }
        }

        text.AppendLine();
        text.AppendLine("=== Also sent ===");
        var package = (crash.Application ?? "unknown").Replace(" ", "").ToLowerInvariant();
        var (version, commit) = CrashReportSender.SplitVersion(crash.Version);
        text.Append("Release: ").Append(package).Append('@').AppendLine(version);
        if (commit != null)
            text.Append("Commit: ").AppendLine(commit);
        text.Append("Environment: ").AppendLine(string.IsNullOrEmpty(crash.Environment) ? "local" : crash.Environment);
        text.Append("CrashGroupKey: ").AppendLine(crash.Signature ?? "(none)");
        text.Append("Occurrences: ").Append(crash.Count).AppendLine();
        if (crash.AffectedAssets.Count > 0)
            text.Append("AffectedAssets: ").AppendLine(string.Join(", ", crash.AffectedAssets));
        text.Append("CapturedUtc: ").AppendLine(crash.TimestampUtc);
        text.AppendLine("AttachedFile: report.txt (the report section above)");
        foreach (var line in alsoSent ?? Enumerable.Empty<string>())
            text.AppendLine(line);
        text.AppendLine("Mapping: the report keys above are also mapped, from the same data, to Sentry tags (application, commit, stride.version, graphics.api), contexts (GPU, memory, opened assets) and breadcrumbs (log, last actions).");
        text.AppendLine("SdkMetadata: the Sentry SDK adds a random installation id (no account or user identity), its version, and the operating system and .NET runtime versions.");
        text.AppendLine("Scrubbed: your user name and home-folder path are stripped from all of the above.");
        return text.ToString();
    }

    // Frames newest-first, as .NET reports them: "    at Function [Module] in File:line N".
    private static void AppendFrames(StringBuilder text, IReadOnlyList<StoredFrame> frames)
    {
        if (frames.Count == 0)
        {
            text.AppendLine("    (no frames)");
            return;
        }
        foreach (var frame in frames)
        {
            text.Append("    at ").Append(string.IsNullOrEmpty(frame.Function) ? "<unknown>" : frame.Function);
            if (!string.IsNullOrEmpty(frame.Module))
                text.Append(" [").Append(frame.Module).Append(']');
            if (!string.IsNullOrEmpty(frame.File))
            {
                text.Append(" in ").Append(CrashReportAnonymizer.Scrub(frame.File));
                if (frame.Line > 0)
                    text.Append(":line ").Append(frame.Line);
            }
            text.AppendLine();
        }
    }

    private static string ThreadName(string name) => string.IsNullOrEmpty(name) ? "" : $" \"{name}\"";
}
