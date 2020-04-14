// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using NShader;
using NuGet.Common;
using NuGet.Frameworks;
using NuGet.Versioning;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Packages;

namespace Stride.VisualStudio.Commands
{
    /// <summary>
    /// Proxies commands to real <see cref="IStrideCommands"/> implementation.
    /// </summary>
    public class StrideCommandsProxy : MarshalByRefObject
    {
        public static readonly PackageVersion MinimumVersion = new PackageVersion(1, 4, 0, 0);

        public struct PackageInfo
        {
            public List<string> SdkPaths;

            public PackageVersion ExpectedVersion;

            public PackageVersion LoadedVersion;
        }

        private static readonly object computedPackageInfoLock = new object();
        private static PackageInfo computedPackageInfo;
        private static string solution;
        private static bool solutionChanged;

        private static readonly object commandProxyLock = new object();
        private static StrideCommandsProxy currentInstance;
        private static AppDomain currentAppDomain;

        private readonly IStrideCommands remote;
        private readonly List<Tuple<string, DateTime>> assembliesLoaded = new List<Tuple<string, DateTime>>();

        public static PackageInfo CurrentPackageInfo
        {
            get { lock (computedPackageInfoLock) { return computedPackageInfo; } }
        }

        static StrideCommandsProxy()
        {
            // This assembly resolve is only used to resolve the GetExecutingAssembly on the Default Domain
            // when casting to StrideCommandsProxy in the StrideCommandsProxy.GetProxy method
            AppDomain.CurrentDomain.AssemblyResolve += DefaultDomainAssemblyResolve;
        }

        public StrideCommandsProxy()
        {
            AppDomain.CurrentDomain.AssemblyResolve += StrideDomainAssemblyResolve;

            var assembly = Assembly.Load("Stride.VisualStudio.Commands");
            remote = (IStrideCommands)assembly.CreateInstance("Stride.VisualStudio.Commands.StrideCommands");
        }

        /// <summary>
        /// Set the solution to use, when resolving the package containing the remote commands.
        /// </summary>
        /// <param name="solutionPath">The full path to the solution file.</param>
        /// <param name="domain">The AppDomain to set the solution on, or null the current AppDomain.</param>
        public static void InitializeFromSolution(string solutionPath, PackageInfo stridePackageInfo, AppDomain domain = null)
        {
            if (domain == null)
            {
                lock (computedPackageInfoLock)
                {
                    // Set the new solution and clear the package info, so it will be recomputed
                    solution = solutionPath;
                    computedPackageInfo = stridePackageInfo;
                }

                lock (commandProxyLock)
                {
                    solutionChanged = true;
                }
            }
            else
            {
                var initializationHelper = (InitializationHelper)domain.CreateInstanceFromAndUnwrap(typeof(InitializationHelper).Assembly.Location, typeof(InitializationHelper).FullName);
                initializationHelper.Initialize(solutionPath, stridePackageInfo.SdkPaths, stridePackageInfo.ExpectedVersion?.ToString(), stridePackageInfo.LoadedVersion?.ToString());
            }
        }

        private class InitializationHelper : MarshalByRefObject
        {
            public void Initialize(string solutionPath, List<string> sdkPaths, string expectedVersion, string loadedVersion)
            {
                InitializeFromSolution(solutionPath, new PackageInfo
                {
                    SdkPaths = sdkPaths,
                    ExpectedVersion = expectedVersion != null ? new PackageVersion(expectedVersion) : null,
                    LoadedVersion = loadedVersion != null ? new PackageVersion(loadedVersion) : null,
                });
            }
        }

        public override object InitializeLifetimeService()
        {
            // See http://stackoverflow.com/questions/5275839/inter-appdomain-communication-problem
            // If this proxy is not used for 6 minutes, it is disconnected and calls to this proxy will fail
            // We return null to allow the service to run for the full live of the appdomain.
            return null;
        }

        /// <summary>
        /// Gets the current proxy.
        /// </summary>
        /// <returns>StrideCommandsProxy.</returns>
        public static StrideCommandsProxy GetProxy()
        {
            lock (commandProxyLock)
            {
                // New instance?
                bool shouldReload = currentInstance == null || solutionChanged;
                if (!shouldReload)
                {
                    // Assemblies changed?
                    shouldReload = currentInstance.ShouldReload();
                }

                // If new instance or assemblies changed, reload
                if (shouldReload)
                {
                    currentInstance = null;
                    if (currentAppDomain != null)
                    {
                        try
                        {
                            AppDomain.Unload(currentAppDomain);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Unexpected exception when unloading AppDomain for StrideCommandsProxy: {ex}");
                        }
                    }

                    var stridePackageInfo = FindStrideSdkDir(solution).Result;
                    if (stridePackageInfo.LoadedVersion == null)
                        return null;

                    currentAppDomain = CreateStrideDomain();
                    InitializeFromSolution(solution, stridePackageInfo, currentAppDomain);
                    currentInstance = CreateProxy(currentAppDomain);
                    currentInstance.Initialize();
                    solutionChanged = false;
                }

                return currentInstance;
            }
        }

