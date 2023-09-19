// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading;
using Microsoft.Build.Locator;

namespace Stride.Core.Assets
{
    /// <summary>
    /// Helper class to load/save a VisualStudio solution.
    /// </summary>
    public static class PackageSessionPublicHelper
    {
        private static readonly string[] s_msBuildAssemblies =
        {
            "Microsoft.Build",
            "Microsoft.Build.Framework",
            "Microsoft.Build.Tasks.Core",
            "Microsoft.Build.Utilities.Core"
        };

        private static int MSBuildLocatorCount = 0;
        private static VisualStudioInstance MSBuildInstance;

        /// <summary>
        /// This method finds a compatible version of MSBuild.
        /// </summary>
        public static void FindAndSetMSBuildVersion()
        {
            // Note: this should be called only once
            if (MSBuildInstance == null && Interlocked.Increment(ref MSBuildLocatorCount) == 1)
            {
                // Detect either .NET Core SDK or Visual Studio depending on current runtime
                var isNETCore = !RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework");
                MSBuildInstance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault(x => isNETCore
                    ? x.DiscoveryType == DiscoveryType.DotNetSdk && x.Version.Major >= 3
                    : (x.DiscoveryType == DiscoveryType.VisualStudioSetup || x.DiscoveryType == DiscoveryType.DeveloperConsole) && x.Version.Major >= 16);
                
                if (MSBuildInstance == null)
                {
                    throw new InvalidOperationException("Could not find a MSBuild installation (expected 16.0 or later) " +
                        "Please ensure you have the .NET 6 SDK installed from Microsoft's website");
                }

                // Make sure it is not already loaded (otherwise MSBuildLocator.RegisterDefaults() throws an exception)
                if (!AppDomain.CurrentDomain.GetAssemblies().Any(IsMSBuildAssembly))
                {
                    // We can't use directly RegisterInstance because we want to avoid NuGet verison conflicts (between MSBuild/dotnet one and ours).
                    // More details at https://github.com/microsoft/MSBuildLocator/issues/127
                    // This code should be equivalent to MSBuildLocator.RegisterInstance(MSBuildInstance);
                    //  except that we load everything in another context.

                    ApplyDotNetSdkEnvironmentVariables(MSBuildInstance.MSBuildPath);

                    var msbuildAssemblyLoadContext = new AssemblyLoadContext("MSBuild");

                    AssemblyLoadContext.Default.Resolving += (assemblyLoadContext, assemblyName) =>
                    {
                        string path = Path.Combine(MSBuildInstance.MSBuildPath, assemblyName.Name + ".dll");
                        if (File.Exists(path))
                        {
                            return msbuildAssemblyLoadContext.LoadFromAssemblyPath(path);
                        }

                        return null;
                    };
                }
            }

            SetupMSBuildCurrentHostForOutOfProc(MSBuildInstance.MSBuildPath);
            CheckMSBuildToolset();

            // Reset MSBUILD_EXE_PATH once MSBuild is resolved, to not spook child process (had issues with ThisProcess(MSBuild)->CompilerApp(net472): CompilerApp couldn't load MSBuild project properly)
            Environment.SetEnvironmentVariable("MSBUILD_EXE_PATH", null);
        }

        // Function copied from MSBuildLocator.ApplyDotNetSdkEnvironmentVariables. Copyright (c) Microsoft. All rights reserved. Licensed under the MIT license.
        private static void ApplyDotNetSdkEnvironmentVariables(string dotNetSdkPath)
        {
            const string MSBUILD_EXE_PATH = nameof(MSBUILD_EXE_PATH);
            const string MSBuildExtensionsPath = nameof(MSBuildExtensionsPath);
            const string MSBuildSDKsPath = nameof(MSBuildSDKsPath);

            var variables = new Dictionary<string, string>
            {
                [MSBUILD_EXE_PATH] = Path.Combine(dotNetSdkPath, "MSBuild.dll"),
                [MSBuildExtensionsPath] = dotNetSdkPath,
                [MSBuildSDKsPath] = Path.Combine(dotNetSdkPath, "Sdks")
            };

            foreach (var kvp in variables)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }

        private static bool IsMSBuildAssembly(System.Reflection.Assembly assembly)
        {
            return IsMSBuildAssembly(assembly.GetName());
        }

        private static bool IsMSBuildAssembly(System.Reflection.AssemblyName assemblyName)
        {
            return s_msBuildAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase);
        }

        private static void SetupMSBuildCurrentHostForOutOfProc(string dotNetSdkPath)
        {
            // Workaround for https://github.com/dotnet/msbuild/pull/7013 (dotnet.exe not properly detected by MSBuild so it fallbacks to launching our own executable instead)
            var currentHostField = typeof(Microsoft.Build.Evaluation.Project).Assembly
                .GetType("Microsoft.Build.BackEnd.NodeProviderOutOfProcBase")?
                .GetField("CurrentHost", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (currentHostField != null)
            {
                currentHostField.SetValue(null, Path.Combine(new DirectoryInfo(dotNetSdkPath).Parent.Parent.FullName, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet"));
            }
        }

        private static void CheckMSBuildToolset()
        {
            // Check that we can create a project
            using (var projectCollection = new Microsoft.Build.Evaluation.ProjectCollection())
            {
                if (projectCollection.GetToolset("Current") == null) // VS 2019+ (https://github.com/Microsoft/msbuild/issues/3778)
                {
                    throw new InvalidOperationException("Could not find a supported MSBuild toolset version (expected 16.0 or later)");
                }
            }
        }
    }
}
