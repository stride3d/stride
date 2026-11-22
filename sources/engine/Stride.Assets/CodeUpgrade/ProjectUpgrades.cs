// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Evaluation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets;

/// <summary>
/// A structural migration applied to the NEW-version side of an upgrade: it mutates the loaded MSBuild
/// <see cref="ProjectUpgradeContext.Project"/> (csproj XML) and may change other files of the project through the context.
/// Counterpart to <see cref="CodeUpgrade"/>, which rewrites user source against the OLD closure.
/// </summary>
public delegate void ProjectUpgrade(ProjectUpgradeContext context);

/// <summary>
/// Context handed to a <see cref="ProjectUpgrade"/>: the loaded project, its solution metadata, a dirty flag the action
/// raises when it changes the project XML, and the changes it makes to other files.
/// </summary>
/// <remarks>
/// A step is applied whole or not at all: the runner saves the project and applies the file changes of a step only once
/// it returned, and drops them when it throws. A file the step couldn't change is reported with <see cref="UnitFailed"/>,
/// and the other files still change.
/// </remarks>
public sealed class ProjectUpgradeContext
{
    private readonly List<(string Path, Action Apply, string DoneMessage)> pendingChanges = [];
    private readonly List<UpgradeUnitFailure> failedUnits = [];

    internal ProjectUpgradeContext(Project project, SolutionProject solutionProject, UFile projectFullPath, ILogger log)
    {
        Project = project;
        SolutionProject = solutionProject;
        ProjectFullPath = projectFullPath;
        Log = log;
    }

    /// <summary>The loaded MSBuild project to mutate.</summary>
    public Project Project { get; }

    /// <summary>The owning solution project (provides type and platform).</summary>
    public SolutionProject SolutionProject { get; }

    /// <summary>The full path to the project file.</summary>
    public UFile ProjectFullPath { get; }

    /// <summary>
    /// The upgrade backup, or <c>null</c> when none is requested. <see cref="WriteFile"/> and <see cref="MoveFile"/> use it;
    /// a step that changes a file another way snapshots it first (the project file is already snapshotted).
    /// </summary>
    public UpgradeBackup Backup { get; internal init; }

    /// <summary>The logger.</summary>
    public ILogger Log { get; }

    /// <summary>Raised by an action when it changed the project XML, so the runner saves it once the step returned.</summary>
    public bool IsDirty { get; set; }

    /// <summary>The files that steps could not change, from <see cref="UnitFailed"/>.</summary>
    internal IReadOnlyList<UpgradeUnitFailure> FailedUnits => failedUnits;

    /// <summary>
    /// Writes <paramref name="text"/> to the file <paramref name="path"/> once the step returned, after the backup took a copy.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    /// <param name="text">The new text of the file.</param>
    /// <param name="encoding">The encoding, with its byte order mark if it has one.</param>
    /// <param name="doneMessage">Logged once the file is written, or <c>null</c>.</param>
    public void WriteFile(string path, string text, Encoding encoding, string doneMessage = null)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(encoding);
        pendingChanges.Add((path, () =>
        {
            Backup?.Snapshot(path);
            File.WriteAllText(path, text, encoding);
        }, doneMessage));
    }

    /// <summary>
    /// Moves the file <paramref name="source"/> to <paramref name="destination"/>, replacing it, once the step returned, after
    /// the backup took a copy of both (of the destination, that it didn't exist).
    /// </summary>
    /// <param name="source">The full path of the file.</param>
    /// <param name="destination">Its new full path.</param>
    /// <param name="doneMessage">Logged once the file is moved, or <c>null</c>.</param>
    public void MoveFile(string source, string destination, string doneMessage = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        pendingChanges.Add((source, () =>
        {
            Backup?.Snapshot(source);
            Backup?.Snapshot(destination);
            File.Move(source, destination, overwrite: true);
        }, doneMessage));
    }

    /// <summary>
    /// Reports a file the step could not change: it is left as it is, and the step changes the other files.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    /// <param name="exception">Why it could not be changed.</param>
    public void UnitFailed(string path, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(path);
        failedUnits.Add(new UpgradeUnitFailure(path, Exception: exception));
        Log.Warning($"Could not upgrade [{Path.GetRelativePath(ProjectFullPath.GetFullDirectory().ToOSPath(), path)}] of [{ProjectFullPath.GetFileName()}]; it is left unchanged.", exception);
    }

    /// <summary>
    /// Reports a file the step could not change, with what to do by hand: it is left as it is, and the step changes the other files.
    /// </summary>
    /// <param name="path">The full path of the file.</param>
    /// <param name="error">Why it could not be changed, and what to do by hand.</param>
    public void UnitFailed(string path, string error)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(error);
        failedUnits.Add(new UpgradeUnitFailure(path, Error: error));
        Log.Warning($"[{Path.GetRelativePath(ProjectFullPath.GetFullDirectory().ToOSPath(), path)}] of [{ProjectFullPath.GetFileName()}] is left unchanged: {error}");
    }

    /// <summary>Applies the file changes of the step that returned.</summary>
    /// <returns>The full paths of the files changed.</returns>
    internal List<string> ApplyChanges()
    {
        var changedFiles = new List<string>();
        foreach (var (path, apply, doneMessage) in pendingChanges)
        {
            try
            {
                apply();
            }
            catch (Exception e)
            {
                UnitFailed(path, e);
                continue;
            }
            changedFiles.Add(path);
            if (doneMessage is not null)
                Log.Info(doneMessage);
        }
        pendingChanges.Clear();
        return changedFiles;
    }
}

/// <summary>
/// One <c>Project</c> registration: the gate version the change landed at, its title, the structural action to run, whether
/// it only keeps the place of a removed step, and whether a project it leaves unchanged may behave differently.
/// </summary>
internal sealed record ProjectUpgradeRegistration(PackageVersion GateVersion, string Title, ProjectUpgrade Upgrade, bool IsRetired = false, bool MayChangeBehaviour = false);
