// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using Microsoft.Build.Construction;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets
{
    public partial class StridePackageUpgrader
    {
        // 4.5.5: macOS is osx-arm64 only, the packages have no osx-x64 native libraries. Before 4.5, Game Studio made
        // macOS projects with <RuntimeIdentifier>osx-x64</RuntimeIdentifier>.
        internal static void UpgradeMacOSToArm64(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var changed = false;

            foreach (var element in project.Xml.AllChildren)
            {
                // Choose, Otherwise and a few others have no condition (null)
                if (!string.IsNullOrEmpty(element.Condition))
                    Set(element.Condition, element.Condition.Replace("'osx-x64'", "'osx-arm64'", StringComparison.OrdinalIgnoreCase), value => element.Condition = value);

                switch (element)
                {
                    case ProjectPropertyElement property when property.Name.Equals("RuntimeIdentifier", StringComparison.OrdinalIgnoreCase)
                        || property.Name.Equals("RuntimeIdentifiers", StringComparison.OrdinalIgnoreCase):
                        var runtimeIdentifiers = property.Value.Split(';').Select(x => x.Trim() == "osx-x64" ? "osx-arm64" : x).DistinctBy(x => x.Trim(), StringComparer.Ordinal);
                        Set(property.Value, string.Join(";", runtimeIdentifiers), value => property.Value = value);
                        break;
                    case ProjectPropertyElement property:
                        Set(property.Value, ToArm64RuntimesPath(property.Value), value => property.Value = value);
                        break;
                    case ProjectItemElement item:
                        Set(item.Include, ToArm64RuntimesPath(item.Include), value => item.Include = value);
                        Set(item.Update, ToArm64RuntimesPath(item.Update), value => item.Update = value);
                        Set(item.Remove, ToArm64RuntimesPath(item.Remove), value => item.Remove = value);
                        break;
                    case ProjectMetadataElement metadata:
                        Set(metadata.Value, ToArm64RuntimesPath(metadata.Value), value => metadata.Value = value);
                        break;
                }
            }

            if (changed)
            {
                context.IsDirty = true;
                context.Log.Info($"[{context.ProjectFullPath.GetFileName()}] Changed osx-x64 to osx-arm64: macOS is osx-arm64 only now.");
            }
            else if ((project.GetPropertyValue("RuntimeIdentifier") + ";" + project.GetPropertyValue("RuntimeIdentifiers")).Split(';').Any(x => x.Trim() == "osx-x64"))
            {
                // Nothing in the project file: it comes from an imported file (Directory.Build.props)
                context.Log.Warning($"[{context.ProjectFullPath.GetFileName()}] gets the osx-x64 runtime identifier from an imported file: macOS is osx-arm64 only now, change it there.");
            }

            void Set(string oldValue, string newValue, Action<string> set)
            {
                if (oldValue == newValue)
                    return;
                set(newValue);
                changed = true;
            }
        }

        // runtimes\osx-x64 and runtimes/osx-x64, in paths such as runtimes\osx-x64\native\*.dylib
        private static string ToArm64RuntimesPath(string value)
            => value.Replace(@"runtimes\osx-x64", @"runtimes\osx-arm64", StringComparison.OrdinalIgnoreCase)
                .Replace("runtimes/osx-x64", "runtimes/osx-arm64", StringComparison.OrdinalIgnoreCase);
    }
}
