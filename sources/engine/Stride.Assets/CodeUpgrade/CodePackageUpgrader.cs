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

        var registry = new UpgradeRegistry();
        DeclareUpgrades(registry);

        // Bump the upgrader's own package family to the current version (subclass hook).
        if (!UpgradeProjectReferences(projectFullPath, log))
            return false;

        // Version-gated structural csproj migrations declared via the upgrade registry (r.Project):
        // load the project once, run each registration the project is upgrading across, save once.
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
                registration.Upgrade(context);

            if (context.IsDirty)
                project.Save();
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

    /// <inheritdoc/>
    public override bool CheckUpgrade(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        // The shader upgrade reads the shaders of the packages the project uses: without them it would write other files, which no later
        // upgrade fixes once the references name this version
        if ((dependentPackage.Container as SolutionProject)?.FullPath is not { } projectFullPath || !ShaderGates(dependency).Any()
            || ShaderSourceUpgrade.UnreadablePackages(projectFullPath, log) is not { } reason)
            return true;
        log.Error($"Unable to upgrade project [{projectFullPath.GetFileNameWithoutExtension()}]: {reason}. Its shader upgrade reads the shaders of its packages, and would write other files without them. Restore the project, then upgrade it again.");
        return false;
    }

    // The shader gates a project upgraded from the version of dependency crosses, in the order of their versions; and all of them
    private IReadOnlyList<ShaderUpgradeRegistration> ShaderGates(PackageDependency dependency, bool all = false)
    {
        var registry = new UpgradeRegistry();
        DeclareUpgrades(registry);
        return registry.ShaderUpgrades.Where(x => all || UpgradingFromBefore(dependency, x.GateVersion)).OrderBy(x => x.GateVersion.Version).ToList();
    }

    /// <inheritdoc/>
    public override bool UpgradeAfterRestore(PackageLoadParameters loadParameters, PackageSession session, ILogger log, Package dependentPackage, PackageDependency dependency, Package dependencyPackage)
    {
        if ((dependentPackage.Container as SolutionProject)?.FullPath is not { } projectFullPath)
            return true;

        // Shader language changes (r.Shader): the shader files of the package, gate by gate in the order of their versions. What it uses is read,
        // never changed: the session upgrades a referenced project before the projects that use it, and a library written for an older
        // version is read at each gate as the gates up to it would write it
        var gates = ShaderGates(dependency);
        if (gates.Count > 0)
        {
            // CheckUpgrade passed before the references moved: only the restore that followed can have broken it
            var name = projectFullPath.GetFileNameWithoutExtension();
            if (ShaderSourceUpgrade.UnreadablePackages(projectFullPath, log) is { } reason)
            {
                log.Error($"Unable to upgrade the shaders of project [{name}]: {reason}. Its references already name this version, so a later upgrade won't do it.");
                return false;
            }

            // Every file it uses is read, for every gate, before any of its own is written
            var folders = ShaderSourceUpgrade.Folders(projectFullPath, dependentPackage);
            ShaderSourceUpgrade.DependencyReader dependencies;
            List<IReadOnlyDictionary<string, string>> used;
            try
            {
                dependencies = new ShaderSourceUpgrade.DependencyReader(ShaderSourceUpgrade.Dependencies(projectFullPath, session, log), ShaderGates(dependency, all: true));
                used = gates.Select(dependencies.Read).ToList();
            }
            catch (Exception e)
            {
                log.Error($"Unable to read the shaders that project [{name}] uses", e);
                return false;
            }
            foreach (var (gate, gateDependencies) in gates.Zip(used))
            {
                if (!ShaderSourceUpgrade.Run(folders, gateDependencies, gate.Steps, session.UpgradeBackup, log))
                    return false;
            }
            dependencies.WriteNotes(log);
        }
        return true;
    }
}
