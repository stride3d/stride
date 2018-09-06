// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.DependencyResolver;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol.Core.Types;
using NuGet.RuntimeModel;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Packages;

namespace Xenko.Core.Assets
{
    partial class PackageSession
    {
        private async Task<RestoreTargetGraph> GenerateRestoreGraph(ILogger log, string projectName, string projectPath)
        {
            var dgFile = await VSProjectHelper.GenerateRestoreGraphFile(log, projectPath);
            var dgProvider = new DependencyGraphSpecRequestProvider(new RestoreCommandProvidersCache(), dgFile);

            using (var cacheContext = new SourceCacheContext())
            {
                var restoreContext = new RestoreArgs();
                restoreContext.CacheContext = cacheContext;
                restoreContext.Log = new NuGet.Common.NullLogger();
                restoreContext.PreLoadedRequestProviders.Add(dgProvider);

                var request = (await dgProvider.CreateRequests(restoreContext)).Single();

                var restoreRequest = request.Request;
                var collectorLogger = new RestoreCollectorLogger(restoreRequest.Log, false);
                var contextForProject = CreateRemoteWalkContext(restoreRequest, collectorLogger);

                // Get external project references
                // If the top level project already exists, update the package spec provided
                // with the RestoreRequest spec.
                var updatedExternalProjects = GetProjectReferences(restoreRequest, contextForProject);

                // Load repositories
                // the external project provider is specific to the current restore project
                contextForProject.ProjectLibraryProviders.Add(new PackageSpecReferenceDependencyProvider(updatedExternalProjects, restoreRequest.Log));


                var walker = new RemoteDependencyWalker(contextForProject);

                var requestProject = request.Request.Project;

                var projectRange = new LibraryRange()
                {
                    Name = projectName,
                    VersionRange = new NuGet.Versioning.VersionRange(requestProject.Version),
                    TypeConstraint = LibraryDependencyTarget.Project | LibraryDependencyTarget.ExternalProject
                };

                var framework = requestProject.TargetFrameworks.First();
                var graphs = new List<GraphNode<RemoteResolveResult>>
                {
                    await walker.WalkAsync(
                    projectRange,
                    framework.FrameworkName,
                    null,
                    RuntimeGraph.Empty,
                    recursive: true)
                };
                return RestoreTargetGraph.Create(graphs, contextForProject, restoreRequest.Log, framework.FrameworkName);
            }
        }

        private async Task PreLoadPackageDependencies(ILogger log, SolutionProject project, PackageLoadParameters loadParameters)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            if (project == null) throw new ArgumentNullException(nameof(project));
            if (loadParameters == null) throw new ArgumentNullException(nameof(loadParameters));

            bool packageDependencyErrors = false;

            // TODO: Remove and recheck Dependencies Ready if some secondary packages are removed?
            if (project.State >= ProjectState.DependenciesReady)
                return;

            log.Verbose("Restore NuGet packages...");

            var restoreGraph = await GenerateRestoreGraph(log, project.Name, project.FullPath);

            // Check if there is any package upgrade to do
            var pendingPackageUpgrades = new List<PendingPackageUpgrade>();
            foreach (var item in restoreGraph.Flattened)
            {
                if (item.Key.Type == LibraryType.Package)
                {
                    var dependencyName = item.Key.Name;
                    var dependencyVersion = item.Key.Version.ToPackageVersion();

                    var packageUpgrader = AssetRegistry.GetPackageUpgrader(dependencyName);
                    if (packageUpgrader != null)
                    {
                        // Check if upgrade is necessary
                        if (dependencyVersion >= packageUpgrader.Attribute.UpdatedVersionRange.MinVersion)
                        {
                            continue;
                        }

                        // Check if upgrade is allowed
                        if (dependencyVersion < packageUpgrader.Attribute.PackageMinimumVersion)
                        {
                            // Throw an exception, because the package update is not allowed and can't be done
                            throw new InvalidOperationException($"Upgrading project [{project.Name}] to use [{dependencyName}] from version [{dependencyVersion}] to [{packageUpgrader.Attribute.UpdatedVersionRange.MinVersion}] is not supported (supported only from version [{packageUpgrader.Attribute.PackageMinimumVersion}]");
                        }

                        log.Info($"Upgrading project [{project.Name}] to use [{dependencyName}] from version [{dependencyVersion}] to [{packageUpgrader.Attribute.UpdatedVersionRange.MinVersion}] will be required");

                        pendingPackageUpgrades.Add(new PendingPackageUpgrade(packageUpgrader, new PackageDependency(dependencyName, new PackageVersionRange(dependencyVersion)), null));
                    }
                }
            }

