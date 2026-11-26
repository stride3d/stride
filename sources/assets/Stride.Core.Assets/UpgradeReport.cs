// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// The status of an upgrade step in an <see cref="UpgradeReport"/>.
/// </summary>
public enum UpgradeStepStatus
{
    /// <summary>The step did not run, because the upgrade stopped before it.</summary>
    NotRun,

    /// <summary>The step ran on every file it changes.</summary>
    Done,

    /// <summary>
    /// The step ran, but left some files unchanged (see <see cref="UpgradeStepResult.FailedUnits"/>), or did not make some of
    /// the changes it found (see <see cref="UpgradeStepResult.MissedChanges"/>).
    /// </summary>
    Partial,

    /// <summary>The step failed, and changed nothing.</summary>
    Failed,
}

/// <summary>
/// How the upgrade of a project ended.
/// </summary>
public enum UpgradeOutcome
{
    /// <summary>Every step that applies ran, done or partial.</summary>
    Completed,

    /// <summary>A step of a whole project of the session failed, and every file the upgrade changed, in every project, was put back.</summary>
    Reverted,

    /// <summary>
    /// A step of the whole project failed, and the files stay as the upgrade left them, as asked or for want of a backup: the
    /// project records the upgrade with the steps to finish by hand (see <see cref="PackageUpgrader.UnfinishedMetadata"/>).
    /// </summary>
    Kept,

    /// <summary>
    /// A step of a whole project of the session failed, and some of the files the upgrade changed could not be put back: each
    /// project records its version before the upgrade as unfinished, until the user copies them from the backup.
    /// </summary>
    PartlyReverted,
}

/// <summary>
/// A step an upgrader declares, as <see cref="PackageUpgrader.GetUpgradeSteps"/> lists it.
/// </summary>
/// <param name="Id">The ID of the step, e.g. <c>Stride-4.5.0-P2</c>.</param>
/// <param name="Title">What the step changes, in one line of Markdown.</param>
/// <param name="HelpLink">The page of the upgrade guide about the step, or <c>null</c>.</param>
/// <param name="MayChangeBehaviour">
/// Whether code the step leaves unchanged may still compile and behave differently, so that the build doesn't show what is
/// left to do.
/// </param>
public sealed record UpgradeStepInfo(string Id, string Title, string? HelpLink, bool MayChangeBehaviour = false)
{
    /// <summary>
    /// Whether a retry of the unfinished upgrade that left the step runs it again (see <see cref="PackageUpgrader.CanRetryStep"/>).
    /// </summary>
    public bool CanRetry { get; init; }

    /// <summary>
    /// Whether an entry of the steps an upgrade left (see <see cref="PackageUpgrader.UnfinishedMetadata"/>) is the ID of a step,
    /// or the title of a part of the upgrade that is not a step, such as "Put the files back as they were".
    /// </summary>
    public static bool IsStepId(string entry) => !entry.Contains(' ', StringComparison.Ordinal);
}

/// <summary>
/// An upgrade a project records as kept unfinished (see <see cref="PackageUpgrader.UnfinishedMetadata"/>).
/// </summary>
/// <param name="Upgrader">The <see cref="PackageUpgraderAttribute.Name"/> of the upgrader.</param>
/// <param name="Version">The version the record holds.</param>
/// <param name="Steps">The IDs of the steps left to finish by hand, or the titles of parts of the upgrade that are not steps.</param>
/// <param name="Backup">
/// The full path of the backup folder of the upgrade (see <see cref="PackageUpgrader.BackupMetadata"/>), or <c>null</c>.
/// </param>
public sealed record UnfinishedUpgrade(string Upgrader, string Version, IReadOnlyList<string> Steps, string? Backup = null);