        /// <summary>
        /// Creates the stride domain.
        /// </summary>
        /// <returns>AppDomain.</returns>
        public static AppDomain CreateStrideDomain()
        {
            return AppDomain.CreateDomain("stride-domain");
        }

        /// <summary>
        /// Gets the current proxy.
        /// </summary>
        /// <returns>StrideCommandsProxy.</returns>
        public static StrideCommandsProxy CreateProxy(AppDomain domain)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));
            return (StrideCommandsProxy)domain.CreateInstanceFromAndUnwrap(typeof(StrideCommandsProxy).Assembly.Location, typeof(StrideCommandsProxy).FullName);
        }

        public void Initialize()
        {
            remote.Initialize(null);
        }

        public bool ShouldReload()
        {
            lock (assembliesLoaded)
            {
                // Check if any assemblies have changed since loaded
                foreach (var assemblyItem in assembliesLoaded)
                {
                    var assemblyPath = assemblyItem.Item1;
                    var lastAssemblyTime = assemblyItem.Item2;

                    if (File.Exists(assemblyPath))
                    {
                        var fileDateTime = File.GetLastWriteTime(assemblyPath);
                        if (fileDateTime != lastAssemblyTime)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void StartRemoteBuildLogServer(BuildMonitorCallback buildMonitorCallback, string logPipeUrl)
        {
            remote.StartRemoteBuildLogServer(buildMonitorCallback, logPipeUrl);
        }

        public byte[] GenerateShaderKeys(string inputFileName, string inputFileContent)
        {
            return remote.GenerateShaderKeys(inputFileName, inputFileContent);
        }

        public RawShaderNavigationResult AnalyzeAndGoToDefinition(string projectPath, string sourceCode, RawSourceSpan span)
        {

            // TODO: We need to know which package is currently selected in order to query all valid shaders
            if (remote is IStrideCommands2 remote2)
                return remote2.AnalyzeAndGoToDefinition(projectPath, sourceCode, span);
            return remote.AnalyzeAndGoToDefinition(sourceCode, span);
        }

        private static Assembly DefaultDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // This assembly resolve is only used to resolve the GetExecutingAssembly on the Default Domain
            // when casting to StrideCommandsProxy in the StrideCommandsProxy.GetProxy method
            var executingAssembly = Assembly.GetExecutingAssembly();

            // Redirect requests for earlier package versions to the current one
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name == executingAssembly.GetName().Name)
                return executingAssembly;

            return null;
        }

        private Assembly StrideDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            // Necessary to avoid conflicts with Visual Studio NuGet
            if (args.Name.StartsWith("NuGet", StringComparison.InvariantCultureIgnoreCase))
                return Assembly.Load(assemblyName);

            var assemblyPath = computedPackageInfo.SdkPaths.FirstOrDefault(x => Path.GetFileNameWithoutExtension(x) == assemblyName.Name);
            if (assemblyPath != null)
            {
                return LoadAssembly(assemblyPath);
            }

            // PCL System assemblies are using version 2.0.5.0 while we have a 4.0
            // Redirect the PCL to use the 4.0 from the current app domain.
            if (assemblyName.Name.StartsWith("System") && (assemblyName.Flags & AssemblyNameFlags.Retargetable) != 0)
            {
                var systemCoreAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(assembly => assembly.GetName().Name == assemblyName.Name);
                return systemCoreAssembly;
            }

            return null;
        }

        private Assembly LoadAssembly(string assemblyFile)
        {
            lock (assembliesLoaded)
            {
                assembliesLoaded.Add(new Tuple<string, DateTime>(assemblyFile, File.GetLastWriteTime(assemblyFile)));
            }

            // Check if .pdb exists as well
            var pdbFile = Path.ChangeExtension(assemblyFile, "pdb");
            if (File.Exists(pdbFile))
                return Assembly.Load(File.ReadAllBytes(assemblyFile), File.ReadAllBytes(pdbFile));

            // Otherwise load assembly without PDB
            return Assembly.Load(File.ReadAllBytes(assemblyFile));
        }

        /// <summary>
        /// Gets the stride SDK dir.
        /// </summary>
        /// <returns></returns>
        internal static async Task<PackageInfo> FindStrideSdkDir(string solution, string packageName = "Stride.VisualStudio.Commands")
        {
            // Resolve the sdk version to load from the solution's package
            var packageInfo = new PackageInfo { ExpectedVersion = await PackageSessionHelper.GetPackageVersion(solution), SdkPaths = new List<string>() };

            // Check if we are in a root directory with store/packages facilities
            var store = new NugetStore(null);
            NugetLocalPackage stridePackage = null;

            // Try to find the package with the expected version
            if (packageInfo.ExpectedVersion != null && packageInfo.ExpectedVersion >= MinimumVersion)
            {
                // Stride up to 3.0
                if (packageInfo.ExpectedVersion < new PackageVersion(3, 1, 0, 0))
                {
                    stridePackage = store.GetPackagesInstalled(new[] { "Stride" }).FirstOrDefault(package => package.Version == packageInfo.ExpectedVersion);
                    if (stridePackage != null)
                    {
                        var strideSdkDir = store.GetRealPath(stridePackage);

                        packageInfo.LoadedVersion = stridePackage.Version;

                        foreach (var path in new[]
                        {
                            // Stride 2.x and 3.0
                            @"Bin\Windows\Direct3D11",
                            @"Bin\Windows",
                            // Stride 1.x
                            @"Bin\Windows-Direct3D11"
                        })
                        {
                            var fullPath = Path.Combine(strideSdkDir, path);
                            if (Directory.Exists(fullPath))
                            {
                                packageInfo.SdkPaths.AddRange(Directory.EnumerateFiles(fullPath, "*.dll", SearchOption.TopDirectoryOnly));
                                packageInfo.SdkPaths.AddRange(Directory.EnumerateFiles(fullPath, "*.exe", SearchOption.TopDirectoryOnly));
                            }
                        }
                    }
                }
                // Stride 3.1+
                else
                {
                    var logger = new Logger();
                    var (request, result) = await RestoreHelper.Restore(logger, NuGetFramework.ParseFrameworkName(".NETFramework,Version=v4.7.2", DefaultFrameworkNameProvider.Instance), "win", packageName, new VersionRange(packageInfo.ExpectedVersion.ToNuGetVersion()));
                    if (result.Success)
                    {
                        packageInfo.SdkPaths.AddRange(RestoreHelper.ListAssemblies(request, result));
                        packageInfo.LoadedVersion = packageInfo.ExpectedVersion;
                    }
                    else
                    {
                        MessageBox.Show( $"Could not restore {packageName} {packageInfo.ExpectedVersion}, this visual studio extension may fail to work properly without it."
                                         + $"To fix this you can either build {packageName} or pull the right version from nugget manually" );
                        throw new InvalidOperationException( $"Could not restore {packageName} {packageInfo.ExpectedVersion}." );
                    }
                }
            }

            return packageInfo;
        }

        public class Logger : ILogger
        {
            private object logLock = new object();
            public List<(LogLevel Level, string Message)> Logs { get; } = new List<(LogLevel, string)>();

            public void LogDebug(string data)
            {
                Log(LogLevel.Debug, data);
            }

            public void LogVerbose(string data)
            {
                Log(LogLevel.Verbose, data);
            }

            public void LogInformation(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogMinimal(string data)
            {
                Log(LogLevel.Minimal, data);
            }

            public void LogWarning(string data)
            {
                Log(LogLevel.Warning, data);
            }

            public void LogError(string data)
            {
                Log(LogLevel.Error, data);
            }

            public void LogInformationSummary(string data)
            {
                Log(LogLevel.Information, data);
            }

            public void LogErrorSummary(string data)
            {
                Log(LogLevel.Error, data);
            }

            public void Log(LogLevel level, string data)
            {
                lock (logLock)
                {
                    Debug.WriteLine($"[{level}] {data}");
                    Logs.Add((level, data));
                }
            }

            public Task LogAsync(LogLevel level, string data)
            {
                Log(level, data);
                return Task.CompletedTask;
            }

            public void Log(ILogMessage message)
            {
                Log(message.Level, message.Message);
            }

            public Task LogAsync(ILogMessage message)
            {
                Log(message);
                return Task.CompletedTask;
            }
        }
    }
}
