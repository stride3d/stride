// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
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
                foreach (var startPattern in new[] { "runtimes/win7-d3d11/lib/net4", "runtimes/win/lib/net4", "lib/net4", "lib/net35", "lib/netstandard2.", "lib/netstandard1." })
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
            var settings = NuGet.Configuration.Settings.LoadDefaultSettings(Directory.GetCurrentDirectory());
            var packageSourceProvider = new PackageSourceProvider(settings);

            // not sure what these do, but it was in the NuGet command line.
            var resourceProviders = new List<Lazy<INuGetResourceProvider>>();
            resourceProviders.AddRange(Repository.Provider.GetCoreV3());

            // Setup source provider as a V3 only.
            var sourceRepositoryProvider = new SourceRepositoryProvider(settings, resourceProviders);

            var installPath = SettingsUtility.GetGlobalPackagesFolder(settings);
            var assemblies = new List<string>();

            var specPath = Path.Combine("TestProject", "project.json");
            var spec = new PackageSpec()
            {
                Name = "TestProject", // make sure this package never collides with a dependency
                FilePath = specPath,
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
            };

            using (var context = new SourceCacheContext())
            {
                context.IgnoreFailedSources = true;

                var provider = RestoreCommandProviders.Create(installPath, new List<string>(), sourceRepositoryProvider.GetRepositories(), context, new LocalPackageFileCache(), logger);
                var request = new RestoreRequest(spec, provider, context, logger)
                {
                    LockFilePath = "project.lock.json",
                    //RequestedRuntimes = { "win7-d3d11" },
                    ProjectStyle = ProjectStyle.DotnetCliTool,
                };

                var command = new RestoreCommand(request);

                // Act
                var result = await command.ExecuteAsync();
                await result.CommitAsync(logger, CancellationToken.None);

                return (request, result);
            }
        }
    }
}
