// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Templates;
using Xenko.Core;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.ProjectTemplating;
using Xenko.Graphics;
using Xenko.Shaders.Parser.Mixins;
using Xenko.Core.VisualStudio;
using Xenko.Core.Extensions;

namespace Xenko.Assets.Templates
{
    public static class ProjectTemplateGeneratorHelper
    {
        private static readonly PropertyKey<Dictionary<string, object>> OptionsKey = new PropertyKey<Dictionary<string, object>>("Options", typeof(ProjectTemplateGeneratorHelper));

        public static UDirectory GetTemplateDataDirectory(TemplateDescription template)
        {
            return template.TemplateDirectory;
        }

        public static void AddOption(TemplateGeneratorParameters parameters, string optionName, object optionValue)
        {
            Dictionary<string, object> options;
            if (!parameters.Tags.TryGetValue(OptionsKey, out options))
            {
                options = new Dictionary<string, object>();
                parameters.Tags.Add(OptionsKey, options);
            }
            options[optionName] = optionValue;
        }

        public static IReadOnlyDictionary<string, object> GetOptions(TemplateGeneratorParameters parameters)
        {
            Dictionary<string, object> options;
            return parameters.Tags.TryGetValue(OptionsKey, out options) ? options : new Dictionary<string, object>();
        }

        public static IEnumerable<SolutionProject> UpdatePackagePlatforms(PackageTemplateGeneratorParameters packageParameters, ICollection<SelectedSolutionPlatform> platforms, DisplayOrientation orientation, bool forcePlatformRegeneration)
        {
            if (platforms == null) throw new ArgumentNullException(nameof(platforms));
            var logger = packageParameters.Logger;
            var package = packageParameters.Package;
            var name = packageParameters.Name;

            var addedProjects = new List<SolutionProject>();

            // Adjust output directory
            var parameters = new SessionTemplateGeneratorParameters
            {
                Logger = logger,
                Name = name,
                OutputDirectory = package.FullPath.GetFullDirectory().GetParent(),
                Session = package.Session,
                Description = packageParameters.Description,
            };

            // Setup the ProjectGameGuid to be accessible from exec (in order to be able to link to the game project.
            AddOption(parameters, "ProjectGameGuid", (package.Container as SolutionProject)?.Id ?? Guid.Empty);
            AddOption(parameters, "ProjectGameRelativePath", (package.Container as SolutionProject)?.FullPath.MakeRelative(parameters.OutputDirectory).ToWindowsPath());
            AddOption(parameters, "PackageGameAssemblyName", package.Meta.Name);

            // Sample templates still have .Game in their name
            var packageNameWithoutGame = package.Meta.Name;
            if (packageNameWithoutGame.EndsWith(".Game"))
                packageNameWithoutGame = packageNameWithoutGame.Substring(0, packageNameWithoutGame.Length - ".Game".Length);

            AddOption(parameters, "PackageGameName", packageNameWithoutGame);
            AddOption(parameters, "PackageGameDisplayName", package.Meta.Title ?? packageNameWithoutGame);
            // Escape illegal characters for the short name
            AddOption(parameters, "PackageGameNameShort", Utilities.BuildValidClassName(packageNameWithoutGame.Replace(" ", string.Empty)));
            AddOption(parameters, "PackageGameRelativePath", package.FullPath.MakeRelative(parameters.OutputDirectory).ToWindowsPath());

            // Override namespace
            AddOption(parameters, "Namespace", parameters.Namespace ?? Utilities.BuildValidNamespaceName(packageNameWithoutGame));

            // Add projects
            var stepIndex = 0;
            var stepCount = platforms.Count + 1;
            var projectsToRemove = AssetRegistry.SupportedPlatforms
                .Where(platform => !platforms.Select(x => x.Platform).Contains(platform))
                .Select(platform =>
                {
                    var projectFullPath = GeneratePlatformProjectLocation(name, package, platform);
                    return package.Session.Projects.OfType<SolutionProject>().FirstOrDefault(project => project.FullPath == projectFullPath);
                })
                .NotNull()
                .ToList();

            foreach (var platform in platforms)
            {
                stepIndex++;

                var projectFullPath = GeneratePlatformProjectLocation(name, package, platform.Platform);
                var projectName = projectFullPath.GetFileNameWithoutExtension();

                // Don't add a platform that is already in the package
                var existingProject = package.Session.Projects.OfType<SolutionProject>().FirstOrDefault(x => x.FullPath == projectFullPath);

                var projectGuid = Guid.NewGuid();

                if (existingProject != null)
                {
                    if (!forcePlatformRegeneration)
                        continue;

                    projectGuid = existingProject.Id;

                    // We are going to regenerate this platform, so we are removing it before
                    package.Session.Projects.Remove(existingProject);
                }

                var projectDirectory = Path.GetDirectoryName(projectFullPath.ToWindowsPath());
                if (projectDirectory != null && Directory.Exists(projectDirectory))
                {
                    try
                    {
                        Directory.Delete(projectDirectory, true);
                    }
                    catch (Exception)
                    {
                        logger.Warning($"Unable to delete directory [{projectDirectory}]");
                    }
                }

                var templatePath = platform.Template?.TemplatePath ?? $"ProjectExecutable.{platform.Platform.Name}/ProjectExecutable.{platform.Platform.Name}.ttproj";

                // Log progress
                Progress(logger, $"Generating {projectName}...", stepIndex - 1, stepCount);

                var graphicsPlatform = platform.Platform.Type.GetDefaultGraphicsPlatform();
                var newExeProject = GenerateTemplate(parameters, platforms, templatePath, projectName, platform.Platform.Type, graphicsPlatform, ProjectType.Executable, orientation, projectGuid);

                package.Session.Projects.Add(newExeProject);

                package.Session.IsDirty = true;

                addedProjects.Add(newExeProject);
            }

            foreach (var project in projectsToRemove)
            {
                var projectFullPath = project.FullPath;
                var projectDirectory = Path.GetDirectoryName(projectFullPath.ToWindowsPath());
                if (projectDirectory != null && Directory.Exists(projectDirectory))
                {
                    try
                    {
                        Directory.Delete(projectDirectory, true);
                    }
                    catch (Exception)
                    {
                        logger.Warning($"Unable to delete directory [{projectDirectory}]");
                    }
                }

                package.Session.Projects.Remove(project);
                package.Session.IsDirty = true;
            }

            return addedProjects;
        }

