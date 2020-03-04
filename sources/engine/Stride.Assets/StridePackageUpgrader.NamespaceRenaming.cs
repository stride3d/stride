// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.MSBuild;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets
{
    public partial class StridePackageUpgrader
    {
        private void UpgradeStrideCode(Package dependentPackage, ILogger log)
        {
            if (dependentPackage == null) throw new ArgumentNullException(nameof(dependentPackage));

            var projectFullPath = (dependentPackage.Container as SolutionProject)?.FullPath;
            if (projectFullPath != null)
            {
                Task.Run(async () =>
                {
                    var allFiles = Directory.GetFiles(Path.GetDirectoryName(projectFullPath), "*.*", SearchOption.AllDirectories);
                    // Search for all source files in project directory
                    foreach (var extension in new[] { new { Extension = ".csproj", Type = XenkoToStrideRenameHelper.StrideContentType.Project }, new { Extension = ".cs", Type = XenkoToStrideRenameHelper.StrideContentType.Code } })
                    {
                        var files = allFiles.Where(file => file.ToLower().EndsWith(extension.Extension));
                        foreach (var file in files)
                        {
                            XenkoToStrideRenameHelper.RenameStrideFile(file, extension.Type);
                        }
                    }

                    /*var files = allFiles.Where(file => file.ToLower().EndsWith(extension.Extension));
                    foreach (var file in files)
                    {
                        var extension = Path.GetExtension(file).ToLower();
                        switch (extension)
                        {
                            case ".cs":
                                XenkoToStrideRenameHelper.RenameStrideFile(file, XenkoToStrideRenameHelper.StrideContentType.Code);
                                break;
                            case ".csproj":
                                XenkoToStrideRenameHelper.RenameStrideFile(file, XenkoToStrideRenameHelper.StrideContentType.Project);
                                break;
                            case ".xksl":
                            case ".xkfx":
                                XenkoToStrideRenameHelper.RenameStrideFile(file, XenkoToStrideRenameHelper.StrideContentType.Asset);
                                break;
                        }
                    }*/
                }).Wait();
            }
        }
    }
}
