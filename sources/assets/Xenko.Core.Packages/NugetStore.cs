// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.Packaging;
using NuGet.ProjectManagement;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using Xenko.Core;
using Xenko.Core.Extensions;
using Xenko.Core.Windows;
using ISettings = NuGet.Configuration.ISettings;
using PackageSource = NuGet.Configuration.PackageSource;
using PackageSourceProvider = NuGet.Configuration.PackageSourceProvider;
using Settings = NuGet.Configuration.Settings;
using NuGet.Resolver;
using System.Reflection;
using NuGet.Frameworks;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Xenko.Core.Packages
{
    /// <summary>
    /// Abstraction to interact with a store backed by the NuGet infrastructure.
    /// </summary>
    public class NugetStore : INugetDownloadProgress
    {
        private const string DefaultTargets = @"Targets\Xenko.Common.targets";
        private const string DefaultTargetsOld = @"Targets\SiliconStudio.Common.targets";

        public const string DefaultGamePackagesDirectory = "GamePackages";

        public const string DefaultConfig = "store.config";

        public const string MainExecutables = @"Bin\Windows\Xenko.GameStudio.exe,Bin\Windows-Direct3D11\Xenko.GameStudio.exe";
        public const string PrerequisitesInstaller = @"Bin\Prerequisites\install-prerequisites.exe";

        public const string DefaultPackageSource = "https://packages.xenko.com/nuget";

        private IPackagesLogger logger;
        private readonly NuGetPackageManager manager, managerV2;
        private readonly ISettings settings, localSettings;
        private ProgressReport currentProgressReport;

        private static Regex powerShellProgressRegex = new Regex(@".*\[ProgressReport:\s*(\d*)%\].*");

        /// <summary>
        /// Initialize NugetStore using <paramref name="rootDirectory"/> as location of the local copies,
        /// and a configuration file <paramref name="configFile"/> as well as an override configuration
        /// file <paramref name="overrideFile"/> where all settings of <paramref name="overrideFile"/> also
        /// presents in <paramref name="configFile"/> take precedence. 
        /// </summary>
        /// <param name="rootDirectory">The location of the Nuget store.</param>
        public NugetStore(string rootDirectory, string configFile = DefaultConfig)
        {
            RootDirectory = rootDirectory ?? throw new ArgumentNullException(nameof(rootDirectory));

            settings = NuGet.Configuration.Settings.LoadDefaultSettings(rootDirectory);

            // Add dev source
            Directory.CreateDirectory(Environment.ExpandEnvironmentVariables(DevSource));
            CheckPackageSource("Xenko Dev", DevSource);
            CheckPackageSource("Xenko", DefaultPackageSource);

            // Override file does not exist, fallback to default config file
            var configFileName = configFile;
            var configFilePath = Path.Combine(rootDirectory, configFileName);

            if (File.Exists(configFilePath))
            {
                localSettings = NuGet.Configuration.Settings.LoadDefaultSettings(rootDirectory, configFileName, machineWideSettings: null);

                // Replicate packageSources in user config so that NuGet restore can find them as well
                foreach (var x in localSettings.GetSettingValues("packageSources", true))
                {
                    CheckPackageSource(x.Key, x.Value);
                }
            }

            InstallPath = SettingsUtility.GetGlobalPackagesFolder(settings);

            var pathContext = NuGetPathContext.Create(settings);
            InstalledPathResolver = new FallbackPackagePathResolver(pathContext);
            var packageSourceProvider = new PackageSourceProvider(settings);

            var availableSources = packageSourceProvider.LoadPackageSources().Where(source => source.IsEnabled);
            var packageSources = new List<PackageSource>();
            packageSources.AddRange(availableSources);
            PackageSources = packageSources;

            // Setup source provider as a V3 only.
            sourceRepositoryProvider = new NugetSourceRepositoryProvider(packageSourceProvider, this);

            manager = new NuGetPackageManager(sourceRepositoryProvider, settings, InstallPath);
            // Override PackagePathResolver
            // Workaround for https://github.com/NuGet/Home/issues/6639
            manager.PackagesFolderNuGetProject.GetType().GetProperty("PackagePathResolver", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager.PackagesFolderNuGetProject, new PackagePathResolverV3(InstallPath));

            // Obsolete (Xenko 2.x support)
            InstallPathV2 = Path.Combine(RootDirectory, DefaultGamePackagesDirectory);
            managerV2 = new NuGetPackageManager(sourceRepositoryProvider, settings, InstallPathV2);
            PathResolverV2 = new PackagePathResolver(InstallPathV2);
        }

        private void CheckPackageSource(string name, string url)
        {
            var settingsPackageSources = settings.GetSettingValues("packageSources", true);
            if (!settingsPackageSources.Any(x => x.Key == name && x.Value == url))
            {
                settings.DeleteValue("packageSources", name);
                settings.SetValue("packageSources", name, url);
            }
        }

        private readonly NugetSourceRepositoryProvider sourceRepositoryProvider;

        public string DevSource { get; } = @"%LocalAppData%\Xenko\NugetDev";

        /// <summary>
        /// Path under which all packages will be installed or cached.
        /// </summary>
        public string RootDirectory { get; }

        /// <summary>
        /// Path where all packages are installed.
        /// Usually `InstallPath = RootDirectory/RepositoryPath`.
        /// </summary>
        public string InstallPath { get; }

        /// <summary>
        /// Path where all packages are installed.
        /// Usually `InstallPath = RootDirectory/RepositoryPath`.
        /// </summary>
        private string InstallPathV2 { get; }

        /// <summary>
        /// List of package Ids under which the main package is known. Usually just one entry, but
        /// we could have several in case there is a product name change.
        /// </summary>
        public IReadOnlyCollection<string> MainPackageIds { get; } = new[] { "Xenko" };

        /// <summary>
        /// Package Id of the Visual Studio Integration plugin.
        /// </summary>
        public string VsixPluginId { get; } = "Xenko.VisualStudio.Package";

        /// <summary>
        /// Path to the Common.targets file. This files list all installed versions
        /// </summary>
        public string TargetFile => Path.Combine(RootDirectory, DefaultTargets);

        /// <summary>
        /// Path to the Common.targets file. This files list all installed versions
        /// </summary>
        public string TargetFileOld => Path.Combine(RootDirectory, DefaultTargetsOld);

        /// <summary>
        /// Logger for all operations of the package manager.
        /// </summary>
        public IPackagesLogger Logger
        {
            get
            {
                return logger ?? NullPackagesLogger.Instance;
            }

            set
            {
                logger = value;
            }
        }

        private ILogger NativeLogger => new NugetLogger(Logger);

        private IEnumerable<PackageSource> PackageSources { get; }

        /// <summary>
        /// Helper to locate packages.
        /// </summary>
        private FallbackPackagePathResolver InstalledPathResolver { get; }

        private PackagePathResolver PathResolverV2 { get; }

        public event Action<int> DownloadProgressChanged;

        /// <summary>
        /// Event executed when a package's installation has completed.
        /// </summary>
        public event EventHandler<PackageOperationEventArgs> NugetPackageInstalled;

        /// <summary>
        /// Event executed when a package's installation is in progress.
        /// </summary>
        public event EventHandler<PackageOperationEventArgs> NugetPackageInstalling;

        /// <summary>
        /// Event executed when a package's uninstallation has completed.
        /// </summary>
        public event EventHandler<PackageOperationEventArgs> NugetPackageUninstalled;

        /// <summary>
        /// Event executed when a package's uninstallation is in progress.
        /// </summary>
        public event EventHandler<PackageOperationEventArgs> NugetPackageUninstalling;

        /// <summary>
        /// Installation path of <paramref name="package"/>
        /// </summary>
        /// <param name="id">Id of package to query.</param>
        /// <param name="version">Version of package to query.</param>
        /// <returns>The installation path if installed, null otherwise.</returns>
        public string GetInstalledPath(string id, PackageVersion version)
        {
            // Xenko 2.x still installs in GamePackages
            if (IsPackageV2(id, version))
            {
                return PathResolverV2.GetInstallPath(new PackageIdentity(id, version.ToNuGetVersion()));
            }

            return InstalledPathResolver.GetPackageDirectory(id, version.ToNuGetVersion());
        }

        /// <summary>
        /// Get the most recent version associated to <paramref name="packageIds"/>. To make sense
        /// it is assumed that packageIds represent the same package under a different name.
        /// </summary>
        /// <param name="packageIds">List of Ids representing a package name.</param>
        /// <returns>The most recent version of `GetPackagesInstalled (packageIds)`.</returns>
        public NugetLocalPackage GetLatestPackageInstalled(IEnumerable<string> packageIds)
        {
            return GetPackagesInstalled(packageIds).FirstOrDefault();
        }

        /// <summary>
        /// List of all packages represented by <paramref name="packageIds"/>. The list is ordered
        /// from the most recent version to the oldest.
        /// </summary>
        /// <param name="packageIds">List of Ids representing the package names to retrieve.</param>
        /// <returns>The list of packages sorted from the most recent to the oldest.</returns>
        public IList<NugetLocalPackage> GetPackagesInstalled(IEnumerable<string> packageIds)
        {
            return packageIds.SelectMany(GetLocalPackages).OrderByDescending(p => p.Version).ToList();
        }

        /// <summary>
        /// List of all installed packages.
        /// </summary>
        /// <returns>A list of packages.</returns>
        public IEnumerable<NugetLocalPackage> GetLocalPackages(string packageId)
        {
            var res = new List<NugetLocalPackage>();

            void FindPackages(bool packagesV2)
            {
                var localResource = packagesV2 ? (FindLocalPackagesResource)new FindLocalPackagesResourceV2(InstallPathV2) : new FindLocalPackagesResourceV3(InstallPath);
                var packages = localResource.FindPackagesById(packageId, NativeLogger, CancellationToken.None);
                foreach (var package in packages)
                {
                    // V2 packages will be cached in V3 folder as well, so make sure we don't list them twice
                    if (IsPackageV2(package.Identity.Id, package.Identity.Version.ToPackageVersion()) == packagesV2)
                        res.Add(new NugetLocalPackage(package));
                }
            }

            // Try both V2 and V3
            FindPackages(false);
            FindPackages(true);

            return res;
        }

        /// <summary>
        /// Name of variable used to hold the version of <paramref name="packageId"/>.
        /// </summary>
        /// <param name="packageId">The package Id.</param>
        /// <returns>The name of the variable holding the version of <paramref name="packageId"/>.</returns>
        public static string GetPackageVersionVariable(string packageId, string packageVariablePrefix = "XenkoPackage")
        {
            if (packageId == null) throw new ArgumentNullException(nameof(packageId));
            var newPackageId = packageId.Replace(".", string.Empty);
            return packageVariablePrefix + newPackageId + "Version";
        }

        /// <summary>
        /// Lock to ensure atomicity of updates to the local repository.
        /// </summary>
        /// <returns>A Lock.</returns>
        private IDisposable GetLocalRepositoryLock()
        {
            return FileLock.Wait("nuget.lock");
        }

        /// <summary>
        /// Update <see cref="TargetFile"/> content with the list of non-internal packages
        /// that is used to build a solution against a specific revision and handle the case
        /// that a revision does not exist anymore.
        /// This can be safely called from multiple instance as it is protected via a <see cref="FileLock"/>.
        /// </summary>
        public void UpdateTargets()
        {
            using (GetLocalRepositoryLock())
            {
                UpdateTargetsHelper();
            }
        }

        /// <summary>
        /// See <see cref="UpdateTargets"/>. This is the non-concurrent version, always make sure
        /// to hold the lock for the local repository.
        /// </summary>
        private void UpdateTargetsHelper()
        {
            var packages = new List<NugetLocalPackage>();

            // Get latest package only for each MainPackageIds (up to 2.x)
            var xenkoOldPackage = GetLocalPackages("Xenko").Where(package => !((package.Tags != null) && package.Tags.Contains("internal"))).Where(x => x.Version.Version.Major < 3).OrderByDescending(p => p.Version).FirstOrDefault();
            if (xenkoOldPackage != null)
                packages.Add(xenkoOldPackage);

            // Xenko 1.x and 2.x are using SiliconStudio target files
            var oldPackages = packages.Where(x => x.Id == "Xenko" && x.Version.Version.Major < 3).ToList();
            var newPackages = packages.Where(x => !oldPackages.Contains(x)).ToList();

            foreach (var target in new[] { new { File = TargetFile, PackageVersionPrefix = "XenkoPackage", Packages = newPackages }, new { File = TargetFileOld, PackageVersionPrefix = "SiliconStudioPackage", Packages = oldPackages } })
            {
                // Generate target file
                var targetGenerator = new TargetGenerator(this, target.Packages, target.PackageVersionPrefix);
                var targetFileContent = targetGenerator.TransformText();

                var targetFilePath = Path.GetDirectoryName(target.File);

                // Make sure directory exists
                if (targetFilePath != null && !Directory.Exists(targetFilePath))
                    Directory.CreateDirectory(targetFilePath);

                File.WriteAllText(target.File, targetFileContent, Encoding.UTF8);
            }
        }
        
        /// <summary>
        /// Name of main executable of current store.
        /// </summary>
        /// <returns>Name of the executable.</returns>
        public string GetMainExecutables()
        {
            return MainExecutables;
        }

        /// <summary>
        /// Locate the main executable from a given package installation path. It throws exceptions if not found.
        /// </summary>
        /// <param name="packagePath">The package installation path.</param>
        /// <returns>The main executable.</returns>
        public string LocateMainExecutable(string packagePath)
        {
            var mainExecutableList = GetMainExecutables();
            var fullExePath = mainExecutableList.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => Path.Combine(packagePath, x)).FirstOrDefault(File.Exists);
            if (fullExePath == null)
                throw new InvalidOperationException("Unable to locate the executable for the selected version");

            return fullExePath;
        }

        /// <summary>
        /// Name of prerequisites executable of current store.
        /// </summary>
        /// <returns>Name of the executable.</returns>
        public string GetPrerequisitesInstaller()
        {
            return PrerequisitesInstaller;
        }

#region Manager
        /// <summary>
        /// Fetch, if not already downloaded, and install the package represented by
        /// (<paramref name="packageId"/>, <paramref name="version"/>).
        /// </summary>
        /// <remarks>It is safe to call it concurrently be cause we operations are done using the FileLock.</remarks>
        /// <param name="packageId">Name of package to install.</param>
        /// <param name="version">Version of package to install.</param>
        public async Task<NugetLocalPackage> InstallPackage(string packageId, PackageVersion version, ProgressReport progress)
        {
            // Xenko 2.x still installs in GamePackages
            var currentManager = IsPackageV2(packageId, version) ? managerV2 : manager;
            using (GetLocalRepositoryLock())
            {
                currentProgressReport = progress;
                try
                {
                    var identity = new PackageIdentity(packageId, version.ToNuGetVersion());

                    var resolutionContext = new ResolutionContext(
                        DependencyBehavior.Lowest,
                        true,
                        true,
                        VersionConstraints.None);

                    var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();

                    var projectContext = new EmptyNuGetProjectContext()
                    {
                        ActionType = NuGetActionType.Install,
                        PackageExtractionContext = new PackageExtractionContext(NativeLogger),
                    };

                    ActivityCorrelationId.StartNew();

                    {
                        // Equivalent to:
                        //   await manager.InstallPackageAsync(manager.PackagesFolderNuGetProject,
                        //       identity, resolutionContext, projectContext, repositories,
                        //       Array.Empty<SourceRepository>(),  // This is a list of secondary source respositories, probably empty
                        //       CancellationToken.None);
                        using (var sourceCacheContext = new SourceCacheContext())
                        {
                            var nuGetProject = currentManager.PackagesFolderNuGetProject;
                            var packageIdentity = identity;
                            var nuGetProjectContext = projectContext;
                            var primarySources = repositories;
                            var secondarySources = Array.Empty<SourceRepository>();
                            var token = CancellationToken.None;
                            var downloadContext = new PackageDownloadContext(sourceCacheContext);

                            // Step-1 : Call PreviewInstallPackageAsync to get all the nuGetProjectActions
                            var nuGetProjectActions = await currentManager.PreviewInstallPackageAsync(nuGetProject, packageIdentity, resolutionContext,
                                nuGetProjectContext, primarySources, secondarySources, token);

                            // Notify that installations started.
                            foreach (var operation in nuGetProjectActions)
                            {
                                if (operation.NuGetProjectActionType == NuGetProjectActionType.Install)
                                {
                                    var installPath = GetInstalledPath(operation.PackageIdentity.Id, operation.PackageIdentity.Version.ToPackageVersion());
                                    OnPackageInstalling(this, new PackageOperationEventArgs(new PackageName(operation.PackageIdentity.Id, operation.PackageIdentity.Version.ToPackageVersion()), installPath));
                                }
                            }

                            NuGetPackageManager.SetDirectInstall(packageIdentity, nuGetProjectContext);

                            // Step-2 : Execute all the nuGetProjectActions
                            if (IsPackageV2(packageId, version))
                            {
                                await currentManager.ExecuteNuGetProjectActionsAsync(
                                    nuGetProject,
                                    nuGetProjectActions,
                                    nuGetProjectContext,
                                    downloadContext,
                                    token);
                            }
                            else
                            {
                                // Download and install package in the global cache (can't use NuGetPackageManager anymore since it is designed for V2)
                                foreach (var operation in nuGetProjectActions)
                                {
                                    if (operation.NuGetProjectActionType == NuGetProjectActionType.Install)
                                    {
                                        using (var downloadResult = await PackageDownloader.GetDownloadResourceResultAsync(primarySources, packageIdentity, downloadContext, InstallPath, NativeLogger, token))
                                        {
                                            if (downloadResult.Status != DownloadResourceResultStatus.Available)
                                                throw new InvalidOperationException($"Could not download package {packageIdentity}");

                                            using (var installResult = await GlobalPackagesFolderUtility.AddPackageAsync(packageIdentity, downloadResult.PackageStream, InstallPath, NativeLogger, token))
                                            {
                                                if (installResult.Status != DownloadResourceResultStatus.Available)
                                                    throw new InvalidOperationException($"Could not install package {packageIdentity}");
                                            }
                                        }
                                    }
                                }
                            }

                            NuGetPackageManager.ClearDirectInstall(nuGetProjectContext);

                            // Notify that installations completed.
                            foreach (var operation in nuGetProjectActions)
                            {
                                if (operation.NuGetProjectActionType == NuGetProjectActionType.Install)
                                {
                                    var installPath = GetInstalledPath(operation.PackageIdentity.Id, operation.PackageIdentity.Version.ToPackageVersion());
                                    OnPackageInstalled(this, new PackageOperationEventArgs(new PackageName(operation.PackageIdentity.Id, operation.PackageIdentity.Version.ToPackageVersion()), installPath));
                                }
                            }
                        }
                    }

                    // Load the recently installed package
                    var installedPackages = GetPackagesInstalled(new[] { packageId });
                    return installedPackages.FirstOrDefault(p => p.Version == version);
                }
                finally
                {
                    currentProgressReport = null;
                }
            }
        }

        /// <summary>
        /// Uninstall <paramref name="package"/>, while still keeping the downloaded file in the cache.
        /// </summary>
        /// <remarks>It is safe to call it concurrently be cause we operations are done using the FileLock.</remarks>
        /// <param name="package">Package to uninstall.</param>
        public async Task UninstallPackage(NugetPackage package, ProgressReport progress)
        {
#if DEBUG
            var installedPackages = GetPackagesInstalled(new [] {package.Id});
            Debug.Assert(installedPackages.FirstOrDefault(p => p.Equals(package)) != null);
#endif
            using (GetLocalRepositoryLock())
            {
                currentProgressReport = progress;
                try
                {
                    var identity = new PackageIdentity(package.Id, package.Version.ToNuGetVersion());

                    // Notify that uninstallation started.
                    var installPath = GetInstalledPath(identity.Id, identity.Version.ToPackageVersion());
                    if (installPath == null)
                        throw new InvalidOperationException($"Could not find installation path for package {identity}");
                    OnPackageUninstalling(this, new PackageOperationEventArgs(new PackageName(package.Id, package.Version), installPath));

                    var projectContext = new EmptyNuGetProjectContext()
                    {
                        ActionType = NuGetActionType.Uninstall,
                        PackageExtractionContext = new PackageExtractionContext(NativeLogger)
                    };

                    // Simply delete the installed package and its .nupkg installed in it.
                    // Note: this doesn't seem to work because it looks for folder in V2 pattern: <id>.<version> rathern than <id>/<version>
                    //await currentManager.PackagesFolderNuGetProject.DeletePackage(package.Identity, projectContext, CancellationToken.None);
                    await Task.Run(() => FileSystemUtility.DeleteDirectorySafe(installPath, true, projectContext));
                    if (IsPackageV2(package.Id, package.Version))
                    {
                        // We also need to clean up global V3 folder (download happen in that folder)
                        var installedPathV3 = InstalledPathResolver.GetPackageDirectory(package.Id, package.Version.ToNuGetVersion());
                        if (installedPathV3 != null)
                            await Task.Run(() => FileSystemUtility.DeleteDirectorySafe(installedPathV3, true, projectContext));
                    }

                    // Notify that uninstallation completed.
                    OnPackageUninstalled(this, new PackageOperationEventArgs(new PackageName(package.Id, package.Version), installPath));
                    //currentProgressReport = progress;
                    //try
                    //{
                    //    manager.UninstallPackage(package.IPackage);
                    //}
                    //finally
                    //{
                    //    currentProgressReport = null;
                    //}
                }
                finally
                {
                    currentProgressReport = null;
                }
            }
        }

        /// <summary>
        /// Find the installed package <paramref name="packageId"/> using the version <paramref name="versionRange"/> if not null, otherwise the <paramref name="constraintProvider"/> if specified.
        /// If no constraints are specified, the first found entry, whatever it means for NuGet, is used.
        /// </summary>
        /// <param name="packageId">Name of the package.</param>
        /// <param name="versionRange">The version range.</param>
        /// <param name="constraintProvider">The package constraint provider.</param>
        /// <param name="allowPrereleaseVersions">if set to <c>true</c> [allow prelease version].</param>
        /// <param name="allowUnlisted">if set to <c>true</c> [allow unlisted].</param>
        /// <returns>A Package matching the search criterion or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">packageIdentity</exception>
        /// <returns></returns>
        public NugetPackage FindLocalPackage(string packageId, PackageVersion version = null, ConstraintProvider constraintProvider = null, bool allowPrereleaseVersions = true, bool allowUnlisted = false)
        {
            var versionRange = new PackageVersionRange(version);
            return FindLocalPackage(packageId, versionRange, constraintProvider, allowPrereleaseVersions, allowUnlisted);
        }

        /// <summary>
        /// Find the installed package <paramref name="packageId"/> using the version <paramref name="versionRange"/> if not null, otherwise the <paramref name="constraintProvider"/> if specified.
        /// If no constraints are specified, the first found entry, whatever it means for NuGet, is used.
        /// </summary>
        /// <param name="packageId">Name of the package.</param>
        /// <param name="versionRange">The version range.</param>
        /// <param name="constraintProvider">The package constraint provider.</param>
        /// <param name="allowPrereleaseVersions">if set to <c>true</c> [allow prelease version].</param>
        /// <param name="allowUnlisted">if set to <c>true</c> [allow unlisted].</param>
        /// <returns>A Package matching the search criterion or null if not found.</returns>
        /// <exception cref="System.ArgumentNullException">packageIdentity</exception>
        /// <returns></returns>
        public NugetLocalPackage FindLocalPackage(string packageId, PackageVersionRange versionRange = null, ConstraintProvider constraintProvider = null, bool allowPrereleaseVersions = true, bool allowUnlisted = false)
        {
            // if an explicit version is specified, disregard the 'allowUnlisted' argument
            // and always allow unlisted packages.
            if (versionRange != null)
            {
                allowUnlisted = true;
            }
            else if (!allowUnlisted && ((constraintProvider == null) || !constraintProvider.HasConstraints))
            {
                // Simple case, we just get the most recent version based on `allowPrereleaseVersions`.
                return GetPackagesInstalled(new[] { packageId }).FirstOrDefault(p => allowPrereleaseVersions || string.IsNullOrEmpty(p.Version.SpecialVersion));
            }

            var packages = GetLocalPackages(packageId);

            if (!allowUnlisted)
            {
                packages = packages.Where(p=>p.Listed);
            }

            if (constraintProvider != null)
            {
                versionRange = constraintProvider.GetConstraint(packageId) ?? versionRange;
            }
            if (versionRange != null)
            {
                packages = packages.Where(p => versionRange.Contains(p.Version));
            }
            return packages?.FirstOrDefault(p => allowPrereleaseVersions || string.IsNullOrEmpty(p.Version.SpecialVersion));
        }

        /// <summary>
        /// Find available packages from source ith Ids matching <paramref name="packageIds"/>.
        /// </summary>
        /// <param name="packageIds">List of package Ids we are looking for.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of packages matching <paramref name="packageIds"/> or an empty list if none is found.</returns>
        public async Task<IEnumerable<NugetServerPackage>> FindSourcePackages(IReadOnlyCollection<string> packageIds, CancellationToken cancellationToken)
        {
            var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
            var res = new List<NugetServerPackage>();
            foreach (var packageId in packageIds)
            {
                await FindSourcePacakgesByIdHelper(packageId, res, repositories, cancellationToken);
            }
            return res;
        }

        /// <summary>
        /// Find available packages from source with Id matching <paramref name="packageId"/>.
        /// </summary>
        /// <param name="packageId">Id of package we are looking for.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>A list of packages matching <paramref name="packageId"/> or an empty list if none is found.</returns>
        public async Task<IEnumerable<NugetServerPackage>> FindSourcePackagesById(string packageId, CancellationToken cancellationToken)
        {
            var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
            var res = new List<NugetServerPackage>();
            await FindSourcePacakgesByIdHelper(packageId, res, repositories, cancellationToken);
            return res;
        }

        private bool IsPackageV2(string packageId, PackageVersion version)
        {
            return packageId == "Xenko" && version < new PackageVersion("2.3.0.0");
        }

        private async Task FindSourcePacakgesByIdHelper(string packageId, List<NugetServerPackage> resultList, SourceRepository [] repositories, CancellationToken cancellationToken)
        {
            foreach (var repo in repositories)
            {
                var metadataResource = await repo.GetResourceAsync<PackageMetadataResource>(CancellationToken.None);
                var metadataList = await metadataResource.GetMetadataAsync(packageId, true, true, NativeLogger, cancellationToken);
                foreach (var metadata in metadataList)
                {
                    resultList.Add(new NugetServerPackage(metadata, repo.PackageSource.Source));
                }
            }
        }

        /// <summary>
        /// Look for available packages from source containing <paramref name="searchTerm"/> in either the Id or description of the package.
        /// </summary>
        /// <param name="searchTerm">Term used for search.</param>
        /// <param name="allowPrereleaseVersions">Are we looking in pre-release versions too?</param>
        /// <returns>A list of packages matching <paramref name="searchTerm"/>.</returns>
        public async Task<IQueryable<NugetPackage>> SourceSearch(string searchTerm, bool allowPrereleaseVersions)
        {
            var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();
            var res = new List<NugetPackage>();
            foreach (var repo in repositories)
            {
                var searchResource = await repo.GetResourceAsync<PackageSearchResource>(CancellationToken.None);

                if (searchResource != null)
                {
                    var searchResults = await searchResource.SearchAsync(searchTerm, new SearchFilter(includePrerelease: false), 0, 0, NativeLogger, CancellationToken.None);

                    if (searchResults != null)
                    {
                        var packages = searchResults.ToArray();

                        foreach (var package in packages)
                        {
                            res.Add(new NugetServerPackage(package, repo.PackageSource.Source));
                        }
                    }
                }
            }
            return res.AsQueryable();
        }

        /// <summary>
        /// Returns updates for packages from the repository 
        /// </summary>
        /// <param name="packageName">Package to look for updates</param>
        /// <param name="includePrerelease">Indicates whether to consider prerelease updates.</param>
        /// <param name="includeAllVersions">Indicates whether to include all versions of an update as opposed to only including the latest version.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns></returns>
        public async Task<IEnumerable<NugetPackage>> GetUpdates(PackageName packageName, bool includePrerelease, bool includeAllVersions, CancellationToken cancellationToken)
        {
            var resolutionContext = new ResolutionContext(
               DependencyBehavior.Lowest,
               includePrerelease,
               true,
               includeAllVersions ? VersionConstraints.None : VersionConstraints.ExactMajor | VersionConstraints.ExactMinor);

            var repositories = PackageSources.Select(sourceRepositoryProvider.CreateRepository).ToArray();

            var res = new List<NugetPackage>();
            var foundPackage = await NuGetPackageManager.GetLatestVersionAsync(packageName.Id, NuGetFramework.AgnosticFramework, resolutionContext, repositories, NativeLogger, cancellationToken);
            if (packageName.Version.ToNuGetVersion() <= foundPackage.LatestVersion)
            {
                foreach (var repo in repositories)
                {
                    var metadataResource = await repo.GetResourceAsync<PackageMetadataResource>(cancellationToken);
                    var metadataList = await metadataResource.GetMetadataAsync(packageName.Id, includePrerelease, includeAllVersions, NativeLogger, cancellationToken);
                    foreach (var metadata in metadataList)
                    {
                        res.Add(new NugetServerPackage(metadata, repo.PackageSource.Source));
                    }
                }
            }
            return res;
        }
#endregion


        /// <summary>
        /// Clean all temporary files created thus far during store operations.
        /// </summary>
        public void PurgeCache()
        {
        }

        public string GetRealPath(NugetLocalPackage package)
        {
            if (IsDevRedirectPackage(package))
            {
                var realPath = File.ReadAllText(GetRedirectFile(package));
                if (!Directory.Exists(realPath))
                    throw new DirectoryNotFoundException();
                return realPath;
            }

            return package.Path;
        }

        public string GetRedirectFile(NugetLocalPackage package)
        {
            return Path.Combine(package.Path, $"{package.Id}.redirect");
        }

        public bool IsDevRedirectPackage(NugetLocalPackage package)
        {
            return File.Exists(GetRedirectFile(package));
        }

        private void OnPackageInstalling(object sender, PackageOperationEventArgs args)
        {
            NugetPackageInstalling?.Invoke(sender, args);
        }

        private void OnPackageInstalled(object sender, PackageOperationEventArgs args)
        {
            var packageInstallPath = Path.Combine(args.InstallPath, "tools\\packageinstall.exe");
            if (File.Exists(packageInstallPath))
            {
                RunPackageInstall(packageInstallPath, "/install", currentProgressReport);
            }

            NugetPackageInstalled?.Invoke(sender, args);
        }

        private void OnPackageUninstalling(object sender, PackageOperationEventArgs args)
        {
            NugetPackageUninstalling?.Invoke(sender, args);

            try
            {
                var packageInstallPath = Path.Combine(args.InstallPath, "tools\\packageinstall.exe");
                if (File.Exists(packageInstallPath))
                {
                    RunPackageInstall(packageInstallPath, "/uninstall", currentProgressReport);
                }
            }
            catch (Exception)
            {
                // We mute errors during uninstall since they are usually non-fatal (OTOH, if we don't catch the exception, the NuGet package isn't uninstalled, which is probably not what we want)
                // If we really wanted to deal with them at some point, we should use another mechanism than exception (i.e. log)
            }
        }

        private void OnPackageUninstalled(object sender, PackageOperationEventArgs args)
        {
            NugetPackageUninstalled?.Invoke(sender, args);
        }

        void INugetDownloadProgress.DownloadProgress(long contentPosition, long contentLength)
        {
            currentProgressReport?.UpdateProgress(ProgressAction.Download, (int)(contentPosition * 100 / contentLength));
        }

        private static void RunPackageInstall(string packageInstall, string arguments, ProgressReport progress)
        {
            // Run packageinstall.exe
            using (var process = Process.Start(new ProcessStartInfo(packageInstall, arguments)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = Path.GetDirectoryName(packageInstall),
            }))
            {
                if (process == null)
                    throw new InvalidOperationException($"Could not start install package process [{packageInstall}] with options {arguments}");

                var errorOutput = new StringBuilder();

                process.OutputDataReceived += (_, args) =>
                {
                    // Report progress
                    if (progress != null && !string.IsNullOrEmpty(args.Data))
                    {
                        var matches = powerShellProgressRegex.Match(args.Data);
                        int percentageResult;
                        if (matches.Success && int.TryParse(matches.Groups[1].Value, out percentageResult))
                        {
                            progress.UpdateProgress(ProgressAction.Install, percentageResult);
                        }
                    }
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (!string.IsNullOrEmpty(args.Data))
                    {
                        // Save errors
                        lock (process)
                        {
                            errorOutput.AppendLine(args.Data);
                        }
                    }
                };

                // Process output and wait for exit
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                // Check exit code
                var exitCode = process.ExitCode;
                if (exitCode != 0)
                {
                    throw new InvalidOperationException($"Error code {exitCode} while running install package process [{packageInstall}]\n\n" + errorOutput);
                }
            }
        }

        private class PackagePathResolverV3 : PackagePathResolver
        {
            private VersionFolderPathResolver pathResolver;

            public PackagePathResolverV3(string rootDirectory) : base(rootDirectory, true)
            {
                pathResolver = new VersionFolderPathResolver(rootDirectory);
            }

            public override string GetPackageDirectoryName(PackageIdentity packageIdentity)
            {
                return pathResolver.GetPackageDirectory(packageIdentity.Id, packageIdentity.Version);
            }

            public override string GetPackageFileName(PackageIdentity packageIdentity)
            {
                return pathResolver.GetPackageFileName(packageIdentity.Id, packageIdentity.Version);
            }

            public override string GetInstallPath(PackageIdentity packageIdentity)
            {
                return pathResolver.GetInstallPath(packageIdentity.Id, packageIdentity.Version);
            }

            public override string GetInstalledPath(PackageIdentity packageIdentity)
            {
                var installPath = GetInstallPath(packageIdentity);
                var installPackagePath = GetInstalledPackageFilePath(packageIdentity);
                return File.Exists(installPackagePath) ? installPath : null;
            }

            public override string GetInstalledPackageFilePath(PackageIdentity packageIdentity)
            {
                var installPackagePath = GetInstalledPackageFilePath(packageIdentity);
                return File.Exists(installPackagePath) ? installPackagePath : null;
            }
        }
    }
}
