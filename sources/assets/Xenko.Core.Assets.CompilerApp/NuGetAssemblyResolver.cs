// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Xenko.Core.Assets.CompilerApp
{
    class NuGetAssemblyResolver
    {
        [ModuleInitializer(-100000)]
        internal static void __Initialize__()
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

            {
                var logger = new Logger();
                var configJson = JObject.Parse(@"
                {
                    ""dependencies"": {
                        ""Xenko.Core.Assets.CompilerApp"": ""3.1.0.1-dev""
                    },
                     ""frameworks"": {
                        ""net462"": { }
                    }
                }");

                var specPath = Path.Combine("TestProject", "project.json");
                var spec = JsonPackageSpecReader.GetPackageSpec(configJson.ToString(), "TestProject", specPath);

                using (var context = new SourceCacheContext())
                {
                    context.IgnoreFailedSources = true;

                    var provider = RestoreCommandProviders.Create(installPath, new List<string>(), sourceRepositoryProvider.GetRepositories(), context, new LocalPackageFileCache(), logger);
                    var request = new RestoreRequest(spec, provider, context, logger)
                    {
                        LockFilePath = "project.lock.json",
                    };

                    var command = new RestoreCommand(request);

                    Console.WriteLine("Restoring");
                    // Act
                    var result = command.ExecuteAsync().Result;
                    Console.WriteLine("Restoring done");

                    foreach (var library in result.LockFile.Libraries)
                    {
                        foreach (var file in library.Files)
                        {
                            if (file.StartsWith("lib/net"))
                            {
                                assemblies.Add(Path.Combine(installPath, library.Path, file));
                            }
                        }
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (sender, eventArgs) =>
            {
                var aname = new AssemblyName(eventArgs.Name);
                if (aname.Name.StartsWith("Microsoft.Build") && aname.Name != "Microsoft.Build.Locator")
                    return null;
                var assemblyPath = assemblies.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == aname.Name);
                if (assemblyPath != null)
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
                return null;
            };
        }

        public class Logger : ILogger
        {
            private List<string> logs = new List<string>();

            public void LogDebug(string data)
            {
                logs.Add(data);
            }

            public void LogVerbose(string data)
            {
                logs.Add(data);
            }

            public void LogInformation(string data)
            {
                Console.WriteLine(data);
                logs.Add(data);
            }

            public void LogMinimal(string data)
            {
                logs.Add(data);
            }

            public void LogWarning(string data)
            {
                logs.Add(data);
            }

            public void LogError(string data)
            {
                logs.Add(data);
            }

            public void LogInformationSummary(string data)
            {
                logs.Add(data);
            }

            public void LogErrorSummary(string data)
            {
                logs.Add(data);
            }

            public void Log(LogLevel level, string data)
            {
            }

            public Task LogAsync(LogLevel level, string data)
            {
                return Task.CompletedTask;
            }

            public void Log(ILogMessage message)
            {
            }

            public Task LogAsync(ILogMessage message)
            {
                return Task.CompletedTask;
            }
        }
    }
}
