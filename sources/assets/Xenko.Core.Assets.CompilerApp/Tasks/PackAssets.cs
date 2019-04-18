// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xenko.Core.Diagnostics;
using Xenko.Core.IO;
using Xenko.Core.Yaml;
using Xenko.Core.Yaml.Events;

namespace Xenko.Core.Assets.CompilerApp.Tasks
{
    public static class PackAssetsHelper
    {
        public static bool Run(Core.Diagnostics.Logger logger, string projectFile, string intermediatePackagePath, List<(string SourcePath, string PackagePath)> generatedItems)
        {
            var package = Package.Load(logger, projectFile, new PackageLoadParameters()
            {
                AutoCompileProjects = false,
                LoadAssemblyReferences = false,
                AutoLoadTemporaryAssets = false,
            });

            var outputPath = new UDirectory(new FileInfo(intermediatePackagePath).FullName);
            var newPackage = new Package
            {
                Meta = package.Meta,
                FullPath = UPath.Combine(outputPath, (UFile)package.FullPath.GetFileName()),
            };

            var resourceOutputPath = UPath.Combine(outputPath, (UDirectory)"Resources");
            var resourcesTargetToSource = new Dictionary<UFile, UFile>();
            var resourcesSourceToTarget = new Dictionary<UFile, UFile>();

            void RegisterItem(UFile targetFilePath)
            {
                generatedItems.Add((targetFilePath.ToWindowsPath(), UPath.Combine("xenko", targetFilePath.MakeRelative(outputPath)).ToWindowsPath()));
            }

            void TryCopyDirectory(UDirectory sourceDirectory, UDirectory targetDirectory, string exclude = null)
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddInclude("**/*.*");
                if (exclude != null)
                {
                    foreach (var excludeEntry in exclude.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                        matcher.AddExclude(excludeEntry);
                }

                //var resourceFiles = Directory.EnumerateFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);
                foreach (var resourceFile in matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(sourceDirectory))).Files)
                {
                    var resourceFilePath = UPath.Combine(sourceDirectory, (UFile)resourceFile.Path);
                    var targetFilePath = UPath.Combine(targetDirectory, (UFile)resourceFile.Path);

                    TryCopyResource(resourceFilePath, targetFilePath);
                }
            }

            void TryCopyResource(UFile resourceFilePath, UFile targetFilePath)
            {
                resourcesSourceToTarget.Add(resourceFilePath, targetFilePath);

                if (resourcesTargetToSource.TryGetValue(targetFilePath, out var otherResourceFilePath))
                {
                    logger.Error($"Could not copy resource file [{targetFilePath.MakeRelative(resourceOutputPath)}] because it exists in multiple locations: [{resourceFilePath.ToWindowsPath()}] and [{otherResourceFilePath.ToWindowsPath()}]");
                }
                else
                {
                    resourcesTargetToSource.Add(targetFilePath, resourceFilePath);

                    try
                    {
                        Directory.CreateDirectory(targetFilePath.GetFullDirectory());
                        File.Copy(resourceFilePath, targetFilePath, true);

                        RegisterItem(targetFilePath);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Could not copy resource file from [{resourceFilePath.ToWindowsPath()}] to [{targetFilePath.MakeRelative(resourceOutputPath)}]", e);
                    }
                }
            }

            foreach (var resourceFolder in package.ResourceFolders)
            {
                if (!Directory.Exists(resourceFolder))
                    continue;

                TryCopyDirectory(resourceFolder, resourceOutputPath);
            }