/// <summary>
/// What putting back the files of the last upgrade did (see <see cref="PackageSession.RevertUpgrades"/>).
/// </summary>
/// <param name="BackupFolders">The backup folders the files were put back from.</param>
/// <param name="NotPutBack">The files that could not be put back, or the backup folders that could not be read.</param>
/// <param name="KeptFolders">The folders of the backups that keep the files as they were before the revert.</param>
/// <param name="ChangedFiles">
/// The files put back that changed since the upgrade (see <see cref="UpgradeBackup.GetChangedFiles"/>): these changes are now
/// only in <paramref name="KeptFolders"/>.
/// </param>
public sealed record UpgradeRevert(IReadOnlyList<string> BackupFolders, IReadOnlyList<string> NotPutBack, IReadOnlyList<string> KeptFolders, IReadOnlyList<string> ChangedFiles);

/// <summary>
/// The version an upgrader recorded in a project before and after a revert of its upgrade (see <see cref="PackageSession.RevertUpgrades"/>).
/// </summary>
/// <param name="Upgrader">The <see cref="PackageUpgraderAttribute.Name"/> of the upgrader.</param>
/// <param name="From">The version the upgrade went to.</param>
/// <param name="To">The version the revert put back, or <c>null</c> when the project had no record before the upgrade.</param>
public sealed record RevertedVersion(string Upgrader, string From, string? To);

/// <summary>
/// What running again the steps of the unfinished upgrades of a project did (see <see cref="PackageSession.RetryUnfinishedUpgrades"/>).
/// </summary>
/// <param name="StepsLeft">
/// The steps still left, as <see cref="PackageSession.DescribeUnfinishedSteps"/> describes them: a part of the upgrade that is
/// not a step has only its title.
/// </param>
/// <param name="ChangedProject">Whether a step changed a file: the project is restored and loaded again to take it.</param>
public sealed record UnfinishedUpgradeRetry(IReadOnlyList<UpgradeStepInfo> StepsLeft, bool ChangedProject);

/// <summary>
/// A file an upgrade step could not change, and why.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Error">Why the step could not change it, or <c>null</c>.</param>
/// <param name="Exception">The exception that stopped the change, or <c>null</c>.</param>
public sealed record UpgradeUnitFailure(string Path, string? Error = null, Exception? Exception = null);

/// <summary>
/// A change an upgrade step found in the code, but did not make.
/// </summary>
/// <param name="Path">The full path of the file.</param>
/// <param name="Line">The line of the change in the code the upgrade wrote, from 1.</param>
/// <param name="Column">The column of the change, from 1.</param>
/// <param name="Change">The change, e.g. <c>fmt.IsSRgb() → fmt.IsSRgb</c>.</param>
public sealed record UpgradeMissedChange(string Path, int Line, int Column, string Change);

/// <summary>
/// What an upgrade step did to a project.
/// </summary>
public sealed class UpgradeStepResult
{
    internal UpgradeStepResult(string? id, string title, string? helpLink, bool mayChangeBehaviour = false)
    {
        Id = id;
        Title = title;
        HelpLink = helpLink;
        MayChangeBehaviour = mayChangeBehaviour;
    }

    /// <summary>The ID of the step, or <c>null</c> for a part of the upgrade that is not a declared step (a hook of the upgrader).</summary>
    public string? Id { get; }

    /// <summary>What the step changes, in one line of Markdown.</summary>
    public string Title { get; }

    /// <summary>The page of the upgrade guide about the step, or <c>null</c>.</summary>
    public string? HelpLink { get; }

    /// <inheritdoc cref="UpgradeStepInfo.MayChangeBehaviour"/>
    public bool MayChangeBehaviour { get; }

    /// <summary>The status of the step.</summary>
    public UpgradeStepStatus Status { get; set; }

    /// <summary>The full paths of the files the step changed.</summary>
    public List<string> ChangedFiles { get; } = [];

    /// <summary>The number of edits the step made in the code, or 0 for a step that doesn't count them.</summary>
    public int Edits { get; set; }

    /// <summary>The files the step could not change, which it left as they were.</summary>
    public List<UpgradeUnitFailure> FailedUnits { get; } = [];

    /// <summary>The changes the step found in the code, but did not make.</summary>
    public List<UpgradeMissedChange> MissedChanges { get; } = [];

    /// <summary>Why the step failed, or <c>null</c>.</summary>
    public string? Error { get; set; }

