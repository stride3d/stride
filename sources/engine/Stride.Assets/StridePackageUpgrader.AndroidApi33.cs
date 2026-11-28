// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Build.Construction;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Assets
{
    public partial class StridePackageUpgrader
    {
        private const int AndroidMinimumApiLevel = 33;

        // 4.5.7: Android 13 (API 33) is the minimum, the first version whose system Vulkan loader exposes Vulkan 1.3. The
        // manifest of Stride.Graphics fails the build of an app with a lower minimum. Before, Game Studio made Android
        // projects with a SupportedOSPlatformVersion of 21.
        internal static void UpgradeAndroidToApi33(ProjectUpgradeContext context)
        {
            var project = context.Project;
            var targetFrameworks = (project.GetPropertyValue("TargetFramework") + ";" + project.GetPropertyValue("TargetFrameworks"))
                .Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (!targetFrameworks.Any(IsAndroidFramework))
                return;

            // In a project of several platforms, only the properties set for Android are the Android minimum
            var isAndroidOnly = targetFrameworks.All(IsAndroidFramework);
            var changed = false;
            var properties = project.Xml.Properties.Where(x => x.Name.Equals("SupportedOSPlatformVersion", StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var property in properties)
            {
                if ((isAndroidOnly || IsSetForAndroid(property)) && IsBelowAndroidMinimum(property.Value))
                {
                    property.Value = AndroidMinimumApiLevel.ToString(CultureInfo.InvariantCulture);
                    changed = true;
                }
            }

            // Without one, the minimum is the .NET for Android default (21), or the value of an imported file
            if (isAndroidOnly && properties.Count == 0 && !IsAtLeastAndroidMinimum(project.GetPropertyValue("SupportedOSPlatformVersion")))
            {
                var propertyGroup = project.Xml.PropertyGroups.FirstOrDefault(x => string.IsNullOrEmpty(x.Condition)) ?? project.Xml.AddPropertyGroup();
                propertyGroup.AddProperty("SupportedOSPlatformVersion", AndroidMinimumApiLevel.ToString(CultureInfo.InvariantCulture));
                changed = true;
            }

            if (changed)
            {
                context.IsDirty = true;
                project.ReevaluateIfNecessary();
                context.Log.Info($"[{context.ProjectFullPath.GetFileName()}] Changed the minimum Android version to Android 13 (API 33): Stride requires it now.");
            }

            // An imported file that comes after the project file (Directory.Build.targets) sets a lower one
            if (isAndroidOnly && IsBelowAndroidMinimum(project.GetPropertyValue("SupportedOSPlatformVersion")))
                context.UnitFailed(context.ProjectFullPath.ToOSPath(), "the project gets its SupportedOSPlatformVersion from an imported file: Android 13 (API 33) is the minimum now, change it to 33 there.");
        }

        private static bool IsAndroidFramework(string targetFramework)
            => targetFramework.Contains("-android", StringComparison.OrdinalIgnoreCase);

        private static bool IsSetForAndroid(ProjectPropertyElement property)
            => property.AllParents.Prepend<ProjectElement>(property).Any(x => x.Condition?.Contains("android", StringComparison.OrdinalIgnoreCase) == true);

        // A value that is not a version, such as an MSBuild expression, is neither below nor above
        private static bool IsBelowAndroidMinimum(string value)
            => TryParseApiLevel(value, out var apiLevel) && apiLevel < AndroidMinimumApiLevel;

        private static bool IsAtLeastAndroidMinimum(string value)
            => TryParseApiLevel(value, out var apiLevel) && apiLevel >= AndroidMinimumApiLevel;

        // "21", or "21.0" as the .NET SDK writes it
        private static bool TryParseApiLevel(string value, out int apiLevel)
        {
            var major = value.Trim().Split('.')[0];
            return int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out apiLevel);
        }
    }
}