            var assetOutputPath = UPath.Combine(outputPath, (UDirectory)"Assets");
            var assets = Package.ListAssetFiles(logger, package, true, true, null);
            if (assets.Count > 0)
            {
                newPackage.AssetFolders.Add(new AssetFolder(assetOutputPath));

                foreach (var asset in assets)
                {
                    // Ignore source files
                    if (asset.FilePath.GetFileExtension() == ".cs")
                        continue;

                    var assetRelativePath = asset.FilePath.MakeRelative(asset.SourceFolder);
                    var outputFile = UPath.Combine(assetOutputPath, assetRelativePath);

                    try
                    {
                        var assetDirectory = asset.FilePath.GetFullDirectory();
                        Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                        var parsingEvents = new List<ParsingEvent>();

                        using (var assetStream = File.OpenRead(asset.FilePath))
                        using (var streamReader = new StreamReader(assetStream))
                        {
                            var yamlEventReader = new EventReader(new Parser(streamReader));
                            yamlEventReader.ReadCurrent(parsingEvents);

                            var hasChanges = false;
                            foreach (var parsingEvent in parsingEvents)
                            {
                                if (parsingEvent is Scalar scalar)
                                {
                                    if (scalar.Tag == "!file")
                                    {
                                        // Transform to absolute path
                                        var sourceResourcePath = UPath.Combine(asset.FilePath.GetFullDirectory(), (UFile)scalar.Value);
                                        // Check if file was copied in resource
                                        if (!resourcesSourceToTarget.TryGetValue(sourceResourcePath, out var targetResourcePath))
                                        {
                                            // This file was not stored in resource, copy it manually
                                            targetResourcePath = UPath.Combine(resourceOutputPath, (UFile)sourceResourcePath.GetFileName());
                                            TryCopyResource(sourceResourcePath, targetResourcePath);
                                        }
                                        var newValue = targetResourcePath.MakeRelative(outputFile.GetFullDirectory());
                                        if (scalar.Value != newValue)
                                        {
                                            hasChanges = true;
                                            scalar.Value = newValue;
                                        }
                                    }
                                }
                            }

                            if (!hasChanges)
                            {
                                // We do this because pure text files could be parsed as YAML events even though they are not
                                File.Copy(asset.FilePath, outputFile, true);
                            }
                            else
                            {
                                using (var output = File.CreateText(outputFile))
                                {
                                    var emitter = new Emitter(output, AssetYamlSerializer.Default.GetSerializerSettings().PreferredIndent);
                                    foreach (var parsingEvent in parsingEvents)
                                    {
                                        emitter.Emit(parsingEvent);
                                    }
                                }
                            }

                            RegisterItem(outputFile);
                        }
                    }
                    catch (YamlException e)
                    {
                        // Not a Yaml asset? Process it as binary (copy)
                        File.Copy(asset.FilePath, outputFile, true);
                        RegisterItem(outputFile);
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Could not process asset [{asset.FilePath}]", e);
                    }
                }
            }

            // If any resource was copied, add resource folder
            if (resourcesTargetToSource.Count > 0)
                newPackage.ResourceFolders.Add(resourceOutputPath);

            // Process templates
            if (package.TemplateFolders.Count > 0)
            {
                var templateOutputPath = UPath.Combine(outputPath, (UDirectory)"Templates");

                var targetFolder = new TemplateFolder(templateOutputPath);

                foreach (var templateFolder in package.TemplateFolders)
                {
                    UDirectory target = templateOutputPath;
                    if (templateFolder.Group != null)
                    {
                        target = UPath.Combine(target, templateFolder.Group);
                    }

                    TryCopyDirectory(templateFolder.Path, target, templateFolder.Exclude);

                    // Add template files
                    foreach (var templateFile in templateFolder.Files)
                    {
                        var newTemplateFile = templateFile.MakeRelative(templateFolder.Path);
                        if (templateFolder.Group != null)
                        {
                            newTemplateFile = UPath.Combine(templateFolder.Group, newTemplateFile);
                        }

                        newTemplateFile = UPath.Combine(targetFolder.Path, newTemplateFile);
                        targetFolder.Files.Add(newTemplateFile);
                    }
                }

                newPackage.TemplateFolders.Add(targetFolder);
            }

            foreach (var rootAsset in package.RootAssets)
                newPackage.RootAssets.Add(rootAsset);

            // Save package only if there is any resources and/or assets
            if (generatedItems.Count > 0)
            {
                // Make sure we have a standalone package
                var standalonePackage = new StandalonePackage(newPackage);
                standalonePackage.Save(logger);
                RegisterItem(newPackage.FullPath);
            }

            return !logger.HasErrors;
        }
    }
    public class PackAssets : Task
    {
        [Required]
        public ITaskItem ProjectFile { get; set; }

        [Required]
        public ITaskItem IntermediatePackagePath { get; set; }

        [Output]
        public ITaskItem[] GeneratedItems { get; private set; }

        public override bool Execute()
        {
            var generatedItems = new List<(string SourcePath, string PackagePath)>();
            var result = PackAssetsHelper.Run(new RedirectLog(Log), ProjectFile.ItemSpec, IntermediatePackagePath.ItemSpec, generatedItems);

            GeneratedItems = generatedItems.Select(x =>
            {
                var generatedItem = new TaskItem(x.SourcePath);
                generatedItem.SetMetadata("Pack", "true");
                generatedItem.SetMetadata("PackagePath", x.PackagePath);
                return generatedItem;
            }).ToArray();
            return result;
        }

        class RedirectLog : Core.Diagnostics.Logger
        {
            TaskLoggingHelper log;

            public RedirectLog(TaskLoggingHelper log)
            {
                this.log = log;

                // Report warnings and errors
                ActivateLog(LogMessageType.Warning);
            }

            protected override void LogRaw(ILogMessage logMessage)
            {
                switch (logMessage.Type)
                {
                    case LogMessageType.Debug:
                    case LogMessageType.Verbose:
                    case LogMessageType.Info:
                        log.LogMessage(logMessage.Text);
                        break;
                    case LogMessageType.Warning:
                        log.LogWarning(logMessage.Text);
                        break;
                    case LogMessageType.Error:
                    case LogMessageType.Fatal:
                        log.LogError(logMessage.Text);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
