// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Xenko.GitVersioning
{
    public class GenerateVersionFile : Task
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

        [Output]
        public string NuGetVersion { get; set; }

        public string NuGetVersionSuffixOverride { get; set; }

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

            if (VersionFile == null || !File.Exists(Path.Combine(RootDirectory.ItemSpec, VersionFile.ItemSpec)))
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

            // TODO: Right now we patch the VersionFile, but ideally we should make a copy and make the build system use it
            var versionFileData = File.ReadAllText(Path.Combine(RootDirectory.ItemSpec, VersionFile.ItemSpec));

            var publicVersionMatch = Regex.Match(versionFileData, "PublicVersion = \"(.*)\";");
            var versionSuffixMatch = Regex.Match(versionFileData, "NuGetVersionSuffix = \"(.*)\";");
            var publicVersion = publicVersionMatch.Success ? publicVersionMatch.Groups[1].Value : "0.0.0.0";
            var versionSuffix = versionSuffixMatch.Success ? versionSuffixMatch.Groups[1].Value : string.Empty;

            if (NuGetVersionSuffixOverride != null)
                versionSuffix = NuGetVersionSuffixOverride;

            // Patch NuGetVersion
            if (SpecialVersion != null)
                versionSuffix += SpecialVersion;

            EnsureLibGit2UnmanagedInPath(mainPlatformDirectory);

            // Compute Git Height using Nerdbank.GitVersioning
            // For now we assume top level package directory is git folder
            try
            {
                var rootDirectory = RootDirectory.ItemSpec;

                var repo = LibGit2Sharp.Repository.IsValid(rootDirectory) ? new LibGit2Sharp.Repository(rootDirectory) : null;
                if (repo == null)
                {
                    Log.LogError("Could not open Git repository");
                    return false;
                }

                // Patch AssemblyInformationalVersion
                var headCommitSha = repo.Head.Commits.FirstOrDefault()?.Sha;

                if (SpecialVersionGitHeight)
                {
                    // Compute version based on Git info
                    var height = Nerdbank.GitVersioning.GitExtensions.GetVersionHeight(repo, VersionFile.ItemSpec);
                    versionSuffix += $"-{height.ToString("D4")}";
                }

                // Replace NuGetVersionSuffix
                versionFileData = Regex.Replace(versionFileData, "NuGetVersionSuffix = \"(.*)\";", $"NuGetVersionSuffix = \"{versionSuffix}\";");

                // Always include git commit (even if not part of NuGetVersionSuffix)
                if (SpecialVersionGitCommit && headCommitSha != null)
                {
                    // Replace build metadata
                    versionFileData = Regex.Replace(versionFileData, "BuildMetadata = (.*);", $"BuildMetadata = \"+g{headCommitSha.Substring(0, 8)}\";");
                }

                // Write back new file
                File.WriteAllText(Path.Combine(RootDirectory.ItemSpec, GeneratedVersionFile.ItemSpec), versionFileData);

                NuGetVersion = publicVersion + versionSuffix;

                return true;
            }
            catch (Exception e)
            {
                NuGetVersion = publicVersion + versionSuffix;
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
