// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using NShader;
using Xenko.Core.Assets;
using Xenko.Core.Packages;

namespace Xenko.VisualStudio.Commands
{
    /// <summary>
    /// Proxies commands to real <see cref="IXenkoCommands"/> implementation.
    /// </summary>
    public class XenkoCommandsProxy : MarshalByRefObject
    {
        public static readonly Version MinimumVersion = new Version(1, 4);

        public struct PackageInfo
        {
            public string StorePath;
            public string SdkPath;

            public Version ExpectedVersion;

            public Version LoadedVersion;
        }

        private static readonly object computedPackageInfoLock = new object();
        private static PackageInfo computedPackageInfo;
        private static string solution;
        private static bool solutionChanged;

        private static readonly object commandProxyLock = new object();
        private static XenkoCommandsProxy currentInstance;
        private static AppDomain currentAppDomain;

        private readonly IXenkoCommands remote;
        private readonly List<Tuple<string, DateTime>> assembliesLoaded = new List<Tuple<string, DateTime>>();

        static XenkoCommandsProxy()
        {
            // This assembly resolve is only used to resolve the GetExecutingAssembly on the Default Domain
            // when casting to XenkoCommandsProxy in the XenkoCommandsProxy.GetProxy method
            AppDomain.CurrentDomain.AssemblyResolve += DefaultDomainAssemblyResolve;
        }

        public XenkoCommandsProxy()
        {
            AppDomain.CurrentDomain.AssemblyResolve += XenkoDomainAssemblyResolve;

            var assembly = Assembly.Load("Xenko.VisualStudio.Commands");
            remote = (IXenkoCommands)assembly.CreateInstance("Xenko.VisualStudio.Commands.XenkoCommands");
        }

        public static PackageInfo CurrentPackageInfo
        {
            get
            {
                lock (computedPackageInfoLock)
                {
                    if (computedPackageInfo.SdkPath == null)
                        computedPackageInfo = FindXenkoSdkDir();

                    return computedPackageInfo;
                }
            }
        }

        /// <summary>
        /// Set the solution to use, when resolving the package containing the remote commands.
        /// </summary>
        /// <param name="solutionPath">The full path to the solution file.</param>
        /// <param name="domain">The AppDomain to set the solution on, or null the current AppDomain.</param>
        public static void InitialzeFromSolution(string solutionPath, AppDomain domain = null)
        {
            if (domain == null)
            {
                lock (computedPackageInfoLock)
                {
                    // Set the new solution and clear the package info, so it will be recomputed
                    solution = solutionPath;
                    computedPackageInfo = new PackageInfo();
                }

                lock (commandProxyLock)
                {
                    solutionChanged = true;
                }
            }
            else
            {
                var initializationHelper = (InitializationHelper)domain.CreateInstanceFromAndUnwrap(typeof(InitializationHelper).Assembly.Location, typeof(InitializationHelper).FullName);
                initializationHelper.Initialze(solutionPath);
            }
        }

        private class InitializationHelper : MarshalByRefObject
        {
            public void Initialze(string solutionPath)
            {
                InitialzeFromSolution(solutionPath);
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
        /// <returns>XenkoCommandsProxy.</returns>
        public static XenkoCommandsProxy GetProxy()
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
                            Trace.WriteLine($"Unexpected exception when unloading AppDomain for XenkoCommandsProxy: {ex}");
                        }
                    }

                    currentAppDomain = CreateXenkoDomain();
                    InitialzeFromSolution(solution, currentAppDomain);
                    currentInstance = CreateProxy(currentAppDomain);
                    currentInstance.Initialize();
                    solutionChanged = false;
                }

