// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Xenko.Core.Assets;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;

namespace Xenko.Assets
{
    public partial class XenkoPackageUpgrader
    {
        private static string RemoveSiliconStudioNamespaces(string content)
        {
            // Namespaces
            content = content.Replace("SiliconStudio.Xenko.Rendering.Composers", "SiliconStudio.Xenko.Rendering.Compositing");
            content = content.Replace("SiliconStudio.Core.Serialization.Assets", "SiliconStudio.Core.Serialization.Contents");
            content = content.Replace("SiliconStudio.Core", "Xenko.Core");
            content = content.Replace("SiliconStudio.Xenko", "Xenko");
            content = content.Replace("SiliconStudio.Common", "Xenko.Common");
            content = content.Replace("SiliconStudio.", "Xenko.Core.");
            content = content.Replace("SiliconStudioXenko", "Xenko");
            content = content.Replace("SiliconStudio", "Xenko");

            // Macros and defines
            content = content.Replace("SILICONSTUDIO_XENKO", "XENKO");
            content = content.Replace("SILICON_STUDIO_XENKO", "XENKO");
            content = content.Replace("SILICON_STUDIO_", "XENKO_");
            content = content.Replace("SILICONSTUDIO_", "XENKO_");

            return content;
        }

        private void UpgradeCode(Package dependentPackage, ILogger log, ICodeUpgrader codeUpgrader)
        {
            if (dependentPackage == null) throw new ArgumentNullException(nameof(dependentPackage));
            if (codeUpgrader == null) throw new ArgumentNullException(nameof(codeUpgrader));

            var csharpWorkspaceAssemblies = new[] { Assembly.Load("Microsoft.CodeAnalysis.Workspaces"), Assembly.Load("Microsoft.CodeAnalysis.CSharp.Workspaces"), Assembly.Load("Microsoft.CodeAnalysis.Workspaces.Desktop") };
            var workspace = MSBuildWorkspace.Create(ImmutableDictionary<string, string>.Empty, MefHostServices.Create(csharpWorkspaceAssemblies));

            var projectFullPath = (dependentPackage.Container as SolutionProject)?.FullPath;
            if (projectFullPath != null)
            {
                Task.Run(async () =>
                {
                    codeUpgrader.UpgradeProject(workspace, projectFullPath);

                    // Upgrade source code
                    var f = new FileInfo(projectFullPath.ToWindowsPath());
                    if (f.Exists)
                    {
                        var project = await workspace.OpenProjectAsync(f.FullName);
                        var subTasks = project.Documents.Concat(project.AdditionalDocuments).Select(x => codeUpgrader.UpgradeSourceFile(x.FilePath)).ToList();

                        await Task.WhenAll(subTasks);
                    }
                    else
                    {
                        log.Error($"Cannot locate project {f.FullName}.");
                    }
                }).Wait();
            }
        }

        /// <summary>
        /// Base interface for code upgrading
        /// </summary>
        private interface ICodeUpgrader
        {
            /// <summary>
            /// Upgrades the specified project file
            /// </summary>
            /// <param name="workspace">The msbuild workspace</param>
            /// <param name="projectPath">A path to a csproj file</param>
            void UpgradeProject(MSBuildWorkspace workspace, UFile projectPath);

            /// <summary>
            /// Upgrades the specified file 
            /// </summary>
            /// <param name="filePath">A path to a source file</param>
            Task UpgradeSourceFile(UFile filePath);
        }
    }
}
