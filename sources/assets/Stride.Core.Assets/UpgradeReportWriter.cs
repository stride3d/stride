// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using System.Text;
using Stride.Core.IO;
using Stride.Core.Solutions;

namespace Stride.Core.Assets;

/// <summary>
/// Writes an <see cref="UpgradeReport"/> as Markdown: the <see cref="FileName"/> file of a solution, and the report of a whole upgrade.
/// </summary>
public static class UpgradeReportWriter
{
    /// <summary>
    /// The name of the file that lists what the upgrades left to do by hand: next to the solution that lists the projects, or
    /// next to a project no solution lists (see <see cref="FindOwner"/>).
    /// </summary>
    public const string FileName = "upgrade-report.md";

    // The number of frames of an exception the report shows: the log has the whole stack
    private const int StackFrameCount = 3;

    /// <summary>
    /// The file whose folder holds the <see cref="FileName"/> of the project: the solution nearest to the project that lists it
    /// (see <see cref="Solution.FindContaining"/>), else the project.
    /// </summary>
    /// <param name="projectPath">The project file.</param>
    public static string FindOwner(string projectPath)
    {
        ArgumentNullException.ThrowIfNull(projectPath);
        return Solution.FindContaining(projectPath) ?? Path.GetFullPath(projectPath);
    }

    /// <summary>
    /// The full path of the <see cref="FileName"/> next to <paramref name="ownerPath"/>.
    /// </summary>
    /// <param name="ownerPath">The solution or project, as <see cref="FindOwner"/> finds it.</param>
    public static string GetPath(string ownerPath)
    {
        ArgumentNullException.ThrowIfNull(ownerPath);
        return Path.Combine(DirectoryOf(ownerPath), FileName);
    }

    /// <summary>
    /// The text of the <see cref="FileName"/> of <paramref name="ownerPath"/>, with the upgrade of <paramref name="projects"/>
    /// on top of the upgrades the file already lists.
    /// </summary>
    /// <param name="existingText">The text of the file, or <c>null</c> when there is none.</param>
    /// <param name="ownerPath">The solution or project, as <see cref="FindOwner"/> finds it.</param>
    /// <param name="projects">The reports of the projects of the upgrade whose report is this file.</param>
    /// <param name="time">When the upgrade ran.</param>
    public static string AddUpgrade(string? existingText, string ownerPath, IReadOnlyCollection<ProjectUpgradeReport> projects, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentNullException.ThrowIfNull(projects);
        return AddSection(existingText, ownerPath, FormatSection(projects, time, DirectoryOf(ownerPath)));
    }

    /// <summary>
    /// The text of the <see cref="FileName"/> of <paramref name="ownerPath"/>, with the revert of the last upgrade on top (see
    /// <see cref="PackageSession.RevertUpgrades"/>).
    /// </summary>
    /// <param name="existingText">The text of the file, or <c>null</c> when there is none.</param>
    /// <param name="ownerPath">The solution or project, as <see cref="FindOwner"/> finds it.</param>
    /// <param name="projects">The names of the projects the revert put back files of.</param>
    /// <param name="revert">What the revert did to the backups that hold files of these projects.</param>
    /// <param name="versions">The records the revert changed, in the order of the projects and their records: the first is in the heading.</param>
    /// <param name="time">When the revert ran.</param>
    public static string AddRevert(string? existingText, string ownerPath, IReadOnlyList<string> projects, UpgradeRevert revert, IReadOnlyList<RevertedVersion> versions, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(revert);
        ArgumentNullException.ThrowIfNull(versions);
        var directory = DirectoryOf(ownerPath);
        var backups = string.Join(", ", revert.BackupFolders.Select(x => Code(Relative(directory, x))));
        var kept = string.Join(", ", revert.KeptFolders.Select(x => Code(Relative(directory, x))));
        var versionTexts = versions.Select(FormatVersions).Distinct().ToList();
        var what = versionTexts.Count > 0 ? versionTexts[0] : "Last upgrade";
        var itemCount = revert.NotPutBack.Count + revert.ChangedFiles.Count;
        var outcome = revert.NotPutBack.Count > 0 ? "partly reverted" : "reverted";
        // The report of a solution names the projects; the report of a project is about that project
        var where = Solution.IsSolutionFile(ownerPath) && projects.Count > 0 ? $" in {string.Join(", ", projects.Select(Code))}" : "";
        var builder = new StringBuilder();
        builder.Append($"## {FormatTime(time)} · {what} · {outcome}{(itemCount > 0 ? $", {Plural(itemCount, "item")} left" : "")}\n\n");
        builder.Append(revert.NotPutBack.Count == 0
            ? $"Every file the last upgrade and its retries changed{where} is put back as it was, from {backups}. Its items below are no longer to do: the next upgrade runs its steps again."
            : $"Some files the last upgrade and its retries changed{where} could not be put back as they were: copy them from {backups}, then open the {(projects.Count > 1 ? "projects" : "project")} again.");
        if (versionTexts.Count > 1)
            builder.Append(" Also reverted: ").Append(string.Join(", ", versionTexts.Skip(1))).Append('.');
        if (revert.KeptFolders.Count > 0)
            builder.Append(" The files as they were before the revert are in ").Append(kept).Append('.');
        builder.Append('\n');

        if (revert.NotPutBack.Count > 0)
        {
            builder.Append("\n### Not put back\n\n");
            foreach (var path in revert.NotPutBack)
            {
                // A backup folder that could not be read puts back none of its files
                var item = revert.BackupFolders.Contains(path, StringComparer.OrdinalIgnoreCase) ? $"The files of {Code(Relative(directory, path))}" : Code(Relative(directory, path));
                builder.Append("- [ ] ").Append(item).Append('\n');
            }
        }

        if (revert.ChangedFiles.Count > 0)
        {
            builder.Append("\n### Changed since the upgrade\n\n");
            builder.Append($"Your changes to these files are only in {kept}: copy back what you want to keep.\n\n");
            foreach (var path in revert.ChangedFiles)
                builder.Append("- [ ] ").Append(Code(Relative(directory, path))).Append('\n');
        }
        return AddSection(existingText, ownerPath, builder.ToString());
    }

