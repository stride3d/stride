// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stride.Core.Assets;
using Stride.Core.Assets.Analysis;
using Stride.Core.Assets.Editor.Components.TemplateDescriptions;
using Stride.Core.Assets.Templates;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.Presentation.Services;
using Stride.Assets.Templates;
using Stride.Graphics;

namespace Stride.Assets.Presentation.Templates
{
    public class TemplateSampleGenerator : SessionTemplateGenerator
    {
        private static readonly PropertyKey<Package> GeneratedPackageKey = new PropertyKey<Package>("GeneratedPackage", typeof(TemplateSampleGenerator));
        private static readonly PropertyKey<List<SelectedSolutionPlatform>> PlatformsKey = new PropertyKey<List<SelectedSolutionPlatform>>("Platforms", typeof(TemplateSampleGenerator));
        private static readonly PropertyKey<bool> AddGamesTestingKey = new PropertyKey<bool>("AddGamesTesting", typeof(TemplateSampleGenerator));

        public static readonly TemplateSampleGenerator Default = new TemplateSampleGenerator();

        /// <summary>
        /// Sets the parameters required by this template when running in <see cref="TemplateGeneratorParameters.Unattended"/> mode.
        /// </summary>
        public static void SetParameters(SessionTemplateGeneratorParameters parameters, IEnumerable<SelectedSolutionPlatform> platforms, bool addGamesTesting = false)
        {
            parameters.SetTag(PlatformsKey, new List<SelectedSolutionPlatform>(platforms));
            parameters.SetTag(AddGamesTestingKey, addGamesTesting);
        }

        public override bool IsSupportingTemplate(TemplateDescription templateDescription)
        {
            if (templateDescription == null) throw new ArgumentNullException(nameof(templateDescription));
            return templateDescription is TemplateSampleDescription;
        }

        public override async Task<bool> PrepareForRun(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            if (!parameters.Unattended)
            {
                var window = new UpdatePlatformsWindow(new[] { PlatformType.Windows })
                {
                    ForcePlatformRegenerationVisible = false
                };

                await window.ShowModal();

                if (window.Result == DialogResult.Cancel)
                    return false;

                parameters.SetTag(PlatformsKey, new List<SelectedSolutionPlatform>(window.SelectedPlatforms));
            }
            return true;
        }