            var package = project.Package;
            if (pendingPackageUpgrades.Count > 0)
            {
                var upgradeAllowed = packageUpgradeAllowed != false ? PackageUpgradeRequestedAnswer.Upgrade : PackageUpgradeRequestedAnswer.DoNotUpgrade;

                // Need upgrades, let's ask user confirmation
                if (loadParameters.PackageUpgradeRequested != null && !packageUpgradeAllowed.HasValue)
                {
                    upgradeAllowed = loadParameters.PackageUpgradeRequested(package, pendingPackageUpgrades);
                    if (upgradeAllowed == PackageUpgradeRequestedAnswer.UpgradeAll)
                        packageUpgradeAllowed = true;
                    if (upgradeAllowed == PackageUpgradeRequestedAnswer.DoNotUpgradeAny)
                        packageUpgradeAllowed = false;
                }

                if (!PackageLoadParameters.ShouldUpgrade(upgradeAllowed))
                {
                    log.Error($"Necessary package migration for [{package.Meta.Name}] has not been allowed");
                    return;
                }

                // Perform pre assembly load upgrade
                foreach (var pendingPackageUpgrade in pendingPackageUpgrades)
                {
                    var expectedVersion = pendingPackageUpgrade.PackageUpgrader.Attribute.UpdatedVersionRange.MinVersion.ToString();

                    // Update NuGet references
                    try
                    {
                        var projectFile = project.FullPath;
                        var msbuildProject = VSProjectHelper.LoadProject(projectFile.ToWindowsPath());
                        var isProjectDirty = false;

                        var packageReferences = msbuildProject.GetItems("PackageReference").ToList();
                        foreach (var packageReference in packageReferences)
                        {
                            if (packageReference.EvaluatedInclude == pendingPackageUpgrade.Dependency.Name && packageReference.GetMetadataValue("Version") != expectedVersion)
                            {
                                packageReference.SetMetadataValue("Version", expectedVersion);
                                isProjectDirty = true;
                            }
                        }

                        if (isProjectDirty)
                            msbuildProject.Save();

                        msbuildProject.ProjectCollection.UnloadAllProjects();
                        msbuildProject.ProjectCollection.Dispose();
                    }
                    catch (Exception e)
                    {
                        log.Warning($"Unable to load project [{project.FullPath.GetFileName()}]", e);
                    }

                    var packageUpgrader = pendingPackageUpgrade.PackageUpgrader;
                    var dependencyPackage = pendingPackageUpgrade.DependencyPackage;
                    if (!packageUpgrader.UpgradeBeforeAssembliesLoaded(loadParameters, package.Session, log, package, pendingPackageUpgrade.Dependency, dependencyPackage))
                    {
                        log.Error($"Error while upgrading package [{package.Meta.Name}] for [{dependencyPackage.Meta.Name}] from version [{pendingPackageUpgrade.Dependency.Version}] to [{dependencyPackage.Meta.Version}]");
                        return;
                    }
                }
            }

            // Now that our references are upgraded, let's do a real nuget restore (download files)
            await VSProjectHelper.RestoreNugetPackages(log, project.FullPath);

            project.FlattenedDependencies.Clear();
            project.LoadedDependencies.Clear();
            var projectAssetsJsonPath = Path.Combine(project.FullPath.GetFullDirectory(), @"obj", LockFileFormat.AssetsFileName);
            if (File.Exists(projectAssetsJsonPath))
            {
                var format = new LockFileFormat();
                var projectAssets = format.Read(projectAssetsJsonPath);

                // Update dependencies
                foreach (var library in projectAssets.Libraries)
                {
                    project.FlattenedDependencies.Add(new Dependency(library.Name, library.Version.ToPackageVersion(), library.Type == "project" ? DependencyType.Project : DependencyType.Package) { MSBuildProject = library.Type == "project" ? library.MSBuildProject : null });
                }

                foreach (var projectReference in projectAssets.PackageSpec.RestoreMetadata.TargetFrameworks.First().ProjectReferences)
                {
                    var projectName = new UFile(projectReference.ProjectUniqueName).GetFileNameWithoutExtension();
                    project.DirectDependencies.Add(new DependencyRange(projectName, null, DependencyType.Project) { MSBuildProject = projectReference.ProjectPath });
                }

                foreach (var dependency in projectAssets.PackageSpec.TargetFrameworks.First().Dependencies)
                {
                    if (dependency.AutoReferenced)
                        continue;
                    project.DirectDependencies.Add(new DependencyRange(dependency.Name, dependency.LibraryRange.VersionRange.ToPackageVersionRange(), DependencyType.Package));
                }

                // Load dependency (if external)

                // Compute output path
            }

