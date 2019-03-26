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
                // Make sure it is not already loaded (otherwise MSBuildLocator.RegisterDefaults() throws an exception)
                if (AppDomain.CurrentDomain.GetAssemblies().Any(IsMSBuildAssembly))
                {
                    MSBuildInstance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault();
                }
                else
                {
                    MSBuildInstance = MSBuildLocator.RegisterDefaults();
                }
            }

            if (MSBuildInstance == null)
                throw new InvalidOperationException("Could not find MSBuild Instance");

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
                if (!projectCollection.Toolsets.Any(x => new Version(x.ToolsVersion).Major >= 15))
                {
                    throw new InvalidOperationException("Could not find a supported MSBuild toolset version");
                }
            }
        }
    }
}