        public static UFile GeneratePlatformProjectLocation(string name, Package package, SolutionPlatform platform)
        {
            // Remove .Game suffix
            if (name.EndsWith(".Game"))
                name = name.Substring(0, name.Length - ".Game".Length);

            var projectName = Utilities.BuildValidNamespaceName(name) + "." + platform.Name;
            return UPath.Combine(UPath.Combine(package.RootDirectory.GetParent(), (UDirectory)projectName), (UFile)(projectName + ".csproj"));
        }

        public static SolutionProject GenerateTemplate(TemplateGeneratorParameters parameters, ICollection<SelectedSolutionPlatform> platforms, UFile templateRelativePath, string projectName, PlatformType platformType, GraphicsPlatform? graphicsPlatform, ProjectType projectType, DisplayOrientation orientation, Guid? projectGuid = null)
        {
            AddOption(parameters, "Platforms", platforms.Select(x => x.Platform).ToList());
            AddOption(parameters, "CurrentPlatform", platformType);
            AddOption(parameters, "Orientation", orientation);

            List<string> generatedFiles;
            var project = GenerateTemplate(parameters, templateRelativePath, projectName, platformType, graphicsPlatform, projectType, out generatedFiles, projectGuid);

            // Special case for xkfx files
            foreach (var file in generatedFiles)
            {
                if (file.EndsWith(".xkfx"))
                {
                    ConvertXkfxToCSharp(file);
                }
            }

            return project;
        }