            // 1. Load store package
            foreach (var projectDependency in project.FlattenedDependencies)
            {
                var loadedPackage = packages.Find(projectDependency);
                if (loadedPackage == null)
                {
                    var file = PackageStore.Instance.GetPackageFileName(projectDependency.Name, new PackageVersionRange(projectDependency.Version), constraintProvider);

                    if (file == null)
                    {
                        // TODO: We need to support automatic download of packages. This is not supported yet when only Xenko
                        // package is supposed to be installed, but It will be required for full store
                        log.Error($"The project {project.Name ?? "[Untitled]"} depends on project or package {projectDependency} which is not installed");
                        packageDependencyErrors = true;
                        continue;
                    }

                    // Load package
                    loadedPackage = PreLoadPackage(log, file, true, loadParameters);
                    Projects.Add(new StandalonePackage(loadedPackage));
                }

                if (loadedPackage != null)
                    project.LoadedDependencies.Add(loadedPackage);
                // TODO CSPROJ=XKPKG
                //if (loadedPackage == null || loadedPackage.State < ProjectState.DependenciesReady)
                //    packageDependencyErrors = true;
            }

            // 2. Load local packages
            /*foreach (var packageReference in package.LocalDependencies)
            {
                // Check that the package was not already loaded, otherwise return the same instance
                if (Packages.ContainsById(packageReference.Id))
                {
                    continue;
                }

                // Expand the string of the location
                var newLocation = packageReference.Location;

                var subPackageFilePath = package.RootDirectory != null ? UPath.Combine(package.RootDirectory, newLocation) : newLocation;

                // Recursive load
                var loadedPackage = PreLoadPackage(log, subPackageFilePath.FullPath, false, loadedPackages, loadParameters);

                if (loadedPackage == null || loadedPackage.State < PackageState.DependenciesReady)
                    packageDependencyErrors = true;
            }*/

            // 3. Update package state
            if (!packageDependencyErrors)
            {
                project.State = ProjectState.DependenciesReady;
            }
        }

        private static RemoteWalkContext CreateRemoteWalkContext(RestoreRequest request, RestoreCollectorLogger logger)
        {
            var context = new RemoteWalkContext(
                request.CacheContext,
                logger);

            foreach (var provider in request.DependencyProviders.LocalProviders)
            {
                context.LocalLibraryProviders.Add(provider);
            }

            foreach (var provider in request.DependencyProviders.RemoteProviders)
            {
                context.RemoteLibraryProviders.Add(provider);
            }

            return context;
        }

        private static ExternalProjectReference ToExternalProjectReference(PackageSpec project)
        {
            return new ExternalProjectReference(
                project.Name,
                project,
                msbuildProjectPath: null,
                projectReferences: Enumerable.Empty<string>());
        }

        private static List<ExternalProjectReference> GetProjectReferences(RestoreRequest _request, RemoteWalkContext context)
        {
            // External references
            var updatedExternalProjects = new List<ExternalProjectReference>();

            if (_request.ExternalProjects.Count == 0)
            {
                // If no projects exist add the current project.json file to the project
                // list so that it can be resolved.
                updatedExternalProjects.Add(ToExternalProjectReference(_request.Project));
            }
            else if (_request.ExternalProjects.Count > 0)
            {
                // There should be at most one match in the external projects.
                var rootProjectMatches = _request.ExternalProjects.Where(proj =>
                        string.Equals(
                            _request.Project.Name,
                            proj.PackageSpecProjectName,
                            StringComparison.OrdinalIgnoreCase))
                        .ToList();

                if (rootProjectMatches.Count > 1)
                {
                    throw new InvalidOperationException($"Ambiguous project name '{_request.Project.Name}'.");
                }

                var rootProject = rootProjectMatches.SingleOrDefault();

                if (rootProject != null)
                {
                    // Replace the project spec with the passed in package spec,
                    // for installs which are done in memory first this will be
                    // different from the one on disk
                    updatedExternalProjects.AddRange(_request.ExternalProjects
                        .Where(project =>
                            !project.UniqueName.Equals(rootProject.UniqueName, StringComparison.Ordinal)));

                    var updatedReference = new ExternalProjectReference(
                        rootProject.UniqueName,
                        _request.Project,
                        rootProject.MSBuildProjectPath,
                        rootProject.ExternalProjectReferences);

                    updatedExternalProjects.Add(updatedReference);
                }
            }
            else
            {
                // External references were passed, but the top level project wasn't found.
                // This is always due to an internal issue and typically caused by errors 
                // building the project closure.
                throw new InvalidOperationException($"Missing external reference metadata for {_request.Project.Name}");
            }

            return updatedExternalProjects;
        }
    }
}
