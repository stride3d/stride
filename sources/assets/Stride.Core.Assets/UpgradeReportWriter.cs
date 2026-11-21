// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Text;

namespace Stride.Core.Assets;

/// <summary>
/// Writes an <see cref="UpgradeReport"/> as Markdown: the <see cref="FileName"/> file of a project, and the report of a whole upgrade.
/// </summary>
public static class UpgradeReportWriter
{
    /// <summary>The name of the file, next to the project file, that lists what the upgrades of the project left to do by hand.</summary>
    public const string FileName = "upgrade-report.md";

    private const string ProjectFileIntro = "Upgrades of this project left the items below to do by hand. Delete this file once every item is handled. The newest upgrade is first.";

    // The number of frames of an exception the report shows: the log has the whole stack
    private const int StackFrameCount = 3;

    /// <summary>
    /// The text of the <see cref="FileName"/> file of the project, with the upgrade of <paramref name="project"/> on top of
    /// the upgrades the file already lists.
    /// </summary>
    /// <param name="existingText">The text of the file, or <c>null</c> when there is none.</param>
    /// <param name="project">The report of the project.</param>
    /// <param name="time">When the upgrade ran.</param>
    public static string AddToProjectFile(string? existingText, ProjectUpgradeReport project, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(project);
        var section = FormatSection(project, time, withProjectName: false);
        if (string.IsNullOrEmpty(existingText))
            return $"# Upgrade report: {project.ProjectFullPath.GetFileName()}\n\n{ProjectFileIntro}\n\n{section}";

        // The newest upgrade goes before the first one the file lists; a file without one, edited by hand, gets it at its end
        var text = existingText.ReplaceLineEndings("\n");
        var firstSection = text.StartsWith("## ", StringComparison.Ordinal) ? 0 : text.IndexOf("\n## ", StringComparison.Ordinal) is var index and >= 0 ? index + 1 : -1;
        return firstSection >= 0
            ? text[..firstSection] + section + "\n" + text[firstSection..]
            : text.TrimEnd('\n') + "\n\n" + section;
    }

    /// <summary>
    /// The report of every project of an upgrade, as Markdown.
    /// </summary>
    /// <param name="report">The report of the session.</param>
    /// <param name="inputName">The name of the solution or project that was upgraded.</param>
    /// <param name="time">When the upgrade ran.</param>
    public static string FormatUpgrade(UpgradeReport report, string inputName, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(report);
        var builder = new StringBuilder();
        builder.Append($"# Upgrade report: {inputName}\n\n");
        builder.Append($"The upgrade of {FormatTime(time)}.");
        if (report.Projects.Count == 0)
            return builder.Append(" No project needed an upgrade.\n").ToString();

        builder.Append('\n');
        foreach (var project in report.Projects)
            builder.Append('\n').Append(FormatSection(project, time, withProjectName: true));
        return builder.ToString();
    }

    /// <summary>
    /// What the upgrade of the project did, in one line, e.g. <c>Stride 4.4.3 to 4.5.0: all 14 steps done</c>.
    /// </summary>
    public static string FormatSummary(ProjectUpgradeReport project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return $"{FormatVersions(project, " to ")}: {FormatOutcome(project)}";
    }

    /// <summary>
    /// The steps of the upgrade of the project, one line each, with their status.
    /// </summary>
    public static IEnumerable<string> FormatStepLines(ProjectUpgradeReport project)
    {
        ArgumentNullException.ThrowIfNull(project);
        foreach (var step in project.Upgraders.SelectMany(x => x.Steps))
        {
            var line = $"  {StatusName(step.Status),-8} {(step.Id is null ? "" : step.Id + " ")}{step.Title}";
            if (step.Status == UpgradeStepStatus.Partial)
                line += $" ({string.Join(", ", LeftParts(step))})";
            yield return line;
        }
    }

