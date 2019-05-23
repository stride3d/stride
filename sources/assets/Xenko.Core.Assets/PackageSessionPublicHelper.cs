// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading;
using Microsoft.Build.Locator;

namespace Xenko.Core.Assets
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
                MSBuildInstance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault(x => x.Version.Major >= 16);

                // Make sure it is not already loaded (otherwise MSBuildLocator.RegisterDefaults() throws an exception)
                if (MSBuildInstance != null && !AppDomain.CurrentDomain.GetAssemblies().Any(IsMSBuildAssembly))
                {
                    MSBuildLocator.RegisterInstance(MSBuildInstance);
                }
            }

            if (MSBuildInstance == null)
                throw new InvalidOperationException("Could not find a MSBuild installation (expected 16.0 or later)");

            CheckMSBuildToolset();
        }

        private static bool IsMSBuildAssembly(System.Reflection.Assembly assembly)
        {
            return IsMSBuildAssembly(assembly.GetName());
        }

        private static bool IsMSBuildAssembly(System.Reflection.AssemblyName assemblyName)
        {
            return s_msBuildAssemblies.Contains(assemblyName.Name, StringComparer.OrdinalIgnoreCase);
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
