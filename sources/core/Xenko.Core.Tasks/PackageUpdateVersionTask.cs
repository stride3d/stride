// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Xenko.Core;
using Xenko.Core.IO;

namespace Xenko.Core.Tasks
{
    public class PackageUpdateVersionTask : Task
    {
        /// <summary>
        /// Gets or sets the version file.
        /// </summary>
        /// <value>The version file.</value>
        [Required]
        public ITaskItem VersionFile { get; set; }

        /// <summary>
        /// The output file for the version information.
        /// </summary>
        [Required]
        public ITaskItem GeneratedVersionFile { get; set; }

        /// <summary>
        /// Gets or sets the root directory.
        /// </summary>
        [Required]
        public ITaskItem RootDirectory { get; set; }

        public string SpecialVersion { get; set; }

        public bool SpecialVersionGitHeight { get; set; }

        public bool SpecialVersionGitCommit { get; set; }

        public override bool Execute()
        {
            if (RootDirectory == null || !Directory.Exists(RootDirectory.ItemSpec))
            {
                Log.LogError("PackageFile is not set or doesn't exist");
                return false;
            }

            if (VersionFile == null || !File.Exists(VersionFile.ItemSpec))
            {
                Log.LogError("VersionFile is not set or doesn't exist");
                return false;
            }

            if (GeneratedVersionFile == null)
            {
                Log.LogError("OutputVersionFile is not set");
                return false;
            }

            var currentAssemblyLocation = Assembly.GetExecutingAssembly().Location;
            var mainPlatformDirectory = Path.GetFileName(Path.GetDirectoryName(currentAssemblyLocation));

            EnsureLibGit2UnmanagedInPath(mainPlatformDirectory);

            // Compute Git Height using Nerdbank.GitVersioning
            // For now we assume top level package directory is git folder
            try
            {
                var rootDirectory = RootDirectory.ItemSpec;

                NativeLibrary.PreloadLibrary("git2-1196807.dll");
                var repo = LibGit2Sharp.Repository.IsValid(rootDirectory) ? new LibGit2Sharp.Repository(rootDirectory) : null;
                if (repo == null)
                {
                    Log.LogError("Could not open Git repository");
                    return false;
                }

                // TODO: Right now we patch the VersionFile, but ideally we should make a copy and make the build system use it
                var versionFileData = File.ReadAllText(VersionFile.ItemSpec);

                // Patch AssemblyInformationalVersion
                var headCommitSha = repo.Head.Commits.FirstOrDefault()?.Sha;

                // Patch NuGetVersion
                var versionSuffix = SpecialVersion ?? string.Empty;
                if (SpecialVersionGitHeight)
                {
                    // Compute version based on Git info
                    var versionFileRelative = new UFile(VersionFile.ItemSpec).MakeRelative(rootDirectory).ToWindowsPath();
                    var height = Nerdbank.GitVersioning.GitExtensions.GetVersionHeight(repo, versionFileRelative);
                    versionSuffix += height.ToString("D5");
                }
                if (SpecialVersionGitCommit && headCommitSha != null)
                {
                    if (versionSuffix.Length > 0)
                        versionSuffix += "-";
                    versionSuffix += "g" + headCommitSha.Substring(0, 8);
                }

                // Prefix with dash (if non empty)
                if (versionSuffix.Length > 0)
                    versionSuffix = "-" + versionSuffix;

                // Replace NuGetVersionSuffix
                versionFileData = Regex.Replace(versionFileData, "NuGetVersionSuffix = (.*);", $"NuGetVersionSuffix = \"{versionSuffix}\";");

                var assemblyInformationalSuffix = "NuGetVersionSuffix";

                // Always include git commit (even if not part of NuGetVersionSuffix)
                if (!SpecialVersionGitCommit && headCommitSha != null)
                {
                    assemblyInformationalSuffix += $" + \"-g{headCommitSha.Substring(0, 8)}\"";
                }

                // Replace AssemblyInformationalSuffix
                versionFileData = Regex.Replace(versionFileData, "AssemblyInformationalSuffix = (.*);", $"AssemblyInformationalSuffix = {assemblyInformationalSuffix};");

                // Write back new file
                File.WriteAllText(GeneratedVersionFile.ItemSpec, versionFileData);

                return true;
            }
            catch (Exception e)
            {
                Log.LogWarning($"Could not determine version using git history: {e}", e);
                return false;
            }
        }

        private static void EnsureLibGit2UnmanagedInPath(string mainPlatformDirectory)
        {
            // On .NET Framework (on Windows), we find native binaries by adding them to our PATH.
            var libgit2Directory = Nerdbank.GitVersioning.GitExtensions.FindLibGit2NativeBinaries(mainPlatformDirectory);
            if (libgit2Directory != null)
            {
                string pathEnvVar = Environment.GetEnvironmentVariable("PATH");
                string[] searchPaths = pathEnvVar.Split(Path.PathSeparator);
                if (!searchPaths.Contains(libgit2Directory, StringComparer.OrdinalIgnoreCase))
                {
                    pathEnvVar += Path.PathSeparator + libgit2Directory;
                    Environment.SetEnvironmentVariable("PATH", pathEnvVar);
                }
            }
        }
    }
}
