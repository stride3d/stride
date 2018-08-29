// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.VisualStudio;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// Helper class to load/save a VisualStudio solution.
    /// </summary>
    internal partial class PackageSessionHelper
    {
        internal static readonly string SolutionHeader = @"Microsoft Visual Studio Solution File, Format Version 12.00
# Visual Studio 14
VisualStudioVersion = {0}
MinimumVisualStudioVersion = {0}".ToFormat(PackageSession.DefaultVisualStudioVersion);

        public static bool IsPackageFile(string filePath)
        {
            return AssetRegistry.GetAssetTypeFromFileExtension(Path.GetExtension(filePath)) == typeof(Package);
        }

        public static void LoadSolution(PackageSession session, string filePath, List<string> packagePaths, ILogger sessionResult)
        {
            var solutionDirectory = Path.GetDirectoryName(filePath);
            if (solutionDirectory == null)
            {
                throw new ArgumentException("Must be absolute", "filePath");
            }

            // The session should save back its changes to the solution
            session.SolutionPath = filePath;

            var solution = Solution.FromFile(filePath);
            session.VSSolution = solution;

            foreach (var project in solution.Projects)
            {
                string packagePath;
                if (IsPackage(project, out packagePath))
                {
                    var packageFullPath = Path.Combine(solutionDirectory, packagePath);
                    packagePaths.Add(packageFullPath);
                }
            }

            var versionHeader = solution.Properties.FirstOrDefault(x=>x.Name == "VisualStudioVersion");
            Version version;
            if (versionHeader != null && Version.TryParse(versionHeader.Value, out version))
                session.VisualStudioVersion = version;
            else
                session.VisualStudioVersion = null;
        }
    }
}
