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
        static List<string> SupportedRuntimes = new List<string>
        {
            "win",
            "any",
            "none",
        };
        static List<string> SupportedTfms = new List<string>
        {
            "net472",
            "net471",
            "net47",
            "net462",
            "net461",
            "net46",
            "net452",
            "net451",
            "net45",
            "net40",
            "net35",
            "net20",
            "net11",
            "net10",
            "netstandard2.0",
            "netstandard1.6",
            "netstandard1.5",
            "netstandard1.4",
            "netstandard1.3",
            "netstandard1.2",
            "netstandard1.1",
            "netstandard1.0",
        };
        struct SortedFile
        {
            public string Filename;
            public int Order;
        }
        public static List<string> ListAssemblies(RestoreRequest request, RestoreResult result)
        {
            var assemblies = new List<string>();
            foreach (var library in result.LockFile.Libraries)
            {
                var libraryFiles = library.Files
                    .Select(x =>
                        new SortedFile
                        {
                            Filename = x,
                            Order = ComputeFilePriority(x),
                        })
                    .Where(x => x.Order != -1)
                    .OrderBy(x => x.Order)
                    .ToList();
                foreach (var file in libraryFiles)
                {
                    var extension = Path.GetExtension(file.Filename).ToLowerInvariant();
                    // Try several known path (note: order matters)
                    if (extension == ".dll" || extension == ".exe")
                    {
                        assemblies.Add(Path.Combine(request.DependencyProviders.GlobalPackages.RepositoryRoot, library.Path, file.Filename));
                    }
                }
            }

            return assemblies;
        }

        static int ComputeFilePriority(string filename)
        {
            var libPosition = 0;
            var runtime = "none";

            var pathParts = filename.Split('/');
            if (pathParts.Length >= 2 && pathParts[0].ToLowerInvariant() == "runtimes")
            {
                libPosition = 2;
                runtime = pathParts[1].ToLowerInvariant();
            }
            var runtimeIndex = SupportedRuntimes.IndexOf(runtime);
            if (runtimeIndex == -1)
                return -1;

            if (!(libPosition + 1 < pathParts.Length // also includes TFM
                && pathParts[libPosition].ToLowerInvariant() == "lib"))
                return -1;

            var tfm = pathParts[libPosition + 1].ToLowerInvariant();
            var tfmIndex = SupportedTfms.IndexOf(tfm);
            if (tfmIndex == -1)
                return -1;

            return tfmIndex * 1000 + runtimeIndex;
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
