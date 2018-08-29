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
        private static readonly string SolutionHeader = @"Microsoft Visual Studio Solution File, Format Version 12.00
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

        public static void SaveSolution(PackageSession session, ILogger log)
        {
            // If the solution path is not set, we can't save a solution (sln)
            if (session.SolutionPath == null)
            {
                return;
            }

            var solutionPath = UPath.Combine(Environment.CurrentDirectory, session.SolutionPath);

            try
            {
                Solution solution;

                var solutionDir = solutionPath.GetParent();

                // If the solution already exists, we need to update it
                if (File.Exists(solutionPath))
                {
                    solution = Solution.FromFile(solutionPath);
                }
                else
                {
                    solution = new Solution { FullPath = solutionPath };
                    solution.Headers.Add(SolutionHeader);
                }

                // Pre-create solution wide global sections
                if (!solution.GlobalSections.Contains("SolutionConfigurationPlatforms"))
                {
                    solution.GlobalSections.Add(new Section("SolutionConfigurationPlatforms", "GlobalSection", "preSolution", Enumerable.Empty<PropertyItem>()));
                }
                if (!solution.GlobalSections.Contains("ProjectConfigurationPlatforms"))
                {
                    solution.GlobalSections.Add(new Section("ProjectConfigurationPlatforms", "GlobalSection", "postSolution", Enumerable.Empty<PropertyItem>()));
                }
                if (!solution.GlobalSections.Contains("NestedProjects"))
                {
                    solution.GlobalSections.Add(new Section("NestedProjects", "GlobalSection", "preSolution", Enumerable.Empty<PropertyItem>()));
                }

                // ---------------------------------------------
                // 0. Pre-select only platforms effectively used by this session
                // ---------------------------------------------
                var platformsUsedBySession = new SolutionPlatformCollection();
                platformsUsedBySession.AddRange(AssetRegistry.SupportedPlatforms.Where(platform => platform.Type == PlatformType.Windows));

                foreach (var package in session.LocalPackages)
                {
                    foreach (var profile in package.Profiles.Where(profile => profile.Platform != PlatformType.Shared && profile.ProjectReferences.Count > 0))
                    {
                        var platformType = profile.Platform;
                        if (!platformsUsedBySession.Contains(platformType))
                        {
                            platformsUsedBySession.AddRange(AssetRegistry.SupportedPlatforms.Where(platform => platform.Type == platformType));
                        }
                    }
                }

                // ---------------------------------------------
                // 1. Update configuration/platform
                // ---------------------------------------------
                var configs = new List<Tuple<string, SolutionPlatform, SolutionPlatformPart>>();
                foreach (var configName in platformsUsedBySession.SelectMany(solutionPlatform => solutionPlatform.Configurations).Select(config => config.Name).Distinct())
                {
                    foreach (var platform in platformsUsedBySession)
                    {
                        foreach (var platformPart in platform.GetParts())
                        {
                            // Skip platforms with IncludeInSolution == false
                            if (!platformPart.IncludeInSolution)
                                continue;

                            configs.Add(new Tuple<string, SolutionPlatform, SolutionPlatformPart>(configName, platform, platformPart));
                        }
                    }
                }

                // Order per config and then per platform names
                configs = configs.OrderBy(part => part.Item1, StringComparer.InvariantCultureIgnoreCase).ThenBy(part => part.Item3.SafeSolutionName, StringComparer.InvariantCultureIgnoreCase).ToList();

                // Write configs in alphabetical order to avoid changes in sln after it is generated
                var solutionPlatforms = solution.GlobalSections["SolutionConfigurationPlatforms"];
                solutionPlatforms.Properties.Clear();
                foreach (var config in configs)
                {
                    var solutionConfigPlatform = string.Format("{0}|{1}", config.Item1, config.Item3.SafeSolutionName);
                    if (!solutionPlatforms.Properties.Contains(solutionConfigPlatform))
                    {
                        solutionPlatforms.Properties.Add(new PropertyItem(solutionConfigPlatform, solutionConfigPlatform));
                    }                    
                }

                // Remove projects that are no longer available on the disk
                var projectToRemove = solution.Projects.Where(project => !project.IsSolutionFolder && !File.Exists(project.GetFullPath(solution))).ToList();
                foreach (var project in projectToRemove)
                {
                    solution.Projects.Remove(project);
                }

                // ---------------------------------------------
                // 2. Update each package
                // ---------------------------------------------
                foreach (var package in session.LocalPackages)
                {
                    if (string.IsNullOrWhiteSpace(package.Meta.Name))
                    {
                        log.Error($"Error while saving solution [{solutionPath}]. Package [{package.FullPath}] should have a Meta.Name");
                        continue;
                    }

                    var packageFolder = solution.Projects.FindByGuid((Guid)package.Id);

                    // Packages are created as solution folders in VisualStudio
                    if (packageFolder == null)
                    {
                        // Create this package as a Solution Folder 
                        packageFolder = new Project(
                            package.Id,
                            KnownProjectTypeGuid.SolutionFolder,
                            package.Meta.Name,
                            package.Meta.Name,
                            Guid.Empty,
                            Enumerable.Empty<Section>(),
                            Enumerable.Empty<PropertyItem>(),
                            Enumerable.Empty<PropertyItem>());

                        // As it is making a copy, we need to get it back
                        solution.Projects.Add(packageFolder);
                        packageFolder = solution.Projects[package.Id];
                    }

                    // Update the path to the solution everytime we save a package
                    packageFolder.Sections.Clear();
                    var relativeUrl = package.FullPath.MakeRelative(solutionDir);
                    packageFolder.Sections.Add(new Section(XenkoPackage, "ProjectSection", "preProject", new[] { new PropertyItem(relativeUrl, relativeUrl) }));

                    // ---------------------------------------------
                    // 2.1. Update each project
                    // ---------------------------------------------
                    foreach (var profile in package.Profiles.OrderBy(x => x.Platform == PlatformType.Windows ? 0 : 1))
                    {
                        foreach (var project in profile.ProjectReferences)
                        {
                            var projectInSolution = solution.Projects.FindByGuid(project.Id);

                            if (projectInSolution == null)
                            {
                                var projectRelativePath = project.Location.MakeRelative(solutionDir);

                                // Create this package as a Solution Folder 
                                projectInSolution = new Project(
                                    project.Id,
                                    KnownProjectTypeGuid.CSharp,
                                    project.Location.GetFileNameWithoutExtension(),
                                    projectRelativePath.ToWindowsPath(),
                                    package.Id,
                                    Enumerable.Empty<Section>(),
                                    Enumerable.Empty<PropertyItem>(),
                                    Enumerable.Empty<PropertyItem>());

                                solution.Projects.Add(projectInSolution);

                                // Resolve it again, as the original code is making a clone of it (why?)
                                projectInSolution = solution.Projects.FindByGuid(project.Id);
                            }

                            // Projects are always in a package (solution folder) in the solution
                            projectInSolution.ParentGuid = package.Id;

                            // Update platforms per project (active solution and build flag per platform)

                            // Clear all platform properties for this project and recompute them here
                            projectInSolution.PlatformProperties.Clear();

                            foreach (var config in configs)
                            {
                                var configName = config.Item1;
                                var platform = config.Item2;
                                var platformPart = config.Item3;

                                // Filter exe project types
                                if (project.Type == ProjectType.Executable && !platformPart.UseWithExecutables)
                                {
                                    continue;
                                }

                                var platformName = platformPart.SafeSolutionName;

                                var solutionConfigPlatform = string.Format("{0}|{1}", configName, platformName);

                                var configNameInProject = configName;
                                var platformNameInProject = platformPart.GetProjectName(project.Type);

                                var platformTarget = platform;
                                if (profile.Platform != PlatformType.Shared)
                                {
                                    platformTarget = platformsUsedBySession.FirstOrDefault(plat => plat.Type == profile.Platform);
                                    if (platformTarget == null)
                                    {
                                        // This should not happen as we control our platforms, but when we develop a new one
                                        // we might get it and it is better to cleary state why we are failing.
                                        log.Error("Project contains an unsupported platform " + profile.Platform);
                                        throw new InvalidOperationException("Unsupported platform " + profile.Platform);
                                    }
                                }

                                bool isPartOfBuild = platformTarget == platform;
                                // If the config doesn't exist for this platform, just use the default config name 
                                if (!platformTarget.Configurations.Contains(configName))
                                {
                                    configNameInProject = platformTarget.Configurations.FirstOrDefault().Name;
                                    isPartOfBuild = false;
                                }

                                // If the config doesn't exist for this platform, just use the default config name 
                                if (platformTarget.GetParts().All(part => part.GetProjectName(project.Type) != platformNameInProject))
                                {
                                    platformNameInProject = platformTarget.GetParts().FirstOrDefault(part => part.IsProjectHandled(project.Type)).SafeSolutionName;
                                    isPartOfBuild = false;
                                }

                                var projectConfigPlatform = string.Format("{0}|{1}", configNameInProject, platformNameInProject);

                                var propertyActive = solutionConfigPlatform + ".ActiveCfg";
                                var propertyBuild = solutionConfigPlatform + ".Build.0";

                                if (!projectInSolution.PlatformProperties.Contains(propertyActive))
                                {
                                    projectInSolution.PlatformProperties.Remove(propertyActive);
                                    projectInSolution.PlatformProperties.Add(new PropertyItem(propertyActive, projectConfigPlatform));
                                }

                                // Only add Build and Deploy for supported configs
                                if (isPartOfBuild)
                                {
                                    projectInSolution.PlatformProperties.Remove(propertyBuild);
                                    projectInSolution.PlatformProperties.Add(new PropertyItem(propertyBuild, projectConfigPlatform));

                                    // If the project is an executable, mark it as deploy
                                    if (project.Type == ProjectType.Executable)
                                    {
                                        var propertyDeploy = solutionConfigPlatform + ".Deploy.0";
                                        projectInSolution.PlatformProperties.Remove(propertyDeploy);
                                        projectInSolution.PlatformProperties.Add(new PropertyItem(propertyDeploy, projectConfigPlatform));
                                    }
                                }
                            }
                        }
                    }

                    // ---------------------------------------------
                    // 3. Remove unused packages from the solution
                    // ---------------------------------------------
                    for (int i = solution.Projects.Count - 1; i >=0; i--)
                    {
                        var project = solution.Projects[i];
                        if (IsPackage(project) && !session.Packages.ContainsById(project.Guid))
                        {
                            solution.Projects.RemoveAt(i);
                        }
                    }
                }

                solution.Save();
            }
            catch (Exception ex)
            {
                log.Error($"Error while saving solution [{solutionPath}]", ex);
            }
        }
    }
}