                return currentInstance;
            }
        }

        /// <summary>
        /// Creates the xenko domain.
        /// </summary>
        /// <returns>AppDomain.</returns>
        public static AppDomain CreateXenkoDomain()
        {
            return AppDomain.CreateDomain("xenko-domain");
        }

        /// <summary>
        /// Gets the current proxy.
        /// </summary>
        /// <returns>XenkoCommandsProxy.</returns>
        public static XenkoCommandsProxy CreateProxy(AppDomain domain)
        {
            if (domain == null) throw new ArgumentNullException(nameof(domain));
            return (XenkoCommandsProxy)domain.CreateInstanceFromAndUnwrap(typeof(XenkoCommandsProxy).Assembly.Location, typeof(XenkoCommandsProxy).FullName);
        }

        public void Initialize()
        {
            remote.Initialize(CurrentPackageInfo.SdkPath);
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

        public RawShaderNavigationResult AnalyzeAndGoToDefinition(string sourceCode, RawSourceSpan span)
        {
            // TODO: We need to know which package is currently selected in order to query all valid shaders
            return remote.AnalyzeAndGoToDefinition(sourceCode, span);
        }

        private static Assembly DefaultDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            // This assembly resolve is only used to resolve the GetExecutingAssembly on the Default Domain
            // when casting to XenkoCommandsProxy in the XenkoCommandsProxy.GetProxy method
            var executingAssembly = Assembly.GetExecutingAssembly();

            // Redirect requests for earlier package versions to the current one
            var assemblyName = new AssemblyName(args.Name);
            if (assemblyName.Name == executingAssembly.GetName().Name)
                return executingAssembly;

            return null;
        }

        private Assembly XenkoDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            var xenkoSdkDir = CurrentPackageInfo.SdkPath;
            if (xenkoSdkDir == null)
                return null;

            var xenkoSdkBinDirectories = new[]
            {
                // Xenko 2.x
                Path.Combine(xenkoSdkDir, @"Bin\Windows\Direct3D11"),
                Path.Combine(xenkoSdkDir, @"Bin\Windows"),
                // Backward compatibility with Xenko 1.x
                Path.Combine(xenkoSdkDir, @"Bin\Windows-Direct3D11"),
            };

            var assemblyName = new AssemblyName(args.Name);

            foreach (var xenkoSdkBinDirectory in xenkoSdkBinDirectories)
            {
                // Try to load .dll/.exe from Xenko SDK directory
                var assemblyFile = Path.Combine(xenkoSdkBinDirectory, assemblyName.Name + ".dll");
                if (File.Exists(assemblyFile))
                    return LoadAssembly(assemblyFile);

                assemblyFile = Path.Combine(xenkoSdkBinDirectory, assemblyName.Name + ".exe");
                if (File.Exists(assemblyFile))
                    return LoadAssembly(assemblyFile);
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
        /// Gets the xenko SDK dir.
        /// </summary>
        /// <returns></returns>
        private static PackageInfo FindXenkoSdkDir()
        {
            // Resolve the sdk version to load from the solution's package
            var packageInfo = new PackageInfo { ExpectedVersion = PackageSessionHelper.GetPackageVersion(solution) };

            // TODO: Maybe move it in some common class somewhere? (in this case it would be included with "Add as link" in VSPackage)
            var xenkoSdkDir = Environment.GetEnvironmentVariable("XenkoDir");

            // Failed to locate xenko
            if (xenkoSdkDir == null)
                return packageInfo;

            // If we are in a dev directory, assume we have the right version
            if (File.Exists(Path.Combine(xenkoSdkDir, "build\\Xenko.sln")))
            {
                packageInfo.StorePath = xenkoSdkDir;
                packageInfo.SdkPath = xenkoSdkDir;
                packageInfo.LoadedVersion = packageInfo.ExpectedVersion;
                return packageInfo;
            }

            // Check if we are in a root directory with store/packages facilities
            var store = new NugetStore(xenkoSdkDir);
            NugetPackage xenkoPackage = null;

            // Try to find the package with the expected version
            if (packageInfo.ExpectedVersion != null && packageInfo.ExpectedVersion >= MinimumVersion)
                xenkoPackage = store.GetPackagesInstalled(store.MainPackageIds).FirstOrDefault(package => GetVersion(package) == packageInfo.ExpectedVersion);

            // If the expected version is not found, get the latest package
            if (xenkoPackage == null)
                xenkoPackage = store.GetLatestPackageInstalled(store.MainPackageIds);

            // If no package was found, return no sdk path
            if (xenkoPackage == null)
                return packageInfo;

            // Return the loaded version and the sdk path
            packageInfo.LoadedVersion = GetVersion(xenkoPackage);
            packageInfo.StorePath = xenkoSdkDir;
            packageInfo.SdkPath = store.GetInstalledPath(xenkoPackage.Id, xenkoPackage.Version);

            return packageInfo;
        }

        private static Version GetVersion(NugetPackage package)
        {
            var originalVersion = package.Version.Version;
            return new Version(originalVersion.Major, originalVersion.Minor);
        }
    }
}