    private static string FormatSection(ProjectUpgradeReport project, DateTime time, bool withProjectName)
    {
        var steps = project.Upgraders.SelectMany(x => x.Steps).ToList();
        var builder = new StringBuilder();
        builder.Append(withProjectName
            ? $"## {project.ProjectFullPath.GetFileName()} · {FormatVersions(project, " → ")} · {FormatOutcome(project)}\n\n"
            : $"## {FormatTime(time)} · {FormatVersions(project, " → ")} · {FormatOutcome(project)}\n\n");

        var counts = string.Join(", ", new[] { UpgradeStepStatus.Done, UpgradeStepStatus.Failed, UpgradeStepStatus.Partial, UpgradeStepStatus.NotRun }
            .Select(status => (Status: status, Count: steps.Count(x => x.Status == status)))
            .Where(x => x.Count > 0)
            .Select(x => $"{x.Count} {StatusName(x.Status)}"));
        builder.Append(steps.Count == 0 ? "No step." : $"{Plural(steps.Count, "step")}: {counts}.");
        if (project.Upgraders.Count > 1)
            builder.Append(" Also upgraded: ").Append(string.Join(", ", project.Upgraders.Skip(1).Select(x => FormatVersions(x, " → ")))).Append('.');
        if (FormatEnd(project) is { } end)
            builder.Append(' ').Append(end);
        if (project.ContinuedFromUnfinished.Count > 0)
            builder.Append(" It continued an upgrade marked as finished, which had left ").Append(string.Join(", ", project.ContinuedFromUnfinished.Select(Code))).Append(" to finish by hand.");
        builder.Append('\n');

        // The leftovers of a step that may change behaviour silently come first: the build doesn't point to them
        var directory = project.ProjectFullPath.GetFullDirectory().ToOSPath();
        var silent = steps.Where(x => x.IsLeftover && x.MayChangeBehaviour).ToList();
        AppendGroup("May change behaviour silently", silent);
        foreach (var (status, heading) in new[] { (UpgradeStepStatus.Failed, "Failed"), (UpgradeStepStatus.Partial, "Partial"), (UpgradeStepStatus.NotRun, "Not run") })
            AppendGroup(heading, steps.Where(x => x.Status == status && !silent.Contains(x)).ToList());

        var done = steps.Where(x => x.Status == UpgradeStepStatus.Done).ToList();
        if (done.Count > 0)
        {
            builder.Append($"\n<details>\n<summary>Done ({done.Count})</summary>\n\n");
            foreach (var step in done)
            {
                builder.Append("- ").Append(FormatStep(step)).Append(FormatChangeCounts(step)).Append('\n');
                if (step.ChangedFiles.Count == 0)
                    continue;

                builder.Append("  <details>\n  <summary>Files</summary>\n\n");
                foreach (var file in step.ChangedFiles)
                    builder.Append("  - ").Append(Code(Relative(directory, file))).Append('\n');
                builder.Append("\n  </details>\n");
            }
            builder.Append("\n</details>\n");
        }
        return builder.ToString();

        void AppendGroup(string heading, List<UpgradeStepResult> group)
        {
            if (group.Count == 0)
                return;

            builder.Append($"\n### {heading}\n\n");
            foreach (var step in group)
            {
                builder.Append("- [ ] ").Append(FormatStep(step));
                if (heading != StatusHeading(step.Status))
                    builder.Append(" (").Append(StatusName(step.Status)).Append(')');
                builder.Append('\n');
                if (step.Error is not null)
                {
                    builder.Append("  - ").Append(step.Error.ReplaceLineEndings(" ")).Append('\n');
                    AppendFrames(builder, step.Exception);
                }
                foreach (var unit in step.FailedUnits)
                {
                    builder.Append("  - ").Append(Code(Relative(directory, unit.Path))).Append(" is left unchanged");
                    if ((unit.Error ?? unit.Exception?.Message) is { } error)
                        builder.Append(": ").Append(error.ReplaceLineEndings(" "));
                    builder.Append('\n');
                    AppendFrames(builder, unit.Exception);
                }
                foreach (var change in step.MissedChanges)
                    builder.Append("  - ").Append(Code($"{Relative(directory, change.Path)}({change.Line},{change.Column})")).Append(": ").Append(Code(change.Change)).Append(", not applied\n");
            }
        }
    }

    // The first frames of the stack of an exception, as a code block under the item of its message
    private static void AppendFrames(StringBuilder builder, Exception? exception)
    {
        var frames = exception?.StackTrace?.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(StackFrameCount).ToList();
        if (frames is null || frames.Count == 0)
            return;

        builder.Append("    ```\n");
        foreach (var frame in frames)
            builder.Append("    ").Append(frame).Append('\n');
        builder.Append("    ```\n");
    }

