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

            var tasks = dependentPackage.Profiles
                .SelectMany(profile => profile.ProjectReferences)
                .Select(projectReference => UPath.Combine(dependentPackage.RootDirectory, projectReference.Location))
                .Distinct()
                .Select(projectFullPath => Task.Run(async () =>
                {
                    if (codeUpgrader.UpgradeProject(workspace, projectFullPath))
                    {
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
                    }
                }))
                .ToArray();

            Task.WaitAll(tasks);
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
            /// <returns><c>true</c> if <see cref="UpgradeSourceFile"/> should be called for each files in the project; otherwise <c>false</c></returns>
            bool UpgradeProject(MSBuildWorkspace workspace, UFile projectPath);

            /// <summary>
            /// Upgrades the specified file 
            /// </summary>
            /// <param name="syntaxTree">The syntaxtree of the file</param>
            /// <returns>An upgrade task</returns>
            Task UpgradeSourceFile(UFile filePath);
        }

        /// <summary>
        /// Code upgrader for renaming to Xenko
        /// </summary>
        private class RenameToXenkoCodeUpgrader : ICodeUpgrader
        {
            public bool UpgradeProject(MSBuildWorkspace workspace, UFile projectPath)
            {
                // Upgrade .csproj file
                // TODO: Use parsed file?
                var fileContents = File.ReadAllText(projectPath);
                var newFileContents = fileContents;

                // Rename namespaces
                newFileContents = RemoveSiliconStudioNamespaces(newFileContents);

                // Save file if there were any changes
                if (newFileContents != fileContents)
                {
                    File.WriteAllText(projectPath, newFileContents);
                }
                return true;
            }

            // TODO: Reverted to simple regex, to upgrade text in .pdxfx's generated code files. Should use syntax analysis again.
            public async Task UpgradeSourceFile(UFile filePath)
            {
                var fileContents = File.ReadAllText(filePath);
                var newFileContents = fileContents;

                // Rename namespaces
                newFileContents = RemoveSiliconStudioNamespaces(newFileContents);

                // Save file if there were any changes
                if (newFileContents != fileContents)
                {
                    File.WriteAllText(filePath, newFileContents);
                }
            }
        }
    }
}