    /// <summary>
    /// The text of the <see cref="FileName"/> of <paramref name="ownerPath"/>, with the last upgrade of these projects marked as
    /// finished on top (see <see cref="PackageSession.MarkUpgradesFinished"/>).
    /// </summary>
    /// <param name="existingText">The text of the file, or <c>null</c> when there is none.</param>
    /// <param name="ownerPath">The solution or project, as <see cref="FindOwner"/> finds it.</param>
    /// <param name="projects">
    /// The name of each project, with the steps its upgrade left, done by hand, or the titles of the parts of it that are not steps.
    /// </param>
    /// <param name="time">When the upgrade was marked as finished.</param>
    public static string AddMarkedFinished(string? existingText, string ownerPath, IReadOnlyCollection<(string Project, IReadOnlyList<string> Steps)> projects, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(ownerPath);
        ArgumentNullException.ThrowIfNull(projects);
        var builder = new StringBuilder();
        builder.Append($"## {FormatTime(time)} · Last upgrade marked as finished\n\nIts items below are done by hand:\n\n");
        foreach (var (project, steps) in projects)
            builder.Append("- ").Append(project).Append(": ").Append(string.Join(", ", steps.Select(x => UpgradeStepInfo.IsStepId(x) ? Code(x) : x))).Append('\n');
        return AddSection(existingText, ownerPath, builder.ToString());
    }

    private static string AddSection(string? existingText, string ownerPath, string section)
    {
        if (string.IsNullOrEmpty(existingText))
        {
            var intro = $"Upgrades of this {(Solution.IsSolutionFile(ownerPath) ? "solution" : "project")} left the items below to do by hand. The newest upgrade is first. Once every item is handled, remove this file and the backup of the upgrades with **Remove the report and backup** in Game Studio: deleting only this file keeps the backup, which a revert still puts back.";
            return $"# Upgrade report: {Path.GetFileName(ownerPath)}\n\n{intro}\n\n{section}";
        }

        // The newest section goes before the first one the file lists; a file without one, edited by hand, gets it at its end
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
    /// <param name="inputPath">The solution or project that was upgraded: the paths of the report are relative to its folder.</param>
    /// <param name="time">When the upgrade ran.</param>
    public static string FormatUpgrade(UpgradeReport report, string inputPath, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(report);
        return FormatUpgrade(report.Projects, inputPath, time);
    }

    /// <summary>
    /// The report of these projects of an upgrade, as Markdown.
    /// </summary>
    /// <param name="projects">The reports of the projects.</param>
    /// <param name="inputPath">The solution or project that was upgraded: the paths of the report are relative to its folder.</param>
    /// <param name="time">When the upgrade ran.</param>
    public static string FormatUpgrade(IReadOnlyCollection<ProjectUpgradeReport> projects, string inputPath, DateTime time)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(inputPath);
        var title = $"# Upgrade report: {Path.GetFileName(inputPath)}\n\n";
        return projects.Count == 0
            ? $"{title}The upgrade of {FormatTime(time)}: no project needed an upgrade.\n"
            : title + FormatSection(projects, time, DirectoryOf(inputPath));
    }