    // What the upgrade did to the files after a step of the whole project failed
    private static string? FormatEnd(ProjectUpgradeReport project)
    {
        var upgrader = project.Upgraders.FirstOrDefault();
        return project.Outcome switch
        {
            UpgradeOutcome.Reverted => $"A step failed, so every file of the project was put back as it was before this upgrade: the project stays on {upgrader?.Name} {upgrader?.FromVersion}, and its next upgrade runs every step again.",
            UpgradeOutcome.Kept => "A step failed, and the files were kept as the upgrade left them: finish the items below by hand, then mark the upgrade as finished. No upgrade runs on the project before that.",
            UpgradeOutcome.Stopped => $"A step failed, and the files stay as the upgrade left them, with no backup to put them back from: the project stays recorded on {upgrader?.Name} {upgrader?.FromVersion}.",
            _ => null,
        };
    }

    // What a step that is done changed: " (5 edits in 3 files)"
    private static string FormatChangeCounts(UpgradeStepResult step)
    {
        if (step.ChangedFiles.Count == 0)
            return "";
        return step.Edits > 0
            ? $" ({Plural(step.Edits, "edit")} in {Plural(step.ChangedFiles.Count, "file")})"
            : $" ({Plural(step.ChangedFiles.Count, "file")})";
    }

    // What a partial step left: "1 file left unchanged, 2 changes not made"
    private static IEnumerable<string> LeftParts(UpgradeStepResult step)
    {
        if (step.FailedUnits.Count > 0)
            yield return $"{Plural(step.FailedUnits.Count, "file")} left unchanged";
        if (step.MissedChanges.Count > 0)
            yield return $"{Plural(step.MissedChanges.Count, "change")} not made";
    }

    // The ID of the step, its title and the link to its section of the upgrade guide
    private static string FormatStep(UpgradeStepResult step)
    {
        var text = step.Id is null ? step.Title : $"`{step.Id}` {step.Title}";
        return step.HelpLink is null ? text : $"{text} ([guide]({step.HelpLink}))";
    }

    private static string FormatOutcome(ProjectUpgradeReport project)
    {
        var steps = project.Upgraders.SelectMany(x => x.Steps).ToList();
        var left = steps.Count(x => x.IsLeftover);
        var outcome = project.Outcome switch
        {
            UpgradeOutcome.Reverted => "reverted, ",
            UpgradeOutcome.Kept => "kept, ",
            UpgradeOutcome.Stopped => "stopped, ",
            _ => "",
        };
        if (left > 0)
            return $"{outcome}{Plural(left, "item")} left";
        return steps.Count switch
        {
            0 => "done",
            1 => "1 step done",
            _ => $"all {steps.Count} steps done",
        };
    }

    // The versions of the first upgrader, the one the project is named after (Stride before its plugins)
    private static string FormatVersions(ProjectUpgradeReport project, string arrow)
        => project.Upgraders.Count == 0 ? "No upgrader" : FormatVersions(project.Upgraders[0], arrow);

    private static string FormatVersions(UpgraderReport upgrader, string arrow)
        => upgrader.ToVersion is null ? $"{upgrader.Name} from {upgrader.FromVersion}" : $"{upgrader.Name} {upgrader.FromVersion}{arrow}{upgrader.ToVersion}";

    private static string FormatTime(DateTime time) => time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string StatusName(UpgradeStepStatus status) => status switch
    {
        UpgradeStepStatus.Done => "done",
        UpgradeStepStatus.Partial => "partial",
        UpgradeStepStatus.Failed => "failed",
        _ => "not run",
    };

    private static string StatusHeading(UpgradeStepStatus status) => status switch
    {
        UpgradeStepStatus.Done => "Done",
        UpgradeStepStatus.Partial => "Partial",
        UpgradeStepStatus.Failed => "Failed",
        _ => "Not run",
    };

    // A path relative to the folder of the project, with forward slashes
    private static string Relative(string directory, string path) => Path.GetRelativePath(directory, path).Replace('\\', '/');

    // Inline code, also for a text with a backtick
    private static string Code(string text) => text.Contains('`') ? $"`` {text} ``" : $"`{text}`";

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
