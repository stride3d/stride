// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using System;
using System.IO;
using NuGet.Frameworks;

namespace Stride.Core.Assets
{
    /// <summary>
    ///   The folders where <see cref="RestoreHelper"/> keeps the restore of a package (its project.assets.json and the
    ///   no-op cache), one per package, version, framework and runtime.
    /// </summary>
    /// <remarks>
    ///   Also compiled in Stride.Core.Packages, which deletes the folders of a package it uninstalls: a dev version is
    ///   built again under the same version, and a later restore would otherwise reuse the old graph as a no-op.
    /// </remarks>
    internal static class NuGetResolverCache
    {
        /// <summary>
        ///   Gets the folder that holds the restore folders. Per-user, not the shared temp dir: the output drives
        ///   assembly loading and its name is predictable (pre-seed risk).
        /// </summary>
        public static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "stride", "nugetresolver");

        /// <summary>
        ///   Gets the restore folder of a package.
        /// </summary>
        public static string GetFolder(string packageName, string version, string framework, string runtimeIdentifier)
            => Path.Combine(Root, $"{packageName}-{version}-{framework}-{runtimeIdentifier}");

        /// <summary>
        ///   Deletes the restore folders of a package version, for any framework and runtime. Best effort: a folder in
        ///   use stays.
        /// </summary>
        /// <returns>The number of folders deleted.</returns>
        public static int Delete(string packageName, string version, string? root = null)
        {
            root ??= Root;
            var deleted = 0;
            try
            {
                if (!Directory.Exists(root))
                    return 0;

                foreach (var folder in Directory.EnumerateDirectories(root, $"{packageName}-{version}-*", new EnumerationOptions { MatchCasing = MatchCasing.CaseInsensitive }))
                {
                    if (!IsFolderOf(Path.GetFileName(folder), packageName, version))
                        continue;
                    try
                    {
                        Directory.Delete(folder, true);
                        ++deleted;
                    }
                    catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                    {
                        // In use by a restore that is running: it is written again anyway
                    }
                }
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                // Unreadable folder: nothing to delete
            }
            return deleted;
        }

        /// <summary>
        ///   Gets whether <paramref name="folderName"/> is the restore folder of this package version: the part after
        ///   the version starts with a framework, so that 4.5.0 doesn't take the folders of 4.5.0-dev.
        /// </summary>
        internal static bool IsFolderOf(string folderName, string packageName, string version)
        {
            var prefix = $"{packageName}-{version}-";
            if (!folderName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var rest = folderName.Substring(prefix.Length);
            var separator = rest.IndexOf('-');
            var framework = separator >= 0 ? rest.Substring(0, separator) : rest;
            return !NuGetFramework.ParseFolder(framework).IsUnsupported;
        }
    }
}
