// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Evaluation;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets;

/// <summary>
/// Base class for package upgraders that declare their migrations through the unified
/// <see cref="UpgradeRegistry"/> (<c>r.Code</c> source rewrites and <c>r.Project</c> structural csproj
/// changes). It owns the structural pass: bump the upgrader's package family, then run the gated
/// <c>r.Project</c> registrations against the project loaded once. Subclasses only declare upgrades and,
/// optionally, how to bump their own references — they never re-implement the orchestration.
/// </summary>
public abstract class CodePackageUpgrader : PackageUpgrader, ICodeUpgradeProvider
{
    /// <inheritdoc cref="ICodeUpgradeProvider.DeclareUpgrades"/>
    public abstract void DeclareUpgrades(UpgradeRegistry registry);

    /// <summary>
    /// True if the version upgraded <em>from</em> is numerically below <paramref name="version"/>, ignoring
    /// the prerelease suffix (4.5.3-beta/-dev2/-mystudio all count as already at 4.5.3). So a new gate must be paired
    /// with a Patch bump, see docs/build/versioning.md.
    /// </summary>
    protected static bool UpgradingFromBefore(PackageDependency dependency, PackageVersion version)
        => dependency.Version.MinVersion.Version < version.Version;

    /// <summary>
    /// Bumps the upgrader's own package family to the current version on the given project. Runs before the
    /// structural registrations (which operate on the bumped project). Default is a no-op.
    /// </summary>
    /// <returns>False when the project could not be changed.</returns>
    protected virtual bool UpgradeProjectReferences(UFile projectFullPath, ILogger log)
    {
        return true;
    }

    /// <inheritdoc/>
    public override bool Upgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage, IList<PackageLoadingAssetFile> assetFiles)
    {
        return true;
    }

    /// <inheritdoc/>
    public override bool UpgradeBeforeAssembliesLoaded(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        var solutionProject = dependentPackage.Container as SolutionProject;
        var projectFullPath = solutionProject?.FullPath;
        if (projectFullPath == null)
            return true;

        // Snapshot the original csproj before the passes below rewrite it.
        session.UpgradeBackup?.Snapshot(projectFullPath.ToOSPath());

        // Bump the upgrader's own package family to the current version (subclass hook).
        if (!UpgradeProjectReferences(projectFullPath, log))
            return false;

        // Version-gated structural csproj migrations declared via the upgrade registry (r.Project): load the project once,
        // and run each registration the project is upgrading across. Each step is saved once it returned, so a step that
        // throws leaves the project as the previous one left it, and stops it.
        var registry = new UpgradeRegistry();
        DeclareUpgrades(registry);
        var applicable = registry.ProjectUpgrades
            .Where(registration => UpgradingFromBefore(dependency, registration.GateVersion))
            .ToArray();
        if (applicable.Length == 0)
            return true;

        Project project = null;
        try
        {
            project = VSProjectHelper.LoadProject(projectFullPath.ToOSPath());
            var context = new ProjectUpgradeContext(project, solutionProject, projectFullPath, log) { Backup = session.UpgradeBackup };

            foreach (var registration in applicable)
            {
                try
                {
                    registration.Upgrade(context);
                }
                catch (Exception e)
                {
                    log.Error($"Unable to upgrade project [{projectFullPath.GetFileName()}]: the step \"{registration.Title}\" failed, and the steps after it don't run", e);
                    return false;
                }

                if (context.IsDirty)
                {
                    project.Save();
                    context.IsDirty = false;
                }
                context.ApplyChanges();
            }
        }
        catch (Exception e)
        {
            log.Error($"Unable to upgrade project [{projectFullPath.GetFileName()}]", e);
            return false;
        }
        finally
        {
            project?.ProjectCollection.UnloadAllProjects();
            project?.ProjectCollection.Dispose();
        }

        return true;
    }
}