    /// <summary>
    /// What the upgrade of the project did, in one line, e.g. <c>Stride 4.4.3 to 4.5.0: all 14 steps done</c>.
    /// </summary>
    public static string FormatSummary(ProjectUpgradeReport project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return $"{FormatVersions(project, " to ")}: {FormatOutcome([project])}";
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

    /// <summary>
    /// The steps of the upgrade of the project that aren't done, as a Markdown list with their status, each ID linked to its
    /// section of the upgrade guide.
    /// </summary>
    public static string FormatStepsLeftList(ProjectUpgradeReport project)
    {
        ArgumentNullException.ThrowIfNull(project);
        return string.Join("\n", project.Upgraders.SelectMany(x => x.Steps).Where(x => x.Status != UpgradeStepStatus.Done).Select(step =>
        {
            var line = $"- {StatusName(step.Status)} {(step.Id is null ? "" : FormatIdLink(step.Id, step.HelpLink) + " ")}{step.Title}";
            // A part of the upgrade that is not a step says why it failed: the projects it references, for example
            if (step.Id is null && step.Error is not null)
                return $"{line}: {step.Error.ReplaceLineEndings(" ")}";
            return step.Status == UpgradeStepStatus.Partial ? $"{line} ({string.Join(", ", LeftParts(step))})" : line;
        }));
    }

    /// <summary>
    /// The steps as a Markdown list, each ID linked to its section of the upgrade guide.
    /// </summary>
    public static string FormatStepList(IEnumerable<UpgradeStepInfo> steps)
    {
        ArgumentNullException.ThrowIfNull(steps);
        return string.Join("\n", steps.Select(step => step.Id.Length == 0 ? $"- {step.Title}" : $"- {FormatIdLink(step.Id, step.HelpLink)} {step.Title}".TrimEnd()));
    }

    // The section of an upgrade: what is of the whole upgrade (versions, outcome, backup), then a subsection per project, which
    // names its versions when they aren't the ones of the heading
    private static string FormatSection(IReadOnlyCollection<ProjectUpgradeReport> projects, DateTime time, string directory)
    {
        var versions = FormatVersions(projects.First(), " → ");
        var builder = new StringBuilder();
        builder.Append($"## {FormatTime(time)} · {versions} · {FormatOutcome(projects)}\n");

        var paragraph = new List<string>();
        if (FormatEnd(projects) is { } end)
            paragraph.Add(end);
        // The report of a reverted upgrade is in its backup folder
        if (Outcome(projects) != UpgradeOutcome.Reverted && projects.Select(x => x.BackupFolder).FirstOrDefault(x => x is not null) is { } backup)
            paragraph.Add($"The files it changed are backed up in {Code(Relative(directory, backup))}.");
        if (paragraph.Count > 0)
            builder.Append('\n').Append(string.Join(' ', paragraph)).Append('\n');

        foreach (var project in projects)
        {
            var projectVersions = FormatVersions(project, " → ");
            builder.Append($"\n### {project.ProjectFullPath.GetFileNameWithoutExtension()}{(projectVersions != versions ? $" · {projectVersions}" : "")} · {FormatOutcome([project], withEnd: false)}\n\n");
            AppendProject(builder, project, directory);
        }
        return builder.ToString();
    }

    // The steps of the upgrade of a project: their counts, the items left grouped by status, then the steps done
    private static void AppendProject(StringBuilder builder, ProjectUpgradeReport project, string directory)
    {
        var steps = project.Upgraders.SelectMany(x => x.Steps).ToList();
        var counts = string.Join(", ", new[] { UpgradeStepStatus.Done, UpgradeStepStatus.Failed, UpgradeStepStatus.Partial, UpgradeStepStatus.NotRun }
            .Select(status => (Status: status, Count: steps.Count(x => x.Status == status)))
            .Where(x => x.Count > 0)
            .Select(x => $"{x.Count} {StatusName(x.Status)}"));
        builder.Append(steps.Count == 0 ? "No step." : $"{Plural(steps.Count, "step")}: {counts}.");
        if (project.Upgraders.Count > 1)
            builder.Append(" Also upgraded: ").Append(string.Join(", ", project.Upgraders.Skip(1).Select(x => FormatVersions(x, " → ")))).Append('.');
        builder.Append('\n');

        // The leftovers of a step that may change behaviour silently come first: the build doesn't point to them
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

        void AppendGroup(string heading, List<UpgradeStepResult> group)
        {
            if (group.Count == 0)
                return;

            builder.Append($"\n#### {heading}\n\n");
            foreach (var step in group)
            {
                builder.Append(project.Outcome == UpgradeOutcome.Reverted ? "- " : "- [ ] ").Append(FormatStep(step));
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

    // What the upgrade did to the files after a step of a whole project failed
    private static string? FormatEnd(IReadOnlyCollection<ProjectUpgradeReport> projects)
    {
        var outcome = Outcome(projects);
        var upgrader = projects.First(x => x.Outcome == outcome).Upgraders.FirstOrDefault();
        var them = projects.Count > 1 ? "the projects" : "the project";
        return outcome switch
        {
            UpgradeOutcome.Reverted => $"A step of a project of the solution failed, so every file the upgrade changed, in every project, was put back as it was: {them} {(projects.Count > 1 ? "stay" : "stays")} on {upgrader?.Name} {upgrader?.FromVersion}. Nothing below is to do by hand: fix why the upgrade failed, and its next upgrade runs these steps again.",
            UpgradeOutcome.Kept => $"A step failed, and the files were kept as the upgrade left them: finish the items below by hand, then mark the upgrade as finished. No upgrade runs on {them} before that.",
            UpgradeOutcome.PartlyReverted => $"A step failed, and some files the upgrade changed could not be put back as they were: copy them from the backup, then mark the upgrade as finished. No upgrade runs on {them} before that.",
            _ => null,
        };
    }

    // How the upgrade of the projects ended: a failure ends the upgrade of every project of the solution as one
    private static UpgradeOutcome Outcome(IEnumerable<ProjectUpgradeReport> projects)
        => projects.Select(x => x.Outcome).FirstOrDefault(x => x != UpgradeOutcome.Completed);

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

    // The ID of a step, as a link to its section of the upgrade guide when it has one
    private static string FormatIdLink(string id, string? helpLink) => helpLink is null ? Code(id) : $"[{id}]({helpLink})";

    // The outcome of the upgrade, then the items left or the steps done; withEnd: false leaves out how a failed upgrade ended,
    // which the heading of a project shares with the heading of its upgrade
    private static string FormatOutcome(IReadOnlyCollection<ProjectUpgradeReport> projects, bool withEnd = true)
    {
        var steps = projects.SelectMany(x => x.Upgraders).SelectMany(x => x.Steps).ToList();
        // A reverted upgrade leaves nothing to do by hand: its next upgrade runs the steps again
        if (Outcome(projects) == UpgradeOutcome.Reverted)
            return "reverted";

        var left = steps.Count(x => x.IsLeftover);
        var outcome = !withEnd ? "" : Outcome(projects) switch
        {
            UpgradeOutcome.Kept => "kept, ",
            UpgradeOutcome.PartlyReverted => "partly reverted, ",
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
        => upgrader.IsRetry ? $"{upgrader.Name} {upgrader.ToVersion}, steps left run again"
            : upgrader.ToVersion is null ? $"{upgrader.Name} from {upgrader.FromVersion}"
            : $"{upgrader.Name} {upgrader.FromVersion}{arrow}{upgrader.ToVersion}";

    private static string FormatVersions(RevertedVersion version)
        => version.To is null ? $"{version.Upgrader} {version.From}" : $"{version.Upgrader} {version.From} → {version.To}";

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

    // The folder of a solution or project file
    private static string DirectoryOf(string path) => Path.GetDirectoryName(Path.GetFullPath(path))!;

    // A path relative to the folder of the report, with forward slashes
    private static string Relative(string directory, string path) => Path.GetRelativePath(directory, path).Replace('\\', '/');

    // Inline code, also for a text with a backtick
    private static string Code(string text) => text.Contains('`') ? $"`` {text} ``" : $"`{text}`";

    private static string Plural(int count, string noun) => count == 1 ? $"1 {noun}" : $"{count} {noun}s";
}