        public static SolutionProject GenerateTemplate(TemplateGeneratorParameters parameters, UFile templateRelativePath, string projectName, PlatformType platformType, GraphicsPlatform? graphicsPlatform, ProjectType projectType, out List<string> generatedFiles, Guid? projectGuidArg = null)
        {
            var options = GetOptions(parameters);
            var outputDirectoryPath = UPath.Combine(parameters.OutputDirectory, (UDirectory)projectName);
            Directory.CreateDirectory(outputDirectoryPath);

            generatedFiles = new List<string>();
            parameters.Logger.Verbose($"Generating {projectName}...");

            var projectGuid = projectGuidArg ?? Guid.NewGuid();
            var packagePath = UPath.Combine(outputDirectoryPath, (UFile)(projectName + Package.PackageFileExtension));
            var projectFullPath = UPath.Combine(outputDirectoryPath, (UFile)(projectName + ".csproj"));

            var package = new Package
            {
                Meta =
                {
                    Name = projectName,
                    Version = new PackageVersion("1.0.0.0")
                },
                FullPath = packagePath,
                IsDirty = true,
            };

            package.AssetFolders.Add(new AssetFolder("Assets"));
            package.ResourceFolders.Add("Resources");

            var projectTemplate = PrepareTemplate(parameters, package, templateRelativePath, platformType, graphicsPlatform, projectType);
            projectTemplate.Generate(outputDirectoryPath, projectName, projectGuid, parameters.Logger, options, generatedFiles);

            var project = new SolutionProject(package, projectGuid, projectFullPath);
            project.Type = projectType;
            project.Platform = platformType;

            return project;
        }

        public static ProjectTemplate PrepareTemplate(TemplateGeneratorParameters parameters, Package package, UFile templateRelativePath, PlatformType platformType, GraphicsPlatform? graphicsPlatform, ProjectType projectType)
        {
            if (platformType != PlatformType.Shared && !graphicsPlatform.HasValue)
            {
                throw new ArgumentException(@"Expecting a value for GraphicsPlatform when platformType is specified", nameof(graphicsPlatform));
            }

            var rootTemplateDir = parameters.Description.TemplateDirectory;

            var templateFilePath = UPath.Combine(rootTemplateDir, templateRelativePath);
            var projectTemplate = ProjectTemplate.Load(templateFilePath);
            // TODO assemblies are not configurable from the outside
            projectTemplate.Assemblies.Add(typeof(ProjectType).Assembly.FullName);
            projectTemplate.Assemblies.Add(typeof(XenkoConfig).Assembly.FullName);
            projectTemplate.Assemblies.Add(typeof(GraphicsPlatform).Assembly.FullName);
            projectTemplate.Assemblies.Add(typeof(DisplayOrientation).Assembly.FullName);

            AddOption(parameters, "Package", package);
            AddOption(parameters, "PackageName", package.Meta.Name);

            // PackageNameCode, same as PackageName without '.' and ' '.
            AddOption(parameters, "PackageNameCode", package.Meta.Name.Replace(" ", string.Empty).Replace(".", string.Empty));

            AddOption(parameters, "PackageDisplayName", package.Meta.Title ?? package.Meta.Name);
            // Escape illegal characters for the short name
            AddOption(parameters, "PackageNameShort", Utilities.BuildValidClassName(package.Meta.Name.Replace(" ", string.Empty)));

            AddOption(parameters, "CurrentPlatform", platformType);
            if (platformType != PlatformType.Shared)
            {
                AddOption(parameters, "CurrentGraphicsPlatform", graphicsPlatform);
            }

            AddOption(parameters, "ProjectType", projectType);
            AddOption(parameters, "Namespace", parameters.Namespace ?? Utilities.BuildValidNamespaceName(package.Meta.Name));

            return projectTemplate;
        }

        public static void Progress(ILogger log, string message, int stepIndex, int stepCount)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var progress = log as IProgressStatus;
            progress?.OnProgressChanged(new ProgressStatusEventArgs(message, stepIndex, stepCount));
        }

        private static void ConvertXkfxToCSharp(string xkfxfile)
        {
            var xkfileContent = File.ReadAllText(xkfxfile);
            var result = ShaderMixinCodeGen.GenerateCsharp(xkfileContent, xkfxfile);
            File.WriteAllText(Path.ChangeExtension(xkfxfile, ".cs"), result, Encoding.UTF8);
        }

        private static void RemoveProject(ProjectReference projectReference, ILogger logger)
        {
            var projectFullPath = projectReference.Location.FullPath;
            var projectDirectory = Path.GetDirectoryName(projectFullPath);
            if (projectDirectory != null && Directory.Exists(projectDirectory))
            {
                try
                {
                    Directory.Delete(projectDirectory, true);
                }
                catch (Exception)
                {
                    logger.Warning($"Unable to delete directory [{projectDirectory}]");
                }
            }
        }
    }
}
