// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Evaluation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets;

/// <summary>
/// A structural migration applied to the NEW-version side of an upgrade: it mutates the loaded MSBuild
/// <see cref="ProjectUpgradeContext.Project"/> (csproj XML) and may touch sibling project files on disk.
/// Counterpart to <see cref="CodeUpgrade"/>, which rewrites user source against the OLD closure.
/// </summary>
public delegate void ProjectUpgrade(ProjectUpgradeContext context);

/// <summary>
/// Context handed to a <see cref="ProjectUpgrade"/>: the loaded project, its solution metadata, and a
/// dirty flag the action raises when it changes the project XML (the runner saves once at the end).
/// </summary>
public sealed class ProjectUpgradeContext
{
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

    /// <summary>The logger.</summary>
    public ILogger Log { get; }

    /// <summary>Raised by an action when it changed the project XML, so the runner saves it once at the end.</summary>
    public bool IsDirty { get; set; }
}

/// <summary>
/// One <c>Project</c> registration: the gate version the change landed at, and the structural action to run.
/// </summary>
internal sealed record ProjectUpgradeRegistration(PackageVersion GateVersion, ProjectUpgrade Upgrade);
