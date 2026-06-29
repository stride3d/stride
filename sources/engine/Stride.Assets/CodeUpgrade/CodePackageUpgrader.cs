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
    /// the prerelease suffix (4.4.0-beta1/-dev2/-mystudio all count as already at 4.4.0). The suffix is
    /// cosmetic, so a format change must be paired with a numeric Patch bump and gates key off that number.
    /// </summary>
    protected static bool UpgradingFromBefore(PackageDependency dependency, PackageVersion version)
        => dependency.Version.MinVersion.Version < version.Version;

    /// <summary>
    /// Bumps the upgrader's own package family to the current version on the given project. Runs before the
    /// structural registrations (which operate on the bumped project). Default is a no-op.
    /// </summary>
    protected virtual void UpgradeProjectReferences(UFile projectFullPath, ILogger log)
    {
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
        UpgradeProjectReferences(projectFullPath, log);

        // Version-gated structural csproj migrations declared via the upgrade registry (r.Project):
        // load the project once, run each registration the project is upgrading across, save once.
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
            var context = new ProjectUpgradeContext(project, solutionProject, projectFullPath, log);

            foreach (var registration in applicable)
                registration.Upgrade(context);

            if (context.IsDirty)
                project.Save();
        }
        catch (Exception e)
        {
            log.Warning($"Unable to load project [{projectFullPath.GetFileName()}]", e);
        }
        finally
        {
            project?.ProjectCollection.UnloadAllProjects();
            project?.ProjectCollection.Dispose();
        }

        return true;
    }
}
