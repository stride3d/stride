// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Xenko.Core.Assets
{
    static class RestoreHelper
    {
        public static List<string> ListAssemblies(RestoreRequest request, RestoreResult result)
        {
            var assemblies = new List<string>();
            foreach (var library in result.LockFile.Libraries)
            {
                // Try several known path (note: order matters)
                // TODO: Create a real sort
                foreach (var startPattern in new[] { "runtimes/win-d3d11/lib/net4", "runtimes/win/lib/net4", "runtimes/any/lib/net4", "lib/net4", "lib/net35", "runtimes/win-d3d11/lib/netstandard2.", "runtimes/win/lib/netstandard2.", "runtimes/any/lib/netstandard2.", "lib/netstandard2.", "lib/netstandard1.", "lib/net10" })
                {
                    foreach (var file in library.Files)
                    {
                        var extension = Path.GetExtension(file).ToLowerInvariant();
                        // Try several known path (note: order matters)
                        if (file.StartsWith(startPattern, StringComparison.InvariantCultureIgnoreCase)
                            && (extension == ".dll" || extension == ".exe"))
                        {
                            assemblies.Add(Path.Combine(request.DependencyProviders.GlobalPackages.RepositoryRoot, library.Path, file));
                        }
                    }
                }
            }

            return assemblies;
        }

        public static async Task<(RestoreRequest, RestoreResult)> Restore(ILogger logger, string packageName, VersionRange versionRange)
        {
            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(null);

            var packageSourceProvider = new PackageSourceProvider(settings);

            // not sure what these do, but it was in the NuGet command line.
            var resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            resourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Setup source provider as a V3 only.
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, resourceProviders);

            var installPath = SettingsUtility.GetGlobalPackagesFolder(settings);
            var assemblies = new List<string>();

            var projectPath = Path.Combine("XenkoNugetResolver.json");
            var spec = new PackageSpec()
            {
                Name = Path.GetFileNameWithoutExtension(projectPath), // make sure this package never collides with a dependency
                FilePath = projectPath,
                Dependencies = new List<LibraryDependency>()
                {
                    new LibraryDependency
                    {
                        LibraryRange = new LibraryRange(packageName, versionRange, LibraryDependencyTarget.Package),
                    }
                },
                TargetFrameworks =
                {
                    new TargetFrameworkInformation
                    {
                        FrameworkName = NuGetFramework.Parse("net472"),
                    }
                },
                RestoreMetadata = new ProjectRestoreMetadata
                {
                    ProjectPath = projectPath,
                    ProjectName = Path.GetFileNameWithoutExtension(projectPath),
                    ProjectStyle = ProjectStyle.PackageReference,
                    ProjectUniqueName = projectPath,
                    OutputPath = Path.Combine(Path.GetTempPath(), $"XenkoNugetResolver-{packageName}-{versionRange.MinVersion.ToString()}"),
                    OriginalTargetFrameworks = new[] { "net472" },
                    ConfigFilePaths = settings.GetConfigFilePaths(),
                    PackagesPath = SettingsUtility.GetGlobalPackagesFolder(settings),
                    Sources = SettingsUtility.GetEnabledSources(settings).ToList(),
                    FallbackFolders = SettingsUtility.GetFallbackPackageFolders(settings).ToList()
                },
            };

            using (var context = new SourceCacheContext())
            {
                context.IgnoreFailedSources = true;

                var dependencyGraphSpec = new DependencyGraphSpec();

                dependencyGraphSpec.AddProject(spec);

                dependencyGraphSpec.AddRestore(spec.RestoreMetadata.ProjectUniqueName);

                IPreLoadedRestoreRequestProvider requestProvider = new DependencyGraphSpecRequestProvider(new RestoreCommandProvidersCache(), dependencyGraphSpec);

                var restoreArgs = new RestoreArgs
                {
                    AllowNoOp = true,
                    CacheContext = context,
                    CachingSourceProvider = new CachingSourceProvider(new PackageSourceProvider(settings)),
                    Log = logger,
                };

                // Create requests from the arguments
                var requests = requestProvider.CreateRequests(restoreArgs).Result;

                // Restore the packages
                for (int tryCount = 0; tryCount < 2; ++tryCount)
                {
                    try
                    {
                        var results = await RestoreRunner.RunWithoutCommit(requests, restoreArgs);

                        // Commit results so that noop cache works next time
                        foreach (var result in results)
                        {
                            await result.Result.CommitAsync(logger, CancellationToken.None);
                        }
                        var mainResult = results.First();
                        return (mainResult.SummaryRequest.Request, mainResult.Result);
                    }
                    catch (Exception e) when (e is UnauthorizedAccessException || e is IOException)
                    {
                        // If we have an unauthorized access exception, it means assemblies are locked by running Xenko process
                        // During first try, kill some known harmless processes, and try again
                        if (tryCount == 1)
                            throw;

                        foreach (var process in new[] { "Xenko.ConnectionRouter" }.SelectMany(Process.GetProcessesByName))
                        {
                            try
                            {
                                if (process.Id != Process.GetCurrentProcess().Id)
                                {
                                    process.Kill();
                                    process.WaitForExit();
                                }
                            }
                            catch (Exception)
                            {
                            }
                        }
                    }
                }

                throw new InvalidOperationException("Unreachable code");
            }
        }
    }
}