    /// <summary>The exception that failed the step, or <c>null</c>.</summary>
    public Exception? Exception { get; set; }

    /// <summary>Whether the step left something to do by hand.</summary>
    public bool IsLeftover => Status != UpgradeStepStatus.Done;

    /// <summary>Sets <see cref="Status"/> once the step ran: partial when it left files or changes, else done.</summary>
    public void SetRan() => Status = FailedUnits.Count > 0 || MissedChanges.Count > 0 ? UpgradeStepStatus.Partial : UpgradeStepStatus.Done;

    /// <summary>Sets the step as failed, with the message of <paramref name="exception"/>.</summary>
    public void SetFailed(Exception exception)
    {
        Status = UpgradeStepStatus.Failed;
        Error = exception.Message;
        Exception = exception;
    }
}

/// <summary>
/// What the steps of one upgrader did to a project.
/// </summary>
public sealed class UpgraderReport
{
    /// <summary>A report with the <paramref name="steps"/> of the upgrader, not run yet.</summary>
    public UpgraderReport(string name, PackageVersion fromVersion, PackageVersion? toVersion, IEnumerable<UpgradeStepInfo> steps)
    {
        Name = name;
        FromVersion = fromVersion;
        ToVersion = toVersion;
        Steps.AddRange(steps.Select(x => new UpgradeStepResult(x.Id, x.Title, x.HelpLink, x.MayChangeBehaviour)));
    }

    /// <summary>The <see cref="PackageUpgraderAttribute.Name"/> of the upgrader.</summary>
    public string Name { get; }

    /// <summary>The version the project is upgraded from.</summary>
    public PackageVersion FromVersion { get; }

    /// <summary>The version the project is upgraded to.</summary>
    public PackageVersion? ToVersion { get; }

    /// <summary>
    /// Whether the report is of the steps an upgrade kept unfinished, run again (see <see cref="PackageUpgrader.RetrySteps"/>):
    /// the project stays on <see cref="ToVersion"/>.
    /// </summary>
    public bool IsRetry { get; init; }

    /// <summary>The steps that apply to the project, in the order they run, then the parts of the upgrade that failed outside a step.</summary>
    public List<UpgradeStepResult> Steps { get; } = [];

    /// <summary>The step with the ID <paramref name="id"/>, or <c>null</c>.</summary>
    public UpgradeStepResult? Find(string id) => Steps.FirstOrDefault(x => x.Id == id);

    /// <summary>
    /// Adds a part of the upgrade that failed outside a declared step, such as the update of the package references.
    /// </summary>
    public UpgradeStepResult AddFailure(string title, string? error = null, Exception? exception = null)
    {
        var result = new UpgradeStepResult(null, title, null) { Status = UpgradeStepStatus.Failed, Error = error ?? exception?.Message, Exception = exception };
        Steps.Add(result);
        return result;
    }
}

/// <summary>
/// What the upgrade did to one project.
/// </summary>
public sealed class ProjectUpgradeReport
{
    internal ProjectUpgradeReport(UFile projectFullPath)
    {
        ProjectFullPath = projectFullPath;
    }

    /// <summary>The full path of the project file.</summary>
    public UFile ProjectFullPath { get; }

    /// <summary>The upgraders that upgraded the project, in the order they ran.</summary>
    public List<UpgraderReport> Upgraders { get; } = [];

    /// <summary>Whether the upgrade left something to do by hand: a step that failed, was partial or did not run.</summary>
    public bool HasLeftovers => Upgraders.SelectMany(x => x.Steps).Any(x => x.IsLeftover);

    /// <summary>Whether a step of the whole project failed: the steps after it did not run.</summary>
    public bool HasFailed => Upgraders.SelectMany(x => x.Steps).Any(x => x.Status == UpgradeStepStatus.Failed);

    /// <summary>How the upgrade ended.</summary>
    public UpgradeOutcome Outcome { get; set; }

    /// <summary>The full path of the backup folder of the files the upgrade changed, or <c>null</c> without one.</summary>
    public string? BackupFolder { get; set; }