        protected override bool Generate(SessionTemplateGeneratorParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            parameters.Validate();

            var description = (TemplateSampleDescription)parameters.Description;
            var log = parameters.Logger;

            // The package might depend on other packages which need to be copied together when instanciating it.
            //  However some of these packages might be in a parent folder, which can result in undesired behavior when copied.
            //  Setting this to true will enforce all package dependencies to be moved to a folder local to the project
            bool doMoveParentDependencies = true;

            var regexes = new List<Tuple<Regex, MatchEvaluator>>();
            var patternName = description.PatternName ?? description.DefaultOutputName;

            // Samples don't support spaces and dot in name (we would need to separate package name, package short name and namespace renaming for that).
            var parametersName = parameters.Name.Replace(" ", string.Empty).Replace(".", string.Empty);
            if (patternName != parametersName)
            {
                // Make sure the target name is a safe for use everywhere, since both an asset or script might reference a filename
                //  in which case they should match and be valid in that context
                string validNamespaceName = Utilities.BuildValidNamespaceName(parametersName);

                // Rename for general occurences of template name
                regexes.Add(new Tuple<Regex, MatchEvaluator>(new Regex($@"\b{patternName}\b"), match => validNamespaceName));
                
                // Rename App as well (used in code) -- this is the only pattern of "package short name" that we have so far in Windows samples
                regexes.Add(new Tuple<Regex, MatchEvaluator>(new Regex($@"\b{patternName}App\b"), match => validNamespaceName));
            }

            var outputDirectory = parameters.OutputDirectory;

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            //write gitignore
            WriteGitIgnore(parameters);

            UFile projectOutputFile = null;
            UFile projectInputFile = null;

            // Process files
            foreach (var directory in FileUtility.EnumerateDirectories(description.TemplateDirectory, SearchDirection.Down))
            {
                foreach (var file in directory.GetFiles())
                {
                    // If the file is ending with the Template extension or a directory with the sample extension, don;t copy it
                    if (file.FullName.EndsWith(TemplateDescription.FileExtension) ||
                        string.Compare(directory.Name, TemplateDescription.FileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        continue;
                    }
                    
                    var relativeFile = new UFile(file.FullName).MakeRelative(description.TemplateDirectory);

                    // Replace the name in the files if necessary
                    foreach (var nameRegex in regexes)
                    {
                        relativeFile = nameRegex.Item1.Replace(relativeFile, nameRegex.Item2);
                    }

                    // Create the output directory if needed
                    var outputFile = UPath.Combine(outputDirectory, relativeFile);
                    var outputFileDirectory = outputFile.GetParent();

                    // Determine if we are processing the main game project
                    var isPackageFile = (projectOutputFile == null && Path.GetExtension(file.FullName).ToLowerInvariant() == ".csproj" && !Path.GetFileNameWithoutExtension(file.FullName).EndsWith(".Windows"));

                    if (isPackageFile)
                    {
                        projectInputFile = file.FullName;
                        projectOutputFile = outputFile;
                    }

                    if (!Directory.Exists(outputFileDirectory))
                    {
                        Directory.CreateDirectory(outputFileDirectory);
                    }

                    if (IsBinaryFile(file.FullName))
                    {
                        File.Copy(file.FullName, outputFile, true);
                    }
                    else
                    {
                        ProcessTextFile(file.FullName, outputFile, regexes, (isPackageFile && doMoveParentDependencies));
                    }
                }
            }

            if (projectOutputFile != null)
            {
                var inputProject = (SolutionProject)Package.LoadProject(log, projectInputFile);
                var outputProject = (SolutionProject)Package.LoadProject(log, projectOutputFile);
                var msbuildProject = VSProjectHelper.LoadProject(outputProject.FullPath, platform: "NoPlatform");

                // If requested, add reference to Stride.Games.Testing
                if (parameters.TryGetTag(AddGamesTestingKey))
                {
                    var items = msbuildProject.AddItem("PackageReference", "Stride.Games.Testing", new[] { new KeyValuePair<string, string>("Version", StrideVersion.NuGetVersion), new KeyValuePair<string, string>("PrivateAssets", "contentfiles;analyzers") });
                    foreach (var item in items)
                    {
                        foreach (var metadata in item.Metadata)
                            metadata.Xml.ExpressedAsAttribute = true;
                    }
                }

                // Copy dependency files locally
                //  We only want to copy the asset files. The raw files are in Resources and the game assets are in Assets.
                //  If we copy each file locally they will be included in the package and we can then delete the dependency packages.
                foreach (var projectReference in msbuildProject.GetItems("ProjectReference").ToList())
                {
                    var packageDirectory = UPath.Combine(inputProject.FullPath.GetFullDirectory(), (UFile)projectReference.EvaluatedInclude).GetFullDirectory();
                    foreach (var directory in FileUtility.EnumerateDirectories(packageDirectory, SearchDirection.Down))
                    {
                        foreach (var file in directory.GetFiles())
                        {
                            // If the file is ending with the Template extension or a directory with the sample extension, don`t copy it
                            if (file.FullName.EndsWith(TemplateDescription.FileExtension) ||
                                string.Compare(directory.Name, TemplateDescription.FileExtension, StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                continue;
                            }

                            var relativeFile = new UFile(file.FullName).MakeRelative(packageDirectory);
                            var relativeFilename = relativeFile.ToString();

                            bool isAsset = relativeFilename.Contains("Assets");
                            bool isResource = relativeFilename.Contains("Resources");

                            if (!isAsset && !isResource)
                                continue;

                            // Replace the name in the files if necessary
                            foreach (var nameRegex in regexes)
                            {
                                relativeFile = nameRegex.Item1.Replace(relativeFile, nameRegex.Item2);
                            }

                            var outputFile = UPath.Combine(outputDirectory, relativeFile);
                            {   // Create the output directory if needed
                                var outputFileDirectory = outputFile.GetParent();
                                if (!Directory.Exists(outputFileDirectory))
                                {
                                    Directory.CreateDirectory(outputFileDirectory);
                                }
                            }

                            if (IsBinaryFile(file.FullName))
                            {
                                File.Copy(file.FullName, outputFile, true);
                            }
                            else
                            {
                                ProcessTextFile(file.FullName, outputFile, regexes);
                            }
                        }
                    }

                    msbuildProject.RemoveItem(projectReference);
                }

                // Save csproj without ProjectReferences
                msbuildProject.Save();
                msbuildProject.ProjectCollection.UnloadAllProjects();
                msbuildProject.ProjectCollection.Dispose();

                // Add package to session
                var loadParams = PackageLoadParameters.Default();
                loadParams.GenerateNewAssetIds = true;
                loadParams.LoadMissingDependencies = false;
                var session = parameters.Session;
                // We should switch to loading .csproj once all samples are upgraded
                var loadedProject = session.AddExistingProject(projectOutputFile, log, loadParams);

                RemoveUnusedAssets(loadedProject.Package, session);

                parameters.Tags.Add(GeneratedPackageKey, loadedProject.Package);
            }
            else
            {
                log.Error("Unable to find generated package for this template");
            }

            // Make sure we transfer overrides, etc. from what we deserialized to the asset graphs that we are going to save right after.
            ApplyMetadata(parameters);
            return true;
        }

        protected void RemoveUnusedAssets(Package loadedPackage, PackageSession session)
        {
            List<AssetItem> assetsToRemove = new List<AssetItem>();
            foreach (var asset in loadedPackage.Assets)
            {
                var assetIsRequired = (asset.Asset is SourceCodeAsset || asset.Asset.GetType().IsAssignableFrom(typeof(SourceCodeAsset)));
                assetIsRequired |= AssetRegistry.IsAssetTypeAlwaysMarkAsRoot(asset.Asset.GetType());
                assetIsRequired |= loadedPackage.RootAssets.ContainsKey(asset.Id);

                if (!assetIsRequired)
                {
                    // Search dependencies to check if any of them is a root asset
                    var depsIn = session.DependencyManager.ComputeDependencies(asset.Id, AssetDependencySearchOptions.In | AssetDependencySearchOptions.Recursive);
                    if (depsIn != null)
                    {
                        foreach (var d in depsIn.LinksIn)
                        {
                            assetIsRequired |= AssetRegistry.IsAssetTypeAlwaysMarkAsRoot(d.Item.Asset.GetType());
                            assetIsRequired |= loadedPackage.RootAssets.ContainsKey(d.Item.Id);
                        }
                    }
                }

                if (!assetIsRequired)
                {
                    assetsToRemove.Add(asset);
                }
            }

            foreach (var asset in assetsToRemove)
            {
                loadedPackage.Assets.RemoveById(asset.Id);
                if (File.Exists(asset.FullPath))
                    File.Delete(asset.FullPath);
            }
        }

        protected override async Task<bool> AfterSave(SessionTemplateGeneratorParameters parameters)
        {
            // If package was not generated
            Package package;
            parameters.Tags.TryGetValue(GeneratedPackageKey, out package);
            if (package == null)
            {
                return false;
            }
            // Update platforms for the sample
            var updateSample = TemplateManager.FindTemplates(package.Session).FirstOrDefault(template => template.Id == UpdatePlatformsTemplateGenerator.TemplateId);
            parameters.Description = updateSample;

            var updateParameters = new PackageTemplateGeneratorParameters(parameters, package);
            updateParameters.Unattended = true;
            var orientation = package.GetGameSettingsAsset()?.GetOrCreate<RenderingSettings>().DisplayOrientation ?? RequiredDisplayOrientation.Default;
            UpdatePlatformsTemplateGenerator.SetOrientation(updateParameters, (DisplayOrientation)orientation);
            UpdatePlatformsTemplateGenerator.SetPlatforms(updateParameters, parameters.GetTag(PlatformsKey));

            // We want to force regeneration of Windows platform in case the sample .csproj is outdated
            UpdatePlatformsTemplateGenerator.SetForcePlatformRegeneration(updateParameters, true);

            var updateTemplate = UpdatePlatformsTemplateGenerator.Default;
            if (!await updateTemplate.PrepareForRun(updateParameters) || !updateTemplate.Run(updateParameters))
            {
                // Remove the created project
                var path = Path.GetDirectoryName(parameters.Session.SolutionPath.ToWindowsPath());
                try
                {
                    Directory.Delete(path ?? "", true);
                }
                catch (IOException ex)
                {
                    parameters.Logger.Error("Error when removing generated project.", ex);
                }
                catch (UnauthorizedAccessException ex)
                {
                    parameters.Logger.Error("Error when removing generated project.", ex);
                }
                // Notify cancellation
                return false;
            }

            // Save again post update
            SaveSession(parameters);

            // Make sure platform projects also gets in fully loaded state
            package.Session.LoadMissingReferences(parameters.Logger);

            return true;
        }

        private static void ProcessTextFile(string inputFile, string outputFile, IEnumerable<Tuple<Regex, MatchEvaluator>> regexes, bool replaceParentDir = false)
        {
            var content = File.ReadAllText(inputFile);

            foreach (var regex in regexes)
            {
                content = regex.Item1.Replace(content, regex.Item2);
            }

            if (replaceParentDir)
            {
                content = content.Replace("../", string.Empty);
            }

            File.WriteAllText(outputFile, content);
        }

        private static bool IsBinaryFile(string file)
        {
            var buffer = new byte[8192];
            using (var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                var length = stream.Read(buffer, 0, buffer.Length);
                for (int i = 0; i < length; i++)
                {
                    if (buffer[i] == 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
    }
}
