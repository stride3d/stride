// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using NuGet.Commands;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Frameworks;
using NuGet.LibraryModel;
using NuGet.ProjectModel;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Xenko.Core.Assets.CompilerApp
{
    class NuGetAssemblyResolver
    {
        [ModuleInitializer(-100000)]
        internal static void __Initialize__()
        {
            var logger = new Logger();
            var (request, result) = RestoreHelper.Restore(logger, Assembly.GetExecutingAssembly().GetName().Name, new VersionRange(new NuGetVersion(XenkoVersion.NuGetVersion))).Result;

            List<string> assemblies = RestoreHelper.ListAssemblies(request, result);

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