    // The session logged the report and wrote upgrade-report.md: a later load of the missing references does not again
    internal bool IsPublished { get; set; }
}

/// <summary>
/// What the upgrade of a session did to each project: the steps of each upgrader with their status.
/// </summary>
public sealed class UpgradeReport
{
    private readonly Lock gate = new();

    /// <summary>The upgraded projects.</summary>
    public List<ProjectUpgradeReport> Projects { get; } = [];

    /// <summary>
    /// The report of <paramref name="upgrader"/> on the project <paramref name="projectFullPath"/>, upgraded from
    /// <paramref name="fromVersion"/>: added the first time, with the steps that apply not run yet. A retry of the upgrader on the
    /// project (see <see cref="AddRetry"/>) has a report of its own, which comes after this one.
    /// </summary>
    public UpgraderReport GetOrAdd(UFile projectFullPath, PackageUpgrader upgrader, PackageVersion fromVersion)
    {
        ArgumentNullException.ThrowIfNull(projectFullPath);
        ArgumentNullException.ThrowIfNull(upgrader);
        ArgumentNullException.ThrowIfNull(fromVersion);
        lock (gate)
        {
            var project = Projects.FirstOrDefault(x => x.ProjectFullPath == projectFullPath);
            if (project is null)
            {
                project = new ProjectUpgradeReport(projectFullPath);
                Projects.Add(project);
            }

            var report = project.Upgraders.FirstOrDefault(x => x.Name == upgrader.Attribute.Name && !x.IsRetry);
            if (report is null)
            {
                report = new UpgraderReport(upgrader.Attribute.Name, fromVersion, upgrader.Attribute.UpdatedVersionRange?.MinVersion, upgrader.GetUpgradeSteps(fromVersion));
                // The upgrade names the section of the report, before the retry that let it run
                var retry = project.Upgraders.FindIndex(x => x.Name == upgrader.Attribute.Name);
                project.Upgraders.Insert(retry >= 0 ? retry : project.Upgraders.Count, report);
            }
            return report;
        }
    }

    /// <summary>
    /// Adds the report of the steps an upgrade of the project by <paramref name="upgrader"/> kept unfinished, run again on the
    /// project at <paramref name="version"/>: the steps the upgrader declares with these IDs, not run yet, then the other parts left.
    /// </summary>
    public UpgraderReport AddRetry(UFile projectFullPath, PackageUpgrader upgrader, PackageVersion version, IReadOnlyList<string> stepsLeft)
    {
        ArgumentNullException.ThrowIfNull(projectFullPath);
        ArgumentNullException.ThrowIfNull(upgrader);
        ArgumentNullException.ThrowIfNull(stepsLeft);
        var declared = upgrader.GetUpgradeSteps(new PackageVersion("0.0.0")).ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        lock (gate)
        {
            var project = Projects.FirstOrDefault(x => x.ProjectFullPath == projectFullPath);
            if (project is null)
            {
                project = new ProjectUpgradeReport(projectFullPath);
                Projects.Add(project);
            }

            // A step the upgrader no longer declares has only its ID; a part of the upgrade that is not a step has no ID
            var report = new UpgraderReport(upgrader.Attribute.Name, version, version, stepsLeft.Where(UpgradeStepInfo.IsStepId).Select(x => declared.GetValueOrDefault(x) ?? new UpgradeStepInfo(x, "", null))) { IsRetry = true };
            report.Steps.AddRange(stepsLeft.Where(x => !UpgradeStepInfo.IsStepId(x)).Select(x => new UpgradeStepResult(null, x, null)));
            project.Upgraders.Add(report);
            return report;
        }
    }

    /// <summary>The report of the upgrade of the project by the upgrader named <paramref name="upgraderName"/>, not of a retry, or <c>null</c>.</summary>
    public UpgraderReport? Find(UFile projectFullPath, string upgraderName)
    {
        lock (gate)
            return Projects.FirstOrDefault(x => x.ProjectFullPath == projectFullPath)?.Upgraders.FirstOrDefault(x => x.Name == upgraderName && !x.IsRetry);
    }
}
